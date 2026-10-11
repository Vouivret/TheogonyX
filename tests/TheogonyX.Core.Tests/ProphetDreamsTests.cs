using TheogonyX.Core;

namespace TheogonyX.Core.Tests;

public class ProphetDreamsTests
{
    private const float Tolerance = 0.001f;

    private static readonly DreamNode[] TreeRainFire = [new("SYM_TREE", 0.5f), new("SYM_RAIN", 0.6f), new("SYM_FIRE", 0.4f)];
    private static readonly DreamNode[] TreeRainRain = [new("SYM_TREE", 0.5f), new("SYM_RAIN", 0.5f), new("SYM_RAIN", 0.5f)];

    // 夢を見るたびに、渡した夢を順に返す（最後の夢はくり返す）
    private static (ProphetDreams Dreams, TimeControl Time) Create(params DreamNode[][] dreams)
    {
        var time = new TimeControl(TestRules.World, TestRules.JumpYears);
        var next = 0;
        var prophet = new ProphetDreams(
            time,
            TestRules.Dream,
            TestRules.ProphetDreams,
            () => dreams[Math.Min(next++, dreams.Length - 1)]);
        return (prophet, time);
    }

    private static void SetYear(TimeControl time, int year) => time.World.Year = year;

    [Fact]
    [Trait("TS", "TS-dream-tampering-002")]
    public void Tamper_2番目を血に入れ替える_記号だけが替わり改ざん済みになり2回目はできない()
    {
        var (prophet, _) = Create(TreeRainFire);

        prophet.Tamper(1, "SYM_BLOOD");

        Assert.Equal([new DreamNode("SYM_TREE", 0.5f), new DreamNode("SYM_BLOOD", 0.6f), new DreamNode("SYM_FIRE", 0.4f)], prophet.Dream);
        Assert.Equal(ProphetDreamState.Tampered, prophet.State);
        Assert.False(prophet.CanTamper);
        Assert.Throws<InvalidOperationException>(() => prophet.Tamper(0, "SYM_FIRE"));
    }

    [Fact]
    [Trait("TS", "TS-dream-tampering-003")]
    public void Pronounce_改ざんした夢_ドグマが加わり夢なしになる()
    {
        var (prophet, time) = Create(TreeRainFire);
        prophet.Tamper(1, "SYM_BLOOD");

        var result = prophet.Pronounce();

        var dogma = Assert.Single(time.Dogmas);
        Assert.Equal("SYM_TREE", dogma.TargetSymbolId);
        Assert.Equal(DogmaType.Taboo, dogma.Type);
        Assert.Equal(0.74f, dogma.Intensity, Tolerance);
        Assert.Equal(dogma, result.Dogma);
        Assert.Equal(ProphetDreamState.None, prophet.State);
        Assert.Empty(prophet.Dream);
    }

    [Fact]
    [Trait("TS", "TS-dream-tampering-003")]
    public void Pronounce_改ざん前の夢_そのまま解釈したドグマが加わる()
    {
        var (prophet, time) = Create(TreeRainRain);

        prophet.Pronounce();

        var dogma = Assert.Single(time.Dogmas);
        Assert.Equal("SYM_RAIN", dogma.TargetSymbolId);
        Assert.Equal(DogmaType.Worship, dogma.Type);
        Assert.Equal(0.6f, dogma.Intensity, Tolerance);
    }

    [Fact]
    [Trait("TS", "TS-dream-tampering-004")]
    public void OnTimeAdvanced_神託から99年と100年_100年で新しい夢を見る()
    {
        var (prophet, time) = Create(TreeRainFire, TreeRainRain);
        prophet.Pronounce();

        SetYear(time, 99);
        var at99 = prophet.OnTimeAdvanced();
        SetYear(time, 100);
        var at100 = prophet.OnTimeAdvanced();

        Assert.False(at99.HasNewDream);
        Assert.True(at100.HasNewDream);
        Assert.Equal(TreeRainRain, prophet.Dream);
        Assert.Equal(ProphetDreamState.Untampered, prophet.State);
    }

    [Fact]
    [Trait("TS", "TS-dream-tampering-004")]
    public void OnTimeAdvanced_神託していない夢があり100年たった_夢は元のまま()
    {
        var (prophet, time) = Create(TreeRainFire, TreeRainRain);
        prophet.Tamper(1, "SYM_BLOOD");

        SetYear(time, 101);
        var update = prophet.OnTimeAdvanced();

        Assert.False(update.HasNewDream);
        Assert.Equal("SYM_BLOOD", prophet.Dream[1].SymbolId);
        Assert.Equal(ProphetDreamState.Tampered, prophet.State);
    }

    [Fact]
    [Trait("TS", "TS-dream-tampering-005")]
    public void TamperとPronounce_崩壊している_できない()
    {
        var (prophet, time) = Create(TreeRainFire);
        time.World.IsDestroyed = true;

        Assert.False(prophet.CanTamper);
        Assert.False(prophet.CanPronounce);
        Assert.Throws<InvalidOperationException>(() => prophet.Tamper(0, "SYM_BLOOD"));
        Assert.Throws<InvalidOperationException>(() => prophet.Pronounce());
        Assert.Empty(time.Dogmas);
    }

    [Fact]
    [Trait("TS", "TS-dream-tampering-006")]
    public void Reset_ドグマと改ざん済みの夢がある_ドグマが空で改ざん前の新しい夢になる()
    {
        var (prophet, time) = Create(TreeRainFire, TreeRainRain, TreeRainFire);
        prophet.Pronounce();
        SetYear(time, 100);
        prophet.OnTimeAdvanced();
        prophet.Tamper(0, "SYM_BLOOD");

        time.Reset();
        prophet.Reset();

        Assert.Empty(time.Dogmas);
        Assert.Equal(ProphetDreamState.Untampered, prophet.State);
        Assert.Equal(TreeRainFire, prophet.Dream);
    }

    [Fact]
    [Trait("TS", "TS-dream-tampering-007")]
    public void TamperとPronounce_状態遷移表で不可_例外になり何も変わらない()
    {
        var (prophet, time) = Create(TreeRainFire);
        prophet.Pronounce();
        var dogmaCount = time.Dogmas.Count;

        Assert.Throws<InvalidOperationException>(() => prophet.Tamper(0, "SYM_BLOOD"));
        Assert.Throws<InvalidOperationException>(() => prophet.Pronounce());
        Assert.Equal(ProphetDreamState.None, prophet.State);
        Assert.Empty(prophet.Dream);
        Assert.Equal(dogmaCount, time.Dogmas.Count);
    }

    [Fact]
    [Trait("TS", "TS-dream-tampering-009")]
    public void OnTimeAdvanced_自動進行中に100年たって夢を見る_自動進行が止まり神託で再開する()
    {
        var (prophet, time) = Create(TreeRainFire, TreeRainRain);
        prophet.Pronounce();
        time.StartAutoPlay();
        SetYear(time, 100);

        var update = prophet.OnTimeAdvanced();

        Assert.True(update.PausedAutoPlay);
        Assert.Equal(TimeControlState.Stopped, time.State);

        var result = prophet.Pronounce();

        Assert.True(result.ResumedAutoPlay);
        Assert.Equal(TimeControlState.AutoPlaying, time.State);
    }

    [Fact]
    [Trait("TS", "TS-dream-tampering-009")]
    public void Pronounce_止まっているときに夢を見た_神託の後も止まったまま()
    {
        var (prophet, time) = Create(TreeRainFire, TreeRainRain);
        prophet.Pronounce();
        SetYear(time, 100);
        prophet.OnTimeAdvanced();

        var result = prophet.Pronounce();

        Assert.False(result.ResumedAutoPlay);
        Assert.Equal(TimeControlState.Stopped, time.State);
    }

    [Fact]
    [Trait("TS", "TS-dream-tampering-009")]
    public void OnTimeAdvanced_夢を放って自動進行を再開した_神託でも状態は変わらない()
    {
        var (prophet, time) = Create(TreeRainFire, TreeRainRain);
        prophet.Pronounce();
        time.StartAutoPlay();
        SetYear(time, 100);
        prophet.OnTimeAdvanced();
        time.StartAutoPlay();
        SetYear(time, 101);
        prophet.OnTimeAdvanced();
        time.Pause();

        var result = prophet.Pronounce();

        Assert.False(result.ResumedAutoPlay);
        Assert.Equal(TimeControlState.Stopped, time.State);
    }

    [Fact]
    [Trait("TS", "TS-prophet-request-007")]
    public void OnTimeAdvanced_夢があり自動進行中に依頼が出る_自動進行が止まる()
    {
        var (prophet, time) = Create(TreeRainFire);
        time.StartAutoPlay();
        time.World.WaterAmount = 10f;

        var update = prophet.OnTimeAdvanced();

        Assert.False(update.HasNewDream);
        Assert.True(update.PausedAutoPlay);
        Assert.Equal(TimeControlState.Stopped, time.State);
    }

    [Fact]
    [Trait("TS", "TS-prophet-request-001")]
    public void OnTimeAdvanced_夢なしで水資源が30になる_渇きの依頼が出て新しい夢を見る()
    {
        var (prophet, time) = Create(TreeRainFire, TreeRainRain);
        prophet.Pronounce();
        SetYear(time, 10);
        time.World.WaterAmount = 30f;

        var update = prophet.OnTimeAdvanced();

        Assert.Equal("THIRST", update.NewRequest?.Id);
        Assert.Equal("SYM_RAIN", prophet.ActiveRequest?.SymbolId);
        Assert.True(update.HasNewDream);
        Assert.Equal(TreeRainRain, prophet.Dream);
    }

    [Fact]
    [Trait("TS", "TS-prophet-request-001")]
    public void OnTimeAdvanced_神託していない夢があり渇きに当てはまる_依頼が出て夢は元のまま()
    {
        var (prophet, time) = Create(TreeRainFire, TreeRainRain);
        time.World.WaterAmount = 20f;

        var update = prophet.OnTimeAdvanced();

        Assert.Equal("THIRST", update.NewRequest?.Id);
        Assert.False(update.HasNewDream);
        Assert.Equal(TreeRainFire, prophet.Dream);
    }

    [Fact]
    [Trait("TS", "TS-prophet-request-002")]
    public void OnTimeAdvanced_渇きと混乱の両方に当てはまる_優先の渇きが出る()
    {
        var (prophet, time) = Create(TreeRainFire);
        time.World.WaterAmount = 10f;
        time.World.ChaosLevel = 80f;

        var update = prophet.OnTimeAdvanced();

        Assert.Equal("THIRST", update.NewRequest?.Id);
    }

    [Fact]
    [Trait("TS", "TS-prophet-request-003")]
    public void Pronounce_渇きの依頼中_依頼が済む()
    {
        var (prophet, time) = Create(TreeRainFire);
        time.World.WaterAmount = 10f;
        prophet.OnTimeAdvanced();

        var result = prophet.Pronounce();

        Assert.Equal("THIRST", result.AnsweredRequest?.Id);
        Assert.Null(prophet.ActiveRequest);
    }

    [Fact]
    [Trait("TS", "TS-prophet-request-003")]
    public void OnTimeAdvanced_依頼中に水資源が戻る_依頼は残る()
    {
        var (prophet, time) = Create(TreeRainFire);
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
        var (prophet, time) = Create(TreeRainFire);
        time.World.WaterAmount = 10f;
        SetYear(time, 100);
        prophet.OnTimeAdvanced();
        prophet.Pronounce();

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
        var (prophet, time) = Create(TreeRainFire);
        time.World.WaterAmount = 10f;
        SetYear(time, 100);
        prophet.OnTimeAdvanced();
        prophet.Pronounce();

        time.World.ForestAmount = 20f;
        SetYear(time, 101);
        var update = prophet.OnTimeAdvanced();

        Assert.Equal("FAMINE", update.NewRequest?.Id);
    }

    [Fact]
    [Trait("TS", "TS-prophet-request-005")]
    public void OnTimeAdvanced_崩壊している_依頼は出ない()
    {
        var (prophet, time) = Create(TreeRainFire);
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
        var (prophet, time) = Create(TreeRainFire);
        time.World.WaterAmount = 10f;
        prophet.OnTimeAdvanced();
        prophet.Pronounce();
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
