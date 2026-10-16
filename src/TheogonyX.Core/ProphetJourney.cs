namespace TheogonyX.Core;

/// <summary>予言者の今の居場所（地域の番号）・知っている土地・向かっている行き先（docs/domain/prophet-life.md）。</summary>
/// <param name="KnownRegions">通った地域とその隣（小さい順）。土地の名が通じる範囲に使う。</param>
public sealed record ProphetState(int Region, IReadOnlyList<int> KnownRegions, TravelDirection Destination = TravelDirection.None);

/// <summary>予言者が動く理由。</summary>
public enum ProphetMoveReason
{
    /// <summary>神の声の行き先に向かう。</summary>
    Destination,

    /// <summary>人が多く信者の少ない隣の地域へ、教えを広めに行く。</summary>
    Wander,
}

/// <summary>予言者が 1 年に動いた記録。</summary>
public sealed record ProphetMove(int Year, int From, int To, ProphetMoveReason Reason);

/// <summary>予言者の旅の定義。値は仮置き（<c>TODO(AI_UNSURE)</c>）で、遊びながら決める。</summary>
/// <param name="WanderProbability">行き先が無い年に、広めに隣の地域へ移る確率。</param>
public sealed record JourneyRules(float WanderProbability)
{
    /// <exception cref="ArgumentException">範囲外の値。</exception>
    public void Validate()
    {
        if (!float.IsFinite(WanderProbability) || WanderProbability is < 0f or > 1f)
        {
            throw new ArgumentException($"予言者の旅の定義の値が範囲外: {this}");
        }
    }
}

/// <summary>
/// 予言者の旅（docs/domain/prophet-life.md の「理由のある旅」の、小さな MVP で入れる 2 つの理由）。
/// 行き先があればそちらへ 1 年に 1 地域進み、無ければ決まった確率で、隣の地域のうち住人が多く信者の少ない地域へ移る。
/// この MVP の星は地域が東西に 1 列に並ぶので、動けるのは東（番号が大きい）と西だけ。端でつながらない。
/// </summary>
public static class ProphetJourney
{
    // 建物・住人・信仰の乱数と同じ並びにならないよう、シードに別の値を混ぜる
    private const uint JourneySalt = 0x4A4F5552; // "JOUR"

    /// <summary>最初の居場所。知っている土地は、そこと両隣。</summary>
    public static ProphetState Initial(int region, int regionCount) => new(region, KnownAround([], region, regionCount));

    /// <summary>1 年進める。動いたときは <see cref="ProphetMove"/> も返す。乱数は「シード・年」から作る。</summary>
    public static (ProphetState State, ProphetMove? Move) AdvanceYear(
        ProphetState prophet, Residents residents, int year, int seed, int regionCount, JourneyRules rules)
    {
        ArgumentNullException.ThrowIfNull(prophet);
        ArgumentNullException.ThrowIfNull(residents);
        ArgumentNullException.ThrowIfNull(rules);
        var roll = new Random(unchecked((int)CrowdHash.Hash((uint)seed ^ JourneySalt, (uint)year, 0))).NextDouble();

        int? to = null;
        var reason = ProphetMoveReason.Wander;
        var destination = prophet.Destination;
        if (destination != TravelDirection.None)
        {
            var step = destination == TravelDirection.East ? 1 : -1;
            var next = prophet.Region + step;
            if (next >= 0 && next < regionCount)
            {
                to = next;
                reason = ProphetMoveReason.Destination;
            }
            // 着いた（端に着いた）ら行き先を消す
            if (next < 0 || next >= regionCount || next + step < 0 || next + step >= regionCount)
            {
                destination = TravelDirection.None;
            }
        }
        else if (roll < rules.WanderProbability)
        {
            // 西の隣を先に見て、同じ点数なら西を取る
            var best = 0;
            foreach (var neighbor in new[] { prophet.Region - 1, prophet.Region + 1 }.Where(region => region >= 0 && region < regionCount))
            {
                var people = residents.People.Where(person => person.Region == neighbor).ToList();
                var score = people.Count - people.Count(person => person.Beliefs.Count > 0);
                if (people.Count > 0 && score > best)
                {
                    best = score;
                    to = neighbor;
                }
            }
        }

        if (to is not { } target)
        {
            return (prophet with { Destination = destination }, null);
        }
        var state = new ProphetState(target, KnownAround(prophet.KnownRegions, target, regionCount), destination);
        return (state, new ProphetMove(year, prophet.Region, target, reason));
    }

    private static List<int> KnownAround(IEnumerable<int> known, int region, int regionCount) =>
        known.Concat([region - 1, region, region + 1]).Where(candidate => candidate >= 0 && candidate < regionCount).Distinct().Order().ToList();
}
