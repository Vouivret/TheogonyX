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

    /// <summary>マスごとのタイル種別。添字は [列, 行]。</summary>
    public static TileType[,] ClassifyTiles(WorldState world, TerrainRules rules)
    {
        var layout = Classify(world, rules);
        var tiles = new TileType[rules.Columns, rules.Rows];
        for (var column = 0; column < rules.Columns; column++)
        {
            var tileType = column < layout.Columns ? layout.TileType : TileType.Grassland;
            for (var row = 0; row < rules.Rows; row++)
            {
                tiles[column, row] = tileType;
            }
        }

        // 水場は氷河・焦土の列では凍る・蒸発し、それ以外の列では残る。
        var waterCells = WaterCellCount(world.WaterAmount, rules.LakeCells.Count);
        for (var i = 0; i < waterCells; i++)
        {
            var cell = rules.LakeCells[i];
            if (tiles[cell.Column, cell.Row] is not (TileType.Glacier or TileType.Scorched))
            {
                tiles[cell.Column, cell.Row] = TileType.Water;
            }
        }
        return tiles;
    }

    private static int WaterCellCount(float waterAmount, int lakeCells)
    {
        var rounded = (int)MathF.Round(waterAmount / 100f * lakeCells, MidpointRounding.AwayFromZero);
        return Math.Clamp(rounded, 0, lakeCells);
    }

    private static int ColumnsOf(float value, TerrainAnomaly anomaly, int columns)
    {
        var ratio = (value - anomaly.ZeroColumnValue) / (anomaly.FullColumnValue - anomaly.ZeroColumnValue);
        // 仕様書の「四捨五入」。既定の偶数丸めだと 14.5 が 14 になる。
        var rounded = (int)MathF.Round(ratio * columns, MidpointRounding.AwayFromZero);
        return Math.Clamp(rounded, 1, columns);
    }
}
