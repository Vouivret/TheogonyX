namespace TheogonyX.Core;

/// <summary>地域ごとの環境を 1 年ずつ進める（plan/feat-planet-grid-1a.md の「1 年を進める」）。</summary>
public static class RegionalWorldSimulator
{
    /// <summary>1 年進める。world を書き換える。崩壊した星は何もしない。</summary>
    /// <param name="share">ドグマの効き目に掛ける、住人のうちの信者の割合（0〜1）。null なら 1（信仰を使わない）。</param>
    public static void AdvanceYear(
        RegionalWorld world, IReadOnlyList<Dogma> dogmas, WorldRules rules, PlanetRules planet, Func<Dogma, double>? share = null)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(dogmas);
        ArgumentNullException.ThrowIfNull(rules);
        ArgumentNullException.ThrowIfNull(planet);
        if (world.IsDestroyed)
        {
            return;
        }
        world.Year++;

        var total = SumDogmas(dogmas, rules, share);
        var grid = world.Grid;
        var influence = new double[grid.Count];
        for (var region = 0; region < grid.Count; region++)
        {
            influence[region] = Math.Max(0, 1 - (double)grid.Distance(world.ProphetRegion, region) / planet.ZeroInfluenceDistance);
        }
        // 平均の変化が、ドグマの効果のとおり（今の星全体の変化と同じ）になるようにそろえる
        var scale = grid.Count / influence.Sum();

        var values = world.Values;
        for (var region = 0; region < grid.Count; region++)
        {
            var factor = (float)(influence[region] * scale);
            var v = values[region];
            values[region] = new EnvironmentValues(
                v.ForestAmount + total.ForestAmount * factor,
                v.WaterAmount + total.WaterAmount * factor,
                v.Temperature + total.Temperature * factor,
                v.ChaosLevel + total.ChaosLevel * factor - rules.ChaosDecayPerYear,
                v.HarmonyLevel + total.HarmonyLevel * factor);
        }

        // 隣の平均へ近づける。全地域を前の値から同時に計算する
        var before = (EnvironmentValues[])values.Clone();
        var rate = planet.SmoothRate;
        for (var region = 0; region < grid.Count; region++)
        {
            var neighbors = grid.Neighbors(region);
            double forest = 0, water = 0, temperature = 0, chaos = 0, harmony = 0;
            foreach (var other in neighbors)
            {
                forest += before[other].ForestAmount;
                water += before[other].WaterAmount;
                temperature += before[other].Temperature;
                chaos += before[other].ChaosLevel;
                harmony += before[other].HarmonyLevel;
            }
            var n = neighbors.Count;
            var v = before[region];
            values[region] = new EnvironmentValues(
                v.ForestAmount + rate * (float)(forest / n - v.ForestAmount),
                v.WaterAmount + rate * (float)(water / n - v.WaterAmount),
                v.Temperature + rate * (float)(temperature / n - v.Temperature),
                v.ChaosLevel + rate * (float)(chaos / n - v.ChaosLevel),
                v.HarmonyLevel + rate * (float)(harmony / n - v.HarmonyLevel));
        }

        for (var region = 0; region < grid.Count; region++)
        {
            var v = values[region];
            values[region] = new EnvironmentValues(
                Math.Clamp(v.ForestAmount, rules.Min.ForestAmount, rules.Max.ForestAmount),
                Math.Clamp(v.WaterAmount, rules.Min.WaterAmount, rules.Max.WaterAmount),
                Math.Clamp(v.Temperature, rules.Min.Temperature, rules.Max.Temperature),
                Math.Clamp(v.ChaosLevel, rules.Min.ChaosLevel, rules.Max.ChaosLevel),
                Math.Clamp(v.HarmonyLevel, rules.Min.HarmonyLevel, rules.Max.HarmonyLevel));
        }

        var average = world.Average();
        var collapse = rules.Collapse;
        world.IsDestroyed = average.ChaosLevel >= collapse.ChaosAtOrAbove
            || (average.WaterAmount <= collapse.BarrenWaterAtOrBelow && average.ForestAmount <= collapse.BarrenForestAtOrBelow)
            || average.Temperature >= collapse.TemperatureAtOrAbove
            || average.Temperature <= collapse.TemperatureAtOrBelow;
    }

    private static EnvironmentValues SumDogmas(IReadOnlyList<Dogma> dogmas, WorldRules rules, Func<Dogma, double>? share)
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
                var amount = effect.PerYear;
                var intensity = dogma.Intensity * (share is null ? 1f : (float)share(dogma));
                sum = new EnvironmentValues(
                    sum.ForestAmount + amount.ForestAmount * intensity,
                    sum.WaterAmount + amount.WaterAmount * intensity,
                    sum.Temperature + amount.Temperature * intensity,
                    sum.ChaosLevel + amount.ChaosLevel * intensity,
                    sum.HarmonyLevel + amount.HarmonyLevel * intensity);
            }
        }
        return sum;
    }
}
