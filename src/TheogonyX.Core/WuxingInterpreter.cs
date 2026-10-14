namespace TheogonyX.Core;

/// <summary>2 枚目から見た、対象（1 枚目）との五行の関係（docs/domain/oracle-wuxing.md の「どうする」）。</summary>
public enum WuxingRelation
{
    /// <summary>同じ五行（比和）。</summary>
    Same,

    /// <summary>2 枚目が対象を生む（相生）。</summary>
    Generated,

    /// <summary>2 枚目が対象を剋す（相剋）。</summary>
    Overcome,

    /// <summary>対象が 2 枚目を剋す（逆らう）。</summary>
    Rebel,

    /// <summary>対象が 2 枚目を生む（漏れる）。</summary>
    Leak,
}

/// <summary>神託の、五行の関係による判定の結果。</summary>
/// <param name="Relation">五行の関係。どちらかが穢れなら null。</param>
/// <param name="Type">ドグマの種別。</param>
/// <param name="Multiplier">強度の倍率。</param>
public sealed record WuxingJudgement(WuxingRelation? Relation, DogmaType Type, float Multiplier);

/// <summary>
/// 手札の 2 枚の五行の関係から、ドグマを決める（docs/domain/oracle-wuxing.md）。
/// 1 枚目が対象（何を）、2 枚目から見た関係が種別（どうする）と強度の倍率を決める。乱数は使わない。
/// </summary>
public static class WuxingInterpreter
{
    // 仕様書の「どうする」の表と 1 対 1。添字は (1 枚目の位置 − 2 枚目の位置) mod 5（相生の並びの位置）。
    private static readonly (WuxingRelation Relation, DogmaType Type)[] Relations =
    [
        (WuxingRelation.Same, DogmaType.Worship),
        (WuxingRelation.Generated, DogmaType.Worship),
        (WuxingRelation.Overcome, DogmaType.Taboo),
        (WuxingRelation.Rebel, DogmaType.Destruction),
        (WuxingRelation.Leak, DogmaType.Worship),
    ];

    /// <summary>2 枚のカードの関係・種別・倍率。どちらかが穢れなら、関係は null で破壊、倍率は 1.0。</summary>
    /// <exception cref="ArgumentException">記号が五行にも穢れにも無い。</exception>
    public static WuxingJudgement Judge(string firstCardId, string secondCardId, OracleRules oracleRules)
    {
        ArgumentNullException.ThrowIfNull(firstCardId);
        ArgumentNullException.ThrowIfNull(secondCardId);
        ArgumentNullException.ThrowIfNull(oracleRules);
        var wuxing = oracleRules.Wuxing ?? throw new ArgumentException("五行の定義が無い", nameof(oracleRules));

        var first = PositionOf(oracleRules.SymbolOf(firstCardId), wuxing);
        var second = PositionOf(oracleRules.SymbolOf(secondCardId), wuxing);
        if (first is null || second is null)
        {
            return new WuxingJudgement(null, DogmaType.Destruction, 1.0f);
        }

        var count = wuxing.Cycle.Count;
        var (relation, type) = Relations[(((first.Value - second.Value) % count) + count) % count];
        return new WuxingJudgement(relation, type, wuxing.Multipliers.Of(relation));
    }

    /// <summary>神託のドグマ。強度 = min(1.0, 2 枚の重みの平均 × 倍率)。強度が 0 なら null。</summary>
    public static Dogma? Interpret(string firstCardId, string secondCardId, OracleRules oracleRules)
    {
        var judgement = Judge(firstCardId, secondCardId, oracleRules);
        var firstSymbolId = oracleRules.SymbolOf(firstCardId);
        var average = (oracleRules.WeightOf(firstSymbolId) + oracleRules.WeightOf(oracleRules.SymbolOf(secondCardId))) / 2f;
        var intensity = MathF.Min(1.0f, average * judgement.Multiplier);
        return intensity <= 0f ? null : new Dogma(firstSymbolId, judgement.Type, intensity);
    }

    // 相生の並びでの位置。穢れなら null
    private static int? PositionOf(string symbolId, WuxingRules wuxing)
    {
        for (var i = 0; i < wuxing.Cycle.Count; i++)
        {
            if (wuxing.Cycle[i] == symbolId)
            {
                return i;
            }
        }
        return wuxing.Impure.Contains(symbolId)
            ? null
            : throw new ArgumentException($"五行にも穢れにも無い記号: symbolId={symbolId}", nameof(symbolId));
    }
}
