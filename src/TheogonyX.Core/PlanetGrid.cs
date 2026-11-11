namespace TheogonyX.Core;

/// <summary>
/// 星の地域の格子。地域の番号は 段 × 列の数 + 列。横は輪（端でつながる）、縦はつながらない。隣は 8 方向。
/// </summary>
public sealed class PlanetGrid
{
    private readonly int[][] _neighbors;

    public PlanetGrid(int columns, int bands)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(columns, 2);
        ArgumentOutOfRangeException.ThrowIfLessThan(bands, 2);
        Columns = columns;
        Bands = bands;
        _neighbors = new int[columns * bands][];
        for (var region = 0; region < _neighbors.Length; region++)
        {
            var list = new SortedSet<int>();
            for (var dy = -1; dy <= 1; dy++)
            {
                for (var dx = -1; dx <= 1; dx++)
                {
                    if ((dx != 0 || dy != 0) && TryNeighbor(region, dx, dy, out var other) && other != region)
                    {
                        list.Add(other);
                    }
                }
            }
            _neighbors[region] = [.. list];
        }
    }

    public int Columns { get; }

    public int Bands { get; }

    public int Count => Columns * Bands;

    public int Index(int column, int band) => band * Columns + column;

    public int ColumnOf(int region) => region % Columns;

    public int BandOf(int region) => region / Columns;

    /// <summary>8 方向の隣（番号の小さい順）。</summary>
    public IReadOnlyList<int> Neighbors(int region) => _neighbors[region];

    /// <summary>向き (dx, dy) の隣。横は輪、縦は端の外なら false。</summary>
    public bool TryNeighbor(int region, int dx, int dy, out int neighbor)
    {
        var band = BandOf(region) + dy;
        if (band < 0 || band >= Bands)
        {
            neighbor = -1;
            return false;
        }
        neighbor = Index(((ColumnOf(region) + dx) % Columns + Columns) % Columns, band);
        return true;
    }

    /// <summary>距離。横（輪）と縦の歩数の大きいほう。</summary>
    public int Distance(int a, int b)
    {
        var dc = Math.Abs(ColumnOf(a) - ColumnOf(b));
        return Math.Max(Math.Min(dc, Columns - dc), Math.Abs(BandOf(a) - BandOf(b)));
    }
}
