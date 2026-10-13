using System.Globalization;
using Godot;
using TheogonyX.Core;

namespace TheogonyX.Scenes.Main;

/// <summary>住人の名前と称号を、翻訳ファイルの表記と書式で組み立てる（docs/domain/agent-needs.md の「名前と称号」）。</summary>
public static class ResidentText
{
    /// <summary>「名・家名 の 称号」。称号が無ければ「名・家名」。</summary>
    public static string NameOf(ResidentName name, ResidentEpithet? epithet)
    {
        var culture = CultureInfo.CurrentCulture;
        var first = string.Format(culture, Tr("RESIDENT_NAME_PART"), Tr(name.FirstPrefix), Tr(name.FirstSuffix));
        var lineage = string.Format(culture, Tr("RESIDENT_NAME_PART"), Tr(name.LineagePrefix), Tr(name.LineageSuffix));
        return epithet is null
            ? string.Format(culture, Tr("RESIDENT_NAME"), first, lineage)
            : string.Format(culture, Tr("RESIDENT_NAME_WITH_EPITHET"), first, lineage, EpithetOf(epithet));
    }

    private static string EpithetOf(ResidentEpithet epithet) =>
        epithet.SymbolId is null
            ? Tr(epithet.Id)
            : string.Format(CultureInfo.CurrentCulture, Tr(epithet.Id), Tr($"SYMBOL_{epithet.SymbolId}"));

    private static string Tr(string key) => TranslationServer.Translate(key).ToString();
}
