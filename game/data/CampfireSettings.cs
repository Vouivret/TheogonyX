using TheogonyX.Core;

namespace TheogonyX.Data;

/// <summary>焚き火の表示の調整値（docs/domain/visual-effects.md）。</summary>
/// <param name="Column">置く地表の列（左から 1 列目と数える）。</param>
/// <param name="Color">明かりの色。</param>
/// <param name="Energy">いちばん明るいとき（夜）の明かりの強さ（<c>PointLight2D.Energy</c>）。</param>
/// <param name="RadiusTiles">明かりの届く半径（タイルの数）。</param>
/// <param name="FlickerAmount">揺らぎの幅（明かりの強さに対する割合。0〜1）。</param>
/// <param name="FlickerSpeed">揺らぎの速さ（大きいほど速く揺らぐ）。</param>
public sealed record CampfireSettings(int Column, LightColor Color, float Energy, float RadiusTiles, float FlickerAmount, float FlickerSpeed);
