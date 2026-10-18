using TheogonyX.Core;

namespace TheogonyX.Core.Tests;

public class ScriptSpeakTests
{
    private const int Seed = 42;

    // 手札は木と水だけ。方言の状態（記録に使う）と星の字を付ける
    private static (ProphetDreams Prophet, TimeControl Time, ScriptState Script) Create()
    {
        var time = new TimeControl(
            TestRules.World, TestRules.JumpYears, new CivilizationSetup(TestRules.Civilization, TestRules.Terrain, Seed, TestRules.Needs), TestRules.History);
        var deck = new OracleDeck(Seed, new DeckRules([new DeckRuneCount("SYM_TREE", 3), new DeckRuneCount("SYM_WATER", 3)], 4));
        var prophet = new ProphetDreams(time, TestRules.ProphetDreams, deck, TestRules.Oracle);
        prophet.AttachDialect(new DialectState(OracleDialectTests.Rules, OracleDialectTests.Silent, Seed));
        var script = new ScriptState(ScriptTests.Rules, PlanetScript.Create(ScriptTests.Rules, ScriptTests.Symbols, Seed), TestRules.Civilization.Regions, [], 0.3f);
        prophet.AttachScript(script);
        return (prophet, time, script);
    }

    [Fact]
    [Trait("TS", "TS-script-decipher-016")]
    public void SpeakScript_手帳に載った語で木と崇めよと東の文_木の崇拝が生まれ予言者が東へ向かい神威を使う()
    {
        var (prophet, time, script) = Create();
        var sentence = script.Script.SentenceFor("SYM_TREE", DogmaType.Worship, TravelDirection.East);
        script.Discover(sentence);

        var outcome = prophet.SpeakScript(sentence);

        Assert.Null(outcome.RejectedReason);
        Assert.Equal(new Dogma("SYM_TREE", DogmaType.Worship, 0.7f), Assert.Single(time.Dogmas));
        Assert.Equal(TravelDirection.East, time.Prophet!.Destination);
        Assert.Equal(2.0, prophet.Faith.Value, 3);
        Assert.Equal(outcome.Result!.Record, Assert.Single(prophet.Dialect!.Records));
    }

    [Fact]
    [Trait("TS", "TS-script-decipher-016")]
    public void SpeakScript_謎の語だけ_教えは生まれないが神威は使う()
    {
        var (prophet, time, script) = Create();
        var fire = script.Script.SentenceFor("SYM_FIRE", DogmaType.Worship); // 予言者は火を知らない
        script.Discover(fire);

        var outcome = prophet.SpeakScript(fire);

        Assert.Null(outcome.RejectedReason);
        Assert.False(outcome.Reading!.Phrases[0].IsReadable);
        Assert.Empty(time.Dogmas);
        Assert.Equal(2.0, prophet.Faith.Value, 3);
    }

    [Fact]
    [Trait("TS", "TS-script-decipher-016")]
    public void SpeakScript_手帳に無い語か神威が足りない_何も変えずに理由を返す()
    {
        var (prophet, time, script) = Create();
        var tree = script.Script.SentenceFor("SYM_TREE", DogmaType.Worship);

        var notFound = prophet.SpeakScript(tree);
        script.Discover(tree);
        prophet.RestoreFaith(0.5, []);
        var noFaith = prophet.SpeakScript(tree);

        Assert.Equal("glyph_not_found", notFound.RejectedReason);
        Assert.Equal("not_enough_faith", noFaith.RejectedReason);
        Assert.Empty(time.Dogmas);
        Assert.Empty(prophet.Dialect!.Records);
    }
}
