namespace TheogonyX.Core;

/// <summary>
/// 人が字を読む・書く（docs/domain/script-decipher.md の「人が神の字を読む」「祈りの字」）。乱数は使わない。
/// </summary>
public static class ScriptReader
{
    /// <summary>
    /// 神が組んだ字の列を、人が読む。字ごとに、星の並び方の決まりで読み、決まりと違う並びの字は、部品の場所が入れ替わって読まれる。
    /// 意味の場所に意味の部品が無い・働きの場所に働きの部品が無い・方角の場所に方角の部品が無い・予言者の知らない記号の字は、謎の字。
    /// 崩れた後の時代は、似た形の組の部品は、組の決まった一方（ID の小さい方）として読まれる。
    /// 教えは、読めた字のうち最初の働きの部品つきの字（記号 × 種別）。無ければ、最初の意味の部品だけの字の記号の崇拝。行き先は最初の方角。
    /// 字が 0 個か上限を越える・定義に無い部品を含むときは、理由（<c>no_glyphs</c>・<c>too_many_glyphs</c>・<c>radical_not_found</c>）を返す。
    /// </summary>
    /// <param name="knownRadicalIds">神が組める部品（手帳に載った部品）。</param>
    /// <param name="handSymbolIds">予言者が知っている記号（手札の記号）。</param>
    public static ScriptReadOutcome Read(
        PlanetScript script, ScriptRules rules, IReadOnlyList<Glyph> glyphs, IReadOnlySet<string> knownRadicalIds,
        IReadOnlyCollection<string> handSymbolIds, bool decayed)
    {
        ArgumentNullException.ThrowIfNull(script);
        ArgumentNullException.ThrowIfNull(rules);
        ArgumentNullException.ThrowIfNull(glyphs);
        ArgumentNullException.ThrowIfNull(knownRadicalIds);
        ArgumentNullException.ThrowIfNull(handSymbolIds);
        if (glyphs.Count == 0)
        {
            return new ScriptReadOutcome(null, "no_glyphs");
        }
        if (glyphs.Count > ScriptRules.MaxGlyphsPerOracle)
        {
            return new ScriptReadOutcome(null, "too_many_glyphs");
        }
        var used = glyphs.SelectMany(glyph => new[] { glyph.First, glyph.Second, glyph.Direction }).OfType<string>();
        if (used.Any(id => !knownRadicalIds.Contains(id) || script.Find(id) is null))
        {
            return new ScriptReadOutcome(null, "radical_not_found");
        }

        var readings = glyphs.Select(glyph => ReadGlyph(script, glyph, handSymbolIds, decayed)).ToList();
        var withType = readings.FirstOrDefault(reading => reading.IsReadable && reading.Type is not null);
        var meaningOnly = readings.FirstOrDefault(reading => reading.IsReadable);
        Dogma? teaching = withType is not null
            ? new Dogma(withType.SymbolId!, withType.Type!.Value, rules.TeachingIntensity)
            : meaningOnly is not null ? new Dogma(meaningOnly.SymbolId!, DogmaType.Worship, rules.TeachingIntensity) : null;
        var destination = readings.FirstOrDefault(reading => reading.IsReadable && reading.Direction != TravelDirection.None)?.Direction
            ?? TravelDirection.None;
        return new ScriptReadOutcome(new ScriptReading(teaching, destination, readings), null);
    }

    /// <summary>字 1 つを読む。</summary>
    public static GlyphReading ReadGlyph(PlanetScript script, Glyph glyph, IReadOnlyCollection<string> handSymbolIds, bool decayed)
    {
        ArgumentNullException.ThrowIfNull(script);
        ArgumentNullException.ThrowIfNull(glyph);
        ArgumentNullException.ThrowIfNull(handSymbolIds);

        // 部品 1 つだけの字は並びを問わない。2 つの字は、並びが決まりと違えば場所が入れ替わって読まれる
        var swapped = glyph.Second is not null && glyph.Arrangement != script.Arrangement;
        var meaningSlot = swapped ? glyph.Second! : glyph.First;
        var functionSlot = swapped ? glyph.First : glyph.Second;

        var meaning = Resolve(script, meaningSlot, decayed);
        if (meaning is not { Kind: RadicalKind.Meaning } || !handSymbolIds.Contains(meaning.Key))
        {
            return GlyphReading.Unreadable;
        }
        DogmaType? type = null;
        if (functionSlot is not null)
        {
            var function = Resolve(script, functionSlot, decayed);
            if (function is not { Kind: RadicalKind.Function })
            {
                return GlyphReading.Unreadable;
            }
            type = Enum.Parse<DogmaType>(function.Key);
        }
        var direction = TravelDirection.None;
        if (glyph.Direction is not null)
        {
            var directionRadical = Resolve(script, glyph.Direction, decayed);
            if (directionRadical is not { Kind: RadicalKind.Direction })
            {
                return GlyphReading.Unreadable;
            }
            direction = Enum.Parse<TravelDirection>(directionRadical.Key);
        }
        return new GlyphReading(meaning.Key, type, direction);
    }

    /// <summary>
    /// 祈りの字の列。依頼の祈りは、1 字目が「困りごとの記号＋崇拝」、続いて選んだ記号の意味の部品だけの字（困りごとの記号は重ねない）。
    /// 日々の祈りは、選んだ記号の意味の部品だけの字。字の無い記号は飛ばす。
    /// </summary>
    public static IReadOnlyList<Glyph> PrayerGlyphs(PlanetScript script, ProphetRequestRule? request, IReadOnlyList<string> symbolIds)
    {
        ArgumentNullException.ThrowIfNull(script);
        ArgumentNullException.ThrowIfNull(symbolIds);
        var glyphs = new List<Glyph>();
        if (request is not null && script.Find(RadicalKind.Meaning, request.SymbolId) is not null)
        {
            glyphs.Add(script.GlyphFor(request.SymbolId, DogmaType.Worship));
        }
        foreach (var symbolId in symbolIds.Distinct())
        {
            if (symbolId != request?.SymbolId && script.Find(RadicalKind.Meaning, symbolId) is not null)
            {
                glyphs.Add(script.GlyphFor(symbolId));
            }
        }
        return glyphs;
    }

    // 崩れた後は、似た形の組のどちらも、ID の小さい方として読まれる
    private static ScriptRadical? Resolve(PlanetScript script, string radicalId, bool decayed)
    {
        var (a, b) = script.SimilarPair;
        if (decayed && (radicalId == a || radicalId == b))
        {
            radicalId = string.CompareOrdinal(a, b) < 0 ? a : b;
        }
        return script.Find(radicalId);
    }
}
