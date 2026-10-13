namespace TheogonyX.Core;

/// <summary><see cref="ProphetDreams.Pronounce"/> の結果。</summary>
/// <param name="Dogma">一覧に加えたドグマ。ペアが作れなければ null。</param>
/// <param name="AnsweredRequest">この神託で済んだ依頼。依頼中でなければ null。</param>
/// <param name="ResumedAutoPlay">夢で止めていた自動進行を再開した。</param>
/// <param name="OverloadCount">神の歪みの回数（無償の回数を超えた分）。歪みが無ければ 0。</param>
public sealed record OracleResult(Dogma? Dogma, ProphetRequestRule? AnsweredRequest, bool ResumedAutoPlay, int OverloadCount = 0);
