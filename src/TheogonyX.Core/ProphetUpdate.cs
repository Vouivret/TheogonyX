namespace TheogonyX.Core;

/// <summary><see cref="ProphetDreams.OnTimeAdvanced"/> の結果。</summary>
/// <param name="NewRequest">新しく出した依頼。出していなければ null。</param>
/// <param name="PausedAutoPlay">新しい依頼のために、自動進行を止めた。</param>
/// <param name="CorruptedAdded">カオス度の閾値を越えて、山札に混ぜた汚染ルーンの数。</param>
/// <param name="RunesGained">建物が建って、捨札に加わった基本ルーンの数。</param>
/// <param name="Prayer">予言者がささやいた祈り（新しい依頼の祈りか、日々の祈り）。祈っていなければ null。</param>
/// <param name="ScriptDecayed">この時間の進みで、星の字が崩れた。</param>
/// <param name="SteleMisreadings">字が崩れて、読み違えられた碑の数。</param>
public sealed record ProphetUpdate(
    ProphetRequestRule? NewRequest, bool PausedAutoPlay, int CorruptedAdded = 0, int RunesGained = 0, Prayer? Prayer = null,
    bool ScriptDecayed = false, int SteleMisreadings = 0);

/// <summary>
/// 予言者の祈り（docs/domain/oracle-pair.md の「祈りの言葉と語彙の見せ方」）。祈りの文は、困りごとと、予言者が知っている名詞から作る。
/// </summary>
/// <param name="Request">祈りの元の依頼。日々の祈りなら null。</param>
/// <param name="NounIds">祈りに入れる名詞（概念の ID。手札の記号のものだけ）。</param>
public sealed record Prayer(ProphetRequestRule? Request, IReadOnlyList<string> NounIds);
