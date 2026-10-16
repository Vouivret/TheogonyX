using System.Text.Json;
using TheogonyX.Core;

namespace TheogonyX.Core.Tests;

public class OraclePrayerTests
{
    private const int Seed = 42;

    private static readonly DialectRules TwoWords = OracleDialectTests.Rules with { PrayerMinWords = 2, PrayerMaxWords = 2 };

    // 手札を決めて作る（山札・捨札は空）。祈りに入れる語を選ぶのに必要な部分だけ
    private static (ProphetDreams Prophet, TimeControl Time, DialectState Dialect) Create(string[] hand, DialectRules? rules = null)
    {
        var time = new TimeControl(
            TestRules.World, TestRules.JumpYears, new CivilizationSetup(TestRules.Civilization, TestRules.Terrain, Seed, TestRules.Needs), TestRules.History);
        var deck = new OracleDeck(Seed, TestRules.Deck);
        deck.Restore(new OracleDeckState([], hand, [], 0, 0, -1, 0));
        var prophet = new ProphetDreams(time, TestRules.ProphetDreams, deck, TestRules.Oracle);
        var dialect = new DialectState(rules ?? TwoWords, OracleDialectTests.Silent, Seed);
        prophet.AttachDialect(dialect);
        return (prophet, time, dialect);
    }

    // 水が足りない星にして、最初の依頼（渇き）を出させる
    private static ProphetUpdate Thirst(ProphetDreams prophet, TimeControl time)
    {
        time.World.WaterAmount = 10f;
        return prophet.OnTimeAdvanced();
    }

    [Fact]
    [Trait("TS", "TS-oracle-prayer-001")]
    public void OnTimeAdvanced_依頼が出る_手札の記号の名詞だけで祈り方言帳に知っている言葉として残る()
    {
        var (prophet, time, dialect) = Create(["SYM_TREE", "SYM_WATER", "SYM_TREE"]);

        var prayer = Thirst(prophet, time).Prayer;

        Assert.Equal("THIRST", prayer!.Request!.Id);
        Assert.Equal(["NOUN_FOREST", "NOUN_RIVER"], prayer.NounIds.Order());
        Assert.Equal(
            [new DialectEntry("NOUN_FOREST", null, DialectReason.Known), new DialectEntry("NOUN_RIVER", null, DialectReason.Known)],
            dialect.Book.Entries.OrderBy(entry => entry.GodWord));
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

        Assert.Equal(["NOUN_FOREST", "NOUN_RIVER"], first.NounIds.Order());
        Assert.Contains("NOUN_FLAME", second.NounIds);
    }

    [Fact]
    [Trait("TS", "TS-oracle-prayer-003")]
    public void OnTimeAdvanced_手札に汚染ルーン_その記号の語が祈りに混じる()
    {
        var (prophet, time, _) = Create(["SYM_TREE", "RUNE_MADNESS", "SYM_WATER"]);

        var prayer = Thirst(prophet, time).Prayer!;

        Assert.Contains("NOUN_BLOOD", prayer.NounIds);
        Assert.Equal(2, prayer.NounIds.Count);
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
        Assert.Equal(2, due.Prayer.NounIds.Count);
        Assert.Null(afterward.Prayer);
        Assert.False(due.PausedAutoPlay);
    }

    [Fact]
    [Trait("TS", "TS-oracle-prayer-005")]
    public void OnTimeAdvanced_同じシードと同じ手札_同じ祈りの語になる()
    {
        string[] hand = ["SYM_TREE", "SYM_WATER", "SYM_FIRE", "SYM_TREE"];
        var rules = OracleDialectTests.Rules with { PrayerMinWords = 2, PrayerMaxWords = 3 };

        var (firstProphet, firstTime, _) = Create(hand, rules);
        var (secondProphet, secondTime, _) = Create(hand, rules);

        Assert.Equal(Thirst(firstProphet, firstTime).Prayer!.NounIds, Thirst(secondProphet, secondTime).Prayer!.NounIds);
    }

    [Fact]
    [Trait("TS", "TS-oracle-prayer-006")]
    public void Restore_次の祈りの年と知っている言葉_セーブで戻り定義に無い語は拒む()
    {
        var (prophet, time, dialect) = Create(["SYM_TREE", "SYM_WATER"]);
        Thirst(prophet, time);
        var save = dialect.Capture();
        var (_, _, target) = Create(["SYM_TREE"]);

        target.Restore(save);

        Assert.Equal(save.NextPrayerYear, target.NextPrayerYear);
        Assert.Equal(dialect.Book.Entries, target.Book.Entries);
        Assert.Throws<ArgumentException>(() => target.Restore(save with { Book = [new DialectEntry("NOUN_UNKNOWN", null, DialectReason.Known)] }));
        Assert.Throws<ArgumentException>(() => target.Restore(save with { NextPrayerYear = -1 }));
    }

    [Fact]
    [Trait("TS", "TS-oracle-prayer-007")]
    public void ParseResponse_祈りの文_入れる名詞が入っていなければ使わない()
    {
        static string Response(string inner) => JsonSerializer.Serialize(new { model = "gemma2:9b", response = inner, done = true });
        var good = Response("""{"prayer": "神よ、森は痩せ、川も細りました"}""");
        var missing = Response("""{"prayer": "神よ、森は痩せました"}""");
        var tooLong = Response(JsonSerializer.Serialize(new { prayer = "森川" + new string('あ', PrayerWriter.MaxPrayerLength) }));

        Assert.Equal("神よ、森は痩せ、川も細りました", PrayerWriter.ParseResponse(good, ["森", "川"]));
        Assert.Null(PrayerWriter.ParseResponse(missing, ["森", "川"]));
        Assert.Null(PrayerWriter.ParseResponse(tooLong, ["森", "川"]));
        Assert.Contains("使う名詞: 森, 川", PrayerWriter.BuildPrompt("渇き", ["森", "川"], "ja"));
        Assert.Contains("困りごとは無い", PrayerWriter.BuildPrompt(null, ["森", "川"], "ja"));
    }
}
