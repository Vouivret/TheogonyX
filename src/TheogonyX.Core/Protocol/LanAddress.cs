using System.Net;
using System.Net.Sockets;

namespace TheogonyX.Core.Protocol;

/// <summary>
/// 接続元が同じ LAN かを判定する（docs/domain/companion-pairing.md）。
/// IPv4 のプライベートアドレス・リンクローカル、IPv6 のリンクローカル・ユニークローカル、ループバックを同じ LAN とする。
/// </summary>
public static class LanAddress
{
    public static bool IsPrivate(IPAddress address)
    {
        ArgumentNullException.ThrowIfNull(address);
        if (address.IsIPv4MappedToIPv6)
        {
            address = address.MapToIPv4();
        }

        if (IPAddress.IsLoopback(address))
        {
            return true;
        }

        if (address.AddressFamily == AddressFamily.InterNetworkV6)
        {
            return address.IsIPv6LinkLocal || address.IsIPv6UniqueLocal;
        }

        if (address.AddressFamily != AddressFamily.InterNetwork)
        {
            return false;
        }

        var bytes = address.GetAddressBytes();
        return bytes[0] == 10
            || (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31)
            || (bytes[0] == 192 && bytes[1] == 168)
            || IsIPv4LinkLocal(bytes);
    }

    /// <summary>
    /// QR コードの URL に使う PC のアドレスを選ぶ。並びの先頭から、リンクローカルでないプライベートな IPv4 を選ぶ。
    /// どれも無ければ null。
    /// </summary>
    public static IPAddress? ChooseForQrCode(IEnumerable<IPAddress> addresses)
    {
        ArgumentNullException.ThrowIfNull(addresses);
        return addresses.FirstOrDefault(address => address.AddressFamily == AddressFamily.InterNetwork
            && !IPAddress.IsLoopback(address) && IsPrivate(address) && !IsIPv4LinkLocal(address.GetAddressBytes()));
    }

    private static bool IsIPv4LinkLocal(byte[] bytes) => bytes[0] == 169 && bytes[1] == 254;
}
