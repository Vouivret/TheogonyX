using System;
using System.Collections.Generic;

namespace TheogonyX.Core;

/// <summary>
/// カメラのスクロールとズームの計算（docs/domain/camera-views.md）。見た目だけで、シミュレーションには影響しない。
/// 座標は世界（地表を描く <c>SubViewport</c> の中）の px、ズームは 1.0 で世界の 1 px が画面（480 × 270）の 1 px になる倍率。
/// </summary>
public static class CameraZoom
{
    // ズームの補間をここまで近づいたら目標に合わせる（倍率の対数の差）
    private const double SnapLogDistance = 0.001;

    /// <summary>ホイール 1 目盛りで移る先のズームの段。端の段から外へは動かない。</summary>
    /// <param name="steps">ズームの段（小さい順）。</param>
    /// <param name="target">今の目標の倍率（補間の途中でも、目標から数える）。</param>
    /// <param name="direction">正なら 1 段大きく、負なら 1 段小さく。</param>
    public static double NextStep(IReadOnlyList<double> steps, double target, int direction)
    {
        if (direction > 0)
        {
            for (var i = 0; i < steps.Count; i++)
            {
                if (steps[i] > target)
                {
                    return steps[i];
                }
            }
            return steps[^1];
        }
        for (var i = steps.Count - 1; i >= 0; i--)
        {
            if (steps[i] < target)
            {
                return steps[i];
            }
        }
        return steps[0];
    }

    /// <summary>ズームを目標へ滑らかに近づける（倍率の対数で補間するので、拡大と縮小の速さがそろう）。目標を越えない。</summary>
    /// <param name="smoothing">近づく速さ（1 秒あたり。大きいほど速い）。</param>
    public static double Approach(double current, double target, double smoothing, double deltaSeconds)
    {
        var from = Math.Log(current);
        var to = Math.Log(target);
        var next = from + (to - from) * (1 - Math.Exp(-smoothing * deltaSeconds));
        return Math.Abs(to - next) < SnapLogDistance ? target : Math.Exp(next);
    }

    /// <summary>ズームを変えても、画面上の <paramref name="anchorOffset"/> の位置にある世界の点が動かないカメラの中心（1 軸ぶん）。</summary>
    /// <param name="center">今のカメラの中心。</param>
    /// <param name="anchorOffset">画面の中心から見た、マウスの位置（画面の px）。</param>
    public static double ZoomAround(double center, double anchorOffset, double fromZoom, double toZoom) =>
        center + anchorOffset / fromZoom - anchorOffset / toZoom;

    /// <summary>
    /// 範囲の外を見せないようにしたカメラの中心（1 軸ぶん）。見えている幅が範囲以上なら、範囲の中央に置く。
    /// </summary>
    /// <param name="viewSize">画面の幅（画面の px）。</param>
    /// <param name="boundsMin">動かせる範囲の端（世界の px）。</param>
    public static double ClampCenter(double center, double zoom, double viewSize, double boundsMin, double boundsMax)
    {
        var half = viewSize / zoom / 2;
        if (half * 2 >= boundsMax - boundsMin)
        {
            return (boundsMin + boundsMax) / 2;
        }
        return Math.Clamp(center, boundsMin + half, boundsMax - half);
    }

    /// <summary>
    /// 整数倍のズームで、ドットが画面の px にそろうカメラの中心（1 軸ぶん。1 / 倍率 の倍数に丸める）。整数倍でなければそのまま返す。
    /// </summary>
    public static double SnapCenter(double center, double zoom)
    {
        if (zoom < 1 || zoom != Math.Floor(zoom))
        {
            return center;
        }
        return Math.Round(center * zoom, MidpointRounding.AwayFromZero) / zoom;
    }
}
