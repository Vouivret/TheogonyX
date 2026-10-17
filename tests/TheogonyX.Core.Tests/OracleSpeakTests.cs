using TheogonyX.Core;
using TheogonyX.Core.Persistence;

namespace TheogonyX.Core.Tests;

public class OracleSpeakTests
{
    private const int Seed = 42;
    private const float Tolerance = 0.001f;

    private static (ProphetDreams Prophet, TimeControl Time, DialectState Dialect) Create(string vessel = OracleDialectTests.Silent, int seed = Seed)
    {
        var time = new TimeControl(
            TestRules.World, TestRules.JumpYears, new CivilizationSetup(TestRules.Civilization, TestRules.Terrain, seed, TestRules.Needs), TestRules.History);
        var deck = new OracleDeck(seed, new DeckRules([new DeckRuneCount("SYM_TREE", 3), new DeckRuneCount("SYM_WATER", 3)], 4));
        var prophet = new ProphetDreams(time, TestRules.ProphetDreams, deck, TestRules.Oracle);
        var dialect = new DialectState(OracleDialectTests.Rules, vessel, seed);
        prophet.AttachDialect(dialect);
        return (prophet, time, dialect);
    }

    private static Hearing Heard(ProphetDreams prophet, HeardAnswer answer) => prophet.Hear(answer).Hearing!;

    private static HeardAnswer Answer(string? noun, string? verb, HeardDestination destination = HeardDestination.None) =>
        new(noun, verb, destination, null, []);

    [Fact]
    [Trait("TS", "TS-oracle-speak-001")]
    public void Speak_木を守れ_木の崇拝が生まれ神威を使い教えの記号のルーンが捨札へ移って補われる()
    {
        var (prophet, time, dialect) = Create();

        var result = prophet.Speak(Heard(prophet, Answer("NOUN_FOREST", "VERB_PROTECT")), "森を守れ");

        Assert.Equal(new Dogma("SYM_TREE", DogmaType.Worship, 0.7f), Assert.Single(time.Dogmas));
        Assert.Equal(2f, (float)prophet.Faith.Value, Tolerance);
        Assert.Equal(4, prophet.Hand.Count);
        Assert.Equal(["SYM_TREE"], prophet.Deck.DiscardPile);
        var record = Assert.Single(dialect.Records);
        Assert.Equal(record, result.Record);
        Assert.Equal((0, "森を守れ", "NOUN_FOREST", "VERB_PROTECT"), (record.Year, record.GodText, record.HeardNounId, record.HeardVerbId));
    }

    [Fact]
    [Trait("TS", "TS-oracle-speak-002")]
    public void Speak_ずれ表に当たる_聞き違えが方言帳に残り_聞き違えた教えが生まれる()
    {
        var (prophet, time, dialect) = Create(OracleDialectTests.Garbled);

        prophet.Speak(Heard(prophet, Answer("NOUN_FOREST", "VERB_PROTECT")), "森を守れ");

        Assert.Equal(new Dogma("SYM_TREE", DogmaType.Destruction, 0.7f), Assert.Single(time.Dogmas));
        Assert.Equal([new DialectEntry("VERB_PROTECT", "VERB_CUT", DialectReason.Dialect)], dialect.Book.Entries);
    }

    [Fact]
    [Trait("TS", "TS-oracle-speak-003")]
    public void Speak_行き先だけの神託_教えは生まれず予言者が東へ向かう()
    {
        var (prophet, time, _) = Create();

        var result = prophet.Speak(Heard(prophet, Answer(null, null, HeardDestination.East)), "東へ行け");

        Assert.Null(result.Dogma);
        Assert.Empty(time.Dogmas);
        Assert.Empty(prophet.Deck.DiscardPile);
        Assert.Equal(TravelDirection.East, time.Prophet!.Destination);
        Assert.Equal(2f, (float)prophet.Faith.Value, Tolerance);
    }

    [Fact]
    [Trait("TS", "TS-oracle-speak-004")]
    public void Speak_神威が足りないか崩壊している_語りかけられず理由が返る()
    {
        var (prophet, time, _) = Create();
        var hearing = Heard(prophet, Answer("NOUN_FOREST", "VERB_PROTECT"));
        var queue = new OracleCommandQueue(prophet, time);

        Assert.Null(queue.SubmitSpeak(hearing, "森を守れ").RejectedReason);
        Assert.Null(queue.SubmitSpeak(hearing, "森を守れ").RejectedReason);
        Assert.Null(queue.SubmitSpeak(hearing, "森を守れ").RejectedReason); // 神威 3 を使い切る
        var outcome = queue.SubmitSpeak(hearing, "森を守れ");

        Assert.Equal("not_enough_faith", outcome.RejectedReason);
        Assert.Null(outcome.Result);
        Assert.Equal(4, outcome.Sequence);
        Assert.Throws<InvalidOperationException>(() => prophet.Speak(hearing, "森を守れ"));
        time.World.IsDestroyed = true;
        Assert.Equal("star_destroyed", prophet.SpeakRejection());
    }

    [Fact]
    [Trait("TS", "TS-oracle-speak-005")]
    public void AttachNarration_同じ年の同じ神の文の記録に聞こえた文と語りを添える()
    {
        var (prophet, _, dialect) = Create();
        prophet.Speak(Heard(prophet, Answer("NOUN_FOREST", "VERB_PROTECT")), "森を守れ");

        Assert.True(dialect.AttachNarration(0, "森を守れ", "森を囲え", "予言者は柵を築いた"));
        Assert.False(dialect.AttachNarration(0, "水を守れ", "水を囲え", "語り"));
        Assert.False(dialect.AttachNarration(0, "森を守れ", "森を\n囲え", "語り"));

        var record = Assert.Single(dialect.Records);
        Assert.Equal(("森を囲え", "予言者は柵を築いた"), (record.HeardText, record.Narration));
    }

    [Fact]
    [Trait("TS", "TS-oracle-speak-006")]
    public void RoundTrip_方言の状態_ずれ表と方言帳と神託の記録が戻る()
    {
        var (prophet, time, dialect) = Create(OracleDialectTests.Garbled);
        prophet.Speak(Heard(prophet, Answer("NOUN_FOREST", "VERB_PROTECT", HeardDestination.West)), "森を守れ、西へ");
        dialect.AttachNarration(0, "森を守れ、西へ", "森を切れ", "予言者は斧を取った");
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

        // シードも器の段も違う星に当てはめても、保存した表がそのまま戻る
        var (loadedProphet, loadedTime, loadedDialect) = Create(OracleDialectTests.Silent, seed: 7);
        SaveGame.Apply(data!, loadedTime, loadedProphet, clock);

        Assert.Equal(dialect.Table.VesselId, loadedDialect.Table.VesselId);
        Assert.Equal(dialect.Table.Shifts, loadedDialect.Table.Shifts);
        Assert.Equal(dialect.Book.Entries, loadedDialect.Book.Entries);
        var (before, after) = (Assert.Single(dialect.Records), Assert.Single(loadedDialect.Records));
        Assert.Equal((before.Year, before.GodText, before.Teaching, before.Destination, before.HeardText, before.Narration),
            (after.Year, after.GodText, after.Teaching, after.Destination, after.HeardText, after.Narration));
        Assert.Equal(TravelDirection.West, loadedTime.Prophet!.Destination);
    }

    [Fact]
    [Trait("TS", "TS-oracle-speak-007")]
    public void Apply_定義に無い語を含む方言の状態_何も変えずに拒む()
    {
        var (prophet, time, dialect) = Create();
        prophet.Speak(Heard(prophet, Answer("NOUN_FOREST", "VERB_PROTECT")), "森を守れ");
        var save = dialect.Capture();
        var broken = save with { Records = [save.Records[0] with { HeardNounId = "NOUN_UNKNOWN" }] };
        var (_, _, target) = Create();

        Assert.Throws<ArgumentException>(() => target.Restore(broken));
        Assert.Empty(target.Records);
    }
}
