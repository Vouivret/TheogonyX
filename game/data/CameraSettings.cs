using System.Collections.Generic;

namespace TheogonyX.Data;

/// <summary>カメラのスクロールとズームの調整値（docs/domain/camera-views.md）。</summary>
/// <param name="ZoomSteps">ホイールで切り替えるズームの段（小さい順。1.0 で世界の 1 px が画面の 1 px）。1.0 より大きい段は整数。</param>
/// <param name="StartZoom">起動したときのズーム（<paramref name="ZoomSteps"/> のどれか）。</param>
/// <param name="ZoomSmoothing">ズームが目標へ近づく速さ（1 秒あたり。大きいほど速い）。</param>
/// <param name="PanSpeed">WASD キーで動かす速さ（画面の px / 秒。ズームによらず同じ）。</param>
public sealed record CameraSettings(IReadOnlyList<double> ZoomSteps, double StartZoom, double ZoomSmoothing, double PanSpeed);
