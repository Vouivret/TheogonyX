namespace TheogonyX.Data;

/// <summary>法線マップの陰影と落ち影の調整値（docs/domain/visual-effects.md）。</summary>
/// <param name="DirectionDegrees">光の向き（<c>DirectionalLight2D</c> の回転。0 で真下へ、正で時計回り。-45 で左上から右下へ）。</param>
/// <param name="Height">光の高さ（<c>DirectionalLight2D.Height</c>。0〜1。1 で真上から当たり、陰影が出なくなる）。</param>
/// <param name="Energy">昼のいちばん明るいときの光の強さ。環境光の明るさに合わせて弱める。</param>
/// <param name="ShadowOpacity">落ち影の濃さ（0〜1）。</param>
/// <param name="ShadowWidthRatio">落ち影の幅（絵の 1 コマの幅に対する割合。0〜1）。</param>
/// <param name="ShadowHeight">落ち影の高さ（px）。</param>
public sealed record LightingSettings(
    float DirectionDegrees, float Height, float Energy, float ShadowOpacity, float ShadowWidthRatio, int ShadowHeight);
