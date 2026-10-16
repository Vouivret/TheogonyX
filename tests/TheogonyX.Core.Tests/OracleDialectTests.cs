using TheogonyX.Core;

namespace TheogonyX.Core.Tests;

public class OracleDialectTests
{
    private const int Seed = 42;
    private const float Tolerance = 0.001f;
    private const string Silent = "VESSEL_SILENT";
    private const string Garbled = "VESSEL_GARBLED";

    // 木と水だけが結びつく。火には隣が無い。器は、ずれない段と、すべての語がずれる段
    private static readonly DialectRules Rules = new(
        [new DialectNoun("NOUN_FOREST", "SYM_TREE"), new DialectNoun("NOUN_RIVER", "SYM_WATER"), new DialectNoun("NOUN_FLAME", "SYM_FIRE")],
        [
            new DialectVerb("VERB_PROTECT", DogmaType.Worship, 0.7f),
            new DialectVerb("VERB_AVOID", DogmaType.Taboo, 0.7f),
            new DialectVerb("VERB_CUT", DogmaType.Destruction, 0.7f),
        ],
        [new SymbolLink("SYM_TREE", "SYM_WATER")],
        [new DialectVessel(Silent, 0f), new DialectVessel(Garbled, 1f)],
        ReversalProbability: 1f,
        DefaultType: DogmaType.Worship,
        DefaultIntensity: 0.7f);

    private static HeardAnswer Answer(
        string? noun = null, string? verb = null, HeardDestination destination = HeardDestination.None, string? substitute = null, string[]? mysteries = null) =>
        new(noun, verb, destination, substitute, mysteries ?? []);

    private static HearOutcome Hear(string vessel, string[] hand, HeardAnswer answer) =>
        OracleHearer.Hear(Rules, DialectTable.Create(Rules, vessel, Seed), hand, answer);

    [Fact]
    [Trait("TS", "TS-oracle-dialect-001")]
    public void Create_器がずれない段_ずれ表は空()
    {
        var table = DialectTable.Create(Rules, Silent, Seed);

        Assert.Empty(table.Shifts);
    }

    [Fact]
    [Trait("TS", "TS-oracle-dialect-001")]
    public void Create_すべての語がずれる段_守るは破壊にずれ川と森が入れ替わり火はずれない()
    {
        var table = DialectTable.Create(Rules, Garbled, Seed);

        Assert.Equal(new DialectShift("VERB_PROTECT", "VERB_CUT", DialectShiftKind.Reversal), table.ShiftOf("VERB_PROTECT"));
        Assert.Equal(new DialectShift("VERB_CUT", "VERB_PROTECT", DialectShiftKind.Reversal), table.ShiftOf("VERB_CUT"));
        Assert.Equal(DialectShiftKind.TypeChange, table.ShiftOf("VERB_AVOID")?.Kind);
        Assert.Equal("NOUN_RIVER", table.ShiftOf("NOUN_FOREST")?.ToId);
        Assert.Equal("NOUN_FOREST", table.ShiftOf("NOUN_RIVER")?.ToId);
        Assert.Null(table.ShiftOf("NOUN_FLAME"));
    }

    [Fact]
    [Trait("TS", "TS-oracle-dialect-002")]
    public void Create_同じシード_同じ表_セーブの行から同じ表に戻せ_定義に合わない行は拒む()
    {
        var first = DialectTable.Create(Rules, Garbled, Seed);
        var second = DialectTable.Create(Rules, Garbled, Seed);
        var restored = DialectTable.Restore(Rules, Garbled, first.Shifts);

        Assert.Equal(first.Shifts, second.Shifts);
        Assert.Equal(first.Shifts, restored.Shifts);
        Assert.Throws<ArgumentException>(() => DialectTable.Restore(Rules, Garbled, [new DialectShift("NOUN_FOREST", "NOUN_FLAME", DialectShiftKind.Neighbor)]));
        Assert.Throws<ArgumentException>(() => DialectTable.Restore(Rules, Garbled, [new DialectShift("VERB_AVOID", "VERB_CUT", DialectShiftKind.Reversal)]));
        Assert.Throws<ArgumentException>(() => DialectTable.Restore(Rules, "VESSEL_UNKNOWN", []));
    }

    [Fact]
    [Trait("TS", "TS-oracle-dialect-003")]
    public void Hear_ずれ表に当たらない_言ったとおりの教えになり方言帳は空()
    {
        var hearing = Hear(Silent, ["SYM_TREE"], Answer("NOUN_FOREST", "VERB_PROTECT")).Hearing!;

        Assert.Equal(new Dogma("SYM_TREE", DogmaType.Worship, 0.7f), hearing.Teaching);
        Assert.Empty(hearing.BookEntries);
    }

    [Fact]
    [Trait("TS", "TS-oracle-dialect-004")]
    public void Hear_名詞も動詞もずれ表に当たる_動詞だけがずれて1か所まで()
    {
        var hearing = Hear(Garbled, ["SYM_TREE"], Answer("NOUN_FOREST", "VERB_PROTECT")).Hearing!;

        Assert.Equal("SYM_TREE", hearing.Teaching!.TargetSymbolId);
        Assert.Equal(DogmaType.Destruction, hearing.Teaching.Type);
        Assert.Equal([new DialectEntry("VERB_PROTECT", "VERB_CUT", DialectReason.Dialect)], hearing.BookEntries);
    }

    [Fact]
    [Trait("TS", "TS-oracle-dialect-004")]
    public void Hear_動詞が無く名詞がずれ表に当たる_隣の記号の教えが既定の崇拝になる()
    {
        var hearing = Hear(Garbled, ["SYM_TREE"], Answer("NOUN_FOREST")).Hearing!;

        Assert.Equal("SYM_WATER", hearing.Teaching!.TargetSymbolId);
        Assert.Equal(DogmaType.Worship, hearing.Teaching.Type);
        Assert.Equal(0.7f, hearing.Teaching.Intensity, Tolerance);
    }

    [Fact]
    [Trait("TS", "TS-oracle-dialect-005")]
    public void Hear_手札に無い記号の名詞_知らない言葉が先に当たりずれ表は使わない()
    {
        var hearing = Hear(Garbled, ["SYM_TREE"], Answer("NOUN_FLAME", "VERB_PROTECT", substitute: "NOUN_FOREST")).Hearing!;

        Assert.Equal(new Dogma("SYM_TREE", DogmaType.Worship, 0.7f), hearing.Teaching);
        Assert.Equal([new DialectEntry("NOUN_FLAME", "NOUN_FOREST", DialectReason.Unknown)], hearing.BookEntries);
    }

    [Fact]
    [Trait("TS", "TS-oracle-dialect-005")]
    public void Hear_知らない言葉の聞き違え先_手札の外か隣の外か無いときは拒む()
    {
        // 水は手札に無く、水の隣の木は手札にある。火は水の隣ではないので選べない
        string[] hand = ["SYM_TREE", "SYM_FIRE"];

        Assert.Equal("substitute_required", Hear(Silent, hand, Answer("NOUN_RIVER")).RejectedReason);
        Assert.Equal("invalid_substitute", Hear(Silent, hand, Answer("NOUN_RIVER", substitute: "NOUN_FLAME")).RejectedReason);
        Assert.Equal("invalid_substitute", Hear(Silent, hand, Answer("NOUN_RIVER", substitute: "NOUN_RIVER")).RejectedReason);
        Assert.Null(Hear(Silent, hand, Answer("NOUN_RIVER", substitute: "NOUN_FOREST")).RejectedReason);
    }

    [Fact]
    [Trait("TS", "TS-oracle-dialect-006")]
    public void Hear_謎の言葉_1か所のずれに数えず方言帳と経典の元に残る()
    {
        var hearing = Hear(Garbled, ["SYM_TREE"], Answer("NOUN_FOREST", "VERB_PROTECT", mysteries: ["電気"])).Hearing!;

        Assert.Equal(
            [new DialectEntry("VERB_PROTECT", "VERB_CUT", DialectReason.Dialect), new DialectEntry("電気", null, DialectReason.Mystery)],
            hearing.BookEntries);
        Assert.Equal(["電気"], hearing.MysteryWords);
    }

    [Fact]
    [Trait("TS", "TS-oracle-dialect-007")]
    public void Hear_行き先_方角は通じ土地の名と北南はなしに聞き違え行き先だけの神託も聞ける()
    {
        var east = Hear(Silent, ["SYM_TREE"], Answer(destination: HeardDestination.East)).Hearing!;
        var north = Hear(Silent, ["SYM_TREE"], Answer("NOUN_FOREST", destination: HeardDestination.North)).Hearing!;

        Assert.Null(east.Teaching);
        Assert.Equal(TravelDirection.East, east.Destination);
        Assert.False(east.DestinationMisheard);
        Assert.Equal(TravelDirection.None, north.Destination);
        Assert.True(north.DestinationMisheard);
        Assert.NotNull(north.Teaching);
    }

    [Fact]
    [Trait("TS", "TS-oracle-dialect-008")]
    public void Hear_壊れた答え_定義に無いIDと長すぎる謎の言葉と何も聞こえない答えは拒む()
    {
        string[] hand = ["SYM_TREE"];

        Assert.Equal("unknown_word", Hear(Silent, hand, Answer("NOUN_UNKNOWN")).RejectedReason);
        Assert.Equal("unknown_word", Hear(Silent, hand, Answer(verb: "VERB_UNKNOWN")).RejectedReason);
        Assert.Equal("invalid_mystery_word", Hear(Silent, hand, Answer("NOUN_FOREST", mysteries: [new string('あ', OracleHearer.MaxMysteryWordLength + 1)])).RejectedReason);
        Assert.Equal("invalid_mystery_word", Hear(Silent, hand, Answer("NOUN_FOREST", mysteries: ["電\n気"])).RejectedReason);
        Assert.Equal("nothing_heard", Hear(Silent, hand, Answer(mysteries: ["電気"])).RejectedReason);
    }

    [Fact]
    [Trait("TS", "TS-oracle-dialect-009")]
    public void Add_同じ行_2度目は足さない()
    {
        var book = new DialectBook();
        var entry = new DialectEntry("VERB_PROTECT", "VERB_CUT", DialectReason.Dialect);

        Assert.True(book.Add(entry));
        Assert.False(book.Add(entry with { }));
        Assert.Single(book.Entries);
    }
}
