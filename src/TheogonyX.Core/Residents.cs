namespace TheogonyX.Core;

/// <summary>住人の一覧と墓（docs/domain/agent-needs.md）。</summary>
public sealed class Residents
{
    private List<Resident> _people;
    private List<Grave> _graves;

    public Residents(IEnumerable<Resident> people, IEnumerable<Grave> graves, int nextResidentId)
    {
        ArgumentNullException.ThrowIfNull(people);
        ArgumentNullException.ThrowIfNull(graves);
        ArgumentOutOfRangeException.ThrowIfNegative(nextResidentId);
        _people = [.. people];
        _graves = [.. graves];
        NextResidentId = nextResidentId;
    }

    /// <summary>生きている住人。ID の小さい順。</summary>
    public IReadOnlyList<Resident> People => _people;

    /// <summary>残っている墓。死んだ順。</summary>
    public IReadOnlyList<Grave> Graves => _graves;

    /// <summary>次に移り住む住人の ID。</summary>
    public int NextResidentId { get; private set; }

    /// <summary>住人も墓も無い状態。</summary>
    public static Residents CreateInitial() => new([], [], 0);

    /// <summary>今の値と同じ中身の、別の状態（セーブに書き出す写し）。</summary>
    public Residents Copy() => new(_people, _graves, NextResidentId);

    /// <summary>世界再創生: 住人と墓をすべて消し、次の住人の ID だけを残した状態を返す。</summary>
    public Residents Recreate() => new([], [], NextResidentId);

    internal void Replace(List<Resident> people, List<Grave> graves, int nextResidentId)
    {
        _people = people;
        _graves = graves;
        NextResidentId = nextResidentId;
    }
}
