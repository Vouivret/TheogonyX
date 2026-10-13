namespace TheogonyX.Core;

/// <summary>建物の種類（docs/domain/emergent-progression.md）。技術と文化のどちらの閾値を越えて建ったかで分ける。</summary>
public enum BuildingType
{
    /// <summary>技術の閾値を越えて建つ工房。</summary>
    Workshop,

    /// <summary>文化の閾値を越えて建つ祠。</summary>
    Shrine,
}

/// <summary>建物。大きさは 2 × 2 マスで、(<paramref name="Column"/>, <paramref name="Row"/>) はその左上のマス（0 から数える）。</summary>
/// <param name="Id">通し番号。</param>
public sealed record Building(int Id, BuildingType Type, int Region, int Column, int Row);

/// <summary>1 つの地域の文明の状態。</summary>
/// <param name="TechStage">技術の閾値をいくつ越えたか（同じ閾値で 2 回建たないように使う）。</param>
/// <param name="CultureStage">文化の閾値をいくつ越えたか。</param>
public sealed record RegionCivilization(double Tech, double Culture, int TechStage, int CultureStage);

/// <summary>地域ごとの文明の値と、建った建物（docs/domain/emergent-progression.md）。</summary>
public sealed class Civilization
{
    private readonly RegionCivilization[] _regions;
    private readonly List<Building> _buildings;

    public Civilization(IEnumerable<RegionCivilization> regions, IEnumerable<Building> buildings, int nextBuildingId)
    {
        ArgumentNullException.ThrowIfNull(regions);
        ArgumentNullException.ThrowIfNull(buildings);
        ArgumentOutOfRangeException.ThrowIfNegative(nextBuildingId);
        _regions = [.. regions];
        _buildings = [.. buildings];
        NextBuildingId = nextBuildingId;
    }

    /// <summary>添字は地域の番号。</summary>
    public IReadOnlyList<RegionCivilization> Regions => _regions;

    /// <summary>建った順。</summary>
    public IReadOnlyList<Building> Buildings => _buildings;

    /// <summary>次に建つ建物の ID。</summary>
    public int NextBuildingId { get; private set; }

    /// <summary>すべての地域が初期値で、建物の無い状態。</summary>
    public static Civilization CreateInitial(CivilizationRules rules)
    {
        ArgumentNullException.ThrowIfNull(rules);
        var region = new RegionCivilization(rules.InitialTech, rules.InitialCulture, StageOf(rules.InitialTech, rules), StageOf(rules.InitialCulture, rules));
        return new Civilization(Enumerable.Repeat(region, rules.Regions.Count), [], 0);
    }

    /// <summary>今の値と同じ中身の、別の状態（セーブに書き出す写し）。</summary>
    public Civilization Copy() => new(_regions, _buildings, NextBuildingId);

    /// <summary>値が越えている閾値の数。</summary>
    public static int StageOf(double value, CivilizationRules rules)
    {
        ArgumentNullException.ThrowIfNull(rules);
        var stage = 0;
        while (stage < rules.Thresholds.Count && value >= rules.Thresholds[stage])
        {
            stage++;
        }
        return stage;
    }

    internal void SetRegion(int region, RegionCivilization state) => _regions[region] = state;

    internal void AddBuilding(BuildingType type, int region, int column, int row)
    {
        _buildings.Add(new Building(NextBuildingId, type, region, column, row));
        NextBuildingId++;
    }
}
