using TheogonyX.Core;

namespace TheogonyX.Core.Tests;

public class AmbientLightTests
{
    private const float Tolerance = 0.001f;

    private static LightColor Compute(double elapsedSeconds, float chaosLevel = 0f, bool isDestroyed = false) =>
        AmbientLight.Compute(
            elapsedSeconds,
            new WorldState { ChaosLevel = chaosLevel, IsDestroyed = isDestroyed },
            TestRules.Ambience);

    private static void AssertColor(float r, float g, float b, LightColor actual)
    {
        Assert.Equal(r / 255f, actual.R, Tolerance);
        Assert.Equal(g / 255f, actual.G, Tolerance);
        Assert.Equal(b / 255f, actual.B, Tolerance);
    }

    [Theory]
    [Trait("TS", "TS-ambience-001")]
    [InlineData(0.0, 255f, 255f, 255f)] // 青空
    [InlineData(3.0, 210f, 125f, 44f)] // 夕焼け
    public void Compute_3秒おきの点_その時刻の色そのもの(double seconds, float r, float g, float b)
    {
        AssertColor(r, g, b, Compute(seconds));
    }

    [Theory]
    [Trait("TS", "TS-ambience-002")]
    [InlineData(1.5, 232.5f, 190f, 149.5f)] // 青空と夕焼けの中間
    [InlineData(10.5, 238.5f, 246.5f, 234.5f)] // 朝と青空の中間
    public void Compute_点と点の間_線形補間になる(double seconds, float r, float g, float b)
    {
        AssertColor(r, g, b, Compute(seconds));
    }

    [Fact]
    [Trait("TS", "TS-ambience-003")]
    public void Compute_12秒を超える_周期が繰り返す()
    {
        AssertColor(210f, 125f, 44f, Compute(15.0));
    }

    [Theory]
    [Trait("TS", "TS-ambience-004")]
    [InlineData(6.0, 50f, 128f, 61f, 90.5f)] // 夜と紫赤の中間
    [InlineData(0.0, 100f, 208f, 70f, 72f)] // 紫赤そのもの
    public void Compute_カオス度がある_その割合で紫赤に寄る(double seconds, float chaosLevel, float r, float g, float b)
    {
        AssertColor(r, g, b, Compute(seconds, chaosLevel));
    }

    [Fact]
    [Trait("TS", "TS-ambience-005")]
    public void Compute_崩壊している_時刻とカオス度に関係なく赤黒()
    {
        AssertColor(68f, 36f, 52f, Compute(3.0, chaosLevel: 40f, isDestroyed: true));
    }
}
