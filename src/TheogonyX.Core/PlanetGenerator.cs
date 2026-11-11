namespace TheogonyX.Core;

/// <summary>
/// 星を作る（plan/feat-planet-grid-1a.md の「星を作る」）。同じシード・同じ定義なら同じ星になる。
/// プレート → 予言者の最初の地域 → 海の割合 → 島の地域の数 → 大陸 → 地域の島 → 沿岸 → 陸の種類 → タイル → タイルの島 → なめらかにする、の順。
/// </summary>
public static class PlanetGenerator
{
    public static RegionalWorld Generate(PlanetRules rules, WorldRules world, int seed)
    {
        ArgumentNullException.ThrowIfNull(rules);
        ArgumentNullException.ThrowIfNull(world);
        rules.Validate();
        var random = new Random(seed);
        var grid = new PlanetGrid(rules.Columns, rules.Bands);
        var count = grid.Count;

        // プレートと境目
        var plates = AssignPlates(grid, rules.PlateCount, random);
        var boundary = new bool[count];
        for (var region = 0; region < count; region++)
        {
            boundary[region] = grid.Neighbors(region).Any(other => plates[other] != plates[region]);
        }

        var prophetRegion = random.Next(count);

        // 海と陸
        var seaShare = rules.SeaShare.Min + (float)random.NextDouble() * (rules.SeaShare.Max - rules.SeaShare.Min);
        var landCount = count - (int)Math.Round(count * seaShare, MidpointRounding.AwayFromZero);
        var islandSizes = new List<int>();
        for (var i = 0; i < rules.IslandSeeds; i++)
        {
            islandSizes.Add(rules.IslandRegions.Pick(random));
        }
        var continentTarget = Math.Max(rules.ContinentSeeds, landCount - islandSizes.Sum());
        continentTarget = Math.Min(continentTarget, landCount);
        var land = new bool[count];
        PlaceContinents(grid, rules.ContinentSeeds, continentTarget, prophetRegion, land, random);
        PlaceRegionIslands(grid, islandSizes, land, random);

        // 沿岸と陸の種類
        var terrain = new BaseTerrain[count];
        for (var region = 0; region < count; region++)
        {
            if (!land[region])
            {
                terrain[region] = BaseTerrain.Sea;
            }
            else if (grid.Neighbors(region).Any(other => !land[other]))
            {
                terrain[region] = BaseTerrain.Coast;
            }
            else
            {
                terrain[region] = PickLandKind(rules, boundary[region], random);
            }
        }

        var (elevation, isLand) = BuildTiles(rules, grid, terrain, random);
        PlaceTileIslands(rules, grid, terrain, boundary, elevation, isLand, random);
        Smooth(rules, grid.Columns * rules.RegionSize, grid.Bands * rules.RegionSize, elevation, isLand);

        var values = new EnvironmentValues[count];
        for (var region = 0; region < count; region++)
        {
            values[region] = InitialValues(rules, world, grid.BandOf(region), terrain[region]);
        }
        return new RegionalWorld(grid, rules.RegionSize, plates, boundary, terrain, elevation, isLand, values, prophetRegion);
    }

    private static int[] AssignPlates(PlanetGrid grid, int plateCount, Random random)
    {
        var order = Enumerable.Range(0, grid.Count).ToList();
        Shuffle(order, random);
        var seeds = order.Take(plateCount).ToArray();
        var plates = new int[grid.Count];
        for (var region = 0; region < grid.Count; region++)
        {
            var best = 0;
            for (var i = 1; i < seeds.Length; i++)
            {
                if (grid.Distance(region, seeds[i]) < grid.Distance(region, seeds[best]))
                {
                    best = i;
                }
            }
            plates[region] = best;
        }
        return plates;
    }

    private static void PlaceContinents(PlanetGrid grid, int seedCount, int target, int prophetRegion, bool[] land, Random random)
    {
        land[prophetRegion] = true;
        var others = Enumerable.Range(0, grid.Count).Where(region => region != prophetRegion).ToList();
        Shuffle(others, random);
        foreach (var region in others.Take(Math.Max(0, Math.Min(seedCount, target) - 1)))
        {
            land[region] = true;
        }
        GrowLand(grid, land, target, random);
    }

    // 陸の数が target になるまで、陸に隣り合う海の地域から 1 つずつ陸にする（無ければ、海のどこかを島として陸にする）
    private static void GrowLand(PlanetGrid grid, bool[] land, int target, Random random)
    {
        while (land.Count(isLand => isLand) < target)
        {
            var candidates = new List<int>();
            for (var region = 0; region < grid.Count; region++)
            {
                if (!land[region] && grid.Neighbors(region).Any(other => land[other]))
                {
                    candidates.Add(region);
                }
            }
            if (candidates.Count == 0)
            {
                for (var region = 0; region < grid.Count; region++)
                {
                    if (!land[region])
                    {
                        candidates.Add(region);
                    }
                }
            }
            land[candidates[random.Next(candidates.Count)]] = true;
        }
    }

    private static void PlaceRegionIslands(PlanetGrid grid, List<int> sizes, bool[] land, Random random)
    {
        foreach (var size in sizes)
        {
            var candidates = Enumerable.Range(0, grid.Count)
                .Where(region => !land[region] && grid.Neighbors(region).All(other => !land[other])).ToList();
            if (candidates.Count == 0)
            {
                GrowLand(grid, land, land.Count(isLand => isLand) + size, random);
                continue;
            }
            var first = candidates[random.Next(candidates.Count)];
            land[first] = true;
            if (size < 2)
            {
                continue;
            }
            var seconds = grid.Neighbors(first)
                .Where(region => !land[region] && grid.Neighbors(region).All(other => other == first || !land[other])).ToList();
            if (seconds.Count == 0)
            {
                GrowLand(grid, land, land.Count(isLand => isLand) + 1, random);
                continue;
            }
            land[seconds[random.Next(seconds.Count)]] = true;
        }
    }

    private static BaseTerrain PickLandKind(PlanetRules rules, bool isBoundary, Random random)
    {
        var plain = (double)rules.PlainWeight;
        var forest = (double)rules.ForestWeight;
        var mountain = rules.MountainWeight * (isBoundary ? rules.BoundaryMountainMultiplier : 1.0);
        var roll = random.NextDouble() * (plain + forest + mountain);
        if (roll < plain)
        {
            return BaseTerrain.Plain;
        }
        return roll < plain + forest ? BaseTerrain.Forest : BaseTerrain.Mountain;
    }

    private static IntRange LandRange(PlanetRules rules, BaseTerrain terrain) => terrain switch
    {
        BaseTerrain.Coast => rules.CoastLand,
        BaseTerrain.Plain => rules.PlainLand,
        BaseTerrain.Forest => rules.ForestLand,
        _ => rules.MountainLand,
    };

    private static (int[] Elevation, bool[] Land) BuildTiles(PlanetRules rules, PlanetGrid grid, BaseTerrain[] terrain, Random random)
    {
        var size = rules.RegionSize;
        var width = grid.Columns * size;
        var height = grid.Bands * size;
        var elevation = new int[width * height];
        var land = new bool[width * height];
        for (var region = 0; region < grid.Count; region++)
        {
            var originX = grid.ColumnOf(region) * size;
            var originY = grid.BandOf(region) * size;
            for (var ly = 0; ly < size; ly++)
            {
                for (var lx = 0; lx < size; lx++)
                {
                    var sea = terrain[region] == BaseTerrain.Sea || (terrain[region] == BaseTerrain.Coast && IsCoastSea(rules, grid, terrain, region, lx, ly));
                    var index = (originY + ly) * width + originX + lx;
                    land[index] = !sea;
                    elevation[index] = terrain[region] == BaseTerrain.Sea
                        ? rules.DeepSea.Pick(random)
                        : sea ? rules.ShallowSea.Pick(random) : LandRange(rules, terrain[region]).Pick(random);
                }
            }
        }
        return (elevation, land);
    }

    // 沿岸の地域のタイルが、海の隣の向きの端から CoastSeaWidth 以内なら海にする（斜めの隣の海は角のタイル）
    private static bool IsCoastSea(PlanetRules rules, PlanetGrid grid, BaseTerrain[] terrain, int region, int lx, int ly)
    {
        var size = rules.RegionSize;
        var width = rules.CoastSeaWidth;
        for (var dy = -1; dy <= 1; dy++)
        {
            for (var dx = -1; dx <= 1; dx++)
            {
                if ((dx == 0 && dy == 0) || !grid.TryNeighbor(region, dx, dy, out var other) || terrain[other] != BaseTerrain.Sea)
                {
                    continue;
                }
                var inX = dx == 0 || (dx < 0 ? lx < width : lx >= size - width);
                var inY = dy == 0 || (dy < 0 ? ly < width : ly >= size - width);
                if (inX && inY)
                {
                    return true;
                }
            }
        }
        return false;
    }

    private static void PlaceTileIslands(
        PlanetRules rules, PlanetGrid grid, BaseTerrain[] terrain, bool[] boundary, int[] elevation, bool[] land, Random random)
    {
        var size = rules.RegionSize;
        var width = grid.Columns * size;
        var height = grid.Bands * size;
        var distance = DistanceFromLand(width, height, land);
        var arcTiles = new HashSet<int>();

        // 島弧: プレートの境目の海の地域
        for (var region = 0; region < grid.Count; region++)
        {
            if (terrain[region] != BaseTerrain.Sea || !boundary[region] || random.NextDouble() >= rules.ArcChance)
            {
                continue;
            }
            for (var i = 0; i < rules.ArcIslands; i++)
            {
                var starts = RegionSeaTiles(grid, size, width, region, land, tile => !TouchesLand(tile, width, height, land, null));
                GrowIsland(starts, rules.ArcSize.Pick(random), width, height, land, null, random, arcTiles);
            }
        }

        // ホットスポット: 陸の地域と隣り合わない海の地域
        var lonely = Enumerable.Range(0, grid.Count)
            .Where(region => terrain[region] == BaseTerrain.Sea && grid.Neighbors(region).All(other => terrain[other] == BaseTerrain.Sea)).ToList();
        Shuffle(lonely, random);
        foreach (var region in lonely.Take(rules.HotspotCount))
        {
            var starts = RegionSeaTiles(grid, size, width, region, land, tile => !TouchesLand(tile, width, height, land, null));
            GrowIsland(starts, rules.HotspotSize.Pick(random), width, height, land, null, random, null);
        }

        // 大陸棚の島: 沿岸の地域の海のタイルのうち、陸からいちばん遠いもの
        for (var region = 0; region < grid.Count; region++)
        {
            if (terrain[region] != BaseTerrain.Coast || random.NextDouble() >= rules.ShelfChance)
            {
                continue;
            }
            var seaTiles = RegionSeaTiles(grid, size, width, region, land, _ => true);
            if (seaTiles.Count == 0)
            {
                continue;
            }
            var farthest = seaTiles.Max(tile => distance[tile]);
            var starts = seaTiles.Where(tile => distance[tile] == farthest && !TouchesLand(tile, width, height, land, null)).ToList();
            GrowIsland(starts, rules.ShelfSize.Pick(random), width, height, land, tile => distance[tile] >= farthest, random, null);
        }

        // 標高: 海に隣り合うタイルは沿岸、ほかは平野。島弧の島の内側は火山
        for (var tile = 0; tile < land.Length; tile++)
        {
            if (!land[tile] || elevation[tile] != 0)
            {
                continue;
            }
            elevation[tile] = !TouchesSea(tile, width, height, land)
                ? (arcTiles.Contains(tile) ? rules.VolcanoLand.Pick(random) : rules.PlainLand.Pick(random))
                : rules.CoastLand.Pick(random);
        }
    }

    // 島のタイルは、標高を 0 のまま置き、あとで決める
    private static void GrowIsland(
        List<int> starts, int target, int width, int height, bool[] land, Func<int, bool>? extra, Random random, HashSet<int>? arc)
    {
        if (starts.Count == 0)
        {
            return;
        }
        var island = new HashSet<int>();
        var first = starts[random.Next(starts.Count)];
        Add(first);
        while (island.Count < target)
        {
            var candidates = new SortedSet<int>();
            foreach (var tile in island)
            {
                foreach (var other in TileNeighbors(tile, width, height))
                {
                    if (!land[other] && !island.Contains(other) && (extra is null || extra(other)) && !TouchesLand(other, width, height, land, island))
                    {
                        candidates.Add(other);
                    }
                }
            }
            if (candidates.Count == 0)
            {
                break;
            }
            Add(candidates.ElementAt(random.Next(candidates.Count)));
        }

        void Add(int tile)
        {
            island.Add(tile);
            land[tile] = true;
            arc?.Add(tile);
        }
    }

    private static List<int> RegionSeaTiles(PlanetGrid grid, int size, int width, int region, bool[] land, Func<int, bool> filter)
    {
        var tiles = new List<int>();
        for (var ly = 0; ly < size; ly++)
        {
            for (var lx = 0; lx < size; lx++)
            {
                var tile = (grid.BandOf(region) * size + ly) * width + grid.ColumnOf(region) * size + lx;
                if (!land[tile] && filter(tile))
                {
                    tiles.Add(tile);
                }
            }
        }
        return tiles;
    }

    private static IEnumerable<int> TileNeighbors(int tile, int width, int height)
    {
        var x = tile % width;
        var y = tile / width;
        for (var dy = -1; dy <= 1; dy++)
        {
            for (var dx = -1; dx <= 1; dx++)
            {
                var ny = y + dy;
                if ((dx == 0 && dy == 0) || ny < 0 || ny >= height)
                {
                    continue;
                }
                yield return ny * width + ((x + dx) % width + width) % width;
            }
        }
    }

    // 自分の島（own）のほかに、陸のタイルが 8 方向で隣り合うか
    private static bool TouchesLand(int tile, int width, int height, bool[] land, HashSet<int>? own) =>
        TileNeighbors(tile, width, height).Any(other => land[other] && (own is null || !own.Contains(other)));

    private static bool TouchesSea(int tile, int width, int height, bool[] land) =>
        TileNeighbors(tile, width, height).Any(other => !land[other]);

    // 陸のタイルからの歩数（8 方向。陸は 0）
    private static int[] DistanceFromLand(int width, int height, bool[] land)
    {
        var distance = new int[land.Length];
        var queue = new Queue<int>();
        for (var tile = 0; tile < land.Length; tile++)
        {
            distance[tile] = land[tile] ? 0 : int.MaxValue;
            if (land[tile])
            {
                queue.Enqueue(tile);
            }
        }
        while (queue.Count > 0)
        {
            var tile = queue.Dequeue();
            foreach (var other in TileNeighbors(tile, width, height))
            {
                if (distance[other] == int.MaxValue)
                {
                    distance[other] = distance[tile] + 1;
                    queue.Enqueue(other);
                }
            }
        }
        return distance;
    }

    // 8 方向の隣のうち同じ種類（陸どうし・海どうし）のタイルと自分の平均にする。全タイルを前の値から同時に計算する
    private static void Smooth(PlanetRules rules, int width, int height, int[] elevation, bool[] land)
    {
        for (var pass = 0; pass < rules.SmoothingPasses; pass++)
        {
            var next = new int[elevation.Length];
            for (var tile = 0; tile < elevation.Length; tile++)
            {
                long sum = elevation[tile];
                var n = 1;
                foreach (var other in TileNeighbors(tile, width, height))
                {
                    if (land[other] == land[tile])
                    {
                        sum += elevation[other];
                        n++;
                    }
                }
                var average = (int)Math.Round((double)sum / n, MidpointRounding.AwayFromZero);
                next[tile] = land[tile] ? Math.Max(1, average) : Math.Min(-1, average);
            }
            Array.Copy(next, elevation, elevation.Length);
        }
    }

    private static EnvironmentValues InitialValues(PlanetRules rules, WorldRules world, int band, BaseTerrain terrain)
    {
        var forest = world.Initial.ForestAmount;
        var water = world.Initial.WaterAmount;
        var temperature = world.Initial.Temperature + rules.BandTemperatureOffsets[band];
        switch (terrain)
        {
            case BaseTerrain.Coast:
                water += rules.CoastWaterOffset;
                break;
            case BaseTerrain.Mountain:
                temperature += rules.MountainTemperatureOffset;
                break;
            case BaseTerrain.Forest:
                forest += rules.ForestForestOffset;
                break;
            case BaseTerrain.Sea:
                forest += rules.SeaForestOffset;
                break;
        }
        return new EnvironmentValues(
            Math.Clamp(forest, world.Min.ForestAmount, world.Max.ForestAmount),
            Math.Clamp(water, world.Min.WaterAmount, world.Max.WaterAmount),
            Math.Clamp(temperature, world.Min.Temperature, world.Max.Temperature),
            world.Initial.ChaosLevel,
            world.Initial.HarmonyLevel);
    }

    private static void Shuffle(List<int> list, Random random)
    {
        for (var i = list.Count - 1; i > 0; i--)
        {
            var j = random.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}
