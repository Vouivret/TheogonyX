namespace TheogonyX.Core.Protocol;

/// <summary><see cref="StreamSessions.Start"/> の結果。</summary>
public enum StreamStartResult
{
    /// <summary>配信していない状態から、配信中になった。</summary>
    Started,

    /// <summary>すでに配信中だった。何も変わらない。</summary>
    AlreadyStreaming,

    /// <summary>配信中の接続が上限に達していて、配信中にならなかった。</summary>
    LimitReached,
}
