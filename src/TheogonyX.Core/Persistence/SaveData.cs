namespace TheogonyX.Core.Persistence;

/// <summary>セーブデータの中身（docs/domain/save-data.md）。時間の状態は持たない（読み込んだ後は止まった状態にする）。</summary>
/// <param name="Seed">シード。デッキのシャッフルと文明の計算の乱数を決める。</param>
/// <param name="RequestId">依頼中の困りごとの ID。依頼なしなら null。</param>
/// <param name="AnsweredYears">困りごとの ID ごとの、依頼が済んだ年（同じ困りごとを待つ年数の起点）。</param>
/// <param name="Speed">自動進行の速さ。</param>
/// <param name="ElapsedSeconds">自動進行の、その年の進み（秒）。</param>
/// <param name="Civilization">地域の文明の値と建物。</param>
/// <param name="Residents">住人と墓。</param>
/// <param name="History">年表の行（古い順）。年表を残さないなら空。</param>
/// <param name="Chapter">年表の今の章の始まり。</param>
/// <param name="Deck">神託のデッキ（山札・手札・捨札・シャッフルの回数）。</param>
/// <param name="Faith">神威の量と、直近の神託を下した年。</param>
/// <param name="Dialect">方言の状態（ずれ表・方言帳・神託の記録）。使っていなければ null。</param>
/// <param name="Prophet">予言者の居場所・知っている土地・行き先。無ければ（古いセーブ・文明の計算をしない）null。</param>
public sealed record SaveData(
    int Seed,
    WorldState World,
    IReadOnlyList<Dogma> Dogmas,
    string? RequestId,
    IReadOnlyDictionary<string, int> AnsweredYears,
    AutoPlaySpeed Speed,
    double ElapsedSeconds,
    Civilization Civilization,
    Residents Residents,
    IReadOnlyList<HistoryEntry> History,
    ChapterStart Chapter,
    OracleDeckState Deck,
    OracleFaithState Faith,
    ProphetState? Prophet = null,
    DialectSave? Dialect = null);
