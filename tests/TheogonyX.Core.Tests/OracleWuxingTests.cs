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

    [Theory]
    [Trait("TS", "TS-oracle-wuxing-004")]
    [InlineData("SYM_FIRE", "SYM_TREE", DogmaType.Worship, 0.7f)] // 木が火を生む
    [InlineData("SYM_FIRE", "SYM_WATER", DogmaType.Taboo, 0.7f)] // 水が火を剋す
    [InlineData("SYM_FIRE", "SYM_METAL", DogmaType.Destruction, 0.7f)] // 火が金を剋す
    [InlineData("SYM_FIRE", "SYM_EARTH", DogmaType.Worship, 0.35f)] // 火が土を生む
    [InlineData("SYM_FIRE", "SYM_FIRE", DogmaType.Worship, 0.91f)] // 比和
    [InlineData("SYM_TREE", "SYM_BLOOD", DogmaType.Destruction, 0.7f)] // 穢れ
    [InlineData("SYM_BLOOD", "SYM_TREE", DogmaType.Destruction, 0.7f)]
    public void Interpret_五行の関係_種別と強度が決まり1枚目が対象(string first, string second, DogmaType type, float intensity)
    {
        var dogma = WuxingInterpreter.Interpret(first, second, TestRules.OracleWuxing);

        Assert.NotNull(dogma);
        Assert.Equal(first, dogma.TargetSymbolId);
        Assert.Equal(type, dogma.Type);
        Assert.Equal(intensity, dogma.Intensity, Tolerance);
    }

    [Fact]
    [Trait("TS", "TS-oracle-wuxing-005")]
    public void Judge_五行の順序付きの25通り_違う五行は4つの関係に5通りずつで同じ五行は比和()
    {
        string[] cycle = ["SYM_TREE", "SYM_FIRE", "SYM_EARTH", "SYM_METAL", "SYM_WATER"];
        var relations = new List<WuxingRelation?>();
        foreach (var first in cycle)
        {
            foreach (var second in cycle)
            {
                var relation = WuxingInterpreter.Judge(first, second, TestRules.OracleWuxing).Relation;
                Assert.Equal(first == second, relation == WuxingRelation.Same);
                relations.Add(relation);
            }
        }

        foreach (var relation in Enum.GetValues<WuxingRelation>())
        {
            Assert.Equal(5, relations.Count(r => r == relation));
        }
    }

    [Theory]
    [Trait("TS", "TS-oracle-wuxing-005")]
    [InlineData("SYM_EARTH", "SYM_TREE", WuxingRelation.Overcome)] // 木は土を剋す
    [InlineData("SYM_WATER", "SYM_EARTH", WuxingRelation.Overcome)] // 土は水を剋す
    [InlineData("SYM_TREE", "SYM_METAL", WuxingRelation.Overcome)] // 金は木を剋す
    [InlineData("SYM_TREE", "SYM_WATER", WuxingRelation.Generated)] // 水は木を生む
    [InlineData("SYM_METAL", "SYM_TREE", WuxingRelation.Rebel)] // 対象の金が木を剋す
    [InlineData("SYM_WATER", "SYM_TREE", WuxingRelation.Leak)] // 対象の水が木を生む
    public void Judge_相生と相剋の並び_仕様書の関係になる(string first, string second, WuxingRelation relation)
    {
        Assert.Equal(relation, WuxingInterpreter.Judge(first, second, TestRules.OracleWuxing).Relation);
    }

    [Fact]
    [Trait("TS", "TS-oracle-wuxing-004")]
    public void Interpret_汚染ルーンの狂乱_穢れとして破壊になる()
    {
        var dogma = WuxingInterpreter.Interpret("SYM_TREE", "RUNE_MADNESS", TestRules.OracleWuxing);

        Assert.NotNull(dogma);
        Assert.Equal(DogmaType.Destruction, dogma.Type);
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

    // 手札を指定した ProphetDreams（種別は五行の関係で決める）
    private static (ProphetDreams Prophet, TimeControl Time) CreateWithHand(params string[] hand)
    {
        var time = new TimeControl(
            TestRules.World, TestRules.JumpYears, new CivilizationSetup(TestRules.Civilization, TestRules.Terrain, Seed, TestRules.Needs), TestRules.History);
        var deck = new OracleDeck(Seed, TestRules.Deck);
        var state = deck.Capture();
        deck.Restore(state with { Hand = hand, DrawPile = [.. state.DrawPile, .. state.Hand] });
        return (new ProphetDreams(time, TestRules.ProphetDreams, deck, TestRules.OracleWuxing), time);
    }

    [Fact]
    [Trait("TS", "TS-oracle-wuxing-007")]
    public void PreviewPair_火と水_相剋で禁忌と予告し下した神託と一致する()
    {
        var (prophet, time) = CreateWithHand("SYM_FIRE", "SYM_WATER", "SYM_TREE", "SYM_EARTH");

        var preview = prophet.PreviewPair(0, 1);
        prophet.PronouncePair(0, 1);

        Assert.NotNull(preview);
        Assert.Equal(WuxingRelation.Overcome, preview.Relation);
        Assert.Equal(DogmaType.Taboo, preview.Type);
        var dogma = Assert.Single(time.Dogmas);
        Assert.Equal("SYM_FIRE", dogma.TargetSymbolId);
        Assert.Equal(DogmaType.Taboo, dogma.Type);
    }

    [Theory]
    [Trait("TS", "TS-oracle-wuxing-007")]
    [InlineData(0, 0)]
    [InlineData(0, 4)]
    [InlineData(-1, 1)]
    public void PreviewPair_番号が正しくない_予告しない(int first, int second)
    {
        var (prophet, _) = CreateWithHand("SYM_FIRE", "SYM_WATER", "SYM_TREE", "SYM_EARTH");

        Assert.Null(prophet.PreviewPair(first, second));
    }
}
