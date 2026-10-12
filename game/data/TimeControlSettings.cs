namespace TheogonyX.Data;

/// <summary>時間の進め方の調整値（docs/domain/time-control.md）。</summary>
/// <param name="JumpYears">100年跳躍ボタンで進める年数。</param>
/// <param name="NormalYearsPerSecond">自動進行（通常）の速さ。</param>
/// <param name="FastYearsPerSecond">自動進行（高速）の速さ。</param>
/// <param name="RealTimeSecondsPerYear">自動進行（現実同期）で、1 年に掛ける現実の秒数。</param>
public sealed record TimeControlSettings(int JumpYears, float NormalYearsPerSecond, float FastYearsPerSecond, double RealTimeSecondsPerYear);
