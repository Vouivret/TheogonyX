namespace TheogonyX.Core;

/// <summary>
/// 新しい星（<see cref="RegionalWorld"/>）の文明の 1 年（plan/feat-planet-grid-1b.md の段 1b-2）。
/// 今の <see cref="CivilizationSimulator"/> と同じ決め方で、地域を 2 次元の格子（<see cref="PlanetGrid"/>）、タイルを星のタイルにしたもの。
/// 地域の数は <see cref="PlanetGrid.Count"/>（<see cref="Civilization"/> は <see cref="Civilization.CreateInitial(int, CivilizationRules)"/> で作る）。
/// </summary>
public static class PlanetCivilizationSimulator
{
    /// <summary>
    /// 1 年進める。<paramref name="civilization"/> を書き換える。環境の値を変えた後の <paramref name="world"/> で呼ぶ。
    /// 順番は、寿命が来た建物を廃墟にする → 年数のたった廃墟を沈める → 値を伸ばす → 足りない数の建物を建てる。
    /// 建てる場所と寿命は、シード・年・地域から作った乱数で選ぶ。
    /// </summary>
    /// <param name="tiles">マスごとのタイル種別（<see cref="PlanetTiles.Classify"/>）。添字は [列, 行]。</param>
    /// <param name="prophetRegion">ドグマの効きの中心の地域。null なら <see cref="RegionalWorld.ProphetRegion"/>。</param>
    public static void AdvanceYear(
        Civilization civilization, RegionalWorld world, TileType[,] tiles, IReadOnlyList<Dogma> dogmas, CivilizationRules rules, int seed,
        Func<Dogma, double>? share = null, int? prophetRegion = null)
    {
        ArgumentNullException.ThrowIfNull(civilization);
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(tiles);
        ArgumentNullException.ThrowIfNull(dogmas);
        ArgumentNullException.ThrowIfNull(rules);
        if (civilization.Regions.Count != world.Grid.Count)
        {
            throw new ArgumentException($"地域の数が合わない: civilization={civilization.Regions.Count} world={world.Grid.Count}", nameof(civilization));
        }

        // 地域の並びは新しい星の格子（建てる枠は、この並びのタイルの範囲から選ぶ）
        rules = rules with { Layout = new PlanetRegionLayout(world.Grid, world.RegionSize) };
        var state = world.ToWorldState();
        foreach (var building in civilization.Buildings.Where(building => building.CollapseYear <= world.Year).ToList())
        {
            civilization.Collapse(building, state, dogmas, rules);
        }
        var idleRuins = civilization.Ruins
            .Where(ruin => !civilization.HasBuildingAt(ruin.Column, ruin.Row) && world.Year - ruin.RuinedYear >= rules.Strata.RuinBurialYears)
            .ToList();
        foreach (var ruin in idleRuins)
        {
            civilization.BuryIdleRuin(ruin, state, dogmas, rules);
        }

        var techBonus = 0.0;
        var cultureBonus = 0.0;
        var tabooShare = 0.0;
        foreach (var dogma in dogmas)
        {
            var weight = share?.Invoke(dogma) ?? 1.0;
            switch (dogma.Type)
            {
                case DogmaType.Worship:
                    cultureBonus += rules.WorshipCulturePerYear * dogma.Intensity * weight;
                    break;
                case DogmaType.Destruction:
                    techBonus += rules.DestructionTechPerYear * dogma.Intensity * weight;
                    break;
                case DogmaType.Taboo:
                    tabooShare = Math.Max(tabooShare, weight);
                    break;
            }
        }
        var tabooFactor = tabooShare <= 0 ? 1.0 : tabooShare >= 1 ? rules.TabooGrowthFactor : 1 - tabooShare * (1 - rules.TabooGrowthFactor);

        var center = prophetRegion ?? world.ProphetRegion;
        for (var region = 0; region < world.Grid.Count; region++)
        {
            var influence = Influence(world.Grid, region, center, rules);
            var factor = EnvironmentMultiplier(tiles, world, region, rules) * tabooFactor;
            var before = civilization.Regions[region];
            var tech = Math.Clamp(before.Tech + Math.Max(0, (rules.NaturalGrowthPerYear + techBonus * influence) * factor), 0, 1);
            var culture = Math.Clamp(before.Culture + Math.Max(0, (rules.NaturalGrowthPerYear + cultureBonus * influence) * factor), 0, 1);

            Random? random = null;
            Random Rng() => random ??= new Random(unchecked((int)CrowdHash.Hash((uint)seed, (uint)world.Year, (uint)region)));

            var techStage = Math.Max(before.TechStage, Civilization.StageOf(tech, rules));
            var cultureStage = Math.Max(before.CultureStage, Civilization.StageOf(culture, rules));
            civilization.SetRegion(region, new RegionCivilization(tech, culture, techStage, cultureStage));
            while (CountStanding(civilization, region, BuildingType.Workshop) < techStage
                && CivilizationSimulator.TryBuild(civilization, tiles, region, BuildingType.Workshop, world.Year, rules, Rng))
            {
            }
            while (CountStanding(civilization, region, BuildingType.Shrine) < cultureStage
                && CivilizationSimulator.TryBuild(civilization, tiles, region, BuildingType.Shrine, world.Year, rules, Rng))
            {
            }
        }
    }

    /// <summary>ドグマの効き目の係数。予言者の地域で 1、格子の距離が遠いほど直線的に弱まり、<see cref="CivilizationRules.ZeroInfluenceDistance"/> で 0。</summary>
    public static double Influence(PlanetGrid grid, int region, int prophetRegion, CivilizationRules rules)
    {
        ArgumentNullException.ThrowIfNull(grid);
        ArgumentNullException.ThrowIfNull(rules);
        return Math.Max(0, 1 - (double)grid.Distance(prophetRegion, region) / rules.ZeroInfluenceDistance);
    }

    /// <summary>
    /// 地域の環境による伸びの倍率。陸のタイルのうちいちばん悪い（倍率の小さい）タイル種別で決め、
    /// それが草地で、地域に水場のタイル（海・沿岸の海）があれば、水場の倍率を使う。陸が無い地域は 0（伸びない）。
    /// </summary>
    public static double EnvironmentMultiplier(TileType[,] tiles, RegionalWorld world, int region, CivilizationRules rules)
    {
        ArgumentNullException.ThrowIfNull(tiles);
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(rules);
        // 海の地域は、島のタイルがあっても文明は伸びない（AC-regional-environment-008）
        if (world.Terrain[region] == BaseTerrain.Sea)
        {
            return 0;
        }
        var (firstColumn, firstRow) = FirstTile(world, region);
        TileType? worst = null;
        var hasWater = false;
        for (var column = firstColumn; column < firstColumn + world.RegionSize; column++)
        {
            for (var row = firstRow; row < firstRow + world.RegionSize; row++)
            {
                var tileType = tiles[column, row];
                if (tileType == TileType.Water)
                {
                    hasWater = true;
                }
                else if (worst is null || rules.EnvironmentMultipliers[tileType] < rules.EnvironmentMultipliers[worst.Value])
                {
                    worst = tileType;
                }
            }
        }
        if (worst is null)
        {
            return 0;
        }
        return worst == TileType.Grassland && hasWater ? rules.EnvironmentMultipliers[TileType.Water] : rules.EnvironmentMultipliers[worst.Value];
    }

    private static (int Column, int Row) FirstTile(RegionalWorld world, int region) =>
        (world.Grid.ColumnOf(region) * world.RegionSize, world.Grid.BandOf(region) * world.RegionSize);

    private static int CountStanding(Civilization civilization, int region, BuildingType type) =>
        civilization.Buildings.Count(building => building.Region == region && building.Type == type);

    /// <summary>
    /// 地域ごとに、住人が住みつける（海の地域でなく、建てられる 2 × 2 の枠が 1 つ以上ある）か。山・氷河だけの地域は false。
    /// </summary>
    public static bool[] Settleable(RegionalWorld world, TileType[,] tiles, CivilizationRules rules)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(tiles);
        ArgumentNullException.ThrowIfNull(rules);
        var layout = new PlanetRegionLayout(world.Grid, world.RegionSize);
        var result = new bool[world.Grid.Count];
        for (var region = 0; region < result.Length; region++)
        {
            if (world.Terrain[region] == BaseTerrain.Sea)
            {
                continue;
            }
            var (firstColumn, firstRow) = FirstTile(world, region);
            for (var column = firstColumn; column < firstColumn + world.RegionSize && !result[region]; column += 2)
            {
                for (var row = firstRow; row < firstRow + world.RegionSize && !result[region]; row += 2)
                {
                    result[region] = layout.IsBuildable(tiles[column, row]) && layout.IsBuildable(tiles[column + 1, row])
                        && layout.IsBuildable(tiles[column, row + 1]) && layout.IsBuildable(tiles[column + 1, row + 1]);
                }
            }
        }
        return result;
    }
}
