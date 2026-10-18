namespace TheogonyX.Core;

/// <summary>刻まれた字の種類（docs/domain/script-decipher.md の「世界に刻まれた字」）。</summary>
public enum CarvingKind
{
    /// <summary>地形の目印（湖の岸の「水」など）。名詞 1 語。星を作るときに刻む。</summary>
    Landmark,

    /// <summary>工房の札（金＋崇めよ）。</summary>
    Workshop,

    /// <summary>家の札（土）。</summary>
    House,

    /// <summary>祠の札（その地域でいちばん信じられている教え）。</summary>
    Shrine,

    /// <summary>禁忌の札（禁忌を信じる人が多い地域）。</summary>
    Taboo,

    /// <summary>神託の碑（神が書いた文。神託を下したとき、予言者のいる地域に刻む）。</summary>
    Stele,
}

/// <summary>字が崩れた年に、碑が読み違えられた。<paramref name="Written"/> は崩れる前の読み、<paramref name="Read"/> は今の人の読み。</summary>
public sealed record SteleMisreading(CarvedGlyph Stele, Dogma Written, Dogma Read);

/// <summary>地形の目印を刻む場所（データ定義）。</summary>
public sealed record ScriptLandmark(string SymbolId, int Column, int Row);

/// <summary>
/// 世界に刻まれた文 1 つ。<paramref name="Decayed"/> は刻んだ時代の形（崩れる前に刻んだ字は、崩れた後も前の形のまま）。
/// <paramref name="BuildingId"/> は建物の札のとき、その建物の ID（建物が無くなると札も消える）。
/// </summary>
public sealed record CarvedGlyph(CarvingKind Kind, Sentence Sentence, int Region, int Column, int Row, int Year, bool Decayed, int? BuildingId = null);

/// <summary>手帳に神が書き込める意味（語の字ごとに 1 つ選ぶ）。<see cref="Unknown"/> は「分からない」。</summary>
public static class NotebookLabels
{
    public const string Unknown = "UNKNOWN";
}

/// <summary>セーブに残す、星の字の状態。</summary>
public sealed record ScriptSave(
    IReadOnlyList<ScriptRadical> Radicals, WordOrder WordOrder, (string A, string B) SimilarPair, bool Decayed,
    IReadOnlyList<CarvedGlyph> Carvings, IReadOnlyDictionary<string, string> Notebook);

/// <summary>
/// 星の字の状態（docs/domain/script-decipher.md）。字のしくみ・崩れ・世界に刻まれた字・字の手帳を持つ。
/// 刻まれた字は、時間が進んだ後に <see cref="Update"/> で、建物と信仰から書き換える。
/// </summary>
public sealed class ScriptState
{
    private readonly List<CarvedGlyph> _carvings = [];

    // 手帳: 載った語の字の ID → 神の書き込み（意味の一覧のどれか。初めは「分からない」）
    private readonly Dictionary<string, string> _notebook = [];

    /// <param name="regions">地域と列の対応（禁忌の札を刻む列を決める）。</param>
    public ScriptState(ScriptRules rules, PlanetScript script, RegionMap regions, IReadOnlyList<ScriptLandmark> landmarks, float tabooPlateShare)
    {
        ArgumentNullException.ThrowIfNull(rules);
        ArgumentNullException.ThrowIfNull(script);
        ArgumentNullException.ThrowIfNull(regions);
        ArgumentNullException.ThrowIfNull(landmarks);
        Rules = rules;
        Script = script;
        Regions = regions;
        Landmarks = landmarks;
        TabooPlateShare = tabooPlateShare;
        CarveLandmarks();
    }

    public ScriptRules Rules { get; }

    public PlanetScript Script { get; private set; }

    public RegionMap Regions { get; }

    public IReadOnlyList<ScriptLandmark> Landmarks { get; }

    /// <summary>禁忌の札を刻む、地域の住人のうちの信者の割合（これ以上）。</summary>
    public float TabooPlateShare { get; }

    /// <summary>字が崩れた後の時代か。</summary>
    public bool Decayed { get; private set; }

    /// <summary>世界に刻まれた字（刻んだ順）。</summary>
    public IReadOnlyList<CarvedGlyph> Carvings => _carvings;

    /// <summary>手帳に載った語の字と、神の書き込み。</summary>
    public IReadOnlyDictionary<string, string> Notebook => _notebook;

    /// <summary>神が書ける語（手帳に載った語の字）。</summary>
    public IReadOnlySet<string> KnownRadicals => _notebook.Keys.ToHashSet();

    /// <summary>
    /// 時間が進んだ後に呼ぶ。崩れの年を越えたら崩れた時代にし、建物の札・祠の札・禁忌の札を書き換える。
    /// </summary>
    /// <returns>この呼び出しで字が崩れたか（年表に残すのに使う）。</returns>
    public bool Update(int year, IReadOnlyList<Building> buildings, Residents? residents)
    {
        ArgumentNullException.ThrowIfNull(buildings);
        var decayedNow = false;
        if (!Decayed && year >= Rules.DecayYears)
        {
            Decayed = true;
            decayedNow = true;
        }

        // 無くなった建物の札を消す
        var standing = buildings.Select(building => building.Id).ToHashSet();
        _carvings.RemoveAll(carving => carving.BuildingId is { } id && !standing.Contains(id));

        // 家の札は、地域ごとにいちばん古い家（ID の小さい家）にだけ刻む（地表が札で埋まらないように）
        var platedHouses = buildings.Where(building => building.Type.IsHouse())
            .GroupBy(building => building.Region).Select(group => group.Min(building => building.Id)).ToHashSet();
        foreach (var building in buildings.OrderBy(building => building.Id))
        {
            var sentence = building.Type switch
            {
                BuildingType.Workshop => Script.SentenceFor("SYM_METAL", DogmaType.Worship),
                BuildingType.Shrine => MostBelieved(residents, building.Region) is { } teaching ? Script.SentenceFor(teaching.SymbolId, teaching.Type) : null,
                _ when building.Type.IsHouse() && platedHouses.Contains(building.Id) => Script.SentenceFor("SYM_EARTH"),
                _ => null,
            };
            var kind = building.Type switch
            {
                BuildingType.Workshop => CarvingKind.Workshop,
                BuildingType.Shrine => CarvingKind.Shrine,
                _ => CarvingKind.House,
            };
            var index = _carvings.FindIndex(carving => carving.BuildingId == building.Id);
            if (sentence is null)
            {
                // 信者のいない祠は、札を刻まない（刻んであれば消す）
                if (index >= 0)
                {
                    _carvings.RemoveAt(index);
                }
                continue;
            }
            if (index >= 0 && _carvings[index].Sentence == sentence)
            {
                continue;
            }
            if (index >= 0)
            {
                _carvings.RemoveAt(index);
            }
            _carvings.Add(new CarvedGlyph(kind, sentence, building.Region, building.Column, building.Row, year, Decayed, building.Id));
        }

        UpdateTabooPlates(year, residents);
        return decayedNow;
    }

    /// <summary>
    /// 神託の碑を刻む（docs/domain/script-decipher.md の 7）。神が書いた文を、今の時代の形で、地域の左端の列に刻む。
    /// 地域の碑が <see cref="ScriptRules.MaxStelesPerRegion"/> 個あれば、いちばん古い碑を消してから刻む。行は、空いている 1 行目から下へ。
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">地域の外。</exception>
    public CarvedGlyph CarveStele(Sentence sentence, int region, int year)
    {
        ArgumentNullException.ThrowIfNull(sentence);
        ArgumentOutOfRangeException.ThrowIfNegative(region);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(region, Regions.Count);
        var steles = _carvings.Where(carving => carving.Kind == CarvingKind.Stele && carving.Region == region).ToList();
        while (steles.Count >= Rules.MaxStelesPerRegion)
        {
            _carvings.Remove(steles[0]);
            steles.RemoveAt(0);
        }
        var used = steles.Select(carving => carving.Row).ToHashSet();
        var row = Enumerable.Range(1, Rules.MaxStelesPerRegion).First(candidate => !used.Contains(candidate));
        var stele = new CarvedGlyph(CarvingKind.Stele, sentence, region, Regions.FirstColumn(region), row, year, Decayed);
        _carvings.Add(stele);
        return stele;
    }

    /// <summary>
    /// 字が崩れた年に呼ぶ。崩れる前に刻まれた碑を、古い順に今の人が読み直す。人はどの記号も知っている（謎の語は無い）。
    /// 崩れる前の読みと今の読みで教えが違う碑を返す（似た形の組の語を含む碑だけが違って読まれる）。
    /// </summary>
    public IReadOnlyList<SteleMisreading> RereadSteles()
    {
        Dogma? Teaching(Sentence sentence, bool decayed) => ReadAsPeople(sentence, decayed)?.Teaching;
        var misreadings = new List<SteleMisreading>();
        foreach (var stele in _carvings.Where(carving => carving.Kind == CarvingKind.Stele && !carving.Decayed))
        {
            if (Teaching(stele.Sentence, false) is { } written && Teaching(stele.Sentence, true) is { } read
                && (written.TargetSymbolId, written.Type) != (read.TargetSymbolId, read.Type))
            {
                misreadings.Add(new SteleMisreading(stele, written, read));
            }
        }
        return misreadings;
    }

    /// <summary>
    /// 人（住人）が文を読む。住人はどの記号も知っているので、謎の語は無い。<paramref name="decayed"/> は読む人の時代（崩れた後か）。
    /// 文が読めない形（語が 0 個・多すぎる・定義に無い語）なら null。
    /// </summary>
    public ScriptReading? ReadAsPeople(Sentence sentence, bool decayed)
    {
        ArgumentNullException.ThrowIfNull(sentence);
        var words = Script.Radicals.Select(radical => radical.Id).ToHashSet();
        var symbols = Script.Radicals.Where(radical => radical.Kind == RadicalKind.Meaning).Select(radical => radical.Key).ToList();
        return ScriptReader.Read(Script, Rules, sentence, words, symbols, decayed).Reading;
    }

    /// <summary>刻まれた文か祈りの文を見た。その文の語の字を手帳に載せる（載っていれば何もしない）。</summary>
    /// <returns>新しく載った語の字の数。</returns>
    public int Discover(Sentence sentence)
    {
        ArgumentNullException.ThrowIfNull(sentence);
        var added = 0;
        foreach (var id in sentence.Words)
        {
            if (Script.Find(id) is not null && _notebook.TryAdd(id, NotebookLabels.Unknown))
            {
                added++;
            }
        }
        return added;
    }

    /// <summary>手帳の語の字に、神の推理を書き込む。</summary>
    /// <param name="label">意味の一覧（記号の ID・種別の名前・方角の名前）か <see cref="NotebookLabels.Unknown"/>。</param>
    /// <exception cref="ArgumentException">手帳に載っていない語の字・一覧に無い意味。</exception>
    public void Label(string radicalId, string label)
    {
        if (!_notebook.ContainsKey(radicalId) || !IsLabel(label))
        {
            throw new ArgumentException($"手帳に無い語の字か、一覧に無い意味: radicalId={radicalId} label={label}");
        }
        _notebook[radicalId] = label;
    }

    /// <summary>手帳に書き込める意味か（語の字が表す意味のどれか、または「分からない」）。</summary>
    public bool IsLabel(string label) => label == NotebookLabels.Unknown || Script.Radicals.Any(radical => radical.Key == label);

    public ScriptSave Capture() =>
        new(Script.Radicals, Script.WordOrder, Script.SimilarPair, Decayed, [.. _carvings], new Dictionary<string, string>(_notebook));

    /// <summary>セーブから読み込んだ状態に置き換える。検証を済ませてから置き換える。</summary>
    /// <exception cref="ArgumentException">定義に合わない値。</exception>
    public void Restore(ScriptSave save)
    {
        ArgumentNullException.ThrowIfNull(save);
        var script = PlanetScript.Restore(Rules, save.Radicals, save.WordOrder, save.SimilarPair);
        var valid = save.Carvings.All(carving => carving.Sentence.Words.Count > 0 && carving.Sentence.Words.All(id => script.Find(id) is not null)
                && carving.Region >= 0 && carving.Region < Regions.Count && carving.Year >= 0 && Enum.IsDefined(carving.Kind))
            && save.Notebook.All(pair => script.Find(pair.Key) is not null
                && (pair.Value == NotebookLabels.Unknown || script.Radicals.Any(radical => radical.Key == pair.Value)));
        if (!valid)
        {
            throw new ArgumentException("刻まれた字か手帳が、字の定義に合わない", nameof(save));
        }
        Script = script;
        Decayed = save.Decayed;
        _carvings.Clear();
        _carvings.AddRange(save.Carvings);
        _notebook.Clear();
        foreach (var (id, label) in save.Notebook)
        {
            _notebook[id] = label;
        }
    }

    /// <summary>世界再創生: 建物と信仰の札を消し、地形の目印を刻み直す。字のしくみ・崩れ・手帳は残す（神の知識）。</summary>
    public void Reset()
    {
        _carvings.Clear();
        CarveLandmarks();
    }

    private void CarveLandmarks()
    {
        foreach (var landmark in Landmarks)
        {
            if (Script.Find(RadicalKind.Meaning, landmark.SymbolId) is not null)
            {
                _carvings.Add(new CarvedGlyph(
                    CarvingKind.Landmark, Script.SentenceFor(landmark.SymbolId), Regions.RegionOf(landmark.Column), landmark.Column, landmark.Row, 0, Decayed));
            }
        }
    }

    // 地域ごとに、住人のうちの信者の割合がいちばん高い禁忌（割合が同じなら記号の ID の順）が、割合の閾値以上なら札を刻む
    private void UpdateTabooPlates(int year, Residents? residents)
    {
        for (var region = 0; region < Regions.Count; region++)
        {
            var people = residents?.People.Where(person => person.Region == region).ToList() ?? [];
            var top = people.Count == 0
                ? null
                : people.SelectMany(person => person.Beliefs.Where(belief => belief.Type == DogmaType.Taboo).Select(belief => belief.SymbolId).Distinct())
                    .GroupBy(symbol => symbol)
                    .Select(group => (Symbol: group.Key, Share: (double)group.Count() / people.Count))
                    .Where(entry => entry.Share + 1e-6 >= TabooPlateShare)
                    .OrderByDescending(entry => entry.Share).ThenBy(entry => entry.Symbol, StringComparer.Ordinal)
                    .Select(entry => entry.Symbol)
                    .FirstOrDefault();
            var index = _carvings.FindIndex(carving => carving.Kind == CarvingKind.Taboo && carving.Region == region);
            var sentence = top is null || Script.Find(RadicalKind.Meaning, top) is null ? null : Script.SentenceFor(top, DogmaType.Taboo);
            if (index >= 0 && (sentence is null || _carvings[index].Sentence != sentence))
            {
                _carvings.RemoveAt(index);
                index = -1;
            }
            if (sentence is not null && index < 0)
            {
                _carvings.Add(new CarvedGlyph(CarvingKind.Taboo, sentence, region, Regions.FirstColumn(region), 0, year, Decayed));
            }
        }
    }

    // 地域の住人のうち、いちばん多くの人が信じている教え（人数が同じなら記号の ID・種別の順）。信者がいなければ null
    private static (string SymbolId, DogmaType Type)? MostBelieved(Residents? residents, int region)
    {
        if (residents is null)
        {
            return null;
        }
        var top = residents.People.Where(person => person.Region == region)
            .SelectMany(person => person.Beliefs.Select(belief => (belief.SymbolId, belief.Type)).Distinct())
            .GroupBy(teaching => teaching)
            .OrderByDescending(group => group.Count()).ThenBy(group => group.Key.SymbolId, StringComparer.Ordinal).ThenBy(group => group.Key.Type)
            .FirstOrDefault();
        return top?.Key;
    }
}
