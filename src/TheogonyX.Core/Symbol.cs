namespace TheogonyX.Core;

/// <summary>夢を構成する記号の定義。</summary>
/// <param name="EmotionalPolarity">-1.0（恐怖）〜 +1.0（歓喜）。</param>
public sealed record Symbol(string Id, float EmotionalPolarity);
