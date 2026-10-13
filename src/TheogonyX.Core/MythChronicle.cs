using System.Globalization;
using System.Text;
using System.Text.Json;

namespace TheogonyX.Core;

/// <summary>章の神話の文。LLM が作る表現で、ゲームの計算には関わらない（docs/domain/ai-presentation.md の「歴史の神話化」）。</summary>
/// <param name="Heading">章の題。</param>
/// <param name="Body">神話の本文。</param>
public sealed record MythText(string Heading, string Body)
{
    // TODO(AI_UNSURE): 文字数の上限は AI が仮に置いた（docs/domain/ai-presentation.md の「未決」）
    public const int MaxHeadingLength = 30;
    public const int MaxBodyLength = 200;

    /// <summary>両方が空でなく、上限の長さ以下で、改行・制御文字を含まないか。</summary>
    public bool IsValid() => AiText.IsPlain(Heading, MaxHeadingLength) && AiText.IsPlain(Body, MaxBodyLength);
}

/// <summary>
/// 1 章分の材料。年と、カオス度の変化、章の出来事の行（表示している言語の文、古い順）、前の章の神話の見出し（新しい順）。
/// </summary>
/// <param name="PreviousHeadings">前の章の神話の見出しと、その章の終わりの年（新しい順）。</param>
public sealed record MythChapter(
    int FromYear, int ToYear, float ChaosFrom, float ChaosTo, IReadOnlyList<string> Events, IReadOnlyList<(int Year, string Heading)> PreviousHeadings);

/// <summary>章の神話の文の頼み（プロンプト）の組み立てと、応答の検証。I/O はしない。</summary>
public static class MythChronicle
{
    /// <summary>Ollama の生成 API に送る本文（JSON）。応答は JSON の形（<c>format: json</c>）で、1 回で返させる（<c>stream: false</c>）。</summary>
    /// <param name="language">表示している言語のコード（<c>ja</c> なら日本語で、ほかは英語で答えさせる）。</param>
    public static string BuildRequest(OracleAiRules rules, MythChapter chapter, string language)
    {
        ArgumentNullException.ThrowIfNull(rules);
        ArgumentNullException.ThrowIfNull(chapter);
        return JsonSerializer.Serialize(new
        {
            model = rules.Model,
            prompt = BuildPrompt(chapter, language),
            stream = false,
            format = "json",
        });
    }

    /// <summary>頼みの文。年・カオス度の変化・出来事を渡し、前の章の言い伝えは細部が欠けた形で織り込ませる。</summary>
    public static string BuildPrompt(MythChapter chapter, string language)
    {
        ArgumentNullException.ThrowIfNull(chapter);
        var culture = CultureInfo.InvariantCulture;
        var chaos = $"{chapter.ChaosFrom.ToString("0", culture)} → {chapter.ChaosTo.ToString("0", culture)}";
        var prompt = new StringBuilder();
        if (language.StartsWith("ja", StringComparison.Ordinal))
        {
            prompt.AppendLine("あなたは滅びゆく星の語り部だ。ある時代の出来事を、後の世の人々が神話として語り継ぐ文にせよ。");
            prompt.AppendLine(culture, $"時代: {chapter.FromYear} 年〜{chapter.ToYear} 年。カオス度の変化: {chaos}（0〜100）。");
            prompt.AppendLine("出来事（古い順）:");
            foreach (var line in chapter.Events)
            {
                prompt.AppendLine($"- {line}");
            }
            if (chapter.PreviousHeadings.Count > 0)
            {
                prompt.AppendLine("前の時代の言い伝え（新しい順）:");
                foreach (var (year, heading) in chapter.PreviousHeadings)
                {
                    prompt.AppendLine(culture, $"- {year} 年の時代「{heading}」");
                }
                prompt.AppendLine("前の言い伝えは、細部が欠けて少しずつ変わった神話として織り込め。");
            }
            prompt.AppendLine("カオス度が上がっていれば荒々しく、下がっていれば穏やかな語り口にせよ。数字は書かず、物語にせよ。");
            prompt.AppendLine(culture, $"次の JSON だけで、日本語で答えよ: {{\"heading\": \"時代の題（{MythText.MaxHeadingLength}字以内）\", \"body\": \"神話の本文（{MythText.MaxBodyLength}字以内）\"}}");
        }
        else
        {
            prompt.AppendLine("You are a chronicler of a dying star. Write the events of an age as a myth that later people will pass down.");
            prompt.AppendLine(culture, $"Age: years {chapter.FromYear} to {chapter.ToYear}. Chaos change: {chaos} (0-100).");
            prompt.AppendLine("Events (oldest first):");
            foreach (var line in chapter.Events)
            {
                prompt.AppendLine($"- {line}");
            }
            if (chapter.PreviousHeadings.Count > 0)
            {
                prompt.AppendLine("Myths of earlier ages (newest first):");
                foreach (var (year, heading) in chapter.PreviousHeadings)
                {
                    prompt.AppendLine(culture, $"- the age of year {year}, \"{heading}\"");
                }
                prompt.AppendLine("Weave the earlier myths in as legends whose details have faded and slowly changed.");
            }
            prompt.AppendLine("If chaos rose, speak wildly; if it fell, speak calmly. Tell a story; do not write the numbers.");
            prompt.AppendLine(culture, $"Answer in English with only this JSON: {{\"heading\": \"title of the age (max {MythText.MaxHeadingLength} characters)\", \"body\": \"the myth (max {MythText.MaxBodyLength} characters)\"}}");
        }
        return prompt.ToString();
    }

    /// <summary>
    /// Ollama の応答から神話の文を取り出す。外部入力として検証し、形が崩れている・長すぎる・空のときは null。前後の空白は取り除く。
    /// </summary>
    public static MythText? ParseResponse(string body)
    {
        ArgumentNullException.ThrowIfNull(body);
        var text = AiText.ReadObject(body, root =>
        {
            var (heading, bodyText) = (AiText.Part(root, "heading"), AiText.Part(root, "body"));
            return heading is null || bodyText is null ? null : new MythText(heading, bodyText);
        });
        return text is not null && text.IsValid() ? text : null;
    }
}
