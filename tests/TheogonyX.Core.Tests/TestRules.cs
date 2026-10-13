using TheogonyX.Core;

namespace TheogonyX.Core.Tests;

/// <summary>
/// テスト用のゲームデータ定義。値は仕様書（docs/domain/dream-interpretation.md, time-skip.md）の
/// 「受け入れ条件の計算に使った値」から写す。
/// game/data/ の本番の値には依存させない（.agent/rules/testing.md）。
/// </summary>
internal static class TestRules
{
    public static readonly DreamRules Dream = new(
        [
            new Symbol("SYM_TREE", 0.4f),
            new Symbol("SYM_RAIN", 0.6f),
            new Symbol("SYM_BLOOD", -0.9f),
            new Symbol("SYM_FIRE", 0.1f),
        ],
        TabooAtOrBelow: -0.3f,
        WorshipAtOrAbove: 0.3f);

    public static readonly WorldRules World = new(
        Initial: new EnvironmentValues(ForestAmount: 100f, WaterAmount: 100f, Temperature: 15f, ChaosLevel: 0f, HarmonyLevel: 50f),
        Min: new EnvironmentValues(ForestAmount: 0f, WaterAmount: 0f, Temperature: -50f, ChaosLevel: 0f, HarmonyLevel: 0f),
        Max: new EnvironmentValues(ForestAmount: 100f, WaterAmount: 100f, Temperature: 80f, ChaosLevel: 100f, HarmonyLevel: 100f),
        DogmaEffects:
        [
            new DogmaEffect("SYM_TREE", DogmaType.Taboo, new EnvironmentValues(ForestAmount: 1.0f, WaterAmount: -0.5f, 0f, 0f, 0f)),
            new DogmaEffect("SYM_TREE", DogmaType.Worship, new EnvironmentValues(ForestAmount: 0.5f, 0f, 0f, 0f, HarmonyLevel: 0.2f)),
            new DogmaEffect("SYM_TREE", DogmaType.Destruction, new EnvironmentValues(ForestAmount: -2.0f, 0f, 0f, ChaosLevel: 0.8f, 0f)),
            new DogmaEffect("SYM_RAIN", DogmaType.Taboo, new EnvironmentValues(0f, WaterAmount: -1.0f, Temperature: 0.1f, 0f, 0f)),
            new DogmaEffect("SYM_RAIN", DogmaType.Worship, new EnvironmentValues(0f, WaterAmount: 0.5f, Temperature: -0.05f, 0f, 0f)),
            new DogmaEffect("SYM_RAIN", DogmaType.Destruction, new EnvironmentValues(0f, WaterAmount: -1.5f, 0f, ChaosLevel: 0.5f, 0f)),
            new DogmaEffect("SYM_FIRE", DogmaType.Taboo, new EnvironmentValues(0f, 0f, Temperature: -0.2f, 0f, 0f)),
            new DogmaEffect("SYM_FIRE", DogmaType.Destruction, new EnvironmentValues(ForestAmount: -1.5f, 0f, Temperature: 0.3f, ChaosLevel: 1.0f, 0f)),
            new DogmaEffect("SYM_BLOOD", DogmaType.Worship, new EnvironmentValues(0f, 0f, 0f, ChaosLevel: 1.5f, HarmonyLevel: -1.0f)),
            new DogmaEffect("SYM_BLOOD", DogmaType.Destruction, new EnvironmentValues(0f, 0f, 0f, ChaosLevel: 1.5f, HarmonyLevel: -1.0f)),
        ],
        Collapse: new CollapseRules(
            ChaosAtOrAbove: 100f,
            BarrenWaterAtOrBelow: 0f,
            BarrenForestAtOrBelow: 0f,
            TemperatureAtOrAbove: 70f,
            TemperatureAtOrBelow: -40f));

    // docs/domain/planet-display.md の「タイル種別の判定」と列数の式。
    public static readonly TerrainRules Terrain = new(
        columns: 16,
        rows: 9,
        anomalies: Anomalies,
        lakeCells: []);

    // 湖のある地表。湖は左から 11〜14 列目 × 上から 4〜6 行目の 12 マス（planet-display.md の受け入れ条件の前提）。
    public static readonly TerrainRules TerrainWithLake = new(
        columns: 16,
        rows: 9,
        anomalies: Anomalies,
        lakeCells:
        [
            new(11, 4), new(12, 4), new(11, 3), new(12, 3), new(11, 5), new(12, 5),
            new(10, 4), new(13, 4), new(10, 3), new(13, 3), new(10, 5), new(13, 5),
        ]);

    // docs/domain/crowd.md の人数（10,000）・狂乱の閾値（カオス度 50）と、game/data/rules.json の仮の値。
    public static readonly CrowdRules Crowd = new(
        Count: 10_000,
        WalkSpeed: 0.5f,
        TurnIntervalSeconds: 2f,
        FrenzySpeed: 2f,
        HolySiteRadius: 1.5f,
        FrenzyChaosAtOrAbove: 50f,
        FieldScale: 0.5f,
        FastTimeScale: 3f,
        Flock: new FlockRules(Enabled: false, CellSize: 0.5f, AlignWeight: 0.5f, CrowdWeight: 0.5f, TargetNeighbors: 20f),
        TileForces: new Dictionary<TileType, float>
        {
            [TileType.Water] = 1.0f,
            [TileType.Jungle] = 0.6f,
            [TileType.Grassland] = 0.4f,
            [TileType.Desert] = -0.6f,
            [TileType.Glacier] = -0.6f,
            [TileType.Scorched] = -1.0f,
        });

    private static TerrainAnomaly[] Anomalies =>
        [
            new TerrainAnomaly(TileType.Glacier, WorldMetric.Temperature, ThresholdComparison.AtOrBelow, Threshold: 0f, ZeroColumnValue: 0f, FullColumnValue: -50f),
            new TerrainAnomaly(TileType.Scorched, WorldMetric.Temperature, ThresholdComparison.AtOrAbove, Threshold: 50f, ZeroColumnValue: 50f, FullColumnValue: 80f),
            new TerrainAnomaly(TileType.Desert, WorldMetric.WaterAmount, ThresholdComparison.AtOrBelow, Threshold: 20f, ZeroColumnValue: 100f, FullColumnValue: 0f),
            new TerrainAnomaly(TileType.Jungle, WorldMetric.ForestAmount, ThresholdComparison.AtOrAbove, Threshold: 70f, ZeroColumnValue: 70f, FullColumnValue: 100f),
        ];

    // docs/domain/dream-tampering.md と prophet-request.md のルールの値。
    public static readonly ProphetDreamRules ProphetDreams = new(
        NodeCount: 3,
        MinWeight: 0.3f,
        MaxWeight: 1.0f,
        YearsBetweenDreams: 100,
        RequestCooldownYears: 50,
        Requests:
        [
            new ProphetRequestRule("THIRST", WorldMetric.WaterAmount, ThresholdComparison.AtOrBelow, 30f, "SYM_RAIN"),
            new ProphetRequestRule("FAMINE", WorldMetric.ForestAmount, ThresholdComparison.AtOrBelow, 30f, "SYM_TREE"),
            new ProphetRequestRule("HEAT", WorldMetric.Temperature, ThresholdComparison.AtOrAbove, 40f, "SYM_RAIN"),
            new ProphetRequestRule("COLD", WorldMetric.Temperature, ThresholdComparison.AtOrBelow, -10f, "SYM_FIRE"),
            new ProphetRequestRule("CHAOS", WorldMetric.ChaosLevel, ThresholdComparison.AtOrAbove, 60f, "SYM_TREE"),
        ]);

    // docs/domain/emergent-progression.md の「受け入れ条件の計算に使った値」。地表は 16 列 × 9 行（Terrain と同じ）なので、地域は 8 個。
    public static readonly CivilizationRules Civilization = new(
        regions: new RegionMap(16, 2),
        rows: 9,
        prophetColumn: 7,
        initialTech: 0,
        initialCulture: 0,
        naturalGrowthPerYear: 0.01,
        worshipCulturePerYear: 0.1,
        destructionTechPerYear: 0.1,
        tabooGrowthFactor: 0.5,
        zeroInfluenceDistance: 4,
        environmentMultipliers: new Dictionary<TileType, double>
        {
            [TileType.Scorched] = 0.3,
            [TileType.Glacier] = 0.3,
            [TileType.Desert] = 0.6,
            [TileType.Grassland] = 1.0,
            [TileType.Jungle] = 1.5,
            [TileType.Water] = 1.5,
        },
        thresholds: [0.2, 0.4, 0.6, 0.8],
        // docs/domain/strata.md の「M4 で作る範囲と値」の仮の値
        strata: new StrataRules(lifespanMin: 200, lifespanMax: 400, ruinBurialYears: 150, maxDepth: 3));

    // docs/domain/agent-needs.md の「M5 で作る最初の範囲と値」の値の表と、名前のパーツ（陸の人の 6 × 6）。
    public static readonly NeedsRules Needs = new(
        ImmigrantAge: 20,
        BaseCapacity: 2,
        CapacityPerBuilding: 4,
        FamineAtOrBelow: 30f,
        HungerFamine: 0.9,
        HungerNormal: 0.1,
        HungerCatatonia: 1.0,
        ShelterEnough: 0.1,
        ShelterLacking: 0.8,
        SocialPerYear: 0.2,
        EsteemPerYear: 0.1,
        ActualizationPerYear: 0.05,
        TaskRelief: 0.5,
        LowerNeedsBelow: 0.6,
        SocialAtOrAbove: 0.6,
        EsteemAtOrAbove: 0.6,
        ActualizationAtOrAbove: 0.5,
        MaxHealth: 100,
        HealthLoss: 20,
        HealthRecovery: 10,
        AgingStartAge: 50,
        AgingLossPerYear: 2,
        MaxSanity: 100,
        SanityChaosDivisor: 10,
        SanityLossPerDeath: 10,
        SanityRecovery: 5,
        RampageProbability: 0.5,
        BreakdownYears: 3,
        RecoveredSanity: 30,
        RampageChaos: 5f,
        GraveYears: 10,
        WitnessAge: 21,
        WitnessIntensityAtOrAbove: 0.8f,
        ElderAge: 81,
        NamePrefixes: ["NAME_PREFIX_ART", "NAME_PREFIX_VAL", "NAME_PREFIX_ILIO", "NAME_PREFIX_EL", "NAME_PREFIX_SELE", "NAME_PREFIX_OR"],
        NameSuffixes: ["NAME_SUFFIX_ORIUS", "NAME_SUFFIX_US", "NAME_SUFFIX_PHIA", "NAME_SUFFIX_GIS", "NAME_SUFFIX_MION", "NAME_SUFFIX_A"]);

    // docs/domain/time-control.md の「100年跳躍ボタン」。
    public const int JumpYears = 100;

    // docs/domain/ambience.md の受け入れ条件の計算に使った色（0〜255 の成分を 255 で割る）。
    public static readonly AmbienceRules Ambience = new(
        CycleSeconds: 12f,
        CycleColors:
        [
            Rgb(255, 255, 255), // 青空
            Rgb(210, 125, 44), // 夕焼け
            Rgb(48, 52, 109), // 夜
            Rgb(222, 238, 214), // 朝
        ],
        ChaosColor: Rgb(208, 70, 72),
        DestroyedColor: Rgb(68, 36, 52));

    public static LightColor Rgb(float r, float g, float b) => new(r / 255f, g / 255f, b / 255f);
}
