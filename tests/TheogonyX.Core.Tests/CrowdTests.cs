using TheogonyX.Core;

namespace TheogonyX.Core.Tests;

public class CrowdTests
{
    private const int Columns = 16;
    private const int Rows = 9;
    private const float HolySiteX = 8f;
    private const int StepsPerSecond = 60;

    private static Crowd Create(int seed = 42) => new(seed, TestRules.Crowd, Columns, Rows, HolySiteX);

    private static TileType[,] Uniform(TileType tileType)
    {
        var tiles = new TileType[Columns, Rows];
        for (var column = 0; column < Columns; column++)
        {
            for (var row = 0; row < Rows; row++)
            {
                tiles[column, row] = tileType;
            }
        }
        return tiles;
    }

    private static PotentialField Field(TileType[,] tiles) => PotentialField.Build(tiles, TestRules.Crowd.TileForces);

    private static void Run(Crowd crowd, CrowdMode mode, PotentialField field, int steps)
    {
        for (var i = 0; i < steps; i++)
        {
            crowd.Step(mode, field);
        }
    }

    private static void AssertInsideGround(Crowd crowd)
    {
        for (var i = 0; i < crowd.Count; i++)
        {
            Assert.InRange(crowd.X[i], 0f, Columns);
            Assert.InRange(crowd.Y[i], 0f, Rows);
        }
    }

    private static float DistanceToHolySite(Crowd crowd, int i)
    {
        var dx = crowd.X[i] - HolySiteX;
        var dy = crowd.Y[i];
        return MathF.Sqrt(dx * dx + dy * dy);
    }

    [Fact]
    [Trait("TS", "TS-crowd-001")]
    public void Crowd_同じシード_始めの配置が同じ()
    {
        var first = Create();
        var second = Create();

        Assert.Equal(first.X.ToArray(), second.X.ToArray());
        Assert.Equal(first.Y.ToArray(), second.Y.ToArray());
    }

    [Theory]
    [Trait("TS", "TS-crowd-002")]
    [InlineData(0)]
    [InlineData(42)]
    [InlineData(int.MaxValue)]
    [InlineData(-1)]
    public void Crowd_任意のシード_定義の人数が地表の中にいる(int seed)
    {
        var crowd = Create(seed);

        Assert.Equal(10_000, crowd.Count);
        AssertInsideGround(crowd);
    }

    [Theory]
    [Trait("TS", "TS-crowd-003")]
    [InlineData(49.9f, false, CrowdMode.Wander)]
    [InlineData(50.0f, false, CrowdMode.Frenzy)]
    [InlineData(80.0f, true, CrowdMode.Stopped)]
    public void ModeOf_カオス度と崩壊_状態が決まる(float chaos, bool destroyed, CrowdMode expected)
    {
        var world = new WorldState { ChaosLevel = chaos, IsDestroyed = destroyed };

        Assert.Equal(expected, Crowd.ModeOf(world, TestRules.Crowd));
    }

    [Fact]
    [Trait("TS", "TS-crowd-004")]
    public void Step_散策で60秒_全員が地表の中にいる()
    {
        var crowd = Create();

        Run(crowd, CrowdMode.Wander, Field(Uniform(TileType.Grassland)), 60 * StepsPerSecond);

        AssertInsideGround(crowd);
    }

    [Fact]
    [Trait("TS", "TS-crowd-005")]
    public void Step_同じシードと同じ状態の移り変わり_位置が同じ()
    {
        var first = Create();
        var second = Create();
        var field = Field(Uniform(TileType.Grassland));

        foreach (var crowd in new[] { first, second })
        {
            Run(crowd, CrowdMode.Wander, field, 300);
            Run(crowd, CrowdMode.Frenzy, field, 60);
            Run(crowd, CrowdMode.Stopped, field, 30);
            Run(crowd, CrowdMode.Wander, field, 300);
        }

        Assert.Equal(first.X.ToArray(), second.X.ToArray());
        Assert.Equal(first.Y.ToArray(), second.Y.ToArray());
    }

    [Fact]
    [Trait("TS", "TS-crowd-006")]
    public void Step_狂乱_範囲の外の人は聖地に近づく()
    {
        var crowd = Create();
        var field = Field(Uniform(TileType.Grassland));
        var before = Enumerable.Range(0, crowd.Count).Select(i => DistanceToHolySite(crowd, i)).ToArray();

        crowd.Step(CrowdMode.Frenzy, field);

        for (var i = 0; i < crowd.Count; i++)
        {
            if (before[i] > TestRules.Crowd.HolySiteRadius)
            {
                Assert.True(DistanceToHolySite(crowd, i) < before[i], $"index={i}");
            }
        }
    }

    [Fact]
    [Trait("TS", "TS-crowd-006")]
    public void Step_狂乱で十分な時間_全員が聖地の周りに留まる()
    {
        var crowd = Create();
        var field = Field(Uniform(TileType.Grassland));

        // 地表の対角線（約 18.4 タイル）を狂乱の速さ 2 タイル/秒で進める時間より長く
        Run(crowd, CrowdMode.Frenzy, field, 15 * StepsPerSecond);
        var settledX = crowd.X.ToArray();
        Run(crowd, CrowdMode.Frenzy, field, StepsPerSecond);

        for (var i = 0; i < crowd.Count; i++)
        {
            Assert.True(DistanceToHolySite(crowd, i) <= TestRules.Crowd.HolySiteRadius + 1e-4f, $"index={i}");
        }
        Assert.Equal(settledX, crowd.X.ToArray());
    }

    [Fact]
    [Trait("TS", "TS-crowd-007")]
    public void Step_停止_位置が変わらない()
    {
        var crowd = Create();
        var field = Field(Uniform(TileType.Grassland));
        Run(crowd, CrowdMode.Wander, field, 120);
        var x = crowd.X.ToArray();
        var y = crowd.Y.ToArray();

        Run(crowd, CrowdMode.Stopped, field, 120);

        Assert.Equal(x, crowd.X.ToArray());
        Assert.Equal(y, crowd.Y.ToArray());
    }

    [Fact]
    [Trait("TS", "TS-crowd-008")]
    public void Reset_動かした後_始めの配置に戻る()
    {
        var crowd = Create();
        var start = (X: crowd.X.ToArray(), Y: crowd.Y.ToArray());
        var field = Field(Uniform(TileType.Grassland));
        Run(crowd, CrowdMode.Wander, field, 300);
        Run(crowd, CrowdMode.Frenzy, field, 300);

        crowd.Reset();

        Assert.Equal(start.X, crowd.X.ToArray());
        Assert.Equal(start.Y, crowd.Y.ToArray());
        Assert.Equal(0u, crowd.Steps);
    }

    [Fact]
    [Trait("TS", "TS-crowd-012")]
    public void Step_散策で左半分が焦土_右半分にいる人が増える()
    {
        var tiles = Uniform(TileType.Grassland);
        for (var column = 0; column < Columns / 2; column++)
        {
            for (var row = 0; row < Rows; row++)
            {
                tiles[column, row] = TileType.Scorched;
            }
        }
        var crowd = Create();
        int RightHalf() => Enumerable.Range(0, crowd.Count).Count(i => crowd.X[i] >= Columns / 2f);
        var before = RightHalf();

        Run(crowd, CrowdMode.Wander, Field(tiles), 30 * StepsPerSecond);

        Assert.True(RightHalf() > before, $"before={before} after={RightHalf()}");
    }

    [Fact]
    [Trait("TS", "TS-crowd-012")]
    public void Step_狂乱_焦土があっても動きが変わらない()
    {
        var scorched = Uniform(TileType.Grassland);
        for (var row = 0; row < Rows; row++)
        {
            scorched[3, row] = TileType.Scorched;
        }
        var withScorched = Create();
        var withoutScorched = Create();

        Run(withScorched, CrowdMode.Frenzy, Field(scorched), 120);
        Run(withoutScorched, CrowdMode.Frenzy, Field(Uniform(TileType.Grassland)), 120);

        Assert.Equal(withoutScorched.X.ToArray(), withScorched.X.ToArray());
        Assert.Equal(withoutScorched.Y.ToArray(), withScorched.Y.ToArray());
    }

    [Fact]
    [Trait("TS", "TS-crowd-012")]
    public void Build_一様な地表_力は0()
    {
        var field = Field(Uniform(TileType.Water));

        for (var column = 0; column < Columns; column++)
        {
            for (var row = 0; row < Rows; row++)
            {
                Assert.Equal((0f, 0f), field.At(column + 0.5f, row + 0.5f));
            }
        }
    }

    [Theory]
    [Trait("TS", "TS-crowd-013")]
    [InlineData(TimeControlState.Stopped, AutoPlaySpeed.X1, 0f)]
    [InlineData(TimeControlState.Stopped, AutoPlaySpeed.X3, 0f)]
    [InlineData(TimeControlState.Destroyed, AutoPlaySpeed.X1, 0f)]
    [InlineData(TimeControlState.AutoPlaying, AutoPlaySpeed.X1, 1f)]
    [InlineData(TimeControlState.AutoPlaying, AutoPlaySpeed.X2, 2f)]
    [InlineData(TimeControlState.AutoPlaying, AutoPlaySpeed.X3, 3f)]
    public void TimeScaleOf_時間の操作_自動進行中だけ進み2倍と3倍では速い(TimeControlState state, AutoPlaySpeed speed, float expected)
    {
        Assert.Equal(expected, Crowd.TimeScaleOf(state, speed, TestRules.Crowd));
    }

    private static readonly CrowdRules FlockingRules = TestRules.Crowd with
    {
        Flock = new FlockRules(Enabled: true, CellSize: 0.5f, AlignWeight: 0.6f, CrowdWeight: 0.4f, TargetNeighbors: 150f),
    };

    [Fact]
    [Trait("TS", "TS-crowd-014")]
    public void Step_群れの動きで60秒_全員が地表の中にいる()
    {
        var crowd = new Crowd(42, FlockingRules, Columns, Rows, HolySiteX);

        Run(crowd, CrowdMode.Wander, Field(Uniform(TileType.Grassland)), 60 * StepsPerSecond);

        AssertInsideGround(crowd);
    }

    [Fact]
    [Trait("TS", "TS-crowd-014")]
    public void Step_群れの動きで同じシード_位置が同じ()
    {
        var first = new Crowd(42, FlockingRules, Columns, Rows, HolySiteX);
        var second = new Crowd(42, FlockingRules, Columns, Rows, HolySiteX);
        var field = Field(Uniform(TileType.Grassland));

        Run(first, CrowdMode.Wander, field, 300);
        Run(second, CrowdMode.Wander, field, 300);

        Assert.Equal(first.X.ToArray(), second.X.ToArray());
        Assert.Equal(first.Y.ToArray(), second.Y.ToArray());
    }

    [Fact]
    [Trait("TS", "TS-crowd-014")]
    public void Step_群れの動き_使わないときと動きが変わる()
    {
        var flocking = new Crowd(42, FlockingRules, Columns, Rows, HolySiteX);
        var plain = Create();
        var field = Field(Uniform(TileType.Grassland));

        Run(flocking, CrowdMode.Wander, field, 60);
        Run(plain, CrowdMode.Wander, field, 60);

        Assert.NotEqual(plain.X.ToArray(), flocking.X.ToArray());
    }
}
