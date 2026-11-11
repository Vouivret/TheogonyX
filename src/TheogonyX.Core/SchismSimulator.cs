namespace TheogonyX.Core;

/// <summary>
/// 教えの対立と追放の定義（docs/domain/script-decipher.md の「9. 危機で読みが変わる」）。値は仮置き（<c>TODO(AI_UNSURE)</c>）で、遊びながら決める。
/// </summary>
/// <param name="ChaosPerConflictRegion">対立のある地域 1 つにつき、1 年に上がるカオス度。</param>
/// <param name="ExileChaosAtOrAbove">カオス度がこの値以上なら、対立のある地域で追放が起きる。</param>
public sealed record SchismRules(float ChaosPerConflictRegion, float ExileChaosAtOrAbove)
{
    /// <exception cref="ArgumentException">範囲外の値。</exception>
    public void Validate()
    {
        if (!float.IsFinite(ChaosPerConflictRegion) || ChaosPerConflictRegion < 0f || !float.IsFinite(ExileChaosAtOrAbove) || ExileChaosAtOrAbove < 0f)
        {
            throw new ArgumentException($"対立と追放の定義の値が範囲外: {this}");
        }
    }
}

/// <summary>地域を追われた住人の群れ。</summary>
/// <param name="SymbolId">追われた側の教えの記号。</param>
/// <param name="Type">追われた側の教えの種別。</param>
/// <param name="ResidentIds">移った住人。</param>
public sealed record Exile(int Year, int From, int To, string SymbolId, DogmaType Type, IReadOnlyList<int> ResidentIds);

/// <summary>
/// 教えの対立と追放（docs/domain/script-decipher.md の 9）。乱数は使わない。
/// 地域の中に、同じ記号の崇拝と破壊を信じる人が両方いれば、その地域は対立している（読みは問わない）。
/// </summary>
public static class SchismSimulator
{
    /// <summary>
    /// 1 年分。対立のある地域の数だけカオス度を上げ（<paramref name="addChaos"/>）、カオス度が閾値以上なら、
    /// 対立のある地域ごとに、少ない側の教えだけを信じる住人を、東西の隣のうち住人の少ない地域（同じなら東）へ移す。
    /// 地域は東西に 1 列に並び、端はつながらない。隣が無ければ移さない。
    /// 1 つの地域で対立する記号が 2 つ以上あれば、記号の ID の順で最初のものだけを見る。
    /// 少ない側は信者の数で決め、同じなら教えの一覧で後に加わった方（碑の読み違いは後に加わる）、一覧に無ければ破壊の側。
    /// 移る人のつがいの相手も一緒に移り、残る人との好感度は消える。
    /// </summary>
    /// <param name="dogmas">教えの一覧（加わった順）。</param>
    /// <param name="addChaos">カオス度を上げる（上限は呼ぶ側が守る）。</param>
    /// <param name="chaosLevel">今のカオス度を返す。</param>
    /// <param name="closedRegions">修繕中の柵がある地域。追放の行き先にしない（行き先が無ければ移さない）。</param>
    /// <returns>この年の追放。</returns>
    public static IReadOnlyList<Exile> AdvanceYear(
        Residents residents, IReadOnlyList<Dogma> dogmas, int year, int regionCount, SchismRules rules, Action<float> addChaos, Func<float> chaosLevel,
        IReadOnlySet<int>? closedRegions = null) =>
        AdvanceYear(residents, dogmas, year, new RegionMap(regionCount, 1), rules, addChaos, chaosLevel, closedRegions);

    /// <summary>地域の並び（<paramref name="layout"/>）で追放を決める。隣は <see cref="RegionLayout.Neighbors"/>（東寄りを先に並べる）。</summary>
    public static IReadOnlyList<Exile> AdvanceYear(
        Residents residents, IReadOnlyList<Dogma> dogmas, int year, RegionLayout layout, SchismRules rules, Action<float> addChaos, Func<float> chaosLevel,
        IReadOnlySet<int>? closedRegions = null)
    {
        ArgumentNullException.ThrowIfNull(layout);
        var regionCount = layout.Count;
        ArgumentNullException.ThrowIfNull(residents);
        ArgumentNullException.ThrowIfNull(dogmas);
        ArgumentNullException.ThrowIfNull(rules);
        ArgumentNullException.ThrowIfNull(addChaos);
        ArgumentNullException.ThrowIfNull(chaosLevel);

        var conflicts = new List<(int Region, string SymbolId)>();
        for (var region = 0; region < regionCount; region++)
        {
            var beliefs = residents.People.Where(person => person.Region == region).SelectMany(person => person.Beliefs).ToList();
            var symbol = beliefs.Where(belief => belief.Type == DogmaType.Worship).Select(belief => belief.SymbolId)
                .Intersect(beliefs.Where(belief => belief.Type == DogmaType.Destruction).Select(belief => belief.SymbolId))
                .Order(StringComparer.Ordinal)
                .FirstOrDefault();
            if (symbol is not null)
            {
                conflicts.Add((region, symbol));
            }
        }
        if (conflicts.Count == 0)
        {
            return [];
        }
        addChaos(rules.ChaosPerConflictRegion * conflicts.Count);
        if (chaosLevel() < rules.ExileChaosAtOrAbove)
        {
            return [];
        }

        var exiles = new List<Exile>();
        foreach (var (region, symbolId) in conflicts)
        {
            var neighbors = layout.Neighbors(region).Reverse().Where(other => closedRegions?.Contains(other) != true).ToList();
            if (neighbors.Count == 0)
            {
                continue;
            }
            var locals = residents.People.Where(person => person.Region == region).ToList();
            bool Believes(Resident person, DogmaType type) => person.Beliefs.Any(belief => belief.SymbolId == symbolId && belief.Type == type);
            var worshippers = locals.Count(person => Believes(person, DogmaType.Worship));
            var destroyers = locals.Count(person => Believes(person, DogmaType.Destruction));
            var minority = worshippers != destroyers
                ? worshippers < destroyers ? DogmaType.Worship : DogmaType.Destruction
                : LastJoined(dogmas, symbolId);
            var majority = minority == DogmaType.Worship ? DogmaType.Destruction : DogmaType.Worship;
            var leaving = locals.Where(person => Believes(person, minority) && !Believes(person, majority)).Select(person => person.Id).ToHashSet();
            if (leaving.Count == 0)
            {
                continue;
            }
            // つがいの相手も一緒に移る（つがいは同じ地域に住む）。残る人との好感度は消える（好感度は同じ地域の人どうしで持つ）
            leaving.UnionWith(locals.Where(person => person.PartnerId is { } partner && leaving.Contains(partner)).Select(person => person.Id));
            residents.ReplaceAffinities(residents.Affinities
                .Where(pair => leaving.Contains(pair.Key.Low) == leaving.Contains(pair.Key.High))
                .ToDictionary());
            int PeopleIn(int other) => residents.People.Count(person => person.Region == other);
            var to = neighbors.OrderBy(PeopleIn).First(); // 東（番号の大きい方）を先に並べたので、同じ数なら東
            var moved = residents.People.Select(person => leaving.Contains(person.Id) ? person with { Region = to } : person).ToList();
            residents.Replace(moved, [.. residents.Graves], residents.NextResidentId);
            exiles.Add(new Exile(year, region, to, symbolId, minority, [.. leaving.Order()]));
        }
        return exiles;
    }

    // 崇拝と破壊のうち、教えの一覧で後に加わった方。どちらも一覧に無ければ破壊
    private static DogmaType LastJoined(IReadOnlyList<Dogma> dogmas, string symbolId)
    {
        int LastIndex(DogmaType type)
        {
            for (var i = dogmas.Count - 1; i >= 0; i--)
            {
                if (dogmas[i].TargetSymbolId == symbolId && dogmas[i].Type == type)
                {
                    return i;
                }
            }
            return -1;
        }
        var worship = LastIndex(DogmaType.Worship);
        return worship > LastIndex(DogmaType.Destruction) ? DogmaType.Worship : DogmaType.Destruction;
    }
}
