namespace TheogonyX.Data;

/// <summary>時間の操作の調整値（docs/domain/time-control.md）。</summary>
/// <param name="JumpYears">時代跳躍ボタンで進める年数。</param>
/// <param name="SecondsPerYear">自動進行（1 倍）で、1 年に掛ける現実の秒数。2 倍・3 倍は、これを 2 分の 1・3 分の 1 にする。</param>
public sealed record TimeControlSettings(int JumpYears, double SecondsPerYear);
