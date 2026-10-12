using TheogonyX.Core;

namespace TheogonyX.Data;

/// <summary>群衆の見た目の調整値（docs/domain/crowd.md）。</summary>
/// <param name="DotSizePixels">1 人の点の大きさ（1〜2 px）。</param>
/// <param name="DotColor">点の色。</param>
public sealed record CrowdLook(int DotSizePixels, LightColor DotColor);
