namespace TheogonyX.Core;

/// <summary>住人の一覧と墓（docs/domain/agent-needs.md）。</summary>
public sealed class Residents
{
    private List<Resident> _people;
    private List<Grave> _graves;
    private Dictionary<ResidentPair, int> _affinities;

    /// <param name="affinities">住人どうしの好感度。0 の組は持たない。</param>
    public Residents(
        IEnumerable<Resident> people, IEnumerable<Grave> graves, int nextResidentId, IEnumerable<KeyValuePair<ResidentPair, int>>? affinities = null)
    {
        ArgumentNullException.ThrowIfNull(people);
        ArgumentNullException.ThrowIfNull(graves);
        ArgumentOutOfRangeException.ThrowIfNegative(nextResidentId);
        _people = [.. people];
        _graves = [.. graves];
        NextResidentId = nextResidentId;
        _affinities = (affinities ?? []).Where(pair => pair.Value > 0).ToDictionary();
    }

    /// <summary>生きている住人。ID の小さい順。</summary>
    public IReadOnlyList<Resident> People => _people;

    /// <summary>残っている墓。死んだ順。</summary>
    public IReadOnlyList<Grave> Graves => _graves;

    /// <summary>次に移り住む住人の ID。</summary>
    public int NextResidentId { get; private set; }

    /// <summary>住人どうしの好感度（docs/domain/agent-needs.md の「好感度」）。0 の組は持たない。</summary>
    public IReadOnlyDictionary<ResidentPair, int> Affinities => _affinities;

    /// <summary>2 人の好感度。組が無ければ 0。</summary>
    public int AffinityOf(int first, int second) => first == second ? 0 : _affinities.GetValueOrDefault(ResidentPair.Of(first, second));

    /// <summary>
    /// <paramref name="id"/> の住人といちばん好感度の高い相手の ID。好感度が <paramref name="atOrAbove"/> に届く相手がいなければ null。
    /// 同じ好感度なら ID の小さい相手。
    /// </summary>
    public int? ClosestOf(int id, int atOrAbove) =>
        _affinities
            .Where(pair => pair.Key.Contains(id) && pair.Value >= atOrAbove)
            .OrderByDescending(pair => pair.Value)
            .ThenBy(pair => pair.Key.Other(id))
            .Select(pair => (int?)pair.Key.Other(id))
            .FirstOrDefault();

    /// <summary>住人も墓も無い状態。</summary>
    public static Residents CreateInitial() => new([], [], 0);

    /// <summary>今の値と同じ中身の、別の状態（セーブに書き出す写し）。</summary>
    public Residents Copy() => new(_people, _graves, NextResidentId, _affinities);

    /// <summary>世界再創生: 住人と墓をすべて消し、次の住人の ID だけを残した状態を返す。</summary>
    public Residents Recreate() => new([], [], NextResidentId);

    internal void Replace(List<Resident> people, List<Grave> graves, int nextResidentId)
    {
        _people = people;
        _graves = graves;
        NextResidentId = nextResidentId;
    }

    internal void ReplaceAffinities(Dictionary<ResidentPair, int> affinities) =>
        _affinities = affinities.Where(pair => pair.Value > 0).ToDictionary();
}
