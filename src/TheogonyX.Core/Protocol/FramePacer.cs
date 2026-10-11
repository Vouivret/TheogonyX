namespace TheogonyX.Core.Protocol;

/// <summary>
/// 画面を送る間隔を、フレームの経過時間（Godot の <c>_Process</c> の delta）から決める（docs/domain/frame-stream.md）。
/// 実時間の時計は持たない。処理落ちしても、たまったコマをまとめて送らない。
/// </summary>
public sealed class FramePacer
{
    private readonly double _intervalSeconds;
    private double _elapsedSeconds;

    public FramePacer(int framesPerSecond)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(framesPerSecond);
        _intervalSeconds = 1.0 / framesPerSecond;
    }

    /// <summary>経過時間を進め、このフレームで 1 コマ送るなら true を返す。</summary>
    public bool Advance(double deltaSeconds)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(deltaSeconds);

        _elapsedSeconds += deltaSeconds;
        if (_elapsedSeconds < _intervalSeconds)
        {
            return false;
        }

        _elapsedSeconds -= _intervalSeconds;
        if (_elapsedSeconds >= _intervalSeconds)
        {
            _elapsedSeconds = 0;
        }

        return true;
    }

    /// <summary>配信中の接続が無くなったとき、経過時間を 0 に戻す。</summary>
    public void Reset() => _elapsedSeconds = 0;
}
