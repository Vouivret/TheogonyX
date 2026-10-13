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
        var first = Tr(JoinedKey(name.FirstPrefix, name.FirstSuffix));
        var lineage = Tr(JoinedKey(name.LineagePrefix, name.LineageSuffix));
        return epithet is null
            ? string.Format(culture, Tr("RESIDENT_NAME"), first, lineage)
            : string.Format(culture, Tr("RESIDENT_NAME_WITH_EPITHET"), first, lineage, EpithetOf(epithet));
    }

    // 接頭辞と接尾辞の組み合わせごとの表記のキー（NAME_PREFIX_ART ＋ NAME_SUFFIX_ORIUS → NAME_ART_ORIUS）。
    // つなぎ目の音は言語ごとに変わる（アルト ＋ トリウス は アルトトリウス でなく アルトリウス）ので、パーツを連結せず組み合わせで引く
    private static string JoinedKey(string prefix, string suffix) =>
        $"NAME_{prefix["NAME_PREFIX_".Length..]}_{suffix["NAME_SUFFIX_".Length..]}";

    private static string EpithetOf(ResidentEpithet epithet) =>
        epithet.SymbolId is null
            ? Tr(epithet.Id)
            : string.Format(CultureInfo.CurrentCulture, Tr(epithet.Id), Tr($"SYMBOL_{epithet.SymbolId}"));

    private static string Tr(string key) => TranslationServer.Translate(key).ToString();
}
