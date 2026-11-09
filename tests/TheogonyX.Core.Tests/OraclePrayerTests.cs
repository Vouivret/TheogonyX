using System.Text.Json;
using TheogonyX.Core;

namespace TheogonyX.Core.Tests;

public class OraclePrayerTests
{
    private const int Seed = 42;

    private static readonly ScriptRules TwoWords = ScriptTests.Rules with { PrayerMinWords = 2, PrayerMaxWords = 2 };

    // 手札を決めて作る（山札・捨札は空）。祈りに入れる記号を選ぶのに必要な部分だけ
    private static (ProphetDreams Prophet, TimeControl Time, ScriptState Script) Create(string[] hand, ScriptRules? rules = null)
    {
        var time = new TimeControl(
            TestRules.World, TestRules.JumpYears, new CivilizationSetup(TestRules.Civilization, TestRules.Terrain, Seed, TestRules.Needs), TestRules.History);
        var deck = new OracleDeck(Seed, TestRules.Deck);
        deck.Restore(new OracleDeckState([], hand, [], 0, 0, -1, 0));
        var prophet = new ProphetDreams(time, TestRules.ProphetDreams, deck, TestRules.Oracle);
        var scriptRules = rules ?? TwoWords;
        var script = new ScriptState(
            scriptRules, PlanetScript.Create(scriptRules, ScriptTests.Symbols, Seed), TestRules.Civilization.Regions, [], 0.3f);
        prophet.AttachScript(script, Seed);
        return (prophet, time, script);
    }

    // 水が足りない星にして、最初の依頼（渇き）を出させる
    private static ProphetUpdate Thirst(ProphetDreams prophet, TimeControl time)
    {
        time.World.WaterAmount = 10f;
        return prophet.OnTimeAdvanced();
    }

    [Fact]
    [Trait("TS", "TS-oracle-prayer-001")]
    public void OnTimeAdvanced_依頼が出る_手札の記号だけで祈る()
    {
        var (prophet, time, _) = Create(["SYM_TREE", "SYM_WATER", "SYM_TREE"]);

        var prayer = Thirst(prophet, time).Prayer;

        Assert.Equal("THIRST", prayer!.Request!.Id);
        Assert.Equal(["SYM_TREE", "SYM_WATER"], prayer.SymbolIds.Order());
    }

    [Fact]
    [Trait("TS", "TS-oracle-prayer-002")]
    public void OnTimeAdvanced_新しい記号が手札に入った_次の祈りに必ず入る()
    {
        var (prophet, time, _) = Create(["SYM_TREE", "SYM_WATER", "SYM_FIRE"]);

        prophet.OnTimeAdvanced();
        time.World.Year = 20;
        var first = prophet.OnTimeAdvanced().Prayer!;
        // 最初の祈りで入れなかった記号が、次の祈りに入る（手札は変わらない）
        time.World.Year = 40;
        var second = prophet.OnTimeAdvanced().Prayer!;

        Assert.Equal(["SYM_TREE", "SYM_WATER"], first.SymbolIds.Order());
        Assert.Contains("SYM_FIRE", second.SymbolIds);
    }

    [Fact]
    [Trait("TS", "TS-oracle-prayer-003")]
    public void OnTimeAdvanced_手札に汚染ルーン_その記号が祈りに混じる()
    {
        var (prophet, time, _) = Create(["SYM_TREE", "RUNE_MADNESS", "SYM_WATER"]);

        var prayer = Thirst(prophet, time).Prayer!;

        Assert.Contains("SYM_BLOOD", prayer.SymbolIds);
        Assert.Equal(2, prayer.SymbolIds.Count);
    }

    [Fact]
    [Trait("TS", "TS-oracle-prayer-004")]
    public void OnTimeAdvanced_依頼の無い星_20年たつと日々の祈りをささやきそれまでは祈らない()
    {
        var (prophet, time, _) = Create(["SYM_TREE", "SYM_WATER"]);

        var start = prophet.OnTimeAdvanced();
        time.World.Year = 19;
        var early = prophet.OnTimeAdvanced();
        time.World.Year = 20;
        var due = prophet.OnTimeAdvanced();
        time.World.Year = 39;
        var afterward = prophet.OnTimeAdvanced();

        Assert.Null(start.Prayer);
        Assert.Null(early.Prayer);
        Assert.Null(due.Prayer!.Request);
        Assert.Equal(2, due.Prayer.SymbolIds.Count);
        Assert.Null(afterward.Prayer);
        Assert.False(due.PausedAutoPlay);
    }

    [Fact]
    [Trait("TS", "TS-oracle-prayer-005")]
    public void OnTimeAdvanced_同じシードと同じ手札_同じ祈りの記号になる()
    {
        string[] hand = ["SYM_TREE", "SYM_WATER", "SYM_FIRE", "SYM_TREE"];
        var rules = ScriptTests.Rules with { PrayerMinWords = 2, PrayerMaxWords = 3 };

        var (firstProphet, firstTime, _) = Create(hand, rules);
        var (secondProphet, secondTime, _) = Create(hand, rules);

        Assert.Equal(Thirst(firstProphet, firstTime).Prayer!.SymbolIds, Thirst(secondProphet, secondTime).Prayer!.SymbolIds);
    }

    [Fact]
    [Trait("TS", "TS-oracle-prayer-006")]
    public void Restore_次の祈りの年_セーブで戻り負の年は拒む()
    {
        var (prophet, time, script) = Create(["SYM_TREE", "SYM_WATER"]);
        Thirst(prophet, time);
        var save = script.Capture();
        var (_, _, target) = Create(["SYM_TREE"]);

        target.Restore(save);

        Assert.Equal(20, save.NextPrayerYear);
        Assert.Equal(save.NextPrayerYear, target.NextPrayerYear);
        Assert.Throws<ArgumentException>(() => target.Restore(save with { NextPrayerYear = -1 }));
    }

    [Fact]
    [Trait("TS", "TS-oracle-prayer-009")]
    public void ReadObject_引用符が崩れたJSON_文字列の項目を拾って読める()
    {
        static string Response(string inner) => JsonSerializer.Serialize(new { model = "gemma2:9b", response = inner, done = true });
        static string? Prayer(string body) => AiText.ReadObject(body, root =>
            root.TryGetProperty("prayer", out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null);
        var broken = Response("{\"prayer\": \"森の実り、川の恵み。渇きを癒したまえ。」} Praying that the forest and the river");

        Assert.Equal("森の実り、川の恵み。渇きを癒したまえ。", Prayer(broken));
        Assert.Null(Prayer(Response("これはJSONではない")));
    }
}
