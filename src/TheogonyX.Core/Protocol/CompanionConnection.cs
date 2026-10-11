namespace TheogonyX.Core.Protocol;

/// <summary>
/// 接続ごとのペアリングの状態（docs/domain/companion-pairing.md）。接続したときは未ペアリング。
/// ペアリングを待つ時間が過ぎたら、ホストは未ペアリングの接続を閉じる。
/// </summary>
public sealed class CompanionConnection
{
    private enum Trigger
    {
        Pair,
        Resume,
        Other,
    }

    // 仕様書の状態遷移表と1対1。キーは（ペアリング済みか, きっかけ）。
    // Paired は「トークンが合えばペアリング済み、合わなければ Rejected」。表の「不可」は Ignored。
    private static readonly Dictionary<(bool IsPaired, Trigger), PairingOutcome> Transitions = new()
    {
        [(false, Trigger.Pair)] = PairingOutcome.Paired,
        [(false, Trigger.Resume)] = PairingOutcome.Paired,
        [(false, Trigger.Other)] = PairingOutcome.Rejected,
        [(true, Trigger.Pair)] = PairingOutcome.Ignored,
        [(true, Trigger.Resume)] = PairingOutcome.Ignored,
        [(true, Trigger.Other)] = PairingOutcome.Accepted,
    };

    private readonly PairingRegistry _registry;

    public CompanionConnection(PairingRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(registry);
        _registry = registry;
    }

    public bool IsPaired { get; private set; }

    /// <param name="sessionToken"><see cref="PairingOutcome.Paired"/> のとき、<c>PAIRED</c> で返す値。ほかは空文字。</param>
    public PairingOutcome Handle(CompanionMessage message, out string sessionToken)
    {
        ArgumentNullException.ThrowIfNull(message);
        var trigger = message.Type switch
        {
            CompanionMessageType.Pair => Trigger.Pair,
            CompanionMessageType.Resume => Trigger.Resume,
            _ => Trigger.Other,
        };

        sessionToken = string.Empty;
        var outcome = Transitions[(IsPaired, trigger)];
        if (outcome != PairingOutcome.Paired)
        {
            return outcome;
        }

        var matched = trigger == Trigger.Pair
            ? _registry.TryPair(message.Token, out sessionToken)
            : _registry.TryResume(message.Token);
        if (!matched)
        {
            sessionToken = string.Empty;
            return PairingOutcome.Rejected;
        }

        if (trigger == Trigger.Resume)
        {
            sessionToken = message.Token;
        }

        IsPaired = true;
        return PairingOutcome.Paired;
    }
}
