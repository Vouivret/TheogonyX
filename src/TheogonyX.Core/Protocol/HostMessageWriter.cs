using System.Text.Json;

namespace TheogonyX.Core.Protocol;

/// <summary>ホストからコンパニオンへのテキストフレーム（JSON）を組み立てる（docs/protocol/messages.md）。</summary>
public static class HostMessageWriter
{
    /// <summary><c>ERROR</c> の <c>reason</c>: 配信の上限に達している。</summary>
    public const string MaxConnectionsReached = "MAX_CONNECTIONS_REACHED";

    /// <summary><c>ERROR</c> の <c>reason</c>: ペアリングできない。</summary>
    public const string PairingFailed = "PAIRING_FAILED";

    public static string Paired(string sessionToken)
    {
        ArgumentException.ThrowIfNullOrEmpty(sessionToken);
        return JsonSerializer.Serialize(new { type = "PAIRED", version = 1, sessionToken });
    }

    public static string Error(string reason)
    {
        ArgumentException.ThrowIfNullOrEmpty(reason);
        return JsonSerializer.Serialize(new { type = "ERROR", version = 1, reason });
    }
}
