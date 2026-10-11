namespace TheogonyX.Core.Protocol;

/// <summary>
/// 画面の配信で送るバイナリフレーム（docs/protocol/messages.md、ADR 0003）。
/// 1 バイト目が種別、2 バイト目がバージョン、3 バイト目以降が JPEG。
/// </summary>
public static class FrameHeader
{
    public const byte FrameKind = 0x01;

    public const byte FrameVersion = 1;

    public const int Length = 2;

    /// <summary>JPEG の前にヘッダーを付けた、送るためのバイト列を返す。</summary>
    public static byte[] CreateFrame(ReadOnlySpan<byte> jpeg)
    {
        var frame = new byte[Length + jpeg.Length];
        frame[0] = FrameKind;
        frame[1] = FrameVersion;
        jpeg.CopyTo(frame.AsSpan(Length));
        return frame;
    }
}
