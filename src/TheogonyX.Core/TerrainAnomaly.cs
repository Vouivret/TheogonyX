namespace TheogonyX.Core;

/// <summary>環境の異常1つ分の定義。</summary>
/// <param name="ZeroColumnValue">列数が 0 になる値（計算上。実際は最小 1 列）。</param>
/// <param name="FullColumnValue">全列がこの種別になる値。</param>
public sealed record TerrainAnomaly(
    TileType TileType,
    WorldMetric Metric,
    ThresholdComparison Comparison,
    float Threshold,
    float ZeroColumnValue,
    float FullColumnValue);
