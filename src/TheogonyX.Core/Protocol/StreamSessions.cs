namespace TheogonyX.Core.Protocol;

/// <summary>
/// 接続ごとの配信の状態（docs/domain/frame-stream.md）。配信中の接続の ID だけを持ち、無いものは「配信していない」。
/// 切断したときは、呼び出し側が <see cref="Stop"/> を呼んで状態を消す。
/// </summary>
public sealed class StreamSessions
{
    private enum Trigger
    {
        StartWithRoom,
        StartWhenFull,
        Stop,
    }

    // 仕様書の状態遷移表と1対1。キーは（配信中か, きっかけ）、値は遷移した後に配信中か。
    private static readonly Dictionary<(bool IsStreaming, Trigger), bool> Transitions = new()
    {
        [(false, Trigger.StartWithRoom)] = true,
        [(false, Trigger.StartWhenFull)] = false,
        [(false, Trigger.Stop)] = false,
        [(true, Trigger.StartWithRoom)] = true,
        [(true, Trigger.StartWhenFull)] = true,
        [(true, Trigger.Stop)] = false,
    };

    private readonly int _maxStreams;
    private readonly List<int> _streamingClients = [];

    /// <param name="maxStreams">同時に配信する接続の上限。</param>
    public StreamSessions(int maxStreams)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxStreams);
        _maxStreams = maxStreams;
    }

    /// <summary>配信中の接続の ID（配信を始めた順）。</summary>
    public IReadOnlyList<int> StreamingClients => _streamingClients;

    public bool IsStreaming(int clientId) => _streamingClients.Contains(clientId);

    /// <summary><c>START_STREAM</c> を受け取ったとき。</summary>
    public StreamStartResult Start(int clientId)
    {
        var wasStreaming = IsStreaming(clientId);
        var trigger = _streamingClients.Count < _maxStreams ? Trigger.StartWithRoom : Trigger.StartWhenFull;
        var isStreaming = Apply(clientId, wasStreaming, trigger);

        if (wasStreaming)
        {
            return StreamStartResult.AlreadyStreaming;
        }

        return isStreaming ? StreamStartResult.Started : StreamStartResult.LimitReached;
    }

    /// <summary><c>STOP_STREAM</c> を受け取ったとき、または切断したとき。配信中だったら true。</summary>
    public bool Stop(int clientId)
    {
        var wasStreaming = IsStreaming(clientId);
        Apply(clientId, wasStreaming, Trigger.Stop);
        return wasStreaming;
    }

    private bool Apply(int clientId, bool wasStreaming, Trigger trigger)
    {
        var isStreaming = Transitions[(wasStreaming, trigger)];
        if (isStreaming && !wasStreaming)
        {
            _streamingClients.Add(clientId);
        }
        else if (!isStreaming && wasStreaming)
        {
            _streamingClients.Remove(clientId);
        }

        return isStreaming;
    }
}
