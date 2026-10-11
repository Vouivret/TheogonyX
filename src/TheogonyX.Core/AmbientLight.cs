namespace TheogonyX.Core;

/// <summary>昼夜とカオス度・崩壊から、環境光の色を求める（docs/domain/ambience.md）。</summary>
public static class AmbientLight
{
    // 仕様書の「カオス度 ÷ 100 の割合」。
    private const float ChaosLevelForFullShift = 100f;

    /// <param name="elapsedSeconds">昼夜の周期を数え始めてからの経過秒。</param>
    public static LightColor Compute(double elapsedSeconds, WorldState world, AmbienceRules rules)
    {
        if (world.IsDestroyed)
        {
            return rules.DestroyedColor;
        }

        var colors = rules.CycleColors;
        var segmentSeconds = (double)rules.CycleSeconds / colors.Count;
        var inCycle = elapsedSeconds % rules.CycleSeconds;
        var index = Math.Min((int)(inCycle / segmentSeconds), colors.Count - 1);
        var weight = (float)((inCycle - (index * segmentSeconds)) / segmentSeconds);
        var dayNight = LightColor.Lerp(colors[index], colors[(index + 1) % colors.Count], weight);

        var chaosWeight = Math.Clamp(world.ChaosLevel / ChaosLevelForFullShift, 0f, 1f);
        return LightColor.Lerp(dayNight, rules.ChaosColor, chaosWeight);
    }
}
