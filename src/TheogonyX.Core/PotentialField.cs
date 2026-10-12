namespace TheogonyX.Core;

/// <summary>地表のマスごとの、群衆を引き寄せる・遠ざける力（docs/domain/crowd.md）。</summary>
public sealed class PotentialField
{
    private readonly float[] _forceX;
    private readonly float[] _forceY;

    private PotentialField(int columns, int rows, float[] forceX, float[] forceY)
    {
        Columns = columns;
        Rows = rows;
        _forceX = forceX;
        _forceY = forceY;
    }

    public int Columns { get; }
    public int Rows { get; }

    /// <summary>マスごとの力。添字は 行 × 列数 + 列（GPU 版に渡す）。</summary>
    public ReadOnlySpan<float> ForceX => _forceX;
    public ReadOnlySpan<float> ForceY => _forceY;

    /// <summary>
    /// マスごとに、他の全マスの (力 − 全マスの力の平均) × そのマスへの向き ÷ (1 + 距離)² を足す。
    /// 平均を引くので、地表が一様なら力は 0 になる。
    /// </summary>
    /// <param name="tiles">添字は [列, 行]（<see cref="TerrainClassifier.ClassifyTiles"/>）。</param>
    public static PotentialField Build(TileType[,] tiles, IReadOnlyDictionary<TileType, float> tileForces)
    {
        ArgumentNullException.ThrowIfNull(tiles);
        ArgumentNullException.ThrowIfNull(tileForces);
        var columns = tiles.GetLength(0);
        var rows = tiles.GetLength(1);
        var cells = columns * rows;

        var potential = new float[cells];
        var sum = 0f;
        for (var i = 0; i < cells; i++)
        {
            var tileType = tiles[i % columns, i / columns];
            if (!tileForces.TryGetValue(tileType, out var force))
            {
                throw new ArgumentException($"タイル種別の力が無い: tileType={tileType}", nameof(tileForces));
            }
            potential[i] = force;
            sum += force;
        }
        var mean = sum / cells;

        var forceX = new float[cells];
        var forceY = new float[cells];
        for (var i = 0; i < cells; i++)
        {
            for (var j = 0; j < cells; j++)
            {
                if (i == j)
                {
                    continue;
                }
                float dx = j % columns - i % columns;
                float dy = j / columns - i / columns;
                var distance = MathF.Sqrt(dx * dx + dy * dy);
                // 向き（dx, dy を距離で割る）× 1 / (1 + 距離)²
                var weight = (potential[j] - mean) / (distance * (1f + distance) * (1f + distance));
                forceX[i] += dx * weight;
                forceY[i] += dy * weight;
            }
        }
        return new PotentialField(columns, rows, forceX, forceY);
    }

    /// <summary>
    /// 位置（タイルの単位）の力。周りの 4 マスの中心の力を、位置に応じて混ぜる（双線形補間）。
    /// マスの中でどこでも同じ力にすると、向きが逆のマスの境目に人が溜まって線に見えるため。端のマスの中心より外は、端の力。
    /// </summary>
    public (float X, float Y) At(float x, float y)
    {
        var gridX = Math.Clamp(x - 0.5f, 0f, Columns - 1f);
        var gridY = Math.Clamp(y - 0.5f, 0f, Rows - 1f);
        var column0 = (int)gridX;
        var row0 = (int)gridY;
        var column1 = Math.Min(column0 + 1, Columns - 1);
        var row1 = Math.Min(row0 + 1, Rows - 1);
        var tx = gridX - column0;
        var ty = gridY - row0;
        return (
            Blend(_forceX, row0 * Columns + column0, row0 * Columns + column1, row1 * Columns + column0, row1 * Columns + column1, tx, ty),
            Blend(_forceY, row0 * Columns + column0, row0 * Columns + column1, row1 * Columns + column0, row1 * Columns + column1, tx, ty));
    }

    // crowd_step.glsl の blend と同じ式・同じ順で計算する
    private static float Blend(float[] values, int topLeft, int topRight, int bottomLeft, int bottomRight, float tx, float ty)
    {
        var top = values[topLeft] + (values[topRight] - values[topLeft]) * tx;
        var bottom = values[bottomLeft] + (values[bottomRight] - values[bottomLeft]) * tx;
        return top + (bottom - top) * ty;
    }
}
