namespace TheogonyX.Core;

/// <summary>昼夜と崩壊から、焚き火の明るさを求める（docs/domain/visual-effects.md）。</summary>
public static class CampfireLight
{
    // 周期の中の位置（0〜1）。昼夜の 4 色を等間隔に置いたときの、夕焼け・夜・朝の位置（docs/domain/ambience.md）。
    private const double LightsUpAt = 0.25;
    private const double PeakAt = 0.5;
    private const double GoesOutAt = 0.75;

    /// <param name="elapsedSeconds">昼夜の周期を数え始めてからの経過秒（<see cref="AmbientLight.Compute"/> と同じもの）。</param>
    /// <returns>0（消えている）〜 1（いちばん明るい）。</returns>
    public static float Brightness(double elapsedSeconds, WorldState world, AmbienceRules rules)
    {
        if (world.IsDestroyed)
        {
            return 0f;
        }

        var position = elapsedSeconds % rules.CycleSeconds / rules.CycleSeconds;
        if (position <= LightsUpAt || position >= GoesOutAt)
        {
            return 0f;
        }
        return (float)(position < PeakAt
            ? (position - LightsUpAt) / (PeakAt - LightsUpAt)
            : (GoesOutAt - position) / (GoesOutAt - PeakAt));
    }
}
