namespace TheogonyX.Core;

/// <summary>予言者の依頼の調整値（docs/domain/prophet-request.md）。</summary>
/// <param name="RequestCooldownYears">依頼が済んでから、同じ困りごとの依頼を出さない年数。</param>
/// <param name="Requests">困りごとの一覧（優先の順）。</param>
public sealed record ProphetDreamRules(int RequestCooldownYears, IReadOnlyList<ProphetRequestRule> Requests);
