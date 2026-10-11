namespace TheogonyX.Core;

/// <summary><see cref="ProphetDreams.OnTimeAdvanced"/> の結果。</summary>
/// <param name="HasNewDream">新しい夢を見た。</param>
/// <param name="NewRequest">新しく出した依頼。出していなければ null。</param>
/// <param name="PausedAutoPlay">新しい夢か依頼のために、自動進行を止めた。</param>
public sealed record DreamUpdate(bool HasNewDream, ProphetRequestRule? NewRequest, bool PausedAutoPlay);
