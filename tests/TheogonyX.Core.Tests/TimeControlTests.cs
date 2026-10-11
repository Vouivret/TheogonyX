using TheogonyX.Core;

namespace TheogonyX.Core.Tests;

public class TimeControlTests
{
    private const float Tolerance = 0.001f;

    private static readonly Dogma TreeTaboo = new("SYM_TREE", DogmaType.Taboo, 1.0f);
    private static readonly Dogma BloodDestruction = new("SYM_BLOOD", DogmaType.Destruction, 1.0f);

    private static TimeControl Create(params Dogma[] dogmas)
    {
        var control = new TimeControl(TestRules.World, TestRules.JumpYears);
        foreach (var dogma in dogmas)
        {
            control.AddDogma(dogma);
        }
        return control;
    }

    private static void AdvanceYears(TimeControl control, int years)
    {
        for (var i = 0; i < years; i++)
        {
            control.AdvanceOneYear();
        }
    }

    private static void AssertInitialWorld(WorldState world)
    {
        Assert.Equal(0, world.Year);
        Assert.Equal(100.0f, world.ForestAmount, Tolerance);
        Assert.Equal(100.0f, world.WaterAmount, Tolerance);
        Assert.Equal(15.0f, world.Temperature, Tolerance);
        Assert.Equal(0.0f, world.ChaosLevel, Tolerance);
        Assert.Equal(50.0f, world.HarmonyLevel, Tolerance);
        Assert.False(world.IsDestroyed);
    }

    [Fact]
    [Trait("TS", "TS-time-control-001")]
    public void Jump_停止中_停止中のまま100年進む()
    {
        var control = Create(TreeTaboo);

        control.Jump();

        Assert.Equal(TimeControlState.Stopped, control.State);
        Assert.Equal(100, control.World.Year);
    }

    [Fact]
    [Trait("TS", "TS-time-control-002")]
    public void Jump_途中で崩壊_崩壊になりリセットだけができる()
    {
        var control = Create(BloodDestruction);

        control.Jump();

        Assert.Equal(TimeControlState.Destroyed, control.State);
        Assert.Equal(67, control.World.Year);
        Assert.True(control.CanDo(TimeControlAction.Reset));
        Assert.False(control.CanDo(TimeControlAction.Jump));
        Assert.False(control.CanDo(TimeControlAction.StartAutoPlay));
        Assert.False(control.CanDo(TimeControlAction.Pause));
        Assert.False(control.CanDo(TimeControlAction.AdvanceOneYear));
    }

    [Fact]
    [Trait("TS", "TS-time-control-003")]
    public void AdvanceOneYear_自動進行中に崩壊_自動進行が止まる()
    {
        var control = Create(BloodDestruction);
        control.StartAutoPlay();
        AdvanceYears(control, 66);
        Assert.Equal(99.0f, control.World.ChaosLevel, Tolerance);

        control.AdvanceOneYear();

        Assert.Equal(TimeControlState.Destroyed, control.State);
        Assert.Equal(67, control.World.Year);
    }

    [Fact]
    [Trait("TS", "TS-time-control-004")]
    public void Reset_崩壊している_初期値に戻りドグマが空になる()
    {
        var control = Create(TreeTaboo, BloodDestruction);
        control.Jump();
        Assert.Equal(TimeControlState.Destroyed, control.State);

        control.Reset();

        Assert.Equal(TimeControlState.Stopped, control.State);
        AssertInitialWorld(control.World);
        Assert.Empty(control.Dogmas);
    }

    [Fact]
    [Trait("TS", "TS-time-control-005")]
    public void Pause_自動進行中_停止中になり年はそのまま()
    {
        var control = Create(TreeTaboo);
        control.StartAutoPlay();
        AdvanceYears(control, 10);

        control.Pause();

        Assert.Equal(TimeControlState.Stopped, control.State);
        Assert.Equal(10, control.World.Year);
    }

    [Fact]
    [Trait("TS", "TS-time-control-006")]
    public void Jump_自動進行中_100年進み自動進行は続く()
    {
        var control = Create(TreeTaboo);
        control.StartAutoPlay();
        AdvanceYears(control, 10);

        control.Jump();

        Assert.Equal(TimeControlState.AutoPlaying, control.State);
        Assert.Equal(110, control.World.Year);
    }

    [Fact]
    [Trait("TS", "TS-time-control-007")]
    public void Reset_崩壊前の自動進行中_停止中になりドグマが空になる()
    {
        var control = Create(TreeTaboo, BloodDestruction);
        control.StartAutoPlay();
        AdvanceYears(control, 30);

        control.Reset();

        Assert.Equal(TimeControlState.Stopped, control.State);
        AssertInitialWorld(control.World);
        Assert.Empty(control.Dogmas);
    }

    public static TheoryData<TimeControlState, TimeControlAction> ForbiddenActions => new()
    {
        { TimeControlState.Stopped, TimeControlAction.Pause },
        { TimeControlState.Stopped, TimeControlAction.AdvanceOneYear },
        { TimeControlState.AutoPlaying, TimeControlAction.StartAutoPlay },
        { TimeControlState.Destroyed, TimeControlAction.Jump },
        { TimeControlState.Destroyed, TimeControlAction.StartAutoPlay },
        { TimeControlState.Destroyed, TimeControlAction.Pause },
        { TimeControlState.Destroyed, TimeControlAction.AdvanceOneYear },
    };

    [Theory]
    [Trait("TS", "TS-time-control-008")]
    [MemberData(nameof(ForbiddenActions))]
    public void 操作_状態遷移表で不可_例外になり状態も年も変わらない(TimeControlState state, TimeControlAction action)
    {
        var control = Create(BloodDestruction);
        if (state == TimeControlState.AutoPlaying)
        {
            control.StartAutoPlay();
        }
        else if (state == TimeControlState.Destroyed)
        {
            control.Jump();
        }
        var year = control.World.Year;

        Assert.False(control.CanDo(action));
        Assert.Throws<InvalidOperationException>(() => Do(control, action));
        Assert.Equal(state, control.State);
        Assert.Equal(year, control.World.Year);
    }

    private static void Do(TimeControl control, TimeControlAction action)
    {
        switch (action)
        {
            case TimeControlAction.Jump: control.Jump(); break;
            case TimeControlAction.StartAutoPlay: control.StartAutoPlay(); break;
            case TimeControlAction.Pause: control.Pause(); break;
            case TimeControlAction.AdvanceOneYear: control.AdvanceOneYear(); break;
            case TimeControlAction.Reset: control.Reset(); break;
        }
    }
}
