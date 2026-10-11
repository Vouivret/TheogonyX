using System.Net;
using TheogonyX.Core.Protocol;

namespace TheogonyX.Core.Tests;

public class LanAddressTests
{
    [Theory]
    [Trait("TS", "TS-companion-pairing-007")]
    [InlineData("10.0.0.1", true)]
    [InlineData("172.16.0.1", true)]
    [InlineData("172.31.255.255", true)]
    [InlineData("192.168.1.20", true)]
    [InlineData("127.0.0.1", true)]
    [InlineData("::1", true)]
    [InlineData("::ffff:192.168.1.20", true)]
    [InlineData("169.254.10.5", true)]
    [InlineData("fe80::1c2a:3bff:fe4d:5e6f", true)]
    [InlineData("fd00::1", true)]
    [InlineData("172.15.255.255", false)]
    [InlineData("172.32.0.1", false)]
    [InlineData("192.169.0.1", false)]
    [InlineData("8.8.8.8", false)]
    [InlineData("2001:db8::1", false)]
    [InlineData("169.253.0.1", false)]
    public void IsPrivate_接続元のアドレス_同じLANだけを受け付ける(string address, bool expected)
    {
        Assert.Equal(expected, LanAddress.IsPrivate(IPAddress.Parse(address)));
    }

    [Fact]
    [Trait("TS", "TS-companion-pairing-012")]
    public void RankForQrCode_PCのアドレス_ゲートウェイのある物理のLANを先に並べる()
    {
        (IPAddress, bool)[] addresses =
        [
            (IPAddress.Parse("172.20.48.1"), false), // WSL の仮想アダプター
            (IPAddress.Parse("10.0.0.5"), true),
            (IPAddress.Parse("127.0.0.1"), false),
            (IPAddress.Parse("192.168.1.20"), true),
            (IPAddress.Parse("169.254.10.5"), false),
            (IPAddress.Parse("fd00::1"), true),
            (IPAddress.Parse("192.168.56.1"), false), // VirtualBox の仮想アダプター
        ];

        var ranked = LanAddress.RankForQrCode(addresses);

        Assert.Equal(["192.168.1.20", "10.0.0.5", "192.168.56.1", "172.20.48.1"], ranked.Select(address => address.ToString()));
    }

    [Fact]
    [Trait("TS", "TS-companion-pairing-012")]
    public void RankForQrCode_LANのIPv4が無い_空になる()
    {
        (IPAddress, bool)[] addresses = [(IPAddress.Parse("127.0.0.1"), false), (IPAddress.Parse("8.8.8.8"), true)];

        Assert.Empty(LanAddress.RankForQrCode(addresses));
    }
}
