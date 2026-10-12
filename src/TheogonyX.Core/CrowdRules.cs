namespace TheogonyX.Core;

/// <summary>群衆のゲームデータ定義（docs/domain/crowd.md）。長さの単位はタイル（1 = 地表のタイル 1 枚）。</summary>
/// <param name="Count">人数。</param>
/// <param name="WalkSpeed">散策の歩く速さ（タイル/秒）。</param>
/// <param name="TurnIntervalSeconds">散策で向きを選び直す間隔（秒）。</param>
/// <param name="FrenzySpeed">狂乱の速さ（タイル/秒）。</param>
/// <param name="HolySiteRadius">聖地の周りに留まる範囲の半径（タイル）。</param>
/// <param name="FrenzyChaosAtOrAbove">狂乱になるカオス度。</param>
/// <param name="FieldScale">ポテンシャルフィールドの力を速さ（タイル/秒）にする倍率。</param>
/// <param name="FastTimeScale">自動進行が高速のとき、群衆の時間を何倍で進めるか。</param>
/// <param name="TileForces">タイル種別ごとの力。正は引き寄せ、負は遠ざける。</param>
public sealed record CrowdRules(
    int Count,
    float WalkSpeed,
    float TurnIntervalSeconds,
    float FrenzySpeed,
    float HolySiteRadius,
    float FrenzyChaosAtOrAbove,
    float FieldScale,
    float FastTimeScale,
    IReadOnlyDictionary<TileType, float> TileForces);
