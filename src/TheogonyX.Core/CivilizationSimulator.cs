namespace TheogonyX.Core;

/// <summary>地域ごとの技術・文化の伸びと、閾値を越えたときの自律建築を 1 年ぶん計算する（docs/domain/emergent-progression.md）。</summary>
public static class CivilizationSimulator
{
    /// <summary>
    /// 1 年進める。<paramref name="civilization"/> を書き換える。環境の値を変えた後の <paramref name="world"/> で呼ぶ。
    /// 建てる場所は、シード・年・地域から作った乱数で選ぶ（乱数の状態をセーブしなくても、読み込んだ後に同じ結果になる）。
    /// </summary>
    public static void AdvanceYear(
        Civilization civilization, WorldState world, IReadOnlyList<Dogma> dogmas, TerrainRules terrain, CivilizationRules rules, int seed)
    {
        ArgumentNullException.ThrowIfNull(civilization);
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(dogmas);
        ArgumentNullException.ThrowIfNull(terrain);
        ArgumentNullException.ThrowIfNull(rules);

        var tiles = TerrainClassifier.ClassifyTiles(world, terrain);
        var techBonus = 0.0;
        var cultureBonus = 0.0;
        var taboo = false;
        foreach (var dogma in dogmas)
        {
            switch (dogma.Type)
            {
                case DogmaType.Worship:
                    cultureBonus += rules.WorshipCulturePerYear * dogma.Intensity;
                    break;
                case DogmaType.Destruction:
                    techBonus += rules.DestructionTechPerYear * dogma.Intensity;
                    break;
                case DogmaType.Taboo:
                    taboo = true;
                    break;
            }
        }

        for (var region = 0; region < rules.Regions.Count; region++)
        {
            var influence = Influence(region, rules);
            var factor = EnvironmentMultiplier(tiles, region, rules) * (taboo ? rules.TabooGrowthFactor : 1.0);
            var before = civilization.Regions[region];
            // 値は下がらない（ドグマの強度が負でも伸びは 0 で止める）
            var tech = Math.Clamp(before.Tech + Math.Max(0, (rules.NaturalGrowthPerYear + techBonus * influence) * factor), 0, 1);
            var culture = Math.Clamp(before.Culture + Math.Max(0, (rules.NaturalGrowthPerYear + cultureBonus * influence) * factor), 0, 1);

            Random? random = null;
            Random Rng() => random ??= new Random(unchecked((int)CrowdHash.Hash((uint)seed, (uint)world.Year, (uint)region)));

            // 越えた閾値 1 つにつき 1 つ建てる。空いた枠が無くても、閾値は越えたものとして数える
            var techStage = Civilization.StageOf(tech, rules);
            for (var stage = before.TechStage; stage < techStage; stage++)
            {
                TryBuild(civilization, tiles, region, BuildingType.Workshop, rules, Rng);
            }
            var cultureStage = Civilization.StageOf(culture, rules);
            for (var stage = before.CultureStage; stage < cultureStage; stage++)
            {
                TryBuild(civilization, tiles, region, BuildingType.Shrine, rules, Rng);
            }
            civilization.SetRegion(region, new RegionCivilization(
                tech, culture, Math.Max(before.TechStage, techStage), Math.Max(before.CultureStage, cultureStage)));
        }
    }

    /// <summary>ドグマの効き目の係数。予言者の地域で 1、離れるほど直線的に弱まり、<see cref="CivilizationRules.ZeroInfluenceDistance"/> 地域離れると 0。</summary>
    public static double Influence(int region, CivilizationRules rules)
    {
        ArgumentNullException.ThrowIfNull(rules);
        var distance = Math.Abs(region - rules.Regions.RegionOf(rules.ProphetColumn));
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

    // 地域の 2 × 2 の枠のうち、建物が無く、4 マスとも草地（水場・異常のタイルでない）の枠から乱数で選んで建てる
    private static void TryBuild(
        Civilization civilization, TileType[,] tiles, int region, BuildingType type, CivilizationRules rules, Func<Random> random)
    {
        var first = rules.Regions.FirstColumn(region);
        var free = new List<(int Column, int Row)>();
        for (var column = first; column < first + rules.Regions.ColumnsPerRegion; column += 2)
        {
            for (var row = 0; row + 1 < rules.Rows; row += 2)
            {
                if (IsBuildable(tiles, column, row) && !civilization.Buildings.Any(building => building.Column == column && building.Row == row))
                {
                    free.Add((column, row));
                }
            }
        }
        if (free.Count == 0)
        {
            return;
        }
        var (c, r) = free[random().Next(free.Count)];
        civilization.AddBuilding(type, region, c, r);
    }

    private static bool IsBuildable(TileType[,] tiles, int column, int row) =>
        tiles[column, row] == TileType.Grassland && tiles[column + 1, row] == TileType.Grassland
        && tiles[column, row + 1] == TileType.Grassland && tiles[column + 1, row + 1] == TileType.Grassland;
}
