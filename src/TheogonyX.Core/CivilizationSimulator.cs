namespace TheogonyX.Core;

/// <summary>
/// 地域ごとの技術・文化の伸びと自律建築（docs/domain/emergent-progression.md）、
/// 老朽化・建て増し・ミクロ沈下（docs/domain/strata.md）を 1 年ぶん計算する。
/// </summary>
public static class CivilizationSimulator
{
    /// <summary>
    /// 1 年進める。<paramref name="civilization"/> を書き換える。環境の値を変えた後の <paramref name="world"/> で呼ぶ。
    /// 建てる場所と寿命は、シード・年・地域から作った乱数で選ぶ（乱数の状態をセーブしなくても、読み込んだ後に同じ結果になる）。
    /// 順番は、寿命が来た建物を廃墟にする → 年数のたった廃墟を沈める → 値を伸ばす → 足りない数の建物を建てる。
    /// </summary>
    public static void AdvanceYear(
        Civilization civilization, WorldState world, IReadOnlyList<Dogma> dogmas, TerrainRules terrain, CivilizationRules rules, int seed,
        Func<Dogma, double>? share = null, int? prophetRegion = null)
    {
        ArgumentNullException.ThrowIfNull(civilization);
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(dogmas);
        ArgumentNullException.ThrowIfNull(terrain);
        ArgumentNullException.ThrowIfNull(rules);

        foreach (var building in civilization.Buildings.Where(building => building.CollapseYear <= world.Year).ToList())
        {
            civilization.Collapse(building, world, dogmas, rules);
        }
        var idleRuins = civilization.Ruins
            .Where(ruin => !civilization.HasBuildingAt(ruin.Column, ruin.Row) && world.Year - ruin.RuinedYear >= rules.Strata.RuinBurialYears)
            .ToList();
        foreach (var ruin in idleRuins)
        {
            civilization.BuryIdleRuin(ruin, world, dogmas, rules);
        }

        var tiles = TerrainClassifier.ClassifyTiles(world, terrain);
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
        // 禁忌の伸びの倍率は、信者の割合の分だけ効く（全員が信じていれば TabooGrowthFactor そのまま）
        var tabooFactor = tabooShare <= 0 ? 1.0 : tabooShare >= 1 ? rules.TabooGrowthFactor : 1 - tabooShare * (1 - rules.TabooGrowthFactor);

        for (var region = 0; region < rules.Regions.Count; region++)
        {
            var influence = Influence(region, rules, prophetRegion);
            var factor = EnvironmentMultiplier(tiles, region, rules) * tabooFactor;
            var before = civilization.Regions[region];
            // 値は下がらない（ドグマの強度が負でも伸びは 0 で止める）
            var tech = Math.Clamp(before.Tech + Math.Max(0, (rules.NaturalGrowthPerYear + techBonus * influence) * factor), 0, 1);
            var culture = Math.Clamp(before.Culture + Math.Max(0, (rules.NaturalGrowthPerYear + cultureBonus * influence) * factor), 0, 1);

            Random? random = null;
            Random Rng() => random ??= new Random(unchecked((int)CrowdHash.Hash((uint)seed, (uint)world.Year, (uint)region)));

            // 立っている数が段階（越えた閾値の数）より少なければ、足りない数だけ建てる。空いた枠が無ければ次の年にまた建てようとする
            var techStage = Math.Max(before.TechStage, Civilization.StageOf(tech, rules));
            var cultureStage = Math.Max(before.CultureStage, Civilization.StageOf(culture, rules));
            civilization.SetRegion(region, new RegionCivilization(tech, culture, techStage, cultureStage));
            while (CountStanding(civilization, region, BuildingType.Workshop) < techStage
                && TryBuild(civilization, tiles, region, BuildingType.Workshop, world.Year, rules, Rng))
            {
            }
            while (CountStanding(civilization, region, BuildingType.Shrine) < cultureStage
                && TryBuild(civilization, tiles, region, BuildingType.Shrine, world.Year, rules, Rng))
            {
            }
        }
    }

    /// <summary>ドグマの効き目の係数。予言者の地域で 1、離れるほど直線的に弱まり、<see cref="CivilizationRules.ZeroInfluenceDistance"/> 地域離れると 0。</summary>
    public static double Influence(int region, CivilizationRules rules, int? prophetRegion = null)
    {
        ArgumentNullException.ThrowIfNull(rules);
        var distance = Math.Abs(region - (prophetRegion ?? rules.Regions.RegionOf(rules.ProphetColumn)));
        return Math.Max(0, 1 - (double)distance / rules.ZeroInfluenceDistance);
    }

    /// <summary>
    /// 地域の環境による伸びの倍率。水場でないマスのうちいちばん悪い（倍率の小さい）タイル種別で決め、
    /// それが草地で、地域に水場のマスがあれば、水場の倍率を使う。
    /// </summary>
    /// <param name="tiles">マスごとのタイル種別。添字は [列, 行]。</param>
    public static double EnvironmentMultiplier(TileType[,] tiles, int region, CivilizationRules rules)
    {
        ArgumentNullException.ThrowIfNull(tiles);
        ArgumentNullException.ThrowIfNull(rules);
        var first = rules.Regions.FirstColumn(region);
        TileType? worst = null;
        var hasWater = false;
        for (var column = first; column < first + rules.Regions.ColumnsPerRegion; column++)
        {
            for (var row = 0; row < tiles.GetLength(1); row++)
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
        if (worst is null || (worst == TileType.Grassland && hasWater))
        {
            return rules.EnvironmentMultipliers[TileType.Water];
        }
        return rules.EnvironmentMultipliers[worst.Value];
    }

    private static int CountStanding(Civilization civilization, int region, BuildingType type) =>
        civilization.Buildings.Count(building => building.Region == region && building.Type == type);

    // 地域の 2 × 2 の枠のうち、建物が無く、4 マスとも草地（水場・異常のタイルでない）の枠から乱数で選んで建てる。
    // 廃墟のある枠（建て増し）を先に選び、無ければ空いている枠から選ぶ。寿命も同じ乱数で決める。住人が家を建てるのにも使う
    internal static bool TryBuild(
        Civilization civilization, TileType[,] tiles, int region, BuildingType type, int year, CivilizationRules rules, Func<Random> random)
    {
        var first = rules.Regions.FirstColumn(region);
        var ruined = new List<(int Column, int Row)>();
        var free = new List<(int Column, int Row)>();
        for (var column = first; column < first + rules.Regions.ColumnsPerRegion; column += 2)
        {
            for (var row = 0; row + 1 < rules.Rows; row += 2)
            {
                if (IsBuildable(tiles, column, row) && !civilization.HasBuildingAt(column, row))
                {
                    (civilization.RuinAt(column, row) is null ? free : ruined).Add((column, row));
                }
            }
        }
        var candidates = ruined.Count > 0 ? ruined : free;
        if (candidates.Count == 0)
        {
            return false;
        }
        var (c, r) = candidates[random().Next(candidates.Count)];
        if (ruined.Count > 0)
        {
            StrataTransitions.Next(StrataState.Ruin, StrataTrigger.Overbuild);
        }
        var lifespan = random().Next(rules.Strata.LifespanMin, rules.Strata.LifespanMax + 1);
        civilization.AddBuilding(type, region, c, r, year + lifespan);
        return true;
    }

    private static bool IsBuildable(TileType[,] tiles, int column, int row) =>
        tiles[column, row] == TileType.Grassland && tiles[column + 1, row] == TileType.Grassland
        && tiles[column, row + 1] == TileType.Grassland && tiles[column + 1, row + 1] == TileType.Grassland;
}
