namespace TheogonyX.Core;

/// <summary>世界の環境の各項目の値の組。初期値・範囲・変化量に使う。</summary>
public readonly record struct EnvironmentValues(
    float ForestAmount,
    float WaterAmount,
    float Temperature,
    float ChaosLevel,
    float HarmonyLevel);
