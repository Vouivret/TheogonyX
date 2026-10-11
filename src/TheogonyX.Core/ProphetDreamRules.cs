namespace TheogonyX.Core;

/// <summary>予言者の夢と依頼の調整値（docs/domain/dream-tampering.md、prophet-request.md）。</summary>
/// <param name="NodeCount">夢の夢ノードの数。</param>
/// <param name="MinWeight">夢ノードの重みの下限。</param>
/// <param name="MaxWeight">夢ノードの重みの上限。</param>
/// <param name="YearsBetweenDreams">前の神託から、次の夢を見るまでの年数。</param>
/// <param name="RequestCooldownYears">依頼が済んでから、同じ困りごとの依頼を出さない年数。</param>
/// <param name="Requests">困りごとの一覧（優先の順）。</param>
public sealed record ProphetDreamRules(
    int NodeCount,
    float MinWeight,
    float MaxWeight,
    int YearsBetweenDreams,
    int RequestCooldownYears,
    IReadOnlyList<ProphetRequestRule> Requests);
