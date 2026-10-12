namespace TheogonyX.Core;

/// <summary>地表の判定に使うゲームデータ定義。</summary>
public sealed record TerrainRules
{
    /// <param name="columns">マップの列数。</param>
    /// <param name="rows">マップの行数。</param>
    /// <param name="anomalies">優先度の高い順。</param>
    /// <param name="lakeCells">湖になりうるマス。水場が広がる順。</param>
    public TerrainRules(int columns, int rows, IReadOnlyList<TerrainAnomaly> anomalies, IReadOnlyList<TerrainCell> lakeCells)
    {
        ArgumentNullException.ThrowIfNull(anomalies);
        ArgumentNullException.ThrowIfNull(lakeCells);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(columns);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(rows);
        var seen = new HashSet<TerrainCell>();
        foreach (var cell in lakeCells)
        {
            if (cell.Column < 0 || cell.Column >= columns || cell.Row < 0 || cell.Row >= rows)
            {
                throw new ArgumentException($"湖のマスが地表の外: column={cell.Column} row={cell.Row}", nameof(lakeCells));
            }
            if (!seen.Add(cell))
            {
                throw new ArgumentException($"湖のマスが重複している: column={cell.Column} row={cell.Row}", nameof(lakeCells));
            }
        }
        Columns = columns;
        Rows = rows;
        Anomalies = anomalies;
        LakeCells = lakeCells;
    }

    public int Columns { get; }
    public int Rows { get; }
    public IReadOnlyList<TerrainAnomaly> Anomalies { get; }
    public IReadOnlyList<TerrainCell> LakeCells { get; }
}
