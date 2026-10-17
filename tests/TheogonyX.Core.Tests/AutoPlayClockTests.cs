namespace TheogonyX.Core.Tests;

public class AutoPlayClockTests
{
    // docs/domain/time-control.md: 1 倍は 10 分（600 秒）に 1 年、2 倍は 300 秒、3 倍は 200 秒
    private static AutoPlayClock CreateClock() => new(new Dictionary<AutoPlaySpeed, double>
    {
        [AutoPlaySpeed.X1] = 600.0,
        [AutoPlaySpeed.X2] = 300.0,
        [AutoPlaySpeed.X3] = 200.0,
    });

    private static AutoPlayClock CreateTripleClock()
    {
        var clock = CreateClock();
        clock.CycleSpeed();
        clock.CycleSpeed();
        return clock;
    }

    [Fact]
    [Trait("TS", "TS-time-control-010")]
    public void Tick_1倍で599秒_年は進まずあと1秒で進み0秒に戻る()
    {
        var clock = CreateClock();

        Assert.False(clock.Tick(599.0));
        Assert.Equal(1.0, clock.RemainingSeconds, 6);
        Assert.True(clock.Tick(1.0));
        Assert.Equal(0.0, clock.ElapsedSeconds, 6);
    }

    [Fact]
    [Trait("TS", "TS-time-control-010")]
    public void Tick_1倍で進み360秒から240秒_1年進む()
    {
        var clock = CreateClock();
        clock.Tick(360.0);

        // 一時停止のあいだは Tick を呼ばない。進みは残る
        Assert.True(clock.Tick(240.0));
    }

    [Fact]
    [Trait("TS", "TS-time-control-010")]
    public void CycleSpeed_1倍で進み360秒_2倍になり進みは0秒()
    {
        var clock = CreateClock();
        clock.Tick(360.0);

        clock.CycleSpeed();

        Assert.Equal(AutoPlaySpeed.X2, clock.Speed);
        Assert.Equal(0.0, clock.ElapsedSeconds);
        Assert.Equal(300.0, clock.SecondsPerYear);
    }

    [Fact]
    [Trait("TS", "TS-time-control-010")]
    public void CycleSpeed_3倍_1倍に戻る()
    {
        var clock = CreateTripleClock();
        Assert.Equal(AutoPlaySpeed.X3, clock.Speed);

        clock.CycleSpeed();

        Assert.Equal(AutoPlaySpeed.X1, clock.Speed);
    }

    [Fact]
    [Trait("TS", "TS-time-control-010")]
    public void Tick_3倍で1回に500秒_1年だけ進み進みは300秒()
    {
        var clock = CreateTripleClock();

        Assert.True(clock.Tick(500.0));
        Assert.Equal(300.0, clock.ElapsedSeconds, 6);
        Assert.True(clock.Tick(0.0));
        Assert.Equal(100.0, clock.ElapsedSeconds, 6);
    }

    [Theory]
    [Trait("TS", "TS-time-control-010")]
    [InlineData(-0.1)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void Tick_不正な経過秒_例外で進みは変わらない(double delta)
    {
        var clock = CreateClock();
        clock.Tick(0.3);

        Assert.Throws<ArgumentOutOfRangeException>(() => clock.Tick(delta));
        Assert.Equal(0.3, clock.ElapsedSeconds, 6);
    }
}
