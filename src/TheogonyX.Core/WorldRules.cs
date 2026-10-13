namespace TheogonyX.Core;

/// <summary>時代跳躍に使うゲームデータ定義。</summary>
/// <param name="ChaosDecayPerYear">カオス度が毎年自然に減る量（ドグマの効果と合算し、下限を下回らない。docs/domain/time-skip.md）。</param>
public sealed record WorldRules(
    EnvironmentValues Initial,
    EnvironmentValues Min,
    EnvironmentValues Max,
    IReadOnlyList<DogmaEffect> DogmaEffects,
    CollapseRules Collapse,
    float ChaosDecayPerYear = 0f);
