namespace TheogonyX.Core;

/// <summary>夢から予言者が読み取るドグマを決める（docs/domain/dream-interpretation.md）。</summary>
public static class DreamInterpreter
{
    /// <returns>生成されたドグマ。夢ノードが2個未満なら null。</returns>
    public static Dogma? Interpret(IReadOnlyList<DreamNode> dream, DreamRules rules)
    {
        ArgumentNullException.ThrowIfNull(dream);
        ArgumentNullException.ThrowIfNull(rules);

        if (dream.Count < 2)
        {
            return null;
        }

        var bestIndex = 0;
        var bestImpact = float.MinValue;
        for (var i = 0; i < dream.Count - 1; i++)
        {
            var impact = dream[i].Weight * MathF.Abs(PolarityOf(dream[i], rules))
                + dream[i + 1].Weight * MathF.Abs(PolarityOf(dream[i + 1], rules));
            // 「以上」で更新するので、同点なら後ろのペアが残る。
            if (impact >= bestImpact)
            {
                bestImpact = impact;
                bestIndex = i;
            }
        }

        var cause = dream[bestIndex];
        var effect = dream[bestIndex + 1];
        // float の誤差（0.6 + -0.9 = -0.29999995）で閾値の判定を外さないよう丸める。
        var polaritySum = MathF.Round(PolarityOf(cause, rules) + PolarityOf(effect, rules), 4);
        var type = polaritySum <= rules.TabooAtOrBelow ? DogmaType.Taboo
            : polaritySum >= rules.WorshipAtOrAbove ? DogmaType.Worship
            : DogmaType.Destruction;

        return new Dogma(cause.SymbolId, type, MathF.Min(1.0f, bestImpact));
    }

    private static float PolarityOf(DreamNode node, DreamRules rules)
    {
        foreach (var symbol in rules.Symbols)
        {
            if (symbol.Id == node.SymbolId)
            {
                return symbol.EmotionalPolarity;
            }
        }
        throw new ArgumentException($"定義に無い記号: symbolId={node.SymbolId}", nameof(rules));
    }
}
