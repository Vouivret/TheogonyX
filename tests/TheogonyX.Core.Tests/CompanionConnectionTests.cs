using TheogonyX.Core.Protocol;

namespace TheogonyX.Core.Tests;

public class CompanionConnectionTests
{
    private static readonly CompanionMessage StartStream = new(CompanionMessageType.StartStream);

    // テストでは暗号用の乱数の代わりに、呼ぶたびに番号が増えるトークンを作る
    private static PairingRegistry CreateRegistry()
    {
        var count = 0;
        return new PairingRegistry(() => $"token-{++count}", maxSessions: 2);
    }

    private static CompanionMessage Pair(string token) => new(CompanionMessageType.Pair, token);

    private static CompanionMessage Resume(string sessionToken) => new(CompanionMessageType.Resume, sessionToken);

    [Fact]
    [Trait("TS", "TS-companion-pairing-001")]
    public void Handle_合うトークンのPAIR_ペアリング済みになりトークンが替わる()
    {
        var registry = CreateRegistry();
        var connection = new CompanionConnection(registry);

        var outcome = connection.Handle(Pair("token-1"), out var sessionToken);

        Assert.Equal(PairingOutcome.Paired, outcome);
        Assert.True(connection.IsPaired);
        Assert.Equal("token-2", sessionToken);
        Assert.Equal("token-2", connection.SessionToken);
        Assert.Equal("token-3", registry.CurrentToken);
    }

    [Fact]
    [Trait("TS", "TS-companion-pairing-002")]
    public void Handle_使用済みのトークンのPAIR_拒否される()
    {
        var registry = CreateRegistry();
        new CompanionConnection(registry).Handle(Pair("token-1"), out _);
        var other = new CompanionConnection(registry);

        var outcome = other.Handle(Pair("token-1"), out var sessionToken);

        Assert.Equal(PairingOutcome.Rejected, outcome);
        Assert.False(other.IsPaired);
        Assert.Equal(string.Empty, sessionToken);
    }

    [Fact]
    [Trait("TS", "TS-companion-pairing-003")]
    public void Handle_発行したsessionTokenのRESUME_ペアリング済みに戻り同じ値を返す()
    {
        var registry = CreateRegistry();
        new CompanionConnection(registry).Handle(Pair("token-1"), out var issued);
        var reconnected = new CompanionConnection(registry);

        var outcome = reconnected.Handle(Resume(issued), out var sessionToken);

        Assert.Equal(PairingOutcome.Paired, outcome);
        Assert.True(reconnected.IsPaired);
        Assert.Equal("token-2", sessionToken);
    }

    [Fact]
    [Trait("TS", "TS-companion-pairing-004")]
    public void Handle_知らないsessionTokenのRESUME_拒否される()
    {
        var registry = CreateRegistry();
        var connection = new CompanionConnection(registry);

        var outcome = connection.Handle(Resume("token-9"), out var sessionToken);

        Assert.Equal(PairingOutcome.Rejected, outcome);
        Assert.False(connection.IsPaired);
        Assert.Equal(string.Empty, sessionToken);
    }

    [Fact]
    [Trait("TS", "TS-companion-pairing-005")]
    public void Handle_未ペアリングでほかのメッセージ_拒否される()
    {
        var connection = new CompanionConnection(CreateRegistry());

        var outcome = connection.Handle(StartStream, out _);

        Assert.Equal(PairingOutcome.Rejected, outcome);
        Assert.False(connection.IsPaired);
    }

    [Fact]
    [Trait("TS", "TS-companion-pairing-006")]
    public void Handle_ペアリング済みでPAIRとRESUME_捨ててトークンを照合しない()
    {
        var registry = CreateRegistry();
        var connection = new CompanionConnection(registry);
        connection.Handle(Pair("token-1"), out _);

        var onPair = connection.Handle(Pair("token-3"), out _);
        var onResume = connection.Handle(Resume("token-2"), out _);
        var onStartStream = connection.Handle(StartStream, out _);

        Assert.Equal(PairingOutcome.Ignored, onPair);
        Assert.Equal(PairingOutcome.Ignored, onResume);
        Assert.Equal(PairingOutcome.Accepted, onStartStream);
        Assert.Equal("token-3", registry.CurrentToken);
    }
}
