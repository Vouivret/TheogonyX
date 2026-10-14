namespace TheogonyX.Core;

/// <summary>予言者が依頼を出す困りごとの定義（docs/domain/prophet-request.md）。</summary>
/// <param name="Id">困りごとの ID（例: <c>THIRST</c>）。ログの <c>requestType</c>。</param>
/// <param name="SymbolId">依頼で示す記号。</param>
public sealed record ProphetRequestRule(
    string Id,
    WorldMetric Metric,
    ThresholdComparison Comparison,
    float Threshold,
    string SymbolId)
{
    /// <param name="civilization">廃墟の数（<see cref="WorldMetric.RuinCount"/>）を数える文明。無ければ廃墟は 0。</param>
    public bool Matches(WorldState world, Civilization? civilization = null)
    {
        ArgumentNullException.ThrowIfNull(world);
        var value = Metric == WorldMetric.RuinCount ? civilization?.Ruins.Count ?? 0 : world.ValueOf(Metric);
        return Comparison == ThresholdComparison.AtOrBelow ? value <= Threshold : value >= Threshold;
    }
}
