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

    [Theory]
    [Trait("TS", "TS-companion-pairing-012")]
    [InlineData(new[] { "127.0.0.1", "fe80::1", "192.168.1.20", "10.0.0.5" }, "192.168.1.20")]
    [InlineData(new[] { "169.254.10.5", "10.0.0.5" }, "10.0.0.5")]
    [InlineData(new[] { "127.0.0.1", "fd00::1", "169.254.10.5", "8.8.8.8" }, null)]
    public void ChooseForQrCode_PCのアドレス_LANのIPv4を選ぶ(string[] addresses, string? expected)
    {
        var chosen = LanAddress.ChooseForQrCode(addresses.Select(IPAddress.Parse));

        Assert.Equal(expected, chosen?.ToString());
    }
}
