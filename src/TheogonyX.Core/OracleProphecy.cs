using System.Globalization;
using System.Text;
using System.Text.Json;

namespace TheogonyX.Core;

/// <summary>
/// 予言者が神の意図を誤解して語った神託の文（docs/domain/ai-presentation.md の「予言者の誤解」）。
/// ローカルの LLM が作る表現で、ゲームの計算には関わらない。
/// </summary>
/// <param name="Heading">神託の見出し。</param>
/// <param name="Intent">予言者が誤解した神の意図。</param>
/// <param name="Doctrine">人々に広める歪んだ教義。</param>
public sealed record OracleText(string Heading, string Intent, string Doctrine)
{
    // TODO(AI_UNSURE): 文字数の上限は AI が仮に置いた（docs/domain/ai-presentation.md の「未決」）
    public const int MaxHeadingLength = 30;
    public const int MaxBodyLength = 80;

    /// <summary>3 つとも空でなく、上限の長さ以下で、改行・制御文字を含まないか。</summary>
    public bool IsValid() =>
        IsValidPart(Heading, MaxHeadingLength) && IsValidPart(Intent, MaxBodyLength) && IsValidPart(Doctrine, MaxBodyLength);

    private static bool IsValidPart(string? text, int maxLength) => AiText.IsPlain(text, maxLength);
}

/// <summary>予言者の誤解に使う、ローカルの LLM の定義。</summary>
/// <param name="Enabled">LLM を使うか。使わなければ、いつも定型文を出す。</param>
/// <param name="Endpoint">Ollama の生成 API。ループバック（127.0.0.1・localhost・::1）の http だけを許す。</param>
/// <param name="Model">Ollama のモデル名。</param>
/// <param name="TimeoutSeconds">応答を待つ秒数。</param>
/// <param name="BubbleSeconds">神託の文を吹き出しに出しておく秒数。</param>
public sealed record OracleAiRules(bool Enabled, string Endpoint, string Model, double TimeoutSeconds, double BubbleSeconds)
{
    /// <summary>値が範囲の中にあり、宛先がループバックであることを確かめる。</summary>
    /// <exception cref="ArgumentException">範囲の外の値か、ループバックでない宛先。</exception>
    public void Validate()
    {
        if (!Uri.TryCreate(Endpoint, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttp || !uri.IsLoopback
            || string.IsNullOrWhiteSpace(Model)
            || !double.IsFinite(TimeoutSeconds) || TimeoutSeconds <= 0 || !double.IsFinite(BubbleSeconds) || BubbleSeconds <= 0)
        {
            throw new ArgumentException($"予言者の誤解の定義の値が範囲外か、宛先がループバックでない: {this}");
        }
    }
}

/// <summary>予言者の誤解の、LLM への頼みの文（プロンプト）の組み立てと、応答の検証。I/O はしない。</summary>
public static class OracleProphecy
{
    /// <summary>
    /// Ollama の生成 API に送る本文（JSON）。応答は JSON の形（<c>format: json</c>）で、1 回で返させる（<c>stream: false</c>）。
    /// </summary>
    /// <param name="symbolName">記号の表示名（表示している言語）。</param>
    /// <param name="dogmaTypeName">ドグマの種別の表示名（表示している言語）。</param>
    /// <param name="language">表示している言語のコード（<c>ja</c> なら日本語で、ほかは英語で答えさせる）。</param>
    public static string BuildRequest(OracleAiRules rules, Dogma dogma, string symbolName, string dogmaTypeName, WorldState world, string language)
    {
        ArgumentNullException.ThrowIfNull(rules);
        ArgumentNullException.ThrowIfNull(dogma);
        ArgumentNullException.ThrowIfNull(world);
        return JsonSerializer.Serialize(new
        {
            model = rules.Model,
            prompt = BuildPrompt(dogma, symbolName, dogmaTypeName, world, language),
            stream = false,
            format = "json",
        });
    }

    /// <summary>LLM への頼みの文。記号・種別・強度と、予言者の心の状態としてカオス度・調和度を渡す。</summary>
    public static string BuildPrompt(Dogma dogma, string symbolName, string dogmaTypeName, WorldState world, string language)
    {
        ArgumentNullException.ThrowIfNull(dogma);
        ArgumentNullException.ThrowIfNull(world);
        var culture = CultureInfo.InvariantCulture;
        var intensity = dogma.Intensity.ToString("0.00", culture);
        var chaos = world.ChaosLevel.ToString("0", culture);
        var harmony = world.HarmonyLevel.ToString("0", culture);
        var prompt = new StringBuilder();
        if (language.StartsWith("ja", StringComparison.Ordinal))
        {
            prompt.AppendLine("あなたは滅びゆく星の予言者だ。神から夢で啓示を受けたが、その意図を誤解したまま人々に伝える。");
            prompt.AppendLine(culture, $"啓示: 「{symbolName}」の{dogmaTypeName}（強さ {intensity}。-1〜1）。");
            prompt.AppendLine(culture, $"いまの星: カオス度 {chaos}（0〜100）、調和度 {harmony}（0〜100）。");
            prompt.AppendLine("カオス度が高いほど、狂気じみた不吉で歪んだ言葉にせよ。調和度が高いほど、穏やかだが的外れな言葉にせよ。");
            prompt.AppendLine("自分が誤解していることには触れず、予言者自身の言葉として、信じきって語れ。");
            prompt.AppendLine(culture, $"次の JSON だけで、日本語で答えよ: {{\"heading\": \"神託の見出し（{OracleText.MaxHeadingLength}字以内）\", \"intent\": \"誤解した神の意図（{OracleText.MaxBodyLength}字以内）\", \"doctrine\": \"人々に広める歪んだ教義（{OracleText.MaxBodyLength}字以内）\"}}");
        }
        else
        {
            prompt.AppendLine("You are the prophet of a dying star. A god sent you a revelation in a dream, but you misunderstand it and preach it to the people.");
            prompt.AppendLine(culture, $"Revelation: {dogmaTypeName} of \"{symbolName}\" (strength {intensity}, from -1 to 1).");
            prompt.AppendLine(culture, $"The star now: chaos {chaos} (0-100), harmony {harmony} (0-100).");
            prompt.AppendLine("The higher the chaos, the more insane, ominous and twisted your words. The higher the harmony, the calmer but more misguided.");
            prompt.AppendLine("Never mention that you misunderstand. Speak in your own words as the prophet, with total conviction.");
            prompt.AppendLine(culture, $"Answer in English with only this JSON: {{\"heading\": \"title of the oracle (max {OracleText.MaxHeadingLength} characters)\", \"intent\": \"the god's intent as you misunderstood it (max {OracleText.MaxBodyLength} characters)\", \"doctrine\": \"the twisted doctrine you preach (max {OracleText.MaxBodyLength} characters)\"}}");
        }
        return prompt.ToString();
    }

    /// <summary>
    /// Ollama の生成 API の応答（JSON の <c>response</c> に、LLM が書いた JSON の文字列が入る）から、神託の文を取り出す。
    /// 応答は検証していない外部入力として扱い、形が崩れている・長すぎる・空のときは null を返す。前後の空白は取り除く。
    /// </summary>
    public static OracleText? ParseResponse(string body)
    {
        ArgumentNullException.ThrowIfNull(body);
        // 形の崩れは想定内。呼び出し側が定型文に戻し、理由をログに出す
        var text = AiText.ReadObject(body, root =>
        {
            var (heading, intent, doctrine) = (AiText.Part(root, "heading"), AiText.Part(root, "intent"), AiText.Part(root, "doctrine"));
            return heading is null || intent is null || doctrine is null ? null : new OracleText(heading, intent, doctrine);
        });
        return text is not null && text.IsValid() ? text : null;
    }
}
