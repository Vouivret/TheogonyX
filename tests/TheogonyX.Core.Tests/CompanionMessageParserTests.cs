using TheogonyX.Core.Protocol;

namespace TheogonyX.Core.Tests;

public class CompanionMessageParserTests
{
    [Theory]
    [Trait("TS", "TS-frame-stream-009")]
    [InlineData("""{ "type": "START_STREAM", "version": 1 }""", CompanionMessageType.StartStream)]
    [InlineData("""{ "type": "STOP_STREAM", "version": 1 }""", CompanionMessageType.StopStream)]
    public void TryParse_表にあるメッセージ_種類が分かる(string text, CompanionMessageType expected)
    {
        var ok = CompanionMessageParser.TryParse(text, out var message, out var reason);

        Assert.True(ok);
        Assert.Equal(new CompanionMessage(expected), message);
        Assert.Equal(string.Empty, reason);
    }

    [Theory]
    [Trait("TS", "TS-frame-stream-010")]
    [InlineData("START_STREAM", "invalid_json")]
    [InlineData("""{ "version": 1 }""", "missing_type")]
    [InlineData("""{ "type": 1, "version": 1 }""", "missing_type")]
    [InlineData("""[ "START_STREAM" ]""", "missing_type")]
    [InlineData("""{ "type": "START_STREAM" }""", "missing_version")]
    [InlineData("""{ "type": "START_STREAM", "version": "1" }""", "missing_version")]
    [InlineData("""{ "type": "SWAP_CARD", "version": 1 }""", "unknown_type_or_version")]
    [InlineData("""{ "type": "START_STREAM", "version": 2 }""", "unknown_type_or_version")]
    public void TryParse_読めないか知らないメッセージ_失敗して理由が分かる(string text, string expectedReason)
    {
        var ok = CompanionMessageParser.TryParse(text, out var message, out var reason);

        Assert.False(ok);
        Assert.Null(message);
        Assert.Equal(expectedReason, reason);
    }

    [Fact]
    [Trait("TS", "TS-companion-pairing-010")]
    public void TryParse_PAIRとRESUME_トークンが取り出せる()
    {
        CompanionMessageParser.TryParse("""{ "type": "PAIR", "version": 1, "token": "q9V3oneTime" }""", out var pair, out _);
        CompanionMessageParser.TryParse("""{ "type": "RESUME", "version": 1, "sessionToken": "Xk2pSession" }""", out var resume, out _);

        Assert.Equal(new CompanionMessage(CompanionMessageType.Pair, "q9V3oneTime"), pair);
        Assert.Equal(new CompanionMessage(CompanionMessageType.Resume, "Xk2pSession"), resume);
    }

    [Theory]
    [Trait("TS", "TS-companion-pairing-010")]
    [InlineData("""{ "type": "PAIR", "version": 1 }""")]
    [InlineData("""{ "type": "PAIR", "version": 1, "token": "" }""")]
    [InlineData("""{ "type": "PAIR", "version": 1, "token": 123 }""")]
    [InlineData("""{ "type": "RESUME", "version": 1, "token": "Xk2pSession" }""")]
    public void TryParse_トークンが無いか空か文字列でない_失敗する(string text)
    {
        var ok = CompanionMessageParser.TryParse(text, out _, out var reason);

        Assert.False(ok);
        Assert.Equal("invalid_token", reason);
    }

    [Fact]
    [Trait("TS", "TS-companion-pairing-010")]
    public void TryParse_トークンが上限の128文字を超える_失敗する()
    {
        var atLimit = new string('a', 128);
        var overLimit = new string('a', 129);

        var okAtLimit = CompanionMessageParser.TryParse($$"""{ "type": "PAIR", "version": 1, "token": "{{atLimit}}" }""", out _, out _);
        var okOverLimit = CompanionMessageParser.TryParse($$"""{ "type": "PAIR", "version": 1, "token": "{{overLimit}}" }""", out _, out var reason);

        Assert.True(okAtLimit);
        Assert.False(okOverLimit);
        Assert.Equal("invalid_token", reason);
    }

    [Fact]
    [Trait("TS", "TS-dream-tampering-010")]
    public void TryParse_TAMPERとPRONOUNCE_位置と記号が取り出せる()
    {
        CompanionMessageParser.TryParse("""{ "type": "TAMPER", "version": 1, "index": 1, "symbolId": "SYM_BLOOD" }""", out var tamper, out _);
        CompanionMessageParser.TryParse("""{ "type": "PRONOUNCE", "version": 1 }""", out var pronounce, out _);

        Assert.Equal(new CompanionMessage(CompanionMessageType.Tamper, Index: 1, SymbolId: "SYM_BLOOD"), tamper);
        Assert.Equal(new CompanionMessage(CompanionMessageType.Pronounce), pronounce);
    }

    [Theory]
    [Trait("TS", "TS-dream-tampering-010")]
    [InlineData("""{ "type": "TAMPER", "version": 1, "symbolId": "SYM_BLOOD" }""")]
    [InlineData("""{ "type": "TAMPER", "version": 1, "index": -1, "symbolId": "SYM_BLOOD" }""")]
    [InlineData("""{ "type": "TAMPER", "version": 1, "index": 1.5, "symbolId": "SYM_BLOOD" }""")]
    [InlineData("""{ "type": "TAMPER", "version": 1, "index": "1", "symbolId": "SYM_BLOOD" }""")]
    [InlineData("""{ "type": "TAMPER", "version": 1, "index": 1 }""")]
    [InlineData("""{ "type": "TAMPER", "version": 1, "index": 1, "symbolId": "" }""")]
    [InlineData("""{ "type": "TAMPER", "version": 1, "index": 1, "symbolId": 3 }""")]
    [InlineData("""{ "type": "TAMPER", "version": 1, "index": 1, "symbolId": "SYM_AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA" }""")]
    public void TryParse_TAMPERの位置か記号がおかしい_失敗する(string text)
    {
        var ok = CompanionMessageParser.TryParse(text, out var message, out var reason);

        Assert.False(ok);
        Assert.Null(message);
        Assert.Equal("invalid_tamper", reason);
    }
}
