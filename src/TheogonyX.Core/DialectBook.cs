namespace TheogonyX.Core;

/// <summary>方言帳の行の理由。</summary>
public enum DialectReason
{
    /// <summary>この予言者の言葉（ずれ表）。</summary>
    Dialect,

    /// <summary>知らない言葉（近い知っている言葉に聞き違えた）。</summary>
    Unknown,

    /// <summary>予言者の祈りに出た、知っている言葉（<see cref="DialectEntry.HeardWordId"/> は null）。</summary>
    Known,

    /// <summary>謎の言葉（どの記号にも当たらず、経典にだけ残る）。</summary>
    Mystery,
}

/// <summary>
/// 方言帳の 1 行。神の言葉は概念の ID。知っている言葉は <paramref name="GodWord"/> がその語の ID で、<paramref name="HeardWordId"/> が null。謎の言葉は <paramref name="GodWord"/> が神の文の言葉そのもので、
/// <paramref name="HeardWordId"/> が null。
/// </summary>
public sealed record DialectEntry(string GodWord, string? HeardWordId, DialectReason Reason);

/// <summary>
/// 方言帳（神託で分かった聞き違えを、自動で書き留める欄）。同じ神の言葉は 1 行だけ持ち、新しい結果で置き換える
/// （知っている言葉が、手札が変わって知らない言葉になったときなど）。
/// </summary>
public sealed class DialectBook
{
    private readonly List<DialectEntry> _entries = [];

    /// <summary>行（古い順。見せるときは新しい順に並べる）。</summary>
    public IReadOnlyList<DialectEntry> Entries => _entries;

    /// <summary>行を足す。同じ神の言葉の行があれば消してから、新しい行として足す。すでに同じ行があれば変えず false。</summary>
    public bool Add(DialectEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        if (_entries.Contains(entry))
        {
            return false;
        }
        _entries.RemoveAll(existing => existing.GodWord == entry.GodWord);
        _entries.Add(entry);
        return true;
    }
}
