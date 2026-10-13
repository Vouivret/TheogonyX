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

    /// <summary>神託の今の状態（<c>ORACLE_STATE</c>）。手札・神威・神託を下せるか・崩壊・依頼。</summary>
    public static string OracleState(ProphetDreams prophet, bool isDestroyed)
    {
        ArgumentNullException.ThrowIfNull(prophet);
        var faith = prophet.Faith;
        var request = prophet.ActiveRequest is { } active ? new { requestType = active.Id, symbolId = active.SymbolId } : null;
        return JsonSerializer.Serialize(new
        {
            type = "ORACLE_STATE",
            version = 1,
            hand = prophet.Hand,
            faith = new { value = faith.Value, max = faith.Max, cost = faith.Cost },
            canPronounce = prophet.CanPronounce,
            isDestroyed,
            request,
        });
    }
}
