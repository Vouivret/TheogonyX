using TheogonyX.Core;

namespace TheogonyX.Core.Tests;

// 建物が建ったときの、基本ルーンの入手（docs/domain/oracle-deck.md の「ルーンの入手」）。
public class OracleBuildingRuneTests
{
    private const int Seed = 42;

    private static OracleDeck Create(int seed = Seed) => new(seed, TestRules.Deck);

    private static Building Build(int id, BuildingType type) => new(id, type, Region: 0, Column: 0, Row: 0, CollapseYear: 9999);

    [Fact]
    [Trait("TS", "TS-oracle-deck-017")]
    public void GainFromBuildings_工房と祠が建つ_対応する基本ルーンが捨札に1枚ずつ加わる()
    {
        var deck = Create();

        var gained = deck.GainFromBuildings([Build(0, BuildingType.Workshop), Build(1, BuildingType.Shrine)]);

        Assert.Equal(2, gained);
        Assert.Equal(["SYM_FIRE", "SYM_RAIN"], deck.DiscardPile);
        Assert.Equal(1, deck.LastBuildingId);
    }

    [Fact]
    [Trait("TS", "TS-oracle-deck-017")]
    public void GainFromBuildings_同じ建物を二度渡す_二度は加わらない()
    {
        var deck = Create();
        Building[] buildings = [Build(0, BuildingType.Workshop)];
        deck.GainFromBuildings(buildings);

        var gained = deck.GainFromBuildings(buildings);

        Assert.Equal(0, gained);
        Assert.Single(deck.DiscardPile);
    }

    [Fact]
    [Trait("TS", "TS-oracle-deck-017")]
    public void GainFromBuildings_家が建つ_加わらないが数えたことになる()
    {
        var deck = Create();

        var gained = deck.GainFromBuildings([Build(0, BuildingType.HouseHut)]);

        Assert.Equal(0, gained);
        Assert.Empty(deck.DiscardPile);
        Assert.Equal(0, deck.LastBuildingId);
    }

    [Fact]
    [Trait("TS", "TS-oracle-deck-018")]
    public void GainFromBuildings_総数が上限_加わらないが数えたことになる()
    {
        var deck = Create();
        Building[] many = [.. Enumerable.Range(0, 10).Select(id => Build(id, BuildingType.Workshop))];

        var gained = deck.GainFromBuildings(many);

        // 最初のデッキは 12 枚、上限は 20 枚
        Assert.Equal(8, gained);
        Assert.Equal(20, deck.TotalCount);
        Assert.Equal(9, deck.LastBuildingId);
        Assert.Equal(0, deck.GainFromBuildings(many));
    }

    [Fact]
    [Trait("TS", "TS-oracle-deck-017")]
    public void OnTimeAdvanced_建物が建つ_ルーンが加わり世界再創生で数え直す()
    {
        var time = new TimeControl(
            TestRules.World, TestRules.JumpYears, new CivilizationSetup(TestRules.Civilization, TestRules.Terrain, Seed, TestRules.Needs), TestRules.History);
        var deck = Create();
        var prophet = new ProphetDreams(time, TestRules.Dream, TestRules.ProphetDreams, deck, TestRules.Oracle);

        Assert.Equal(0, prophet.OnTimeAdvanced().RunesGained);

        deck.GainFromBuildings([Build(5, BuildingType.Workshop)]);
        Assert.Equal(5, deck.LastBuildingId);
        time.Reset();
        prophet.Reset();

        Assert.Equal(-1, deck.LastBuildingId);
    }

    [Fact]
    [Trait("TS", "TS-oracle-deck-019")]
    public void Validate_建物のルーンの定義が正しくない_例外になる()
    {
        var symbols = new HashSet<string> { "SYM_TREE", "SYM_RAIN", "SYM_BLOOD", "SYM_FIRE" };

        void Check(DeckRules rules) => rules.Validate(symbols);

        Check(TestRules.Deck);
        Assert.Throws<ArgumentException>(() => Check(TestRules.Deck with { BuildingRunes = [new BuildingRune("HouseHut", "SYM_FIRE")] }));
        Assert.Throws<ArgumentException>(() => Check(TestRules.Deck with { BuildingRunes = [new BuildingRune("Castle", "SYM_FIRE")] }));
        Assert.Throws<ArgumentException>(() => Check(TestRules.Deck with { BuildingRunes = [new BuildingRune("Workshop", "SYM_UNKNOWN")] }));
        Assert.Throws<ArgumentException>(() => Check(TestRules.Deck with { MaxSize = -1 }));
    }
}
