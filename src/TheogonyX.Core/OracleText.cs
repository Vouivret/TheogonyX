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
