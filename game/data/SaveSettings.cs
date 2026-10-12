namespace TheogonyX.Data;

/// <summary>セーブの設定（docs/domain/save-data.md）。</summary>
/// <param name="AutoSaveIntervalSeconds">自動進行中に保存する間隔（現実の秒数）。この時間がたった後の、次の 1 年が進んだときに保存する。</param>
public sealed record SaveSettings(double AutoSaveIntervalSeconds);
