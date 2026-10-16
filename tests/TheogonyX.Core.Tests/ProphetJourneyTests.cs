using TheogonyX.Core;

namespace TheogonyX.Core.Tests;

public class ProphetJourneyTests
{
    private const int Seed = 42;
    private const int RegionCount = 8;

    private static readonly ResidentName Name = new("NAME_PREFIX_ART", "NAME_SUFFIX_ORIUS", "NAME_PREFIX_ILIO", "NAME_SUFFIX_A");

    private static Resident Person(int id, int region, bool believer = false) =>
        new(id, region, 280, 0.1, 0.1, 0.4, 0.3, 0.2, 100, 100, null, 0, ResidentTask.Idle, Name, null)
        {
            Beliefs = new BeliefSet(believer ? [new Belief("SYM_TREE", DogmaType.Worship, 0.5)] : []),
        };

    private static Residents Residents(params Resident[] people) => new(people, [], people.Length);

    private static (ProphetState State, ProphetMove? Move) Advance(ProphetState prophet, Residents residents, float wander = 0f) =>
        ProphetJourney.AdvanceYear(prophet, residents, 100, Seed, RegionCount, new JourneyRules(wander));

    [Fact]
    [Trait("TS", "TS-prophet-journey-001")]
    public void AdvanceYear_行き先が東_1年に1地域ずつ東へ進み知っている土地が増える()
    {
        var prophet = ProphetJourney.Initial(3, RegionCount) with { Destination = TravelDirection.East };

        var (state, move) = Advance(prophet, Residents());

        Assert.Equal(new ProphetMove(100, 3, 4, ProphetMoveReason.Destination), move);
        Assert.Equal(4, state.Region);
        Assert.Equal([2, 3, 4, 5], state.KnownRegions);
        Assert.Equal(TravelDirection.East, state.Destination);
    }

    [Fact]
    [Trait("TS", "TS-prophet-journey-002")]
    public void AdvanceYear_東の端に着く_行き先が消え_端でさらに東なら動かず行き先が消える()
    {
        var nearEdge = ProphetJourney.Initial(6, RegionCount) with { Destination = TravelDirection.East };
        var atEdge = ProphetJourney.Initial(7, RegionCount) with { Destination = TravelDirection.East };

        var (arrived, move) = Advance(nearEdge, Residents());
        var (stayed, noMove) = Advance(atEdge, Residents());

        Assert.Equal(7, arrived.Region);
        Assert.NotNull(move);
        Assert.Equal(TravelDirection.None, arrived.Destination);
        Assert.Equal(7, stayed.Region);
        Assert.Null(noMove);
        Assert.Equal(TravelDirection.None, stayed.Destination);
    }

    [Fact]
    [Trait("TS", "TS-prophet-journey-003")]
    public void AdvanceYear_行き先が無い_確率1なら人が多く信者の少ない隣の地域へ広めに行く()
    {
        var prophet = ProphetJourney.Initial(3, RegionCount);
        // 西の地域 2 は 2 人（信者なし）で点数 2、東の地域 4 は 3 人のうち信者 2 人で点数 1
        var residents = Residents(Person(1, 2), Person(2, 2), Person(3, 4), Person(4, 4, believer: true), Person(5, 4, believer: true));

        var (state, move) = Advance(prophet, residents, wander: 1f);

        Assert.Equal(new ProphetMove(100, 3, 2, ProphetMoveReason.Wander), move);
        Assert.Equal(2, state.Region);
    }

    [Fact]
    [Trait("TS", "TS-prophet-journey-004")]
    public void AdvanceYear_隣に誰もいないか確率0_動かない()
    {
        var prophet = ProphetJourney.Initial(3, RegionCount);

        Assert.Null(Advance(prophet, Residents(Person(1, 6)), wander: 1f).Move);
        Assert.Null(Advance(prophet, Residents(Person(1, 2)), wander: 0f).Move);
    }

    [Fact]
    [Trait("TS", "TS-prophet-journey-005")]
    public void Jump_行き先を告げた予言者が動く_新しい信者は動いた先の近くの住人になる()
    {
        var time = new TimeControl(
            TestRules.World, 1, new CivilizationSetup(TestRules.Civilization, TestRules.Terrain, Seed, TestRules.Needs), TestRules.History,
            new BeliefRules(MaxBeliefs: 3, SeedRegions: 1, GatherTransmit: 0.5f, PartnerTransmit: 0.5f, Dampen: 0.8f, InheritDampen: 0.8f), new JourneyRules(0f));
        time.Jump(); // 各地域に住人が 1 人ずつ移り住む。予言者は地域 3
        Assert.Equal(3, time.Prophet!.Region);

        time.SetDestination(TravelDirection.East);
        time.Jump();
        time.AddDogma(new Dogma("SYM_TREE", DogmaType.Worship, 0.8f));

        Assert.Equal(4, time.Prophet.Region);
        Assert.Equal([new ProphetMove(2, 3, 4, ProphetMoveReason.Destination)], time.ProphetMoves);
        // 地域 3・4・5 の住人が信じ始める（予言者が地域 3 のままなら 2・3・4）
        var believerRegions = time.Residents!.People.Where(person => person.Beliefs.Count > 0).Select(person => person.Region).Distinct().Order();
        Assert.Equal([3, 4, 5], believerRegions);
    }
}
