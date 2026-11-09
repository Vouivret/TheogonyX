namespace TheogonyX.Core;

/// <summary>印を置く場所の決め方（docs/domain/structures.md の印、docs/domain/script-decipher.md の 9）。</summary>
public enum MarkPlace
{
    /// <summary>同じ地域に記号の物の目印があればその隣、無ければ地域の真ん中（祭壇）。</summary>
    SymbolOrCenter,

    /// <summary>同じ地域に記号の物の目印があればその隣、無ければ地域の中の乱数の場所（切り株）。</summary>
    SymbolOrAnywhere,

    /// <summary>隣の地域がある側の、地域の端の列（柵・壁）。</summary>
    Border,
}

/// <summary>読みの跡の印の型（game/data/structures.json の 1 行）。</summary>
/// <param name="Id">型の ID（例: <c>STRUCT_ALTAR</c>）。</param>
/// <param name="Reading">この印を残す読み。1 つの読みに型は 1 つ。</param>
/// <param name="Material">材料の ID（例: <c>STONE</c>）。今は見た目と記録だけに使う。</param>
/// <param name="LifespanYears">信者がいなくなってから消えるまでの年数。</param>
public sealed record MarkType(string Id, OracleReading Reading, string Material, int LifespanYears, MarkPlace Place);

/// <summary>
/// 読みの跡の印の定義（docs/domain/script-decipher.md の 9）。値は仮置き（<c>TODO(AI_UNSURE)</c>）で、遊びながら決める。
/// </summary>
/// <param name="BelieversAtOrAbove">地域でその読みの教えを信じる住人がこの人数以上なら、印を建てる。</param>
public sealed record MarkRules(int BelieversAtOrAbove, IReadOnlyList<MarkType> Types)
{
    /// <summary>読みの印の型。その読みの型が無ければ null。</summary>
    public MarkType? TypeOf(OracleReading reading) => Types.FirstOrDefault(type => type.Reading == reading);

    /// <exception cref="ArgumentException">範囲外の値か、同じ読み・同じ ID の型が 2 つある。</exception>
    public void Validate()
    {
        if (BelieversAtOrAbove < 1 || Types.Count == 0
            || Types.Any(type => string.IsNullOrWhiteSpace(type.Id) || string.IsNullOrWhiteSpace(type.Material) || type.LifespanYears < 1
                || !Enum.IsDefined(type.Reading) || !Enum.IsDefined(type.Place))
            || Types.Select(type => type.Reading).Distinct().Count() != Types.Count
            || Types.Select(type => type.Id).Distinct().Count() != Types.Count)
        {
            throw new ArgumentException($"読みの跡の印の定義が正しくない: believersAtOrAbove={BelieversAtOrAbove} types={Types.Count}");
        }
    }
}

/// <summary>印の計算に使う定義と、記号の物の目印の場所。</summary>
public sealed record MarkSetup(MarkRules Rules, IReadOnlyList<ScriptLandmark> Landmarks);

/// <summary>
/// 地表に残った読みの跡の印（docs/domain/structures.md の「印」）。枠を占めない点の構造物で、1 地域に同じ読み × 記号の印は 1 つ。
/// </summary>
/// <param name="Reading">印を残した読み。教えの種別は読みで決まる。</param>
/// <param name="SymbolId">教えの記号。</param>
/// <param name="BuiltYear">建った年。</param>
/// <param name="TendedYear">その教えの信者が地域にいた最後の年（修繕された年）。ここから寿命の年数がたつと消える。</param>
public sealed record Mark(OracleReading Reading, string SymbolId, int Region, int Column, int Row, int BuiltYear, int TendedYear)
{
    /// <summary>印を残した教えの種別。</summary>
    public DogmaType Type => OracleReadings.TypeOf(Reading);
}

/// <summary>読みの跡の印を建てる・修繕する・消す（docs/domain/script-decipher.md の 9、docs/domain/structures.md の 8）。</summary>
public static class MarkSimulator
{
    private const uint MarkSalt = 0x4D41524B; // "MARK"

    /// <summary>
    /// 1 年分。先に今ある印を見て、地域にその教えの信者がいれば修繕し、いなければ寿命の年数がたった印を消す（定義から型が消えた印も消す）。
    /// 次に地域ごとに、読みの型がある教えの信者を「読み × 記号」で数え、閾値以上で、まだ同じ印が無いもののうち 1 つに印を建てる。
    /// 信者の多いもの、同じなら記号の ID の順、同じなら読みの順で選ぶ。
    /// </summary>
    /// <returns>この年に建った印。</returns>
    public static IReadOnlyList<Mark> AdvanceYear(
        Civilization civilization, Residents residents, int year, int seed, MarkSetup setup, CivilizationRules civilizationRules)
    {
        ArgumentNullException.ThrowIfNull(civilization);
        ArgumentNullException.ThrowIfNull(residents);
        ArgumentNullException.ThrowIfNull(setup);
        ArgumentNullException.ThrowIfNull(civilizationRules);
        var rules = setup.Rules;

        int BelieversOf(int region, OracleReading reading, string symbolId) => residents.People.Count(person =>
            person.Region == region && person.Beliefs.Any(belief => belief.Reading == reading && belief.SymbolId == symbolId));

        var kept = new List<Mark>();
        foreach (var mark in civilization.Marks)
        {
            if (rules.TypeOf(mark.Reading) is not { } type)
            {
                continue;
            }
            if (BelieversOf(mark.Region, mark.Reading, mark.SymbolId) > 0)
            {
                kept.Add(mark with { TendedYear = year });
            }
            else if (year - mark.TendedYear < type.LifespanYears)
            {
                kept.Add(mark);
            }
        }

        var built = new List<Mark>();
        var regions = civilizationRules.Regions;
        for (var region = 0; region < regions.Count; region++)
        {
            var candidate = residents.People
                .Where(person => person.Region == region)
                .SelectMany(person => person.Beliefs
                    .Where(belief => belief.Reading is { } reading && rules.TypeOf(reading) is not null)
                    .Select(belief => (Reading: belief.Reading!.Value, belief.SymbolId))
                    .Distinct())
                .GroupBy(teaching => teaching)
                .Select(group => (group.Key.Reading, group.Key.SymbolId, Count: group.Count()))
                .Where(teaching => teaching.Count >= rules.BelieversAtOrAbove
                    && !kept.Exists(mark => mark.Region == region && mark.Reading == teaching.Reading && mark.SymbolId == teaching.SymbolId))
                .OrderByDescending(teaching => teaching.Count)
                .ThenBy(teaching => teaching.SymbolId, StringComparer.Ordinal)
                .ThenBy(teaching => teaching.Reading)
                .FirstOrDefault();
            if (candidate.Count == 0)
            {
                continue;
            }
            var random = new Random(unchecked((int)CrowdHash.Hash((uint)seed ^ MarkSalt, (uint)year, (uint)region)));
            var (column, row) = Place(rules.TypeOf(candidate.Reading)!.Place, candidate.SymbolId, region, kept, civilization, setup.Landmarks, civilizationRules, random);
            var mark = new Mark(candidate.Reading, candidate.SymbolId, region, column, row, year, year);
            kept.Add(mark);
            built.Add(mark);
        }
        civilization.ReplaceMarks(kept);
        return built;
    }

    // 印を置くマス。ほかの印か、建物・廃墟の 2 × 2 マスと重なれば、同じ列で下の行へずらす（一周しても空かなければ重ねる）。
    // 印を建てた後に建物が上に建つことはある（建物は印を避けない）
    private static (int Column, int Row) Place(
        MarkPlace place, string symbolId, int region, IReadOnlyList<Mark> marks, Civilization civilization,
        IReadOnlyList<ScriptLandmark> landmarks, CivilizationRules rules, Random random)
    {
        var regions = rules.Regions;
        var first = regions.FirstColumn(region);
        var last = first + regions.ColumnsPerRegion - 1;
        var landmark = landmarks.FirstOrDefault(item => item.SymbolId == symbolId && regions.RegionOf(item.Column) == region);
        int column;
        int row;
        if (place != MarkPlace.Border && landmark is not null)
        {
            // 目印の隣の列（地域の中に収める）
            column = landmark.Column < last ? landmark.Column + 1 : landmark.Column - 1;
            column = Math.Clamp(column, first, last);
            row = landmark.Row;
        }
        else if (place == MarkPlace.SymbolOrCenter)
        {
            column = first + (regions.ColumnsPerRegion / 2);
            row = rules.Rows / 2;
        }
        else if (place == MarkPlace.Border)
        {
            var west = region > 0;
            var east = region < regions.Count - 1;
            column = (west, east) switch
            {
                (true, true) => random.Next(2) == 0 ? first : last,
                (true, false) => first,
                (false, true) => last,
                _ => first + random.Next(regions.ColumnsPerRegion),
            };
            row = random.Next(rules.Rows);
        }
        else
        {
            column = first + random.Next(regions.ColumnsPerRegion);
            row = random.Next(rules.Rows);
        }
        bool Covers(int left, int top) => left <= column && column <= left + 1 && top <= row && row <= top + 1;
        bool Taken() => marks.Any(mark => mark.Column == column && mark.Row == row)
            || civilization.Buildings.Any(building => Covers(building.Column, building.Row))
            || civilization.Ruins.Any(ruin => Covers(ruin.Column, ruin.Row));
        for (var tries = 0; tries < rules.Rows && Taken(); tries++)
        {
            row = (row + 1) % rules.Rows;
        }
        return (column, row);
    }
}
