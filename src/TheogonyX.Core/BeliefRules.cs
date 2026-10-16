namespace TheogonyX.Core;

/// <summary>
/// 住人の信仰に使うゲームデータ定義（docs/domain/dogma-belief.md の「小さな MVP での仮の形」）。
/// 値は仮置き（<c>TODO(AI_UNSURE)</c>）で、遊びながら決める。
/// </summary>
/// <param name="MaxBeliefs">1 人が信じられる教えの数。</param>
/// <param name="SeedRegions">神託を告げたとき、予言者の地域からこの地域数以内の住人が信じ始める。</param>
/// <param name="GatherTransmit">広場に集まった住人どうしで、うつす人の強さに掛けるうつりやすさ。</param>
/// <param name="PartnerTransmit">つがいの相手にうつすときのうつりやすさ。</param>
/// <param name="Dampen">人から人へうつるときに、強さに掛けて弱める倍率。</param>
/// <param name="InheritDampen">親から子へ継ぐときに、強さに掛けて弱める倍率。</param>
public sealed record BeliefRules(
    int MaxBeliefs, int SeedRegions, float GatherTransmit, float PartnerTransmit, float Dampen, float InheritDampen)
{
    /// <summary>値が範囲の中にあることを確かめる。</summary>
    /// <exception cref="ArgumentException">範囲外の値。</exception>
    public void Validate()
    {
        if (MaxBeliefs < 1 || SeedRegions < 0
            || !IsRatio(GatherTransmit) || !IsRatio(PartnerTransmit) || !IsRatio(Dampen) || !IsRatio(InheritDampen))
        {
            throw new ArgumentException($"信仰の定義の値が範囲外: {this}");
        }
    }

    private static bool IsRatio(float value) => float.IsFinite(value) && value is >= 0f and <= 1f;
}
