using System.Text.Json;

namespace TheogonyX.Core.Protocol;

/// <summary>
/// コンパニオンから届いたテキストフレーム（JSON）を検証して、<see cref="CompanionMessage"/> に直す。
/// 届いたものは外部入力として扱い、読めないもの・知らないものは理由を付けて失敗にする。
/// </summary>
public static class CompanionMessageParser
{
    /// <summary>トークンとして受け付ける最大の文字数。ホストが作るトークンより十分に長い。</summary>
    public const int MaxTokenLength = 128;

    // docs/protocol/messages.md の表と1対1。値は（種類, 必須の文字列の項目。無ければ null）。
    private static readonly Dictionary<(string Type, int Version), (CompanionMessageType Type, string? TokenField)> Messages = new()
    {
        [("PAIR", 1)] = (CompanionMessageType.Pair, "token"),
        [("RESUME", 1)] = (CompanionMessageType.Resume, "sessionToken"),
        [("START_STREAM", 1)] = (CompanionMessageType.StartStream, null),
        [("STOP_STREAM", 1)] = (CompanionMessageType.StopStream, null),
    };

    /// <param name="message">成功したときのメッセージ。失敗したときは null。</param>
    /// <param name="reason">失敗したときの理由（ログの <c>reason</c>）。成功したときは空文字。</param>
    public static bool TryParse(string text, out CompanionMessage? message, out string reason)
    {
        ArgumentNullException.ThrowIfNull(text);
        message = null;

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

            if (!Messages.TryGetValue((typeName, version), out var definition))
            {
                reason = "unknown_type_or_version";
                return false;
            }

            var token = string.Empty;
            if (definition.TokenField is { } field)
            {
                if (!root.TryGetProperty(field, out var tokenElement)
                    || tokenElement.ValueKind != JsonValueKind.String
                    || tokenElement.GetString() is not { Length: > 0 and <= MaxTokenLength } value)
                {
                    reason = "invalid_token";
                    return false;
                }

                token = value;
            }

            message = new CompanionMessage(definition.Type, token);
        }

        reason = string.Empty;
        return true;
    }
}
