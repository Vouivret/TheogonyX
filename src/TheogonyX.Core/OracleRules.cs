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

/// <summary>神託の作り方（ルーンの組）のゲームデータ定義（docs/domain/oracle-pair.md）。</summary>
public sealed record OracleRules(IReadOnlyList<RuneWeight> RuneWeights, FaithRules Faith, OverloadRules Overload)
{
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
    }
}
