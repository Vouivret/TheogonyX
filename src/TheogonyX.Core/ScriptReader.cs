namespace TheogonyX.Core;

/// <summary>
/// 人が文を読む・書く（docs/domain/script-decipher.md の「7. 語の字と神託の碑」）。乱数は使わない。
/// </summary>
public static class ScriptReader
{
    /// <summary>
    /// 神が書いた文を、人が読む。方角の語は組の外で読み、最初の方角を行き先にする。残りの語を、星の語順で前から読む。
    /// 名詞が先の星では、名詞のすぐ後の動詞がその名詞の組になる。動詞が先の星では、動詞のすぐ後の名詞がその動詞の組になる。
    /// 動詞のつながらない名詞は崇拝と読み、名詞につながらない動詞は読み落とす。予言者の知らない記号の名詞は謎の語。
    /// 崩れた後の時代は、似た形の組の語は、組の決まった一方（ID の小さい方）として読まれる。
    /// 教えは、動詞で読めた組の最初のもの。無ければ、崇拝と読んだ組の最初のもの。
    /// 語が 0 個か上限を越える・手帳に無い語を含むときは、理由（<c>no_glyphs</c>・<c>too_many_glyphs</c>・<c>glyph_not_found</c>）を返す。
    /// </summary>
    /// <param name="knownRadicalIds">神が書ける語（手帳に載った語）。</param>
    /// <param name="handSymbolIds">予言者が知っている記号（手札の記号）。</param>
    public static ScriptReadOutcome Read(
        PlanetScript script, ScriptRules rules, Sentence sentence, IReadOnlySet<string> knownRadicalIds,
        IReadOnlyCollection<string> handSymbolIds, bool decayed)
    {
        ArgumentNullException.ThrowIfNull(script);
        ArgumentNullException.ThrowIfNull(rules);
        ArgumentNullException.ThrowIfNull(sentence);
        ArgumentNullException.ThrowIfNull(knownRadicalIds);
        ArgumentNullException.ThrowIfNull(handSymbolIds);
        if (sentence.Words.Count == 0)
        {
            return new ScriptReadOutcome(null, "no_glyphs");
        }
        if (sentence.Words.Count > ScriptRules.MaxGlyphsPerOracle)
        {
            return new ScriptReadOutcome(null, "too_many_glyphs");
        }
        if (sentence.Words.Any(id => !knownRadicalIds.Contains(id) || script.Find(id) is null))
        {
            return new ScriptReadOutcome(null, "glyph_not_found");
        }

        var words = sentence.Words.Select(id => Resolve(script, id, decayed)).ToList();
        var destination = words.FirstOrDefault(word => word.Kind == RadicalKind.Direction) is { } direction
            ? Enum.Parse<TravelDirection>(direction.Key)
            : TravelDirection.None;
        var core = words.Where(word => word.Kind != RadicalKind.Direction).ToList();
        var phrases = new List<PhraseReading>();
        var dropped = 0;
        PhraseReading Phrase(ScriptRadical noun, ScriptRadical? verb) => new(
            handSymbolIds.Contains(noun.Key) ? noun.Key : null, verb is null ? DogmaType.Worship : Enum.Parse<DogmaType>(verb.Key), verb is not null);
        for (var i = 0; i < core.Count; i++)
        {
            var (word, next) = (core[i], i + 1 < core.Count ? core[i + 1] : null);
            var (head, tail) = script.WordOrder == WordOrder.NounFirst
                ? (RadicalKind.Meaning, RadicalKind.Function)
                : (RadicalKind.Function, RadicalKind.Meaning);
            if (word.Kind != head)
            {
                // 組の後ろにしか来ない語が、前の語につながらずに残った。名詞なら崇拝と読み、動詞なら読み落とす
                if (word.Kind == RadicalKind.Meaning)
                {
                    phrases.Add(Phrase(word, null));
                }
                else
                {
                    dropped++;
                }
                continue;
            }
            if (next?.Kind == tail)
            {
                phrases.Add(head == RadicalKind.Meaning ? Phrase(word, next) : Phrase(next, word));
                i++;
            }
            else if (head == RadicalKind.Meaning)
            {
                phrases.Add(Phrase(word, null));
            }
            else
            {
                dropped++;
            }
        }

        var chosen = phrases.FirstOrDefault(phrase => phrase.IsReadable && phrase.VerbRead) ?? phrases.FirstOrDefault(phrase => phrase.IsReadable);
        var teaching = chosen is null ? null : new Dogma(chosen.SymbolId!, chosen.Type, rules.TeachingIntensity);
        return new ScriptReadOutcome(new ScriptReading(teaching, destination, phrases, dropped), null);
    }

    /// <summary>
    /// 祈りの文。依頼の祈りは、初めに「困りごとの記号＋崇めよ」を星の語順で、続いて選んだ記号の名詞（困りごとの記号は重ねない）。
    /// 日々の祈りは、選んだ記号の名詞を並べる。字の無い記号は飛ばす。
    /// </summary>
    public static Sentence PrayerSentence(PlanetScript script, ProphetRequestRule? request, IReadOnlyList<string> symbolIds)
    {
        ArgumentNullException.ThrowIfNull(script);
        ArgumentNullException.ThrowIfNull(symbolIds);
        var words = new List<string>();
        if (request is not null && script.Find(RadicalKind.Meaning, request.SymbolId) is not null)
        {
            words.AddRange(script.SentenceFor(request.SymbolId, DogmaType.Worship).Words);
        }
        foreach (var symbolId in symbolIds.Distinct())
        {
            if (symbolId != request?.SymbolId && script.Find(RadicalKind.Meaning, symbolId) is { } noun)
            {
                words.Add(noun.Id);
            }
        }
        return new Sentence(words);
    }

    // 崩れた後は、似た形の組のどちらも、ID の小さい方として読まれる
    private static ScriptRadical Resolve(PlanetScript script, string radicalId, bool decayed)
    {
        var (a, b) = script.SimilarPair;
        if (decayed && (radicalId == a || radicalId == b))
        {
            radicalId = string.CompareOrdinal(a, b) < 0 ? a : b;
        }
        return script.Find(radicalId)!;
    }
}
