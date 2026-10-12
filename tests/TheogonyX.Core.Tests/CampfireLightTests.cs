using TheogonyX.Core;

namespace TheogonyX.Core.Tests;

public class CampfireLightTests
{
    private const float Tolerance = 0.001f;

    private static float Brightness(double elapsedSeconds, float chaosLevel = 0f, bool isDestroyed = false) =>
        CampfireLight.Brightness(
            elapsedSeconds,
            new WorldState { ChaosLevel = chaosLevel, IsDestroyed = isDestroyed },
            TestRules.Ambience);

    [Theory]
    [Trait("TS", "TS-visual-effects-001")]
    [InlineData(0.0)] // 青空
    [InlineData(1.5)]
    [InlineData(3.0)] // 夕焼け
    [InlineData(9.0)] // 朝
    [InlineData(10.5)]
    public void Brightness_夕焼けより前と朝より後_消えている(double seconds)
    {
        Assert.Equal(0f, Brightness(seconds), Tolerance);
    }

    [Theory]
    [Trait("TS", "TS-visual-effects-002")]
    [InlineData(4.5, 0.5f)] // 夕焼けと夜の中間
    [InlineData(6.0, 1f)] // 夜
    [InlineData(7.5, 0.5f)] // 夜と朝の中間
    public void Brightness_夕焼けから朝まで_夜に向かって明るくなり朝に向かって暗くなる(double seconds, float expected)
    {
        Assert.Equal(expected, Brightness(seconds), Tolerance);
    }

    [Fact]
    [Trait("TS", "TS-visual-effects-003")]
    public void Brightness_12秒を超える_周期が繰り返す()
    {
        Assert.Equal(1f, Brightness(18.0), Tolerance);
    }

    [Fact]
    [Trait("TS", "TS-visual-effects-004")]
    public void Brightness_カオス度100_変わらない()
    {
        Assert.Equal(1f, Brightness(6.0, chaosLevel: 100f), Tolerance);
    }

    [Fact]
    [Trait("TS", "TS-visual-effects-005")]
    public void Brightness_崩壊している_夜でも消えている()
    {
        Assert.Equal(0f, Brightness(6.0, isDestroyed: true), Tolerance);
    }
}
