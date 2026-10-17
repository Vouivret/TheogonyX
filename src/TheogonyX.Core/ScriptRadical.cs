namespace TheogonyX.Core;

/// <summary>字の部品の種類（docs/domain/script-decipher.md の「字のしくみ」）。</summary>
public enum RadicalKind
{
    /// <summary>意味の部品（何について。記号ごとに 1 つ）。</summary>
    Meaning,

    /// <summary>働きの部品（どうする。崇拝・禁忌・破壊）。</summary>
    Function,

    /// <summary>方角の部品（東・西。字の下に小さく付ける）。</summary>
    Direction,
}

/// <summary>部品の線 1 本。格子点 2 つを結ぶ。向きを持たないので、端点は小さい順にそろえる（<see cref="Of"/>）。</summary>
public readonly record struct Stroke(int X1, int Y1, int X2, int Y2) : IComparable<Stroke>
{
    public int CompareTo(Stroke other) => (X1, Y1, X2, Y2).CompareTo((other.X1, other.Y1, other.X2, other.Y2));

    /// <summary>端点をそろえた線。</summary>
    public static Stroke Of(int x1, int y1, int x2, int y2) =>
        (x1, y1).CompareTo((x2, y2)) <= 0 ? new Stroke(x1, y1, x2, y2) : new Stroke(x2, y2, x1, y1);
}

/// <summary>
/// 字の部品。形は格子点を結ぶ線の集まり。崩れると <paramref name="DecayStroke"/> の線が消える。
/// <paramref name="Key"/> は、意味の部品なら記号の ID、働きの部品なら <see cref="DogmaType"/> の名前、方角の部品なら <see cref="TravelDirection"/> の名前。
/// ID は意味を表さない（見た目からだけ推理するため）。
/// </summary>
public sealed record ScriptRadical(string Id, RadicalKind Kind, string Key, IReadOnlyList<Stroke> Strokes, Stroke DecayStroke)
{
    /// <summary>時代ごとの形（崩れた後は <see cref="DecayStroke"/> を除く）。線は並べ替えてそろえる。</summary>
    public IReadOnlyList<Stroke> ShapeIn(bool decayed) =>
        [.. Strokes.Where(stroke => !decayed || stroke != DecayStroke).Order()];
}
