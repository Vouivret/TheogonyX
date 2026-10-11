using TheogonyX.Core.Protocol;

namespace TheogonyX.Core.Tests;

public class PairingRegistryTests
{
    // テストでは暗号用の乱数の代わりに、呼ぶたびに番号が増えるトークンを作る
    private static PairingRegistry CreateRegistry()
    {
        var count = 0;
        return new PairingRegistry(() => $"token-{++count}", maxSessions: 2);
    }

    private static string Pair(PairingRegistry registry)
    {
        registry.TryPair(registry.CurrentToken, out var sessionToken);
        return sessionToken;
    }

    [Fact]
    [Trait("TS", "TS-companion-pairing-013")]
    public void TryPair_sessionTokenが上限の2個ある_一番古いものが無効になる()
    {
        var registry = CreateRegistry();
        var first = Pair(registry);
        var second = Pair(registry);

        var third = Pair(registry);

        Assert.False(registry.TryResume(first));
        Assert.True(registry.TryResume(second));
        Assert.True(registry.TryResume(third));
    }

    [Fact]
    [Trait("TS", "TS-companion-pairing-014")]
    public void RevokeAll_発行済みのsessionToken_すべて無効になる()
    {
        var registry = CreateRegistry();
        var first = Pair(registry);
        var second = Pair(registry);

        registry.RevokeAll();

        Assert.False(registry.TryResume(first));
        Assert.False(registry.TryResume(second));
    }
}
