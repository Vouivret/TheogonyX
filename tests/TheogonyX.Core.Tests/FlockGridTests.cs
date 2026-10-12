using TheogonyX.Core;

namespace TheogonyX.Core.Tests;

public class FlockGridTests
{
    [Theory]
    [Trait("TS", "TS-crowd-015")]
    [InlineData(0.1f, 102)] // 102.4 → 102
    [InlineData(0.2f, 205)] // 204.8 → 205
    [InlineData(-0.5f, -512)]
    [InlineData(1f, 1024)]
    public void ToFixed_小数_1024倍して四捨五入した整数(float value, int expected)
    {
        Assert.Equal(expected, FlockGrid.ToFixed(value));
    }

    [Fact]
    [Trait("TS", "TS-crowd-015")]
    public void Neighborhood_周りの3x3のマス_人数と重心と平均の向き()
    {
        // 地表 16 × 9、マス 0.5 → 32 × 18 マス
        var grid = new FlockGrid(16, 9, 0.5f);
        // マス (0, 0) に向き 0 の人と、マス (1, 1) に向き π/2 の人。マス (5, 5) の人は 3 × 3 の外
        grid.Accumulate([0.25f, 0.75f, 2.75f], [0.25f, 0.75f, 2.75f], [0f, MathF.PI / 2f, 0f]);

        var (count, centerX, centerY, directionX, directionY) = grid.Neighborhood(0, 0);

        Assert.Equal(32, grid.GridColumns);
        Assert.Equal(18, grid.GridRows);
        Assert.Equal(2, count);
        Assert.Equal(0.5f, centerX, 1e-3f);
        Assert.Equal(0.5f, centerY, 1e-3f);
        Assert.Equal(0.5f, directionX, 1e-3f);
        Assert.Equal(0.5f, directionY, 1e-3f);
    }

    [Fact]
    [Trait("TS", "TS-crowd-015")]
    public void Neighborhood_誰もいない_すべて0()
    {
        var grid = new FlockGrid(16, 9, 0.5f);
        grid.Accumulate([15.9f], [8.9f], [0f]);

        Assert.Equal((0, 0f, 0f, 0f, 0f), grid.Neighborhood(0, 0));
    }

    [Fact]
    [Trait("TS", "TS-crowd-015")]
    public void Accumulate_もう一度_前の集計を消して数え直す()
    {
        var grid = new FlockGrid(16, 9, 0.5f);
        grid.Accumulate([0.25f, 0.25f], [0.25f, 0.25f], [0f, 0f]);

        grid.Accumulate([0.25f], [0.25f], [0f]);

        Assert.Equal(1, grid.Neighborhood(0, 0).Count);
    }
}
