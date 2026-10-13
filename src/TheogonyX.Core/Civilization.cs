namespace TheogonyX.Core;

/// <summary>建物の種類（docs/domain/emergent-progression.md）。技術と文化のどちらの閾値を越えて建ったかで分ける。</summary>
public enum BuildingType
{
    /// <summary>技術の閾値を越えて建つ工房。</summary>
    Workshop,

    /// <summary>文化の閾値を越えて建つ祠。</summary>
    Shrine,
}

/// <summary>建っている建物。大きさは 2 × 2 マスで、(<paramref name="Column"/>, <paramref name="Row"/>) はその左上のマス（0 から数える）。</summary>
/// <param name="Id">通し番号。廃墟・遺物になっても同じ番号を使う。</param>
/// <param name="CollapseYear">老朽化で崩れて廃墟になる年（docs/domain/strata.md）。</param>
public sealed record Building(int Id, BuildingType Type, int Region, int Column, int Row, int CollapseYear);

/// <summary>地上に残った廃墟（土台）。1 つの枠に 1 つまで。上に建物が建っていれば建て増しされている。</summary>
/// <param name="RuinedYear">廃墟になった年。上に建物が無いまま一定の年数がたつと地下に沈む。</param>
public sealed record Ruin(int Id, BuildingType Type, int Region, int Column, int Row, int RuinedYear);

/// <summary>地下に埋もれた遺物。埋もれた瞬間の地域の技術・文化の値と、星のカオス度（÷ 100）とドグマを残す。</summary>
/// <param name="Depth">地下の何層目か（1 から数える）。</param>
public sealed record Relic(
    int Id, BuildingType Type, int Region, int Column, int Row, int Depth, int BuriedYear,
    double Tech, double Culture, double Chaos, IReadOnlyList<Dogma> Dogmas)
{
    public bool Equals(Relic? other) =>
        other is not null && Id == other.Id && Type == other.Type && Region == other.Region && Column == other.Column && Row == other.Row
        && Depth == other.Depth && BuriedYear == other.BuriedYear && Tech.Equals(other.Tech) && Culture.Equals(other.Culture)
        && Chaos.Equals(other.Chaos) && Dogmas.SequenceEqual(other.Dogmas);

    public override int GetHashCode() => HashCode.Combine(Id, Depth, Column, Row);
}

/// <summary>1 つの地域の文明の状態。</summary>
/// <param name="TechStage">技術の閾値をいくつ越えたか。立っている工房の数がこれより少なければ建てる。</param>
/// <param name="CultureStage">文化の閾値をいくつ越えたか。立っている祠の数がこれより少なければ建てる。</param>
public sealed record RegionCivilization(double Tech, double Culture, int TechStage, int CultureStage);

/// <summary>地域ごとの文明の値と、建物・廃墟・遺物（docs/domain/emergent-progression.md、docs/domain/strata.md）。</summary>
public sealed class Civilization
{
    private readonly RegionCivilization[] _regions;
    private readonly List<Building> _buildings;
    private readonly List<Ruin> _ruins;
    private List<Relic> _relics;

    public Civilization(
        IEnumerable<RegionCivilization> regions, IEnumerable<Building> buildings, IEnumerable<Ruin> ruins, IEnumerable<Relic> relics,
        int nextBuildingId)
    {
        ArgumentNullException.ThrowIfNull(regions);
        ArgumentNullException.ThrowIfNull(buildings);
        ArgumentNullException.ThrowIfNull(ruins);
        ArgumentNullException.ThrowIfNull(relics);
        ArgumentOutOfRangeException.ThrowIfNegative(nextBuildingId);
        _regions = [.. regions];
        _buildings = [.. buildings];
        _ruins = [.. ruins];
        _relics = [.. relics];
        NextBuildingId = nextBuildingId;
    }

    /// <summary>添字は地域の番号。</summary>
    public IReadOnlyList<RegionCivilization> Regions => _regions;

    /// <summary>建っている建物。建った順。</summary>
    public IReadOnlyList<Building> Buildings => _buildings;

    /// <summary>地上の廃墟。廃墟になった順。</summary>
    public IReadOnlyList<Ruin> Ruins => _ruins;

    /// <summary>地下の遺物。</summary>
    public IReadOnlyList<Relic> Relics => _relics;

    /// <summary>次に建つ建物の ID。</summary>
    public int NextBuildingId { get; private set; }

    /// <summary>すべての地域が初期値で、建物・廃墟・遺物の無い状態。</summary>
    public static Civilization CreateInitial(CivilizationRules rules) => CreateInitial(rules, [], 0);

    /// <summary>今の値と同じ中身の、別の状態（セーブに書き出す写し）。</summary>
    public Civilization Copy() => new(_regions, _buildings, _ruins, _relics, NextBuildingId);

    /// <summary>
    /// 世界再創生: 地上の建物と廃墟をすべて地下 1 層目に沈め（マクロ沈下）、遺物だけを残して地域を初期値に戻した状態を返す。
    /// 廃墟の上に建物がある枠では、廃墟が 2 層目、建物が 1 層目になる。
    /// </summary>
    public Civilization Recreate(WorldState world, IReadOnlyList<Dogma> dogmas, CivilizationRules rules)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(dogmas);
        ArgumentNullException.ThrowIfNull(rules);
        foreach (var ruin in _ruins.ToList())
        {
            StrataTransitions.Next(StrataState.Ruin, StrataTrigger.MacroSubduction);
            BuryRuin(ruin, world, dogmas, rules);
        }
        foreach (var building in _buildings.ToList())
        {
            StrataTransitions.Next(StrataState.Building, StrataTrigger.MacroSubduction);
            _buildings.Remove(building);
            Bury(building.Id, building.Type, building.Region, building.Column, building.Row, world, dogmas, rules);
        }
        return CreateInitial(rules, _relics, NextBuildingId);
    }

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

    internal void AddBuilding(BuildingType type, int region, int column, int row, int collapseYear)
    {
        _buildings.Add(new Building(NextBuildingId, type, region, column, row, collapseYear));
        NextBuildingId++;
    }

    // 建物を廃墟にする。その枠に廃墟（建て増しの土台）があれば、新しい廃墟に押し込まれて地下 1 層目に沈む
    internal void Collapse(Building building, WorldState world, IReadOnlyList<Dogma> dogmas, CivilizationRules rules)
    {
        StrataTransitions.Next(StrataState.Building, StrataTrigger.Collapse);
        var below = RuinAt(building.Column, building.Row);
        if (below is not null)
        {
            StrataTransitions.Next(StrataState.Ruin, StrataTrigger.MicroSubduction);
            BuryRuin(below, world, dogmas, rules);
        }
        _buildings.Remove(building);
        _ruins.Add(new Ruin(building.Id, building.Type, building.Region, building.Column, building.Row, world.Year));
    }

    // 上に建物の無い廃墟を地下 1 層目に沈める（ミクロ沈下）
    internal void BuryIdleRuin(Ruin ruin, WorldState world, IReadOnlyList<Dogma> dogmas, CivilizationRules rules)
    {
        StrataTransitions.Next(StrataState.Ruin, StrataTrigger.MicroSubduction);
        BuryRuin(ruin, world, dogmas, rules);
    }

    internal Ruin? RuinAt(int column, int row) => _ruins.Find(ruin => ruin.Column == column && ruin.Row == row);

    internal bool HasBuildingAt(int column, int row) => _buildings.Exists(building => building.Column == column && building.Row == row);

    private static Civilization CreateInitial(CivilizationRules rules, IEnumerable<Relic> relics, int nextBuildingId)
    {
        ArgumentNullException.ThrowIfNull(rules);
        var region = new RegionCivilization(rules.InitialTech, rules.InitialCulture, StageOf(rules.InitialTech, rules), StageOf(rules.InitialCulture, rules));
        return new Civilization(Enumerable.Repeat(region, rules.Regions.Count), [], [], relics, nextBuildingId);
    }

    private void BuryRuin(Ruin ruin, WorldState world, IReadOnlyList<Dogma> dogmas, CivilizationRules rules)
    {
        _ruins.Remove(ruin);
        Bury(ruin.Id, ruin.Type, ruin.Region, ruin.Column, ruin.Row, world, dogmas, rules);
    }

    // 枠の遺物をすべて 1 層深くし（層の数より深くなったものは消す）、1 層目に新しい遺物を置く
    private void Bury(int id, BuildingType type, int region, int column, int row, WorldState world, IReadOnlyList<Dogma> dogmas, CivilizationRules rules)
    {
        _relics = [.. _relics
            .Select(relic => relic.Column == column && relic.Row == row ? relic with { Depth = relic.Depth + 1 } : relic)
            .Where(relic => relic.Depth <= rules.Strata.MaxDepth)];
        var state = _regions[region];
        _relics.Add(new Relic(id, type, region, column, row, 1, world.Year, state.Tech, state.Culture, world.ChaosLevel / 100.0, [.. dogmas]));
    }
}
