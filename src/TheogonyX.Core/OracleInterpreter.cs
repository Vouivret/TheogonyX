namespace TheogonyX.Core;

/// <summary>
/// 手札から選んだ 2 枚のルーンから、ドグマを決める（docs/domain/oracle-pair.md）。
/// 種別の決め方は <see cref="OracleRules.TypeRule"/> で選ぶ。極性の和（旧い方式）では、衝撃度・種別・強度の式は夢の解釈と同じで、
/// 夢ノードの重みの代わりにルーンの重みを使う。五行の関係では <see cref="WuxingInterpreter"/> に任せる。
/// </summary>
public static class OracleInterpreter
{
    /// <param name="firstCardId">1 枚目のカード（記号の ID か汚染ルーンの ID）。持つ記号がドグマの対象になる。</param>
    /// <param name="secondCardId">2 枚目のカード。</param>
    /// <returns>生成されたドグマ。衝撃度が 0 なら null。</returns>
    public static Dogma? Interpret(string firstCardId, string secondCardId, DreamRules dreamRules, OracleRules oracleRules)
    {
        ArgumentNullException.ThrowIfNull(firstCardId);
        ArgumentNullException.ThrowIfNull(secondCardId);
        ArgumentNullException.ThrowIfNull(dreamRules);
        ArgumentNullException.ThrowIfNull(oracleRules);
        if (oracleRules.TypeRule == OracleTypeRule.Wuxing)
        {
            return WuxingInterpreter.Interpret(firstCardId, secondCardId, oracleRules);
        }

        var firstSymbolId = oracleRules.SymbolOf(firstCardId);
        var secondSymbolId = oracleRules.SymbolOf(secondCardId);

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
