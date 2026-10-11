namespace TheogonyX.Core;

/// <summary>時代跳躍に使うゲームデータ定義。</summary>
public sealed record WorldRules(
    EnvironmentValues Initial,
    EnvironmentValues Min,
    EnvironmentValues Max,
    IReadOnlyList<DogmaEffect> DogmaEffects,
    CollapseRules Collapse);
