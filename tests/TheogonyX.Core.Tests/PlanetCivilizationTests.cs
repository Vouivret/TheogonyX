using TheogonyX.Core;

namespace TheogonyX.Core.Tests;

/// <summary>新しい星の文明の 1 年とタイルの分類（docs/domain/regional-environment.md、plan/feat-planet-grid-1b.md の段 1b-2a）。</summary>
public class PlanetCivilizationTests
{
    private static RegionalWorld Make(int seed) => PlanetGenerator.Generate(PlanetTests.Rules, PlanetTests.World, seed);

    private static Civilization Run(RegionalWorld world, TileType[,] tiles, int years, int seed)
    {
        var civilization = Civilization.CreateInitial(world.Grid.Count, TestRules.Civilization);
        for (var i = 0; i < years; i++)
        {
            world.Year++;
            PlanetCivilizationSimulator.AdvanceYear(civilization, world, tiles, [], TestRules.Civilization, seed);
        }
        return civilization;
    }

    [Fact]
    [Trait("TS", "TS-regional-environment-010")]
    public void Classify_海は水_極の陸は氷河_赤道の陸は土台の地形()
    {
        var world = Make(1);
        var tiles = PlanetTiles.Classify(world, TestRules.Terrain.Anomalies);

        Assert.Equal(world.Width, tiles.GetLength(0));
        Assert.Equal(world.Height, tiles.GetLength(1));
        for (var x = 0; x < world.Width; x++)
        {
            for (var y = 0; y < world.Height; y++)
            {
                // 水のタイルは海のタイルだけ（陸のタイルは水にならない）
                Assert.Equal(!world.IsLandAt(x, y), tiles[x, y] == TileType.Water);
                // 初期の気温は 15 + 極の帯 −30 ＝ −15（0 以下）なので、極の帯（段 0）の陸は氷河
                if (world.IsLandAt(x, y) && world.Grid.BandOf(world.RegionAt(x, y)) == 0)
                {
                    Assert.Equal(TileType.Glacier, tiles[x, y]);
                }
            }
        }
    }

    [Fact]
    [Trait("TS", "TS-regional-environment-011")]
    public void AdvanceYear_建物は陸の建てられる2x2の枠だけに建ち_枠は地域の中に収まる()
    {
        var world = Make(1);
        var tiles = PlanetTiles.Classify(world, TestRules.Terrain.Anomalies);

        var civilization = Run(world, tiles, 120, seed: 1);

        Assert.NotEmpty(civilization.Buildings);
        foreach (var building in civilization.Buildings)
        {
            Assert.Equal(0, building.Column % 2);
            Assert.Equal(0, building.Row % 2);
            Assert.Equal(building.Region, world.RegionAt(building.Column, building.Row));
            Assert.Equal(building.Region, world.RegionAt(building.Column + 1, building.Row + 1));
            foreach (var (dx, dy) in new[] { (0, 0), (1, 0), (0, 1), (1, 1) })
            {
                Assert.True(tiles[building.Column + dx, building.Row + dy] is TileType.Grassland or TileType.Jungle);
            }
        }
    }

    [Fact]
    [Trait("TS", "TS-regional-environment-012")]
    public void AdvanceYear_陸の無い地域は値が伸びず建物も建たない()
    {
        // 海だけの地域がある星を探す（種の数を決めておき、見つからなければ失敗）
        RegionalWorld? world = null;
        var seaRegion = -1;
        for (var seed = 1; seed <= 20 && seaRegion < 0; seed++)
        {
            world = Make(seed);
            for (var region = 0; region < world.Grid.Count; region++)
            {
                if (world.Terrain[region] == BaseTerrain.Sea)
                {
                    seaRegion = region;
                    break;
                }
            }
        }
        Assert.True(seaRegion >= 0);
        var tiles = PlanetTiles.Classify(world!, TestRules.Terrain.Anomalies);

        var civilization = Run(world!, tiles, 120, seed: 1);

        Assert.Equal(0, civilization.Regions[seaRegion].Tech);
        Assert.DoesNotContain(civilization.Buildings, building => building.Region == seaRegion);
    }

    [Theory]
    [Trait("TS", "TS-regional-environment-013")]
    [InlineData(0, 0, 0, 0, 1.0)] // 予言者の地域
    [InlineData(1, 0, 0, 0, 0.75)] // 隣（距離 1。ゼロの距離は 4）
    [InlineData(0, 0, 15, 0, 0.75)] // 横は輪なので、列 0 と列 15 は距離 1
    [InlineData(2, 2, 0, 0, 0.5)] // 斜めは大きいほうの距離（2）
    [InlineData(4, 0, 0, 0, 0.0)] // ゼロの距離
    [InlineData(0, 5, 0, 0, 0.0)] // 縦は輪でない（距離 5）
    public void Influence_予言者の地域から格子の距離で弱まる(int column, int band, int prophetColumn, int prophetBand, double expected)
    {
        var grid = new PlanetGrid(16, 6);

        var actual = PlanetCivilizationSimulator.Influence(grid, grid.Index(column, band), grid.Index(prophetColumn, prophetBand), TestRules.Civilization);

        Assert.Equal(expected, actual, 6);
    }

    [Fact]
    [Trait("TS", "TS-regional-environment-014")]
    public void AdvanceYear_同じ入力なら同じ結果()
    {
        var first = Make(3);
        var second = Make(3);
        var tilesFirst = PlanetTiles.Classify(first, TestRules.Terrain.Anomalies);
        var tilesSecond = PlanetTiles.Classify(second, TestRules.Terrain.Anomalies);

        var a = Run(first, tilesFirst, 150, seed: 7);
        var b = Run(second, tilesSecond, 150, seed: 7);

        Assert.Equal(a.Buildings, b.Buildings);
        Assert.Equal(a.Regions, b.Regions);
    }

    [Fact]
    [Trait("TS", "TS-regional-environment-015")]
    public void ToWorldState_全地域の平均と年と崩壊を写す()
    {
        var world = Make(1);
        world.Year = 42;
        world.IsDestroyed = true;
        var average = world.Average();

        var state = world.ToWorldState();

        Assert.Equal(42, state.Year);
        Assert.True(state.IsDestroyed);
        Assert.Equal(average.ForestAmount, state.ForestAmount);
        Assert.Equal(average.Temperature, state.Temperature);
        Assert.Equal(average.ChaosLevel, state.ChaosLevel);
    }
}
