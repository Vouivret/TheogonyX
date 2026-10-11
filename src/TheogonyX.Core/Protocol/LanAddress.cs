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
    /// QR コードの URL に使う PC のアドレスの候補を、使う順に並べる（docs/domain/companion-pairing.md）。
    /// リンクローカルでないプライベートな IPv4 だけを残し、デフォルトゲートウェイがあるネットワークのものを先に、
    /// 次に 192.168.x.x、172.16〜31.x.x、10.x.x.x の順にする。同じ順位なら渡された順。
    /// </summary>
    /// <param name="addresses">PC のアドレスと、そのネットワークにデフォルトゲートウェイがあるか。</param>
    public static IReadOnlyList<IPAddress> RankForQrCode(IEnumerable<(IPAddress Address, bool HasGateway)> addresses)
    {
        ArgumentNullException.ThrowIfNull(addresses);
        return addresses
            .Where(candidate => candidate.Address.AddressFamily == AddressFamily.InterNetwork
                && !IPAddress.IsLoopback(candidate.Address)
                && IsPrivate(candidate.Address)
                && !IsIPv4LinkLocal(candidate.Address.GetAddressBytes()))
            .OrderBy(candidate => candidate.HasGateway ? 0 : 1)
            .ThenBy(candidate => candidate.Address.GetAddressBytes()[0] switch
            {
                192 => 0,
                172 => 1,
                _ => 2,
            })
            .Select(candidate => candidate.Address)
            .Distinct()
            .ToList();
    }

    private static bool IsIPv4LinkLocal(byte[] bytes) => bytes[0] == 169 && bytes[1] == 254;
}
