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
    private readonly PairingRegistry _registry = new(CreateToken);
    private readonly StreamSessions _sessions;
    private readonly FramePacer _pacer;
    private readonly Dictionary<int, Client> _clients = [];
    private readonly TcpServer _webSocketServer = new();
    private CompanionHttpServer? _httpServer;
    private readonly IPAddress? _hostAddress;
    private readonly bool _isListening;
    private int _nextClientId = 1;
    private bool _captureFailureLogged;

    public CompanionHost(Node owner, CompanionSettings settings, string companionDirectory)
    {
        _settings = settings;
        _worldViewport = owner.GetNode<SubViewport>("%WorldViewport");
        _pairingQr = owner.GetNode<TextureRect>("%PairingQr");
        _sessions = new StreamSessions(settings.MaxStreams);
        _pacer = new FramePacer(settings.FramesPerSecond);
        _pairingQr.Visible = false;

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

        _hostAddress = LanAddress.ChooseForQrCode(LocalAddresses());
        if (_hostAddress is null)
        {
            GD.PushWarning("event=lan_address_not_found");
            return;
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
                if (message.Type == CompanionMessageType.Pair)
                {
                    DrawPairingQr();
                }
                break;
            case PairingOutcome.Rejected:
                GD.PushWarning($"event=pairing_failed clientId={client.Id}");
                Reject(client, HostMessageWriter.PairingFailed);
                break;
            case PairingOutcome.Ignored:
                WarnMessageRejected(client, "already_paired");
                break;
            case PairingOutcome.Accepted:
                HandleStreamMessage(client, message.Type);
                break;
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
        if (_hostAddress is null)
        {
            return;
        }

        var url = $"http://{_hostAddress}:{_settings.HttpPort}/?token={_registry.CurrentToken}";
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

    private static IEnumerable<IPAddress> LocalAddresses() =>
        NetworkInterface.GetAllNetworkInterfaces()
            .Where(network => network.OperationalStatus == OperationalStatus.Up)
            .SelectMany(network => network.GetIPProperties().UnicastAddresses)
            .Select(unicast => unicast.Address);

    private sealed record Client(int Id, WebSocketPeer Peer, CompanionConnection Connection)
    {
        public double WaitedSeconds { get; set; }

        public bool IsClosing { get; set; }

        public double ClosingSeconds { get; set; }

        /// <summary>記録済みの <c>message_rejected</c> の理由。</summary>
        public HashSet<string> RejectedReasons { get; } = [];
    }
}
