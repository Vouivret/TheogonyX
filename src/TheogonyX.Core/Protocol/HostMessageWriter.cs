using System.Text.Json;

namespace TheogonyX.Core.Protocol;

/// <summary>ホストからコンパニオンへのテキストフレーム（JSON）を組み立てる（docs/protocol/messages.md）。</summary>
public static class HostMessageWriter
{
    /// <summary><c>ERROR</c> の <c>reason</c>: 配信の上限に達している。</summary>
    public const string MaxConnectionsReached = "MAX_CONNECTIONS_REACHED";

    /// <summary><c>ERROR</c> の <c>reason</c>: ペアリングできない。</summary>
    public const string PairingFailed = "PAIRING_FAILED";

    /// <summary><c>ERROR</c> の <c>reason</c>: 同じ <c>sessionToken</c> で新しい接続が来たので、この接続を切る。</summary>
    public const string SessionReplaced = "SESSION_REPLACED";

    public static string Paired(string sessionToken)
    {
        ArgumentException.ThrowIfNullOrEmpty(sessionToken);
        return JsonSerializer.Serialize(new { type = "PAIRED", version = 1, sessionToken });
    }

    public static string Error(string reason)
    {
        ArgumentException.ThrowIfNullOrEmpty(reason);
        return JsonSerializer.Serialize(new { type = "ERROR", version = 1, reason });
    }

    /// <summary>予言者の夢と依頼の今の状態（<c>DREAM_STATE</c>）。</summary>
    /// <param name="cards">手札（使う記号）。</param>
    public static string DreamState(ProphetDreams prophet, IEnumerable<Symbol> cards, bool isDestroyed)
    {
        ArgumentNullException.ThrowIfNull(prophet);
        ArgumentNullException.ThrowIfNull(cards);
        var state = prophet.State switch
        {
            ProphetDreamState.None => "NONE",
            ProphetDreamState.Untampered => "UNTAMPERED",
            ProphetDreamState.Tampered => "TAMPERED",
            _ => throw new ArgumentOutOfRangeException(nameof(prophet), prophet.State, "夢の状態が分からない"),
        };
        var request = prophet.ActiveRequest is { } active ? new { requestType = active.Id, symbolId = active.SymbolId } : null;
        return JsonSerializer.Serialize(new
        {
            type = "DREAM_STATE",
            version = 1,
            state,
            nodes = prophet.Dream.Select(node => new { symbolId = node.SymbolId, weight = node.Weight }),
            cards = cards.Select(symbol => symbol.Id),
            canTamper = prophet.CanTamper,
            canPronounce = prophet.CanPronounce,
            isDestroyed,
            request,
        });
    }
}
