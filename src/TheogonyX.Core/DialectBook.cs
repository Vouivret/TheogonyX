namespace TheogonyX.Core;

/// <summary>方言帳の行の理由。</summary>
public enum DialectReason
{
    /// <summary>この予言者の言葉（ずれ表）。</summary>
    Dialect,

    /// <summary>知らない言葉（近い知っている言葉に聞き違えた）。</summary>
    Unknown,

    /// <summary>謎の言葉（どの記号にも当たらず、経典にだけ残る）。</summary>
    Mystery,
}

/// <summary>
/// 方言帳の 1 行。神の言葉は概念の ID。謎の言葉は <paramref name="GodWord"/> が神の文の言葉そのもので、
/// <paramref name="HeardWordId"/> が null。
/// </summary>
public sealed record DialectEntry(string GodWord, string? HeardWordId, DialectReason Reason);

/// <summary>方言帳（神託で分かった聞き違えを、自動で書き留める欄）。同じ行は 2 度足さない。</summary>
public sealed class DialectBook
{
    private readonly List<DialectEntry> _entries = [];

    /// <summary>行（古い順。見せるときは新しい順に並べる）。</summary>
    public IReadOnlyList<DialectEntry> Entries => _entries;

    /// <summary>行を足す。すでに同じ行があれば足さず false。</summary>
    public bool Add(DialectEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        if (_entries.Contains(entry))
        {
            return false;
        }
        _entries.Add(entry);
        return true;
    }
}
