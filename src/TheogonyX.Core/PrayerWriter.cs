using System.Globalization;
using System.Text;
using System.Text.Json;

namespace TheogonyX.Core;

/// <summary>
/// 予言者の祈りの文の頼み（プロンプト）の組み立てと、応答の検証（docs/domain/oracle-pair.md の「祈りの言葉と語彙の見せ方」）。
/// LLM には、予言者が知っている名詞の一覧だけを渡す。祈りは表現だけで、ゲームの計算には関わらない。I/O はしない。
/// </summary>
public static class PrayerWriter
{
    // TODO(AI_UNSURE): 祈りの文字数の上限は AI が仮に置いた
    public const int MaxPrayerLength = 80;

    /// <param name="troubleText">困りごとの表示名。日々の祈りなら null。</param>
    /// <param name="wordTexts">祈りに入れる名詞（表示している言語の語）。</param>
    public static string BuildRequest(OracleAiRules rules, string? troubleText, IReadOnlyList<string> wordTexts, string language)
    {
        ArgumentNullException.ThrowIfNull(rules);
        return JsonSerializer.Serialize(new
        {
            model = rules.Model,
            prompt = BuildPrompt(troubleText, wordTexts, language),
            stream = false,
            format = "json",
        });
    }

    public static string BuildPrompt(string? troubleText, IReadOnlyList<string> wordTexts, string language)
    {
        ArgumentNullException.ThrowIfNull(wordTexts);
        var words = string.Join(", ", wordTexts);
        var prompt = new StringBuilder();
        if (language.StartsWith("ja", StringComparison.Ordinal))
        {
            prompt.AppendLine("あなたは滅びゆく星の予言者だ。神に短く祈る。祈りに使ってよい名詞は次の語だけで、すべて使え。ほかの名詞は使うな。");
            prompt.AppendLine($"使う名詞: {words}");
            prompt.AppendLine(troubleText is null ? "困りごとは無い。感謝か、日々の願いをささやけ。" : $"困りごと: {troubleText}");
            prompt.AppendLine(string.Create(CultureInfo.InvariantCulture, $"次の JSON だけで、日本語で答えよ: {{\"prayer\": \"祈りの一文（{MaxPrayerLength}字以内）\"}}"));
        }
        else
        {
            prompt.AppendLine("You are the prophet of a dying star, praying briefly to your god. The only nouns you may use are the following, and you must use all of them. Use no other nouns.");
            prompt.AppendLine($"Nouns to use: {words}");
            prompt.AppendLine(troubleText is null ? "There is no trouble. Whisper thanks or a daily wish." : $"Trouble: {troubleText}");
            prompt.AppendLine(string.Create(CultureInfo.InvariantCulture, $"Answer in English with only this JSON: {{\"prayer\": \"one sentence of prayer (max {MaxPrayerLength} characters)\"}}"));
        }
        return prompt.ToString();
    }

    /// <summary>応答から祈りの文を取り出す。形が崩れている・長すぎる・空のとき、または <paramref name="requiredWords"/> が入っていないとき null。</summary>
    public static string? ParseResponse(string body, IReadOnlyList<string> requiredWords)
    {
        ArgumentNullException.ThrowIfNull(body);
        ArgumentNullException.ThrowIfNull(requiredWords);
        var prayer = AiText.ReadObject(body, root => AiText.Part(root, "prayer") is { } text ? new PrayerText(text) : null)?.Text;
        return prayer is not null && AiText.IsPlain(prayer, MaxPrayerLength) && requiredWords.All(word => prayer.Contains(word, StringComparison.Ordinal))
            ? prayer
            : null;
    }

    private sealed record PrayerText(string Text);
}
