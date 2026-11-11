namespace TheogonyX.Core;

/// <summary>新しい星のタイルの種別（docs/domain/planet-display.md、docs/domain/regional-environment.md）。</summary>
public static class PlanetTiles
{
    /// <summary>
    /// マスごとのタイル種別。添字は [列, 行]。海のタイルは水、陸は土台の地形（平地 ＝ 草地、森 ＝ 密林、山 ＝ 焦土の絵）。
    /// 地域の値が異常の条件（今の地表と同じ。優先度の順）に当たれば、その地域の陸はぜんぶ異常のタイルにする。
    /// </summary>
    public static TileType[,] Classify(RegionalWorld world, IReadOnlyList<TerrainAnomaly> anomalies)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(anomalies);
        var tiles = new TileType[world.Width, world.Height];
        var regionAnomaly = new TileType?[world.Grid.Count];
        for (var region = 0; region < regionAnomaly.Length; region++)
        {
            regionAnomaly[region] = AnomalyOf(world.Values[region], anomalies);
        }
        for (var x = 0; x < world.Width; x++)
        {
            for (var y = 0; y < world.Height; y++)
            {
                if (!world.IsLandAt(x, y))
                {
                    tiles[x, y] = TileType.Water;
                    continue;
                }
                var region = world.RegionAt(x, y);
                tiles[x, y] = regionAnomaly[region] ?? world.Terrain[region] switch
                {
                    BaseTerrain.Forest => TileType.Jungle,
                    BaseTerrain.Mountain => TileType.Scorched,
                    _ => TileType.Grassland,
                };
            }
        }
        return tiles;
    }

    private static TileType? AnomalyOf(EnvironmentValues values, IReadOnlyList<TerrainAnomaly> anomalies)
    {
        foreach (var anomaly in anomalies)
        {
            var value = anomaly.Metric switch
            {
                WorldMetric.ForestAmount => values.ForestAmount,
                WorldMetric.WaterAmount => values.WaterAmount,
                WorldMetric.Temperature => values.Temperature,
                WorldMetric.ChaosLevel => values.ChaosLevel,
                WorldMetric.HarmonyLevel => values.HarmonyLevel,
                _ => throw new ArgumentOutOfRangeException(nameof(anomalies), anomaly.Metric, null),
            };
            if (anomaly.Comparison == ThresholdComparison.AtOrBelow ? value <= anomaly.Threshold : value >= anomaly.Threshold)
            {
                return anomaly.TileType;
            }
        }
        return null;
    }
}
