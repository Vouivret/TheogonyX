using System.Collections;

namespace TheogonyX.Core;

/// <summary>
/// 住人が信じている教えの並び。住人の記録（record）が値として比べられるよう、中身で同じかどうかを判る。
/// 作った後は変えない。
/// </summary>
public sealed class BeliefSet : IReadOnlyList<Belief>, IEquatable<BeliefSet>
{
    public static readonly BeliefSet Empty = new([]);

    private readonly Belief[] _items;

    public BeliefSet(IEnumerable<Belief> items)
    {
        ArgumentNullException.ThrowIfNull(items);
        _items = [.. items];
    }

    public int Count => _items.Length;

    public Belief this[int index] => _items[index];

    public static implicit operator BeliefSet(List<Belief> items) => new(items);

    public bool Equals(BeliefSet? other) => other is not null && _items.AsSpan().SequenceEqual(other._items);

    public override bool Equals(object? obj) => obj is BeliefSet other && Equals(other);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (var item in _items)
        {
            hash.Add(item);
        }
        return hash.ToHashCode();
    }

    public IEnumerator<Belief> GetEnumerator() => ((IEnumerable<Belief>)_items).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
