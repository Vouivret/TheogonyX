namespace TheogonyX.Core;

/// <summary>
/// 住人どうしの好感度に使うゲームデータ定義（docs/domain/agent-needs.md の「好感度（小さな星で作る）」）。
/// 好感度は 0〜<see cref="Max"/> の整数。値は仮置き（<c>TODO(AI_UNSURE)</c>）で、遊びながら決める。
/// </summary>
/// <param name="Max">好感度の上限。</param>
/// <param name="NeighborGain">同じ地域で暮らす 2 人の間で、毎年上がる量（顔を合わせるだけで親しくなる）。</param>
/// <param name="NeighborCap">暮らしだけで上がるのはこの好感度まで（顔見知りまで）。これより上は、広場・祠・教えでしか上がらない。</param>
/// <param name="GatherGain">同じ年に 2 人とも広場に集まったときに上がる量。</param>
/// <param name="PrayGain">同じ年に 2 人とも祠で祈ったときに上がる量。</param>
/// <param name="SameBeliefGain">年の初めに同じ教えを 1 つ以上信じているときに上がる量。</param>
/// <param name="ConflictLoss">年の初めに矛盾する教え（同じ記号の別の種別）を信じているときに下がる量。</param>
/// <param name="RampageLoss">その年に狂乱した住人と、同じ地域の住人との間で下がる量。</param>
/// <param name="DecayPerYear">毎年、すべての組で薄れる量。</param>
/// <param name="PairAtOrAbove">つがいになれる好感度。</param>
/// <param name="LateAge">2 人ともこの年齢以上なら、<see cref="LatePairAtOrAbove"/> でつがいになれる（見合い）。</param>
/// <param name="LatePairAtOrAbove">見合いでつがいになれる好感度。</param>
/// <param name="GriefAtOrAbove">死んだ人との好感度がこれ以上なら、悲しみで SAN 値が下がる。</param>
/// <param name="GriefSanityLoss">悲しみで下がる SAN 値。</param>
/// <param name="GatherTransmitAtZero">好感度 0 の 2 人の間で、広場でのうつりやすさに掛ける倍率。</param>
/// <param name="GatherTransmitAtMax">好感度が上限の 2 人の間で、広場でのうつりやすさに掛ける倍率。間は直線で変わる。</param>
/// <param name="FriendAtOrAbove">住人の情報の欄に「親しい人」として出す好感度。</param>
public sealed record AffinityRules(
    int Max,
    int NeighborGain,
    int NeighborCap,
    int GatherGain,
    int PrayGain,
    int SameBeliefGain,
    int ConflictLoss,
    int RampageLoss,
    int DecayPerYear,
    int PairAtOrAbove,
    int LateAge,
    int LatePairAtOrAbove,
    int GriefAtOrAbove,
    double GriefSanityLoss,
    double GatherTransmitAtZero,
    double GatherTransmitAtMax,
    int FriendAtOrAbove)
{
    /// <summary>値が範囲の中にあることを確かめる。</summary>
    /// <exception cref="ArgumentException">範囲外の値。</exception>
    public void Validate()
    {
        int[] amounts = [NeighborGain, GatherGain, PrayGain, SameBeliefGain, ConflictLoss, RampageLoss, DecayPerYear, LateAge];
        int[] levels = [NeighborCap, PairAtOrAbove, LatePairAtOrAbove, GriefAtOrAbove, FriendAtOrAbove];
        if (Max < 1 || amounts.Any(value => value < 0) || levels.Any(value => value < 1 || value > Max)
            || LatePairAtOrAbove > PairAtOrAbove
            || !double.IsFinite(GriefSanityLoss) || GriefSanityLoss < 0
            || !double.IsFinite(GatherTransmitAtZero) || GatherTransmitAtZero < 0
            || !double.IsFinite(GatherTransmitAtMax) || GatherTransmitAtMax < 0)
        {
            throw new ArgumentException($"好感度の定義の値が範囲外: {this}");
        }
    }

    /// <summary>好感度 <paramref name="affinity"/> の 2 人の間で、広場でのうつりやすさに掛ける倍率。</summary>
    public double GatherTransmitFactor(int affinity) =>
        GatherTransmitAtZero + (GatherTransmitAtMax - GatherTransmitAtZero) * Math.Clamp(affinity, 0, Max) / Max;
}

/// <summary>好感度を持つ 2 人の住人の ID の組。<see cref="Low"/> が小さいほう。</summary>
public readonly record struct ResidentPair
{
    private ResidentPair(int low, int high)
    {
        Low = low;
        High = high;
    }

    public int Low { get; }

    public int High { get; }

    /// <summary>2 人の ID から組を作る（順番は問わない）。</summary>
    /// <exception cref="ArgumentException">同じ ID の 2 人。</exception>
    public static ResidentPair Of(int first, int second) =>
        first == second
            ? throw new ArgumentException($"自分どうしの組は作れない: id={first}")
            : new ResidentPair(Math.Min(first, second), Math.Max(first, second));

    public bool Contains(int id) => Low == id || High == id;

    /// <summary>組のうち <paramref name="id"/> でないほうの ID。</summary>
    public int Other(int id) => Low == id ? High : Low;
}
