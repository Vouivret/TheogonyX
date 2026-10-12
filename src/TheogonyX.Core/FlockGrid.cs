namespace TheogonyX.Core;

/// <summary>
/// 群れの動きのための、マスごとの集計（人数・位置の和・向きの和）。
/// 和は固定小数点の整数で持つ。整数の足し算は順番によらないので、GPU 版（atomicAdd）と同じ値になる。
/// </summary>
public sealed class FlockGrid
{
    /// <summary>固定小数点の倍率（1 = 1/1024）。</summary>
    public const float FixedScale = 1024f;

    private readonly float _cellSize;
    private readonly int[] _count;
    private readonly int[] _sumX;
    private readonly int[] _sumY;
    private readonly int[] _sumDirectionX;
    private readonly int[] _sumDirectionY;

    /// <param name="columns">地表の列数（タイル）。</param>
    /// <param name="rows">地表の行数（タイル）。</param>
    public FlockGrid(int columns, int rows, float cellSize)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(cellSize);
        _cellSize = cellSize;
        GridColumns = (int)MathF.Ceiling(columns / cellSize);
        GridRows = (int)MathF.Ceiling(rows / cellSize);
        var cells = GridColumns * GridRows;
        _count = new int[cells];
        _sumX = new int[cells];
        _sumY = new int[cells];
        _sumDirectionX = new int[cells];
        _sumDirectionY = new int[cells];
    }

    public int GridColumns { get; }
    public int GridRows { get; }

    /// <summary>固定小数点にする（GLSL の <c>int(floor(v * 1024.0 + 0.5))</c> と同じ）。</summary>
    public static int ToFixed(float value) => (int)MathF.Floor(value * FixedScale + 0.5f);

    public (int Column, int Row) CellOf(float x, float y) =>
        (Math.Clamp((int)(x / _cellSize), 0, GridColumns - 1), Math.Clamp((int)(y / _cellSize), 0, GridRows - 1));

    /// <summary>全員の位置と向きを集計し直す。</summary>
    public void Accumulate(ReadOnlySpan<float> x, ReadOnlySpan<float> y, ReadOnlySpan<float> heading)
    {
        Array.Clear(_count);
        Array.Clear(_sumX);
        Array.Clear(_sumY);
        Array.Clear(_sumDirectionX);
        Array.Clear(_sumDirectionY);
        for (var i = 0; i < x.Length; i++)
        {
            var (column, row) = CellOf(x[i], y[i]);
            var cell = row * GridColumns + column;
            _count[cell]++;
            _sumX[cell] += ToFixed(x[i]);
            _sumY[cell] += ToFixed(y[i]);
            _sumDirectionX[cell] += ToFixed(MathF.Cos(heading[i]));
            _sumDirectionY[cell] += ToFixed(MathF.Sin(heading[i]));
        }
    }

    /// <summary>マスを中心とした 3 × 3 のマスの人数・重心・平均の向き。人数が 0 なら重心と向きは 0。</summary>
    public (int Count, float CenterX, float CenterY, float DirectionX, float DirectionY) Neighborhood(int column, int row)
    {
        int count = 0, sumX = 0, sumY = 0, sumDirectionX = 0, sumDirectionY = 0;
        for (var r = Math.Max(row - 1, 0); r <= Math.Min(row + 1, GridRows - 1); r++)
        {
            for (var c = Math.Max(column - 1, 0); c <= Math.Min(column + 1, GridColumns - 1); c++)
            {
                var cell = r * GridColumns + c;
                count += _count[cell];
                sumX += _sumX[cell];
                sumY += _sumY[cell];
                sumDirectionX += _sumDirectionX[cell];
                sumDirectionY += _sumDirectionY[cell];
            }
        }
        if (count == 0)
        {
            return (0, 0f, 0f, 0f, 0f);
        }
        var scale = count * FixedScale;
        return (count, sumX / scale, sumY / scale, sumDirectionX / scale, sumDirectionY / scale);
    }
}
