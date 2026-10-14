using TheogonyX.Core;

namespace TheogonyX.Core.Tests;

// 世界再創生のときの精査（ルーンの削除。docs/domain/oracle-deck.md の「再創生」）。
public class OraclePruneTests
{
    private const int Seed = 42;

    private static (ProphetDreams Prophet, TimeControl Time, OracleDeck Deck) Create()
    {
        var time = new TimeControl(
            TestRules.World, TestRules.JumpYears, new CivilizationSetup(TestRules.Civilization, TestRules.Terrain, Seed, TestRules.Needs), TestRules.History);
        var deck = new OracleDeck(Seed, TestRules.Deck);
        return (new ProphetDreams(time, TestRules.Dream, TestRules.ProphetDreams, deck, TestRules.Oracle), time, deck);
    }

    private static void SetDeck(OracleDeck deck, string[] drawPile, string[] hand, string[] discardPile) =>
        deck.Restore(new OracleDeckState(drawPile, hand, discardPile, ShuffleCount: 1));

    [Fact]
    [Trait("TS", "TS-oracle-deck-014")]
    public void Reset_世界再創生_持ち分が2回になりデッキは変わらない()
    {
        var (prophet, time, deck) = Create();
        Assert.Equal(0, prophet.PruneCharges);
        var before = deck.Capture();

        time.Reset();
        prophet.Reset();

        Assert.Equal(2, prophet.PruneCharges);
        Assert.Equal(before.DrawPile, deck.DrawPile);
        Assert.Equal(before.Hand, deck.Hand);
        Assert.Equal(before.DiscardPile, deck.DiscardPile);
    }

    [Fact]
    [Trait("TS", "TS-oracle-deck-014")]
    public void Prune_捨札の汚染ルーン_1枚消えて持ち分が減り手札は変わらない()
    {
        var (prophet, time, deck) = Create();
        SetDeck(deck, ["SYM_RAIN", "SYM_FIRE"], ["SYM_TREE", "SYM_BLOOD", "SYM_RAIN", "SYM_FIRE"], ["RUNE_MADNESS"]);
        time.Reset();
        prophet.Reset();

        prophet.Prune("RUNE_MADNESS");

        Assert.Empty(deck.DiscardPile);
        Assert.Equal(["SYM_TREE", "SYM_BLOOD", "SYM_RAIN", "SYM_FIRE"], deck.Hand);
        Assert.Equal(1, prophet.PruneCharges);
    }

    [Fact]
    [Trait("TS", "TS-oracle-deck-014")]
    public void Prune_手札にだけある_消えた分を山札から補う()
    {
        var (prophet, time, deck) = Create();
        SetDeck(deck, ["SYM_RAIN", "SYM_FIRE"], ["SYM_TREE", "SYM_BLOOD", "SYM_BLOOD", "SYM_FIRE"], []);
        time.Reset();
        prophet.Reset();

        prophet.Prune("SYM_TREE");

        Assert.Equal(4, deck.Hand.Count);
        Assert.DoesNotContain("SYM_TREE", deck.Hand);
        Assert.Equal(["SYM_FIRE"], deck.DrawPile);
        Assert.Equal(1, prophet.PruneCharges);
    }

    [Fact]
    [Trait("TS", "TS-oracle-deck-014")]
    public void Prune_同じルーンが複数_捨札から先に消える()
    {
        var (prophet, time, deck) = Create();
        SetDeck(deck, ["SYM_TREE", "SYM_RAIN"], ["SYM_TREE", "SYM_BLOOD", "SYM_BLOOD", "SYM_FIRE"], ["SYM_TREE"]);
        time.Reset();
        prophet.Reset();

        prophet.Prune("SYM_TREE");

        Assert.Empty(deck.DiscardPile);
        Assert.Equal(["SYM_TREE", "SYM_RAIN"], deck.DrawPile);
        Assert.Equal(["SYM_TREE", "SYM_BLOOD", "SYM_BLOOD", "SYM_FIRE"], deck.Hand);
    }

    [Fact]
    [Trait("TS", "TS-oracle-deck-015")]
    public void PruneRejection_持ち分が無い_拒否され状態は変わらない()
    {
        var (prophet, _, deck) = Create();
        var before = deck.Capture();

        Assert.Equal("no_prune_charges", prophet.PruneRejection("SYM_TREE"));
        Assert.Throws<InvalidOperationException>(() => prophet.Prune("SYM_TREE"));

        Assert.Equal(before.Hand, deck.Hand);
        Assert.Equal(before.DrawPile, deck.DrawPile);
    }

    [Fact]
    [Trait("TS", "TS-oracle-deck-015")]
    public void PruneRejection_デッキに無いルーン_拒否される()
    {
        var (prophet, time, _) = Create();
        time.Reset();
        prophet.Reset();

        Assert.Equal("card_not_in_deck", prophet.PruneRejection("RUNE_MADNESS"));
        Assert.Equal(2, prophet.PruneCharges);
    }

    [Fact]
    [Trait("TS", "TS-oracle-deck-015")]
    public void PruneRejection_デッキが2枚_拒否される()
    {
        var (prophet, time, deck) = Create();
        SetDeck(deck, [], ["SYM_TREE", "SYM_RAIN"], []);
        time.Reset();
        prophet.Reset();

        Assert.Equal("deck_too_small", prophet.PruneRejection("SYM_TREE"));
        Assert.Equal(2, deck.Hand.Count);
    }

    [Fact]
    [Trait("TS", "TS-oracle-deck-014")]
    public void Prune_持ち分を使い切る_それ以上は消せない()
    {
        var (prophet, time, _) = Create();
        time.Reset();
        prophet.Reset();

        prophet.Prune("SYM_TREE");
        prophet.Prune("SYM_TREE");

        Assert.Equal(0, prophet.PruneCharges);
        Assert.Equal("no_prune_charges", prophet.PruneRejection("SYM_TREE"));
    }
}
