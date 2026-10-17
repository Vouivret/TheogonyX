namespace TheogonyX.Core;

/// <summary>
/// 字 1 つ（docs/domain/script-decipher.md の「字」）。<paramref name="First"/> は左（左右の並び）か上（上下の並び）の部品、
/// <paramref name="Second"/> は右か下の部品（無ければ null。部品 1 つだけの字）。<paramref name="Direction"/> は下に小さく付ける方角の部品。
/// 人が書く字は、意味の部品が <paramref name="First"/>、働きの部品が <paramref name="Second"/> で、並びは星の決まり（<see cref="PlanetScript.Arrangement"/>）。
/// 神が組む字は、部品の場所も並びも自由（決まりと違えば、人は読み違える）。
/// </summary>
public sealed record Glyph(string First, string? Second, string? Direction, GlyphArrangement Arrangement);

/// <summary>人が字 1 つを読んだ結果。読めなければ <paramref name="SymbolId"/> は null（謎の字）。</summary>
/// <param name="Type">働きの部品の種別。部品 1 つだけの字なら null。</param>
public sealed record GlyphReading(string? SymbolId, DogmaType? Type, TravelDirection Direction)
{
    public bool IsReadable => SymbolId is not null;

    public static readonly GlyphReading Unreadable = new(null, null, TravelDirection.None);
}

/// <summary>人が神託の字の列を読んだ結果。</summary>
/// <param name="Teaching">教え。読めた字が無ければ null。</param>
/// <param name="Glyphs">字ごとの読み（神が組んだ順）。</param>
public sealed record ScriptReading(Dogma? Teaching, TravelDirection Destination, IReadOnlyList<GlyphReading> Glyphs);

/// <summary>字の神託を下した結果。<paramref name="RejectedReason"/> が null なら、<paramref name="Result"/> と <paramref name="Reading"/> がある。</summary>
public sealed record ScriptSpeakOutcome(string? RejectedReason, SpeakResult? Result, ScriptReading? Reading);

/// <summary>読み取りの結果。<paramref name="RejectedReason"/> が null なら <paramref name="Reading"/> がある。</summary>
public sealed record ScriptReadOutcome(ScriptReading? Reading, string? RejectedReason);
