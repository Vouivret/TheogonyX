using TheogonyX.Core;

namespace TheogonyX.Core.Tests;

/// <summary>新しい星での時間の進み（住人・文明・予言者。plan/feat-planet-grid-1b.md の段 1b-2b）。</summary>
public class PlanetTimeControlTests
{
    private static TimeControl Make(int seed) => new(
        PlanetTests.World, 100, new CivilizationSetup(TestRules.Civilization, TestRules.Terrain, seed, TestRules.Needs),
        planet: new PlanetSetup(PlanetTests.Rules));

    [Fact]
    [Trait("TS", "TS-regional-environment-016")]
    public void 新しい星_星全体の値は全地域の平均で_年は進む()
    {
        var time = Make(1);

        time.Jump();

        Assert.Equal(100, time.World.Year);
        Assert.Equal(100, time.Planet!.Year);
        var average = time.Planet.Average();
        Assert.Equal(average.ForestAmount, time.World.ForestAmount, 3);
        Assert.Equal(average.Temperature, time.World.Temperature, 3);
        Assert.Equal(96, time.Civilization!.Regions.Count);
    }

    [Fact]
    [Trait("TS", "TS-regional-environment-017")]
    public void 新しい星_住人は予言者の最初の地域から始まり_海の地域には住まない_隣へ広がる()
    {
        var time = Make(1);
        var start = time.Planet!.ProphetRegion;

        time.StartAutoPlay();
        time.AdvanceOneYear();
        time.Pause();
        Assert.All(time.Residents!.People, person => Assert.Equal(start, person.Region));

        for (var i = 0; i < 4; i++)
        {
            time.Jump();
        }

        var populated = time.Residents.People.Select(person => person.Region).Distinct().ToList();
        Assert.True(populated.Count > 1, "人が隣の地域へ広がる");
        Assert.All(populated, region => Assert.NotEqual(BaseTerrain.Sea, time.Planet.Terrain[region]));
    }

    [Fact]
    [Trait("TS", "TS-regional-environment-018")]
    public void 新しい星_建物は陸の建てられるタイルにだけ建つ()
    {
        var time = Make(1);

        for (var i = 0; i < 4; i++)
        {
            time.Jump();
        }

        Assert.NotEmpty(time.Civilization!.Buildings);
        var tiles = time.Tiles!;
        foreach (var building in time.Civilization.Buildings)
        {
            Assert.Equal(building.Region, time.Planet!.RegionAt(building.Column, building.Row));
            Assert.True(tiles[building.Column, building.Row] is TileType.Grassland or TileType.Jungle);
        }
    }

    [Fact]
    [Trait("TS", "TS-regional-environment-019")]
    public void 新しい星_カオス度を足すと全地域に同じだけ足され_同じ入力なら同じ結果()
    {
        var first = Make(2);
        var second = Make(2);
        var before = first.Planet!.Average().ChaosLevel;

        first.AddChaos(20f);
        first.StartAutoPlay();
        first.AdvanceOneYear();
        second.AddChaos(20f);
        second.StartAutoPlay();
        second.AdvanceOneYear();

        Assert.True(first.Planet.Average().ChaosLevel > before + 10f);
        Assert.Equal(first.Planet.Average(), second.Planet!.Average());
    }

    [Fact]
    [Trait("TS", "TS-regional-environment-020")]
    public void 新しい星_世界再創生でも同じ星_年と住人は初期に戻る()
    {
        var time = Make(3);
        var terrain = time.Planet!.Terrain.ToList();
        time.Jump();

        time.Reset();

        Assert.Equal(0, time.World.Year);
        Assert.Equal(terrain, time.Planet!.Terrain.ToList());
        Assert.Empty(time.Residents!.People);
    }
}
