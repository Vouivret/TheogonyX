namespace TheogonyX.Core;

public sealed class WorldState
{
    public int Year { get; set; }
    public float ForestAmount { get; set; }
    public float WaterAmount { get; set; }
    public float Temperature { get; set; }
    public float ChaosLevel { get; set; }
    public float HarmonyLevel { get; set; }
    public bool IsDestroyed { get; set; }

    /// <summary>指標の今の値。</summary>
    public float ValueOf(WorldMetric metric) => metric switch
    {
        WorldMetric.ForestAmount => ForestAmount,
        WorldMetric.WaterAmount => WaterAmount,
        WorldMetric.Temperature => Temperature,
        WorldMetric.ChaosLevel => ChaosLevel,
        WorldMetric.HarmonyLevel => HarmonyLevel,
        _ => throw new ArgumentOutOfRangeException(nameof(metric), metric, null),
    };

    /// <summary>年 0、環境は定義の初期値の世界を作る。</summary>
    public static WorldState CreateInitial(WorldRules rules)
    {
        ArgumentNullException.ThrowIfNull(rules);
        return new WorldState
        {
            ForestAmount = rules.Initial.ForestAmount,
            WaterAmount = rules.Initial.WaterAmount,
            Temperature = rules.Initial.Temperature,
            ChaosLevel = rules.Initial.ChaosLevel,
            HarmonyLevel = rules.Initial.HarmonyLevel,
        };
    }
}
