namespace TheogonyX.Core;

/// <summary>ずれ方。</summary>
public enum DialectShiftKind
{
    /// <summary>動詞の種別が変わる（守る → 囲う）。</summary>
    TypeChange,

    /// <summary>名詞が隣の記号へずれる（川 → 土地）。</summary>
    Neighbor,

    /// <summary>良い・悪いの反転（崇拝 ↔ 破壊）。</summary>
    Reversal,
}

/// <summary>語義のずれ表の 1 行: 神の言葉 → 予言者に聞こえる意味（概念の ID）。</summary>
public sealed record DialectShift(string FromId, string ToId, DialectShiftKind Kind);

/// <summary>
/// 語義のずれ表（docs/domain/oracle-pair.md）。星のシードと器の段から、予言者の言葉ごとに作る。
/// 作った後は変えない。セーブには表そのものを残し、読み込みでシードから作り直さない。
/// </summary>
public sealed class DialectTable
{
    // 表を作る乱数は、住人・建物・デッキの乱数と分ける
    private const int SeedSalt = 0x4D1A1EC7;

    private readonly Dictionary<string, DialectShift> _shifts;

    private DialectTable(string vesselId, IEnumerable<DialectShift> shifts)
    {
        VesselId = vesselId;
        _shifts = shifts.ToDictionary(shift => shift.FromId);
    }

    public string VesselId { get; }

    /// <summary>ずれ表の行。</summary>
    public IReadOnlyCollection<DialectShift> Shifts => _shifts.Values;

    public DialectShift? ShiftOf(string wordId) => _shifts.GetValueOrDefault(wordId);

    /// <summary>シードと器の段から表を作る。同じ入力なら同じ表。</summary>
    /// <exception cref="ArgumentException">器の段が定義に無い。</exception>
    public static DialectTable Create(DialectRules rules, string vesselId, int seed)
    {
        ArgumentNullException.ThrowIfNull(rules);
        var vessel = rules.Vessels.FirstOrDefault(candidate => candidate.Id == vesselId)
            ?? throw new ArgumentException($"定義に無い器の段: vesselId={vesselId}", nameof(vesselId));
        var random = new Random(unchecked(seed + SeedSalt));
        var shifts = new List<DialectShift>();
        foreach (var verb in rules.Verbs)
        {
            if (random.NextDouble() >= vessel.MisheardRatio)
            {
                continue;
            }
            var reversal = verb.Type != DogmaType.Taboo && random.NextDouble() < rules.ReversalProbability;
            var candidates = rules.Verbs
                .Where(other => reversal ? other.Type == OppositeOf(verb.Type) : other.Type != verb.Type)
                .ToList();
            if (candidates.Count > 0)
            {
                var kind = reversal ? DialectShiftKind.Reversal : DialectShiftKind.TypeChange;
                shifts.Add(new DialectShift(verb.Id, candidates[random.Next(candidates.Count)].Id, kind));
            }
        }
        foreach (var noun in rules.Nouns)
        {
            if (random.NextDouble() >= vessel.MisheardRatio)
            {
                continue;
            }
            var neighbors = rules.NeighborsOf(noun.SymbolId).ToHashSet();
            var candidates = rules.Nouns.Where(other => neighbors.Contains(other.SymbolId)).ToList();
            if (candidates.Count > 0)
            {
                shifts.Add(new DialectShift(noun.Id, candidates[random.Next(candidates.Count)].Id, DialectShiftKind.Neighbor));
            }
        }
        return new DialectTable(vessel.Id, shifts);
    }

    /// <summary>セーブから読み込んだ行で表を作る（シードから作り直さない）。</summary>
    /// <exception cref="ArgumentException">器の段・語の ID が定義に無い、ずれ方が語に合わない、同じ語の行が重なっている。</exception>
    public static DialectTable Restore(DialectRules rules, string vesselId, IEnumerable<DialectShift> shifts)
    {
        ArgumentNullException.ThrowIfNull(rules);
        ArgumentNullException.ThrowIfNull(shifts);
        if (rules.Vessels.All(vessel => vessel.Id != vesselId))
        {
            throw new ArgumentException($"定義に無い器の段: vesselId={vesselId}", nameof(vesselId));
        }
        var list = shifts.ToList();
        if (list.Select(shift => shift.FromId).Distinct().Count() != list.Count || list.Any(shift => !IsConsistent(rules, shift)))
        {
            throw new ArgumentException("ずれ表の行が定義に合わない", nameof(shifts));
        }
        return new DialectTable(vesselId, list);
    }

    private static bool IsConsistent(DialectRules rules, DialectShift shift)
    {
        if (shift.Kind == DialectShiftKind.Neighbor)
        {
            return rules.FindNoun(shift.FromId) is { } from && rules.FindNoun(shift.ToId) is { } to
                && rules.NeighborsOf(from.SymbolId).Contains(to.SymbolId);
        }
        if (rules.FindVerb(shift.FromId) is not { } fromVerb || rules.FindVerb(shift.ToId) is not { } toVerb)
        {
            return false;
        }
        return shift.Kind == DialectShiftKind.Reversal
            ? fromVerb.Type != DogmaType.Taboo && toVerb.Type == OppositeOf(fromVerb.Type)
            : toVerb.Type != fromVerb.Type;
    }

    private static DogmaType OppositeOf(DogmaType type) => type == DogmaType.Worship ? DogmaType.Destruction : DogmaType.Worship;
}
