using TheogonyX.Core;

namespace TheogonyX.Core.Tests;

// 神託の五行（docs/domain/oracle-wuxing.md）。土・金の記号と、五行の関係によるドグマの種別・強度・予告。
public class OracleWuxingTests
{
    private const int Seed = 42;
    private const float Tolerance = 0.001f;

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

    [Fact]
    [Trait("TS", "TS-oracle-wuxing-006")]
    public void Validate_五行の定義が正しくない_例外になる()
    {
        var symbols = new HashSet<string> { "SYM_TREE", "SYM_FIRE", "SYM_EARTH", "SYM_METAL", "SYM_WATER", "SYM_BLOOD" };
        var wuxing = TestRules.OracleWuxing.Wuxing!;

        void Check(OracleRules rules) => rules.Validate(symbols);

        Check(TestRules.OracleWuxing);
        Assert.Throws<ArgumentException>(() => Check(TestRules.OracleWuxing with { Wuxing = null! }));
        Assert.Throws<ArgumentException>(() => Check(TestRules.OracleWuxing with { Wuxing = wuxing with { Cycle = ["SYM_TREE", "SYM_FIRE", "SYM_EARTH", "SYM_METAL"] } }));
        Assert.Throws<ArgumentException>(() => Check(TestRules.OracleWuxing with { Wuxing = wuxing with { Cycle = ["SYM_TREE", "SYM_TREE", "SYM_EARTH", "SYM_METAL", "SYM_WATER"] } }));
        Assert.Throws<ArgumentException>(() => Check(TestRules.OracleWuxing with { Wuxing = wuxing with { Impure = [] } }));
        Assert.Throws<ArgumentException>(() => Check(TestRules.OracleWuxing with { Wuxing = wuxing with { Impure = ["SYM_BLOOD", "SYM_TREE"] } }));
        Assert.Throws<ArgumentException>(() => Check(TestRules.OracleWuxing with { Wuxing = wuxing with { Multipliers = wuxing.Multipliers with { Leak = -0.1f } } }));
    }
}
