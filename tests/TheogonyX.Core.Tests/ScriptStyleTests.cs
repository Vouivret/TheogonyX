using TheogonyX.Core;

namespace TheogonyX.Core.Tests;

public class ScriptStyleTests
{
    private static PlanetScript Create(int seed) => PlanetScript.Create(ScriptTests.Rules, ScriptTests.Symbols, seed);

    [Fact]
    [Trait("TS", "TS-script-decipher-022")]
    public void Of_同じ星_同じ書きぶりになり崩れた後は線の端の形が変わる()
    {
        for (var seed = 0; seed < 20; seed++)
        {
            var script = Create(seed);
            var restored = PlanetScript.Restore(ScriptTests.Rules, script.Radicals, script.WordOrder, script.SimilarPair);

            Assert.Equal(ScriptStyle.Of(script, false), ScriptStyle.Of(restored, false));
            Assert.Equal(ScriptStyle.Of(script, true), ScriptStyle.Of(restored, true));
            Assert.NotEqual(ScriptStyle.Of(script, false).Cap, ScriptStyle.Of(script, true).Cap);
        }
    }

    [Fact]
    [Trait("TS", "TS-script-decipher-022")]
    public void Of_20通りの星_書きぶりは星ごとに違う()
    {
        var styles = Enumerable.Range(0, 20).Select(seed => ScriptStyle.Of(Create(seed), false)).ToList();

        Assert.True(styles.Distinct().Count() > 10);
    }

    [Fact]
    [Trait("TS", "TS-script-decipher-022")]
    public void Jitter_同じ場所と年_同じ揺れで大きさは上限まで()
    {
        var shakes = new List<(float X, float Y)>();
        for (var column = 0; column < 8; column++)
        {
            for (var stroke = 0; stroke < 6; stroke++)
            {
                for (var end = 0; end < 2; end++)
                {
                    var shake = ScriptStyle.Jitter(0.1f, column, 3, 120, stroke, end);
                    Assert.Equal(shake, ScriptStyle.Jitter(0.1f, column, 3, 120, stroke, end));
                    Assert.InRange(shake.X, -0.1f, 0.1f);
                    Assert.InRange(shake.Y, -0.1f, 0.1f);
                    shakes.Add(shake);
                }
            }
        }

        Assert.True(shakes.Distinct().Count() > shakes.Count / 2);
        Assert.Equal((0f, 0f), ScriptStyle.Jitter(0f, 1, 2, 3, 4, 1));
    }

    [Fact]
    [Trait("TS", "TS-script-decipher-022")]
    public void Validate_揺れが格子の4分の1を越える_拒む()
    {
        Assert.Throws<ArgumentException>(() => (ScriptTests.Rules with { StyleJitter = 0.3f }).Validate());
        (ScriptTests.Rules with { StyleJitter = 0.25f }).Validate();
    }
}
