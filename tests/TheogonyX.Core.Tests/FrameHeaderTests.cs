using TheogonyX.Core.Protocol;

namespace TheogonyX.Core.Tests;

public class FrameHeaderTests
{
    [Fact]
    [Trait("TS", "TS-frame-stream-008")]
    public void CreateFrame_JPEGのバイト列_先頭に種別とバージョンが付く()
    {
        // JPEG の先頭（SOI）と末尾（EOI）のマーカー
        byte[] jpeg = [0xFF, 0xD8, 0xFF, 0xD9];

        var frame = FrameHeader.CreateFrame(jpeg);

        Assert.Equal([0x01, 0x01, 0xFF, 0xD8, 0xFF, 0xD9], frame);
    }
}
