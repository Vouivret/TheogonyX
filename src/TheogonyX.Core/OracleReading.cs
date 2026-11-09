namespace TheogonyX.Core;

/// <summary>文を読んだときの地域の危機（docs/domain/script-decipher.md の「9. 危機で読みが変わる」）。</summary>
public enum Crisis
{
    /// <summary>どの危機でもない。</summary>
    Peace,

    /// <summary>渇き・飢え・暑さ・寒さ。</summary>
    Want,

    /// <summary>病が流行っている（疫病ができるまでは起きない）。</summary>
    Plague,

    /// <summary>攻められている・混乱。</summary>
    Strife,
}

/// <summary>危機で決まる、教えの行い方（docs/domain/script-decipher.md の 9）。</summary>
public enum OracleReading
{
    /// <summary>祀る（崇めよ × 平時）。</summary>
    Revere,

    /// <summary>蓄える（崇めよ × 欠乏）。</summary>
    Store,

    /// <summary>清める（崇めよ × 疫病）。</summary>
    Purify,

    /// <summary>守り神にする（崇めよ × 争い）。</summary>
    Guardian,

    /// <summary>遠ざける（触れるな × 平時）。</summary>
    Avoid,

    /// <summary>控える（触れるな × 欠乏）。</summary>
    Ration,

    /// <summary>隔てる（触れるな × 疫病）。</summary>
    Isolate,

    /// <summary>境を閉ざす（触れるな × 争い）。</summary>
    Seal,

    /// <summary>切り開く（壊せ × 平時）。</summary>
    Clear,

    /// <summary>取り尽くす（壊せ × 欠乏）。</summary>
    Strip,

    /// <summary>焼き払う（壊せ × 疫病）。</summary>
    Burn,

    /// <summary>武器と壁にする（壊せ × 争い）。</summary>
    Arm,
}

/// <summary>危機の判定と、動詞 × 危機の読みの表（docs/domain/script-decipher.md の 9）。乱数は使わない。</summary>
public static class OracleReadings
{
    /// <summary>読みを当てない記号（「記号の物」が決まっていない）。</summary>
    public const string UnreadSymbolId = "SYM_BLOOD";

    // 仕様書の表と 1 対 1 に対応させる（行が動詞の種別、列が危機）
    private static readonly IReadOnlyDictionary<(DogmaType, Crisis), OracleReading> Table = new Dictionary<(DogmaType, Crisis), OracleReading>
    {
        [(DogmaType.Worship, Crisis.Peace)] = OracleReading.Revere,
        [(DogmaType.Worship, Crisis.Want)] = OracleReading.Store,
        [(DogmaType.Worship, Crisis.Plague)] = OracleReading.Purify,
        [(DogmaType.Worship, Crisis.Strife)] = OracleReading.Guardian,
        [(DogmaType.Taboo, Crisis.Peace)] = OracleReading.Avoid,
        [(DogmaType.Taboo, Crisis.Want)] = OracleReading.Ration,
        [(DogmaType.Taboo, Crisis.Plague)] = OracleReading.Isolate,
        [(DogmaType.Taboo, Crisis.Strife)] = OracleReading.Seal,
        [(DogmaType.Destruction, Crisis.Peace)] = OracleReading.Clear,
        [(DogmaType.Destruction, Crisis.Want)] = OracleReading.Strip,
        [(DogmaType.Destruction, Crisis.Plague)] = OracleReading.Burn,
        [(DogmaType.Destruction, Crisis.Strife)] = OracleReading.Arm,
    };

    /// <summary>読みが、動詞の種別 <paramref name="type"/> の行にあるか（セーブの検証に使う）。</summary>
    public static bool Fits(DogmaType type, OracleReading reading) => Table.Any(entry => entry.Key.Item1 == type && entry.Value == reading);

    /// <summary>読みが表のどの動詞の種別の行にあるか。</summary>
    public static DogmaType TypeOf(OracleReading reading) => Table.First(entry => entry.Value == reading).Key.Item1;

    /// <summary>動詞の種別と危機から読みを引く。</summary>
    public static OracleReading Of(DogmaType type, Crisis crisis) => Table[(type, crisis)];

    /// <summary>教えに、危機で決まる読みを付ける。読みを当てない記号の教えは、そのまま返す。</summary>
    public static Dogma WithReading(Dogma teaching, Crisis crisis)
    {
        ArgumentNullException.ThrowIfNull(teaching);
        return teaching.TargetSymbolId == UnreadSymbolId ? teaching : teaching with { Reading = Of(teaching.Type, crisis) };
    }

    /// <summary>
    /// 今の危機。予言者の依頼の困りごとの条件をそのまま使い、争い・疫病・欠乏の順で先に当てはまるもの。どれでもなければ平時。
    /// 依頼の待つ年数は見ない（困りごとに当てはまるかだけを見る）。
    /// </summary>
    public static Crisis CrisisOf(
        WorldState world, Civilization? civilization, IReadOnlyList<ProphetRequestRule> requests, ScriptRules rules)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(requests);
        ArgumentNullException.ThrowIfNull(rules);
        bool Any(IReadOnlyList<string> ids) => requests.Any(request => ids.Contains(request.Id) && request.Matches(world, civilization));
        return Any(rules.StrifeRequests) ? Crisis.Strife : Any(rules.WantRequests) ? Crisis.Want : Crisis.Peace;
    }
}
