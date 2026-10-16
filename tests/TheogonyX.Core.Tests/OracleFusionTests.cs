using TheogonyX.Core;
using TheogonyX.Core.Persistence;

namespace TheogonyX.Core.Tests;

// 精査の融合（docs/domain/oracle-wuxing.md の「融合（比和）」）。同じ五行の基本ルーン 2 枚 → 上位のルーン 1 枚。
public class OracleFusionTests
{
    private const int Seed = 42;
    private const float Tolerance = 0.001f;

    private static (ProphetDreams Prophet, TimeControl Time, OracleDeck Deck) Create()
    {
        var time = new TimeControl(
            TestRules.World, TestRules.JumpYears, new CivilizationSetup(TestRules.Civilization, TestRules.Terrain, Seed, TestRules.Needs), TestRules.History);
        var deck = new OracleDeck(Seed, TestRules.Deck);
        return (new ProphetDreams(time, TestRules.ProphetDreams, deck, TestRules.OracleWuxing), time, deck);
    }

    // 世界再創生をして、精査の持ち分（2 回）を得た状態
    private static (ProphetDreams Prophet, TimeControl Time, OracleDeck Deck) CreateAfterReset()
    {
        var (prophet, time, deck) = Create();
        time.Reset();
        prophet.Reset();
        return (prophet, time, deck);
    }

    private static int Count(OracleDeck deck, string cardId) =>
        deck.DrawPile.Count(id => id == cardId) + deck.Hand.Count(id => id == cardId) + deck.DiscardPile.Count(id => id == cardId);

    [Fact]
    [Trait("TS", "TS-oracle-wuxing-010")]
    public void Fuse_木2枚_木が2枚減り大樹が捨札に加わって持ち分が減る()
    {
        var (prophet, _, deck) = CreateAfterReset();
        var total = deck.TotalCount;

        var greater = prophet.Fuse("SYM_TREE");

        Assert.Equal("RUNE_GREAT_TREE", greater.Id);
        Assert.Equal(1, Count(deck, "SYM_TREE"));
        Assert.Contains("RUNE_GREAT_TREE", deck.DiscardPile);
        Assert.Equal(total - 1, deck.TotalCount);
        Assert.Equal(1, prophet.PruneCharges);
        Assert.Equal(4, deck.Hand.Count);
    }

    [Theory]
    [Trait("TS", "TS-oracle-wuxing-011")]
    [InlineData("SYM_BLOOD")]
    [InlineData("RUNE_MADNESS")]
    [InlineData("RUNE_GREAT_TREE")]
    public void FuseRejection_基本ルーンでない_not_fusableで何も変わらない(string cardId)
    {
        var (prophet, _, deck) = CreateAfterReset();
        var before = deck.Capture();

        Assert.Equal("not_fusable", prophet.FuseRejection(cardId));
        Assert.Throws<InvalidOperationException>(() => prophet.Fuse(cardId));

        Assert.Equal(before.Hand, deck.Hand);
        Assert.Equal(before.DrawPile, deck.DrawPile);
        Assert.Equal(before.DiscardPile, deck.DiscardPile);
        Assert.Equal(2, prophet.PruneCharges);
    }

    [Fact]
    [Trait("TS", "TS-oracle-wuxing-011")]
    public void FuseRejection_持ち分が無い_no_prune_charges()
    {
        var (prophet, _, _) = Create();

        Assert.Equal("no_prune_charges", prophet.FuseRejection("SYM_TREE"));
    }

    [Fact]
    [Trait("TS", "TS-oracle-wuxing-011")]
    public void FuseRejection_木が1枚だけ_card_not_in_deck()
    {
        var (prophet, _, deck) = CreateAfterReset();
        deck.Restore(new OracleDeckState(["SYM_FIRE", "SYM_FIRE"], ["SYM_TREE", "SYM_WATER", "SYM_EARTH", "SYM_METAL"], [], ShuffleCount: 1, PruneCharges: 2));

        Assert.Equal("card_not_in_deck", prophet.FuseRejection("SYM_TREE"));
    }

    [Fact]
    [Trait("TS", "TS-oracle-wuxing-011")]
    public void FuseRejection_デッキが木2枚だけ_deck_too_small()
    {
        var (prophet, _, deck) = CreateAfterReset();
        deck.Restore(new OracleDeckState([], ["SYM_TREE", "SYM_TREE"], [], ShuffleCount: 1, PruneCharges: 2));

        Assert.Equal("deck_too_small", prophet.FuseRejection("SYM_TREE"));
    }

    [Fact]
    [Trait("TS", "TS-oracle-wuxing-012")]
    public void Validate_上位のルーンの定義が正しくない_例外になる()
    {
        var symbols = new HashSet<string> { "SYM_TREE", "SYM_FIRE", "SYM_EARTH", "SYM_METAL", "SYM_WATER", "SYM_BLOOD" };
        var rules = TestRules.OracleWuxing;

        void Check(params GreaterRune[] runes) => (rules with { GreaterRunes = runes }).Validate(symbols);

        Check(new GreaterRune("RUNE_GREAT_TREE", "SYM_TREE", 1.0f));
        Assert.Throws<ArgumentException>(() => Check(new GreaterRune("RUNE_GREAT_BLOOD", "SYM_BLOOD", 1.0f)));
        Assert.Throws<ArgumentException>(() => Check(new GreaterRune("SYM_FIRE", "SYM_TREE", 1.0f)));
        Assert.Throws<ArgumentException>(() => Check(new GreaterRune("RUNE_MADNESS", "SYM_TREE", 1.0f)));
        Assert.Throws<ArgumentException>(() => Check(new GreaterRune("RUNE_A", "SYM_TREE", 1.0f), new GreaterRune("RUNE_B", "SYM_TREE", 1.0f)));
        Assert.Throws<ArgumentException>(() => Check(new GreaterRune("RUNE_A", "SYM_TREE", 1.0f), new GreaterRune("RUNE_A", "SYM_FIRE", 1.0f)));
        Assert.Throws<ArgumentException>(() => Check(new GreaterRune("RUNE_GREAT_TREE", "SYM_TREE", 1.1f)));
    }

    [Fact]
    [Trait("TS", "TS-oracle-wuxing-013")]
    public void RoundTrip_上位のルーンを含むデッキ_保存して読み込むと戻り定義が無ければ読めない()
    {
        var (prophet, time, deck) = CreateAfterReset();
        prophet.Fuse("SYM_TREE");
        var clock = new AutoPlayClock(new Dictionary<AutoPlaySpeed, double>
        {
            [AutoPlaySpeed.Normal] = 1.0,
            [AutoPlaySpeed.Fast] = 0.1,
            [AutoPlaySpeed.RealTime] = 86400.0,
        });
        var json = SaveDataSerializer.Write(SaveGame.Capture(time, prophet, Seed, clock));

        Assert.True(
            SaveDataSerializer.TryParse(
                json, TestRules.Dream, TestRules.ProphetDreams, TestRules.Civilization, TestRules.Needs, TestRules.OracleWuxing, out var data, out var reason),
            reason);
        Assert.Equal(deck.DiscardPile, data!.Deck.DiscardPile);

        var ok = SaveDataSerializer.TryParse(
            json, TestRules.Dream, TestRules.ProphetDreams, TestRules.Civilization, TestRules.Needs,
            TestRules.OracleWuxing with { GreaterRunes = null }, out _, out var rejected);
        Assert.False(ok);
        Assert.Equal("invalid_deck", rejected);
    }
}
