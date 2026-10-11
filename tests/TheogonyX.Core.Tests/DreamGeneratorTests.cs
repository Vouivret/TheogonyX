using TheogonyX.Core;

namespace TheogonyX.Core.Tests;

public class DreamGeneratorTests
{
    private const int Seed = 20261012;

    [Fact]
    [Trait("TS", "TS-dream-tampering-001")]
    public void Generate_シード付きの乱数_3つの夢ノードが記号と重みの範囲に収まる()
    {
        var symbolIds = TestRules.Dream.Symbols.Select(symbol => symbol.Id).ToList();

        for (var seed = 0; seed < 100; seed++)
        {
            var dream = DreamGenerator.Generate(new Random(seed), TestRules.Dream, TestRules.ProphetDreams);

            Assert.Equal(3, dream.Count);
            Assert.All(dream, node =>
            {
                Assert.Contains(node.SymbolId, symbolIds);
                Assert.InRange(node.Weight, 0.3f, 1.0f);
            });
        }
    }

    [Fact]
    [Trait("TS", "TS-dream-tampering-001")]
    public void Generate_同じシード_同じ夢になる()
    {
        var first = DreamGenerator.Generate(new Random(Seed), TestRules.Dream, TestRules.ProphetDreams);
        var second = DreamGenerator.Generate(new Random(Seed), TestRules.Dream, TestRules.ProphetDreams);

        Assert.Equal(first, second);
    }
}
