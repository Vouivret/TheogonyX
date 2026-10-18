using TheogonyX.Core;
using TheogonyX.Core.Persistence;

namespace TheogonyX.Core.Tests;

public class SteleTests
{
    private const int Seed = 42;

    private static readonly ResidentName Name = new("NAME_PREFIX_ART", "NAME_SUFFIX_ORIUS", "NAME_PREFIX_ILIO", "NAME_SUFFIX_A");

    private static readonly BeliefRules Beliefs = new(
        MaxBeliefs: 3, SeedRegions: 1, GatherTransmit: 1f, PartnerTransmit: 1f, Dampen: 0.8f, InheritDampen: 0.8f);

    private static ScriptState CreateScript() =>
        new(ScriptTests.Rules, PlanetScript.Create(ScriptTests.Rules, ScriptTests.Symbols, Seed), TestRules.Civilization.Regions, [], 0.3f);

    private static Resident Person(int id, int region) =>
        new(id, region, 0, 0.1, 0.1, 0.4, 0.3, 0.2, 100, 100, null, 0, ResidentTask.Idle, Name, null);

    // 住人と信仰を計算する時間。地域 5 に 10 人、地域 1 に 2 人
    private static TimeControl CreateTime()
    {
        var time = new TimeControl(
            TestRules.World, TestRules.JumpYears, new CivilizationSetup(TestRules.Civilization, TestRules.Terrain, Seed, TestRules.Needs),
            TestRules.History, Beliefs);
        var people = Enumerable.Range(0, 10).Select(id => Person(id, 5)).Concat([Person(10, 1), Person(11, 1)]);
        time.Restore(time.World, [], time.Civilization!, new Residents(people, [], 12));
        return time;
    }

    // 似た形の組の、ID の大きい方（崩れると小さい方に読まれる）と小さい方の記号
    private static (string Larger, string Smaller) Twins(PlanetScript script)
    {
        var (a, b) = script.SimilarPair;
        var (smaller, larger) = string.CompareOrdinal(a, b) < 0 ? (a, b) : (b, a);
        return (script.Find(larger)!.Key, script.Find(smaller)!.Key);
    }

    private static HashSet<int> BelieversIn(TimeControl time, Dogma dogma) =>
        [.. time.Residents!.People.Where(person => person.Beliefs.Any(belief => belief.SymbolId == dogma.TargetSymbolId && belief.Type == dogma.Type))
            .Select(person => person.Id)];

    [Fact]
    [Trait("TS", "TS-script-decipher-018")]
    public void CarveStele_同じ地域に4つ刻む_新しい3つが残り地域の左端の列の1から3行目に並ぶ()
    {
        var state = CreateScript();
        var sentence = state.Script.SentenceFor("SYM_TREE", DogmaType.Worship);

        foreach (var year in new[] { 10, 20, 30, 40 })
        {
            state.CarveStele(sentence, 2, year);
        }

        var steles = state.Carvings.Where(carving => carving.Kind == CarvingKind.Stele).ToList();
        Assert.Equal([20, 30, 40], steles.Select(stele => stele.Year));
        Assert.Equal([1, 2, 3], steles.Select(stele => stele.Row).Order());
        Assert.All(steles, stele => Assert.Equal(TestRules.Civilization.Regions.FirstColumn(2), stele.Column));
        Assert.All(steles, stele => Assert.False(stele.Decayed));
        Assert.Throws<ArgumentOutOfRangeException>(() => state.CarveStele(sentence, TestRules.Civilization.Regions.Count, 50));
    }

    [Fact]
    [Trait("TS", "TS-script-decipher-019")]
    public void SpeakScript_神託を下す_予言者のいる地域に神が書いた文の碑が刻まれる()
    {
        var time = new TimeControl(
            TestRules.World, TestRules.JumpYears, new CivilizationSetup(TestRules.Civilization, TestRules.Terrain, Seed, TestRules.Needs), TestRules.History);
        var deck = new OracleDeck(Seed, new DeckRules([new DeckRuneCount("SYM_TREE", 3), new DeckRuneCount("SYM_WATER", 3)], 4));
        var prophet = new ProphetDreams(time, TestRules.ProphetDreams, deck, TestRules.Oracle);
        prophet.AttachDialect(new DialectState(OracleDialectTests.Rules, OracleDialectTests.Silent, Seed));
        var state = CreateScript();
        prophet.AttachScript(state);
        var region = time.Prophet!.Region;
        time.World.Year = 120;
        var sentence = state.Script.SentenceFor("SYM_FIRE", DogmaType.Worship); // 予言者の知らない記号でも碑は刻む
        state.Discover(sentence);

        prophet.SpeakScript(sentence);

        var stele = Assert.Single(state.Carvings, carving => carving.Kind == CarvingKind.Stele);
        Assert.Equal(sentence, stele.Sentence);
        Assert.Equal(region, stele.Region);
        Assert.Equal(120, stele.Year);
    }

    [Fact]
    [Trait("TS", "TS-script-decipher-020")]
    public void RereadSteles_崩れた後_似た形の組の語を含む崩れる前の碑だけが別の教えに読まれる()
    {
        var state = CreateScript();
        var (larger, smaller) = Twins(state.Script);
        var other = ScriptTests.Symbols.First(symbol => symbol != larger && symbol != smaller);
        state.CarveStele(state.Script.SentenceFor(larger, DogmaType.Taboo), 2, 10);
        state.CarveStele(state.Script.SentenceFor(other, DogmaType.Taboo), 2, 20);
        state.Update(300, [], null); // 崩れる
        state.CarveStele(state.Script.SentenceFor(larger, DogmaType.Taboo), 3, 300); // 崩れた後に刻んだ碑は読み直さない

        var misreading = Assert.Single(state.RereadSteles());

        Assert.Equal(10, misreading.Stele.Year);
        Assert.Equal((larger, DogmaType.Taboo), (misreading.Written.TargetSymbolId, misreading.Written.Type));
        Assert.Equal((smaller, DogmaType.Taboo), (misreading.Read.TargetSymbolId, misreading.Read.Type));
    }

    [Fact]
    [Trait("TS", "TS-script-decipher-021")]
    public void AdoptMisreading_住人10人と2人と0人の地域_3割の3人と1人が信じ0人なら何もしない()
    {
        var time = CreateTime();
        var dogma = new Dogma("SYM_WATER", DogmaType.Taboo, 0.7f);

        var empty = time.AdoptMisreading(dogma, 0, 0.3, 1);
        Assert.Equal(0, empty);
        Assert.Empty(time.Dogmas);

        var many = time.AdoptMisreading(dogma, 5, 0.3, 1);
        var chosen = BelieversIn(time, dogma);
        var few = time.AdoptMisreading(dogma, 1, 0.3, 1);

        Assert.Equal(3, many);
        Assert.Equal(1, few);
        Assert.Equal(3, chosen.Count);
        Assert.All(chosen, id => Assert.Equal(5, time.Residents!.People.First(person => person.Id == id).Region));
        Assert.Equal(4, BelieversIn(time, dogma).Count);
        Assert.Equal(dogma, Assert.Single(time.Dogmas));
        var again = CreateTime();
        again.AdoptMisreading(dogma, 5, 0.3, 1);
        Assert.Equal(chosen, BelieversIn(again, dogma));
    }

    [Fact]
    [Trait("TS", "TS-script-decipher-021")]
    public void OnTimeAdvanced_字が崩れた年_読み違えた教えを地域の住人の3割が信じ年表に残りセーブから戻る()
    {
        var time = CreateTime();
        var prophet = new ProphetDreams(time, TestRules.ProphetDreams, new OracleDeck(Seed, TestRules.Deck), TestRules.Oracle);
        var state = CreateScript();
        prophet.AttachScript(state);
        var (larger, smaller) = Twins(state.Script);
        state.CarveStele(state.Script.SentenceFor(larger, DogmaType.Worship), 5, 40);
        time.World.Year = 300;

        var update = prophet.OnTimeAdvanced();

        Assert.True(update.ScriptDecayed);
        Assert.Equal(1, update.SteleMisreadings);
        var misread = new Dogma(smaller, DogmaType.Worship, 0.7f);
        Assert.Equal(3, BelieversIn(time, misread).Count);
        var entry = Assert.Single(time.History!.Entries, entry => entry.Type == HistoryEventType.SteleMisread);
        Assert.Equal(
            new HistoryEntry(300, HistoryEventType.SteleMisread, smaller, DogmaType.Worship, FromYear: 40, WrittenSymbolId: larger, WrittenDogmaType: DogmaType.Worship),
            entry);

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
        Assert.Contains(data.Script!.Carvings, carving => carving.Kind == CarvingKind.Stele && carving.Year == 40);
    }
}
