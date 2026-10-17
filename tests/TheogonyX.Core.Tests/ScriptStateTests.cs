using TheogonyX.Core;
using TheogonyX.Core.Persistence;

namespace TheogonyX.Core.Tests;

public class ScriptStateTests
{
    private const int Seed = 42;

    private static readonly ResidentName Name = new("NAME_PREFIX_ART", "NAME_SUFFIX_ORIUS", "NAME_PREFIX_ILIO", "NAME_SUFFIX_A");

    // 湖の岸（列 11）に「水」、列 2 に「木」の目印。禁忌の札は信者 3 割以上
    private static readonly ScriptLandmark[] Landmarks = [new("SYM_WATER", 11, 3), new("SYM_TREE", 2, 0)];

    private static ScriptState Create() =>
        new(ScriptTests.Rules, PlanetScript.Create(ScriptTests.Rules, ScriptTests.Symbols, Seed), TestRules.Civilization.Regions, Landmarks, 0.3f);

    private static Resident Person(int id, int region, params Belief[] beliefs) =>
        new(id, region, 0, 0.1, 0.1, 0.4, 0.3, 0.2, 100, 100, null, 0, ResidentTask.Idle, Name, null) { Beliefs = new BeliefSet(beliefs) };

    // 地域 region に count 人。そのうち believers 人が belief を信じる
    private static IEnumerable<Resident> People(int firstId, int region, int count, int believers, Belief belief) =>
        Enumerable.Range(0, count).Select(i => i < believers ? Person(firstId + i, region, belief) : Person(firstId + i, region));

    private static Belief Teaching(string symbolId, DogmaType type) => new(symbolId, type, 0.7);

    [Fact]
    [Trait("TS", "TS-script-decipher-009")]
    public void Constructor_湖と木の目印_意味の部品だけの字が年0で刻まれる()
    {
        var state = Create();

        Assert.Equal(
            [
                new CarvedGlyph(CarvingKind.Landmark, state.Script.GlyphFor("SYM_WATER"), 5, 11, 3, 0, false),
                new CarvedGlyph(CarvingKind.Landmark, state.Script.GlyphFor("SYM_TREE"), 1, 2, 0, 0, false),
            ],
            state.Carvings);
    }

    [Fact]
    [Trait("TS", "TS-script-decipher-010")]
    public void Update_工房と家と祠_工房は金と崇拝で家は土で祠はいちばん信じられている教え()
    {
        var state = Create();
        Building[] buildings =
        [
            new(1, BuildingType.Workshop, 3, 6, 0, 500), new(2, BuildingType.HouseHut, 3, 7, 2, 500), new(3, BuildingType.Shrine, 3, 6, 4, 500),
        ];
        var residents = new Residents(
            [Person(1, 3, Teaching("SYM_FIRE", DogmaType.Worship)), Person(2, 3, Teaching("SYM_TREE", DogmaType.Taboo)), Person(3, 3, Teaching("SYM_TREE", DogmaType.Taboo))],
            [], 4);

        state.Update(10, buildings, residents);

        var plates = state.Carvings.Where(carving => carving.BuildingId is not null).ToDictionary(carving => carving.BuildingId!.Value);
        Assert.Equal(state.Script.GlyphFor("SYM_METAL", DogmaType.Worship), plates[1].Glyph);
        Assert.Equal(state.Script.GlyphFor("SYM_EARTH"), plates[2].Glyph);
        Assert.Equal(state.Script.GlyphFor("SYM_TREE", DogmaType.Taboo), plates[3].Glyph);
        Assert.Equal(CarvingKind.Shrine, plates[3].Kind);
        Assert.Equal(10, plates[3].Year);
    }

    [Fact]
    [Trait("TS", "TS-script-decipher-010")]
    public void Update_祠の地域の信仰が替わる_新しい年の字で刻み直し信者がいなければ札は消える()
    {
        var state = Create();
        Building[] shrine = [new(3, BuildingType.Shrine, 3, 6, 4, 500)];
        state.Update(10, shrine, new Residents([Person(1, 3, Teaching("SYM_FIRE", DogmaType.Worship))], [], 2));

        state.Update(20, shrine, new Residents([Person(1, 3, Teaching("SYM_WATER", DogmaType.Destruction))], [], 2));
        var rewritten = Assert.Single(state.Carvings, carving => carving.Kind == CarvingKind.Shrine);
        state.Update(30, shrine, new Residents([Person(1, 3)], [], 2));

        Assert.Equal(state.Script.GlyphFor("SYM_WATER", DogmaType.Destruction), rewritten.Glyph);
        Assert.Equal(20, rewritten.Year);
        Assert.DoesNotContain(state.Carvings, carving => carving.Kind == CarvingKind.Shrine);
    }

    [Fact]
    [Trait("TS", "TS-script-decipher-011")]
    public void Update_禁忌の信者が3割と2割_3割の地域だけに禁忌の札が立ち下回ると消える()
    {
        var state = Create();
        var taboo = Teaching("SYM_TREE", DogmaType.Taboo);
        var residents = new Residents([.. People(1, 2, 10, 3, taboo), .. People(11, 4, 10, 2, taboo)], [], 21);

        state.Update(10, [], residents);
        var plate = Assert.Single(state.Carvings, carving => carving.Kind == CarvingKind.Taboo);
        state.Update(11, [], new Residents([.. People(1, 2, 10, 2, taboo)], [], 21));

        Assert.Equal(2, plate.Region);
        Assert.Equal(state.Script.GlyphFor("SYM_TREE", DogmaType.Taboo), plate.Glyph);
        Assert.Equal(4, plate.Column);
        Assert.DoesNotContain(state.Carvings, carving => carving.Kind == CarvingKind.Taboo);
    }

    [Fact]
    [Trait("TS", "TS-script-decipher-012")]
    public void Update_300年を越える_一度だけ崩れ古い字は古い形のまま新しい字は崩れた形()
    {
        var state = Create();
        Building[] workshop = [new(1, BuildingType.Workshop, 3, 6, 0, 900)];
        var before = state.Update(299, workshop, null);
        var decayed = state.Update(300, [.. workshop, new Building(2, BuildingType.HouseHut, 3, 7, 2, 900)], null);
        var again = state.Update(301, workshop, null);

        Assert.False(before);
        Assert.True(decayed);
        Assert.False(again);
        Assert.True(state.Decayed);
        Assert.False(state.Carvings.Single(carving => carving.BuildingId == 1).Decayed);
        Assert.DoesNotContain(state.Carvings, carving => carving.BuildingId == 2);
        Assert.All(state.Carvings.Where(carving => carving.Kind == CarvingKind.Landmark), carving => Assert.False(carving.Decayed));
    }

    [Fact]
    [Trait("TS", "TS-script-decipher-013")]
    public void Discover_刻まれた字を見る_部品が手帳に載り書き込みは一覧の意味だけ()
    {
        var state = Create();
        var glyph = state.Script.GlyphFor("SYM_TREE", DogmaType.Worship);

        var added = state.Discover(glyph);
        var again = state.Discover(glyph);
        state.Label(glyph.First, "SYM_WATER");

        Assert.Equal(2, added);
        Assert.Equal(0, again);
        Assert.Equal("SYM_WATER", state.Notebook[glyph.First]);
        Assert.Equal(NotebookLabels.Unknown, state.Notebook[glyph.Second!]);
        Assert.Equal(state.Notebook.Keys.ToHashSet(), state.KnownRadicals);
        Assert.Throws<ArgumentException>(() => state.Label(glyph.First, "SYM_UNKNOWN"));
        Assert.Throws<ArgumentException>(() => state.Label("RAD_99", "SYM_TREE"));
    }

    [Fact]
    [Trait("TS", "TS-script-decipher-014")]
    public void RoundTrip_字と刻まれた字と手帳_シードの違う星に読み戻しても同じに戻る()
    {
        var time = new TimeControl(
            TestRules.World, TestRules.JumpYears, new CivilizationSetup(TestRules.Civilization, TestRules.Terrain, Seed, TestRules.Needs), TestRules.History);
        var prophet = new ProphetDreams(time, TestRules.ProphetDreams, new OracleDeck(Seed, TestRules.Deck), TestRules.Oracle);
        var state = Create();
        prophet.AttachScript(state);
        state.Update(400, [new Building(1, BuildingType.Workshop, 3, 6, 0, 900)], null);
        state.Discover(state.Carvings[0].Glyph);
        state.Label(state.Carvings[0].Glyph.First, "SYM_WATER");
        var clock = new AutoPlayClock(new Dictionary<AutoPlaySpeed, double>
        {
            [AutoPlaySpeed.X1] = 600.0,
            [AutoPlaySpeed.X2] = 300.0,
            [AutoPlaySpeed.X3] = 200.0,
        });
        time.World.Year = 400;
        var json = SaveDataSerializer.Write(SaveGame.Capture(time, prophet, Seed, clock));
        Assert.True(
            SaveDataSerializer.TryParse(json, TestRules.Dream, TestRules.ProphetDreams, TestRules.Civilization, TestRules.Needs, TestRules.Oracle, out var data, out var reason),
            reason);
        var loadedTime = new TimeControl(
            TestRules.World, TestRules.JumpYears, new CivilizationSetup(TestRules.Civilization, TestRules.Terrain, Seed, TestRules.Needs), TestRules.History);
        var loadedProphet = new ProphetDreams(loadedTime, TestRules.ProphetDreams, new OracleDeck(Seed, TestRules.Deck), TestRules.Oracle);
        var loaded = new ScriptState(
            ScriptTests.Rules, PlanetScript.Create(ScriptTests.Rules, ScriptTests.Symbols, 7), TestRules.Civilization.Regions, Landmarks, 0.3f);
        loadedProphet.AttachScript(loaded);

        SaveGame.Apply(data!, loadedTime, loadedProphet, clock);

        Assert.Equal(state.Script.Arrangement, loaded.Script.Arrangement);
        Assert.Equal(state.Script.SimilarPair, loaded.Script.SimilarPair);
        Assert.Equal(
            state.Script.Radicals.Select(radical => (radical.Id, radical.Key, string.Join(';', radical.Strokes), radical.DecayStroke)),
            loaded.Script.Radicals.Select(radical => (radical.Id, radical.Key, string.Join(';', radical.Strokes), radical.DecayStroke)));
        Assert.True(loaded.Decayed);
        Assert.Equal(state.Carvings, loaded.Carvings);
        Assert.Equal(state.Notebook, loaded.Notebook);
    }

    [Fact]
    [Trait("TS", "TS-script-decipher-014")]
    public void Restore_定義に無い部品を指す手帳_何も変えずに拒む()
    {
        var state = Create();
        var save = state.Capture() with { Notebook = new Dictionary<string, string> { ["RAD_99"] = NotebookLabels.Unknown } };
        var target = Create();

        Assert.Throws<ArgumentException>(() => target.Restore(save));
        Assert.Empty(target.Notebook);
    }
}
