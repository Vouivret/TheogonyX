namespace TheogonyX.Core;

/// <summary>語の一覧の名詞。概念の ID と、それが指す記号。表示する語は翻訳ファイルに置く。</summary>
public sealed record DialectNoun(string Id, string SymbolId);

/// <summary>語の一覧の動詞。概念の ID と、教えの種別・強度。</summary>
public sealed record DialectVerb(string Id, DogmaType Type, float Intensity);

/// <summary>記号どうしの結びつき（向きの無い対。docs/domain/oracle-wuxing.md の「現実の結びつきの表」）。</summary>
public sealed record SymbolLink(string A, string B);

/// <summary>予言者の器の段。<paramref name="MisheardRatio"/> は、ずれ表に当たる語の割合（0〜1）。</summary>
public sealed record DialectVessel(string Id, float MisheardRatio);

/// <summary>
/// 方言の聞き違えの定義（docs/domain/oracle-pair.md の「方言のように聞き違える」）。
/// 値は仮置き（<c>TODO(AI_UNSURE)</c>）で、データ定義に置く。
/// </summary>
/// <param name="ReversalProbability">ずれる動詞のうち、崇拝と破壊が入れ替わる確率。</param>
/// <param name="DefaultType">動詞が聞き取れなかったときの種別。</param>
/// <param name="DefaultIntensity">動詞が聞き取れなかったときの強度。</param>
/// <param name="PrayerMinWords">祈りに入れる名詞の数の下限（仮）。</param>
/// <param name="PrayerMaxWords">祈りに入れる名詞の数の上限（仮）。</param>
/// <param name="DailyPrayerYears">困りごとの無い間に、短い祈りをささやく間隔（年。仮）。</param>
public sealed record DialectRules(
    IReadOnlyList<DialectNoun> Nouns,
    IReadOnlyList<DialectVerb> Verbs,
    IReadOnlyList<SymbolLink> Links,
    IReadOnlyList<DialectVessel> Vessels,
    float ReversalProbability,
    DogmaType DefaultType,
    float DefaultIntensity,
    int PrayerMinWords = 2,
    int PrayerMaxWords = 3,
    int DailyPrayerYears = 20)
{
    /// <summary>値が範囲の中にあり、ID が重複していないことを確かめる。</summary>
    /// <exception cref="ArgumentException">範囲外の値、重複した ID。</exception>
    public void Validate()
    {
        ArgumentNullException.ThrowIfNull(Nouns);
        ArgumentNullException.ThrowIfNull(Verbs);
        ArgumentNullException.ThrowIfNull(Links);
        ArgumentNullException.ThrowIfNull(Vessels);
        var wordIds = Nouns.Select(noun => noun.Id).Concat(Verbs.Select(verb => verb.Id)).ToList();
        if (wordIds.Count != wordIds.Distinct().Count()
            || Vessels.Select(vessel => vessel.Id).Distinct().Count() != Vessels.Count
            || Vessels.Any(vessel => !IsRatio(vessel.MisheardRatio))
            || Verbs.Any(verb => !IsRatio(verb.Intensity))
            || !IsRatio(ReversalProbability) || !IsRatio(DefaultIntensity)
            || PrayerMinWords < 1 || PrayerMaxWords < PrayerMinWords || DailyPrayerYears < 1)
        {
            throw new ArgumentException("方言の定義の値が範囲外か、ID が重複している");
        }
    }

    public DialectNoun? FindNoun(string id) => Nouns.FirstOrDefault(noun => noun.Id == id);

    public DialectVerb? FindVerb(string id) => Verbs.FirstOrDefault(verb => verb.Id == id);

    /// <summary>記号 <paramref name="symbolId"/> に結びつく記号。</summary>
    public IEnumerable<string> NeighborsOf(string symbolId) =>
        Links.Where(link => link.A == symbolId || link.B == symbolId).Select(link => link.A == symbolId ? link.B : link.A);

    private static bool IsRatio(float value) => float.IsFinite(value) && value is >= 0f and <= 1f;
}
