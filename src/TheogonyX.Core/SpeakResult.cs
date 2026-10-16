namespace TheogonyX.Core;

/// <summary><see cref="ProphetDreams.Speak"/> の結果。</summary>
/// <param name="Dogma">一覧に加えたドグマ。行き先だけの神託では null。</param>
/// <param name="Record">残した神託の記録。</param>
public sealed record SpeakResult(
    Dogma? Dogma, ProphetRequestRule? AnsweredRequest, bool ResumedAutoPlay, int OverloadCount, int CorruptedCount, OracleRecord Record);
