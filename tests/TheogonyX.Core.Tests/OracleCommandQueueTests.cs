using TheogonyX.Core;

namespace TheogonyX.Core.Tests;

public class OracleCommandQueueTests
{
    private static readonly DreamNode[] TreeRainFire = [new("SYM_TREE", 0.5f), new("SYM_RAIN", 0.6f), new("SYM_FIRE", 0.4f)];
    private static readonly DreamNode[] TreeRainRain = [new("SYM_TREE", 0.5f), new("SYM_RAIN", 0.5f), new("SYM_RAIN", 0.5f)];

    // 夢を見るたびに、渡した夢を順に返す（最後の夢はくり返す）
    private static (OracleCommandQueue Queue, ProphetDreams Prophet, TimeControl Time) Create(params DreamNode[][] dreams)
    {
        var time = new TimeControl(TestRules.World, TestRules.JumpYears);
        var next = 0;
        var prophet = new ProphetDreams(
            time,
            TestRules.Dream,
            TestRules.ProphetDreams,
            () => dreams[Math.Min(next++, dreams.Length - 1)]);
        return (new OracleCommandQueue(prophet, time, TestRules.Dream), prophet, time);
    }

    [Fact]
    [Trait("TS", "TS-multiplayer-001")]
    public void Submit_改ざんと神託の命令を順に積む_ドグマが加わり夢が空になる()
    {
        var (queue, prophet, time) = Create(TreeRainFire);

        var tamper = queue.SubmitTamper(1, "SYM_BLOOD");
        var pronounce = queue.SubmitPronounce();

        Assert.True(tamper.Accepted);
        Assert.True(pronounce.Accepted);
        var dogma = Assert.Single(time.Dogmas);
        Assert.Equal("SYM_TREE", dogma.TargetSymbolId);
        Assert.Equal(DogmaType.Taboo, dogma.Type);
        Assert.Equal(dogma, pronounce.Result!.Dogma);
        Assert.Empty(prophet.Dream);
        Assert.Equal(ProphetDreamState.None, prophet.State);
    }

    [Fact]
    [Trait("TS", "TS-multiplayer-002")]
    public void Submit_同じコマへの改ざんを2つ積む_先の命令だけが効き後の命令は捨てる()
    {
        var (queue, prophet, _) = Create(TreeRainFire);

        var first = queue.SubmitTamper(1, "SYM_BLOOD");
        var second = queue.SubmitTamper(1, "SYM_FIRE");

        Assert.True(first.Accepted);
        Assert.Equal("cannot_tamper", second.RejectedReason);
        Assert.Equal("SYM_BLOOD", prophet.Dream[1].SymbolId);
        Assert.True(first.Command.Sequence < second.Command.Sequence);
    }

    [Fact]
    [Trait("TS", "TS-multiplayer-003")]
    public void Submit_夢なしで神託を積む_何も変わらず捨てる()
    {
        var (queue, prophet, time) = Create(TreeRainFire);
        queue.SubmitPronounce();

        var outcome = queue.SubmitPronounce();

        Assert.Equal("cannot_pronounce", outcome.RejectedReason);
        Assert.Single(time.Dogmas);
        Assert.Equal(ProphetDreamState.None, prophet.State);
    }

    [Theory]
    [Trait("TS", "TS-multiplayer-003")]
    [InlineData(-1, "SYM_BLOOD", "invalid_dream_index")]
    [InlineData(3, "SYM_BLOOD", "invalid_dream_index")]
    [InlineData(1, "SYM_UNKNOWN", "unknown_symbol")]
    public void Submit_範囲外の番号か知らない記号の改ざん_夢を変えずに捨てる(int index, string symbolId, string reason)
    {
        var (queue, prophet, _) = Create(TreeRainFire);

        var outcome = queue.SubmitTamper(index, symbolId);

        Assert.Equal(reason, outcome.RejectedReason);
        Assert.Equal(TreeRainFire, prophet.Dream);
        Assert.Equal(ProphetDreamState.Untampered, prophet.State);
    }

    [Fact]
    [Trait("TS", "TS-multiplayer-003")]
    public void Submit_崩壊した星_改ざんも神託も捨てる()
    {
        var (queue, prophet, time) = Create(TreeRainFire);
        time.World.IsDestroyed = true;

        var tamper = queue.SubmitTamper(1, "SYM_BLOOD");
        var pronounce = queue.SubmitPronounce();

        Assert.Equal("cannot_tamper", tamper.RejectedReason);
        Assert.Equal("cannot_pronounce", pronounce.RejectedReason);
        Assert.Equal(TreeRainFire, prophet.Dream);
        Assert.Empty(time.Dogmas);
    }

    [Fact]
    [Trait("TS", "TS-multiplayer-004")]
    public void Submit_同じシードと同じ命令の列を2回再生する_世界とドグマが一致する()
    {
        (int Year, IReadOnlyList<Dogma> Dogmas, float Chaos, float Forest) Replay()
        {
            var (queue, _, time) = Create(TreeRainFire, TreeRainRain, TreeRainFire);
            queue.SubmitTamper(1, "SYM_BLOOD");
            queue.SubmitPronounce();
            time.Jump();
            queue.SubmitTamper(0, "SYM_FIRE");
            queue.SubmitPronounce();
            time.Jump();
            return (time.World.Year, [.. time.Dogmas], time.World.ChaosLevel, time.World.ForestAmount);
        }

        var first = Replay();
        var second = Replay();

        Assert.Equal(first.Year, second.Year);
        Assert.Equal(first.Dogmas, second.Dogmas);
        Assert.Equal(first.Chaos, second.Chaos);
        Assert.Equal(first.Forest, second.Forest);
        Assert.NotEmpty(first.Dogmas);
    }

    [Fact]
    [Trait("TS", "TS-multiplayer-005")]
    public void Submit_命令を積んで実行した後_待ち行列は空で順番の番号は続く()
    {
        var (queue, _, _) = Create(TreeRainFire);

        var a = queue.SubmitTamper(0, "SYM_FIRE");
        var b = queue.SubmitPronounce();

        Assert.Equal(0, queue.PendingCount);
        Assert.Equal(1, a.Command.Sequence);
        Assert.Equal(2, b.Command.Sequence);
    }
}
