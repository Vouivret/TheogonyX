namespace TheogonyX.Core;

/// <summary>
/// 自動進行の時計（docs/domain/time-control.md）。速さと、その年の進み（前の年から何秒たったか）を持つ。
/// 自動進行中だけ、呼び出し側が毎フレーム <see cref="Tick"/> を呼ぶ。進みは一時停止しても残り、セーブに残す。
/// </summary>
public sealed class AutoPlayClock
{
    private readonly IReadOnlyDictionary<AutoPlaySpeed, double> _secondsPerYear;

    /// <param name="secondsPerYear">速さごとの、1 年の秒数。すべての速さを持つ。</param>
    public AutoPlayClock(IReadOnlyDictionary<AutoPlaySpeed, double> secondsPerYear)
    {
        ArgumentNullException.ThrowIfNull(secondsPerYear);
        foreach (var speed in Enum.GetValues<AutoPlaySpeed>())
        {
            if (!secondsPerYear.TryGetValue(speed, out var seconds) || !double.IsFinite(seconds) || seconds <= 0)
            {
                throw new ArgumentException($"1 年の秒数が無いか正でない: speed={speed}", nameof(secondsPerYear));
            }
        }
        _secondsPerYear = secondsPerYear;
    }

    public AutoPlaySpeed Speed { get; private set; } = AutoPlaySpeed.X1;

    /// <summary>その年の進み（秒）。</summary>
    public double ElapsedSeconds { get; private set; }

    /// <summary>今の速さの、1 年の秒数。</summary>
    public double SecondsPerYear => _secondsPerYear[Speed];

    /// <summary>次の年までの残り秒数。</summary>
    public double RemainingSeconds => Math.Max(0, SecondsPerYear - ElapsedSeconds);

    /// <summary>進みに <paramref name="deltaSeconds"/> を足す。</summary>
    /// <returns>1 年たったか。1 回で進めるのは 1 年まで（残りは次の呼び出しに回す）。</returns>
    public bool Tick(double deltaSeconds)
    {
        if (!double.IsFinite(deltaSeconds) || deltaSeconds < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(deltaSeconds), deltaSeconds, "経過した秒数は 0 以上の有限の数");
        }

        ElapsedSeconds += deltaSeconds;
        if (ElapsedSeconds < SecondsPerYear)
        {
            return false;
        }
        ElapsedSeconds -= SecondsPerYear;
        return true;
    }

    /// <summary>1 倍 → 2 倍 → 3 倍 → 1 倍 と回し、進みを 0 にする。</summary>
    public void CycleSpeed()
    {
        Speed = Speed switch
        {
            AutoPlaySpeed.X1 => AutoPlaySpeed.X2,
            AutoPlaySpeed.X2 => AutoPlaySpeed.X3,
            _ => AutoPlaySpeed.X1,
        };
        ElapsedSeconds = 0;
    }

    /// <summary>世界再創生: 進みを 0 にする。速さはそのまま。</summary>
    public void Reset() => ElapsedSeconds = 0;

    /// <summary>セーブから読み込んだ速さと進みに戻す。</summary>
    public void Restore(AutoPlaySpeed speed, double elapsedSeconds)
    {
        if (!Enum.IsDefined(speed))
        {
            throw new ArgumentOutOfRangeException(nameof(speed), speed, "知らない速さ");
        }
        if (!double.IsFinite(elapsedSeconds) || elapsedSeconds < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(elapsedSeconds), elapsedSeconds, "進みは 0 以上の有限の数");
        }
        Speed = speed;
        ElapsedSeconds = elapsedSeconds;
    }
}
