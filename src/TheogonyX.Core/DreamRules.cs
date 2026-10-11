namespace TheogonyX.Core;

/// <summary>夢の解釈に使うゲームデータ定義。</summary>
/// <param name="TabooAtOrBelow">極性の和がこの値以下なら禁忌。</param>
/// <param name="WorshipAtOrAbove">極性の和がこの値以上なら崇拝。</param>
public sealed record DreamRules(IReadOnlyList<Symbol> Symbols, float TabooAtOrBelow, float WorshipAtOrAbove);
