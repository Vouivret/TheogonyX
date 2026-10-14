using TheogonyX.Core;

namespace TheogonyX.Core.Tests;

// 神託のデッキの山札・手札・捨札（docs/domain/oracle-deck.md）。2 枚で神託を下すときの動きは OraclePairTests。
public class OracleDeckTests
{
    private const int Seed = 42;

    // 山札・手札・捨札の並びとシャッフルの回数が同じか
    private static void AssertSameDeck(OracleDeckState expected, OracleDeckState actual)
    {
        Assert.Equal(expected.DrawPile, actual.DrawPile);
        Assert.Equal(expected.Hand, actual.Hand);
        Assert.Equal(expected.DiscardPile, actual.DiscardPile);
        Assert.Equal(expected.ShuffleCount, actual.ShuffleCount);
    }

    private static DeckRules Rules(int handSize, params (string SymbolId, int Count)[] runes) =>
        new([.. runes.Select(rune => new DeckRuneCount(rune.SymbolId, rune.Count))], handSize);

    [Fact]
    [Trait("TS", "TS-oracle-deck-001")]
    public void Constructor_最初のデッキ_15枚のうち4枚が手札で11枚が山札()
    {
        var deck = new OracleDeck(Seed, TestRules.Deck);

        Assert.Equal(4, deck.Hand.Count);
        Assert.Equal(11, deck.DrawPile.Count);
        Assert.Empty(deck.DiscardPile);
        var all = deck.Hand.Concat(deck.DrawPile).OrderBy(id => id).ToList();
        Assert.Equal(
            [.. new[] { "SYM_TREE", "SYM_FIRE", "SYM_EARTH", "SYM_METAL", "SYM_WATER" }.SelectMany(id => Enumerable.Repeat(id, 3)).OrderBy(id => id)],
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
    public void SpendPair_手札の2枚_捨札へ移り山札から2枚を補う()
    {
        var deck = new OracleDeck(Seed, TestRules.Deck);
        var first = deck.Hand[1];
        var second = deck.Hand[3];
        var nextTwo = deck.DrawPile.Take(2).ToList();

        deck.SpendPair(1, 3);

        Assert.Equal(4, deck.Hand.Count);
        Assert.Equal(9, deck.DrawPile.Count);
        Assert.Equal([first, second], deck.DiscardPile);
        Assert.Equal(nextTwo, deck.Hand.Skip(2).ToList());
    }

    [Theory]
    [Trait("TS", "TS-oracle-deck-002")]
    [InlineData(-1, 1)]
    [InlineData(0, 9)]
    [InlineData(2, 2)]
    public void SpendPair_番号が範囲外か同じ_例外で何も動かない(int first, int second)
    {
        var deck = new OracleDeck(Seed, TestRules.Deck);
        var before = deck.Capture();

        Assert.Throws<ArgumentException>(() => deck.SpendPair(first, second));

        AssertSameDeck(before, deck.Capture());
    }

    [Fact]
    [Trait("TS", "TS-oracle-deck-003")]
    public void SpendPair_山札が尽きている_捨札をシャッフルして戻して引く()
    {
        // 4 枚のデッキ: 手札 4 枚で山札は空。2 枚が捨札に入ってから 2 枚引くので、同じ 4 枚が手札に戻る
        var deck = new OracleDeck(Seed, Rules(4, ("SYM_TREE", 1), ("SYM_WATER", 1), ("SYM_BLOOD", 1), ("SYM_FIRE", 1)));
        Assert.Empty(deck.DrawPile);
        var shuffles = deck.ShuffleCount;

        deck.SpendPair(0, 1);

        Assert.Equal(4, deck.Hand.Count);
        Assert.Empty(deck.DiscardPile);
        Assert.Empty(deck.DrawPile);
        Assert.Equal(shuffles + 1, deck.ShuffleCount);
        Assert.Equal(["SYM_BLOOD", "SYM_FIRE", "SYM_TREE", "SYM_WATER"], [.. deck.Hand.OrderBy(id => id)]);
    }

    [Fact]
    [Trait("TS", "TS-oracle-deck-003")]
    public void Constructor_手札の枚数がデッキより多い_引けるだけ引く()
    {
        var deck = new OracleDeck(Seed, Rules(6, ("SYM_TREE", 1), ("SYM_WATER", 1), ("SYM_BLOOD", 1), ("SYM_FIRE", 1)));

        Assert.Equal(4, deck.Hand.Count);
        Assert.Empty(deck.DrawPile);
    }

    [Fact]
    [Trait("TS", "TS-oracle-deck-004")]
    public void SpendPair_同じシードと同じ操作を繰り返す_引く順が一致する()
    {
        List<string> Play()
        {
            var deck = new OracleDeck(Seed, TestRules.Deck);
            var seen = new List<string>(deck.Hand);
            for (var round = 0; round < 20; round++)
            {
                deck.SpendPair(0, 1);
                seen.AddRange(deck.Hand);
            }
            return seen;
        }

        var first = Play();
        var second = Play();

        Assert.Equal(first, second);
        Assert.Equal(4 * 21, first.Count);
    }

    [Fact]
    [Trait("TS", "TS-oracle-deck-006")]
    public void Reset_世界再創生_デッキの手札と山札と捨札は変わらない()
    {
        var time = new TimeControl(TestRules.World, TestRules.JumpYears);
        var deck = new OracleDeck(Seed, TestRules.Deck);
        var prophet = new ProphetDreams(time, TestRules.Dream, TestRules.ProphetDreams, deck, TestRules.Oracle);
        prophet.PronouncePair(0, 1);
        var before = deck.Capture();

        time.Reset();
        prophet.Reset();

        AssertSameDeck(before, deck.Capture());
    }
}
