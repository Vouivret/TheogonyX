namespace TheogonyX.Core;

/// <summary>星の崩壊の条件。どれか1つを満たしたら崩壊する。</summary>
public sealed record CollapseRules(
    float ChaosAtOrAbove,
    float BarrenWaterAtOrBelow,
    float BarrenForestAtOrBelow,
    float TemperatureAtOrAbove,
    float TemperatureAtOrBelow);
