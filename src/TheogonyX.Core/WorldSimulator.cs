namespace TheogonyX.Core;

/// <summary>ドグマによる環境の変化を年ごとに計算する（docs/domain/time-skip.md）。</summary>
public static class WorldSimulator
{
    /// <summary>世界を years 年進める。world を書き換える。崩壊したらその年で止まる。</summary>
    public static void SimulateTimeSkip(WorldState world, IReadOnlyList<Dogma> dogmas, int years, WorldRules rules)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(dogmas);
        ArgumentNullException.ThrowIfNull(rules);
        ArgumentOutOfRangeException.ThrowIfNegative(years);

        if (world.IsDestroyed)
        {
            return;
        }

        var perYear = SumPerYear(dogmas, rules);
        for (var i = 0; i < years; i++)
        {
            world.Year++;
            world.ForestAmount = Math.Clamp(world.ForestAmount + perYear.ForestAmount, rules.Min.ForestAmount, rules.Max.ForestAmount);
            world.WaterAmount = Math.Clamp(world.WaterAmount + perYear.WaterAmount, rules.Min.WaterAmount, rules.Max.WaterAmount);
            world.Temperature = Math.Clamp(world.Temperature + perYear.Temperature, rules.Min.Temperature, rules.Max.Temperature);
            world.ChaosLevel = Math.Clamp(world.ChaosLevel + perYear.ChaosLevel, rules.Min.ChaosLevel, rules.Max.ChaosLevel);
            world.HarmonyLevel = Math.Clamp(world.HarmonyLevel + perYear.HarmonyLevel, rules.Min.HarmonyLevel, rules.Max.HarmonyLevel);

            if (IsCollapsed(world, rules.Collapse))
            {
                world.IsDestroyed = true;
                break;
            }
        }
    }

    // ドグマは減衰しないので、1年分の変化量は跳躍の間ずっと同じ。
    private static EnvironmentValues SumPerYear(IReadOnlyList<Dogma> dogmas, WorldRules rules)
    {
        var sum = new EnvironmentValues();
        foreach (var dogma in dogmas)
        {
            foreach (var effect in rules.DogmaEffects)
            {
                if (effect.SymbolId != dogma.TargetSymbolId || effect.DogmaType != dogma.Type)
                {
                    continue;
                }
                sum = new EnvironmentValues(
                    sum.ForestAmount + effect.PerYear.ForestAmount * dogma.Intensity,
                    sum.WaterAmount + effect.PerYear.WaterAmount * dogma.Intensity,
                    sum.Temperature + effect.PerYear.Temperature * dogma.Intensity,
                    sum.ChaosLevel + effect.PerYear.ChaosLevel * dogma.Intensity,
                    sum.HarmonyLevel + effect.PerYear.HarmonyLevel * dogma.Intensity);
            }
        }
        return sum;
    }

    private static bool IsCollapsed(WorldState world, CollapseRules collapse) =>
        world.ChaosLevel >= collapse.ChaosAtOrAbove
        || (world.WaterAmount <= collapse.BarrenWaterAtOrBelow && world.ForestAmount <= collapse.BarrenForestAtOrBelow)
        || world.Temperature >= collapse.TemperatureAtOrAbove
        || world.Temperature <= collapse.TemperatureAtOrBelow;
}
