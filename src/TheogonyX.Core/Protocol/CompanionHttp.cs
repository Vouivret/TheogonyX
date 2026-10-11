using System.Text;

namespace TheogonyX.Core.Protocol;

/// <summary>
/// コンパニオンの静的ファイルを配る HTTP の、要求の読み取りと応答の頭の組み立て（docs/domain/companion-pairing.md）。
/// 受け取ったパスはファイルパスに使わず、ホストが起動時に作った一覧の鍵として引くだけにする。
/// </summary>
public static class CompanionHttp
{
    /// <summary>要求の頭（要求行とヘッダー）として受け付ける最大のバイト数。</summary>
    public const int MaxRequestHeadBytes = 8192;

    private static readonly Dictionary<int, string> ReasonPhrases = new()
    {
        [200] = "OK",
        [400] = "Bad Request",
        [404] = "Not Found",
        [405] = "Method Not Allowed",
    };

    // 配るファイルの拡張子。ここに無いものは配らない。
    private static readonly Dictionary<string, string> ContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        [".html"] = "text/html; charset=utf-8",
        [".js"] = "text/javascript; charset=utf-8",
        [".css"] = "text/css; charset=utf-8",
        [".json"] = "application/json; charset=utf-8",
    };

    /// <summary>
    /// 要求の頭から、クエリを除いたパスを取り出す。<c>/</c> は <c>/index.html</c> にする。
    /// </summary>
    /// <param name="status">失敗したときの応答の状態コード（400 か 405）。成功したときは 200。</param>
    public static bool TryParseRequest(string requestHead, out string path, out int status)
    {
        ArgumentNullException.ThrowIfNull(requestHead);
        path = string.Empty;

        var lineEnd = requestHead.IndexOf("\r\n", StringComparison.Ordinal);
        var parts = (lineEnd < 0 ? requestHead : requestHead[..lineEnd]).Split(' ');
        if (parts.Length != 3 || !parts[2].StartsWith("HTTP/1.", StringComparison.Ordinal) || !parts[1].StartsWith('/'))
        {
            status = 400;
            return false;
        }

        if (parts[0] != "GET")
        {
            status = 405;
            return false;
        }

        var target = parts[1];
        var query = target.IndexOf('?');
        path = query < 0 ? target : target[..query];
        if (path == "/")
        {
            path = "/index.html";
        }

        status = 200;
        return true;
    }

    /// <summary>ファイル名の拡張子から Content-Type を返す。配らない拡張子なら null。</summary>
    public static string? ContentTypeFor(string fileName)
    {
        ArgumentNullException.ThrowIfNull(fileName);
        return ContentTypes.GetValueOrDefault(Path.GetExtension(fileName));
    }

    /// <summary>応答の頭（状態行とヘッダー）を返す。本文は呼び出し側が続けて書く。</summary>
    public static byte[] ResponseHead(int status, string contentType, int contentLength)
    {
        ArgumentException.ThrowIfNullOrEmpty(contentType);
        ArgumentOutOfRangeException.ThrowIfNegative(contentLength);
        if (!ReasonPhrases.TryGetValue(status, out var reason))
        {
            throw new ArgumentOutOfRangeException(nameof(status), status, "対応していない状態コード");
        }

        // Referrer-Policy: URL のトークンを外に出さない。Cache-Control: トークン付きの URL を残さない。
        var head = $"HTTP/1.1 {status} {reason}\r\n"
            + $"Content-Type: {contentType}\r\n"
            + $"Content-Length: {contentLength}\r\n"
            + "Cache-Control: no-store\r\n"
            + "Referrer-Policy: no-referrer\r\n"
            + "X-Content-Type-Options: nosniff\r\n"
            + "Connection: close\r\n"
            + "\r\n";
        return Encoding.ASCII.GetBytes(head);
    }
}
