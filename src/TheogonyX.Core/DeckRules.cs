namespace TheogonyX.Core;

/// <summary>最初のデッキの、記号 1 種類ぶんの枚数。</summary>
/// <param name="SymbolId">基本ルーンの記号（第 1 段では、ルーンの ID は記号の ID と同じ）。</param>
/// <param name="Count">山札に入れる枚数。</param>
public sealed record DeckRuneCount(string SymbolId, int Count);

/// <summary>建物が建ったときに、捨札に加わる基本ルーン。</summary>
/// <param name="BuildingType"><see cref="TheogonyX.Core.BuildingType"/> の名前（<c>Workshop</c>・<c>Shrine</c>。家は不可）。</param>
/// <param name="SymbolId">加わる基本ルーンの記号。</param>
public sealed record BuildingRune(string BuildingType, string SymbolId);

/// <summary>神託のデッキのゲームデータ定義（docs/domain/oracle-deck.md）。</summary>
/// <param name="InitialRunes">最初の山札に入れるルーン。</param>
/// <param name="HandSize">最初に手札に引く枚数。</param>
/// <param name="PruneChargesPerReset">世界再創生のたびに得る、精査（ルーンの削除）の持ち分。</param>
/// <param name="BuildingRunes">建物が建ったときに捨札に加わる基本ルーン。省略したら加わらない。</param>
/// <param name="MaxSize">デッキの総数（山札・手札・捨札）の上限。これ以上は加えない。0 は上限なし。</param>
public sealed record DeckRules(
    IReadOnlyList<DeckRuneCount> InitialRunes, int HandSize, int PruneChargesPerReset = 0, IReadOnlyList<BuildingRune>? BuildingRunes = null, int MaxSize = 0)
{
    /// <summary>値の範囲を確かめる。範囲外なら <see cref="ArgumentException"/>。</summary>
    public void Validate(IReadOnlySet<string> symbolIds)
    {
        ArgumentNullException.ThrowIfNull(symbolIds);
        if (InitialRunes is null || InitialRunes.Count == 0 || HandSize < 1)
        {
            throw new ArgumentException("デッキの定義が空か、手札の枚数が 1 未満");
        }
        if (PruneChargesPerReset < 0)
        {
            throw new ArgumentException($"精査の持ち分が負: pruneChargesPerReset={PruneChargesPerReset}");
        }
        if (MaxSize < 0)
        {
            throw new ArgumentException($"デッキの上限が負: maxSize={MaxSize}");
        }
        foreach (var rune in BuildingRunes ?? [])
        {
            if (!Enum.TryParse<BuildingType>(rune.BuildingType, out var type) || type.IsHouse() || !symbolIds.Contains(rune.SymbolId))
            {
                throw new ArgumentException($"建物のルーンの定義の値が範囲外: buildingType={rune.BuildingType} symbolId={rune.SymbolId}");
            }
        }
        foreach (var rune in InitialRunes)
        {
            if (!symbolIds.Contains(rune.SymbolId) || rune.Count < 1)
            {
                throw new ArgumentException($"デッキの定義の値が範囲外: symbolId={rune.SymbolId} count={rune.Count}");
            }
        }
    }
}
