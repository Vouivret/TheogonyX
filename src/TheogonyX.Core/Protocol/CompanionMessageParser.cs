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
        [("SPEAK", 1)] = (CompanionMessageType.Speak, null),
        [("PRONOUNCE", 3)] = (CompanionMessageType.Pronounce, null),
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

            if (definition.Type == CompanionMessageType.Speak)
            {
                // 神の言葉（長さと制御文字だけここで確かめる。文はファイルのパスやコマンドに使わない）
                if (!root.TryGetProperty("text", out var textElement)
                    || textElement.ValueKind != JsonValueKind.String
                    || !OracleListening.IsValidGodText(textElement.GetString()))
                {
                    reason = "invalid_speak";
                    return false;
                }

                message = new CompanionMessage(definition.Type, Text: textElement.GetString()!);
            }
            else if (definition.Type == CompanionMessageType.Pronounce)
            {
                // 名詞・動詞の ID（語の一覧にあるかは、ゲームの側で確かめる）と行き先。3 つとも「なし」の語りかけは捨てる
                if (!TryReadWordId(root, "noun", out var noun) || !TryReadWordId(root, "verb", out var verb)
                    || !TryReadDestination(root, out var destination)
                    || (noun is null && verb is null && destination == HeardDestination.None))
                {
                    reason = "invalid_pronounce";
                    return false;
                }

                message = new CompanionMessage(definition.Type, NounId: noun, VerbId: verb, Destination: destination);
            }
            else
            {
                message = new CompanionMessage(definition.Type, token);
            }
        }

        reason = string.Empty;
        return true;
    }

    /// <summary>語の ID として受け付ける最大の文字数。</summary>
    public const int MaxWordIdLength = 64;

    // 文字列か null。空の文字列・長すぎる文字列・制御文字を含む文字列・ほかの型は失敗
    private static bool TryReadWordId(JsonElement root, string name, out string? id)
    {
        id = null;
        if (!root.TryGetProperty(name, out var element))
        {
            return false;
        }
        if (element.ValueKind == JsonValueKind.Null)
        {
            return true;
        }
        if (element.ValueKind != JsonValueKind.String || !AiText.IsPlain(element.GetString(), MaxWordIdLength))
        {
            return false;
        }
        id = element.GetString();
        return true;
    }

    private static bool TryReadDestination(JsonElement root, out HeardDestination destination)
    {
        destination = HeardDestination.None;
        if (!root.TryGetProperty("destination", out var element) || element.ValueKind != JsonValueKind.String)
        {
            return false;
        }
        switch (element.GetString())
        {
            case "NONE":
                return true;
            case "EAST":
                destination = HeardDestination.East;
                return true;
            case "WEST":
                destination = HeardDestination.West;
                return true;
            default:
                return false;
        }
    }
}
