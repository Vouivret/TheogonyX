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
    /// 実際の時間 1 秒で群衆の時間を何秒進めるか。自動進行中だけ進み（高速では速く）、止まっている間は 0。
    /// </summary>
    public static float TimeScaleOf(TimeControlState state, AutoPlaySpeed speed, CrowdRules rules)
    {
        ArgumentNullException.ThrowIfNull(rules);
        if (state != TimeControlState.AutoPlaying)
        {
            return 0f;
        }
        return speed == AutoPlaySpeed.Fast ? rules.FastTimeScale : 1f;
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
        for (var i = 0; i < _x.Length; i++)
        {
            // 全員が同時に向きを変えないよう、人ごとに時期をずらす
            if ((Steps + (uint)i) % _turnSteps == 0)
            {
                _heading[i] = Random01(i, Steps) * MathF.Tau;
            }
            var (forceX, forceY) = field.At(_x[i], _y[i]);
            var x = _x[i] + (MathF.Cos(_heading[i]) * _rules.WalkSpeed + forceX * _rules.FieldScale) * StepSeconds;
            var y = _y[i] + (MathF.Sin(_heading[i]) * _rules.WalkSpeed + forceY * _rules.FieldScale) * StepSeconds;

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
