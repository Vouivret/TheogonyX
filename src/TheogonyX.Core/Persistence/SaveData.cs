namespace TheogonyX.Core.Persistence;

/// <summary>セーブデータの中身（docs/domain/save-data.md）。時間の状態は持たない（読み込んだ後は止まった状態にする）。</summary>
/// <param name="Seed">夢の乱数のシード。</param>
/// <param name="DreamsGenerated">これまでに見た夢の数。乱数を同じところまで進め直すのに使う。</param>
/// <param name="RequestId">依頼中の困りごとの ID。依頼なしなら null。</param>
/// <param name="AnsweredYears">困りごとの ID ごとの、依頼が済んだ年（同じ困りごとを待つ年数の起点）。</param>
/// <param name="Speed">自動進行の速さ。</param>
/// <param name="ElapsedSeconds">自動進行の、その年の進み（秒）。</param>
/// <param name="Civilization">地域の文明の値と建物。</param>
/// <param name="Residents">住人と墓。</param>
/// <param name="History">年表の行（古い順）。年表を残さないなら空。</param>
/// <param name="Chapter">年表の今の章の始まり。</param>
/// <param name="Deck">神託のデッキ。デッキを持たないときは null。</param>
public sealed record SaveData(
    int Seed,
    int DreamsGenerated,
    WorldState World,
    IReadOnlyList<Dogma> Dogmas,
    ProphetDreamState DreamState,
    IReadOnlyList<DreamNode> Dream,
    string? RequestId,
    IReadOnlyDictionary<string, int> AnsweredYears,
    int LastOracleYear,
    AutoPlaySpeed Speed,
    double ElapsedSeconds,
    Civilization Civilization,
    Residents Residents,
    IReadOnlyList<HistoryEntry> History,
    ChapterStart Chapter,
    OracleDeckState? Deck = null);
