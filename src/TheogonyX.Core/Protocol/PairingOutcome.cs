namespace TheogonyX.Core.Protocol;

/// <summary><see cref="CompanionConnection"/> がきっかけを受け取った結果。ホストが次にすること。</summary>
public enum PairingOutcome
{
    /// <summary>ペアリング済みになった。<c>PAIRED</c> を返す。</summary>
    Paired,

    /// <summary><c>ERROR</c>（<c>PAIRING_FAILED</c>）を返して接続を閉じる。</summary>
    Rejected,

    /// <summary>捨てて <c>Warn</c> で記録する。接続はそのまま。</summary>
    Ignored,

    /// <summary>ペアリング済みなので、メッセージを処理してよい。</summary>
    Accepted,
}
