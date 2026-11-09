namespace TheogonyX.Core;

/// <summary><see cref="ProphetDreams.Pronounce"/> の結果。</summary>
/// <param name="Dogma">一覧に加えたドグマ。行き先だけの神託では null。</param>
public sealed record SpeakResult(
    Dogma? Dogma, ProphetRequestRule? AnsweredRequest, bool ResumedAutoPlay, int OverloadCount, int CorruptedCount);
