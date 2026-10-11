namespace TheogonyX.Core;

/// <summary>地表の判定に使うゲームデータ定義。</summary>
/// <param name="Columns">マップの列数。</param>
/// <param name="Anomalies">優先度の高い順。</param>
public sealed record TerrainRules(int Columns, IReadOnlyList<TerrainAnomaly> Anomalies);
