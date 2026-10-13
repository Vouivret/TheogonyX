namespace TheogonyX.Core.Tests;

public class StrataTests
{
    private const int Seed = 42;

    // 地域 6 は列 12・13。枠は行 0・2・4・6 の 4 つ
    private const int Region = 6;
    private const int Column = 12;

    // 森林量 50・水資源 100・気温 15 では、どの列も草地
    private static WorldState GrasslandWorld(int year, float chaos = 0f) =>
        new() { Year = year, ForestAmount = 50f, WaterAmount = 100f, Temperature = 15f, ChaosLevel = chaos };

    private static Civilization Create(RegionCivilization region, Building[] buildings, Ruin[] ruins, Relic[]? relics = null, int nextId = 10)
    {
        var regions = Enumerable.Repeat(new RegionCivilization(0, 0, 0, 0), TestRules.Civilization.Regions.Count).ToArray();
        regions[Region] = region;
        return new Civilization(regions, buildings, ruins, relics ?? [], nextId);
    }

    private static void Advance(Civilization civilization, WorldState world, IReadOnlyList<Dogma>? dogmas = null) =>
        CivilizationSimulator.AdvanceYear(civilization, world, dogmas ?? [], TestRules.Terrain, TestRules.Civilization, Seed);

    [Fact]
    [Trait("TS", "TS-strata-001")]
    public void AdvanceYear_寿命の年_廃墟になり同じ年にその上に建て直す()
    {
        var civilization = Create(new RegionCivilization(0.25, 0, 1, 0), [new Building(0, BuildingType.Workshop, Region, Column, 0, 10)], []);

        Advance(civilization, GrasslandWorld(10));

        Assert.Equal(new Ruin(0, BuildingType.Workshop, Region, Column, 0, 10), Assert.Single(civilization.Ruins));
        var rebuilt = Assert.Single(civilization.Buildings);
        Assert.Equal(new Building(10, BuildingType.Workshop, Region, Column, 0, rebuilt.CollapseYear), rebuilt); // 廃墟の枠を先に選ぶ
        Assert.InRange(rebuilt.CollapseYear, 210, 410);
    }

    [Fact]
    [Trait("TS", "TS-strata-001")]
    public void AdvanceYear_寿命の前の年_崩れない()
    {
        var building = new Building(0, BuildingType.Workshop, Region, Column, 0, 10);
        var civilization = Create(new RegionCivilization(0.25, 0, 1, 0), [building], []);

        Advance(civilization, GrasslandWorld(9));

        Assert.Equal(building, Assert.Single(civilization.Buildings));
        Assert.Empty(civilization.Ruins);
    }

    [Fact]
    [Trait("TS", "TS-strata-002")]
    public void AdvanceYear_建て増しした建物が崩れる_下の廃墟がその年の値と一緒に1層目に沈む()
    {
        var blood = new Dogma("SYM_BLOOD", DogmaType.Taboo, 0.5f);
        var civilization = Create(
            new RegionCivilization(0.25, 0, 1, 0),
            [new Building(1, BuildingType.Workshop, Region, Column, 0, 10)],
            [new Ruin(0, BuildingType.Shrine, Region, Column, 0, 5)]);

        Advance(civilization, GrasslandWorld(10, chaos: 30f), [blood]);

        Assert.Equal(new Relic(0, BuildingType.Shrine, Region, Column, 0, 1, 10, 0.25, 0, 0.3, [blood]), Assert.Single(civilization.Relics));
        Assert.Equal(new Ruin(1, BuildingType.Workshop, Region, Column, 0, 10), Assert.Single(civilization.Ruins));
    }

    [Theory]
    [Trait("TS", "TS-strata-003")]
    [InlineData(149, false)]
    [InlineData(150, true)]
    public void AdvanceYear_上に建物の無い廃墟_150年で1層目に沈む(int year, bool buried)
    {
        var civilization = Create(new RegionCivilization(0, 0, 0, 0), [], [new Ruin(0, BuildingType.Shrine, Region, Column, 2, 0)]);

        Advance(civilization, GrasslandWorld(year));

        Assert.Equal(buried, civilization.Ruins.Count == 0);
        Assert.Equal(buried, civilization.Relics.Count == 1);
    }

    [Fact]
    [Trait("TS", "TS-strata-003")]
    public void AdvanceYear_上に建物が建っている廃墟_150年たっても沈まない()
    {
        var civilization = Create(
            new RegionCivilization(0, 0.25, 0, 1),
            [new Building(1, BuildingType.Shrine, Region, Column, 2, 1000)],
            [new Ruin(0, BuildingType.Shrine, Region, Column, 2, 0)]);

        Advance(civilization, GrasslandWorld(200));

        Assert.Single(civilization.Ruins);
        Assert.Empty(civilization.Relics);
    }

    [Fact]
    [Trait("TS", "TS-strata-004")]
    public void AdvanceYear_3層とも埋まった枠に沈む_遺物が1層ずつ深くなり3層目だったものが消える()
    {
        Relic Old(int id, int depth) => new(id, BuildingType.Workshop, Region, Column, 2, depth, 0, 0.5, 0.5, 0, []);
        var civilization = Create(
            new RegionCivilization(0, 0, 0, 0), [], [new Ruin(0, BuildingType.Shrine, Region, Column, 2, 0)],
            [Old(20, 1), Old(21, 2), Old(22, 3), new Relic(23, BuildingType.Workshop, Region, Column, 4, 1, 0, 0.5, 0.5, 0, [])]);

        Advance(civilization, GrasslandWorld(150));

        var depths = civilization.Relics.ToDictionary(relic => relic.Id, relic => relic.Depth);
        Assert.Equal(new Dictionary<int, int> { [0] = 1, [20] = 2, [21] = 3, [23] = 1 }, depths); // 別の枠（行 4）の遺物は変わらない
    }

    [Fact]
    [Trait("TS", "TS-strata-005")]
    public void Reset_建物と廃墟がある_すべて1層目に沈み遺物とIDの続きが残る()
    {
        var time = new TimeControl(TestRules.World, TestRules.JumpYears, new CivilizationSetup(TestRules.Civilization, TestRules.Terrain, Seed));
        var fire = new Dogma("SYM_FIRE", DogmaType.Worship, 0.4f);
        time.Restore(
            GrasslandWorld(300, chaos: 20f),
            [fire],
            Create(
                new RegionCivilization(0.45, 0.25, 2, 1),
                [new Building(1, BuildingType.Workshop, Region, Column, 0, 400), new Building(2, BuildingType.Shrine, Region, Column, 4, 400)],
                [new Ruin(0, BuildingType.Shrine, Region, Column, 0, 250)],
                [new Relic(5, BuildingType.Workshop, Region, Column, 0, 1, 100, 0.1, 0.1, 0, [])],
                nextId: 6));

        time.Reset();

        var civilization = time.Civilization!;
        Assert.Equal(0, time.World.Year);
        Assert.Empty(civilization.Buildings);
        Assert.Empty(civilization.Ruins);
        Assert.All(civilization.Regions, region => Assert.Equal(new RegionCivilization(0, 0, 0, 0), region));
        Assert.Equal(6, civilization.NextBuildingId);
        Relic Expected(int id, BuildingType type, int row, int depth) => new(id, type, Region, Column, row, depth, 300, 0.45, 0.25, 0.2, [fire]);
        Assert.Equal(
            new[] { Expected(1, BuildingType.Workshop, 0, 1), Expected(0, BuildingType.Shrine, 0, 2), Expected(2, BuildingType.Shrine, 4, 1) }
                .Append(new Relic(5, BuildingType.Workshop, Region, Column, 0, 3, 100, 0.1, 0.1, 0, []))
                .OrderBy(relic => relic.Id),
            civilization.Relics.OrderBy(relic => relic.Id));
    }

    [Theory]
    [Trait("TS", "TS-strata-006")]
    [InlineData(StrataState.Building, StrataTrigger.Collapse, StrataState.Ruin)]
    [InlineData(StrataState.Building, StrataTrigger.MacroSubduction, StrataState.Buried)]
    [InlineData(StrataState.Ruin, StrataTrigger.MicroSubduction, StrataState.Buried)]
    [InlineData(StrataState.Ruin, StrataTrigger.MacroSubduction, StrataState.Buried)]
    [InlineData(StrataState.Ruin, StrataTrigger.Overbuild, StrataState.Ruin)]
    [InlineData(StrataState.Buried, StrataTrigger.MicroSubduction, StrataState.Buried)]
    [InlineData(StrataState.Buried, StrataTrigger.MacroSubduction, StrataState.Buried)]
    [InlineData(StrataState.Buried, StrataTrigger.Excavation, StrataState.Excavated)]
    [InlineData(StrataState.Excavated, StrataTrigger.MicroSubduction, StrataState.Buried)]
    [InlineData(StrataState.Excavated, StrataTrigger.MacroSubduction, StrataState.Buried)]
    public void Next_表で可の遷移_次の状態になる(StrataState state, StrataTrigger trigger, StrataState expected)
    {
        Assert.Equal(expected, StrataTransitions.Next(state, trigger));
    }

    [Theory]
    [Trait("TS", "TS-strata-006")]
    [InlineData(StrataState.Building, StrataTrigger.MicroSubduction)]
    [InlineData(StrataState.Building, StrataTrigger.Excavation)]
    [InlineData(StrataState.Building, StrataTrigger.Overbuild)]
    [InlineData(StrataState.Ruin, StrataTrigger.Collapse)]
    [InlineData(StrataState.Ruin, StrataTrigger.Excavation)]
    [InlineData(StrataState.Buried, StrataTrigger.Collapse)]
    [InlineData(StrataState.Buried, StrataTrigger.Overbuild)]
    [InlineData(StrataState.Excavated, StrataTrigger.Collapse)]
    [InlineData(StrataState.Excavated, StrataTrigger.Excavation)]
    [InlineData(StrataState.Excavated, StrataTrigger.Overbuild)]
    public void Next_表で不可の遷移_例外になる(StrataState state, StrataTrigger trigger)
    {
        Assert.Throws<InvalidOperationException>(() => StrataTransitions.Next(state, trigger));
    }

    [Fact]
    [Trait("TS", "TS-strata-007")]
    public void Jump_同じシードで再創生をはさんで1000年_建物と廃墟と遺物がすべて一致する()
    {
        static TimeControl Run()
        {
            var time = new TimeControl(TestRules.World, TestRules.JumpYears, new CivilizationSetup(TestRules.Civilization, TestRules.TerrainWithLake, Seed));
            for (var cycle = 0; cycle < 2; cycle++)
            {
                time.World.ForestAmount = 50f;
                time.AddDogma(new Dogma("SYM_BLOOD", DogmaType.Taboo, 0.5f)); // 環境を変えないので、星は崩壊しない
                for (var i = 0; i < 5; i++)
                {
                    time.Jump();
                }
                if (cycle == 0)
                {
                    time.Reset();
                }
            }
            return time;
        }

        var first = Run();
        var second = Run();

        Assert.NotEmpty(first.Civilization!.Ruins);
        Assert.NotEmpty(first.Civilization.Relics);
        Assert.Equal(first.Civilization.Buildings, second.Civilization!.Buildings);
        Assert.Equal(first.Civilization.Ruins, second.Civilization.Ruins);
        Assert.Equal(first.Civilization.Relics, second.Civilization.Relics);
    }
}
