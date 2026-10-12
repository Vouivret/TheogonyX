namespace TheogonyX.Core;

/// <summary>群衆の群れの動き（Boids の簡略版。docs/domain/crowd.md）のゲームデータ定義。散策のときだけ使う。</summary>
/// <param name="Enabled">群れの動きを使うか。</param>
/// <param name="CellSize">集計のマスの大きさ（タイル）。周りは自分のマスを中心とした 3 × 3 のマス。</param>
/// <param name="AlignWeight">周りの平均の向きに寄せる強さ（歩く速さに対する倍率）。</param>
/// <param name="CrowdWeight">周りの人数を目標に近づける強さ（歩く速さに対する倍率）。</param>
/// <param name="TargetNeighbors">周り（3 × 3 のマス、自分を含む）の人数の目標。</param>
public sealed record FlockRules(
    bool Enabled,
    float CellSize,
    float AlignWeight,
    float CrowdWeight,
    float TargetNeighbors);
