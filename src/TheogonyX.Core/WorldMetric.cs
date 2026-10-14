namespace TheogonyX.Core;

/// <summary>地表のタイル種別の判定に使う、世界の状態の項目。</summary>
public enum WorldMetric
{
    ForestAmount,
    WaterAmount,
    Temperature,
    ChaosLevel,
    HarmonyLevel,

    /// <summary>地上の廃墟の数（文明の値。<see cref="WorldState"/> には無い。予言者の依頼の判定だけに使う）。</summary>
    RuinCount,
}
