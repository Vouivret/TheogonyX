using TheogonyX.Core;

namespace TheogonyX.Core.Tests;

// 神託の五行の段階 1（docs/domain/oracle-wuxing.md）。土・金の記号を足し、種別はまだ極性の和で決める。
public class OracleWuxingTests
{
    private const int Seed = 42;
    private const float Tolerance = 0.001f;

    // 2 枚だけの山札（記号は a, b）を、手札に引いた状態で作る
    private static (ProphetDreams Prophet, TimeControl Time) CreatePair(string a, string b)
    {
        var time = new TimeControl(
            TestRules.World, TestRules.JumpYears, new CivilizationSetup(TestRules.Civilization, TestRules.Terrain, Seed, TestRules.Needs), TestRules.History);
        var rules = a == b
            ? new DeckRules([new DeckRuneCount(a, 2)], HandSize: 2)
            : new DeckRules([new DeckRuneCount(a, 1), new DeckRuneCount(b, 1)], HandSize: 2);
        var deck = new OracleDeck(Seed, rules);
        return (new ProphetDreams(time, TestRules.Dream, TestRules.ProphetDreams, deck, TestRules.Oracle), time);
    }

    [Theory]
    [Trait("TS", "TS-oracle-wuxing-001")]
    [InlineData("SYM_EARTH", "SYM_EARTH", DogmaType.Worship)]
    [InlineData("SYM_METAL", "SYM_METAL", DogmaType.Taboo)]
    [InlineData("SYM_EARTH", "SYM_METAL", DogmaType.Destruction)]
    public void PronouncePair_土と金_極性の和で種別が決まる(string first, string second, DogmaType type)
    {
        var (prophet, time) = CreatePair(first, second);
        var firstIndex = prophet.Hand.ToList().IndexOf(first);
        var secondIndex = first == second ? 1 - firstIndex : prophet.Hand.ToList().IndexOf(second);

        prophet.PronouncePair(firstIndex, secondIndex);

        var dogma = Assert.Single(time.Dogmas);
        Assert.Equal(first, dogma.TargetSymbolId);
        Assert.Equal(type, dogma.Type);
        // 衝撃度 = 0.7 × 0.3 ＋ 0.7 × 0.3
        Assert.Equal(0.42f, dogma.Intensity, Tolerance);
    }

    [Fact]
    [Trait("TS", "TS-oracle-wuxing-002")]
    public void Constructor_最初のデッキ_五行が3枚ずつで血は無い()
    {
        var deck = new OracleDeck(Seed, TestRules.Deck);
        var all = deck.Hand.Concat(deck.DrawPile).ToList();

        Assert.Equal(15, all.Count);
        foreach (var symbolId in new[] { "SYM_TREE", "SYM_FIRE", "SYM_EARTH", "SYM_METAL", "SYM_WATER" })
        {
            Assert.Equal(3, all.Count(id => id == symbolId));
        }
        Assert.DoesNotContain("SYM_BLOOD", all);
    }

    [Fact]
    [Trait("TS", "TS-oracle-wuxing-002")]
    public void GainFromBuildings_工房_金のルーンが加わる()
    {
        var deck = new OracleDeck(Seed, TestRules.Deck);

        deck.GainFromBuildings([new Building(0, BuildingType.Workshop, Region: 0, Column: 0, Row: 0, CollapseYear: 9999)]);

        Assert.Equal(["SYM_METAL"], deck.DiscardPile);
    }
}
