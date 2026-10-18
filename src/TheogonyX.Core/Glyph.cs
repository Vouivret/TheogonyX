namespace TheogonyX.Core;

/// <summary>
/// 文（docs/domain/script-decipher.md の「7. 語の字と神託の碑」）。語の字（<see cref="ScriptRadical"/>）の ID を並べたもの。
/// 人が書く文は星の語順（<see cref="PlanetScript.WordOrder"/>）で並ぶ。神が書く文は自由に並べられる（語順が違えば、人は読み違える）。
/// 語の並びで等しさを比べる。
/// </summary>
public sealed record Sentence(IReadOnlyList<string> Words)
{
    public bool Equals(Sentence? other) => other is not null && Words.SequenceEqual(other.Words);

    public override int GetHashCode() => Words.Aggregate(17, (hash, word) => HashCode.Combine(hash, word));

    public override string ToString() => string.Join(' ', Words);
}

/// <summary>
/// 人が文の中の名詞 1 つを読んだ結果（名詞と、それにつながった動詞の組）。読めなければ <paramref name="SymbolId"/> は null（謎の語）。
/// </summary>
/// <param name="Type">つながった動詞の種別。動詞がつながらなければ崇拝（<paramref name="VerbRead"/> は false）。</param>
public sealed record PhraseReading(string? SymbolId, DogmaType Type, bool VerbRead)
{
    public bool IsReadable => SymbolId is not null;
}

/// <summary>人が神託の文を読んだ結果。</summary>
/// <param name="Teaching">教え。読めた組が無ければ null。</param>
/// <param name="Phrases">組ごとの読み（文の順）。</param>
/// <param name="DroppedVerbs">名詞につながらずに読み落とされた動詞の数。</param>
public sealed record ScriptReading(Dogma? Teaching, TravelDirection Destination, IReadOnlyList<PhraseReading> Phrases, int DroppedVerbs);

/// <summary>字の神託を下した結果。<paramref name="RejectedReason"/> が null なら、<paramref name="Result"/> と <paramref name="Reading"/> がある。</summary>
public sealed record ScriptSpeakOutcome(string? RejectedReason, SpeakResult? Result, ScriptReading? Reading);

/// <summary>読み取りの結果。<paramref name="RejectedReason"/> が null なら <paramref name="Reading"/> がある。</summary>
public sealed record ScriptReadOutcome(ScriptReading? Reading, string? RejectedReason);
