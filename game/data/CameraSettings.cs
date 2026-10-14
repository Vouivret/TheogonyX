using System.Collections.Generic;

namespace TheogonyX.Data;

/// <summary>カメラのスクロールとズームの調整値（docs/domain/camera-views.md）。</summary>
/// <param name="ZoomSteps">ホイールで切り替えるズームの段（小さい順。1.0 で世界の 1 px が画面の 1 px）。1.0 より大きい段は整数。</param>
/// <param name="StartZoom">起動したときのズーム（<paramref name="ZoomSteps"/> のどれか）。</param>
/// <param name="ZoomSmoothing">ズームが目標へ近づく速さ（1 秒あたり。大きいほど速い）。</param>
/// <param name="PanSpeed">WASD キーで動かす速さ（画面の px / 秒。ズームによらず同じ）。</param>
/// <param name="ResidentIcons">詳細の視点で、選んだ住人の頭上に欲求の印を出す閾値。</param>
public sealed record CameraSettings(
    IReadOnlyList<double> ZoomSteps, double StartZoom, double ZoomSmoothing, double PanSpeed, ResidentIconSettings ResidentIcons);

/// <summary>住人の欲求の印を出す閾値（docs/domain/camera-views.md）。</summary>
/// <param name="Hunger">空腹（0.0〜1.0。1.0 が切実）がこの値以上なら印を出す。</param>
/// <param name="Shelter">住居（0.0〜1.0。1.0 が切実）がこの値以上なら印を出す。</param>
/// <param name="SanityBelow">SAN 値（0〜100）がこの値以下なら印を出す。</param>
public sealed record ResidentIconSettings(double Hunger, double Shelter, double SanityBelow);
