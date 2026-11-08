using System.Globalization;
using System.Linq;
using Godot;
using TheogonyX.Core;

namespace TheogonyX.Scenes.Main;

/// <summary>
/// 年表の行を、表示している言語の文にする（docs/domain/history.md）。年表の欄（<see cref="HistoryView"/>）と、
/// 神話の頼み（<see cref="MythView"/>）で同じ文を使う。
/// </summary>
public static class ChronicleText
{
    public static string LineOf(HistoryEntry entry)
    {
        var culture = CultureInfo.CurrentCulture;
        string Who() => ResidentText.NameOf(entry.Name!, entry.Epithet);
        return entry.Type switch
        {
            HistoryEventType.DogmaFormed => string.Format(
                culture, Tr(entry.Oracle is null ? "HISTORY_DOGMA_FORMED" : "HISTORY_DOGMA_FORMED_ORACLE"), entry.Year,
                TeachingOf(entry.SymbolId, entry.DogmaType, entry.Reading), entry.Oracle?.Heading),
            HistoryEventType.DogmaForgotten => string.Format(
                culture, Tr("HISTORY_DOGMA_FORGOTTEN"), entry.Year,
                string.Format(culture, Tr("DOGMA_ENTRY"), Tr($"SYMBOL_{entry.SymbolId}"), Tr($"DOGMA_TYPE_{entry.DogmaType.ToString()!.ToUpperInvariant()}"))),
            HistoryEventType.ScriptDecayed => string.Format(culture, Tr("HISTORY_SCRIPT_DECAYED"), entry.Year),
            HistoryEventType.SteleMisread => string.Format(
                culture, Tr("HISTORY_STELE_MISREAD"), entry.Year, entry.FromYear,
                TeachingOf(entry.SymbolId, entry.DogmaType, entry.Reading),
                string.Format(culture, Tr("DOGMA_ENTRY"), Tr($"SYMBOL_{entry.WrittenSymbolId}"), Tr($"DOGMA_TYPE_{entry.WrittenDogmaType.ToString()!.ToUpperInvariant()}"))),
            HistoryEventType.Exiled => string.Format(
                culture, Tr("HISTORY_EXILED"), entry.Year, entry.Count, TeachingOf(entry.SymbolId, entry.DogmaType, null), entry.Region, entry.ToRegion),
            HistoryEventType.ResidentBrokeDown => string.Format(
                culture, Tr(entry.Breakdown == BreakdownKind.Rampage ? "HISTORY_RAMPAGE" : "HISTORY_CATATONIA"), entry.Year, Who()),
            HistoryEventType.NotableDied => string.Format(culture, Tr("HISTORY_NOTABLE_DIED"), entry.Year, Who()),
            HistoryEventType.WorldDestroyed => string.Format(culture, Tr("HISTORY_WORLD_DESTROYED"), entry.Year),
            HistoryEventType.ChapterEnded when entry.Myth is null => string.Format(
                culture, Tr("HISTORY_CHAPTER"), entry.FromYear, entry.Year, entry.ChaosFrom, entry.ChaosTo),
            HistoryEventType.ChapterEnded => string.Format(
                culture, Tr("HISTORY_CHAPTER_MYTH"), entry.FromYear, entry.Year, entry.Myth.Heading),
            _ => string.Format(culture, Tr("HISTORY_WORLD_RECREATED"), entry.Year),
        };
    }

    /// <summary>教えの名前。読みがあれば添える（docs/domain/script-decipher.md の 9。例: 木の破壊（切り開く））。</summary>
    public static string TeachingOf(string? symbolId, DogmaType? type, OracleReading? reading)
    {
        var culture = CultureInfo.CurrentCulture;
        var teaching = string.Format(culture, Tr("DOGMA_ENTRY"), Tr($"SYMBOL_{symbolId}"), Tr($"DOGMA_TYPE_{type.ToString()!.ToUpperInvariant()}"));
        return reading is { } read
            ? string.Format(culture, Tr("DOGMA_ENTRY_READING"), teaching, Tr($"READING_{read.ToString().ToUpperInvariant()}"))
            : teaching;
    }

    public static string Tr(string key) => TranslationServer.Translate(key).ToString();
}
