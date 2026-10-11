namespace TheogonyX.Core;

/// <summary>世界の状態から地表のタイル種別と列数を決める（docs/domain/planet-display.md）。</summary>
public static class TerrainClassifier
{
    public static TerrainLayout Classify(WorldState world, TerrainRules rules)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(rules);

        // 優先度の順に調べ、最初に当てはまった異常で決める。
        foreach (var anomaly in rules.Anomalies)
        {
            var value = world.ValueOf(anomaly.Metric);
            var matched = anomaly.Comparison == ThresholdComparison.AtOrBelow
                ? value <= anomaly.Threshold
                : value >= anomaly.Threshold;
            if (matched)
            {
                return new TerrainLayout(anomaly.TileType, ColumnsOf(value, anomaly, rules.Columns));
            }
        }
        return new TerrainLayout(TileType.Grassland, 0);
    }

    private static int ColumnsOf(float value, TerrainAnomaly anomaly, int columns)
    {
        var ratio = (value - anomaly.ZeroColumnValue) / (anomaly.FullColumnValue - anomaly.ZeroColumnValue);
        // 仕様書の「四捨五入」。既定の偶数丸めだと 14.5 が 14 になる。
        var rounded = (int)MathF.Round(ratio * columns, MidpointRounding.AwayFromZero);
        return Math.Clamp(rounded, 1, columns);
    }
}
