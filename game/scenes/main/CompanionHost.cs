using System;
using System.Buffers.Text;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using Godot;
using QRCoder;
using TheogonyX.Core.Protocol;
using TheogonyX.Data;

namespace TheogonyX.Scenes.Main;

/// <summary>
/// コンパニオンとの通信（docs/domain/companion-pairing.md・frame-stream.md）。
/// HTTP で HTML/JS を配り、WebSocket でペアリングと画面の配信を行い、ペアリング用の QR コードを HUD に出す。
/// ペアリングした接続には夢と依頼の状態を送り、スマホからの改ざん・神託を <see cref="DreamView"/> に渡す（dream-tampering.md）。
/// HUD には、QR コードの IP の切り替えボタン（候補が 2 つ以上のとき）と「全セッションリセット」のボタンも出す。
/// <see cref="WorldCore"/> が作り、<c>_Process</c> から <see cref="Process"/> を呼ぶ。トークンの値はログに出さない。
/// </summary>
public sealed class CompanionHost : IDisposable
{
    // 1 コマ（480×270 の JPEG）が入る大きさ。既定の 64KiB では足りないことがある。
    private const int OutboundBufferBytes = 1024 * 1024;

    // 閉じる合図を送ってから、相手の応答を待つ秒数。過ぎたら相手に知らせずに切る。
    private const double CloseGraceSeconds = 5.0;

    private readonly CompanionSettings _settings;
    private readonly SubViewport _worldViewport;
    private readonly TextureRect _pairingQr;
    private readonly Button _hostAddressButton;
    private readonly Button _resetSessionsButton;
    private readonly PairingRegistry _registry;
    private readonly StreamSessions _sessions;
    private readonly FramePacer _pacer;
    private readonly DreamView _dreams;
    private readonly Dictionary<int, Client> _clients = [];
    private readonly TcpServer _webSocketServer = new();
    private CompanionHttpServer? _httpServer;
    private readonly IReadOnlyList<IPAddress> _hostAddresses = [];
    private readonly bool _isListening;
    private int _hostAddressIndex;
    private int _nextClientId = 1;
    private bool _captureFailureLogged;
    private bool _unpairedLimitLogged;

    // 最後に送った DREAM_STATE。同じ内容は送らない。ペアリングした接続が無ければ null。
    private string? _lastDreamState;

    public CompanionHost(Node owner, CompanionSettings settings, string companionDirectory, DreamView dreams)
    {
        _settings = settings;
        _dreams = dreams;
        _worldViewport = owner.GetNode<SubViewport>("%WorldViewport");
        _pairingQr = owner.GetNode<TextureRect>("%PairingQr");
        _hostAddressButton = owner.GetNode<Button>("%HostAddressButton");
        _resetSessionsButton = owner.GetNode<Button>("%ResetSessionsButton");
        _registry = new PairingRegistry(CreateToken, settings.MaxStreams);
        _sessions = new StreamSessions(settings.MaxStreams);
        _pacer = new FramePacer(settings.FramesPerSecond);
        _pairingQr.Visible = false;
        _hostAddressButton.Visible = false;
        _resetSessionsButton.Visible = false;
        // Dispose で必ず外せるよう、最初につなぐ（使えないあいだはボタンを隠している）
        _hostAddressButton.Pressed += SwitchHostAddress;
        _resetSessionsButton.Pressed += ResetSessions;
        _dreams.Changed += SendDreamState;

        // 通信が使えなくても、PC だけでゲームは続けられるようにする
        if (!Directory.Exists(companionDirectory))
        {
            GD.PushError("event=companion_files_not_found");
            return;
        }

        try
        {
            _httpServer = new CompanionHttpServer(settings.HttpPort, CompanionHttpServer.LoadFiles(companionDirectory));
            _httpServer.Start();
        }
        catch (SocketException ex)
        {
            GD.PushError($"event=http_listen_failed reason={ex.SocketErrorCode}");
            StopHttpServer();
            return;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            GD.PushError($"event=companion_files_load_failed error={ex.GetType().Name}");
            StopHttpServer();
            return;
        }

        var error = _webSocketServer.Listen((ushort)settings.WebSocketPort);
        if (error != Error.Ok)
        {
            GD.PushError($"event=websocket_listen_failed reason={error}");
            StopHttpServer();
            return;
        }
        _isListening = true;
        _resetSessionsButton.Visible = true;

        _hostAddresses = LanAddress.RankForQrCode(LocalAddresses());
        if (_hostAddresses.Count == 0)
        {
            GD.PushWarning("event=lan_address_not_found");
            return;
        }
        if (_hostAddresses.Count > 1)
        {
            _hostAddressButton.Visible = true;
        }
        DrawPairingQr();
        GD.Print("event=companion_host_started");
    }

    public void Process(double delta)
    {
        if (!_isListening)
        {
            return;
        }

        AcceptConnections();
        foreach (var client in _clients.Values.ToList())
        {
            PollClient(client, delta);
        }
        StreamFrame(delta);
    }

    public void Dispose()
    {
        _hostAddressButton.Pressed -= SwitchHostAddress;
        _resetSessionsButton.Pressed -= ResetSessions;
        _dreams.Changed -= SendDreamState;
        foreach (var client in _clients.Values)
        {
            client.Peer.Close();
        }
        _webSocketServer.Stop();
        _httpServer?.Dispose();
    }

    private void AcceptConnections()
    {
        while (_webSocketServer.IsConnectionAvailable())
        {
            var tcp = _webSocketServer.TakeConnection();
            if (_clients.Values.Count(client => !client.Connection.IsPaired && !client.IsClosing) >= _settings.MaxUnpairedConnections)
            {
                // 新しい接続ごとに起こせるので、上限を下回るまでは 1 回だけ記録する
                if (!_unpairedLimitLogged)
                {
                    GD.PushWarning("event=connection_rejected reason=unpaired_limit_reached");
                    _unpairedLimitLogged = true;
                }
                tcp.DisconnectFromHost();
                continue;
            }
            _unpairedLimitLogged = false;

            if (!IPAddress.TryParse(tcp.GetConnectedHost(), out var remote) || !LanAddress.IsPrivate(remote))
            {
                GD.PushWarning("event=connection_rejected reason=not_private_address");
                tcp.DisconnectFromHost();
                continue;
            }

            var peer = new WebSocketPeer { OutboundBufferSize = OutboundBufferBytes };
            var error = peer.AcceptStream(tcp);
            if (error != Error.Ok)
            {
                GD.PushWarning($"event=connection_rejected reason=websocket_accept_failed error={error}");
                tcp.DisconnectFromHost();
                continue;
            }

            var client = new Client(_nextClientId++, peer, new CompanionConnection(_registry));
            _clients.Add(client.Id, client);
        }
    }

    private void PollClient(Client client, double delta)
    {
        client.Peer.Poll();
        var state = client.Peer.GetReadyState();
        if (state == WebSocketPeer.State.Closed)
        {
            RemoveClient(client);
            return;
        }

        if (client.IsClosing)
        {
            client.ClosingSeconds += delta;
            if (client.ClosingSeconds >= CloseGraceSeconds)
            {
                // -1: 閉じる手順を待たずに切る（Godot の WebSocketPeer.Close）
                client.Peer.Close(-1);
                RemoveClient(client);
            }
            return;
        }

        if (state == WebSocketPeer.State.Open)
        {
            while (client.Peer.GetAvailablePacketCount() > 0 && !client.IsClosing)
            {
                var packet = client.Peer.GetPacket();
                if (!client.Peer.WasStringPacket())
                {
                    WarnMessageRejected(client, "binary_from_companion");
                    continue;
                }
                HandleText(client, Encoding.UTF8.GetString(packet));
            }
        }

        // 接続の確立中も含めて数える
        if (!client.Connection.IsPaired && !client.IsClosing)
        {
            client.WaitedSeconds += delta;
            if (client.WaitedSeconds >= _settings.PairingTimeoutSeconds)
            {
                GD.PushWarning($"event=pairing_timeout clientId={client.Id}");
                Close(client);
            }
        }
    }

    private void HandleText(Client client, string text)
    {
        if (!CompanionMessageParser.TryParse(text, out var message, out var reason) || message is null)
        {
            WarnMessageRejected(client, reason);
            if (!client.Connection.IsPaired)
            {
                Reject(client, HostMessageWriter.PairingFailed);
            }
            return;
        }

        switch (client.Connection.Handle(message, out var sessionToken))
        {
            case PairingOutcome.Paired:
                client.Peer.SendText(HostMessageWriter.Paired(sessionToken));
                GD.Print($"event=companion_paired clientId={client.Id}");
                // ほかのペアリングした接続には、変わるたびに送っているので、同じ内容を持っている
                _lastDreamState = _dreams.StateMessage();
                SendText(client, _lastDreamState);
                if (message.Type == CompanionMessageType.Pair)
                {
                    DrawPairingQr();
                }
                CloseOtherSessions(client);
                break;
            case PairingOutcome.Rejected:
                GD.PushWarning($"event=pairing_failed clientId={client.Id}");
                Reject(client, HostMessageWriter.PairingFailed);
                break;
            case PairingOutcome.Ignored:
                WarnMessageRejected(client, "already_paired");
                break;
            case PairingOutcome.Accepted when message.Type is CompanionMessageType.Tamper:
                RejectDreamOperation(client, _dreams.TryTamper(message.Index, message.SymbolId));
                break;
            case PairingOutcome.Accepted when message.Type is CompanionMessageType.Pronounce:
                RejectDreamOperation(client, _dreams.TryPronounce() ? null : "cannot_pronounce");
                break;
            case PairingOutcome.Accepted:
                HandleStreamMessage(client, message.Type);
                break;
        }
    }

    // PC かほかのスマホが先に操作したなど、今はできない操作は捨て、その接続に今の状態を送り直す
    private void RejectDreamOperation(Client client, string? reason)
    {
        if (reason is null)
        {
            return;
        }
        WarnMessageRejected(client, reason);
        SendText(client, _dreams.StateMessage());
    }

    /// <summary>夢と依頼の状態が前に送ったものと変わっていたら、ペアリングしたすべての接続に送る。</summary>
    private void SendDreamState()
    {
        var paired = _clients.Values.Where(client => client.Connection.IsPaired && !client.IsClosing).ToList();
        if (paired.Count == 0)
        {
            _lastDreamState = null;
            return;
        }

        var message = _dreams.StateMessage();
        if (message == _lastDreamState)
        {
            return;
        }
        _lastDreamState = message;
        foreach (var client in paired)
        {
            SendText(client, message);
        }
    }

    // 送れなかった接続は壊れているので、閉じる
    private void SendText(Client client, string text)
    {
        var error = client.Peer.SendText(text);
        if (error != Error.Ok)
        {
            GD.PushWarning($"event=message_send_failed clientId={client.Id} reason={error}");
            Close(client);
        }
    }

    private void HandleStreamMessage(Client client, CompanionMessageType type)
    {
        if (type == CompanionMessageType.StartStream)
        {
            switch (_sessions.Start(client.Id))
            {
                case StreamStartResult.Started:
                    GD.Print($"event=stream_started clientId={client.Id}");
                    break;
                case StreamStartResult.LimitReached:
                    GD.PushWarning($"event=stream_rejected clientId={client.Id} reason=max_connections_reached");
                    Reject(client, HostMessageWriter.MaxConnectionsReached);
                    break;
            }
        }
        else if (type == CompanionMessageType.StopStream && _sessions.Stop(client.Id))
        {
            GD.Print($"event=stream_stopped clientId={client.Id}");
        }
    }

    private void StreamFrame(double delta)
    {
        if (_sessions.StreamingClients.Count == 0)
        {
            _pacer.Reset();
            return;
        }
        if (!_pacer.Advance(delta))
        {
            return;
        }

        var jpeg = _worldViewport.GetTexture()?.GetImage()?.SaveJpgToBuffer(_settings.JpegQualityPercent / 100f);
        if (jpeg is null || jpeg.Length == 0)
        {
            if (!_captureFailureLogged)
            {
                GD.PushWarning("event=frame_capture_failed");
                _captureFailureLogged = true;
            }
            return;
        }
        _captureFailureLogged = false;

        var frame = FrameHeader.CreateFrame(jpeg);
        foreach (var clientId in _sessions.StreamingClients.ToList())
        {
            var peer = _clients[clientId].Peer;
            // 前のコマを送り切っていない遅い接続には送らない
            if (peer.GetCurrentOutboundBufferedAmount() > 0)
            {
                continue;
            }
            var error = peer.Send(frame);
            if (error != Error.Ok)
            {
                _sessions.Stop(clientId);
                GD.PushWarning($"event=frame_send_failed clientId={clientId} reason={error}");
            }
        }
    }

    /// <summary><c>ERROR</c> を送ってから閉じる。</summary>
    private void Reject(Client client, string reason)
    {
        client.Peer.SendText(HostMessageWriter.Error(reason));
        Close(client);
    }

    private void Close(Client client)
    {
        StopStream(client);
        client.Peer.Close();
        client.IsClosing = true;
    }

    /// <summary>
    /// ペアリングした後に、同じ <c>sessionToken</c> の古い接続（後勝ち）と、
    /// 上限を超えて無効になった <c>sessionToken</c> の接続を切る。
    /// </summary>
    private void CloseOtherSessions(Client paired)
    {
        foreach (var other in _clients.Values)
        {
            if (other == paired || !other.Connection.IsPaired || other.IsClosing)
            {
                continue;
            }

            if (other.Connection.SessionToken == paired.Connection.SessionToken)
            {
                GD.Print($"event=session_replaced clientId={other.Id}");
                Reject(other, HostMessageWriter.SessionReplaced);
            }
            else if (!_registry.TryResume(other.Connection.SessionToken))
            {
                // 何も送らずに切る。コンパニオンは RESUME し、PAIRING_FAILED で QR コードの読み直しを案内する
                GD.Print($"event=session_revoked clientId={other.Id}");
                Close(other);
            }
        }
    }

    /// <summary>「全セッションリセット」: すべての <c>sessionToken</c> を無効にし、ペアリング済みの接続を切る。</summary>
    private void ResetSessions()
    {
        _registry.RevokeAll();
        foreach (var client in _clients.Values)
        {
            if (client.Connection.IsPaired && !client.IsClosing)
            {
                Close(client);
            }
        }
        GD.Print("event=sessions_reset");
    }

    private void SwitchHostAddress()
    {
        _hostAddressIndex = (_hostAddressIndex + 1) % _hostAddresses.Count;
        DrawPairingQr();
    }

    // 接続を保ったまま何度でも送れるため、同じ接続・同じ理由では 1 回だけ記録する（project.md 7章）
    private static void WarnMessageRejected(Client client, string reason)
    {
        if (client.RejectedReasons.Add(reason))
        {
            GD.PushWarning($"event=message_rejected clientId={client.Id} reason={reason}");
        }
    }

    // 片方のポートだけでは使えないので、HTTP も止める
    private void StopHttpServer()
    {
        _httpServer?.Dispose();
        _httpServer = null;
    }

    private void RemoveClient(Client client)
    {
        StopStream(client);
        _clients.Remove(client.Id);
    }

    private void StopStream(Client client)
    {
        if (_sessions.Stop(client.Id))
        {
            GD.Print($"event=stream_stopped clientId={client.Id}");
        }
    }

    private void DrawPairingQr()
    {
        if (_hostAddresses.Count == 0)
        {
            return;
        }

        var hostAddress = _hostAddresses[_hostAddressIndex];
        _hostAddressButton.Text = hostAddress.ToString();
        var url = $"http://{hostAddress}:{_settings.HttpPort}/?token={_registry.CurrentToken}";
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(url, QRCodeGenerator.ECCLevel.M);
        var png = new PngByteQRCode(data).GetGraphic(1);

        var image = new Image();
        var error = image.LoadPngFromBuffer(png);
        if (error != Error.Ok)
        {
            GD.PushError($"event=pairing_qr_failed reason={error}");
            _pairingQr.Visible = false;
            return;
        }
        _pairingQr.Texture = ImageTexture.CreateFromImage(image);
        _pairingQr.Visible = true;
    }

    // 暗号用の乱数で作る、URL にそのまま入れられるトークン
    private static string CreateToken() => Base64Url.EncodeToString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));

    // PC のアドレスと、そのネットワークにデフォルトゲートウェイがあるか（仮想アダプターには無いことが多い）
    private static IEnumerable<(IPAddress Address, bool HasGateway)> LocalAddresses() =>
        NetworkInterface.GetAllNetworkInterfaces()
            .Where(network => network.OperationalStatus == OperationalStatus.Up
                && network.NetworkInterfaceType is not (NetworkInterfaceType.Loopback or NetworkInterfaceType.Tunnel))
            .SelectMany(network =>
            {
                var properties = network.GetIPProperties();
                var hasGateway = properties.GatewayAddresses.Any(gateway =>
                    gateway.Address.AddressFamily == AddressFamily.InterNetwork && !gateway.Address.Equals(IPAddress.Any));
                return properties.UnicastAddresses.Select(unicast => (unicast.Address, hasGateway));
            });

    private sealed record Client(int Id, WebSocketPeer Peer, CompanionConnection Connection)
    {
        public double WaitedSeconds { get; set; }

        public bool IsClosing { get; set; }

        public double ClosingSeconds { get; set; }

        /// <summary>記録済みの <c>message_rejected</c> の理由。</summary>
        public HashSet<string> RejectedReasons { get; } = [];
    }
}
