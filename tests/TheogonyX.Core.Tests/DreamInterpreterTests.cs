using TheogonyX.Core;

namespace TheogonyX.Core.Tests;

public class DreamInterpreterTests
{
    private const float Tolerance = 0.001f;

    private static Dogma? Interpret(params DreamNode[] dream) => DreamInterpreter.Interpret(dream, TestRules.Dream);

    private static void AssertDogma(Dogma? actual, string symbolId, DogmaType type, float intensity)
    {
        Assert.NotNull(actual);
        Assert.Equal(symbolId, actual.TargetSymbolId);
        Assert.Equal(type, actual.Type);
        Assert.Equal(intensity, actual.Intensity, Tolerance);
    }

    [Fact]
    [Trait("TS", "TS-dream-interpretation-001")]
    public void Interpret_木と血で衝撃度が1を超える_強度が1に抑えられる()
    {
        var dogma = Interpret(new("SYM_TREE", 0.5f), new("SYM_BLOOD", 0.9f));

        AssertDogma(dogma, "SYM_TREE", DogmaType.Taboo, 1.0f);
    }

    [Fact]
    [Trait("TS", "TS-dream-interpretation-002")]
    public void Interpret_極性の和がちょうど禁忌の境界_禁忌になる()
    {
        var dogma = Interpret(new("SYM_RAIN", 1.0f), new("SYM_BLOOD", 1.0f));

        AssertDogma(dogma, "SYM_RAIN", DogmaType.Taboo, 1.0f);
    }

    [Fact]
    [Trait("TS", "TS-dream-interpretation-003")]
    public void Interpret_木と雨_崇拝になる()
    {
        var dogma = Interpret(new("SYM_TREE", 0.5f), new("SYM_RAIN", 0.5f));

        AssertDogma(dogma, "SYM_TREE", DogmaType.Worship, 0.5f);
    }

    [Fact]
    [Trait("TS", "TS-dream-interpretation-004")]
    public void Interpret_火と火で極性の和が禁忌と崇拝の間_破壊になる()
    {
        var dogma = Interpret(new("SYM_FIRE", 1.0f), new("SYM_FIRE", 1.0f));

        AssertDogma(dogma, "SYM_FIRE", DogmaType.Destruction, 0.2f);
    }

    [Fact]
    [Trait("TS", "TS-dream-interpretation-005")]
    public void Interpret_3個の夢_衝撃度が最大の雨と血が選ばれる()
    {
        var dogma = Interpret(new("SYM_TREE", 0.5f), new("SYM_RAIN", 0.5f), new("SYM_BLOOD", 1.0f));

        AssertDogma(dogma, "SYM_RAIN", DogmaType.Taboo, 1.0f);
    }

    [Fact]
    [Trait("TS", "TS-dream-interpretation-006")]
    public void Interpret_衝撃度が同点_後ろのペアが選ばれる()
    {
        var dogma = Interpret(new("SYM_RAIN", 0.5f), new("SYM_TREE", 0.5f), new("SYM_RAIN", 0.5f));

        AssertDogma(dogma, "SYM_TREE", DogmaType.Worship, 0.5f);
    }

    [Fact]
    [Trait("TS", "TS-dream-interpretation-007")]
    public void Interpret_夢ノードが0個か1個_ドグマ無し()
    {
        Assert.Null(Interpret());
        Assert.Null(Interpret(new DreamNode("SYM_BLOOD", 1.0f)));
    }
}
