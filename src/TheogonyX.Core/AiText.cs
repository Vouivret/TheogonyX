using System.Text.Json;
using System.Text.RegularExpressions;

namespace TheogonyX.Core;

/// <summary>AI（ローカルの LLM）が作った文の扱い。応答は検証していない外部入力として扱う。</summary>
public static class AiText
{
    /// <summary>空でなく、上限の長さ以下で、改行・制御文字を含まないか。</summary>
    public static bool IsPlain(string? text, int maxLength) =>
        !string.IsNullOrWhiteSpace(text) && text.Length <= maxLength && !text.Any(char.IsControl);

    /// <summary>
    /// Ollama の生成 API の応答（JSON の <c>response</c> に、LLM が書いた文字列が入る）から、その文字列を取り出す。
    /// 形が崩れていれば null。
    /// </summary>
    public static string? Content(string body)
    {
        ArgumentNullException.ThrowIfNull(body);
        try
        {
            using var outer = JsonDocument.Parse(body);
            if (outer.RootElement.ValueKind != JsonValueKind.Object
                || !outer.RootElement.TryGetProperty("response", out var response) || response.ValueKind != JsonValueKind.String)
            {
                return null;
            }
            return response.GetString();
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>
    /// <see cref="Content"/> で取り出した、LLM が書いた JSON のオブジェクトを <paramref name="read"/> で読む。
    /// 中身が JSON でない・オブジェクトでないときは null。
    /// </summary>
    public static T? ReadObject<T>(string body, Func<JsonElement, T?> read) where T : class
    {
        var content = Content(body);
        if (content is null)
        {
            return null;
        }
        try
        {
            using var inner = JsonDocument.Parse(content);
            return inner.RootElement.ValueKind == JsonValueKind.Object ? read(inner.RootElement) : null;
        }
        catch (JsonException)
        {
            return RepairStrings(content, read);
        }
    }

    // 小さな LLM は、文字列の終わりの引用符を「」や } に書き違えて JSON を崩すことがある（例: {"prayer": "…。」} 蛇足の文）。
    // 崩れたときだけ、文字列の項目（"名前": "値"）を拾って作り直す。配列・数値・null は拾えないので、無い項目として読まれる。
    private static T? RepairStrings<T>(string content, Func<JsonElement, T?> read) where T : class
    {
        if (!content.TrimStart().StartsWith('{'))
        {
            return null;
        }
        var fields = new Dictionary<string, string>();
        foreach (Match match in Regex.Matches(content, "\"(\\w+)\"\\s*:\\s*\"((?:[^\"\\\\」}\\n]|\\\\.)*)[\"」}]"))
        {
            fields.TryAdd(match.Groups[1].Value, match.Groups[2].Value);
        }
        if (fields.Count == 0)
        {
            return null;
        }
        try
        {
            using var repaired = JsonDocument.Parse(JsonSerializer.Serialize(fields));
            return read(repaired.RootElement);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>JSON のオブジェクトから文字列の項目を取り出す（前後の空白は取り除く）。無い・文字列でないなら null。</summary>
    public static string? Part(JsonElement root, string name) =>
        root.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString()?.Trim() : null;
}
