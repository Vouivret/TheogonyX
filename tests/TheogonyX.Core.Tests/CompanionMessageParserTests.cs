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
        var ok = CompanionMessageParser.TryParse(text, out var type, out var reason);

        Assert.True(ok);
        Assert.Equal(expected, type);
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
        var ok = CompanionMessageParser.TryParse(text, out _, out var reason);

        Assert.False(ok);
        Assert.Equal(expectedReason, reason);
    }
}
