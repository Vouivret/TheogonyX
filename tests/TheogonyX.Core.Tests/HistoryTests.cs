namespace TheogonyX.Core.Tests;

/// <summary>星の年表（docs/domain/history.md の受け入れ条件 AC-history-001〜006）。</summary>
public class HistoryTests
{
    private const int Seed = 42;

    private static readonly ResidentName AnyName = new("NAME_PREFIX_ART", "NAME_SUFFIX_ORIUS", "NAME_PREFIX_ILIO", "NAME_SUFFIX_A");

    private static TimeControl CreateTime(HistoryRules? history = null) =>
        new(TestRules.World, TestRules.JumpYears, new CivilizationSetup(TestRules.Civilization, TestRules.Terrain, Seed, TestRules.Needs),
            history ?? TestRules.History);

    private static Resident Person(int id, ResidentEpithet? epithet = null, BreakdownKind? breakdown = null) =>
        new(id, 0, 0, 0, 0, 0, 0, 0, 100, 0, breakdown, 0, ResidentTask.Idle, AnyName, epithet);

    [Fact]
    [Trait("TS", "TS-history-001")]
    public void AddDogma_年120に木の禁忌_その年のドグマの行が残る()
    {
        var time = CreateTime();
        time.World.Year = 120;

        time.AddDogma(new Dogma("SYM_TREE", DogmaType.Taboo, -0.8f));

        Assert.Equal(new HistoryEntry(120, HistoryEventType.DogmaFormed, "SYM_TREE", DogmaType.Taboo), Assert.Single(time.History!.Entries));
    }

    [Fact]
    [Trait("TS", "TS-history-002")]
    public void AddResidentEvents_いろいろな出来事_精神崩壊と称号を持つ住人の死だけが残る()
    {
        var history = new History([], TestRules.History);
        var elder = new ResidentEpithet(Epithets.Elder);

        history.AddResidentEvents(
        [
            new ResidentEvent(ResidentEventType.Arrived, 10, Person(0)),
            new ResidentEvent(ResidentEventType.BrokeDown, 10, Person(1, breakdown: BreakdownKind.Catatonia)),
            new ResidentEvent(ResidentEventType.Died, 11, Person(2)),
            new ResidentEvent(ResidentEventType.Died, 11, Person(3, elder)),
            new ResidentEvent(ResidentEventType.Born, 12, Person(4)),
            new ResidentEvent(ResidentEventType.Paired, 12, Person(5), Person(6)),
        ]);

        Assert.Equal(
            [
                new HistoryEntry(10, HistoryEventType.ResidentBrokeDown, Name: AnyName, Breakdown: BreakdownKind.Catatonia),
                new HistoryEntry(11, HistoryEventType.NotableDied, Name: AnyName, Epithet: elder),
            ],
            history.Entries);
    }

    [Fact]
    [Trait("TS", "TS-history-003")]
    public void Jump_カオス度995で崩壊する_崩壊の行が1つだけ残り世界再創生で再創生の行が足される()
    {
        var time = CreateTime();
        time.Restore(
            new WorldState { Year = 40, ForestAmount = 50f, WaterAmount = 50f, Temperature = 15f, ChaosLevel = 99.5f, HarmonyLevel = 50f },
            [new Dogma("SYM_BLOOD", DogmaType.Worship, 1f)], Civilization.CreateInitial(TestRules.Civilization), Residents.CreateInitial(),
            [new HistoryEntry(30, HistoryEventType.DogmaFormed, "SYM_BLOOD", DogmaType.Worship)]);

        time.Jump();
        var destroyedYear = time.World.Year;
        time.Reset();

        Assert.Equal(
            [
                new HistoryEntry(30, HistoryEventType.DogmaFormed, "SYM_BLOOD", DogmaType.Worship),
                new HistoryEntry(destroyedYear, HistoryEventType.WorldDestroyed),
                new HistoryEntry(destroyedYear, HistoryEventType.WorldRecreated),
            ],
            time.History!.Entries);
        Assert.Equal(41, destroyedYear);
    }

    [Fact]
    [Trait("TS", "TS-history-004")]
    public void Add_上限3の年表に5行_古い2行が消え新しい3行が残る()
    {
        var history = new History([], new HistoryRules(MaxEntries: 3));

        foreach (var year in Enumerable.Range(1, 5))
        {
            history.Add(new HistoryEntry(year, HistoryEventType.WorldRecreated));
        }

        Assert.Equal([3, 4, 5], history.Entries.Select(entry => entry.Year));
    }

    [Fact]
    [Trait("TS", "TS-history-005")]
    public void TimeControl_同じシードで森の破壊と血の崇拝_年表が一致し精神崩壊と崩壊の行がある()
    {
        IReadOnlyList<HistoryEntry> Run()
        {
            var time = CreateTime();
            time.AddDogma(new Dogma("SYM_TREE", DogmaType.Destruction, 0.3f));
            time.AddDogma(new Dogma("SYM_BLOOD", DogmaType.Worship, 0.3f));
            for (var i = 0; i < 3 && time.CanDo(TimeControlAction.Jump); i++)
            {
                time.Jump();
            }
            return time.History!.Entries;
        }

        var first = Run();

        Assert.Equal(first, Run());
        Assert.Contains(first, entry => entry.Type == HistoryEventType.ResidentBrokeDown);
        Assert.Single(first, entry => entry.Type == HistoryEventType.WorldDestroyed);
    }

    [Fact]
    [Trait("TS", "TS-history-006")]
    public void TimeControl_年表の定義が無い_年表を持たない()
    {
        var time = new TimeControl(TestRules.World, TestRules.JumpYears);

        time.AddDogma(new Dogma("SYM_TREE", DogmaType.Taboo, -0.8f));

        Assert.Null(time.History);
    }
}
