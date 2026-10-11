using TheogonyX.Core.Protocol;

namespace TheogonyX.Core.Tests;

public class FramePacerTests
{
    // 8 fps（間隔 0.125 秒）
    private const int FramesPerSecond = 8;

    [Fact]
    [Trait("TS", "TS-frame-stream-006")]
    public void Advance_間隔に達した_1コマ送る()
    {
        var pacer = new FramePacer(FramesPerSecond);

        var first = pacer.Advance(0.1);
        var second = pacer.Advance(0.1);

        Assert.False(first);
        Assert.True(second);
    }

    [Fact]
    [Trait("TS", "TS-frame-stream-007")]
    public void Advance_処理落ちで0点3秒たった_1コマだけ送り次は送らない()
    {
        var pacer = new FramePacer(FramesPerSecond);

        var afterStall = pacer.Advance(0.3);
        var next = pacer.Advance(0.1);

        Assert.True(afterStall);
        Assert.False(next);
    }
}
