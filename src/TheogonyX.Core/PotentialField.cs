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

    /// <summary>位置（タイルの単位）のあるマスの力。地表の外は端のマスの力。</summary>
    public (float X, float Y) At(float x, float y)
    {
        var column = Math.Clamp((int)x, 0, Columns - 1);
        var row = Math.Clamp((int)y, 0, Rows - 1);
        var i = row * Columns + column;
        return (_forceX[i], _forceY[i]);
    }
}
