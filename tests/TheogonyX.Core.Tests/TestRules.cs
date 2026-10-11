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
}
