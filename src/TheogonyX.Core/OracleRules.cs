namespace TheogonyX.Core;

/// <summary>ルーンごとの重み（影響の強さ。0.0〜1.0）。第 1 段では、ルーンの ID は記号の ID と同じ。</summary>
public sealed record RuneWeight(string SymbolId, float Weight);

/// <summary>神威（docs/domain/oracle-pair.md の「神威の代償」）の定義。</summary>
/// <param name="Max">溜まる量の上限。</param>
/// <param name="PerYear">1 年に溜まる量。</param>
/// <param name="Cost">神託 1 回に使う量。</param>
/// <param name="Initial">最初と、世界再創生のときの量。</param>
public sealed record FaithRules(float Max, float PerYear, float Cost, float Initial);

/// <summary>神の歪み（短い年数に神託を重ねすぎたときの代償）の定義。</summary>
/// <param name="WindowYears">数える年数。今回を含め、この年数のうちに下した神託を数える。</param>
/// <param name="FreeCount">歪みなしで下せる回数。これを超えた分 1 つにつき歪みが起きる。</param>
/// <param name="SanityLoss">超えた分 1 つにつき、すべての住人の SAN 値が減る量。</param>
/// <param name="ChaosGain">超えた分 1 つにつき、カオス度が増える量。</param>
public sealed record OverloadRules(int WindowYears, int FreeCount, double SanityLoss, float ChaosGain);

/// <summary>汚染ルーン 1 種類の定義。</summary>
/// <param name="Id">ルーン（カード）の ID（例: <c>RUNE_MADNESS</c>）。記号の ID とは別。</param>
/// <param name="SymbolId">このルーンが持つ記号。夢の解釈では、この記号の極性と重みで計算する。</param>
public sealed record CorruptedRune(string Id, string SymbolId);

/// <summary>汚染ルーン（docs/domain/oracle-deck.md の「汚染」）の定義。</summary>
/// <param name="Thresholds">カオス度の閾値（小さい順）。初めて越えたとき、汚染ルーンを山札に 1 枚混ぜる。</param>
/// <param name="Runes">混ざる汚染ルーン。閾値の順に、定義の順で混ざる（足りなければ最初から巡る）。</param>
/// <param name="ChaosPerRune">汚染ルーンを神託に使ったとき、1 枚につき上がるカオス度。</param>
public sealed record CorruptionRules(IReadOnlyList<float> Thresholds, IReadOnlyList<CorruptedRune> Runes, float ChaosPerRune);

/// <summary>神託の作り方（ルーンの組）のゲームデータ定義（docs/domain/oracle-pair.md）。</summary>
/// <param name="Corruption">汚染ルーンの定義。省略したら、汚染ルーンは混ざらない。</param>
public sealed record OracleRules(IReadOnlyList<RuneWeight> RuneWeights, FaithRules Faith, OverloadRules Overload, CorruptionRules? Corruption = null)
{
    private static readonly CorruptionRules NoCorruption = new([], [], 0f);

    /// <summary>汚染ルーンの定義（無ければ、混ざらない定義）。</summary>
    public CorruptionRules CorruptionOrNone => Corruption ?? NoCorruption;

    /// <summary>汚染ルーンの ID か。</summary>
    public bool IsCorrupted(string cardId) => CorruptionOrNone.Runes.Any(rune => rune.Id == cardId);

    /// <summary>カードが持つ記号。汚染ルーンなら定義の記号、基本ルーンなら ID そのもの。</summary>
    public string SymbolOf(string cardId) => CorruptionOrNone.Runes.FirstOrDefault(rune => rune.Id == cardId)?.SymbolId ?? cardId;

    /// <summary>ルーン（記号）の重み。定義に無ければ <see cref="ArgumentException"/>。</summary>
    public float WeightOf(string symbolId)
    {
        foreach (var rune in RuneWeights)
        {
            if (rune.SymbolId == symbolId)
            {
                return rune.Weight;
            }
        }
        throw new ArgumentException($"ルーンの重みの定義が無い: symbolId={symbolId}", nameof(symbolId));
    }

    /// <summary>値の範囲を確かめる。範囲外なら <see cref="ArgumentException"/>。</summary>
    public void Validate(IReadOnlySet<string> symbolIds)
    {
        ArgumentNullException.ThrowIfNull(symbolIds);
        if (RuneWeights is null || Faith is null || Overload is null)
        {
            throw new ArgumentException("神託の定義が足りない");
        }
        foreach (var symbolId in symbolIds)
        {
            var weight = RuneWeights.FirstOrDefault(rune => rune.SymbolId == symbolId)
                ?? throw new ArgumentException($"ルーンの重みの定義が無い: symbolId={symbolId}");
            if (weight.Weight is < 0f or > 1f)
            {
                throw new ArgumentException($"ルーンの重みが範囲外: symbolId={symbolId} weight={weight.Weight}");
            }
        }
        if (Faith.Max <= 0 || Faith.PerYear < 0 || Faith.Cost <= 0 || Faith.Cost > Faith.Max || Faith.Initial < 0 || Faith.Initial > Faith.Max)
        {
            throw new ArgumentException("神威の定義の値が範囲外");
        }
        if (Overload.WindowYears < 1 || Overload.FreeCount < 0 || Overload.SanityLoss < 0 || Overload.ChaosGain < 0)
        {
            throw new ArgumentException("神の歪みの定義の値が範囲外");
        }

        var corruption = CorruptionOrNone;
        if (corruption.Thresholds is null || corruption.Runes is null || corruption.ChaosPerRune < 0
            || corruption.Thresholds.Any(threshold => threshold is <= 0f or > 100f)
            || corruption.Thresholds.Zip(corruption.Thresholds.Skip(1), (a, b) => a >= b).Any(notAscending => notAscending)
            || (corruption.Thresholds.Count > 0 && corruption.Runes.Count == 0))
        {
            throw new ArgumentException("汚染ルーンの定義の値が範囲外");
        }
        foreach (var rune in corruption.Runes)
        {
            if (string.IsNullOrEmpty(rune.Id) || symbolIds.Contains(rune.Id) || !symbolIds.Contains(rune.SymbolId)
                || corruption.Runes.Count(other => other.Id == rune.Id) > 1)
            {
                throw new ArgumentException($"汚染ルーンの定義が正しくない: id={rune.Id} symbolId={rune.SymbolId}");
            }
        }
    }
}
