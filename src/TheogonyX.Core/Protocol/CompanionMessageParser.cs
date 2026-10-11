using System.Text.Json;

namespace TheogonyX.Core.Protocol;

/// <summary>
/// コンパニオンから届いたテキストフレーム（JSON）を検証して、メッセージの種類に直す。
/// 届いたものは外部入力として扱い、読めないもの・知らないものは理由を付けて失敗にする。
/// </summary>
public static class CompanionMessageParser
{
    // docs/protocol/messages.md の表と1対1。
    private static readonly Dictionary<(string Type, int Version), CompanionMessageType> Messages = new()
    {
        [("START_STREAM", 1)] = CompanionMessageType.StartStream,
        [("STOP_STREAM", 1)] = CompanionMessageType.StopStream,
    };

    /// <param name="reason">失敗したときの理由（ログ用の短い英字）。成功したときは空文字。</param>
    public static bool TryParse(string text, out CompanionMessageType type, out string reason)
    {
        ArgumentNullException.ThrowIfNull(text);
        type = default;

        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(text);
        }
        catch (JsonException)
        {
            reason = "invalid_json";
            return false;
        }

        using (document)
        {
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object
                || !root.TryGetProperty("type", out var typeElement)
                || typeElement.ValueKind != JsonValueKind.String
                || typeElement.GetString() is not { } typeName)
            {
                reason = "missing_type";
                return false;
            }

            if (!root.TryGetProperty("version", out var versionElement)
                || versionElement.ValueKind != JsonValueKind.Number
                || !versionElement.TryGetInt32(out var version))
            {
                reason = "missing_version";
                return false;
            }

            if (!Messages.TryGetValue((typeName, version), out type))
            {
                reason = "unknown_type_or_version";
                return false;
            }
        }

        reason = string.Empty;
        return true;
    }
}
