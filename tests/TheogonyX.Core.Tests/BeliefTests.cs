using TheogonyX.Core;

namespace TheogonyX.Core.Tests;

public class BeliefTests
{
    private const int Seed = 42;
    private const double Tolerance = 0.001;

    private static readonly ResidentName Name = new("NAME_PREFIX_ART", "NAME_SUFFIX_ORIUS", "NAME_PREFIX_ILIO", "NAME_SUFFIX_A");

    // 上限 3・予言者から 1 地域以内。広場とつがいでは確実にうつり、弱める倍率は 0.8
    private static readonly BeliefRules Rules = new(
        MaxBeliefs: 3, SeedRegions: 1, GatherTransmit: 1f, PartnerTransmit: 1f, Dampen: 0.8f, InheritDampen: 0.8f);

    private static Resident Person(int id, int region = 3, ResidentTask task = ResidentTask.Idle, int? partnerId = null, params Belief[] beliefs) =>
        new(id, region, 280, 0.1, 0.1, 0.4, 0.3, 0.2, 100, 100, null, 0, task, Name, null, partnerId)
        {
            Beliefs = new BeliefSet(beliefs),
        };

    private static Belief Worship(string symbolId, double strength) => new(symbolId, DogmaType.Worship, strength);

    private static Belief Destruction(string symbolId, double strength) => new(symbolId, DogmaType.Destruction, strength);

    [Fact]
    [Trait("TS", "TS-belief-001")]
    public void Believe_上限の数を信じている_新しい教えでいちばん弱い教えを捨てる()
    {
        var person = Person(1, beliefs: [Worship("SYM_TREE", 0.6), Worship("SYM_WATER", 0.3), Worship("SYM_FIRE", 0.5)]);

        var after = BeliefSimulator.Believe(person, Worship("SYM_EARTH", 0.4), Rules.MaxBeliefs);

        Assert.Equal(["SYM_TREE", "SYM_FIRE", "SYM_EARTH"], after.Beliefs.Select(belief => belief.SymbolId));
    }

    [Fact]
    [Trait("TS", "TS-belief-001")]
    public void Believe_上限の数を信じていて新しい教えがいちばん弱い_新しい教えを信じない()
    {
        var person = Person(1, beliefs: [Worship("SYM_TREE", 0.6), Worship("SYM_WATER", 0.3), Worship("SYM_FIRE", 0.5)]);

        var after = BeliefSimulator.Believe(person, Worship("SYM_EARTH", 0.2), Rules.MaxBeliefs);

        Assert.Equal(person.Beliefs, after.Beliefs);
    }

    [Fact]
    [Trait("TS", "TS-belief-002")]
    public void Believe_矛盾する教え_弱いほうを捨て強さが同じなら古いほうを残す()
    {
        var worshipTree = Person(1, beliefs: [Worship("SYM_TREE", 0.5)]);

        var stronger = BeliefSimulator.Believe(worshipTree, Destruction("SYM_TREE", 0.7), Rules.MaxBeliefs);
        var weaker = BeliefSimulator.Believe(worshipTree, Destruction("SYM_TREE", 0.3), Rules.MaxBeliefs);
        var equal = BeliefSimulator.Believe(worshipTree, Destruction("SYM_TREE", 0.5), Rules.MaxBeliefs);

        Assert.Equal([Destruction("SYM_TREE", 0.7)], stronger.Beliefs);
        Assert.Equal([Worship("SYM_TREE", 0.5)], weaker.Beliefs);
        Assert.Equal([Worship("SYM_TREE", 0.5)], equal.Beliefs);
    }

    [Fact]
    [Trait("TS", "TS-belief-003")]
    public void Seed_予言者の地域から1地域以内の住人だけが_教えの強度で信じ始める()
    {
        var residents = new Residents([Person(1, region: 3), Person(2, region: 4), Person(3, region: 5)], [], 4);

        BeliefSimulator.Seed(residents, new Dogma("SYM_WATER", DogmaType.Worship, 0.6f), prophetRegion: 3, Rules);

        Assert.Equal([Worship("SYM_WATER", 0.6f)], residents.People[0].Beliefs);
        Assert.Equal([Worship("SYM_WATER", 0.6f)], residents.People[1].Beliefs);
        Assert.Empty(residents.People[2].Beliefs);
    }

    [Fact]
    [Trait("TS", "TS-belief-004")]
    public void Spread_広場に集まった住人とつがい_信者から弱めてうつり_集まらなかった住人にはうつらない()
    {
        var believer = Person(1, task: ResidentTask.Gather, beliefs: [Worship("SYM_TREE", 1.0)]);
        var gatherer = Person(2, task: ResidentTask.Gather);
        var idle = Person(3);
        var farAway = Person(4, region: 6, task: ResidentTask.Gather);
        var partnerOfBeliever = Person(5, region: 6, partnerId: 6, task: ResidentTask.Idle, beliefs: [Worship("SYM_FIRE", 0.5)]);
        var partner = Person(6, region: 6, partnerId: 5, task: ResidentTask.Idle);
        var residents = new Residents([believer, gatherer, idle, farAway, partnerOfBeliever, partner], [], 7);

        BeliefSimulator.Spread(residents, year: 100, Seed, Rules);

        var byId = residents.People.ToDictionary(person => person.Id);
        Assert.Equal(0.8, byId[2].Beliefs.Single().Strength, Tolerance);
        Assert.Equal("SYM_TREE", byId[2].Beliefs.Single().SymbolId);
        Assert.Empty(byId[3].Beliefs);
        Assert.Empty(byId[4].Beliefs);
        Assert.Equal(0.4, byId[6].Beliefs.Single().Strength, Tolerance);
        Assert.Equal("SYM_FIRE", byId[6].Beliefs.Single().SymbolId);
    }

    [Fact]
    [Trait("TS", "TS-belief-005")]
    public void Inherit_親の信仰_弱めて継ぐ()
    {
        var parent = Person(1, beliefs: [Worship("SYM_TREE", 0.5)]);
        var otherParent = Person(2, beliefs: [Worship("SYM_TREE", 0.3), Destruction("SYM_FIRE", 0.5)]);

        var child = BeliefSimulator.Inherit(Person(3), parent, otherParent, Rules);

        Assert.Equal(["SYM_TREE", "SYM_FIRE"], child.Beliefs.Select(belief => belief.SymbolId));
        Assert.Equal(0.4, child.Beliefs[0].Strength, Tolerance);
        Assert.Equal(0.4, child.Beliefs[1].Strength, Tolerance);
    }

    private static TimeControl CreateTime(int seed = Seed) =>
        new(TestRules.World, 1, new CivilizationSetup(TestRules.Civilization, TestRules.Terrain, seed, TestRules.Needs), TestRules.History, Rules);

    [Fact]
    [Trait("TS", "TS-belief-006")]
    public void Jump_信者は住人8人のうち3人_ドグマの効き目は信者の割合だけ()
    {
        var time = CreateTime();
        time.Jump(); // 各地域に住人が 1 人ずつ移り住む（8 人）
        time.AddDogma(new Dogma("SYM_WATER", DogmaType.Destruction, 1f)); // 予言者は地域 3。信じ始めるのは地域 2・3・4 の 3 人

        Assert.Equal(3, time.BelieversOf(time.Dogmas[0]));

        time.Jump();

        // 水の破壊は 1 年に水資源 −1.5。信者の割合 3/8 だけ効く
        Assert.Equal(100f - 1.5f * 3 / 8, time.World.WaterAmount, 0.001f);
    }

    [Fact]
    [Trait("TS", "TS-belief-010")]
    public void AddDogma_同じ教えをもう一度告げる_一覧と年表は1つのまま()
    {
        var time = CreateTime();
        time.Jump(); // 各地域に住人が 1 人ずつ移り住む（8 人）
        time.AddDogma(new Dogma("SYM_TREE", DogmaType.Worship, 0.7f));

        time.AddDogma(new Dogma("SYM_TREE", DogmaType.Worship, 0.7f));

        Assert.Equal([new Dogma("SYM_TREE", DogmaType.Worship, 0.7f)], time.Dogmas);
        Assert.Single(time.History!.Entries, entry => entry.Type == HistoryEventType.DogmaFormed);
        Assert.Equal(3, time.BelieversOf(time.Dogmas[0]));
    }

    [Fact]
    [Trait("TS", "TS-belief-007")]
    public void Jump_信者のいない教え_忘れられて一覧から消え年表に残る()
    {
        var time = CreateTime();
        time.AddDogma(new Dogma("SYM_WATER", DogmaType.Destruction, 1f)); // 住人がまだいないので、誰も信じない

        time.Jump();

        Assert.Empty(time.Dogmas);
        Assert.Equal([new Dogma("SYM_WATER", DogmaType.Destruction, 1f)], time.ForgottenDogmas);
        Assert.Contains(time.History!.Entries, entry => entry.Type == HistoryEventType.DogmaForgotten && entry.SymbolId == "SYM_WATER");
        Assert.Equal(100f, time.World.WaterAmount, 0.001f);
    }

    [Fact]
    [Trait("TS", "TS-belief-008")]
    public void Jump_同じシード_30年後の住人ごとの信仰が同じ()
    {
        TimeControl Run()
        {
            var time = CreateTime();
            time.Jump();
            time.AddDogma(new Dogma("SYM_TREE", DogmaType.Worship, 0.8f));
            for (var i = 0; i < 30; i++)
            {
                time.Jump();
            }
            return time;
        }

        var first = Run();
        var second = Run();

        Assert.Equal(first.Residents!.People, second.Residents!.People);
        Assert.Equal(first.Dogmas, second.Dogmas);
        Assert.Contains(first.Residents.People, person => person.Beliefs.Count > 0);
    }
}
