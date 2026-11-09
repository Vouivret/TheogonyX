using TheogonyX.Core;
using TheogonyX.Core.Persistence;

namespace TheogonyX.Core.Tests;

// docs/domain/script-decipher.md の「9. 危機で読みが変わる」の読みの跡の印（段 3）と、docs/domain/structures.md の印
public class MarkTests
{
    private const int Seed = 42;
    private const int Year = 120;

    // game/data/structures.json と同じ形の仮の値（閾値 2 人、木の印 60 年・石の印 300 年、祭壇は調和度 +0.3、木の切り株 -0.5・木の壁 -0.3）
    private static readonly MarkRules Rules = new(2,
    [
        new MarkType("STRUCT_ALTAR", OracleReading.Revere, "STONE", 300, MarkPlace.SymbolOrCenter,
            [new MarkEffect(null, new EnvironmentValues(0f, 0f, 0f, 0f, HarmonyLevel: 0.3f))]),
        new MarkType("STRUCT_FENCE", OracleReading.Seal, "WOOD", 60, MarkPlace.Border, ClosesRegion: true),
        new MarkType("STRUCT_STUMP", OracleReading.Clear, "WOOD", 40, MarkPlace.SymbolOrAnywhere,
            [new MarkEffect("SYM_TREE", new EnvironmentValues(ForestAmount: -0.5f, 0f, 0f, 0f, 0f))]),
        new MarkType("STRUCT_WALL", OracleReading.Arm, "STONE", 300, MarkPlace.Border,
            [new MarkEffect("SYM_TREE", new EnvironmentValues(ForestAmount: -0.3f, 0f, 0f, 0f, 0f))]),
    ]);

    // テストの星は 16 列・8 地域（1 地域 2 列）。焚き火の目印は列 11（地域 5 は列 10・11）
    private static readonly MarkSetup Setup = new(Rules, [new ScriptLandmark("SYM_FIRE", 11, 2)]);

    private static readonly ResidentName Name = new("NAME_PREFIX_ART", "NAME_SUFFIX_ORIUS", "NAME_PREFIX_ILIO", "NAME_SUFFIX_A");

    private static readonly Belief FireRevere = new("SYM_FIRE", DogmaType.Worship, 0.7, OracleReading.Revere);
    private static readonly Belief WaterRevere = new("SYM_WATER", DogmaType.Worship, 0.7, OracleReading.Revere);
    private static readonly Belief TreeSeal = new("SYM_TREE", DogmaType.Taboo, 0.7, OracleReading.Seal);
    private static readonly Belief TreeArm = new("SYM_TREE", DogmaType.Destruction, 0.7, OracleReading.Arm);
    private static readonly Belief TreeStore = new("SYM_TREE", DogmaType.Worship, 0.7, OracleReading.Store);

    private static Resident Person(int id, int region, params Belief[] beliefs) =>
        new(id, region, 0, 0.1, 0.1, 0.4, 0.3, 0.2, 100, 100, null, 0, ResidentTask.Idle, Name, null)
        {
            Beliefs = new BeliefSet(beliefs),
        };

    private static Residents People(params Resident[] people) => new(people, [], 1000);

    private static IReadOnlyList<Mark> Advance(Civilization civilization, Residents residents, int year = Year) =>
        MarkSimulator.AdvanceYear(civilization, residents, year, Seed, Setup, TestRules.Civilization);

    [Fact]
    [Trait("TS", "TS-script-decipher-035")]
    public void AdvanceYear_火を祀る信者が2人_焚き火の目印の隣に祭壇が建ち_1人の地域には建たない()
    {
        var civilization = Civilization.CreateInitial(TestRules.Civilization);
        var residents = People(Person(0, 5, FireRevere), Person(1, 5, FireRevere), Person(2, 7, FireRevere));

        var built = Advance(civilization, residents);

        var altar = Assert.Single(built);
        Assert.Equal(new Mark(OracleReading.Revere, "SYM_FIRE", 5, 10, 2, Year, Year), altar);
        Assert.Equal(DogmaType.Worship, altar.Type);
        Assert.Equal([altar], civilization.Marks);
    }

    [Fact]
    [Trait("TS", "TS-script-decipher-035")]
    public void AdvanceYear_水の目印が無い地域で水を祀る_祭壇は地域の真ん中に建つ()
    {
        var civilization = Civilization.CreateInitial(TestRules.Civilization);
        var residents = People(Person(0, 2, WaterRevere), Person(1, 2, WaterRevere));

        var altar = Assert.Single(Advance(civilization, residents));

        Assert.Equal((5, 4), (altar.Column, altar.Row)); // 地域 2 は列 4・5、行は 9 行の真ん中
    }

    [Fact]
    [Trait("TS", "TS-script-decipher-035")]
    public void AdvanceYear_目印の隣に建物と廃墟がある_祭壇は2つの2x2マスを避けて下の行に建つ()
    {
        var civilization = new Civilization(
            Civilization.CreateInitial(TestRules.Civilization).Regions,
            [new Building(0, BuildingType.Shrine, 5, 10, 2, 1000)], // 列 10・11 の行 2・3
            [new Ruin(1, BuildingType.HouseHut, 5, 10, 4, 50)], // 列 10・11 の行 4・5
            [], 2);
        var residents = People(Person(0, 5, FireRevere), Person(1, 5, FireRevere));

        var altar = Assert.Single(Advance(civilization, residents));

        Assert.Equal((10, 6), (altar.Column, altar.Row));
    }

    [Fact]
    [Trait("TS", "TS-script-decipher-036")]
    public void AdvanceYear_西の端の地域で境を閉ざし東の端の地域で武器と壁にする_柵と壁は隣のある側の端の列に建つ()
    {
        Mark[] Build()
        {
            var civilization = Civilization.CreateInitial(TestRules.Civilization);
            var residents = People(Person(0, 0, TreeSeal), Person(1, 0, TreeSeal), Person(2, 7, TreeArm), Person(3, 7, TreeArm));
            return [.. Advance(civilization, residents)];
        }

        var marks = Build();

        Assert.Equal(2, marks.Length);
        var fence = marks.Single(mark => mark.Reading == OracleReading.Seal);
        var wall = marks.Single(mark => mark.Reading == OracleReading.Arm);
        Assert.Equal(1, fence.Column); // 地域 0 は列 0・1。隣は東だけ
        Assert.Equal(14, wall.Column); // 地域 7（東の端）は列 14・15。隣は西だけ
        Assert.InRange(fence.Row, 0, 8);
        Assert.Equal(marks, Build()); // 同じシード・同じ年なら同じ場所
    }

    [Fact]
    [Trait("TS", "TS-script-decipher-036")]
    public void AdvanceYear_1つの地域に印の候補が2つ_その年は信者の多い方だけ建ち次の年にもう1つが建つ()
    {
        var civilization = Civilization.CreateInitial(TestRules.Civilization);
        var residents = People(
            Person(0, 3, TreeArm), Person(1, 3, TreeArm),
            Person(2, 3, FireRevere), Person(3, 3, FireRevere), Person(4, 3, FireRevere),
            Person(5, 3, TreeStore), Person(6, 3, TreeStore)); // 蓄えるには印の型が無い

        var first = Assert.Single(Advance(civilization, residents));
        var second = Assert.Single(Advance(civilization, residents, Year + 1));

        Assert.Equal(OracleReading.Revere, first.Reading);
        Assert.Equal(OracleReading.Arm, second.Reading);
        Assert.Empty(Advance(civilization, residents, Year + 2));
        Assert.Equal(2, civilization.Marks.Count);
    }

    [Fact]
    [Trait("TS", "TS-script-decipher-037")]
    public void AdvanceYear_信者がいる間は修繕され_いなくなって寿命の年数がたつと消える()
    {
        var fence = new Mark(OracleReading.Seal, "SYM_TREE", 0, 1, 3, 50, 100);
        var wall = new Mark(OracleReading.Arm, "SYM_TREE", 7, 14, 3, 50, 100);
        var tended = new Mark(OracleReading.Seal, "SYM_TREE", 4, 9, 3, 50, 100);
        var civilization = new Civilization(
            Civilization.CreateInitial(TestRules.Civilization).Regions, [], [], [], 0, [fence, wall, tended]);
        var residents = People(Person(0, 4, TreeSeal)); // 地域 4 にだけ信者が 1 人（閾値より少なくても修繕はする）

        Advance(civilization, residents, 159);
        Assert.Equal(3, civilization.Marks.Count); // 木の柵は 59 年目でまだ残る

        Advance(civilization, residents, 160);

        Assert.Equal(
            [wall, tended with { TendedYear = 160 }], // 木の柵は 60 年で消え、石の壁は残る
            civilization.Marks);
    }

    [Fact]
    [Trait("TS", "TS-script-decipher-040")]
    public void ApplyEffects_その年に修繕された印だけが効き_記号の合う効き目を足す()
    {
        Mark Tended(OracleReading reading, string symbolId, int region, int tendedYear = Year) =>
            new(reading, symbolId, region, 0, 0, 50, tendedYear);
        var marks = new[]
        {
            Tended(OracleReading.Revere, "SYM_FIRE", 0), // 調和度 +0.3
            Tended(OracleReading.Revere, "SYM_WATER", 1), // 調和度 +0.3
            Tended(OracleReading.Revere, "SYM_FIRE", 2, Year - 1), // 修繕されていない（効かない）
            Tended(OracleReading.Clear, "SYM_TREE", 3), // 森林量 -0.5
            Tended(OracleReading.Clear, "SYM_WATER", 4), // 木以外の更地は効かない
            Tended(OracleReading.Arm, "SYM_TREE", 5), // 森林量 -0.3
            Tended(OracleReading.Seal, "SYM_TREE", 6), // 柵は世界の値を変えない
        };
        var world = new WorldState { Year = Year, ForestAmount = 50f, WaterAmount = 50f, Temperature = 15f, ChaosLevel = 10f, HarmonyLevel = 99.8f };

        MarkSimulator.ApplyEffects(marks, Year, Rules, world, TestRules.World);

        Assert.Equal(49.2f, world.ForestAmount, 4);
        Assert.Equal(100f, world.HarmonyLevel); // 99.8 + 0.6 は上限 100 に収まる
        Assert.Equal((50f, 15f, 10f), (world.WaterAmount, world.Temperature, world.ChaosLevel));
    }

    [Fact]
    [Trait("TS", "TS-script-decipher-041")]
    public void ClosedRegions_前の年の終わりまでに修繕された柵の地域だけを返し_祭壇や古い柵の地域は含まない()
    {
        var marks = new[]
        {
            new Mark(OracleReading.Seal, "SYM_TREE", 1, 3, 0, 50, Year - 1), // 前の年の終わりに修繕
            new Mark(OracleReading.Seal, "SYM_TREE", 4, 9, 0, 50, Year - 2), // 信者がいなくなった柵
            new Mark(OracleReading.Revere, "SYM_FIRE", 6, 12, 0, 50, Year - 1),
        };

        Assert.Equal([1], MarkSimulator.ClosedRegions(marks, Year, Rules));
    }

    [Fact]
    [Trait("TS", "TS-script-decipher-041")]
    public void AdvanceYear_柵のある地域_大人がいなくても移住者は来ず_ほかの地域には来る()
    {
        var residents = Residents.CreateInitial();
        var civilization = Civilization.CreateInitial(TestRules.Civilization);
        var world = new WorldState { Year = 1, ForestAmount = 50f, WaterAmount = 50f, Temperature = 15f };

        ResidentSimulator.AdvanceYear(
            residents, civilization, world, [], TestRules.Needs, TestRules.Civilization, TestRules.Terrain, Seed, closedRegions: new HashSet<int> { 2, 5 });

        Assert.Equal([0, 1, 3, 4, 6, 7], residents.People.Select(person => person.Region));
    }

    [Fact]
    [Trait("TS", "TS-script-decipher-038")]
    public void AdvanceOneYear_印が建つ_年表に1行残りセーブから戻る_地域の外の印は拒む()
    {
        var beliefs = new BeliefRules(MaxBeliefs: 3, SeedRegions: 1, GatherTransmit: 0f, PartnerTransmit: 0f, Dampen: 0.8f, InheritDampen: 0.8f);
        var time = new TimeControl(
            TestRules.World, TestRules.JumpYears, new CivilizationSetup(TestRules.Civilization, TestRules.Terrain, Seed, TestRules.Needs),
            TestRules.History, beliefs, marks: Setup);
        time.Restore(time.World, [], time.Civilization!, People(Person(0, 5, FireRevere), Person(1, 5, FireRevere)));
        time.StartAutoPlay();

        time.AdvanceOneYear();

        var altar = Assert.Single(time.Civilization!.Marks);
        Assert.Equal((5, 10, 2), (altar.Region, altar.Column, altar.Row));
        var entry = Assert.Single(time.History!.Entries, entry => entry.Type == HistoryEventType.MarkBuilt);
        Assert.Equal((5, "SYM_FIRE", DogmaType.Worship, OracleReading.Revere), (entry.Region, entry.SymbolId, entry.DogmaType, entry.Reading));

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
        Assert.Equal([altar], data!.Civilization.Marks);
        Assert.Contains(entry, data.History);

        var broken = json.Replace("\"column\":10,\"row\":2,\"builtYear\"", "\"column\":12,\"row\":2,\"builtYear\"", StringComparison.Ordinal);
        Assert.NotEqual(json, broken);
        Assert.False(SaveDataSerializer.TryParse(
            broken, TestRules.Dream, TestRules.ProphetDreams, TestRules.Civilization, TestRules.Needs, TestRules.Oracle, out _, out _));
    }
}
