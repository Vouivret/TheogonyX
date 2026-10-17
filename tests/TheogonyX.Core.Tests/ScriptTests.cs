using TheogonyX.Core;

namespace TheogonyX.Core.Tests;

public class ScriptTests
{
    private const int Seed = 42;

    internal static readonly ScriptRules Rules = new(GridSize: 4, MinStrokes: 3, MaxStrokes: 6, DecayYears: 300, TeachingIntensity: 0.7f);

    internal static readonly string[] Symbols = ["SYM_TREE", "SYM_WATER", "SYM_BLOOD", "SYM_FIRE", "SYM_EARTH", "SYM_METAL"];

    private static readonly HashSet<string> AllHand = [.. Symbols];

    private static PlanetScript Create(int seed = Seed) => PlanetScript.Create(Rules, Symbols, seed);

    private static HashSet<string> AllRadicals(PlanetScript script) => [.. script.Radicals.Select(radical => radical.Id)];

    private static string Id(PlanetScript script, RadicalKind kind, string key) => script.Find(kind, key)!.Id;

    private static GlyphArrangement Other(GlyphArrangement arrangement) =>
        arrangement == GlyphArrangement.LeftRight ? GlyphArrangement.TopBottom : GlyphArrangement.LeftRight;

    private static ScriptReading Read(PlanetScript script, IReadOnlyCollection<string>? hand = null, bool decayed = false, params Glyph[] glyphs) =>
        ScriptReader.Read(script, Rules, glyphs, AllRadicals(script), hand ?? AllHand, decayed).Reading!;

    private static string Shape(IEnumerable<Stroke> strokes) => string.Join(';', strokes.Order());

    [Fact]
    [Trait("TS", "TS-script-decipher-001")]
    public void Create_同じシード_同じ字になり違うシードでは違う()
    {
        var first = Create();
        var second = Create();
        var other = Create(seed: 7);

        Assert.Equal(first.Arrangement, second.Arrangement);
        Assert.Equal(first.SimilarPair, second.SimilarPair);
        Assert.Equal(
            first.Radicals.Select(radical => (radical.Id, radical.Kind, radical.Key, Shape(radical.Strokes), radical.DecayStroke)),
            second.Radicals.Select(radical => (radical.Id, radical.Kind, radical.Key, Shape(radical.Strokes), radical.DecayStroke)));
        Assert.NotEqual(
            first.Radicals.Select(radical => Shape(radical.Strokes)),
            other.Radicals.Select(radical => Shape(radical.Strokes)));
    }

    [Fact]
    [Trait("TS", "TS-script-decipher-002")]
    public void Create_多くのシード_部品は11個で形が重ならず似た組だけが1本違いで崩れると同じ形になる()
    {
        for (var seed = 0; seed < 50; seed++)
        {
            var script = Create(seed);
            var (a, b) = script.SimilarPair;
            var pair = new[] { a, b };

            Assert.Equal(11, script.Radicals.Count);
            Assert.All(script.Radicals, radical => Assert.InRange(radical.Strokes.Count, 3, 6));
            Assert.Equal(11, script.Radicals.Select(radical => Shape(radical.ShapeIn(false))).Distinct().Count());
            var decayedOthers = script.Radicals.Where(radical => !pair.Contains(radical.Id)).Select(radical => Shape(radical.ShapeIn(true))).ToList();
            Assert.Equal(9, decayedOthers.Distinct().Count());
            var (radicalA, radicalB) = (script.Find(a)!, script.Find(b)!);
            Assert.Equal(RadicalKind.Meaning, radicalA.Kind);
            Assert.Equal(RadicalKind.Meaning, radicalB.Kind);
            Assert.Single(radicalA.Strokes.Except(radicalB.Strokes));
            Assert.Equal(Shape(radicalA.ShapeIn(true)), Shape(radicalB.ShapeIn(true)));
        }
    }

    [Fact]
    [Trait("TS", "TS-script-decipher-003")]
    public void Read_決まりどおりの木と崇拝の字_木の崇拝になる()
    {
        var script = Create();

        var reading = Read(script, glyphs: script.GlyphFor("SYM_TREE", DogmaType.Worship));

        Assert.Equal(new Dogma("SYM_TREE", DogmaType.Worship, 0.7f), reading.Teaching);
        Assert.Equal(TravelDirection.None, reading.Destination);
    }

    [Fact]
    [Trait("TS", "TS-script-decipher-004")]
    public void Read_並びが決まりと違う_部品の場所が入れ替わって読まれ意味の場所に働きの部品があれば謎の字()
    {
        var script = Create();
        var (tree, worship) = (Id(script, RadicalKind.Meaning, "SYM_TREE"), Id(script, RadicalKind.Function, nameof(DogmaType.Worship)));

        var wrong = Read(script, glyphs: new Glyph(tree, worship, null, Other(script.Arrangement)));
        var swappedBack = Read(script, glyphs: new Glyph(worship, tree, null, Other(script.Arrangement)));
        var meaningOnly = Read(script, glyphs: new Glyph(tree, null, null, Other(script.Arrangement)));

        Assert.Null(wrong.Teaching);
        Assert.False(wrong.Glyphs[0].IsReadable);
        Assert.Equal(new Dogma("SYM_TREE", DogmaType.Worship, 0.7f), swappedBack.Teaching);
        Assert.Equal(new Dogma("SYM_TREE", DogmaType.Worship, 0.7f), meaningOnly.Teaching);
    }

    [Fact]
    [Trait("TS", "TS-script-decipher-004")]
    public void Read_予言者の知らない記号_謎の字になり教えは後の読める字から作る()
    {
        var script = Create();

        var reading = Read(
            script, hand: ["SYM_WATER"], glyphs: [script.GlyphFor("SYM_TREE", DogmaType.Destruction), script.GlyphFor("SYM_WATER")]);

        Assert.False(reading.Glyphs[0].IsReadable);
        Assert.Equal(new Dogma("SYM_WATER", DogmaType.Worship, 0.7f), reading.Teaching);
    }

    [Fact]
    [Trait("TS", "TS-script-decipher-004")]
    public void Read_意味だけの字と働きつきの字と方角_教えは働きつきの字で行き先は方角()
    {
        var script = Create();

        var reading = Read(
            script,
            glyphs: [script.GlyphFor("SYM_FIRE"), script.GlyphFor("SYM_WATER", DogmaType.Taboo), script.GlyphFor("SYM_EARTH", direction: TravelDirection.East)]);

        Assert.Equal(new Dogma("SYM_WATER", DogmaType.Taboo, 0.7f), reading.Teaching);
        Assert.Equal(TravelDirection.East, reading.Destination);
    }

    [Fact]
    [Trait("TS", "TS-script-decipher-005")]
    public void Read_崩れた後の似た形の組_どちらの部品で書いてもIDの小さい方として読まれる()
    {
        var script = Create();
        var (a, b) = script.SimilarPair;
        var smaller = string.CompareOrdinal(a, b) < 0 ? a : b;
        var larger = smaller == a ? b : a;
        var smallerSymbol = script.Find(smaller)!.Key;
        var largerSymbol = script.Find(larger)!.Key;

        var before = Read(script, glyphs: new Glyph(larger, null, null, script.Arrangement));
        var after = Read(script, decayed: true, glyphs: new Glyph(larger, null, null, script.Arrangement));

        Assert.Equal(largerSymbol, before.Teaching!.TargetSymbolId);
        Assert.Equal(smallerSymbol, after.Teaching!.TargetSymbolId);
    }

    [Fact]
    [Trait("TS", "TS-script-decipher-006")]
    public void Read_字が無い多すぎる手帳に無い部品_理由つきで拒む()
    {
        var script = Create();
        var glyph = script.GlyphFor("SYM_TREE", DogmaType.Worship);
        var known = new HashSet<string> { glyph.First };

        Assert.Equal("no_glyphs", ScriptReader.Read(script, Rules, [], AllRadicals(script), AllHand, false).RejectedReason);
        Assert.Equal("too_many_glyphs", ScriptReader.Read(script, Rules, [glyph, glyph, glyph, glyph], AllRadicals(script), AllHand, false).RejectedReason);
        Assert.Equal("radical_not_found", ScriptReader.Read(script, Rules, [glyph], known, AllHand, false).RejectedReason);
        Assert.Equal("radical_not_found", ScriptReader.Read(script, Rules, [new Glyph("RAD_99", null, null, script.Arrangement)], AllRadicals(script), AllHand, false).RejectedReason);
    }

    [Fact]
    [Trait("TS", "TS-script-decipher-007")]
    public void PrayerGlyphs_渇きの依頼_1字目が水と崇拝で後は選んだ記号の意味だけの字()
    {
        var script = Create();
        var thirst = TestRules.ProphetDreams.Requests.First(request => request.Id == "THIRST");

        var glyphs = ScriptReader.PrayerGlyphs(script, thirst, ["SYM_TREE", "SYM_WATER", "SYM_FIRE"]);
        var daily = ScriptReader.PrayerGlyphs(script, null, ["SYM_TREE"]);

        Assert.Equal([script.GlyphFor("SYM_WATER", DogmaType.Worship), script.GlyphFor("SYM_TREE"), script.GlyphFor("SYM_FIRE")], glyphs);
        Assert.Equal([script.GlyphFor("SYM_TREE")], daily);
    }

    [Fact]
    [Trait("TS", "TS-script-decipher-008")]
    public void Restore_作った字の部品_同じ字に戻り定義に合わない字は拒む()
    {
        var script = Create();

        var restored = PlanetScript.Restore(Rules, script.Radicals, script.Arrangement, script.SimilarPair);

        Assert.Equal(script.Radicals, restored.Radicals);
        Assert.Throws<ArgumentException>(() => PlanetScript.Restore(Rules, script.Radicals, script.Arrangement, (script.SimilarPair.A, script.SimilarPair.A)));
        Assert.Throws<ArgumentException>(() => PlanetScript.Restore(
            Rules, [.. script.Radicals.Where(radical => radical.Kind != RadicalKind.Direction)], script.Arrangement, script.SimilarPair));
        var outside = script.Radicals[0] with { Strokes = [new Stroke(0, 0, 9, 9), .. script.Radicals[0].Strokes] };
        Assert.Throws<ArgumentException>(() => PlanetScript.Restore(Rules, [outside, .. script.Radicals.Skip(1)], script.Arrangement, script.SimilarPair));
    }
}
