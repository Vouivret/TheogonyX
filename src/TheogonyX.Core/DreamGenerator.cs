namespace TheogonyX.Core;

/// <summary>予言者が見る夢を、シード付きの乱数で作る（docs/domain/dream-tampering.md）。</summary>
public static class DreamGenerator
{
    /// <summary>夢ノードを決めた数だけ作る。記号は定義の記号から、重みは下限〜上限から選ぶ。</summary>
    public static List<DreamNode> Generate(Random random, DreamRules dreamRules, ProphetDreamRules rules)
    {
        ArgumentNullException.ThrowIfNull(random);
        ArgumentNullException.ThrowIfNull(dreamRules);
        ArgumentNullException.ThrowIfNull(rules);

        var dream = new List<DreamNode>(rules.NodeCount);
        for (var i = 0; i < rules.NodeCount; i++)
        {
            var symbol = dreamRules.Symbols[random.Next(dreamRules.Symbols.Count)];
            var weight = rules.MinWeight + (float)random.NextDouble() * (rules.MaxWeight - rules.MinWeight);
            dream.Add(new DreamNode(symbol.Id, weight));
        }
        return dream;
    }
}
