namespace TheogonyX.Core;

/// <summary>環境光の色。各成分は 0〜1。</summary>
public readonly record struct LightColor(float R, float G, float B)
{
    /// <summary><paramref name="from"/> から <paramref name="to"/> へ、割合 <paramref name="weight"/>（0〜1）で線形補間する。</summary>
    public static LightColor Lerp(LightColor from, LightColor to, float weight) => new(
        from.R + ((to.R - from.R) * weight),
        from.G + ((to.G - from.G) * weight),
        from.B + ((to.B - from.B) * weight));
}
