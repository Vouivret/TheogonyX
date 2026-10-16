namespace TheogonyX.Core;

/// <summary>自由文の神託の命令を実行した結果。<paramref name="RejectedReason"/> が null なら <paramref name="Result"/> がある。</summary>
public sealed record SpeakOutcome(long Sequence, string? RejectedReason, SpeakResult? Result);
