namespace TheogonyX.Core;

/// <summary>ドグマによる環境の変化を年ごとに計算する（docs/domain/time-skip.md）。</summary>
public static class WorldSimulator
{
    /// <summary>世界を years 年進める。world を書き換える。崩壊したらその年で止まる。</summary>
    /// <param name="afterYear">毎年、環境の値を変えた後に呼ぶ（文明の計算）。崩壊した年には呼ばない。</param>
    /// <param name="share">ドグマの効き目に掛ける、住人のうちの信者の割合（0〜1）。毎年の初めの値で求める。null なら 1（信仰を使わない）。</param>
    public static void SimulateTimeSkip(
        WorldState world, IReadOnlyList<Dogma> dogmas, int years, WorldRules rules, Action? afterYear = null, Func<Dogma, double>? share = null)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(dogmas);
        ArgumentNullException.ThrowIfNull(rules);
        ArgumentOutOfRangeException.ThrowIfNegative(years);

        if (world.IsDestroyed)
        {
            return;
        }

        var fixedPerYear = share is null ? SumPerYear(dogmas, rules, null) : default;
        for (var i = 0; i < years; i++)
        {
            var perYear = share is null ? fixedPerYear : SumPerYear(dogmas, rules, share);
            world.Year++;
            world.ForestAmount = Math.Clamp(world.ForestAmount + perYear.ForestAmount, rules.Min.ForestAmount, rules.Max.ForestAmount);
            world.WaterAmount = Math.Clamp(world.WaterAmount + perYear.WaterAmount, rules.Min.WaterAmount, rules.Max.WaterAmount);
            world.Temperature = Math.Clamp(world.Temperature + perYear.Temperature, rules.Min.Temperature, rules.Max.Temperature);
            world.ChaosLevel = Math.Clamp(
                world.ChaosLevel + perYear.ChaosLevel - rules.ChaosDecayPerYear, rules.Min.ChaosLevel, rules.Max.ChaosLevel);
            world.HarmonyLevel = Math.Clamp(world.HarmonyLevel + perYear.HarmonyLevel, rules.Min.HarmonyLevel, rules.Max.HarmonyLevel);

            if (IsCollapsed(world, rules.Collapse))
            {
                world.IsDestroyed = true;
                break;
            }
            afterYear?.Invoke();
        }
    }

    // ドグマは減衰しないので、信者の割合を使わなければ、1年分の変化量は跳躍の間ずっと同じ。
    private static EnvironmentValues SumPerYear(IReadOnlyList<Dogma> dogmas, WorldRules rules, Func<Dogma, double>? share)
    {
        var sum = new EnvironmentValues();
        foreach (var dogma in dogmas)
        {
            var weight = share is null ? 1f : (float)share(dogma);
            foreach (var effect in rules.DogmaEffects)
            {
                if (effect.SymbolId != dogma.TargetSymbolId || effect.DogmaType != dogma.Type)
                {
                    continue;
                }
                sum = new EnvironmentValues(
                    sum.ForestAmount + effect.PerYear.ForestAmount * dogma.Intensity * weight,
                    sum.WaterAmount + effect.PerYear.WaterAmount * dogma.Intensity * weight,
                    sum.Temperature + effect.PerYear.Temperature * dogma.Intensity * weight,
                    sum.ChaosLevel + effect.PerYear.ChaosLevel * dogma.Intensity * weight,
                    sum.HarmonyLevel + effect.PerYear.HarmonyLevel * dogma.Intensity * weight);
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
