using TheogonyX.Core;

namespace TheogonyX.Core.Tests;

public class CameraZoomTests
{
    private const double Tolerance = 0.0001;

    private static readonly double[] Steps = [0.25, 0.5, 0.75, 1.0, 2.0, 3.0, 4.0];

    [Theory]
    [Trait("TS", "TS-camera-views-001")]
    [InlineData(1.0, 1, 2.0)]
    [InlineData(1.0, -1, 0.75)]
    [InlineData(4.0, 1, 4.0)] // いちばん大きい段
    [InlineData(0.25, -1, 0.25)] // いちばん小さい段
    [InlineData(1.5, 1, 2.0)] // 段の間（補間の途中で目標を変えたとき）
    [InlineData(1.5, -1, 1.0)]
    public void NextStep_ホイール1目盛り_隣の段に移り端の段からは動かない(double target, int direction, double expected)
    {
        Assert.Equal(expected, CameraZoom.NextStep(Steps, target, direction), Tolerance);
    }

    [Fact]
    [Trait("TS", "TS-camera-views-002")]
    public void Approach_1フレーム_今と目標の間に近づく()
    {
        var zoom = CameraZoom.Approach(1.0, 2.0, 12.0, 1.0 / 60);

        Assert.InRange(zoom, 1.0 + Tolerance, 2.0 - Tolerance);
    }

    [Theory]
    [Trait("TS", "TS-camera-views-002")]
    [InlineData(1.0, 2.0)]
    [InlineData(2.0, 1.0)]
    public void Approach_十分に時間がたつ_目標ちょうどになる(double current, double target)
    {
        Assert.Equal(target, CameraZoom.Approach(current, target, 12.0, 10.0));
    }

    [Theory]
    [Trait("TS", "TS-camera-views-003")]
    [InlineData(240.0, -240.0, 1.0, 2.0, 120.0)] // 画面の左端の世界の点 0 が、2 倍でも左端に残る
    [InlineData(135.0, -135.0, 1.0, 2.0, 67.5)]
    [InlineData(120.0, 240.0, 2.0, 1.0, 0.0)] // 右端の世界の点 240 が、1 倍でも右端に残る
    [InlineData(240.0, 0.0, 1.0, 4.0, 240.0)] // 画面の中心なら中心は動かない
    public void ZoomAround_ズームを変える_マウスの下の世界の点が動かない(
        double center, double anchorOffset, double fromZoom, double toZoom, double expected)
    {
        Assert.Equal(expected, CameraZoom.ZoomAround(center, anchorOffset, fromZoom, toZoom), Tolerance);
    }

    [Theory]
    [Trait("TS", "TS-camera-views-004")]
    [InlineData(50.0, 2.0, 120.0)] // 2 倍では見える幅が 240 px なので、中心は 120〜360
    [InlineData(400.0, 2.0, 360.0)]
    [InlineData(200.0, 2.0, 200.0)]
    [InlineData(50.0, 1.0, 240.0)] // 見える幅が範囲と同じなら中央
    [InlineData(400.0, 0.5, 240.0)] // 見える幅が範囲より広いなら中央
    public void ClampCenter_範囲は横480px_外を見せない(double center, double zoom, double expected)
    {
        Assert.Equal(expected, CameraZoom.ClampCenter(center, zoom, 480.0, 0.0, 480.0), Tolerance);
    }

    [Theory]
    [Trait("TS", "TS-camera-views-007")]
    [InlineData(1, 1.0, 340.0)] // 画面の上で 1 秒に 100 px なので、1 倍では世界の 100 px
    [InlineData(1, 2.0, 290.0)] // 2 倍では世界の 50 px
    [InlineData(1, 0.5, 440.0)] // 0.5 倍では世界の 200 px
    [InlineData(-1, 1.0, 140.0)]
    [InlineData(0, 1.0, 240.0)]
    public void Pan_速さ100で1秒_画面の上で100px動く(int direction, double zoom, double expected)
    {
        Assert.Equal(expected, CameraZoom.Pan(240.0, direction, 100.0, zoom, 1.0), Tolerance);
    }

    [Theory]
    [Trait("TS", "TS-camera-views-005")]
    [InlineData(120.3, 2.0, 120.5)]
    [InlineData(100.2, 3.0, 100.0 + 1.0 / 3)]
    [InlineData(100.2, 1.0, 100.0)]
    [InlineData(100.2, 0.75, 100.2)] // 整数倍でなければ丸めない
    [InlineData(100.2, 0.5, 100.2)]
    public void SnapCenter_整数倍のズーム_1割る倍率の倍数に丸める(double center, double zoom, double expected)
    {
        Assert.Equal(expected, CameraZoom.SnapCenter(center, zoom), Tolerance);
    }

    [Theory]
    [Trait("TS", "TS-camera-views-005")]
    [InlineData(0.5, 0.0)]
    [InlineData(1.0, 0.0)]
    [InlineData(1.5, 0.5)]
    [InlineData(2.0, 1.0)]
    [InlineData(4.0, 1.0)]
    public void DetailBlend_範囲の中で直線に変わり外では0か1(double zoom, double expected)
    {
        Assert.Equal(expected, CameraZoom.DetailBlend(zoom, 1.0, 2.0), Tolerance);
    }

    [Fact]
    [Trait("TS", "TS-camera-views-005")]
    public void DetailBlend_範囲が正しくない_例外になる()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => CameraZoom.DetailBlend(1.0, 2.0, 2.0));
    }
}
