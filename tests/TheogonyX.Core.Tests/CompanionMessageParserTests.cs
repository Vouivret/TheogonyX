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
    [Trait("TS", "TS-oracle-pair-010")]
    public void TryParse_SPEAK_神の言葉が取り出せる()
    {
        var ok = CompanionMessageParser.TryParse("""{ "type": "SPEAK", "version": 1, "text": "森を守れ" }""", out var message, out _);

        Assert.True(ok);
        Assert.Equal(new CompanionMessage(CompanionMessageType.Speak, Text: "森を守れ"), message);
    }

    [Theory]
    [Trait("TS", "TS-oracle-pair-010")]
    [InlineData("""{ "type": "SPEAK", "version": 1 }""")]
    [InlineData("""{ "type": "SPEAK", "version": 1, "text": 3 }""")]
    [InlineData("""{ "type": "SPEAK", "version": 1, "text": null }""")]
    [InlineData("""{ "type": "SPEAK", "version": 1, "text": "   " }""")]
    [InlineData("""{ "type": "SPEAK", "version": 1, "text": "森を\n守れ" }""")]
    public void TryParse_SPEAKの文がおかしい_失敗する(string text)
    {
        var ok = CompanionMessageParser.TryParse(text, out var message, out var reason);

        Assert.False(ok);
        Assert.Null(message);
        Assert.Equal("invalid_speak", reason);
    }

    [Fact]
    [Trait("TS", "TS-oracle-pair-010")]
    public void TryParse_SPEAKの文が上限を超える_失敗する()
    {
        var atLimit = new string('あ', OracleListening.MaxGodTextLength);
        var okAtLimit = CompanionMessageParser.TryParse($$"""{ "type": "SPEAK", "version": 1, "text": "{{atLimit}}" }""", out _, out _);
        var okOverLimit = CompanionMessageParser.TryParse($$"""{ "type": "SPEAK", "version": 1, "text": "{{atLimit}}あ" }""", out _, out var reason);

        Assert.True(okAtLimit);
        Assert.False(okOverLimit);
        Assert.Equal("invalid_speak", reason);
    }

    [Fact]
    [Trait("TS", "TS-oracle-pair-010")]
    public void TryParse_PRONOUNCE_名詞と動詞と行き先が取り出せる()
    {
        var ok = CompanionMessageParser.TryParse(
            """{ "type": "PRONOUNCE", "version": 3, "noun": "NOUN_FOREST", "verb": null, "destination": "EAST" }""", out var message, out _);

        Assert.True(ok);
        Assert.Equal(
            new CompanionMessage(CompanionMessageType.Pronounce, NounId: "NOUN_FOREST", VerbId: null, Destination: HeardDestination.East), message);
    }

    [Theory]
    [Trait("TS", "TS-oracle-pair-010")]
    [InlineData("""{ "type": "PRONOUNCE", "version": 3 }""")]
    [InlineData("""{ "type": "PRONOUNCE", "version": 3, "noun": "NOUN_FOREST", "verb": null }""")]
    [InlineData("""{ "type": "PRONOUNCE", "version": 3, "noun": null, "verb": null, "destination": "NONE" }""")]
    [InlineData("""{ "type": "PRONOUNCE", "version": 3, "noun": 1, "verb": null, "destination": "NONE" }""")]
    [InlineData("""{ "type": "PRONOUNCE", "version": 3, "noun": "", "verb": null, "destination": "EAST" }""")]
    [InlineData("""{ "type": "PRONOUNCE", "version": 3, "noun": "N\nX", "verb": null, "destination": "EAST" }""")]
    [InlineData("""{ "type": "PRONOUNCE", "version": 3, "noun": "NOUN_FOREST", "verb": null, "destination": "NORTH" }""")]
    [InlineData("""{ "type": "PRONOUNCE", "version": 3, "noun": "NOUN_FOREST", "verb": null, "destination": 1 }""")]
    public void TryParse_PRONOUNCEの中身がおかしい_失敗する(string text)
    {
        var ok = CompanionMessageParser.TryParse(text, out var message, out var reason);

        Assert.False(ok);
        Assert.Null(message);
        Assert.Equal("invalid_pronounce", reason);
    }

    [Theory]
    [Trait("TS", "TS-oracle-pair-010")]
    [InlineData("""{ "type": "PRONOUNCE", "version": 1 }""")]
    [InlineData("""{ "type": "PRONOUNCE", "version": 2, "first": 3, "second": 0 }""")]
    [InlineData("""{ "type": "TAMPER", "version": 1, "index": 1, "symbolId": "SYM_BLOOD" }""")]
    public void TryParse_廃止した形のメッセージ_知らない種別と版として失敗する(string text)
    {
        var ok = CompanionMessageParser.TryParse(text, out var message, out var reason);

        Assert.False(ok);
        Assert.Null(message);
        Assert.Equal("unknown_type_or_version", reason);
    }
}
