namespace TheogonyX.Core;

/// <summary>
/// 群衆の位置と動き（docs/domain/crowd.md）。見た目だけで、世界の状態には書き戻さない。
/// 位置はタイルの単位で、地表は 0 ≦ x ≦ 列数、0 ≦ y ≦ 行数。固定の歩幅（<see cref="StepSeconds"/>）で進める。
/// </summary>
public sealed class Crowd
{
    /// <summary>1 歩の秒数。</summary>
    public const float StepSeconds = 1f / 60f;

    // 乱数の用途ごとに、歩数の代わりに使う値（歩数とぶつからないよう、大きな値にする）。
    private const uint StartXChannel = 0xFFFFFFF0u;
    private const uint StartYChannel = 0xFFFFFFF1u;
    private const uint StartHeadingChannel = 0xFFFFFFF2u;
    private const uint TargetAngleChannel = 0xFFFFFFF3u;
    private const uint TargetRadiusChannel = 0xFFFFFFF4u;

    private readonly uint _seed;
    private readonly CrowdRules _rules;
    private readonly float _width;
    private readonly float _height;
    private readonly uint _turnSteps;
    private readonly float[] _x;
    private readonly float[] _y;
    private readonly float[] _heading;
    private readonly float[] _targetX;
    private readonly float[] _targetY;
    private readonly FlockGrid? _flock;

    /// <param name="holySiteX">聖地の左右の位置（タイルの単位）。上下は地表の上端（y = 0）。</param>
    public Crowd(int seed, CrowdRules rules, int columns, int rows, float holySiteX)
    {
        ArgumentNullException.ThrowIfNull(rules);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(rules.Count);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(columns);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(rows);
        _seed = unchecked((uint)seed);
        _rules = rules;
        _width = columns;
        _height = rows;
        _turnSteps = (uint)Math.Max(1, (int)MathF.Round(rules.TurnIntervalSeconds / StepSeconds));
        _flock = rules.Flock.Enabled ? new FlockGrid(columns, rows, rules.Flock.CellSize) : null;
        _x = new float[rules.Count];
        _y = new float[rules.Count];
        _heading = new float[rules.Count];
        _targetX = new float[rules.Count];
        _targetY = new float[rules.Count];

        // 狂乱で向かう点: 聖地を中心に、地表の側の半円の中
        for (var i = 0; i < rules.Count; i++)
        {
            var angle = Random01(i, TargetAngleChannel) * MathF.PI;
            var radius = rules.HolySiteRadius * MathF.Sqrt(Random01(i, TargetRadiusChannel));
            _targetX[i] = Math.Clamp(holySiteX + radius * MathF.Cos(angle), 0f, _width);
            _targetY[i] = Math.Clamp(radius * MathF.Sin(angle), 0f, _height);
        }
        Reset();
    }

    public int Count => _x.Length;

    /// <summary>進めた歩数。停止の間は数えず、世界再創生で 0 に戻る。</summary>
    public uint Steps { get; private set; }

    public ReadOnlySpan<float> X => _x;
    public ReadOnlySpan<float> Y => _y;

    // 以下は GPU 版（同じ計算を Compute Shader で行う）に今の状態を渡すためのもの。
    public uint Seed => _seed;
    public int Columns => (int)_width;
    public int Rows => (int)_height;

    /// <summary>散策で向きを選び直す間隔（歩数）。</summary>
    public uint TurnSteps => _turnSteps;

    /// <summary>散策の向き（ラジアン）。</summary>
    public ReadOnlySpan<float> Heading => _heading;

    /// <summary>狂乱で向かう点。</summary>
    public ReadOnlySpan<float> TargetX => _targetX;
    public ReadOnlySpan<float> TargetY => _targetY;

    /// <summary>群れの集計のマス。群れの動きを使わないなら null。</summary>
    public FlockGrid? Flock => _flock;

    /// <summary>星の状態から、群衆の状態を決める。</summary>
    public static CrowdMode ModeOf(WorldState world, CrowdRules rules)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(rules);
        if (world.IsDestroyed)
        {
            return CrowdMode.Stopped;
        }
        return world.ChaosLevel >= rules.FrenzyChaosAtOrAbove ? CrowdMode.Frenzy : CrowdMode.Wander;
    }

    /// <summary>
    /// 実際の時間 1 秒で群衆の時間を何秒進めるか。自動進行中だけ進み（2 倍・3 倍では速く）、止まっている間は 0。
    /// 1 倍で 1、3 倍で <see cref="CrowdRules.FastTimeScale"/>、2 倍はその真ん中。
    /// </summary>
    public static float TimeScaleOf(TimeControlState state, AutoPlaySpeed speed, CrowdRules rules)
    {
        ArgumentNullException.ThrowIfNull(rules);
        if (state != TimeControlState.AutoPlaying)
        {
            return 0f;
        }
        return speed switch
        {
            AutoPlaySpeed.X3 => rules.FastTimeScale,
            AutoPlaySpeed.X2 => (1f + rules.FastTimeScale) / 2f,
            _ => 1f,
        };
    }

    /// <summary>始めの配置・向きに戻し、歩数を 0 にする（世界再創生）。</summary>
    public void Reset()
    {
        for (var i = 0; i < _x.Length; i++)
        {
            _x[i] = Random01(i, StartXChannel) * _width;
            _y[i] = Random01(i, StartYChannel) * _height;
            _heading[i] = Random01(i, StartHeadingChannel) * MathF.Tau;
        }
        Steps = 0;
    }

    /// <summary>1 歩進める。</summary>
    public void Step(CrowdMode mode, PotentialField field)
    {
        ArgumentNullException.ThrowIfNull(field);
        switch (mode)
        {
            case CrowdMode.Stopped:
                return;
            case CrowdMode.Wander:
                Wander(field);
                break;
            case CrowdMode.Frenzy:
                Frenzy();
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(mode), mode, null);
        }
        Steps++;
    }

    private void Wander(PotentialField field)
    {
        // 群れの集計は、この歩を始めたときの位置と向きで行う（GPU 版も全員の集計を終えてから動かす）
        _flock?.Accumulate(_x, _y, _heading);
        for (var i = 0; i < _x.Length; i++)
        {
            // 全員が同時に向きを変えないよう、人ごとに時期をずらす
            if ((Steps + (uint)i) % _turnSteps == 0)
            {
                _heading[i] = Random01(i, Steps) * MathF.Tau;
            }
            var (forceX, forceY) = field.At(_x[i], _y[i]);
            var (flockX, flockY) = FlockVelocity(i);
            var x = _x[i] + (MathF.Cos(_heading[i]) * _rules.WalkSpeed + forceX * _rules.FieldScale + flockX) * StepSeconds;
            var y = _y[i] + (MathF.Sin(_heading[i]) * _rules.WalkSpeed + forceY * _rules.FieldScale + flockY) * StepSeconds;

            // 地表の端を越えたら、位置を折り返して向きを反転する
            if (x < 0f || x > _width)
            {
                x = x < 0f ? -x : 2f * _width - x;
                _heading[i] = MathF.PI - _heading[i];
            }
            if (y < 0f || y > _height)
            {
                y = y < 0f ? -y : 2f * _height - y;
                _heading[i] = -_heading[i];
            }
            _x[i] = Math.Clamp(x, 0f, _width);
            _y[i] = Math.Clamp(y, 0f, _height);
        }
    }

    /// <summary>
    /// 群れの動きの速さ（タイル/秒）。周り（3 × 3 のマス）の平均の向きに寄せ、周りの人数が目標より少なければ重心に近づき、
    /// 多ければ離れる。集計は向きを選び直す前の向きで行う。
    /// </summary>
    private (float X, float Y) FlockVelocity(int i)
    {
        if (_flock is null)
        {
            return (0f, 0f);
        }
        var (column, row) = _flock.CellOf(_x[i], _y[i]);
        var (count, centerX, centerY, directionX, directionY) = _flock.Neighborhood(column, row);
        if (count <= 1)
        {
            return (0f, 0f);
        }
        var flock = _rules.Flock;
        var velocityX = directionX * _rules.WalkSpeed * flock.AlignWeight;
        var velocityY = directionY * _rules.WalkSpeed * flock.AlignWeight;
        var toCenterX = centerX - _x[i];
        var toCenterY = centerY - _y[i];
        var distance = MathF.Sqrt(toCenterX * toCenterX + toCenterY * toCenterY);
        if (distance > 1e-6f)
        {
            var crowding = Math.Clamp(1f - count / flock.TargetNeighbors, -1f, 1f);
            var speed = crowding * _rules.WalkSpeed * flock.CrowdWeight / distance;
            velocityX += toCenterX * speed;
            velocityY += toCenterY * speed;
        }
        return (velocityX, velocityY);
    }

    private void Frenzy()
    {
        var reach = _rules.FrenzySpeed * StepSeconds;
        for (var i = 0; i < _x.Length; i++)
        {
            var dx = _targetX[i] - _x[i];
            var dy = _targetY[i] - _y[i];
            var distance = MathF.Sqrt(dx * dx + dy * dy);
            if (distance <= reach)
            {
                _x[i] = _targetX[i];
                _y[i] = _targetY[i];
            }
            else
            {
                _x[i] += dx / distance * reach;
                _y[i] += dy / distance * reach;
            }
        }
    }

    private float Random01(int index, uint step) => CrowdHash.Next01(_seed, (uint)index, step);
}
