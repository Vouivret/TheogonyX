namespace TheogonyX.Core.Tests;

public class AutoPlayClockTests
{
    // docs/domain/time-control.md: 通常 1 秒に 1 年、高速 0.1 秒に 1 年、現実同期 24 時間に 1 年
    private static AutoPlayClock CreateClock() => new(new Dictionary<AutoPlaySpeed, double>
    {
        [AutoPlaySpeed.Normal] = 1.0,
        [AutoPlaySpeed.Fast] = 0.1,
        [AutoPlaySpeed.RealTime] = 86400.0,
    });

    private static AutoPlayClock CreateRealTimeClock()
    {
        var clock = CreateClock();
        clock.CycleSpeed();
        clock.CycleSpeed();
        return clock;
    }

    [Fact]
    [Trait("TS", "TS-time-control-010")]
    public void Tick_現実同期で86399秒_年は進まずあと1秒で進み0秒に戻る()
    {
        var clock = CreateRealTimeClock();

        Assert.False(clock.Tick(86399.0));
        Assert.Equal(1.0, clock.RemainingSeconds, 6);
        Assert.True(clock.Tick(1.0));
        Assert.Equal(0.0, clock.ElapsedSeconds, 6);
    }

    [Fact]
    [Trait("TS", "TS-time-control-010")]
    public void Tick_通常で進み0点6秒から0点4秒_1年進む()
    {
        var clock = CreateClock();
        clock.Tick(0.6);

        // 一時停止のあいだは Tick を呼ばない。進みは残る
        Assert.True(clock.Tick(0.4));
    }

    [Fact]
    [Trait("TS", "TS-time-control-010")]
    public void CycleSpeed_通常で進み0点6秒_高速になり進みは0秒()
    {
        var clock = CreateClock();
        clock.Tick(0.6);

        clock.CycleSpeed();

        Assert.Equal(AutoPlaySpeed.Fast, clock.Speed);
        Assert.Equal(0.0, clock.ElapsedSeconds);
    }

    [Fact]
    [Trait("TS", "TS-time-control-010")]
    public void CycleSpeed_現実同期_通常に戻る()
    {
        var clock = CreateRealTimeClock();
        Assert.Equal(AutoPlaySpeed.RealTime, clock.Speed);

        clock.CycleSpeed();

        Assert.Equal(AutoPlaySpeed.Normal, clock.Speed);
    }

    [Fact]
    [Trait("TS", "TS-time-control-010")]
    public void Tick_通常で1回に2点5秒_1年だけ進み進みは1点5秒()
    {
        var clock = CreateClock();

        Assert.True(clock.Tick(2.5));
        Assert.Equal(1.5, clock.ElapsedSeconds, 6);
        Assert.True(clock.Tick(0.0));
        Assert.Equal(0.5, clock.ElapsedSeconds, 6);
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
