namespace TheogonyX.Data;

/// <summary>コンパニオンとの通信の設定（docs/domain/companion-pairing.md・frame-stream.md）。</summary>
/// <param name="HttpPort">コンパニオンの HTML/JS を配るポート。</param>
/// <param name="WebSocketPort">WebSocket を待ち受けるポート。<c>companion/js/protocol.js</c> の <c>WEBSOCKET_PORT</c> と同じにする。</param>
/// <param name="PairingTimeoutSeconds">接続してからペアリングを待つ秒数。</param>
/// <param name="MaxStreams">同時に画面を配信する接続の上限。</param>
/// <param name="FramesPerSecond">画面を配信する頻度。</param>
/// <param name="JpegQualityPercent">配信する JPEG の品質（1〜100）。</param>
public sealed record CompanionSettings(
    int HttpPort,
    int WebSocketPort,
    double PairingTimeoutSeconds,
    int MaxStreams,
    int FramesPerSecond,
    int JpegQualityPercent);
