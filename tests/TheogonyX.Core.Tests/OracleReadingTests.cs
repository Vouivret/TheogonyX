using TheogonyX.Core;
using TheogonyX.Core.Persistence;

namespace TheogonyX.Core.Tests;

// docs/domain/script-decipher.md の「9. 危機で読みが変わる」
public class OracleReadingTests
{
    private const int Seed = 42;

    // 争いは混乱、欠乏は渇き・飢え・暑さ・寒さ（game/data/rules.json と同じ）
    private static readonly ScriptRules Rules = ScriptTests.Rules with
    {
        StrifeRequestIds = ["CHAOS"],
        WantRequestIds = ["THIRST", "FAMINE", "HEAT", "COLD"],
    };

    private static (ProphetDreams Prophet, TimeControl Time, ScriptState Script) Create()
    {
        var time = new TimeControl(
            TestRules.World, TestRules.JumpYears, new CivilizationSetup(TestRules.Civilization, TestRules.Terrain, Seed, TestRules.Needs), TestRules.History);
        var deck = new OracleDeck(Seed, new DeckRules([new DeckRuneCount("SYM_TREE", 3), new DeckRuneCount("SYM_WATER", 3)], 4));
        var prophet = new ProphetDreams(time, TestRules.ProphetDreams, deck, TestRules.Oracle);
        var script = new ScriptState(Rules, PlanetScript.Create(Rules, ScriptTests.Symbols, Seed), TestRules.Civilization.Regions, [], 0.3f);
        prophet.AttachScript(script, Seed);
        // 困りごとの無い星にしてから、テストごとに危機を起こす
        time.World.WaterAmount = 80f;
        time.World.ForestAmount = 80f;
        time.World.Temperature = 20f;
        time.World.ChaosLevel = 0f;
        return (prophet, time, script);
    }

    private static Dogma SpeakTreeDestruction(ProphetDreams prophet, ScriptState script)
    {
        var sentence = script.Script.SentenceFor("SYM_TREE", DogmaType.Destruction);
        script.Discover(sentence);
        return prophet.SpeakScript(sentence).Result!.Dogma!;
    }

    [Fact]
    [Trait("TS", "TS-script-decipher-025")]
    public void CurrentCrisis_混乱と渇きが重なる_争い_渇きだけなら欠乏_どちらも無ければ平時()
    {
        var (prophet, time, _) = Create();
        Assert.Equal(Crisis.Peace, prophet.CurrentCrisis());

        time.World.WaterAmount = 20f;
        Assert.Equal(Crisis.Want, prophet.CurrentCrisis());

        time.World.ChaosLevel = 70f;
        Assert.Equal(Crisis.Strife, prophet.CurrentCrisis());
    }

    [Fact]
    [Trait("TS", "TS-script-decipher-026")]
    public void SpeakScript_木と壊せ_平時は切り開く_争いは武器と壁にする()
    {
        var (peaceProphet, _, peaceScript) = Create();
        var (strifeProphet, strifeTime, strifeScript) = Create();
        strifeTime.World.ChaosLevel = 70f;

        Assert.Equal(new Dogma("SYM_TREE", DogmaType.Destruction, 0.7f, OracleReading.Clear), SpeakTreeDestruction(peaceProphet, peaceScript));
        Assert.Equal(new Dogma("SYM_TREE", DogmaType.Destruction, 0.7f, OracleReading.Arm), SpeakTreeDestruction(strifeProphet, strifeScript));
        Assert.Equal(OracleReading.Arm, Assert.Single(strifeTime.History!.Entries, entry => entry.Type == HistoryEventType.DogmaFormed).Reading);
    }

    [Fact]
    [Trait("TS", "TS-script-decipher-026")]
    public void AddDogma_記号と種別が同じで読みが違う_別の教えとして並ぶ()
    {
        var (prophet, time, script) = Create();
        SpeakTreeDestruction(prophet, script);
        time.World.ChaosLevel = 70f;
        prophet.Faith.Advance(100);

        SpeakTreeDestruction(prophet, script);

        Assert.Equal(
            [new Dogma("SYM_TREE", DogmaType.Destruction, 0.7f, OracleReading.Clear), new Dogma("SYM_TREE", DogmaType.Destruction, 0.7f, OracleReading.Arm)],
            time.Dogmas);
    }

    [Fact]
    [Trait("TS", "TS-script-decipher-027")]
    public void WithReading_血の教え_読みを付けない_ほかの記号は12通りの表から引く()
    {
        var blood = new Dogma("SYM_BLOOD", DogmaType.Worship, 0.7f);

        Assert.Equal(blood, OracleReadings.WithReading(blood, Crisis.Strife));
        Assert.Equal(OracleReading.Isolate, OracleReadings.WithReading(new Dogma("SYM_WATER", DogmaType.Taboo, 0.7f), Crisis.Plague).Reading);
        Assert.Equal(OracleReading.Store, OracleReadings.Of(DogmaType.Worship, Crisis.Want));
        Assert.Equal(OracleReading.Seal, OracleReadings.Of(DogmaType.Taboo, Crisis.Strife));
        Assert.Equal(OracleReading.Strip, OracleReadings.Of(DogmaType.Destruction, Crisis.Want));
    }

    [Fact]
    [Trait("TS", "TS-script-decipher-028")]
    public void ReadAs_混ざった組の大きい方_混ざる前はそのまま_混ざった後は小さい方として読む()
    {
        var script = PlanetScript.Create(Rules, ScriptTests.Symbols, Seed);
        var (a, b) = script.SimilarPairs[0];
        var (smaller, larger) = string.CompareOrdinal(a, b) < 0 ? (a, b) : (b, a);

        Assert.Equal(larger, ScriptReader.ReadAs(script, larger, 0));
        Assert.Equal(smaller, ScriptReader.ReadAs(script, larger, 1));
        Assert.Equal(smaller, ScriptReader.ReadAs(script, smaller, 1));
    }

    [Fact]
    [Trait("TS", "TS-script-decipher-029")]
    public void TryParse_読みの付いた教え_戻る_種別の行に無い読みは拒む()
    {
        var (prophet, time, script) = Create();
        var teaching = SpeakTreeDestruction(prophet, script);
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
        Assert.Equal(teaching, Assert.Single(data!.Dogmas));

        var broken = json.Replace("\"reading\":\"Clear\"", "\"reading\":\"Revere\"", StringComparison.Ordinal);
        Assert.NotEqual(json, broken);
        Assert.False(SaveDataSerializer.TryParse(
            broken, TestRules.Dream, TestRules.ProphetDreams, TestRules.Civilization, TestRules.Needs, TestRules.Oracle, out _, out _));
    }
}
