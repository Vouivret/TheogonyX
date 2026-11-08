using TheogonyX.Core;
using TheogonyX.Core.Persistence;

namespace TheogonyX.Core.Tests;

// docs/domain/script-decipher.md の「9. 危機で読みが変わる」の対立と追放
public class SchismTests
{
    private const int Seed = 42;

    private static readonly SchismRules Rules = new(ChaosPerConflictRegion: 2f, ExileChaosAtOrAbove: 50f);

    private static readonly ResidentName Name = new("NAME_PREFIX_ART", "NAME_SUFFIX_ORIUS", "NAME_PREFIX_ILIO", "NAME_SUFFIX_A");

    private static readonly Belief TreeWorship = new("SYM_TREE", DogmaType.Worship, 0.7, OracleReading.Revere);
    private static readonly Belief TreeDestruction = new("SYM_TREE", DogmaType.Destruction, 0.7, OracleReading.Arm);

    private static Resident Person(int id, int region, params Belief[] beliefs) =>
        new(id, region, 0, 0.1, 0.1, 0.4, 0.3, 0.2, 100, 100, null, 0, ResidentTask.Idle, Name, null)
        {
            Beliefs = new BeliefSet(beliefs),
        };

    // 地域 2 に木の崇拝 worship 人・木の破壊 destruction 人。地域 1 に 5 人、地域 3 に 2 人（信仰なし）
    private static Residents Village(int worship, int destruction)
    {
        var people = new List<Resident>();
        people.AddRange(Enumerable.Range(0, worship).Select(i => Person(i, 2, TreeWorship)));
        people.AddRange(Enumerable.Range(100, destruction).Select(i => Person(i, 2, TreeDestruction)));
        people.AddRange(Enumerable.Range(200, 5).Select(i => Person(i, 1)));
        people.AddRange(Enumerable.Range(300, 2).Select(i => Person(i, 3)));
        return new Residents(people, [], 400);
    }

    private static (IReadOnlyList<Exile> Exiles, float Chaos) Advance(Residents residents, float chaos, int regionCount = 8, IReadOnlyList<Dogma>? dogmas = null)
    {
        var exiles = SchismSimulator.AdvanceYear(residents, dogmas ?? [], 120, regionCount, Rules, amount => chaos += amount, () => chaos);
        return (exiles, chaos);
    }

    [Fact]
    [Trait("TS", "TS-script-decipher-031")]
    public void AdvanceYear_同じ地域に木の崇拝と破壊_対立の地域1つ分カオス度が上がり閾値未満なら誰も移らない()
    {
        var residents = Village(3, 7);

        var (exiles, chaos) = Advance(residents, 10f);

        Assert.Equal(12f, chaos);
        Assert.Empty(exiles);
        Assert.Equal(10, residents.People.Count(person => person.Region == 2));
    }

    [Fact]
    [Trait("TS", "TS-script-decipher-031")]
    public void AdvanceYear_崇拝と破壊が別の地域_対立は無くカオス度も変わらない()
    {
        var residents = new Residents([Person(0, 2, TreeWorship), Person(1, 3, TreeDestruction)], [], 2);

        var (exiles, chaos) = Advance(residents, 55f);

        Assert.Equal(55f, chaos);
        Assert.Empty(exiles);
    }

    [Fact]
    [Trait("TS", "TS-script-decipher-032")]
    public void AdvanceYear_崇拝3人と破壊7人でカオス度が閾値以上_崇拝の3人が住人の少ない地域3へ移る()
    {
        var residents = Village(3, 7);

        var (exiles, _) = Advance(residents, 49f);

        var exile = Assert.Single(exiles);
        Assert.Equal((120, 2, 3, "SYM_TREE", DogmaType.Worship), (exile.Year, exile.From, exile.To, exile.SymbolId, exile.Type));
        Assert.Equal([0, 1, 2], exile.ResidentIds);
        Assert.All(residents.People.Where(person => person.Id < 3), person => Assert.Equal(3, person.Region));
        Assert.Equal(7, residents.People.Count(person => person.Region == 2));
    }

    [Fact]
    [Trait("TS", "TS-script-decipher-032")]
    public void AdvanceYear_同じ人数_教えの一覧で後に加わった側が移り_両方を信じる人は残る()
    {
        var people = new List<Resident>
        {
            Person(0, 2, TreeWorship), Person(1, 2, TreeWorship),
            Person(2, 2, TreeDestruction), Person(3, 2, TreeDestruction),
            Person(4, 2, TreeWorship, TreeDestruction),
        };
        var residents = new Residents(people, [], 5);
        var dogmas = new[] { new Dogma("SYM_TREE", DogmaType.Destruction, 0.7f), new Dogma("SYM_TREE", DogmaType.Worship, 0.7f) };

        var (exiles, _) = Advance(residents, 60f, dogmas: dogmas);

        var exile = Assert.Single(exiles);
        Assert.Equal(DogmaType.Worship, exile.Type);
        Assert.Equal([0, 1], exile.ResidentIds);
        Assert.Equal(3, exile.To); // 隣は地域 1・3 とも 0 人なので東
        Assert.Equal(2, residents.People.Single(person => person.Id == 4).Region);
    }

    [Fact]
    [Trait("TS", "TS-script-decipher-032")]
    public void AdvanceYear_移る人につがいと友がいる_つがいの相手は一緒に移り残る人との好感度は消える()
    {
        var people = new List<Resident>
        {
            Person(0, 2, TreeWorship) with { PartnerId = 1 },
            Person(1, 2) with { PartnerId = 0 },
            Person(2, 2, TreeDestruction),
            Person(3, 2, TreeDestruction),
            Person(4, 2, TreeDestruction),
        };
        var residents = new Residents(
            people, [], 5,
            new Dictionary<ResidentPair, int> { [ResidentPair.Of(0, 1)] = 5, [ResidentPair.Of(0, 2)] = 3, [ResidentPair.Of(3, 4)] = 2 });

        var (exiles, _) = Advance(residents, 60f);

        Assert.Equal([0, 1], Assert.Single(exiles).ResidentIds);
        Assert.Equal(
            new Dictionary<ResidentPair, int> { [ResidentPair.Of(0, 1)] = 5, [ResidentPair.Of(3, 4)] = 2 },
            residents.Affinities);
    }

    [Fact]
    [Trait("TS", "TS-script-decipher-033")]
    public void AdvanceYear_陸でつながる隣の地域が無い_誰も移らない()
    {
        var residents = new Residents([Person(0, 0, TreeWorship), Person(1, 0, TreeDestruction), Person(2, 0, TreeDestruction)], [], 3);

        var (exiles, chaos) = Advance(residents, 60f, regionCount: 1);

        Assert.Empty(exiles);
        Assert.Equal(62f, chaos);
        Assert.All(residents.People, person => Assert.Equal(0, person.Region));
    }

    [Fact]
    [Trait("TS", "TS-script-decipher-034")]
    public void AdvanceOneYear_追放が起きる_年表に1行残りセーブから戻る_隣でない地域の行は拒む()
    {
        var beliefs = new BeliefRules(MaxBeliefs: 3, SeedRegions: 1, GatherTransmit: 0f, PartnerTransmit: 0f, Dampen: 0.8f, InheritDampen: 0.8f, Rules);
        var time = new TimeControl(
            TestRules.World, TestRules.JumpYears, new CivilizationSetup(TestRules.Civilization, TestRules.Terrain, Seed, TestRules.Needs),
            TestRules.History, beliefs);
        time.Restore(time.World, [], time.Civilization!, Village(3, 7));
        time.World.ChaosLevel = 70f;
        time.StartAutoPlay();

        time.AdvanceOneYear();

        var entry = Assert.Single(time.History!.Entries, entry => entry.Type == HistoryEventType.Exiled);
        Assert.Equal((2, 3, 3, "SYM_TREE", DogmaType.Worship), (entry.Region, entry.ToRegion, entry.Count, entry.SymbolId, entry.DogmaType));
        Assert.Equal(entry.Year, Assert.Single(time.Exiles).Year);

        var prophet = new ProphetDreams(time, TestRules.ProphetDreams, new OracleDeck(Seed, TestRules.Deck), TestRules.Oracle);
        var clock = new AutoPlayClock(new Dictionary<AutoPlaySpeed, double>
        {
            [AutoPlaySpeed.X1] = 600.0,
            [AutoPlaySpeed.X2] = 300.0,
            [AutoPlaySpeed.X3] = 200.0,
        });
        var json = SaveDataSerializer.Write(SaveGame.Capture(time, prophet, Seed, clock));
        Assert.True(
            SaveDataSerializer.TryParse(json, TestRules.Dream, TestRules.ProphetDreams, TestRules.Civilization, TestRules.Needs, TestRules.Oracle, out var data, out var reason),
            reason);
        Assert.Contains(entry, data!.History);

        var broken = json.Replace("\"toRegion\":3", "\"toRegion\":5", StringComparison.Ordinal);
        Assert.NotEqual(json, broken);
        Assert.False(SaveDataSerializer.TryParse(
            broken, TestRules.Dream, TestRules.ProphetDreams, TestRules.Civilization, TestRules.Needs, TestRules.Oracle, out _, out _));
    }
}
