namespace TheogonyX.Core;

/// <summary><see cref="ProphetDreams.OnTimeAdvanced"/> の結果。</summary>
/// <param name="NewRequest">新しく出した依頼。出していなければ null。</param>
/// <param name="PausedAutoPlay">新しい依頼のために、自動進行を止めた。</param>
/// <param name="CorruptedAdded">カオス度の閾値を越えて、山札に混ぜた汚染ルーンの数。</param>
public sealed record ProphetUpdate(ProphetRequestRule? NewRequest, bool PausedAutoPlay, int CorruptedAdded = 0);
