namespace TheogonyX.Core;

/// <summary>
/// 手札から選んだ 2 枚のルーンから、ドグマを決める（docs/domain/oracle-pair.md）。
/// 衝撃度・種別・強度の式は夢の解釈（<see cref="DreamInterpreter"/>）と同じで、夢ノードの重みの代わりにルーンの重みを使う。
/// </summary>
public static class OracleInterpreter
{
    /// <param name="firstSymbolId">1 枚目のルーンの記号。ドグマの対象になる。</param>
    /// <param name="secondSymbolId">2 枚目のルーンの記号。</param>
    /// <returns>生成されたドグマ。衝撃度が 0 なら null。</returns>
    public static Dogma? Interpret(string firstSymbolId, string secondSymbolId, DreamRules dreamRules, OracleRules oracleRules)
    {
        ArgumentNullException.ThrowIfNull(firstSymbolId);
        ArgumentNullException.ThrowIfNull(secondSymbolId);
        ArgumentNullException.ThrowIfNull(dreamRules);
        ArgumentNullException.ThrowIfNull(oracleRules);

        var firstPolarity = PolarityOf(firstSymbolId, dreamRules);
        var secondPolarity = PolarityOf(secondSymbolId, dreamRules);
        var impact = oracleRules.WeightOf(firstSymbolId) * MathF.Abs(firstPolarity)
            + oracleRules.WeightOf(secondSymbolId) * MathF.Abs(secondPolarity);
        if (impact <= 0f)
        {
            return null;
        }

        // float の誤差（0.6 + -0.9 = -0.29999995）で閾値の判定を外さないよう丸める。
        var polaritySum = MathF.Round(firstPolarity + secondPolarity, 4);
        var type = polaritySum <= dreamRules.TabooAtOrBelow ? DogmaType.Taboo
            : polaritySum >= dreamRules.WorshipAtOrAbove ? DogmaType.Worship
            : DogmaType.Destruction;
        return new Dogma(firstSymbolId, type, MathF.Min(1.0f, impact));
    }

    private static float PolarityOf(string symbolId, DreamRules rules)
    {
        foreach (var symbol in rules.Symbols)
        {
            if (symbol.Id == symbolId)
            {
                return symbol.EmotionalPolarity;
            }
        }
        throw new ArgumentException($"定義に無い記号: symbolId={symbolId}", nameof(rules));
    }
}
