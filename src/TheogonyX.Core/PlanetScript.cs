namespace TheogonyX.Core;

/// <summary>語順の決まり: 名詞が先（水 崇めよ）か、動詞が先（崇めよ 水）か。</summary>
public enum WordOrder
{
    NounFirst,
    VerbFirst,
}

/// <summary>
/// 星の字（docs/domain/script-decipher.md の「字のしくみ」と「7. 語の字と神託の碑」）。語の字の形・語順の決まり・似た形の組・崩れの線を、
/// 星のシードから作る。語の字は <see cref="ScriptRadical"/> で表す（名詞は <see cref="RadicalKind.Meaning"/>、動詞は <see cref="RadicalKind.Function"/>）。
/// 作った後は変えない。セーブには形そのものを残し、読み込みでシードから作り直さない。
/// </summary>
public sealed class PlanetScript
{
    // 字を作る乱数は、ほかの乱数と分ける
    private const int SeedSalt = 0x53435249; // "SCRI"

    private static readonly string[] FunctionKeys = [nameof(DogmaType.Worship), nameof(DogmaType.Taboo), nameof(DogmaType.Destruction)];
    private static readonly string[] DirectionKeys = [nameof(TravelDirection.East), nameof(TravelDirection.West)];

    private PlanetScript(IReadOnlyList<ScriptRadical> radicals, WordOrder wordOrder, (string A, string B) similarPair)
    {
        Radicals = radicals;
        WordOrder = wordOrder;
        SimilarPair = similarPair;
    }

    /// <summary>語の字（作った順。順番に意味は無い）。</summary>
    public IReadOnlyList<ScriptRadical> Radicals { get; }

    /// <summary>人の文の語順の決まり。</summary>
    public WordOrder WordOrder { get; }

    /// <summary>似た形の組（名詞 2 つ。線が 1 本だけ違い、崩れると同じ形になる）。</summary>
    public (string A, string B) SimilarPair { get; }

    public ScriptRadical? Find(string radicalId) => Radicals.FirstOrDefault(radical => radical.Id == radicalId);

    /// <summary>種類と意味（記号の ID・種別の名前・方角の名前）の部品。無ければ null。</summary>
    public ScriptRadical? Find(RadicalKind kind, string key) => Radicals.FirstOrDefault(radical => radical.Kind == kind && radical.Key == key);

    /// <summary>人が書く、星の語順の文（名詞、または名詞と動詞の組。方角の語は最後に置く）。</summary>
    public Sentence SentenceFor(string symbolId, DogmaType? type = null, TravelDirection direction = TravelDirection.None)
    {
        var noun = Find(RadicalKind.Meaning, symbolId) ?? throw new ArgumentException($"字の無い記号: symbolId={symbolId}", nameof(symbolId));
        var words = new List<string> { noun.Id };
        if (type is { } dogmaType)
        {
            words.Insert(WordOrder == WordOrder.NounFirst ? 1 : 0, Find(RadicalKind.Function, dogmaType.ToString())!.Id);
        }
        if (direction != TravelDirection.None)
        {
            words.Add(Find(RadicalKind.Direction, direction.ToString())!.Id);
        }
        return new Sentence(words);
    }

    /// <summary>星のシードから字を作る。同じ入力なら同じ字になる。</summary>
    /// <param name="symbolIds">意味の部品を作る記号（定義の順）。2 つ以上。</param>
    /// <exception cref="ArgumentException">記号が 2 つ未満。</exception>
    /// <exception cref="InvalidOperationException">作り直しの上限を越えた（格子が小さすぎるなど、定義が悪い）。</exception>
    public static PlanetScript Create(ScriptRules rules, IReadOnlyList<string> symbolIds, int seed)
    {
        ArgumentNullException.ThrowIfNull(rules);
        ArgumentNullException.ThrowIfNull(symbolIds);
        if (symbolIds.Count < 2)
        {
            throw new ArgumentException("似た形の組を作るには、記号が 2 つ以上要る", nameof(symbolIds));
        }
        var random = new Random(unchecked(seed + SeedSalt));
        var specs = symbolIds.Select(id => (Kind: RadicalKind.Meaning, Key: id))
            .Concat(FunctionKeys.Select(key => (Kind: RadicalKind.Function, Key: key)))
            .Concat(DirectionKeys.Select(key => (Kind: RadicalKind.Direction, Key: key)))
            .ToList();

        // ID は意味を表さないよう、番号を混ぜて振る
        var numbers = Enumerable.Range(1, specs.Count).OrderBy(_ => random.Next()).ToList();
        var pairIndex = random.Next(symbolIds.Count);
        var pairOther = (pairIndex + 1 + random.Next(symbolIds.Count - 1)) % symbolIds.Count;

        var shapes = new List<(List<Stroke> Strokes, Stroke Decay)>();
        var taken = new HashSet<string>();
        foreach (var _ in specs)
        {
            for (var attempt = 0; ; attempt++)
            {
                ThrowIfTooMany(attempt, rules);
                var strokes = RandomStrokes(rules, random);
                var decay = strokes[random.Next(strokes.Count)];
                var (whole, decayed) = (Signature(strokes), Signature(strokes.Where(stroke => stroke != decay)));
                if (!taken.Contains(whole) && !taken.Contains(decayed))
                {
                    taken.Add(whole);
                    taken.Add(decayed);
                    shapes.Add((strokes, decay));
                    break;
                }
            }
        }

        // 似た形の組: 後の方を、前の方の線を 1 本だけ替えた形にする。前の方は替えた線が、後の方は新しい線が崩れで消え、同じ形になる
        for (var attempt = 0; ; attempt++)
        {
            ThrowIfTooMany(attempt, rules);
            var original = shapes[pairIndex].Strokes;
            var (twin, added) = Twin(original, rules, random, out var removed);
            var others = shapes.Where((_, i) => i != pairIndex && i != pairOther)
                .SelectMany(shape => new[] { Signature(shape.Strokes), Signature(shape.Strokes.Where(stroke => stroke != shape.Decay)) })
                .ToHashSet();
            var (twinWhole, merged) = (Signature(twin), Signature(original.Where(stroke => stroke != removed)));
            if (!others.Contains(twinWhole) && !others.Contains(merged) && twinWhole != Signature(original))
            {
                shapes[pairIndex] = (original, removed);
                shapes[pairOther] = (twin, added);
                break;
            }
        }

        var radicals = specs
            .Select((spec, i) => new ScriptRadical($"RAD_{numbers[i]:00}", spec.Kind, spec.Key, shapes[i].Strokes, shapes[i].Decay))
            .ToList();
        var wordOrder = random.Next(2) == 0 ? WordOrder.NounFirst : WordOrder.VerbFirst;
        return new PlanetScript(radicals, wordOrder, (radicals[pairIndex].Id, radicals[pairOther].Id));
    }

    /// <summary>セーブから読み込んだ字に置き換える（シードから作り直さない）。</summary>
    /// <exception cref="ArgumentException">ID が重なる・語の種類が足りない・似た形の組が名詞でない・線が格子の外。</exception>
    public static PlanetScript Restore(ScriptRules rules, IReadOnlyList<ScriptRadical> radicals, WordOrder wordOrder, (string A, string B) similarPair)
    {
        ArgumentNullException.ThrowIfNull(rules);
        ArgumentNullException.ThrowIfNull(radicals);
        bool InGrid(int value) => value >= 0 && value < rules.GridSize;
        var valid = radicals.Select(radical => radical.Id).Distinct().Count() == radicals.Count
            && radicals.All(radical => radical.Strokes.Count >= 2 && radical.Strokes.Contains(radical.DecayStroke)
                && radical.Strokes.All(stroke => InGrid(stroke.X1) && InGrid(stroke.Y1) && InGrid(stroke.X2) && InGrid(stroke.Y2)))
            && FunctionKeys.All(key => radicals.Any(radical => radical.Kind == RadicalKind.Function && radical.Key == key))
            && DirectionKeys.All(key => radicals.Any(radical => radical.Kind == RadicalKind.Direction && radical.Key == key))
            && similarPair.A != similarPair.B
            && radicals.Any(radical => radical.Id == similarPair.A && radical.Kind == RadicalKind.Meaning)
            && radicals.Any(radical => radical.Id == similarPair.B && radical.Kind == RadicalKind.Meaning)
            && Enum.IsDefined(wordOrder);
        if (!valid)
        {
            throw new ArgumentException("字の定義が正しくない", nameof(radicals));
        }
        return new PlanetScript([.. radicals], wordOrder, similarPair);
    }

    private static void ThrowIfTooMany(int attempt, ScriptRules rules)
    {
        if (attempt >= rules.MaxAttempts)
        {
            throw new InvalidOperationException($"字の部品の形を作れない: rules={rules}");
        }
    }

    private static List<Stroke> RandomStrokes(ScriptRules rules, Random random)
    {
        var count = random.Next(rules.MinStrokes, rules.MaxStrokes + 1);
        var strokes = new List<Stroke>();
        while (strokes.Count < count)
        {
            var stroke = RandomStroke(rules, random);
            if (!strokes.Contains(stroke))
            {
                strokes.Add(stroke);
            }
        }
        return strokes;
    }

    private static Stroke RandomStroke(ScriptRules rules, Random random)
    {
        while (true)
        {
            int Coordinate() => random.Next(rules.GridSize);
            var (x1, y1, x2, y2) = (Coordinate(), Coordinate(), Coordinate(), Coordinate());
            if ((x1, y1) != (x2, y2))
            {
                return Stroke.Of(x1, y1, x2, y2);
            }
        }
    }

    // 元の線のうち 1 本（removed）を、新しい線（返す Decay）に替えた形。元は removed、新しい方は Decay が消えると同じ形になる
    private static (List<Stroke> Strokes, Stroke Decay) Twin(List<Stroke> original, ScriptRules rules, Random random, out Stroke removed)
    {
        var index = random.Next(original.Count);
        removed = original[index];
        Stroke added;
        do
        {
            added = RandomStroke(rules, random);
        }
        while (original.Contains(added));
        var strokes = original.ToList();
        strokes[index] = added;
        return (strokes, added);
    }

    private static string Signature(IEnumerable<Stroke> strokes) =>
        string.Join(';', strokes.Order().Select(stroke => $"{stroke.X1},{stroke.Y1},{stroke.X2},{stroke.Y2}"));
}
