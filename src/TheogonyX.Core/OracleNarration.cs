using System.Globalization;
using System.Text;
using System.Text.Json;

namespace TheogonyX.Core;

/// <summary>予言者が神の言葉を聞いた「聞こえた文」と、民に告げる語り。LLM が作る表現で、ゲームの計算には関わらない。</summary>
public sealed record NarrationText(string Heard, string Narration)
{
    // TODO(AI_UNSURE): 文字数の上限は AI が仮に置いた（docs/domain/oracle-pair.md の「神託の直後に告げる」）
    public const int MaxHeardLength = 60;
    public const int MaxNarrationLength = 120;
}

/// <summary>
/// 聞こえた文と語りの頼みの組み立てと、応答の検証。聞き違え（どの語がどうずれたか）はコアが決めたもので、LLM は文に言い換えるだけ。
/// 聞こえた文には、聞き違えた後の名詞と動詞の語が入っていなければ使わない（LLM がずれ方を作り直さないため）。
/// </summary>
public static class OracleNarrationWriter
{
    /// <summary>
    /// 語りの頼み。予言者は聞こえたことしか知らないので、神の文と聞き違えた語の対は渡さない（渡すと、小さな LLM が神の文を写してしまい、
    /// 聞き違えが語りに出ない）。
    /// </summary>
    /// <param name="heardNoun">聞こえた名詞の語。無ければ null。</param>
    /// <param name="heardVerb">聞こえた動詞の語。無ければ null。</param>
    public static string BuildRequest(OracleAiRules rules, string? heardNoun, string? heardVerb, WorldState world, string language)
    {
        ArgumentNullException.ThrowIfNull(rules);
        return JsonSerializer.Serialize(new
        {
            model = rules.Model,
            prompt = BuildPrompt(heardNoun, heardVerb, world, language),
            stream = false,
            format = "json",
        });
    }

    public static string BuildPrompt(string? heardNoun, string? heardVerb, WorldState world, string language)
    {
        ArgumentNullException.ThrowIfNull(world);
        var culture = CultureInfo.InvariantCulture;
        var chaos = world.ChaosLevel.ToString("0", culture);
        var harmony = world.HarmonyLevel.ToString("0", culture);
        var words = string.Join(" / ", new[] { heardNoun, heardVerb }.Where(word => word is not null));
        var prompt = new StringBuilder();
        if (language.StartsWith("ja", StringComparison.Ordinal))
        {
            prompt.AppendLine("あなたは滅びゆく星の予言者だ。あなたは神の声を聞いた。聞こえた言葉は次のとおりで、これを変えてはならない。");
            prompt.AppendLine($"聞こえた言葉: {words}");
            prompt.AppendLine($"いまの星: カオス度 {chaos}（0〜100）、調和度 {harmony}（0〜100）。カオス度が高いほど不穏に、調和度が高いほど穏やかに語れ。");
            prompt.AppendLine("heard は、聞こえた言葉だけで作った、神の命令の自然な一文にせよ（例: 聞こえた言葉が「森 / 切る」なら「森を切れ」）。");
            prompt.AppendLine("予言者自身の言葉として語れ。");
            prompt.AppendLine($"次の JSON だけで、日本語で答えよ: {{\"heard\": \"聞こえた言葉をすべて含む、予言者が聞いた一文（{NarrationText.MaxHeardLength}字以内）\", \"narration\": \"民に告げる語り（{NarrationText.MaxNarrationLength}字以内）\"}}");
        }
        else
        {
            prompt.AppendLine("You are the prophet of a dying star. You heard the voice of the god. The words you heard are below and must not be changed.");
            prompt.AppendLine($"Words heard: {words}");
            prompt.AppendLine($"The star now: chaos {chaos} (0-100), harmony {harmony} (0-100). The higher the chaos, the more ominous; the higher the harmony, the calmer.");
            prompt.AppendLine("heard is one natural sentence of the god's command made only from the words heard (e.g. words heard \"forest / cut\" -> \"Cut the forest\").");
            prompt.AppendLine("Speak in your own words as the prophet.");
            prompt.AppendLine($"Answer in English with only this JSON: {{\"heard\": \"one sentence you heard that contains every word heard (max {NarrationText.MaxHeardLength} characters)\", \"narration\": \"what you tell the people (max {NarrationText.MaxNarrationLength} characters)\"}}");
        }
        return prompt.ToString();
    }

    /// <summary>
    /// 動詞を聞こえた文に入っているか確かめるときの形。文の中では活用する（守る → 守れ）ので、最初の語の最後の 1 字を外す
    /// （守る → 守、育てる → 育て、protect → protec）。1 字の語はそのまま。
    /// </summary>
    public static string VerbStem(string verb)
    {
        ArgumentNullException.ThrowIfNull(verb);
        var head = verb.Split(' ', 2)[0];
        return head.Length >= 2 ? head[..^1] : head;
    }

    /// <summary>
    /// 応答から取り出す。形が崩れている・長すぎる・空のとき、または聞こえた文に <paramref name="requiredWords"/> が入っていないとき null。
    /// 動詞は <see cref="VerbStem"/> の形で渡す。
    /// </summary>
    public static NarrationText? ParseResponse(string body, IReadOnlyList<string> requiredWords)
    {
        ArgumentNullException.ThrowIfNull(body);
        ArgumentNullException.ThrowIfNull(requiredWords);
        var text = AiText.ReadObject(body, root =>
        {
            var (heard, narration) = (AiText.Part(root, "heard"), AiText.Part(root, "narration"));
            return heard is null || narration is null ? null : new NarrationText(heard, narration);
        });
        return text is not null
            && AiText.IsPlain(text.Heard, NarrationText.MaxHeardLength) && AiText.IsPlain(text.Narration, NarrationText.MaxNarrationLength)
            && requiredWords.All(word => text.Heard.Contains(word, StringComparison.Ordinal))
            ? text
            : null;
    }
}
