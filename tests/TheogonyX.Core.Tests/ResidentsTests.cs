namespace TheogonyX.Core.Tests;

/// <summary>住人の欲求と健康（docs/domain/agent-needs.md の受け入れ条件 AC-agent-needs-001〜020・022・023）。</summary>
public class ResidentsTests
{
    private const int Seed = 42;
    private const int Year = 100;

    // テスト用の地表は 16 列なので、地域は 8 個（列 2r・2r+1 が地域 r）
    private const int RegionA = 2;
    private const int RegionB = 5;

    private static readonly ResidentName AnyName = new("NAME_PREFIX_ART", "NAME_SUFFIX_ORIUS", "NAME_PREFIX_ILIO", "NAME_SUFFIX_A");

    private static Resident Person(
        int id,
        int region,
        int age = 30,
        double hunger = 0.1,
        double shelter = 0.1,
        double social = 0,
        double esteem = 0,
        double actualization = 0,
        double health = 100,
        double sanity = 100,
        BreakdownKind? breakdown = null,
        int breakdownYear = 0,
        ResidentEpithet? epithet = null) =>
        new(id, region, Year - age, hunger, shelter, social, esteem, actualization, health, sanity, breakdown, breakdownYear,
            ResidentTask.Idle, AnyName, epithet);

    // Year の 1 つ前の年の世界。Advance で 1 年進めて Year にする
    private static WorldState WorldBefore(float forest = 50f, float water = 50f, float chaos = 0f) => new()
    {
        Year = Year - 1,
        ForestAmount = forest,
        WaterAmount = water,
        Temperature = 15f,
        ChaosLevel = chaos,
        HarmonyLevel = 50f,
    };

    private static Building Workshop(int id, int region, int row = 0) => new(id, BuildingType.Workshop, region, region * 2, row, 10_000);

    private static Building Shrine(int id, int region, int row = 0) => new(id, BuildingType.Shrine, region, region * 2, row, 10_000);

    private static Building House(int id, int region, int row = 0) => new(id, BuildingType.HouseHut, region, region * 2, row, 10_000);

    private static Civilization CivilizationWith(params Building[] buildings) =>
        new(
            Enumerable.Repeat(new RegionCivilization(0, 0, 0, 0), TestRules.Civilization.Regions.Count),
            buildings, [], [], buildings.Length == 0 ? 0 : buildings.Max(building => building.Id) + 1);

    // 世界を 1 年進めてから（環境の値は変えない）、住人の 1 年を計算する
    private static IReadOnlyList<ResidentEvent> Advance(
        Residents residents, Civilization civilization, WorldState world, IReadOnlyList<Dogma>? dogmas = null, NeedsRules? rules = null)
    {
        world.Year++;
        return ResidentSimulator.AdvanceYear(
            residents, civilization, world, dogmas ?? [], rules ?? TestRules.Needs, TestRules.Civilization, TestRules.World, TestRules.Terrain, Seed);
    }

    private static Residents With(params Resident[] people) =>
        new(people, [], people.Length == 0 ? 0 : people.Max(person => person.Id) + 1);

    private static Resident Find(Residents residents, int id) => residents.People.Single(person => person.Id == id);

    [Fact]
    [Trait("TS", "TS-agent-needs-001")]
    public void AdvanceYear_住人も建物も無い_年1と年2に地域に1人ずつ移り住む()
    {
        var residents = Residents.CreateInitial();
        var civilization = CivilizationWith();
        var world = WorldBefore();
        world.Year = 0;

        world.Year++;
        ResidentSimulator.AdvanceYear(residents, civilization, world, [], TestRules.Needs, TestRules.Civilization, TestRules.World, TestRules.Terrain, Seed);

        // 地域の番号の順に ID 0〜7（テスト用の地表は 8 地域）
        Assert.Equal(Enumerable.Range(0, 8), residents.People.Select(person => person.Id));
        Assert.Equal(Enumerable.Range(0, 8), residents.People.Select(person => person.Region));
        var first = residents.People[0];
        Assert.Equal(20, first.AgeIn(1));
        Assert.Equal(100, first.Health);
        Assert.Equal(100, first.Sanity);
        Assert.Null(first.Breakdown);
        Assert.Equal(0.2, first.Social, 6);
        Assert.Equal(0.1, first.Esteem, 6);
        Assert.Equal(0.05, first.Actualization, 6);

        world.Year++;
        ResidentSimulator.AdvanceYear(residents, civilization, world, [], TestRules.Needs, TestRules.Civilization, TestRules.World, TestRules.Terrain, Seed);
        Assert.Equal(16, residents.People.Count);
        Assert.Equal(Enumerable.Range(8, 8), residents.People.Skip(8).Select(person => person.Id));
    }

    [Fact]
    [Trait("TS", "TS-agent-needs-001")]
    public void AdvanceYear_家1軒の地域に6人と5人_6人の地域は増えず5人の地域に1人移り住む()
    {
        var residents = With([.. Enumerable.Range(0, 6).Select(id => Person(id, RegionA)), .. Enumerable.Range(6, 5).Select(id => Person(id, RegionB))]);

        Advance(residents, CivilizationWith(House(0, RegionA), House(1, RegionB)), WorldBefore());

        Assert.Equal(6, residents.People.Count(person => person.Region == RegionA));
        Assert.Equal(6, residents.People.Count(person => person.Region == RegionB));
    }

    [Fact]
    [Trait("TS", "TS-agent-needs-002")]
    public void AdvanceYear_飢饉の閾値30と自閉_空腹は09か01か10()
    {
        double HungerAfter(float forest, float water, BreakdownKind? breakdown = null)
        {
            var residents = With(Person(0, RegionA, breakdown: breakdown, breakdownYear: Year - 1));
            Advance(residents, CivilizationWith(House(0, RegionA)), WorldBefore(forest, water));
            return Find(residents, 0).Hunger;
        }

        Assert.Equal(0.9, HungerAfter(30f, 80f));
        Assert.Equal(0.9, HungerAfter(80f, 30f));
        Assert.Equal(0.1, HungerAfter(31f, 31f));
        Assert.Equal(1.0, HungerAfter(50f, 50f, BreakdownKind.Catatonia));
    }

    [Fact]
    [Trait("TS", "TS-agent-needs-002")]
    public void AdvanceYear_移り住んで4人と5人と家の無い2人_住居は01と08と08()
    {
        var residents = With(
        [
            .. Enumerable.Range(0, 3).Select(id => Person(id, RegionA)),
            .. Enumerable.Range(3, 4).Select(id => Person(id, RegionB)),
            Person(7, 0),
        ]);

        Advance(residents, CivilizationWith(House(0, RegionA), House(1, RegionB), Workshop(2, 0)), WorldBefore());

        Assert.Equal(4, residents.People.Count(person => person.Region == RegionA));
        Assert.Equal(5, residents.People.Count(person => person.Region == RegionB));
        Assert.Equal(2, residents.People.Count(person => person.Region == 0));
        Assert.Equal(0.1, Find(residents, 0).Shelter);
        Assert.Equal(0.8, Find(residents, 3).Shelter);
        Assert.Equal(0.8, Find(residents, 7).Shelter);
    }

    [Fact]
    [Trait("TS", "TS-agent-needs-003")]
    public void AdvanceYear_祠の地域で交流05承認055自己実現045_年1は集まり年2は祈る()
    {
        var residents = With(Person(0, RegionA, social: 0.5, esteem: 0.55, actualization: 0.45));
        var civilization = CivilizationWith(Shrine(0, RegionA), House(1, RegionA, row: 2));
        var world = WorldBefore();

        Advance(residents, civilization, world);
        var year1 = Find(residents, 0);
        Assert.Equal(ResidentTask.Gather, year1.Task);
        Assert.Equal(0.2, year1.Social, 6);
        Assert.Equal(0.65, year1.Esteem, 6);
        Assert.Equal(0.5, year1.Actualization, 6);

        Advance(residents, civilization, world);
        var year2 = Find(residents, 0);
        Assert.Equal(ResidentTask.Pray, year2.Task);
        Assert.Equal(0.4, year2.Social, 6);
        Assert.Equal(0.25, year2.Esteem, 6);
        Assert.Equal(0.55, year2.Actualization, 6);
    }

    [Fact]
    [Trait("TS", "TS-agent-needs-003")]
    public void AdvanceYear_交流と承認が06で並ぶ_下の段階を選び祠の無い地域の承認は選べない()
    {
        var residents = With(Person(0, RegionA, social: 0.4, esteem: 0.5), Person(1, RegionB, esteem: 0.9));

        Advance(residents, CivilizationWith(Shrine(0, RegionA), House(1, RegionA, row: 2), Workshop(2, RegionB), House(3, RegionB, row: 2)), WorldBefore());

        Assert.Equal(ResidentTask.Gather, Find(residents, 0).Task);
        var withoutShrine = Find(residents, 1);
        Assert.Equal(1.0, withoutShrine.Esteem, 6);
        Assert.Equal(ResidentTask.Idle, withoutShrine.Task);
    }

    [Fact]
    [Trait("TS", "TS-agent-needs-003")]
    public void AdvanceYear_空腹か住居が06以上_交流が高くても食料か屋根を探し交流は減らない()
    {
        (ResidentTask Task, double Social) After(float forest, bool house)
        {
            var residents = With(Person(0, RegionA, social: 0.9));
            Advance(residents, house ? CivilizationWith(House(0, RegionA)) : CivilizationWith(Shrine(0, RegionA)), WorldBefore(forest));
            var person = Find(residents, 0);
            return (person.Task, person.Social);
        }

        Assert.Equal((ResidentTask.SeekFood, 1.0), After(20f, house: true));
        Assert.Equal((ResidentTask.SeekShelter, 1.0), After(50f, house: false));
        Assert.Equal((ResidentTask.SeekFood, 1.0), After(20f, house: false));
    }

    [Fact]
    [Trait("TS", "TS-agent-needs-012")]
    public void AdvanceYear_家の無い地域で屋根を探す_家が1軒建ち次の年は住居が足りて建たない()
    {
        var residents = With([Person(0, RegionA), .. Enumerable.Range(1, 3).Select(id => Person(id, RegionB))]);
        var civilization = CivilizationWith(House(0, RegionB));
        var world = WorldBefore();

        Advance(residents, civilization, world);

        Assert.All(residents.People.Where(person => person.Region == RegionA), person => Assert.Equal(ResidentTask.SeekShelter, person.Task));
        Assert.Equal(2, residents.People.Count(person => person.Region == RegionA));
        var built = Assert.Single(civilization.Buildings, building => building.Region == RegionA);
        Assert.Equal(BuildingType.HouseHut, built.Type);
        Assert.InRange(built.CollapseYear, Year + 200, Year + 400);
        Assert.Single(civilization.Buildings, building => building.Region == RegionB);
        Assert.All(residents.People.Where(person => person.Region == RegionB), person => Assert.Equal(0.1, person.Shelter));

        Advance(residents, civilization, world);

        Assert.Equal(3, residents.People.Count(person => person.Region == RegionA));
        Assert.All(residents.People.Where(person => person.Region == RegionA), person => Assert.Equal(0.1, person.Shelter));
        Assert.Single(civilization.Buildings, building => building.Region == RegionA);
    }

    [Fact]
    [Trait("TS", "TS-agent-needs-012")]
    public void AdvanceYear_技術の段階0から4の地域で屋根を探す_小屋と木組みとれんがの家が建つ()
    {
        var residents = With([.. Enumerable.Range(0, 5).Select(region => Person(region, region))]);
        var regions = Enumerable.Range(0, TestRules.Civilization.Regions.Count)
            .Select(region => new RegionCivilization(0, 0, Math.Min(region, 4), 0));
        var civilization = new Civilization(regions, [], [], [], 0);

        Advance(residents, civilization, WorldBefore());

        BuildingType HouseIn(int region) => Assert.Single(civilization.Buildings, building => building.Region == region).Type;
        Assert.Equal(
            [BuildingType.HouseHut, BuildingType.HouseTimber, BuildingType.HouseTimber, BuildingType.HouseBrick, BuildingType.HouseBrick],
            Enumerable.Range(0, 5).Select(HouseIn));
    }

    [Fact]
    [Trait("TS", "TS-agent-needs-004")]
    public void AdvanceYear_飢饉の後に平年_体力は減って戻り老いた住人は上限で止まる()
    {
        var residents = With(Person(0, RegionA, age: 30), Person(1, RegionA, age: 60, health: 90), Person(2, RegionA, breakdown: BreakdownKind.Catatonia, breakdownYear: Year));
        var civilization = CivilizationWith(House(0, RegionA));
        var world = WorldBefore(forest: 20f);

        Advance(residents, civilization, world);
        Assert.Equal(80, Find(residents, 0).Health);
        Assert.Equal(70, Find(residents, 1).Health);
        Assert.Equal(80, Find(residents, 2).Health);

        world.ForestAmount = 50f;
        Advance(residents, civilization, world);
        Assert.Equal(90, Find(residents, 0).Health);
        Assert.Equal(78, Find(residents, 1).Health); // 61 歳の上限
        Assert.Equal(60, Find(residents, 2).Health); // 自閉・絶望は平年でも減る
    }

    [Fact]
    [Trait("TS", "TS-agent-needs-004")]
    public void AdvanceYear_体力2の99歳が100歳になる_上限0で死に墓が残る()
    {
        var residents = With(Person(0, RegionA, age: 100, health: 2));

        var events = Advance(residents, CivilizationWith(House(0, RegionA)), WorldBefore());

        Assert.DoesNotContain(residents.People, person => person.Id == 0);
        Assert.Equal(new Grave(0, RegionA, Year, AnyName, null), Assert.Single(residents.Graves));
        Assert.Contains(events, residentEvent => residentEvent.Type == ResidentEventType.Died && residentEvent.Resident.Id == 0);
    }

    [Fact]
    [Trait("TS", "TS-agent-needs-005")]
    public void AdvanceYear_SAN値50でカオス度30_平年は52飢饉は47死があれば42上限は100()
    {
        double SanityAfter(float forest, bool death, double sanity = 50, float chaos = 30f)
        {
            Resident[] people = death ? [Person(0, RegionA, sanity: sanity), Person(1, RegionA, health: 2, age: 100)] : [Person(0, RegionA, sanity: sanity)];
            var residents = With(people);
            Advance(residents, CivilizationWith(House(0, RegionA)), WorldBefore(forest, chaos: chaos));
            return Find(residents, 0).Sanity;
        }

        Assert.Equal(52, SanityAfter(50f, death: false), 6);
        Assert.Equal(47, SanityAfter(20f, death: false), 6);
        Assert.Equal(42, SanityAfter(50f, death: true), 6);
        Assert.Equal(100, SanityAfter(50f, death: false, sanity: 99, chaos: 0f), 6);
    }

    [Fact]
    [Trait("TS", "TS-agent-needs-006")]
    public void AdvanceYear_SAN値3でカオス度50の飢饉_同じシードなら同じ種類の精神崩壊になる()
    {
        Resident After()
        {
            var residents = With(Person(0, RegionA, sanity: 3));
            Advance(residents, CivilizationWith(House(0, RegionA)), WorldBefore(forest: 20f, chaos: 50f));
            return Find(residents, 0);
        }

        var first = After();
        var second = After();

        Assert.Equal(0, first.Sanity);
        Assert.NotNull(first.Breakdown);
        Assert.Equal(Year, first.BreakdownYear);
        Assert.Equal(first.Breakdown, second.Breakdown);
    }

    [Fact]
    [Trait("TS", "TS-agent-needs-006")]
    public void AdvanceYear_2人が狂乱_カオス度は10上がり建物のある地域の1つが廃墟になり次の年に建て直される()
    {
        // 移り住んだ住人が家を建てて廃墟の枠を使わないよう、移り住ませない
        var rampageOnly = TestRules.Needs with { RampageProbability = 1.0, BaseCapacity = 0 };
        var residents = With(Person(0, RegionA, sanity: 3), Person(1, RegionB, sanity: 3));
        var regions = Enumerable.Repeat(new RegionCivilization(0, 0, 0, 0), TestRules.Civilization.Regions.Count).ToArray();
        regions[RegionA] = new RegionCivilization(0.45, 0, 2, 0);
        var civilization = new Civilization(regions, [Workshop(0, RegionA, row: 0), Workshop(1, RegionA, row: 2)], [], [], 2);
        var world = WorldBefore(forest: 20f, chaos: 50f);

        Advance(residents, civilization, world, rules: rampageOnly);

        Assert.Equal(60f, world.ChaosLevel);
        var ruin = Assert.Single(civilization.Ruins);
        Assert.Equal(Year, ruin.RuinedYear);
        Assert.Equal(RegionA, ruin.Region);
        Assert.Single(civilization.Buildings);
        foreach (var person in residents.People.Where(person => person.Id is 0 or 1))
        {
            Assert.Equal(BreakdownKind.Rampage, person.Breakdown);
            Assert.Equal(ResidentTask.Breakdown, person.Task);
            Assert.Equal(new ResidentEpithet(Epithets.Bloodless), person.Epithet);
        }

        world.Year++;
        CivilizationSimulator.AdvanceYear(civilization, world, [], TestRules.Terrain, TestRules.Civilization, Seed);
        Assert.Equal(2, civilization.Buildings.Count);
        Assert.Contains(civilization.Buildings, building => building.Column == ruin.Column && building.Row == ruin.Row);
    }

    [Fact]
    [Trait("TS", "TS-agent-needs-006")]
    public void AdvanceYear_年10に崩壊した住人_年11と12は崩壊のままで年13にSAN値30で戻る()
    {
        var residents = With(Person(0, RegionA, sanity: 0, breakdown: BreakdownKind.Rampage, breakdownYear: Year));
        var civilization = CivilizationWith(House(0, RegionA));
        var world = WorldBefore(chaos: 50f);
        world.Year = Year;

        Advance(residents, civilization, world);
        Advance(residents, civilization, world);
        Assert.Equal(BreakdownKind.Rampage, Find(residents, 0).Breakdown);
        Assert.Equal(Year, Find(residents, 0).BreakdownYear);
        Assert.Equal(50f, world.ChaosLevel); // 狂乱でカオス度が上がるのは崩壊した年だけ
        Assert.Single(civilization.Buildings, building => building.Region == RegionA);

        Advance(residents, civilization, world);
        var recovered = Find(residents, 0);
        Assert.Null(recovered.Breakdown);
        Assert.Equal(30, recovered.Sanity);
    }

    [Fact]
    [Trait("TS", "TS-agent-needs-006")]
    public void AdvanceYear_崩壊中で体力20の自閉_平年でも体力0で死ぬ()
    {
        var residents = With(Person(0, RegionA, health: 20, breakdown: BreakdownKind.Catatonia, breakdownYear: Year - 1));

        Advance(residents, CivilizationWith(House(0, RegionA)), WorldBefore());

        Assert.DoesNotContain(residents.People, person => person.Id == 0);
        Assert.Single(residents.Graves);
    }

    [Theory]
    [Trait("TS", "TS-agent-needs-007")]
    [InlineData(MentalState.Normal, MentalTrigger.SanityZero, MentalState.Breakdown)]
    [InlineData(MentalState.Normal, MentalTrigger.HealthZero, MentalState.Dead)]
    [InlineData(MentalState.Breakdown, MentalTrigger.BreakdownElapsed, MentalState.Normal)]
    [InlineData(MentalState.Breakdown, MentalTrigger.HealthZero, MentalState.Dead)]
    public void Next_表で可の遷移_表のとおりの状態になる(MentalState state, MentalTrigger trigger, MentalState expected)
    {
        Assert.Equal(expected, MentalTransitions.Next(state, trigger));
    }

    [Theory]
    [Trait("TS", "TS-agent-needs-007")]
    [InlineData(MentalState.Normal, MentalTrigger.BreakdownElapsed)]
    [InlineData(MentalState.Breakdown, MentalTrigger.SanityZero)]
    [InlineData(MentalState.Dead, MentalTrigger.SanityZero)]
    [InlineData(MentalState.Dead, MentalTrigger.BreakdownElapsed)]
    [InlineData(MentalState.Dead, MentalTrigger.HealthZero)]
    public void Next_表で不可の遷移_エラーになる(MentalState state, MentalTrigger trigger)
    {
        Assert.Throws<InvalidOperationException>(() => MentalTransitions.Next(state, trigger));
    }

    [Fact]
    [Trait("TS", "TS-agent-needs-008")]
    public void AdvanceYear_年10に住人が死ぬ_墓は年19まで残り年20に消え同じ地域のSAN値は10減る()
    {
        // 受け入れ条件の年 10 を Year（100）に読み替える
        var residents = With(Person(0, RegionA, age: 100, health: 2), Person(1, RegionA));
        var civilization = CivilizationWith(House(0, RegionA));
        var world = WorldBefore();

        Advance(residents, civilization, world);
        Assert.Single(residents.Graves);
        Assert.Equal(90, Find(residents, 1).Sanity);

        while (world.Year < Year + 9)
        {
            Advance(residents, civilization, world);
        }
        Assert.Single(residents.Graves);

        Advance(residents, civilization, world);
        Assert.Empty(residents.Graves);
    }

    [Fact]
    [Trait("TS", "TS-agent-needs-009")]
    public void NameOf_同じシードと同じID_毎回同じ名前で陸の人のパーツから選ぶ()
    {
        var names = Enumerable.Range(0, 50).Select(id => ResidentSimulator.NameOf(id, Seed, TestRules.Needs)).ToList();

        Assert.Equal(names, Enumerable.Range(0, 50).Select(id => ResidentSimulator.NameOf(id, Seed, TestRules.Needs)));
        Assert.All(names, name =>
        {
            Assert.Contains(name.FirstPrefix, TestRules.Needs.NamePrefixes);
            Assert.Contains(name.LineagePrefix, TestRules.Needs.NamePrefixes);
            Assert.Contains(name.FirstSuffix, TestRules.Needs.NameSuffixes);
            Assert.Contains(name.LineageSuffix, TestRules.Needs.NameSuffixes);
        });
        Assert.True(names.Distinct().Count() > 1);
    }

    [Fact]
    [Trait("TS", "TS-agent-needs-009")]
    public void AdvanceYear_21歳になる年のドグマと81歳と狂乱_称号は表のとおりで最初のものを残す()
    {
        ResidentEpithet? WitnessAfter(params Dogma[] dogmas)
        {
            var residents = With(Person(0, RegionA, age: 21));
            Advance(residents, CivilizationWith(House(0, RegionA)), WorldBefore(), dogmas);
            return Find(residents, 0).Epithet;
        }

        Assert.Equal(
            new ResidentEpithet(Epithets.Witness, "SYM_TREE"),
            WitnessAfter(new Dogma("SYM_RAIN", DogmaType.Worship, 0.8f), new Dogma("SYM_TREE", DogmaType.Worship, 0.9f)));
        Assert.Null(WitnessAfter(new Dogma("SYM_RAIN", DogmaType.Worship, 0.79f)));
        Assert.Equal(
            new ResidentEpithet(Epithets.Witness, "SYM_RAIN"),
            WitnessAfter(new Dogma("SYM_RAIN", DogmaType.Worship, 0.9f), new Dogma("SYM_TREE", DogmaType.Worship, 0.9f)));
        Assert.Null(WitnessAfter());

        var residents = With(
            Person(0, RegionA, age: 81),
            Person(1, RegionA, sanity: 3, epithet: new ResidentEpithet(Epithets.Witness, "SYM_RAIN")));
        Advance(residents, CivilizationWith(House(0, RegionA)), WorldBefore(forest: 20f, chaos: 50f), rules: TestRules.Needs with { RampageProbability = 1.0 });
        Assert.Equal(new ResidentEpithet(Epithets.Elder), Find(residents, 0).Epithet);
        Assert.Equal(BreakdownKind.Rampage, Find(residents, 1).Breakdown);
        Assert.Equal(new ResidentEpithet(Epithets.Witness, "SYM_RAIN"), Find(residents, 1).Epithet);
    }

    [Fact]
    [Trait("TS", "TS-agent-needs-010")]
    public void TimeControl_同じシードで300年と再創生と300年_住人と墓と建物とカオス度が一致する()
    {
        (TimeControl Time, List<ResidentEvent> Events) Run()
        {
            var time = new TimeControl(TestRules.World, TestRules.JumpYears, new CivilizationSetup(TestRules.Civilization, TestRules.Terrain, Seed, TestRules.Needs));
            var events = new List<ResidentEvent>();
            void Jumps()
            {
                // 森を壊すドグマで、100 年余りで飢饉になり、カオス度も少しずつ上がる
                time.AddDogma(new Dogma("SYM_TREE", DogmaType.Destruction, 0.3f));
                for (var i = 0; i < 3 && time.CanDo(TimeControlAction.Jump); i++)
                {
                    time.Jump();
                    events.AddRange(time.ResidentEvents);
                }
            }
            Jumps();
            time.Reset();
            Jumps();
            return (time, events);
        }

        var (first, firstEvents) = Run();
        var (second, _) = Run();

        Assert.Contains(firstEvents, residentEvent => residentEvent.Type == ResidentEventType.Died);
        Assert.Contains(firstEvents, residentEvent => residentEvent.Type == ResidentEventType.BrokeDown);
        Assert.Equal(first.Residents!.People, second.Residents!.People);
        Assert.Equal(first.Residents.Graves, second.Residents.Graves);
        Assert.Equal(first.Residents.NextResidentId, second.Residents.NextResidentId);
        Assert.Equal(first.Civilization!.Buildings, second.Civilization!.Buildings);
        Assert.Equal(first.World.ChaosLevel, second.World.ChaosLevel);
    }

    [Fact]
    [Trait("TS", "TS-agent-needs-010")]
    public void TimeControl_崩壊する年と世界再創生_崩壊した年は変わらず再創生で消えてIDは続く()
    {
        var time = new TimeControl(TestRules.World, TestRules.JumpYears, new CivilizationSetup(TestRules.Civilization, TestRules.Terrain, Seed, TestRules.Needs));
        var world = WorldBefore(chaos: 99.5f);
        var residents = new Residents([Person(0, RegionA)], [new Grave(3, RegionA, Year - 2, AnyName, null)], 4);
        time.Restore(world, [new Dogma("SYM_BLOOD", DogmaType.Worship, 1f)], CivilizationWith(), residents);

        time.Jump();

        Assert.Equal(TimeControlState.Destroyed, time.State);
        Assert.Equal(residents.People, time.Residents!.People);
        Assert.Equal(residents.Graves, time.Residents.Graves);

        time.Reset();
        Assert.Empty(time.Residents!.People);
        Assert.Empty(time.Residents.Graves);
        Assert.Equal(4, time.Residents.NextResidentId);

        time.StartAutoPlay();
        time.AdvanceOneYear();
        Assert.Equal(4, time.Residents.People.Min(person => person.Id));
    }
}
