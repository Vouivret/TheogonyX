namespace TheogonyX.Core;

/// <summary>昼夜の環境光のゲームデータ定義（docs/domain/ambience.md）。</summary>
/// <param name="CycleSeconds">昼夜の周期（実際の時間の秒）。</param>
/// <param name="CycleColors">周期の中に等間隔で置く色。周期の始まりから順に並べる。</param>
/// <param name="ChaosColor">カオス度 100 のときの色。</param>
/// <param name="DestroyedColor">星が崩壊したときの色。</param>
public sealed record AmbienceRules(
    float CycleSeconds,
    IReadOnlyList<LightColor> CycleColors,
    LightColor ChaosColor,
    LightColor DestroyedColor);
