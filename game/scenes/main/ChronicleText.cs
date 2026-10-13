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
                string.Format(culture, Tr("DOGMA_ENTRY"), Tr($"SYMBOL_{entry.SymbolId}"), Tr($"DOGMA_TYPE_{entry.DogmaType.ToString()!.ToUpperInvariant()}")),
                entry.Oracle?.Heading),
            HistoryEventType.ResidentBrokeDown => string.Format(
                culture, Tr(entry.Breakdown == BreakdownKind.Rampage ? "HISTORY_RAMPAGE" : "HISTORY_CATATONIA"), entry.Year, Who()),
            HistoryEventType.NotableDied => string.Format(culture, Tr("HISTORY_NOTABLE_DIED"), entry.Year, Who()),
            HistoryEventType.WorldDestroyed => string.Format(culture, Tr("HISTORY_WORLD_DESTROYED"), entry.Year),
            HistoryEventType.ChapterEnded when entry.Myth is null => string.Format(
                culture, Tr("HISTORY_CHAPTER"), entry.FromYear, entry.Year, entry.ChaosFrom, entry.ChaosTo),
            HistoryEventType.ChapterEnded => string.Format(
                culture, Tr("HISTORY_CHAPTER_MYTH"), entry.FromYear, entry.Year, entry.Myth.Heading, entry.Myth.Body),
            _ => string.Format(culture, Tr("HISTORY_WORLD_RECREATED"), entry.Year),
        };
    }

    public static string Tr(string key) => TranslationServer.Translate(key).ToString();
}
