using TheogonyX.Core;

namespace TheogonyX.Core.Tests;

/// <summary>星の格子・星を作る処理・地域ごとの 1 年（docs/domain/regional-environment.md、plan/feat-planet-grid-1a.md）。</summary>
public class PlanetTests
{
    // plan/feat-planet-grid-1a.md の「仮の値」（game/data/rules.json の planet と同じ）。本番の値には依存させない。
    internal static readonly PlanetRules Rules = new(
        Columns: 16, Bands: 6, RegionSize: 12, PlateCount: 7, ContinentSeeds: 3,
        SeaShare: new FloatRange(0.3f, 0.4f), PlainWeight: 4f, ForestWeight: 4f, MountainWeight: 2f, BoundaryMountainMultiplier: 4f,
        DeepSea: new IntRange(-4000, -200), ShallowSea: new IntRange(-200, -1), CoastLand: new IntRange(1, 50),
        PlainLand: new IntRange(50, 300), ForestLand: new IntRange(100, 800), MountainLand: new IntRange(1500, 4000),
        VolcanoLand: new IntRange(500, 2500), CoastSeaWidth: 3,
        IslandSeeds: 2, IslandRegions: new IntRange(1, 2), ArcChance: 0.5f, ArcIslands: 3, ArcSize: new IntRange(2, 5),
        HotspotCount: 2, HotspotSize: new IntRange(1, 4), ShelfChance: 0.3f, ShelfSize: new IntRange(2, 8),
        SmoothingPasses: 3, BandTemperatureOffsets: [-30f, -7f, 10f, 10f, -7f, -30f],
        CoastWaterOffset: 10f, MountainTemperatureOffset: -10f, ForestForestOffset: 20f, SeaForestOffset: -50f,
        SmoothRate: 0.1f, ZeroInfluenceDistance: 8);

    private static readonly PlanetRules NoIslands = Rules with { IslandSeeds = 0, ArcChance = 0f, HotspotCount = 0, ShelfChance = 0f };

    internal static readonly WorldRules World = TestRules.World with
    {
        Initial = new EnvironmentValues(ForestAmount: 50f, WaterAmount: 100f, Temperature: 15f, ChaosLevel: 0f, HarmonyLevel: 50f),
    };

    private static RegionalWorld Make(int seed, PlanetRules? rules = null) => PlanetGenerator.Generate(rules ?? Rules, World, seed);

    [Fact]
    [Trait("TS", "TS-regional-environment-001")]
    public void PlanetGrid_96地域_横は輪で縦はつながらない()
    {
        var grid = new PlanetGrid(16, 6);

        Assert.Equal(96, grid.Count);
        Assert.Contains(grid.Index(0, 2), grid.Neighbors(grid.Index(15, 2))); // 列 15 の隣に列 0
        Assert.Contains(grid.Index(0, 1), grid.Neighbors(grid.Index(15, 2)));
        Assert.Contains(grid.Index(0, 3), grid.Neighbors(grid.Index(15, 2)));
        Assert.DoesNotContain(grid.Index(0, 5), grid.Neighbors(grid.Index(0, 0))); // 段 0 と段 5 は隣でない
        Assert.Equal(5, grid.Neighbors(grid.Index(0, 0)).Count); // 北の極の端の地域は隣 5
        Assert.Equal(8, grid.Neighbors(grid.Index(7, 3)).Count);
    }

    [Theory]
    [Trait("TS", "TS-regional-environment-001")]
    [InlineData(1, 0, 15, 0, 2)] // 列 1 と列 15 は輪で 2
    [InlineData(0, 0, 0, 3, 3)]
    [InlineData(0, 0, 8, 5, 8)] // 横 8（輪の反対側）と縦 5 の大きいほう
    [InlineData(2, 2, 2, 2, 0)]
    public void PlanetGrid_距離_横と縦の歩数の大きいほう(int c1, int b1, int c2, int b2, int expected)
    {
        var grid = new PlanetGrid(16, 6);

        Assert.Equal(expected, grid.Distance(grid.Index(c1, b1), grid.Index(c2, b2)));
    }

    [Fact]
    [Trait("TS", "TS-regional-environment-002")]
    public void Generate_同じシード_同じ星_違うシード_違う星()
    {
        var a = Make(42);
        var b = Make(42);
        var c = Make(43);

        Assert.Equal(a.ProphetRegion, b.ProphetRegion);
        Assert.Equal(a.Terrain, b.Terrain);
        Assert.Equal(a.Plates, b.Plates);
        Assert.Equal(Elevations(a), Elevations(b));
        Assert.NotEqual(Elevations(a), Elevations(c));
    }

    [Fact]
    [Trait("TS", "TS-regional-environment-003")]
    public void Generate_海の割合と予言者の地域と沿岸()
    {
        for (var seed = 1; seed <= 30; seed++)
        {
            var world = Make(seed);
            var sea = world.Terrain.Count(terrain => terrain == BaseTerrain.Sea);

            Assert.InRange(sea, 29, 38); // 96 地域の 3〜4 割（四捨五入）
            Assert.NotEqual(BaseTerrain.Sea, world.Terrain[world.ProphetRegion]);
            for (var region = 0; region < 96; region++)
            {
                var touchesSea = world.Grid.Neighbors(region).Any(other => world.Terrain[other] == BaseTerrain.Sea);
                Assert.Equal(world.Terrain[region] == BaseTerrain.Coast, world.Terrain[region] != BaseTerrain.Sea && touchesSea);
            }
        }
    }

    [Fact]
    [Trait("TS", "TS-regional-environment-004")]
    public void Generate_プレートの境目は隣にほかのプレートがある_山岳が境目に寄る()
    {
        double boundaryMountain = 0, boundaryLand = 0, innerMountain = 0, innerLand = 0;
        for (var seed = 1; seed <= 40; seed++)
        {
            var world = Make(seed);
            for (var region = 0; region < 96; region++)
            {
                Assert.Equal(world.Grid.Neighbors(region).Any(other => world.Plates[other] != world.Plates[region]), world.IsBoundary[region]);
                var inland = world.Terrain[region] is BaseTerrain.Plain or BaseTerrain.Forest or BaseTerrain.Mountain;
                if (!inland)
                {
                    continue;
                }
                var mountain = world.Terrain[region] == BaseTerrain.Mountain ? 1 : 0;
                if (world.IsBoundary[region])
                {
                    boundaryLand++;
                    boundaryMountain += mountain;
                }
                else
                {
                    innerLand++;
                    innerMountain += mountain;
                }
            }
        }

        Assert.True(boundaryMountain / boundaryLand > innerMountain / innerLand);
    }

    [Fact]
    [Trait("TS", "TS-regional-environment-005")]
    public void Generate_島なしの定義_海の地域はすべて海_陸の地域はすべて陸_沿岸は両方()
    {
        for (var seed = 1; seed <= 10; seed++)
        {
            var world = Make(seed, NoIslands);
            for (var region = 0; region < 96; region++)
            {
                var land = 0;
                for (var ly = 0; ly < 12; ly++)
                {
                    for (var lx = 0; lx < 12; lx++)
                    {
                        land += world.IsLandAt(world.Grid.ColumnOf(region) * 12 + lx, world.Grid.BandOf(region) * 12 + ly) ? 1 : 0;
                    }
                }
                switch (world.Terrain[region])
                {
                    case BaseTerrain.Sea:
                        Assert.Equal(0, land);
                        break;
                    case BaseTerrain.Coast:
                        Assert.InRange(land, 1, 143);
                        break;
                    default:
                        Assert.Equal(144, land);
                        break;
                }
            }
        }
    }

    [Fact]
    [Trait("TS", "TS-regional-environment-005")]
    public void Generate_ホットスポットだけ_海の地域に置く島のタイルは多くても島の数かける大きさ()
    {
        var hotspotOnly = NoIslands with { HotspotCount = 2, SmoothingPasses = 0 };
        for (var seed = 1; seed <= 20; seed++)
        {
            var world = Make(seed, hotspotOnly);
            var islandTiles = 0;
            for (var y = 0; y < world.Height; y++)
            {
                for (var x = 0; x < world.Width; x++)
                {
                    var region = world.RegionAt(x, y);
                    if (world.Terrain[region] == BaseTerrain.Sea && world.IsLandAt(x, y))
                    {
                        islandTiles++;
                    }
                }
            }

            Assert.InRange(islandTiles, 0, 8); // 島 2 つ × 最大 4 タイル（置ける海の地域が無いシードでは 0）
            Assert.InRange(world.Terrain.Count(terrain => terrain == BaseTerrain.Sea), 29, 38);
        }
    }

    [Fact]
    [Trait("TS", "TS-regional-environment-006")]
    public void Generate_標高_陸は1m以上_海は負_なめらかにした後も()
    {
        for (var seed = 1; seed <= 10; seed++)
        {
            var world = Make(seed);
            for (var y = 0; y < world.Height; y++)
            {
                for (var x = 0; x < world.Width; x++)
                {
                    if (world.IsLandAt(x, y))
                    {
                        Assert.True(world.ElevationAt(x, y) >= 1);
                    }
                    else
                    {
                        Assert.True(world.ElevationAt(x, y) <= -1);
                    }
                }
            }
        }
    }

    [Fact]
    [Trait("TS", "TS-regional-environment-006")]
    public void Generate_最初の値_段の気温の補正と土台の補正()
    {
        var world = Make(5);

        for (var region = 0; region < 96; region++)
        {
            var band = world.Grid.BandOf(region);
            var offset = new[] { -30f, -7f, 10f, 10f, -7f, -30f }[band];
            var expectedTemperature = 15f + offset + (world.Terrain[region] == BaseTerrain.Mountain ? -10f : 0f);

            Assert.Equal(expectedTemperature, world.Values[region].Temperature, 0.001f);
            Assert.Equal(world.Terrain[region] == BaseTerrain.Sea ? 0f : world.Terrain[region] == BaseTerrain.Forest ? 70f : 50f, world.Values[region].ForestAmount, 0.001f);
        }
    }

    [Fact]
    [Trait("TS", "TS-regional-environment-007")]
    public void AdvanceYear_96地域の平均_水資源が半分の地域で0なら50()
    {
        var world = Make(1);
        for (var region = 0; region < 96; region++)
        {
            world.Values[region] = world.Values[region] with { WaterAmount = region < 48 ? 0f : 100f };
        }

        Assert.Equal(50f, world.Average().WaterAmount, 0.001f);
    }

    [Fact]
    [Trait("TS", "TS-regional-environment-007")]
    public void AdvanceYear_ドグマが無く値がそろっている_変わらない()
    {
        var world = Make(1);
        Uniform(world, 50f);

        RegionalWorldSimulator.AdvanceYear(world, [], World, Rules);

        Assert.Equal(1, world.Year);
        Assert.All(world.Values, value => Assert.Equal(50f, value.ForestAmount, 0.001f));
    }

    [Fact]
    [Trait("TS", "TS-regional-environment-008")]
    public void AdvanceYear_ドグマ_予言者の地域がいちばん変わり_遠い地域は変わらず_平均は効果どおり()
    {
        var world = Make(1);
        Uniform(world, 50f);
        var noSmoothing = Rules with { SmoothRate = 0f };
        var dogmas = new[] { new Dogma("SYM_TREE", DogmaType.Worship, 1f) }; // 森林量 +0.5 / 年

        RegionalWorldSimulator.AdvanceYear(world, dogmas, World, noSmoothing);

        var changes = world.Values.Select(value => value.ForestAmount - 50f).ToArray();
        Assert.Equal(changes.Max(), changes[world.ProphetRegion], 0.0001f);
        for (var region = 0; region < 96; region++)
        {
            if (world.Grid.Distance(world.ProphetRegion, region) >= 8)
            {
                Assert.Equal(0f, changes[region], 0.0001f);
            }
        }
        Assert.Equal(0.5f, changes.Average(), 0.001f);
    }

    [Fact]
    [Trait("TS", "TS-regional-environment-009")]
    public void AdvanceYear_平均が限界を越えたら崩壊_1地域だけなら崩壊しない_崩壊後は変わらない()
    {
        var one = Make(1);
        Uniform(one, 50f);
        one.Values[0] = one.Values[0] with { ChaosLevel = 100f };
        RegionalWorldSimulator.AdvanceYear(one, [], World, Rules with { SmoothRate = 0f });
        Assert.False(one.IsDestroyed);

        var all = Make(1);
        Uniform(all, 50f);
        for (var region = 0; region < 96; region++)
        {
            all.Values[region] = all.Values[region] with { ChaosLevel = 100f };
        }
        RegionalWorldSimulator.AdvanceYear(all, [], World, Rules);
        Assert.True(all.IsDestroyed);
        var year = all.Year;

        RegionalWorldSimulator.AdvanceYear(all, [], World, Rules);

        Assert.Equal(year, all.Year);
    }

    private static void Uniform(RegionalWorld world, float forest)
    {
        for (var region = 0; region < world.Values.Length; region++)
        {
            world.Values[region] = new EnvironmentValues(forest, 50f, 15f, 0f, 50f);
        }
    }

    private static int[] Elevations(RegionalWorld world)
    {
        var list = new List<int>();
        for (var y = 0; y < world.Height; y++)
        {
            for (var x = 0; x < world.Width; x++)
            {
                list.Add(world.ElevationAt(x, y));
            }
        }
        return [.. list];
    }
}
