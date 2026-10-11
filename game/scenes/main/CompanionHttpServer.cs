using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Godot;
using TheogonyX.Core.Protocol;

namespace TheogonyX.Scenes.Main;

/// <summary>
/// コンパニオンの HTML/JS を配る、GET だけの小さな HTTP サーバー（docs/domain/companion-pairing.md）。
/// <c>HttpListener</c> は LAN で待ち受けるのに管理者権限が要るため、<c>TcpListener</c> で応答する。
/// 要求のパスはファイルパスに使わず、起動時に作ったファイルの一覧から引く。URL（トークンを含む）はログに出さない。
/// </summary>
public sealed class CompanionHttpServer : IDisposable
{
    private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(5);
    // エラーの応答の本文。状態コードだけで区別する。
    private static readonly StaticFile Empty = new([], "text/plain; charset=utf-8");

    private readonly TcpListener _listener;
    private readonly IReadOnlyDictionary<string, StaticFile> _files;
    private readonly CancellationTokenSource _stopping = new();

    /// <param name="files">鍵は <c>/index.html</c> の形のパス。</param>
    public CompanionHttpServer(int port, IReadOnlyDictionary<string, StaticFile> files)
    {
        _listener = TcpListener.Create(port);
        _files = files;
    }

    /// <summary>待ち受けを始める。ポートが使われていれば <see cref="SocketException"/> を投げる。</summary>
    public void Start()
    {
        _listener.Start();
        _ = AcceptLoopAsync();
    }

    public void Dispose()
    {
        _stopping.Cancel();
        _listener.Stop();
    }

    /// <summary>ディレクトリの中の、配る拡張子のファイルを読み、パスを鍵にした一覧を返す。</summary>
    public static Dictionary<string, StaticFile> LoadFiles(string directory)
    {
        var files = new Dictionary<string, StaticFile>(StringComparer.Ordinal);
        foreach (var file in Directory.EnumerateFiles(directory, "*", SearchOption.AllDirectories))
        {
            if (CompanionHttp.ContentTypeFor(file) is not { } contentType)
            {
                continue;
            }
            var key = "/" + Path.GetRelativePath(directory, file).Replace('\\', '/');
            files[key] = new StaticFile(File.ReadAllBytes(file), contentType);
        }
        return files;
    }

    private async Task AcceptLoopAsync()
    {
        while (true)
        {
            TcpClient client;
            try
            {
                client = await _listener.AcceptTcpClientAsync(_stopping.Token);
            }
            catch (Exception ex) when (ex is OperationCanceledException or ObjectDisposedException)
            {
                return;
            }
            catch (SocketException ex)
            {
                GD.PushError($"event=http_accept_failed reason={ex.SocketErrorCode}");
                return;
            }
            _ = HandleAsync(client);
        }
    }

    private async Task HandleAsync(TcpClient client)
    {
        using (client)
        {
            try
            {
                if (client.Client.RemoteEndPoint is not IPEndPoint remote || !LanAddress.IsPrivate(remote.Address))
                {
                    GD.PushWarning("event=connection_rejected reason=not_private_address");
                    return;
                }

                using var timeout = CancellationTokenSource.CreateLinkedTokenSource(_stopping.Token);
                timeout.CancelAfter(RequestTimeout);
                var stream = client.GetStream();
                var head = await ReadRequestHeadAsync(stream, timeout.Token);

                var status = 400;
                var path = string.Empty;
                if (head is not null)
                {
                    CompanionHttp.TryParseRequest(head, out path, out status);
                }
                var file = status == 200 ? _files.GetValueOrDefault(path) : Empty;
                if (file is null)
                {
                    (status, file) = (404, Empty);
                }

                await stream.WriteAsync(CompanionHttp.ResponseHead(status, file.ContentType, file.Body.Length), timeout.Token);
                await stream.WriteAsync(file.Body, timeout.Token);
            }
            catch (OperationCanceledException)
            {
                GD.PushWarning("event=http_request_failed reason=timeout");
            }
            catch (IOException ex)
            {
                GD.PushWarning($"event=http_request_failed reason=io_error error={ex.GetType().Name}");
            }
            catch (Exception ex)
            {
                // 投げっぱなしのタスクなので、ここで記録しないと例外が消える
                GD.PushError($"event=http_request_failed reason=unexpected error={ex.GetType().Name}");
            }
        }
    }

    /// <summary>空行（<c>\r\n\r\n</c>）までを読む。上限を超えても空行が無ければ null。</summary>
    private static async Task<string?> ReadRequestHeadAsync(NetworkStream stream, CancellationToken cancellationToken)
    {
        var buffer = new byte[CompanionHttp.MaxRequestHeadBytes];
        var length = 0;
        while (length < buffer.Length)
        {
            var read = await stream.ReadAsync(buffer.AsMemory(length), cancellationToken);
            if (read == 0)
            {
                return null;
            }
            length += read;

            var text = Encoding.Latin1.GetString(buffer, 0, length);
            var end = text.IndexOf("\r\n\r\n", StringComparison.Ordinal);
            if (end >= 0)
            {
                return text[..end];
            }
        }
        return null;
    }
}
