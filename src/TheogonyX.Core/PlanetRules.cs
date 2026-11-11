namespace TheogonyX.Core;

/// <summary>
/// 星の格子と、星を作る処理の定義（docs/domain/regional-environment.md・plan/feat-planet-grid-1a.md）。値は仮（TODO(AI_UNSURE)）。
/// </summary>
/// <param name="Columns">地域の列の数（横は輪）。</param>
/// <param name="Bands">地域の段の数（0 が北の極）。</param>
/// <param name="RegionSize">1 地域の一辺のタイルの数。</param>
/// <param name="PlateCount">プレートの数。</param>
/// <param name="ContinentSeeds">大陸の種の数。</param>
/// <param name="SeaShare">海の地域の割合の範囲。</param>
/// <param name="PlainWeight">陸の種類の重み（平野）。</param>
/// <param name="ForestWeight">陸の種類の重み（森林）。</param>
/// <param name="MountainWeight">陸の種類の重み（山岳）。</param>
/// <param name="BoundaryMountainMultiplier">プレートの境目で山岳の重みに掛ける倍率。</param>
/// <param name="DeepSea">海の地域のタイルの標高（m）。</param>
/// <param name="ShallowSea">沿岸の海のタイルの標高（m）。</param>
/// <param name="CoastLand">沿岸の地域の陸のタイルの標高（m）。</param>
/// <param name="PlainLand">平野の標高（m）。</param>
/// <param name="ForestLand">森林の標高（m）。</param>
/// <param name="MountainLand">山岳の標高（m）。</param>
/// <param name="VolcanoLand">島弧の島の内側の標高（m）。</param>
/// <param name="CoastSeaWidth">沿岸の地域の海のタイルの幅。</param>
/// <param name="IslandSeeds">地域の島の種の数。</param>
/// <param name="IslandRegions">地域の島の大きさ（地域の数）。</param>
/// <param name="ArcChance">島弧: プレートの境目の海の地域ごとの確率。</param>
/// <param name="ArcIslands">島弧: 1 地域に置く島の数。</param>
/// <param name="ArcSize">島弧: 島の大きさ（タイルの数）。</param>
/// <param name="HotspotCount">ホットスポットの島の数。</param>
/// <param name="HotspotSize">ホットスポットの島の大きさ。</param>
/// <param name="ShelfChance">大陸棚の島: 沿岸の地域ごとの確率。</param>
/// <param name="ShelfSize">大陸棚の島の大きさ。</param>
/// <param name="SmoothingPasses">標高をなめらかにする回数。</param>
/// <param name="BandTemperatureOffsets">段ごとの気温の補正（北の極から順に、段の数だけ）。</param>
/// <param name="CoastWaterOffset">沿岸の水資源の補正。</param>
/// <param name="MountainTemperatureOffset">山岳の気温の補正。</param>
/// <param name="ForestForestOffset">森林の森林量の補正。</param>
/// <param name="SeaForestOffset">海の森林量の補正。</param>
/// <param name="SmoothRate">隣の地域の平均へ近づける割合（1 年あたり）。</param>
/// <param name="ZeroInfluenceDistance">ドグマの効き目が 0 になる、予言者の地域からの距離。</param>
public sealed record PlanetRules(
    int Columns,
    int Bands,
    int RegionSize,
    int PlateCount,
    int ContinentSeeds,
    FloatRange SeaShare,
    float PlainWeight,
    float ForestWeight,
    float MountainWeight,
    float BoundaryMountainMultiplier,
    IntRange DeepSea,
    IntRange ShallowSea,
    IntRange CoastLand,
    IntRange PlainLand,
    IntRange ForestLand,
    IntRange MountainLand,
    IntRange VolcanoLand,
    int CoastSeaWidth,
    int IslandSeeds,
    IntRange IslandRegions,
    float ArcChance,
    int ArcIslands,
    IntRange ArcSize,
    int HotspotCount,
    IntRange HotspotSize,
    float ShelfChance,
    IntRange ShelfSize,
    int SmoothingPasses,
    IReadOnlyList<float> BandTemperatureOffsets,
    float CoastWaterOffset,
    float MountainTemperatureOffset,
    float ForestForestOffset,
    float SeaForestOffset,
    float SmoothRate,
    int ZeroInfluenceDistance)
{
    /// <summary>地域の数。</summary>
    public int RegionCount => Columns * Bands;

    /// <summary>値が正しいか確かめる。正しくなければ <see cref="ArgumentException"/>。</summary>
    public void Validate()
    {
        Require(Columns >= 2 && Bands >= 2 && RegionSize >= 2, "格子の大きさが小さすぎる");
        Require(PlateCount >= 1 && PlateCount <= RegionCount, "プレートの数が範囲外");
        Require(ContinentSeeds >= 1, "大陸の種の数が範囲外");
        Require(SeaShare.Min >= 0 && SeaShare.Max <= 1 && SeaShare.Min <= SeaShare.Max, "海の割合が範囲外");
        Require(PlainWeight >= 0 && ForestWeight >= 0 && MountainWeight >= 0 && PlainWeight + ForestWeight + MountainWeight > 0, "陸の種類の重みが範囲外");
        Require(BoundaryMountainMultiplier >= 0, "境目の山岳の倍率が負");
        Require(DeepSea.Max < 0 && DeepSea.Min <= DeepSea.Max, "深い海の標高が範囲外");
        Require(ShallowSea.Max < 0 && ShallowSea.Min <= ShallowSea.Max, "浅い海の標高が範囲外");
        foreach (var land in new[] { CoastLand, PlainLand, ForestLand, MountainLand, VolcanoLand })
        {
            Require(land.Min >= 1 && land.Min <= land.Max, "陸の標高が範囲外");
        }
        Require(CoastSeaWidth >= 1 && CoastSeaWidth * 2 < RegionSize, "沿岸の海の幅が範囲外");
        Require(IslandSeeds >= 0 && IslandRegions.Min is >= 1 and <= 2 && IslandRegions.Max is >= 1 and <= 2 && IslandRegions.Min <= IslandRegions.Max, "地域の島が範囲外");
        Require(ArcChance is >= 0 and <= 1 && ShelfChance is >= 0 and <= 1, "島の確率が範囲外");
        Require(ArcIslands >= 0 && HotspotCount >= 0, "島の数が負");
        foreach (var size in new[] { ArcSize, HotspotSize, ShelfSize })
        {
            Require(size.Min >= 1 && size.Min <= size.Max, "タイルの島の大きさが範囲外");
        }
        Require(SmoothingPasses >= 0, "なめらかにする回数が負");
        Require(BandTemperatureOffsets is not null && BandTemperatureOffsets.Count == Bands, "段ごとの気温の補正の数が段の数と違う");
        Require(SmoothRate is >= 0 and <= 1, "ならす速さが範囲外");
        Require(ZeroInfluenceDistance >= 1, "ドグマの効き目の距離が範囲外");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new ArgumentException(message);
        }
    }
}
