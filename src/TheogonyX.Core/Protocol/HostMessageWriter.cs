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

    /// <summary>
    /// 神託の今の状態（<c>ORACLE_STATE</c> version 2）。神威・語りかけられるか・崩壊・語りかけ方・依頼と祈り・選べる言葉・告げた神託・方言帳。
    /// 手札（予言者の語彙）は送らない。
    /// </summary>
    /// <param name="listening">予言者が聞いている途中か。</param>
    /// <param name="choiceMode">言葉を選んで語りかける形（<c>WORDS</c>）か。</param>
    /// <param name="prayerText">依頼中の祈りの文。無ければ null。</param>
    public static string OracleState(ProphetDreams prophet, bool isDestroyed, bool listening, bool choiceMode, string? prayerText)
    {
        ArgumentNullException.ThrowIfNull(prophet);
        var faith = prophet.Faith;
        var dialect = prophet.Dialect;
        var request = prophet.ActiveRequest is { } active
            ? new { requestType = active.Id, symbolId = active.SymbolId, prayer = prayerText }
            : null;

        // 言葉を選ぶ形のときだけ、選べる言葉を送る。名詞は、手札の記号の名詞だけ
        var knownSymbols = prophet.Hand.Select(prophet.SymbolOf).ToHashSet();
        var nouns = choiceMode && dialect is not null
            ? dialect.Rules.Nouns.Where(noun => knownSymbols.Contains(noun.SymbolId)).Select(noun => noun.Id).ToList()
            : [];
        var verbs = choiceMode && dialect is not null ? dialect.Rules.Verbs.Select(verb => verb.Id).ToList() : [];

        var told = dialect?.Records.Count > 0
            ? dialect.Records[^1] is { } record
                ? new
                {
                    year = record.Year,
                    teaching = record.Teaching is { } teaching
                        ? new { symbolId = teaching.TargetSymbolId, dogmaType = teaching.Type.ToString().ToUpperInvariant() }
                        : null,
                    heardNounId = record.HeardNounId,
                    heardVerbId = record.HeardVerbId,
                    destination = record.Destination.ToString().ToUpperInvariant(),
                    mysteryWords = record.MysteryWords,
                    heardText = record.HeardText,
                    narration = record.Narration,
                }
                : null
            : null;
        var book = (dialect?.Book.Entries ?? []).Select(entry => new
        {
            godWord = entry.GodWord,
            heardWordId = entry.HeardWordId,
            reason = entry.Reason.ToString().ToUpperInvariant(),
        });
        return JsonSerializer.Serialize(new
        {
            type = "ORACLE_STATE",
            version = 2,
            faith = new { value = faith.Value, max = faith.Max, cost = faith.Cost },
            canSpeak = !isDestroyed && !listening && dialect is not null && faith.CanAfford,
            isDestroyed,
            mode = choiceMode ? "WORDS" : "VOICE",
            listening,
            request,
            nouns,
            verbs,
            told,
            book,
        });
    }
}
