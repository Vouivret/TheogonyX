namespace TheogonyX.Core;

/// <summary>
/// 住人の信仰の広がり（docs/domain/dogma-belief.md の「小さな MVP での仮の形」）。
/// 最初の信者は予言者の近く、人から人へは広場に集まった住人どうしとつがいでうつり、親から子へは弱めて継ぐ。
/// 乱数は「シード・年・地域」から作り、住人は ID の小さい順に見るので、同じ入力なら同じ結果になる。
/// </summary>
public static class BeliefSimulator
{
    // 建物・住人の乱数と同じ並びにならないよう、シードに別の値を混ぜる
    private const uint BeliefSalt = 0x42454C49; // "BELI"

    /// <summary>
    /// 住人 1 人が教えを信じる。同じ教えなら強いほうの強さ、矛盾する教え（同じ記号の別の種別）は弱いほうを捨て（同じ強さなら古いほうを残す）、
    /// 上限を越えたらいちばん弱い教えを捨てる（同じ強さなら新しいほうを捨てる）。
    /// </summary>
    public static Resident Believe(Resident person, Belief belief, int maxBeliefs)
    {
        ArgumentNullException.ThrowIfNull(person);
        ArgumentNullException.ThrowIfNull(belief);
        var beliefs = person.Beliefs.ToList();
        var same = beliefs.FindIndex(existing => existing.IsSameTeaching(belief));
        if (same >= 0)
        {
            if (belief.Strength > beliefs[same].Strength)
            {
                beliefs[same] = belief;
                return person with { Beliefs = beliefs };
            }
            return person;
        }
        var conflict = beliefs.FindIndex(existing => existing.SymbolId == belief.SymbolId);
        if (conflict >= 0)
        {
            if (belief.Strength <= beliefs[conflict].Strength)
            {
                return person;
            }
            beliefs.RemoveAt(conflict);
        }
        beliefs.Add(belief);
        if (beliefs.Count > maxBeliefs)
        {
            var weakest = beliefs.Min(existing => existing.Strength);
            beliefs.RemoveAt(beliefs.FindLastIndex(existing => existing.Strength == weakest));
        }
        return person with { Beliefs = beliefs };
    }

    /// <summary>神託を告げた。予言者の地域から <see cref="BeliefRules.SeedRegions"/> 地域以内の住人が、教えの強度で信じ始める。</summary>
    public static void Seed(Residents residents, Dogma dogma, int prophetRegion, BeliefRules rules)
    {
        ArgumentNullException.ThrowIfNull(residents);
        ArgumentNullException.ThrowIfNull(dogma);
        ArgumentNullException.ThrowIfNull(rules);
        var belief = new Belief(dogma.TargetSymbolId, dogma.Type, dogma.Intensity);
        var people = residents.People
            .Select(person => Math.Abs(person.Region - prophetRegion) <= rules.SeedRegions ? Believe(person, belief, rules.MaxBeliefs) : person)
            .ToList();
        residents.Replace(people, [.. residents.Graves], residents.NextResidentId);
    }

    /// <summary>
    /// 1 年ぶん、人から人へ広げる。その年に広場に集まった（<see cref="ResidentTask.Gather"/>）同じ地域の住人どうしと、つがいの相手に、
    /// 年の初めの信仰を元にうつす。うつる確率は、うつす人の強さ × うつりやすさ。うつった強さは、うつす人の強さ × <see cref="BeliefRules.Dampen"/>。
    /// </summary>
    public static void Spread(Residents residents, int year, int seed, BeliefRules rules)
    {
        ArgumentNullException.ThrowIfNull(residents);
        ArgumentNullException.ThrowIfNull(rules);
        var people = residents.People.ToList();
        if (people.All(person => person.Beliefs.Count == 0))
        {
            return;
        }
        var sources = people.ToDictionary(person => person.Id, person => person.Beliefs);
        var indexOf = people.Select((person, index) => (person.Id, index)).ToDictionary(pair => pair.Id, pair => pair.index);

        void Transmit(Random random, int targetId, int sourceId, double transmit)
        {
            foreach (var belief in sources[sourceId])
            {
                var target = people[indexOf[targetId]];
                if (target.Beliefs.Any(existing => existing.IsSameTeaching(belief)))
                {
                    continue;
                }
                if (random.NextDouble() < belief.Strength * transmit)
                {
                    people[indexOf[targetId]] = Believe(target, belief with { Strength = belief.Strength * rules.Dampen }, rules.MaxBeliefs);
                }
            }
        }

        foreach (var region in people.Select(person => person.Region).Distinct().Order())
        {
            var random = new Random(unchecked((int)CrowdHash.Hash((uint)seed ^ BeliefSalt, (uint)year, (uint)region)));
            var gatherers = people
                .Where(person => person.Region == region && person.Task == ResidentTask.Gather && person.Breakdown is null)
                .OrderBy(person => person.Id)
                .ToList();
            foreach (var target in gatherers)
            {
                foreach (var source in gatherers.Where(other => other.Id != target.Id))
                {
                    Transmit(random, target.Id, source.Id, rules.GatherTransmit);
                }
            }
            foreach (var person in people.Where(person => person.Region == region && person.PartnerId is { } partnerId && person.Id < partnerId).OrderBy(person => person.Id))
            {
                var partnerId = person.PartnerId!.Value;
                Transmit(random, partnerId, person.Id, rules.PartnerTransmit);
                Transmit(random, person.Id, partnerId, rules.PartnerTransmit);
            }
        }
        residents.Replace(people, [.. residents.Graves], residents.NextResidentId);
    }

    /// <summary>生まれた子が、親の信仰を <see cref="BeliefRules.InheritDampen"/> で弱めて継ぐ（親の ID の小さい順）。</summary>
    public static Resident Inherit(Resident child, Resident parent, Resident otherParent, BeliefRules rules)
    {
        ArgumentNullException.ThrowIfNull(child);
        ArgumentNullException.ThrowIfNull(rules);
        foreach (var belief in parent.Beliefs.Concat(otherParent.Beliefs))
        {
            child = Believe(child, belief with { Strength = belief.Strength * rules.InheritDampen }, rules.MaxBeliefs);
        }
        return child;
    }

    /// <summary>教えを信じている住人の数。</summary>
    public static int BelieversOf(Dogma dogma, Residents residents)
    {
        ArgumentNullException.ThrowIfNull(dogma);
        ArgumentNullException.ThrowIfNull(residents);
        return residents.People.Count(person => person.Beliefs.Any(belief => belief.IsSameTeaching(dogma)));
    }

    /// <summary>住人のうちの信者の割合（0〜1）。住人がいなければ 0。教えの効き目に掛ける。</summary>
    public static double Share(Dogma dogma, Residents residents)
    {
        ArgumentNullException.ThrowIfNull(residents);
        return residents.People.Count == 0 ? 0 : (double)BelieversOf(dogma, residents) / residents.People.Count;
    }
}
