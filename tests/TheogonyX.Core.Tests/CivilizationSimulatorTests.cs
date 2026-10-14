namespace TheogonyX.Core.Tests;

public class CivilizationSimulatorTests
{
    private const double Tolerance = 0.000001;
    private const int Seed = 42;

    // 予言者は列 7 なので地域 3。地域 3 から 4 地域離れると、ドグマは効かない
    private const int ProphetRegion = 3;

    // 森林量 50・水資源 100・気温 15 では、どの列も草地（異常のタイルが無い）
    private static WorldState GrasslandWorld() => new() { Year = 1, ForestAmount = 50f, WaterAmount = 100f, Temperature = 15f };

    private static Civilization Advance(WorldState world, IReadOnlyList<Dogma> dogmas, Civilization? civilization = null, TerrainRules? terrain = null)
    {
        civilization ??= Civilization.CreateInitial(TestRules.Civilization);
        CivilizationSimulator.AdvanceYear(civilization, world, dogmas, terrain ?? TestRules.Terrain, TestRules.Civilization, Seed);
        return civilization;
    }

    private static Civilization WithRegion(int region, RegionCivilization state)
    {
        var regions = Enumerable.Repeat(new RegionCivilization(0, 0, 0, 0), TestRules.Civilization.Regions.Count).ToArray();
        regions[region] = state;
        return new Civilization(regions, [], [], [], 0);
    }

    [Fact]
    [Trait("TS", "TS-emergent-progression-004")]
    public void AdvanceYear_ドグマなしの草地_どの地域も自然な伸びだけ伸びる()
    {
        var civilization = Advance(GrasslandWorld(), []);

        Assert.All(civilization.Regions, region =>
        {
            Assert.Equal(0.01, region.Tech, Tolerance);
            Assert.Equal(0.01, region.Culture, Tolerance);
        });
    }

    [Theory]
    [Trait("TS", "TS-emergent-progression-005")]
    [InlineData(ProphetRegion, 0.06)] // 予言者の地域では強度 0.5 のまま効く
    [InlineData(5, 0.035)] // 2 地域離れると半分
    [InlineData(1, 0.035)] // 西へ離れても同じ
    [InlineData(7, 0.01)] // 4 地域離れると効かない
    public void AdvanceYear_強度05の崇拝_文化が予言者から離れるほど伸びない(int region, double expectedCulture)
    {
        var civilization = Advance(GrasslandWorld(), [new Dogma("SYM_FIRE", DogmaType.Worship, 0.5f)]);

        Assert.Equal(expectedCulture, civilization.Regions[region].Culture, Tolerance);
        Assert.Equal(0.01, civilization.Regions[region].Tech, Tolerance);
    }

    [Fact]
    [Trait("TS", "TS-emergent-progression-005")]
    public void AdvanceYear_強度1の破壊_予言者の地域の技術が伸びる()
    {
        var civilization = Advance(GrasslandWorld(), [new Dogma("SYM_TREE", DogmaType.Destruction, 1f)]);

        Assert.Equal(0.11, civilization.Regions[ProphetRegion].Tech, Tolerance);
        Assert.Equal(0.01, civilization.Regions[ProphetRegion].Culture, Tolerance);
    }

    [Fact]
    [Trait("TS", "TS-emergent-progression-005")]
    public void AdvanceYear_禁忌と強度1の崇拝_伸びが半分になる()
    {
        var civilization = Advance(GrasslandWorld(),
            [new Dogma("SYM_WATER", DogmaType.Taboo, 0.7f), new Dogma("SYM_FIRE", DogmaType.Worship, 1f)]);

        Assert.Equal(0.055, civilization.Regions[ProphetRegion].Culture, Tolerance);
        Assert.Equal(0.005, civilization.Regions[ProphetRegion].Tech, Tolerance);
    }

    [Fact]
    [Trait("TS", "TS-emergent-progression-005")]
    public void AdvanceYear_値が1に近い_1で止まる()
    {
        var civilization = Advance(GrasslandWorld(), [], WithRegion(ProphetRegion, new RegionCivilization(0.995, 1.0, 4, 4)));

        Assert.Equal(1.0, civilization.Regions[ProphetRegion].Tech, Tolerance);
        Assert.Equal(1.0, civilization.Regions[ProphetRegion].Culture, Tolerance);
    }

    [Theory]
    [Trait("TS", "TS-emergent-progression-006")]
    [InlineData(TileType.Grassland, TileType.Grassland, false, 1.0)]
    [InlineData(TileType.Desert, TileType.Grassland, false, 0.6)] // 2 列が違えば悪いほう
    [InlineData(TileType.Glacier, TileType.Desert, false, 0.3)]
    [InlineData(TileType.Scorched, TileType.Grassland, false, 0.3)]
    [InlineData(TileType.Jungle, TileType.Jungle, false, 1.5)]
    [InlineData(TileType.Grassland, TileType.Grassland, true, 1.5)] // 草地の地域に水場がある
    [InlineData(TileType.Desert, TileType.Desert, true, 0.6)] // 砂漠の地域の水場は倍率を変えない
    public void EnvironmentMultiplier_地域の2列のタイル_倍率が決まる(TileType west, TileType east, bool water, double expected)
    {
        var tiles = new TileType[16, 9];
        for (var row = 0; row < 9; row++)
        {
            tiles[4, row] = west;
            tiles[5, row] = east;
        }
        if (water)
        {
            tiles[5, 4] = TileType.Water;
        }

        Assert.Equal(expected, CivilizationSimulator.EnvironmentMultiplier(tiles, 2, TestRules.Civilization), Tolerance);
    }

    [Fact]
    [Trait("TS", "TS-emergent-progression-006")]
    public void AdvanceYear_砂漠の地域_伸びが06倍になる()
    {
        // 水資源 10 では、左から 14 列が砂漠になる（planet-display.md の列数の式）
        var world = GrasslandWorld();
        world.WaterAmount = 10f;

        var civilization = Advance(world, []);

        Assert.Equal(0.006, civilization.Regions[0].Tech, Tolerance);
        Assert.Equal(0.01, civilization.Regions[7].Tech, Tolerance);
    }

    [Fact]
    [Trait("TS", "TS-emergent-progression-007")]
    public void AdvanceYear_技術が02を越える_その地域に工房が1つ建つ()
    {
        var civilization = Advance(GrasslandWorld(), [], WithRegion(6, new RegionCivilization(0.195, 0, 0, 0)));

        var building = Assert.Single(civilization.Buildings);
        Assert.Equal(new Building(0, BuildingType.Workshop, 6, 12, building.Row, building.CollapseYear), building);
        Assert.Contains(building.Row, new[] { 0, 2, 4, 6 });
        Assert.InRange(building.CollapseYear, 201, 401); // 年 1 に建ち、寿命は 200〜400 年
        Assert.Equal(1, civilization.Regions[6].TechStage);
        Assert.Equal(1, civilization.NextBuildingId);

        var world = GrasslandWorld();
        world.Year = 2;
        Advance(world, [], civilization);
        Assert.Single(civilization.Buildings); // 同じ閾値では 2 回建たない
    }

    [Fact]
    [Trait("TS", "TS-emergent-progression-007")]
    public void AdvanceYear_文化が1年で02と04を越える_祠が2つ建つ()
    {
        var worship = new Dogma("SYM_FIRE", DogmaType.Worship, 1f);

        var civilization = Advance(GrasslandWorld(), [worship, worship], WithRegion(ProphetRegion, new RegionCivilization(0, 0.195, 0, 0)));

        Assert.Equal(2, civilization.Buildings.Count);
        Assert.All(civilization.Buildings, building => Assert.Equal(BuildingType.Shrine, building.Type));
        Assert.NotEqual(civilization.Buildings[0].Row, civilization.Buildings[1].Row);
        Assert.Equal(2, civilization.Regions[ProphetRegion].CultureStage);
    }

    [Fact]
    [Trait("TS", "TS-emergent-progression-008")]
    public void AdvanceYear_湖のある地域_水場にかかる枠には建たない()
    {
        // 地域 5（列 10・11）は、行 3〜5 が水場。建てられる枠は行 0 と行 6 だけ
        for (var year = 1; year <= 20; year++)
        {
            var world = GrasslandWorld();
            world.Year = year;

            var civilization = Advance(world, [], WithRegion(5, new RegionCivilization(0.195, 0, 0, 0)), TestRules.TerrainWithLake);

            Assert.Contains(Assert.Single(civilization.Buildings).Row, new[] { 0, 6 });
        }
    }

    [Fact]
    [Trait("TS", "TS-emergent-progression-008")]
    public void AdvanceYear_密林の地域_建たないが閾値は越えたことになる()
    {
        var world = GrasslandWorld();
        world.ForestAmount = 100f;

        var civilization = Advance(world, [], WithRegion(6, new RegionCivilization(0.195, 0, 0, 0)));

        Assert.Empty(civilization.Buildings);
        Assert.Equal(1, civilization.Regions[6].TechStage);
    }

    [Fact]
    [Trait("TS", "TS-emergent-progression-008")]
    public void AdvanceYear_枠が埋まっている_それ以上建たない()
    {
        var regions = Enumerable.Repeat(new RegionCivilization(0, 0, 0, 0), 8).ToArray();
        regions[6] = new RegionCivilization(0.195, 0, 0, 0);
        Building[] full = [.. new[] { 0, 2, 4, 6 }.Select((row, id) => new Building(id, BuildingType.Shrine, 6, 12, row, 1000))];
        var civilization = new Civilization(regions, full, [], [], 4);

        Advance(GrasslandWorld(), [], civilization);

        Assert.Equal(4, civilization.Buildings.Count);
        Assert.Equal(1, civilization.Regions[6].TechStage);
    }

    [Fact]
    [Trait("TS", "TS-emergent-progression-009")]
    public void Jump_同じシードで10000年_地域の値と建物がすべて一致する()
    {
        static TimeControl Run()
        {
            var time = new TimeControl(TestRules.World, TestRules.JumpYears, new CivilizationSetup(TestRules.Civilization, TestRules.TerrainWithLake, Seed));
            time.World.ForestAmount = 50f;
            time.AddDogma(new Dogma("SYM_FIRE", DogmaType.Worship, 0.4f));
            time.AddDogma(new Dogma("SYM_BLOOD", DogmaType.Taboo, 0.5f)); // どちらも環境を変えないので、星は崩壊しない
            for (var i = 0; i < 100; i++)
            {
                time.Jump();
            }
            return time;
        }

        var first = Run();
        var second = Run();

        Assert.Equal(10_000, first.World.Year);
        Assert.NotEmpty(first.Civilization!.Buildings);
        Assert.Equal(first.Civilization.Regions, second.Civilization!.Regions);
        Assert.Equal(first.Civilization.Buildings, second.Civilization.Buildings);
    }
}
