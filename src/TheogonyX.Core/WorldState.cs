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
