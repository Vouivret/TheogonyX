using TheogonyX.Core;

namespace TheogonyX.Core.Tests;

public class ScriptTests
{
    private const int Seed = 42;

    internal static readonly ScriptRules Rules = new(GridSize: 4, MinStrokes: 3, MaxStrokes: 6, DecayYears: [300, 600, 900], TeachingIntensity: 0.7f, MaxStelesPerRegion: 3, MisreadShare: 0.3f, StyleJitter: 0.1f);

    internal static readonly string[] Symbols = ["SYM_TREE", "SYM_WATER", "SYM_BLOOD", "SYM_FIRE", "SYM_EARTH", "SYM_METAL"];

    private static readonly HashSet<string> AllHand = [.. Symbols];

    private static PlanetScript Create(int seed = Seed) => PlanetScript.Create(Rules, Symbols, seed);

    private static HashSet<string> AllRadicals(PlanetScript script) => [.. script.Radicals.Select(radical => radical.Id)];

    private static string Id(PlanetScript script, RadicalKind kind, string key) => script.Find(kind, key)!.Id;

    private static PlanetScript CreateWith(WordOrder order) =>
        Enumerable.Range(0, 100).Select(seed => Create(seed)).First(script => script.WordOrder == order);

    private static ScriptReading Read(PlanetScript script, IReadOnlyCollection<string>? hand = null, int stage = 0, params string[] words) =>
        ScriptReader.Read(script, Rules, new Sentence(words), AllRadicals(script), hand ?? AllHand, stage).Reading!;

    private static string Shape(IEnumerable<Stroke> strokes) => string.Join(';', strokes.Order());

    [Fact]
    [Trait("TS", "TS-script-decipher-001")]
    public void Create_同じシード_同じ字になり違うシードでは違う()
    {
        var first = Create();
        var second = Create();
        var other = Create(seed: 7);

        Assert.Equal(first.WordOrder, second.WordOrder);
        Assert.Equal(first.SimilarPairs, second.SimilarPairs);
        Assert.Equal(
            first.Radicals.Select(radical => (radical.Id, radical.Kind, radical.Key, Shape(radical.Strokes), radical.DecayStroke, radical.DecayStage)),
            second.Radicals.Select(radical => (radical.Id, radical.Kind, radical.Key, Shape(radical.Strokes), radical.DecayStroke, radical.DecayStage)));
        Assert.NotEqual(
            first.Radicals.Select(radical => Shape(radical.Strokes)),
            other.Radicals.Select(radical => Shape(radical.Strokes)));
    }

    [Fact]
    [Trait("TS", "TS-script-decipher-002")]
    public void Create_多くのシード_語は11個で形が重ならず組は回ごとに名詞名詞動詞で混ざるまで別の形()
    {
        for (var seed = 0; seed < 50; seed++)
        {
            var script = Create(seed);

            Assert.Equal(11, script.Radicals.Count);
            Assert.All(script.Radicals, radical => Assert.InRange(radical.Strokes.Count, 3, 6));
            Assert.All(script.Radicals, radical => Assert.InRange(radical.DecayStage, 1, 3));
            Assert.Equal(3, script.SimilarPairs.Count);
            Assert.Equal(6, script.SimilarPairs.SelectMany(pair => new[] { pair.A, pair.B }).Distinct().Count());
            RadicalKind[] kinds = [RadicalKind.Meaning, RadicalKind.Meaning, RadicalKind.Function];
            for (var i = 0; i < 3; i++)
            {
                var (radicalA, radicalB) = (script.Find(script.SimilarPairs[i].A)!, script.Find(script.SimilarPairs[i].B)!);
                Assert.Equal(kinds[i], radicalA.Kind);
                Assert.Equal(kinds[i], radicalB.Kind);
                Assert.Equal(i + 1, radicalA.DecayStage);
                Assert.Equal(i + 1, radicalB.DecayStage);
                Assert.Single(radicalA.Strokes.Except(radicalB.Strokes));
                Assert.NotEqual(Shape(radicalA.ShapeIn(i)), Shape(radicalB.ShapeIn(i)));
                Assert.Equal(Shape(radicalA.ShapeIn(i + 1)), Shape(radicalB.ShapeIn(i + 1)));
            }
            // どの時代も、形が同じになるのは、その時代までに混ざった組だけ
            for (var stage = 0; stage <= 3; stage++)
            {
                Assert.Equal(11 - stage, script.Radicals.Select(radical => Shape(radical.ShapeIn(stage))).Distinct().Count());
            }
        }
    }

    [Fact]
    [Trait("TS", "TS-script-decipher-002")]
    public void ShapeIn_語の崩れる回の前後_その回から線が1本消え後の回では変わらない()
    {
        var radical = Create().Radicals.First(candidate => candidate.DecayStage == 2);

        Assert.Equal(radical.Strokes.Count, radical.ShapeIn(1).Count);
        Assert.Equal(radical.Strokes.Count - 1, radical.ShapeIn(2).Count);
        Assert.DoesNotContain(radical.DecayStroke, radical.ShapeIn(2));
        Assert.Equal(radical.ShapeIn(2), radical.ShapeIn(3));
    }

    [Theory]
    [Trait("TS", "TS-script-decipher-003")]
    [InlineData(WordOrder.NounFirst)]
    [InlineData(WordOrder.VerbFirst)]
    public void Read_星の語順の木と触れるなの文_木の禁忌になる(WordOrder order)
    {
        var script = CreateWith(order);

        var reading = Read(script, words: [.. script.SentenceFor("SYM_TREE", DogmaType.Taboo).Words]);

        Assert.Equal(new Dogma("SYM_TREE", DogmaType.Taboo, 0.7f), reading.Teaching);
        Assert.Equal(new PhraseReading("SYM_TREE", DogmaType.Taboo, true), Assert.Single(reading.Phrases));
        Assert.Equal(0, reading.DroppedVerbs);
        Assert.Equal(TravelDirection.None, reading.Destination);
    }

    [Theory]
    [Trait("TS", "TS-script-decipher-004")]
    [InlineData(WordOrder.NounFirst)]
    [InlineData(WordOrder.VerbFirst)]
    public void Read_語順が逆の文_動詞は読み落とされ名詞だけが崇拝と読まれる(WordOrder order)
    {
        var script = CreateWith(order);
        var (tree, taboo) = (Id(script, RadicalKind.Meaning, "SYM_TREE"), Id(script, RadicalKind.Function, nameof(DogmaType.Taboo)));
        string[] reversed = order == WordOrder.NounFirst ? [taboo, tree] : [tree, taboo];

        var reading = Read(script, words: reversed);

        Assert.Equal(new Dogma("SYM_TREE", DogmaType.Worship, 0.7f), reading.Teaching);
        Assert.Equal(new PhraseReading("SYM_TREE", DogmaType.Worship, false), Assert.Single(reading.Phrases));
        Assert.Equal(1, reading.DroppedVerbs);
    }

    [Fact]
    [Trait("TS", "TS-script-decipher-004")]
    public void Read_方角の語が文の頭や組の間にある_行き先になり組はつながったまま()
    {
        var script = CreateWith(WordOrder.NounFirst);
        var (tree, taboo) = (Id(script, RadicalKind.Meaning, "SYM_TREE"), Id(script, RadicalKind.Function, nameof(DogmaType.Taboo)));
        var east = Id(script, RadicalKind.Direction, nameof(TravelDirection.East));

        var head = Read(script, words: [east, tree, taboo]);
        var middle = Read(script, words: [tree, east, taboo]);

        Assert.Equal(TravelDirection.East, head.Destination);
        Assert.Equal(TravelDirection.East, middle.Destination);
        Assert.Equal(new Dogma("SYM_TREE", DogmaType.Taboo, 0.7f), head.Teaching);
        Assert.Equal(new Dogma("SYM_TREE", DogmaType.Taboo, 0.7f), middle.Teaching);
    }

    [Fact]
    [Trait("TS", "TS-script-decipher-004")]
    public void Read_予言者の知らない記号_謎の語になり教えは後の読める組から作る()
    {
        var script = Create();

        var reading = Read(
            script, hand: ["SYM_WATER"], words: [.. script.SentenceFor("SYM_TREE", DogmaType.Destruction).Words, Id(script, RadicalKind.Meaning, "SYM_WATER")]);

        Assert.False(reading.Phrases[0].IsReadable);
        Assert.Equal(new Dogma("SYM_WATER", DogmaType.Worship, 0.7f), reading.Teaching);
    }

    [Fact]
    [Trait("TS", "TS-script-decipher-004")]
    public void Read_名詞だけの語と動詞つきの組と方角_教えは動詞つきの組で行き先は方角()
    {
        var script = Create();

        var reading = Read(
            script,
            words: [Id(script, RadicalKind.Meaning, "SYM_FIRE"), .. script.SentenceFor("SYM_WATER", DogmaType.Taboo, TravelDirection.East).Words]);

        Assert.Equal(new Dogma("SYM_WATER", DogmaType.Taboo, 0.7f), reading.Teaching);
        Assert.Equal(TravelDirection.East, reading.Destination);
        Assert.Equal(2, reading.Phrases.Count);
    }

    [Fact]
    [Trait("TS", "TS-script-decipher-005")]
    public void Read_崩れた後の似た形の組_どちらの語で書いてもIDの小さい方として読まれる()
    {
        var script = Create();
        var (a, b) = script.SimilarPairs[0];
        var smaller = string.CompareOrdinal(a, b) < 0 ? a : b;
        var larger = smaller == a ? b : a;
        var smallerSymbol = script.Find(smaller)!.Key;
        var largerSymbol = script.Find(larger)!.Key;

        var before = Read(script, words: larger);
        var after = Read(script, stage: 1, words: larger);

        Assert.Equal(largerSymbol, before.Teaching!.TargetSymbolId);
        Assert.Equal(smallerSymbol, after.Teaching!.TargetSymbolId);
    }

    [Fact]
    [Trait("TS", "TS-script-decipher-005")]
    public void Read_2回目の組の語_1回目の時代では混ざらず2回目の時代で混ざる()
    {
        var script = Create();
        var (a, b) = script.SimilarPairs[1];
        var (smaller, larger) = string.CompareOrdinal(a, b) < 0 ? (a, b) : (b, a);

        Assert.Equal(script.Find(larger)!.Key, Read(script, stage: 1, words: larger).Teaching!.TargetSymbolId);
        Assert.Equal(script.Find(smaller)!.Key, Read(script, stage: 2, words: larger).Teaching!.TargetSymbolId);
    }

    [Fact]
    [Trait("TS", "TS-script-decipher-023")]
    public void Read_3回目に混ざる動詞_同じ文が別の種別の教えに読まれる()
    {
        var script = Create();
        var (a, b) = script.SimilarPairs[2];
        var (smaller, larger) = string.CompareOrdinal(a, b) < 0 ? (a, b) : (b, a);
        var (smallerType, largerType) = (Enum.Parse<DogmaType>(script.Find(smaller)!.Key), Enum.Parse<DogmaType>(script.Find(larger)!.Key));
        var sentence = script.SentenceFor("SYM_TREE", largerType).Words.ToArray();

        Assert.Equal(largerType, Read(script, stage: 2, words: sentence).Teaching!.Type);
        Assert.Equal(smallerType, Read(script, stage: 3, words: sentence).Teaching!.Type);
    }

    [Fact]
    [Trait("TS", "TS-script-decipher-006")]
    public void Read_語が無い多すぎる手帳に無い語_理由つきで拒む()
    {
        var script = Create();
        var tree = Id(script, RadicalKind.Meaning, "SYM_TREE");
        var known = new HashSet<string> { tree };
        Sentence Of(params string[] words) => new(words);

        Assert.Equal("no_glyphs", ScriptReader.Read(script, Rules, Of(), AllRadicals(script), AllHand, 0).RejectedReason);
        Assert.Equal("too_many_glyphs", ScriptReader.Read(script, Rules, Of(tree, tree, tree, tree, tree), AllRadicals(script), AllHand, 0).RejectedReason);
        Assert.Null(ScriptReader.Read(script, Rules, Of(tree, tree, tree, tree), AllRadicals(script), AllHand, 0).RejectedReason);
        Assert.Equal("glyph_not_found", ScriptReader.Read(script, Rules, script.SentenceFor("SYM_TREE", DogmaType.Worship), known, AllHand, 0).RejectedReason);
        Assert.Equal("glyph_not_found", ScriptReader.Read(script, Rules, Of("RAD_99"), AllRadicals(script), AllHand, 0).RejectedReason);
    }

    [Fact]
    [Trait("TS", "TS-script-decipher-007")]
    public void PrayerSentence_渇きの依頼_初めに水と崇めよで後は選んだ記号の名詞()
    {
        var script = Create();
        var thirst = TestRules.ProphetDreams.Requests.First(request => request.Id == "THIRST");
        string Noun(string symbolId) => Id(script, RadicalKind.Meaning, symbolId);

        var prayer = ScriptReader.PrayerSentence(script, thirst, ["SYM_TREE", "SYM_WATER", "SYM_FIRE"]);
        var daily = ScriptReader.PrayerSentence(script, null, ["SYM_TREE"]);

        Assert.Equal(new Sentence([.. script.SentenceFor("SYM_WATER", DogmaType.Worship).Words, Noun("SYM_TREE"), Noun("SYM_FIRE")]), prayer);
        Assert.Equal(new Sentence([Noun("SYM_TREE")]), daily);
    }

    [Fact]
    [Trait("TS", "TS-script-decipher-008")]
    public void Restore_作った字の部品_同じ字に戻り定義に合わない字は拒む()
    {
        var script = Create();

        var restored = PlanetScript.Restore(Rules, script.Radicals, script.WordOrder, script.SimilarPairs);

        Assert.Equal(script.Radicals, restored.Radicals);
        Assert.Throws<ArgumentException>(() => PlanetScript.Restore(Rules, script.Radicals, script.WordOrder, [(script.SimilarPairs[0].A, script.SimilarPairs[0].A)]));
        Assert.Throws<ArgumentException>(() => PlanetScript.Restore(Rules, script.Radicals, script.WordOrder, []));
        Assert.Throws<ArgumentException>(() => PlanetScript.Restore(Rules, script.Radicals, script.WordOrder, [script.SimilarPairs[1]])); // 組の回と語の回が合わない
        var lateStage = script.Radicals[0] with { DecayStage = 4 };
        Assert.Throws<ArgumentException>(() => PlanetScript.Restore(Rules, [lateStage, .. script.Radicals.Skip(1)], script.WordOrder, script.SimilarPairs));
        Assert.Throws<ArgumentException>(() => PlanetScript.Restore(
            Rules, [.. script.Radicals.Where(radical => radical.Kind != RadicalKind.Direction)], script.WordOrder, script.SimilarPairs));
        var outside = script.Radicals[0] with { Strokes = [new Stroke(0, 0, 9, 9), .. script.Radicals[0].Strokes] };
        Assert.Throws<ArgumentException>(() => PlanetScript.Restore(Rules, [outside, .. script.Radicals.Skip(1)], script.WordOrder, script.SimilarPairs));
    }
}
