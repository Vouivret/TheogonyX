using TheogonyX.Core;
using TheogonyX.Core.Persistence;

namespace TheogonyX.Core.Tests;

public class OracleDeckTests
{
    private const int Seed = 42;

    private static readonly DreamNode[] TreeRainFire = [new("SYM_TREE", 0.5f), new("SYM_RAIN", 0.6f), new("SYM_FIRE", 0.4f)];

    // 山札・手札・捨札の並びと、使ったカード・シャッフルの回数が同じか
    private static void AssertSameDeck(OracleDeckState expected, OracleDeckState actual)
    {
        Assert.Equal(expected.DrawPile, actual.DrawPile);
        Assert.Equal(expected.Hand, actual.Hand);
        Assert.Equal(expected.DiscardPile, actual.DiscardPile);
        Assert.Equal(expected.UsedSymbolId, actual.UsedSymbolId);
        Assert.Equal(expected.ShuffleCount, actual.ShuffleCount);
    }

    private static DeckRules Rules(params (string SymbolId, int Count)[] runes) =>
        new([.. runes.Select(rune => new DeckRuneCount(rune.SymbolId, rune.Count))], HandSize: 4);

    // 夢は常に木・雨・火。手札から選べる記号は、デッキの手札に入っているものだけ
    private static (ProphetDreams Prophet, TimeControl Time, OracleDeck Deck) CreateGame(int seed = Seed, DeckRules? rules = null)
    {
        var time = new TimeControl(
            TestRules.World, TestRules.JumpYears, new CivilizationSetup(TestRules.Civilization, TestRules.Terrain, seed, TestRules.Needs), TestRules.History);
        var deck = new OracleDeck(seed, rules ?? TestRules.Deck);
        var prophet = new ProphetDreams(time, TestRules.Dream, TestRules.ProphetDreams, () => TreeRainFire, deck);
        return (prophet, time, deck);
    }

    [Fact]
    [Trait("TS", "TS-oracle-deck-001")]
    public void Constructor_最初のデッキ_12枚のうち4枚が手札で8枚が山札()
    {
        var deck = new OracleDeck(Seed, TestRules.Deck);

        Assert.Equal(4, deck.Hand.Count);
        Assert.Equal(8, deck.DrawPile.Count);
        Assert.Empty(deck.DiscardPile);
        var all = deck.Hand.Concat(deck.DrawPile).OrderBy(id => id).ToList();
        Assert.Equal(
            [.. new[] { "SYM_BLOOD", "SYM_FIRE", "SYM_RAIN", "SYM_TREE" }.SelectMany(id => Enumerable.Repeat(id, 3)).OrderBy(id => id)],
            all);
    }

    [Fact]
    [Trait("TS", "TS-oracle-deck-001")]
    public void Constructor_同じシード_同じ手札と山札になり_別のシードでは並びが変わる()
    {
        var a = new OracleDeck(Seed, TestRules.Deck);
        var b = new OracleDeck(Seed, TestRules.Deck);
        var others = Enumerable.Range(1, 5).Select(seed => new OracleDeck(seed, TestRules.Deck)).ToList();

        Assert.Equal(a.Hand, b.Hand);
        Assert.Equal(a.DrawPile, b.DrawPile);
        Assert.Contains(others, other => !other.DrawPile.SequenceEqual(a.DrawPile));
    }

    [Fact]
    [Trait("TS", "TS-oracle-deck-002")]
    public void Settle_改ざんして神託を下す_使ったカードが捨札に移り1枚補う()
    {
        var (prophet, _, deck) = CreateGame();
        var used = deck.Hand[0];
        var drawn = deck.DrawPile[0];

        prophet.Tamper(0, used);
        Assert.Equal(4, deck.Hand.Count); // 神託を下すまでは手札から動かない
        prophet.Pronounce();

        Assert.Equal(4, deck.Hand.Count);
        Assert.Equal([used], deck.DiscardPile);
        Assert.Equal(7, deck.DrawPile.Count);
        Assert.Contains(drawn, deck.Hand);
        Assert.Null(deck.UsedSymbolId);
    }

    [Fact]
    [Trait("TS", "TS-oracle-deck-002")]
    public void Settle_改ざんせずに神託を下す_何も動かない()
    {
        var (prophet, _, deck) = CreateGame();
        var before = deck.Capture();

        prophet.Pronounce();

        AssertSameDeck(before, deck.Capture());
    }

    [Fact]
    [Trait("TS", "TS-oracle-deck-003")]
    public void Settle_山札が尽きている_捨札をシャッフルして戻して引く()
    {
        // 4 枚のデッキ: 手札 4 枚で山札は空。使ったカードが捨札に入ってから 1 枚引くので、そのカードが手札に戻る
        var rules = Rules(("SYM_TREE", 1), ("SYM_RAIN", 1), ("SYM_BLOOD", 1), ("SYM_FIRE", 1));
        var (prophet, _, deck) = CreateGame(rules: rules);
        var used = deck.Hand[0];
        Assert.Empty(deck.DrawPile);
        var shuffles = deck.ShuffleCount;

        prophet.Tamper(0, used);
        prophet.Pronounce();

        Assert.Equal(4, deck.Hand.Count);
        Assert.Empty(deck.DiscardPile);
        Assert.Empty(deck.DrawPile);
        Assert.Equal(shuffles + 1, deck.ShuffleCount);
        Assert.Equal(["SYM_BLOOD", "SYM_FIRE", "SYM_RAIN", "SYM_TREE"], [.. deck.Hand.OrderBy(id => id)]);
    }

    [Fact]
    [Trait("TS", "TS-oracle-deck-003")]
    public void Settle_山札も捨札も空_引けるだけ引く()
    {
        var rules = Rules(("SYM_TREE", 1), ("SYM_RAIN", 1), ("SYM_BLOOD", 1), ("SYM_FIRE", 1));
        var deck = new OracleDeck(Seed, new DeckRules(rules.InitialRunes, HandSize: 6));

        Assert.Equal(4, deck.Hand.Count);
        Assert.Empty(deck.DrawPile);
    }

    [Fact]
    [Trait("TS", "TS-oracle-deck-004")]
    public void Settle_同じシードと同じ操作を繰り返す_引く順が一致する()
    {
        List<string> Play()
        {
            var (prophet, time, deck) = CreateGame();
            var seen = new List<string>(deck.Hand);
            for (var round = 0; round < 20; round++)
            {
                prophet.Tamper(0, deck.Hand[0]);
                prophet.Pronounce();
                seen.AddRange(deck.Hand);
                // 次の神託の夢を見るために年を進める
                time.World.Year += 100;
                prophet.OnTimeAdvanced();
            }
            return seen;
        }

        var first = Play();
        var second = Play();

        Assert.Equal(first, second);
        Assert.Equal(4 * 21, first.Count);
    }

    [Fact]
    [Trait("TS", "TS-oracle-deck-005")]
    public void Tamper_手札に無い記号_受け付けない()
    {
        var rules = Rules(("SYM_TREE", 4));
        var (prophet, _, deck) = CreateGame(rules: rules);

        Assert.DoesNotContain("SYM_BLOOD", deck.Hand);
        Assert.Throws<ArgumentException>(() => prophet.Tamper(0, "SYM_BLOOD"));
        Assert.Equal(ProphetDreamState.Untampered, prophet.State);
        Assert.Equal(TreeRainFire, prophet.Dream);
    }

    [Fact]
    [Trait("TS", "TS-oracle-deck-005")]
    public void SubmitTamper_手札に無い記号_card_not_in_handで捨てる()
    {
        var (prophet, time, _) = CreateGame(rules: Rules(("SYM_TREE", 4)));
        var queue = new OracleCommandQueue(prophet, time, TestRules.Dream);

        var outcome = queue.SubmitTamper(1, "SYM_BLOOD");

        Assert.Equal("card_not_in_hand", outcome.RejectedReason);
        Assert.Equal(TreeRainFire, prophet.Dream);
    }

    [Fact]
    [Trait("TS", "TS-oracle-deck-006")]
    public void Reset_改ざん済みで世界再創生_使ったカードは手札に残る()
    {
        var (prophet, time, deck) = CreateGame();
        var used = deck.Hand[0];
        prophet.Tamper(0, used);
        var hand = deck.Hand.ToList();

        time.Reset();
        prophet.Reset();

        Assert.Equal(hand, deck.Hand);
        Assert.Null(deck.UsedSymbolId);
        Assert.Empty(deck.DiscardPile);
        Assert.Equal(ProphetDreamState.Untampered, prophet.State);
    }

    [Fact]
    [Trait("TS", "TS-oracle-deck-007")]
    public void RoundTrip_デッキを持つセーブ_山札と手札と捨札と使ったカードが戻る()
    {
        var (prophet, time, deck) = CreateGame();
        prophet.Tamper(0, deck.Hand[0]);
        prophet.Pronounce();
        time.World.Year += 100;
        prophet.OnTimeAdvanced();
        prophet.Tamper(1, deck.Hand[0]); // 改ざん済みで保存する（使ったカードが手札に残る）
        var dreams = new DreamSource(Seed, TestRules.Dream, TestRules.ProphetDreams);
        var clock = new AutoPlayClock(new Dictionary<AutoPlaySpeed, double> { [AutoPlaySpeed.Normal] = 1.0, [AutoPlaySpeed.Fast] = 0.1, [AutoPlaySpeed.RealTime] = 86400.0 });
        var expected = deck.Capture();

        var json = SaveDataSerializer.Write(SaveGame.Capture(time, prophet, dreams, clock));
        Assert.True(SaveDataSerializer.TryParse(json, TestRules.Dream, TestRules.ProphetDreams, TestRules.Civilization, TestRules.Needs, out var data, out var reason), reason);
        var (loadedProphet, loadedTime, loadedDeck) = CreateGame();
        SaveGame.Apply(data!, loadedTime, loadedProphet, new DreamSource(Seed, TestRules.Dream, TestRules.ProphetDreams), clock);

        AssertSameDeck(expected, loadedDeck.Capture());
        Assert.NotNull(loadedDeck.UsedSymbolId);
        Assert.Equal(ProphetDreamState.Tampered, loadedProphet.State);
    }
}
