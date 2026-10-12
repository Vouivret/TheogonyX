using TheogonyX.Core;

namespace TheogonyX.Data;

/// <summary>群衆の見た目と計算の方式の調整値（docs/domain/crowd.md）。</summary>
/// <param name="DotSizePixels">1 人の点の大きさ（1〜2 px）。</param>
/// <param name="DotColor">点の色。</param>
/// <param name="UseGpu">GPU（Compute Shader）で計算するか。false なら CPU 版に固定する。使えない環境では true でも CPU 版になる。</param>
public sealed record CrowdLook(int DotSizePixels, LightColor DotColor, bool UseGpu);
