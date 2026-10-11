using System.Text;
using TheogonyX.Core.Protocol;

namespace TheogonyX.Core.Tests;

public class CompanionHttpTests
{
    [Theory]
    [Trait("TS", "TS-companion-pairing-011")]
    [InlineData("GET /?token=q9V3oneTime HTTP/1.1\r\nHost: 192.168.1.20:8080", "/index.html")]
    [InlineData("GET /js/app.js HTTP/1.1\r\nHost: 192.168.1.20:8080", "/js/app.js")]
    [InlineData("GET /../game/data/companion.json HTTP/1.1", "/../game/data/companion.json")]
    public void TryParseRequest_GETの要求_クエリを除いたパスになる(string head, string expectedPath)
    {
        var ok = CompanionHttp.TryParseRequest(head, out var path, out var status);

        Assert.True(ok);
        Assert.Equal(expectedPath, path);
        Assert.Equal(200, status);
    }

    [Theory]
    [Trait("TS", "TS-companion-pairing-011")]
    [InlineData("POST / HTTP/1.1", 405)]
    [InlineData("GET /", 400)]
    [InlineData("GET index.html HTTP/1.1", 400)]
    [InlineData("GET / SSH-2.0", 400)]
    [InlineData("", 400)]
    public void TryParseRequest_GETでないか形が違う_状態コードが分かる(string head, int expectedStatus)
    {
        var ok = CompanionHttp.TryParseRequest(head, out _, out var status);

        Assert.False(ok);
        Assert.Equal(expectedStatus, status);
    }

    [Theory]
    [Trait("TS", "TS-companion-pairing-011")]
    [InlineData("index.html", "text/html; charset=utf-8")]
    [InlineData("app.js", "text/javascript; charset=utf-8")]
    [InlineData("ja.json", "application/json; charset=utf-8")]
    [InlineData("secret.txt", null)]
    [InlineData("README", null)]
    public void ContentTypeFor_拡張子_配るものだけ型が決まる(string fileName, string? expected)
    {
        Assert.Equal(expected, CompanionHttp.ContentTypeFor(fileName));
    }

    [Fact]
    [Trait("TS", "TS-companion-pairing-011")]
    public void ResponseHead_200_トークンを外に出さないヘッダーが付く()
    {
        var head = Encoding.ASCII.GetString(CompanionHttp.ResponseHead(200, "text/html; charset=utf-8", 42));

        Assert.StartsWith("HTTP/1.1 200 OK\r\n", head, StringComparison.Ordinal);
        Assert.Contains("Content-Length: 42\r\n", head, StringComparison.Ordinal);
        Assert.Contains("Referrer-Policy: no-referrer\r\n", head, StringComparison.Ordinal);
        Assert.Contains("Cache-Control: no-store\r\n", head, StringComparison.Ordinal);
        Assert.EndsWith("\r\n\r\n", head, StringComparison.Ordinal);
    }
}
