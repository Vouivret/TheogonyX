namespace TheogonyX.Core;

/// <summary>住人に起きた出来事の種類。ログに出すのに使う。</summary>
public enum ResidentEventType
{
    Arrived,
    Died,
    BrokeDown,
    Born,
    Paired,
}

/// <summary>住人に起きた出来事。<paramref name="Resident"/> は出来事が起きたときの姿。</summary>
/// <param name="Partner">つがいになったときの相手。ほかの出来事では null。</param>
public sealed record ResidentEvent(ResidentEventType Type, int Year, Resident Resident, Resident? Partner = null);

/// <summary>
/// 住人の欲求・体力・SAN 値・精神崩壊・死・称号・タスクと、住人が建てる家、子供とつがいを 1 年ぶん計算する（docs/domain/agent-needs.md）。
/// </summary>
public static class ResidentSimulator
{
    // 建物の乱数（シード・年・地域）と同じ並びにならないよう、住人の乱数と名前の乱数はシードに別の値を混ぜる
    private const uint YearSalt = 0x52455349; // "RESI"
    private const uint NameSalt = 0x4E414D45; // "NAME"

    /// <summary>
    /// 1 年進める。<paramref name="residents"/>・<paramref name="civilization"/>（狂乱で壊れる建物と、住人が建てる家）・<paramref name="world"/>（狂乱で上がるカオス度）を書き換える。
    /// 文明の計算の後に呼ぶ。星が崩壊した年には呼ばない。
    /// </summary>
    /// <returns>その年に起きた出来事（移り住む・死・精神崩壊・誕生・つがい）。</returns>
    public static IReadOnlyList<ResidentEvent> AdvanceYear(
        Residents residents,
        Civilization civilization,
        WorldState world,
        IReadOnlyList<Dogma> dogmas,
        NeedsRules rules,
        CivilizationRules civilizationRules,
        WorldRules worldRules,
        TerrainRules terrain,
        int seed)
    {
        ArgumentNullException.ThrowIfNull(residents);
        ArgumentNullException.ThrowIfNull(civilization);
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(dogmas);
        ArgumentNullException.ThrowIfNull(rules);
        ArgumentNullException.ThrowIfNull(civilizationRules);
        ArgumentNullException.ThrowIfNull(worldRules);
        ArgumentNullException.ThrowIfNull(terrain);

        var year = world.Year;
        var regionCount = civilizationRules.Regions.Count;
        var events = new List<ResidentEvent>();
        var people = residents.People.ToList();
        var graves = residents.Graves.ToList();
        var nextId = residents.NextResidentId;

        var randoms = new Random?[regionCount];
        Random Rng(int region) =>
            randoms[region] ??= new Random(unchecked((int)CrowdHash.Hash((uint)seed ^ YearSalt, (uint)year, (uint)region)));
        int HousesIn(int region) => civilization.Buildings.Count(building => building.Region == region && building.Type.IsHouse());
        int PeopleIn(int region) => people.Count(person => person.Region == region);
        bool IsAdult(Resident person) => person.AgeIn(year) >= rules.AdultAge;

        // 1. 移り住む（正常な大人が少ない地域に 1 人）
        for (var region = 0; region < regionCount; region++)
        {
            if (people.Count(person => person.Region == region && IsAdult(person) && person.Breakdown is null) < rules.ImmigrateBelowAdults)
            {
                var arrived = new Resident(
                    nextId, region, year - rules.ImmigrantAge, 0, 0, 0, 0, 0, rules.MaxHealth, rules.MaxSanity, null, 0,
                    ResidentTask.Idle, NameOf(nextId, seed, rules), null);
                people.Add(arrived);
                events.Add(new ResidentEvent(ResidentEventType.Arrived, year, arrived));
                nextId++;
            }
        }

        // 2. 欲求を動かす
        var famine = world.ForestAmount <= rules.FamineAtOrBelow || world.WaterAmount <= rules.FamineAtOrBelow;
        var populations = Enumerable.Range(0, regionCount).Select(PeopleIn).ToArray();
        var houses = Enumerable.Range(0, regionCount).Select(HousesIn).ToArray();
        for (var i = 0; i < people.Count; i++)
        {
            var person = people[i];
            var region = person.Region;
            var housed = houses[region] > 0 && populations[region] <= houses[region] * rules.CapacityPerHouse;
            people[i] = person with
            {
                Hunger = person.Breakdown == BreakdownKind.Catatonia ? rules.HungerCatatonia : famine ? rules.HungerFamine : rules.HungerNormal,
                Shelter = housed ? rules.ShelterEnough : rules.ShelterLacking,
                Social = Math.Min(1, person.Social + rules.SocialPerYear),
                Esteem = Math.Min(1, person.Esteem + rules.EsteemPerYear),
                Actualization = Math.Min(1, person.Actualization + rules.ActualizationPerYear),
            };
        }

        // 3. 体力・SAN 値を動かし、体力 0 の住人を死なせる
        var deaths = new int[regionCount];
        for (var i = 0; i < people.Count; i++)
        {
            var person = people[i];
            var starving = famine || person.Breakdown == BreakdownKind.Catatonia;
            var health = Math.Clamp(person.Health + (starving ? -rules.HealthLoss : rules.HealthRecovery), 0, MaxHealthAt(person.AgeIn(year), rules));
            // 住居は問わない（建物の無い時代に SAN 値が戻らず、全員が同じ年に崩れるのを避ける）
            var sanity = person.Sanity - world.ChaosLevel / rules.SanityChaosDivisor
                + (person.Hunger < rules.LowerNeedsBelow ? rules.SanityRecovery : 0);
            people[i] = person with { Health = health, Sanity = Math.Clamp(sanity, 0, rules.MaxSanity) };
        }
        // パートナーを亡くした住人は独身に戻り、その年のうちは組み直さない
        var widowed = new HashSet<int>();
        foreach (var dead in people.Where(person => person.Health <= 0).ToList())
        {
            MentalTransitions.Next(MentalTransitions.StateOf(dead), MentalTrigger.HealthZero);
            people.Remove(dead);
            var partner = people.FindIndex(person => person.Id == dead.PartnerId);
            if (partner >= 0)
            {
                people[partner] = people[partner] with { PartnerId = null };
                widowed.Add(people[partner].Id);
            }
            graves.Add(new Grave(dead.Id, dead.Region, year, dead.Name, dead.Epithet));
            events.Add(new ResidentEvent(ResidentEventType.Died, year, dead));
            deaths[dead.Region]++;
        }
        for (var i = 0; i < people.Count; i++)
        {
            var person = people[i];
            if (deaths[person.Region] > 0)
            {
                people[i] = person with { Sanity = Math.Max(0, person.Sanity - deaths[person.Region] * rules.SanityLossPerDeath) };
            }
        }

        // 4. 崩壊が終わった住人を戻し、SAN 値 0 の住人を精神崩壊させる（地域の乱数は ID の小さい住人から順に使う）
        for (var i = 0; i < people.Count; i++)
        {
            var person = people[i];
            var state = MentalTransitions.StateOf(person);
            if (state == MentalState.Breakdown && year - person.BreakdownYear >= rules.BreakdownYears)
            {
                MentalTransitions.Next(state, MentalTrigger.BreakdownElapsed);
                people[i] = person with { Breakdown = null, Sanity = rules.RecoveredSanity };
            }
            else if (state == MentalState.Normal && person.Sanity <= 0)
            {
                MentalTransitions.Next(state, MentalTrigger.SanityZero);
                var kind = Rng(person.Region).NextDouble() < rules.RampageProbability ? BreakdownKind.Rampage : BreakdownKind.Catatonia;
                people[i] = person with { Breakdown = kind, BreakdownYear = year };
                events.Add(new ResidentEvent(ResidentEventType.BrokeDown, year, people[i]));
            }
        }

        // 5. 称号（最初に得たものを残す。同じ年に重なったら 血を恐れぬ者 → 証人 → 長老）
        var strongest = dogmas
            .Where(dogma => dogma.Intensity >= rules.WitnessIntensityAtOrAbove)
            .Aggregate((Dogma?)null, (best, dogma) => best is null || dogma.Intensity > best.Intensity ? dogma : best);
        for (var i = 0; i < people.Count; i++)
        {
            var person = people[i];
            if (person.Epithet is not null)
            {
                continue;
            }
            var age = person.AgeIn(year);
            var epithet = RampagedIn(person, year) ? new ResidentEpithet(Epithets.Bloodless)
                : age == rules.WitnessAge && strongest is not null ? new ResidentEpithet(Epithets.Witness, strongest.TargetSymbolId)
                : age == rules.ElderAge ? new ResidentEpithet(Epithets.Elder)
                : null;
            people[i] = person with { Epithet = epithet };
        }

        // 6. タスクを選び、狂乱の行動と欲求の減りを当てはめる
        for (var i = 0; i < people.Count; i++)
        {
            var person = people[i];
            if (person.Breakdown is not null)
            {
                if (RampagedIn(person, year))
                {
                    world.ChaosLevel = Math.Min(world.ChaosLevel + rules.RampageChaos, worldRules.Max.ChaosLevel);
                    var standing = civilization.Buildings.Where(building => building.Region == person.Region).ToList();
                    if (standing.Count > 0)
                    {
                        civilization.Collapse(standing[Rng(person.Region).Next(standing.Count)], world, dogmas, civilizationRules);
                    }
                }
                people[i] = person with { Task = ResidentTask.Breakdown };
                continue;
            }
            if (!IsAdult(person))
            {
                people[i] = person with { Task = ResidentTask.Idle };
                continue;
            }
            if (person.Hunger >= rules.LowerNeedsBelow || person.Shelter >= rules.LowerNeedsBelow)
            {
                people[i] = person with { Task = person.Hunger >= person.Shelter ? ResidentTask.SeekFood : ResidentTask.SeekShelter };
                continue;
            }

            // 下の段階から順に見て、より高いときだけ置き換える（同じ値なら下の段階が残る）
            var task = ResidentTask.Idle;
            var highest = double.NegativeInfinity;
            void Consider(ResidentTask candidate, double value, double threshold, bool available)
            {
                if (available && value >= threshold && value > highest)
                {
                    task = candidate;
                    highest = value;
                }
            }
            Consider(ResidentTask.Gather, person.Social, rules.SocialAtOrAbove, true);
            Consider(ResidentTask.Pray, person.Esteem, rules.EsteemAtOrAbove, HasBuilding(civilization, person.Region, BuildingType.Shrine));
            Consider(ResidentTask.Research, person.Actualization, rules.ActualizationAtOrAbove, HasBuilding(civilization, person.Region, BuildingType.Workshop));
            people[i] = task switch
            {
                ResidentTask.Gather => person with { Task = task, Social = Math.Max(0, person.Social - rules.TaskRelief) },
                ResidentTask.Pray => person with { Task = task, Esteem = Math.Max(0, person.Esteem - rules.TaskRelief) },
                ResidentTask.Research => person with { Task = task, Actualization = Math.Max(0, person.Actualization - rules.TaskRelief) },
                _ => person with { Task = task },
            };
        }

        // 7. 屋根を探す住人がいる地域に、家を 1 軒建てる（地域の番号の順。空いた枠が無ければ建たない）
        TileType[,]? tiles = null;
        foreach (var region in people.Where(person => person.Task == ResidentTask.SeekShelter).Select(person => person.Region).Distinct().Order())
        {
            tiles ??= TerrainClassifier.ClassifyTiles(world, terrain);
            var house = HouseFor(civilization.Regions[region].TechStage, rules);
            CivilizationSimulator.TryBuild(civilization, tiles, region, house, year, civilizationRules, () => Rng(region));
        }

        // 8. 子供が生まれる（地域ごとに、つがいを ID の小さい順に見る。人数が上限より少ないときだけ。上限はその年の初めの家の数で決める）
        for (var region = 0; region < regionCount; region++)
        {
            var couples = people
                .Where(person => person.Region == region && person.PartnerId is { } partnerId && person.Id < partnerId)
                .OrderBy(person => person.Id)
                .Select(person => (First: person, Second: people.Single(other => other.Id == person.PartnerId)))
                .ToList();
            foreach (var (first, second) in couples)
            {
                var fertile = first.Breakdown is null && second.Breakdown is null
                    && first.AgeIn(year) <= rules.FertileUntilAge && second.AgeIn(year) <= rules.FertileUntilAge;
                if (fertile
                    && PeopleIn(region) < rules.BaseCapacity + houses[region] * rules.CapacityPerHouse
                    && Rng(region).NextDouble() < rules.BirthProbability)
                {
                    var child = new Resident(
                        nextId, region, year, 0, 0, 0, 0, 0, rules.MaxHealth, rules.MaxSanity, null, 0,
                        ResidentTask.Idle, NameOfChild(nextId, seed, rules, first.Name, second.Name), null);
                    people.Add(child);
                    events.Add(new ResidentEvent(ResidentEventType.Born, year, child));
                    nextId++;
                }
            }
        }

        // 9. つがいを組む（地域ごとに、独身で正常な大人を ID の小さい順に 2 人ずつ）
        for (var region = 0; region < regionCount; region++)
        {
            var singles = people
                .Where(person => person.Region == region && person.PartnerId is null && person.Breakdown is null && IsAdult(person)
                    && !widowed.Contains(person.Id))
                .OrderBy(person => person.Id)
                .ToList();
            for (var k = 0; k + 1 < singles.Count; k += 2)
            {
                var (first, second) = (singles[k], singles[k + 1]);
                var firstIndex = people.FindIndex(person => person.Id == first.Id);
                var secondIndex = people.FindIndex(person => person.Id == second.Id);
                people[firstIndex] = first with { PartnerId = second.Id };
                people[secondIndex] = second with { PartnerId = first.Id };
                events.Add(new ResidentEvent(ResidentEventType.Paired, year, people[firstIndex], people[secondIndex]));
            }
        }

        // 10. 年数のたった墓を消す
        graves.RemoveAll(grave => grave.DeathYear + rules.GraveYears <= year);

        residents.Replace(people, graves, nextId);
        return events;
    }

    /// <summary>その年齢の体力の上限。<see cref="NeedsRules.AgingStartAge"/> から 1 年に <see cref="NeedsRules.AgingLossPerYear"/> ずつ下がり、0 で止まる。</summary>
    public static double MaxHealthAt(int age, NeedsRules rules)
    {
        ArgumentNullException.ThrowIfNull(rules);
        return Math.Max(0, rules.MaxHealth - Math.Max(0, age - rules.AgingStartAge) * rules.AgingLossPerYear);
    }

    /// <summary>地域の技術の段階で決まる、住人が建てる家の種類。</summary>
    public static BuildingType HouseFor(int techStage, NeedsRules rules)
    {
        ArgumentNullException.ThrowIfNull(rules);
        return techStage >= rules.BrickHouseFromTechStage ? BuildingType.HouseBrick
            : techStage >= rules.TimberHouseFromTechStage ? BuildingType.HouseTimber
            : BuildingType.HouseHut;
    }

    /// <summary>住人の名と家名。「シード × 住人の ID」から作った、シミュレーションとは別の乱数でパーツを選ぶ。</summary>
    public static ResidentName NameOf(int residentId, int seed, NeedsRules rules)
    {
        ArgumentNullException.ThrowIfNull(rules);
        var random = NameRandom(residentId, seed);
        return new ResidentName(Prefix(random, rules), Suffix(random, rules), Prefix(random, rules), Suffix(random, rules));
    }

    /// <summary>生まれた子供の名前。名は <see cref="NameOf"/> と同じ乱数で新しく作り、家名は同じ乱数でどちらかの親のものを選ぶ。</summary>
    public static ResidentName NameOfChild(int residentId, int seed, NeedsRules rules, ResidentName parent, ResidentName otherParent)
    {
        ArgumentNullException.ThrowIfNull(rules);
        ArgumentNullException.ThrowIfNull(parent);
        ArgumentNullException.ThrowIfNull(otherParent);
        var random = NameRandom(residentId, seed);
        var (prefix, suffix) = (Prefix(random, rules), Suffix(random, rules));
        var lineage = random.Next(2) == 0 ? parent : otherParent;
        return new ResidentName(prefix, suffix, lineage.LineagePrefix, lineage.LineageSuffix);
    }

    private static Random NameRandom(int residentId, int seed) =>
        new(unchecked((int)CrowdHash.Hash((uint)seed ^ NameSalt, (uint)residentId, 0)));

    private static string Prefix(Random random, NeedsRules rules) => rules.NamePrefixes[random.Next(rules.NamePrefixes.Count)];

    private static string Suffix(Random random, NeedsRules rules) => rules.NameSuffixes[random.Next(rules.NameSuffixes.Count)];

    private static bool RampagedIn(Resident person, int year) => person.Breakdown == BreakdownKind.Rampage && person.BreakdownYear == year;

    private static bool HasBuilding(Civilization civilization, int region, BuildingType type) =>
        civilization.Buildings.Any(building => building.Region == region && building.Type == type);
}
