namespace TheogonyX.Core;

/// <summary>神の文に書かれた行き先。方角はいつも通じる。土地の名は予言者の知らない土地として扱う。</summary>
public enum HeardDestination
{
    None,
    East,
    West,
    North,
    South,
    Place,
}

/// <summary>予言者が向かう方角。この MVP の星は地域が東西に並ぶだけなので、東と西だけ。</summary>
public enum TravelDirection
{
    None,
    East,
    West,
}

/// <summary>
/// 聞き取りの答え（LLM が神の文を概念の ID に落としたもの。検証していない外部入力）。
/// </summary>
/// <param name="NounId">神の文の名詞。無ければ null。</param>
/// <param name="VerbId">神の文の動詞。無ければ null。</param>
/// <param name="SubstituteNounId">名詞が予言者の知らない言葉のとき、LLM が選んだ、近い知っている言葉の名詞。</param>
/// <param name="MysteryWords">どの記号にも当たらない言葉（神の文の言葉そのもの）。</param>
public sealed record HeardAnswer(
    string? NounId, string? VerbId, HeardDestination Destination, string? SubstituteNounId, IReadOnlyList<string> MysteryWords);

/// <summary>聞き違えた結果。</summary>
/// <param name="Teaching">教え。名詞が聞こえなかった（行き先だけの神託）ときは null。</param>
/// <param name="DestinationMisheard">神の文の行き先が、通じずに「なし」に聞き違えられた。</param>
/// <param name="BookEntries">方言帳に足す行（謎の言葉を含む）。</param>
public sealed record Hearing(
    Dogma? Teaching, string? HeardNounId, string? HeardVerbId, TravelDirection Destination, bool DestinationMisheard,
    IReadOnlyList<DialectEntry> BookEntries, IReadOnlyList<string> MysteryWords);

/// <summary>聞き取りの結果。<paramref name="RejectedReason"/> が null なら <paramref name="Hearing"/> がある。</summary>
public sealed record HearOutcome(Hearing? Hearing, string? RejectedReason);

/// <summary>
/// 予言者の聞き違え（docs/domain/oracle-pair.md の「方言のように聞き違える」）。
/// 知らない言葉を先に取り、ずれ表は動詞、次に名詞の順で、1 回の神託で 1 か所までずらす。謎の言葉は数えない。
/// 定義に無い名詞・動詞は「当てはまらなかった」（null）として扱う。知らない言葉の聞き違え先を LLM が選ばなかったときは、
/// 選べる名詞から乱数で選ぶ。答えが定義に合わないときは、例外にせず理由（<c>substitute_required</c>・<c>invalid_substitute</c>・
/// <c>invalid_mystery_word</c>・<c>nothing_heard</c>）を返す。
/// </summary>
public static class OracleHearer
{
    // TODO(AI_UNSURE): 謎の言葉の長さと数の上限は AI が仮に置いた
    public const int MaxMysteryWordLength = 20;
    public const int MaxMysteryWords = 5;

    /// <param name="handSymbolIds">手札の記号。予言者が知っている名詞の記号。</param>
    /// <param name="random">知らない言葉の聞き違え先を、LLM が選ばなかったときに選ぶ乱数。</param>
    public static HearOutcome Hear(
        DialectRules rules, DialectTable table, IReadOnlyCollection<string> handSymbolIds, HeardAnswer answer, Random random)
    {
        ArgumentNullException.ThrowIfNull(rules);
        ArgumentNullException.ThrowIfNull(table);
        ArgumentNullException.ThrowIfNull(handSymbolIds);
        ArgumentNullException.ThrowIfNull(answer);
        ArgumentNullException.ThrowIfNull(random);

        // 小さな LLM は一覧に無い ID を作ることがあるので、聞き取り全体を捨てずに、その語だけ聞こえなかったことにする
        var noun = answer.NounId is null ? null : rules.FindNoun(answer.NounId);
        var verb = answer.VerbId is null ? null : rules.FindVerb(answer.VerbId);
        var mysteries = answer.MysteryWords ?? [];
        if (mysteries.Count > MaxMysteryWords || mysteries.Any(word => !AiText.IsPlain(word, MaxMysteryWordLength)))
        {
            return Reject("invalid_mystery_word");
        }
        if (noun is null && verb is null && answer.Destination == HeardDestination.None)
        {
            // 近い言葉が無くても、予言者はいちばん近い（知っている）言葉に聞き違え、必ず教えになる。
            // 小さな LLM は選ばないので、知っている名詞から乱数で選ぶ。聞こえた言葉も謎の言葉も無ければ、聞こえなかったことにする
            var known = rules.Nouns.Where(candidate => handSymbolIds.Contains(candidate.SymbolId)).ToList();
            if (mysteries.Count == 0 || known.Count == 0)
            {
                return Reject("nothing_heard");
            }
            noun = known[random.Next(known.Count)];
        }

        var entries = new List<DialectEntry>();
        var heardNoun = noun;
        var heardVerb = verb;
        var unknown = noun is not null && !handSymbolIds.Contains(noun.SymbolId);
        if (unknown)
        {
            DialectNoun? substitute;
            if (answer.SubstituteNounId is null)
            {
                // 小さな LLM は聞き違え先を空で返しがちなので、選べる名詞（定義の順）から乱数で選ぶ
                var candidates = rules.Nouns.Where(candidate => IsValidSubstitute(rules, handSymbolIds, noun!, candidate)).ToList();
                if (candidates.Count == 0)
                {
                    return Reject("substitute_required");
                }
                substitute = candidates[random.Next(candidates.Count)];
            }
            else
            {
                substitute = rules.FindNoun(answer.SubstituteNounId);
                if (substitute is null || !IsValidSubstitute(rules, handSymbolIds, noun!, substitute))
                {
                    return Reject("invalid_substitute");
                }
            }
            heardNoun = substitute;
            entries.Add(new DialectEntry(noun!.Id, substitute.Id, DialectReason.Unknown));
        }
        else if (verb is not null && table.ShiftOf(verb.Id) is { } verbShift)
        {
            heardVerb = rules.FindVerb(verbShift.ToId);
            entries.Add(new DialectEntry(verb.Id, verbShift.ToId, DialectReason.Dialect));
        }
        else if (noun is not null && table.ShiftOf(noun.Id) is { } nounShift)
        {
            heardNoun = rules.FindNoun(nounShift.ToId);
            entries.Add(new DialectEntry(noun.Id, nounShift.ToId, DialectReason.Dialect));
        }

        entries.AddRange(mysteries.Select(word => new DialectEntry(word, null, DialectReason.Mystery)));
        Dogma? teaching = heardNoun is null
            ? null
            : new Dogma(heardNoun.SymbolId, heardVerb?.Type ?? rules.DefaultType, heardVerb?.Intensity ?? rules.DefaultIntensity);
        var destination = answer.Destination switch
        {
            HeardDestination.East => TravelDirection.East,
            HeardDestination.West => TravelDirection.West,
            _ => TravelDirection.None,
        };
        var misheard = answer.Destination is HeardDestination.North or HeardDestination.South or HeardDestination.Place;
        return new HearOutcome(new Hearing(teaching, heardNoun?.Id, heardVerb?.Id, destination, misheard, entries, [.. mysteries]), null);
    }

    // 選べるのは、手札の記号の名詞のうち、その言葉の隣の記号の名詞。隣に手札の記号が無ければ、手札の全部の記号の名詞
    private static bool IsValidSubstitute(DialectRules rules, IReadOnlyCollection<string> hand, DialectNoun noun, DialectNoun substitute)
    {
        if (!hand.Contains(substitute.SymbolId))
        {
            return false;
        }
        var neighborsInHand = rules.NeighborsOf(noun.SymbolId).Where(hand.Contains).ToList();
        return neighborsInHand.Count == 0 || neighborsInHand.Contains(substitute.SymbolId);
    }

    private static HearOutcome Reject(string reason) => new(null, reason);
}
