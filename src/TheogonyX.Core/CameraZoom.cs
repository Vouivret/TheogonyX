using System;
using System.Collections.Generic;

namespace TheogonyX.Core;

/// <summary>
/// カメラのスクロールとズームの計算（docs/domain/camera-views.md）。見た目だけで、シミュレーションには影響しない。
/// 座標は世界（地表を描く <c>SubViewport</c> の中）の px、ズームは 1.0 で世界の 1 px が画面（480 × 270）の 1 px になる倍率。
/// </summary>
public static class CameraZoom
{
    /// <summary>街の視点になる倍率（これ未満は星の視点）。</summary>
    public const double TownFromZoom = 1.0;

    /// <summary>暮らしの視点になる倍率（これ未満は街の視点）。</summary>
    public const double LifeFromZoom = 2.0;

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

    /// <summary>ズームを変えても、画面上の <paramref name="anchorOffset"/> の位置にある世界の点が動かないカメラの中心（1 軸ぶん）。</summary>
    /// <param name="center">今のカメラの中心。</param>
    /// <param name="anchorOffset">画面の中心から見た、マウスの位置（画面の px）。</param>
    public static double ZoomAround(double center, double anchorOffset, double fromZoom, double toZoom) =>
        center + anchorOffset / fromZoom - anchorOffset / toZoom;

    /// <summary>キーでカメラを動かした後の中心（1 軸ぶん）。画面の上での速さは、ズームによらず同じ。</summary>
    /// <param name="direction">−1（左・上）、0、1（右・下）。</param>
    /// <param name="speed">画面の上での速さ（画面の px / 秒）。</param>
    public static double Pan(double center, int direction, double speed, double zoom, double deltaSeconds) =>
        center + direction * speed * deltaSeconds / zoom;

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

    /// <summary>東西がつながった星で、カメラの中心を 1 周の範囲 [<paramref name="origin"/>, <paramref name="origin"/> + <paramref name="circumference"/>) に戻す（1 軸ぶん）。</summary>
    public static double WrapCenter(double center, double origin, double circumference)
    {
        var offset = (center - origin) % circumference;
        return origin + (offset < 0 ? offset + circumference : offset);
    }

    /// <summary>倍率から視点の段を決める。<see cref="TownFromZoom"/> 未満は星、<see cref="LifeFromZoom"/> 未満は街、それ以上は暮らし。</summary>
    public static CameraViewLevel LevelOf(double zoom) =>
        zoom < TownFromZoom ? CameraViewLevel.Star : zoom < LifeFromZoom ? CameraViewLevel.Town : CameraViewLevel.Life;

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

/// <summary>視点の 3 段。ホイールの 1 目盛りで切り替わる。</summary>
public enum CameraViewLevel
{
    /// <summary>星の視点: 地表全体と、群衆の点。</summary>
    Star,

    /// <summary>街の視点: 家と住人の絵。</summary>
    Town,

    /// <summary>暮らしの視点: 住人の営みを見る（欲求の印・行動）。</summary>
    Life,
}
