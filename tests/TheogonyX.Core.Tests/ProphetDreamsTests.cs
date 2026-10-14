using TheogonyX.Core;

namespace TheogonyX.Core.Tests;

// 予言者の依頼（docs/domain/prophet-request.md）。神託の作り方は OraclePairTests。
public class ProphetDreamsTests
{
    private static (ProphetDreams Prophet, TimeControl Time) Create()
    {
        var time = new TimeControl(TestRules.World, TestRules.JumpYears);
        var prophet = new ProphetDreams(time, TestRules.ProphetDreams, new OracleDeck(42, TestRules.Deck), TestRules.Oracle);
        return (prophet, time);
    }

    private static void SetYear(TimeControl time, int year) => time.World.Year = year;

    [Fact]
    [Trait("TS", "TS-prophet-request-007")]
    public void OnTimeAdvanced_自動進行中に依頼が出る_自動進行が止まる()
    {
        var (prophet, time) = Create();
        time.StartAutoPlay();
        time.World.WaterAmount = 10f;

        var update = prophet.OnTimeAdvanced();

        Assert.True(update.PausedAutoPlay);
        Assert.Equal(TimeControlState.Stopped, time.State);
    }

    [Fact]
    [Trait("TS", "TS-prophet-request-007")]
    public void OnTimeAdvanced_自動進行中に100年たつ_依頼が無ければ止まらない()
    {
        var (prophet, time) = Create();
        time.StartAutoPlay();
        SetYear(time, 100);

        var update = prophet.OnTimeAdvanced();

        Assert.False(update.PausedAutoPlay);
        Assert.Equal(TimeControlState.AutoPlaying, time.State);
    }

    [Fact]
    [Trait("TS", "TS-prophet-request-009")]
    public void PronouncePair_依頼で止めた自動進行_神託を下すと再開する()
    {
        var (prophet, time) = Create();
        time.StartAutoPlay();
        time.World.WaterAmount = 10f;
        prophet.OnTimeAdvanced();

        var result = prophet.PronouncePair(0, 1);

        Assert.True(result.ResumedAutoPlay);
        Assert.Equal(TimeControlState.AutoPlaying, time.State);
    }

    [Fact]
    [Trait("TS", "TS-prophet-request-009")]
    public void PronouncePair_依頼で止めて自分で再開した_神託でも状態は変わらない()
    {
        var (prophet, time) = Create();
        time.StartAutoPlay();
        time.World.WaterAmount = 10f;
        prophet.OnTimeAdvanced();
        time.StartAutoPlay();
        prophet.OnTimeAdvanced();

        var result = prophet.PronouncePair(0, 1);

        Assert.False(result.ResumedAutoPlay);
        Assert.Equal(TimeControlState.AutoPlaying, time.State);
    }

    [Fact]
    [Trait("TS", "TS-prophet-request-009")]
    public void PronouncePair_止まっているときに依頼が出た_神託の後も止まったまま()
    {
        var (prophet, time) = Create();
        time.World.WaterAmount = 10f;
        prophet.OnTimeAdvanced();

        var result = prophet.PronouncePair(0, 1);

        Assert.False(result.ResumedAutoPlay);
        Assert.Equal(TimeControlState.Stopped, time.State);
    }

    [Fact]
    [Trait("TS", "TS-prophet-request-001")]
    public void OnTimeAdvanced_水資源が30になる_渇きの依頼が出る()
    {
        var (prophet, time) = Create();
        SetYear(time, 10);
        time.World.WaterAmount = 30f;

        var update = prophet.OnTimeAdvanced();

        Assert.Equal("THIRST", update.NewRequest?.Id);
        Assert.Equal("SYM_WATER", prophet.ActiveRequest?.SymbolId);
    }

    [Fact]
    [Trait("TS", "TS-prophet-request-001")]
    public void OnTimeAdvanced_調和度が20になる_荒れの依頼で大地を示す()
    {
        var (prophet, time) = Create();
        SetYear(time, 10);
        time.World.HarmonyLevel = 20f;

        var update = prophet.OnTimeAdvanced();

        Assert.Equal("UNREST", update.NewRequest?.Id);
        Assert.Equal("SYM_EARTH", prophet.ActiveRequest?.SymbolId);
    }

    [Fact]
    [Trait("TS", "TS-prophet-request-002")]
    public void OnTimeAdvanced_渇きと混乱の両方に当てはまる_優先の渇きが出る()
    {
        var (prophet, time) = Create();
        time.World.WaterAmount = 10f;
        time.World.ChaosLevel = 80f;

        var update = prophet.OnTimeAdvanced();

        Assert.Equal("THIRST", update.NewRequest?.Id);
    }

    [Fact]
    [Trait("TS", "TS-prophet-request-003")]
    public void PronouncePair_渇きの依頼中_依頼が済む()
    {
        var (prophet, time) = Create();
        time.World.WaterAmount = 10f;
        prophet.OnTimeAdvanced();

        var result = prophet.PronouncePair(0, 1);

        Assert.Equal("THIRST", result.AnsweredRequest?.Id);
        Assert.Null(prophet.ActiveRequest);
    }

    [Fact]
    [Trait("TS", "TS-prophet-request-003")]
    public void OnTimeAdvanced_依頼中に水資源が戻る_依頼は残る()
    {
        var (prophet, time) = Create();
        time.World.WaterAmount = 10f;
        prophet.OnTimeAdvanced();

        time.World.WaterAmount = 80f;
        SetYear(time, 1);
        var update = prophet.OnTimeAdvanced();

        Assert.Null(update.NewRequest);
        Assert.Equal("THIRST", prophet.ActiveRequest?.Id);
    }

    [Fact]
    [Trait("TS", "TS-prophet-request-004")]
    public void OnTimeAdvanced_年100で渇きが済んだ_年149では出ず年150で出る()
    {
        var (prophet, time) = Create();
        time.World.WaterAmount = 10f;
        SetYear(time, 100);
        prophet.OnTimeAdvanced();
        prophet.PronouncePair(0, 1);

        SetYear(time, 149);
        var at149 = prophet.OnTimeAdvanced();
        SetYear(time, 150);
        var at150 = prophet.OnTimeAdvanced();

        Assert.Null(at149.NewRequest);
        Assert.Equal("THIRST", at150.NewRequest?.Id);
    }

    [Fact]
    [Trait("TS", "TS-prophet-request-004")]
    public void OnTimeAdvanced_年100で渇きが済み飢えに当てはまる_飢えの依頼が出る()
    {
        var (prophet, time) = Create();
        time.World.WaterAmount = 10f;
        SetYear(time, 100);
        prophet.OnTimeAdvanced();
        prophet.PronouncePair(0, 1);

        time.World.ForestAmount = 20f;
        SetYear(time, 101);
        var update = prophet.OnTimeAdvanced();

        Assert.Equal("FAMINE", update.NewRequest?.Id);
    }

    [Fact]
    [Trait("TS", "TS-prophet-request-005")]
    public void OnTimeAdvanced_崩壊している_依頼は出ない()
    {
        var (prophet, time) = Create();
        time.World.WaterAmount = 0f;
        time.World.IsDestroyed = true;

        var update = prophet.OnTimeAdvanced();

        Assert.Null(update.NewRequest);
        Assert.Null(prophet.ActiveRequest);
    }

    [Fact]
    [Trait("TS", "TS-prophet-request-005")]
    public void Reset_依頼中で待つ年数の中の困りごとがある_依頼も待つ年数も消える()
    {
        var (prophet, time) = Create();
        time.World.WaterAmount = 10f;
        prophet.OnTimeAdvanced();
        prophet.PronouncePair(0, 1);
        time.World.ForestAmount = 10f;
        SetYear(time, 1);
        prophet.OnTimeAdvanced();

        time.Reset();
        prophet.Reset();
        time.World.WaterAmount = 10f;
        var update = prophet.OnTimeAdvanced();

        Assert.Equal("THIRST", update.NewRequest?.Id);
    }
}
