namespace TheogonyX.Core;

/// <summary>地域の文明の値と自律建築に使うゲームデータ定義（docs/domain/emergent-progression.md）。</summary>
public sealed record CivilizationRules
{
    /// <param name="regions">地表の列と地域の対応。</param>
    /// <param name="rows">地表の行の数。建物の枠（2 × 2 マス）を縦に並べるのに使う。</param>
    /// <param name="prophetColumn">予言者のいる列（0 から数える）。ドグマがいちばん強く効く地域を決める。</param>
    /// <param name="naturalGrowthPerYear">技術・文化が毎年伸びる量。</param>
    /// <param name="worshipCulturePerYear">崇拝のドグマが、強度 1 で文化に毎年足す量。</param>
    /// <param name="destructionTechPerYear">破壊のドグマが、強度 1 で技術に毎年足す量。</param>
    /// <param name="tabooGrowthFactor">禁忌のドグマがあるときの伸びの倍率。</param>
    /// <param name="zeroInfluenceDistance">ドグマの効き目が 0 になる、予言者の地域からの地域の数。</param>
    /// <param name="environmentMultipliers">地域の環境（タイル種別）ごとの伸びの倍率。小さいほど悪い環境。</param>
    /// <param name="thresholds">建物が建つ技術・文化の値（小さい順）。</param>
    /// <param name="strata">建物の寿命と地層沈下の定義（docs/domain/strata.md）。</param>
    public CivilizationRules(
        RegionMap regions,
        int rows,
        int prophetColumn,
        double initialTech,
        double initialCulture,
        double naturalGrowthPerYear,
        double worshipCulturePerYear,
        double destructionTechPerYear,
        double tabooGrowthFactor,
        int zeroInfluenceDistance,
        IReadOnlyDictionary<TileType, double> environmentMultipliers,
        IReadOnlyList<double> thresholds,
        StrataRules strata)
    {
        ArgumentNullException.ThrowIfNull(regions);
        ArgumentNullException.ThrowIfNull(environmentMultipliers);
        ArgumentNullException.ThrowIfNull(thresholds);
        ArgumentNullException.ThrowIfNull(strata);
        ArgumentOutOfRangeException.ThrowIfLessThan(rows, 2);
        ArgumentOutOfRangeException.ThrowIfNegative(prophetColumn);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(prophetColumn, regions.Columns);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(zeroInfluenceDistance);
        if (regions.ColumnsPerRegion % 2 != 0)
        {
            throw new ArgumentException($"建物は 2 列の幅なので、地域の列の数は偶数: columnsPerRegion={regions.ColumnsPerRegion}", nameof(regions));
        }
        double[] fractions = [initialTech, initialCulture, tabooGrowthFactor];
        double[] amounts = [naturalGrowthPerYear, worshipCulturePerYear, destructionTechPerYear];
        if (fractions.Any(value => !double.IsFinite(value) || value is < 0 or > 1)
            || amounts.Any(value => !double.IsFinite(value) || value < 0))
        {
            throw new ArgumentException("初期値・伸びの量・禁忌の倍率が範囲外");
        }
        if (Enum.GetValues<TileType>().Any(tileType => !environmentMultipliers.TryGetValue(tileType, out var value) || !double.IsFinite(value) || value < 0))
        {
            throw new ArgumentException("環境の倍率が、すべてのタイル種別に 0 以上で無い", nameof(environmentMultipliers));
        }
        for (var i = 0; i < thresholds.Count; i++)
        {
            if (!double.IsFinite(thresholds[i]) || thresholds[i] is <= 0 or > 1 || (i > 0 && thresholds[i] <= thresholds[i - 1]))
            {
                throw new ArgumentException($"閾値は 0 より大きく 1 以下で、小さい順: thresholds={string.Join(',', thresholds)}", nameof(thresholds));
            }
        }
        Regions = regions;
        Rows = rows;
        ProphetColumn = prophetColumn;
        InitialTech = initialTech;
        InitialCulture = initialCulture;
        NaturalGrowthPerYear = naturalGrowthPerYear;
        WorshipCulturePerYear = worshipCulturePerYear;
        DestructionTechPerYear = destructionTechPerYear;
        TabooGrowthFactor = tabooGrowthFactor;
        ZeroInfluenceDistance = zeroInfluenceDistance;
        EnvironmentMultipliers = environmentMultipliers;
        Thresholds = thresholds;
        Strata = strata;
    }

    public RegionMap Regions { get; }
    public int Rows { get; }
    public int ProphetColumn { get; }
    public double InitialTech { get; }
    public double InitialCulture { get; }
    public double NaturalGrowthPerYear { get; }
    public double WorshipCulturePerYear { get; }
    public double DestructionTechPerYear { get; }
    public double TabooGrowthFactor { get; }
    public int ZeroInfluenceDistance { get; }
    public IReadOnlyDictionary<TileType, double> EnvironmentMultipliers { get; }
    public IReadOnlyList<double> Thresholds { get; }
    public StrataRules Strata { get; }
}
