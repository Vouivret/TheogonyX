namespace TheogonyX.Core;

/// <summary>精神崩壊の種類（docs/domain/agent-needs.md）。</summary>
public enum BreakdownKind
{
    /// <summary>狂乱。崩壊した年に、まわりの建物を壊しカオス度を上げる。</summary>
    Rampage,

    /// <summary>自閉・絶望。食事を拒む。</summary>
    Catatonia,
}

/// <summary>住人がその年に主にしたこと。</summary>
public enum ResidentTask
{
    /// <summary>何もしない。</summary>
    Idle,

    /// <summary>食料を探す（空腹）。</summary>
    SeekFood,

    /// <summary>屋根を探す（住居）。</summary>
    SeekShelter,

    /// <summary>広場に集まる（交流）。</summary>
    Gather,

    /// <summary>祠で祈る（承認）。</summary>
    Pray,

    /// <summary>工房で研究する（自己実現）。</summary>
    Research,

    /// <summary>精神崩壊の行動（狂乱・自閉・絶望）。</summary>
    Breakdown,
}

/// <summary>名と家名。どれも名前のパーツの ID（翻訳ファイルのキー）。</summary>
public sealed record ResidentName(string FirstPrefix, string FirstSuffix, string LineagePrefix, string LineageSuffix);

/// <summary>称号。証人の称号は、差し込む記号の ID を持つ。</summary>
public sealed record ResidentEpithet(string Id, string? SymbolId = null);

/// <summary>住人（群衆とは別の、欲求と健康を持つ少数の人。docs/domain/agent-needs.md）。</summary>
/// <param name="Id">通し番号。</param>
/// <param name="Region">属する地域。1 年ごとの計算の中では位置を持たない。</param>
/// <param name="BirthYear">生まれた年。年齢は「年 − 生まれた年」。</param>
/// <param name="Breakdown">精神崩壊中ならその種類。正常なら null。</param>
/// <param name="BreakdownYear">精神崩壊した年。正常なら意味を持たない。</param>
/// <param name="Task">その年に選んだタスク。</param>
/// <param name="Epithet">最初に得た称号。無ければ null。</param>
/// <param name="PartnerId">つがいの相手の ID。独身なら null。相手も同じ地域にいて、相手の <paramref name="PartnerId"/> は自分。</param>
public sealed record Resident(
    int Id,
    int Region,
    int BirthYear,
    double Hunger,
    double Shelter,
    double Social,
    double Esteem,
    double Actualization,
    double Health,
    double Sanity,
    BreakdownKind? Breakdown,
    int BreakdownYear,
    ResidentTask Task,
    ResidentName Name,
    ResidentEpithet? Epithet,
    int? PartnerId = null)
{
    public int AgeIn(int year) => year - BirthYear;
}

/// <summary>死んだ住人の墓。死んだ年から一定の年数だけ残す。</summary>
public sealed record Grave(int ResidentId, int Region, int DeathYear, ResidentName Name, ResidentEpithet? Epithet);

/// <summary>称号の ID（docs/domain/agent-needs.md の「M5 で作る称号」）。</summary>
public static class Epithets
{
    public const string Bloodless = "EPITHET_BLOODLESS";
    public const string Witness = "EPITHET_WITNESS";
    public const string Elder = "EPITHET_ELDER";
}

/// <summary>精神の状態（状態遷移表の行）。</summary>
public enum MentalState
{
    Normal,
    Breakdown,
    Dead,
}

/// <summary>精神の状態が変わるきっかけ（状態遷移表の列）。</summary>
public enum MentalTrigger
{
    SanityZero,
    BreakdownElapsed,
    HealthZero,
}

/// <summary>精神の状態遷移。docs/domain/agent-needs.md の「精神の状態遷移」の表と 1 対 1。表に無い組み合わせは「不可」。</summary>
public static class MentalTransitions
{
    private static readonly Dictionary<(MentalState, MentalTrigger), MentalState> Table = new()
    {
        [(MentalState.Normal, MentalTrigger.SanityZero)] = MentalState.Breakdown,
        [(MentalState.Normal, MentalTrigger.HealthZero)] = MentalState.Dead,
        [(MentalState.Breakdown, MentalTrigger.BreakdownElapsed)] = MentalState.Normal,
        [(MentalState.Breakdown, MentalTrigger.HealthZero)] = MentalState.Dead,
    };

    public static MentalState Next(MentalState state, MentalTrigger trigger) =>
        Table.TryGetValue((state, trigger), out var next)
            ? next
            : throw new InvalidOperationException($"精神の状態でできない遷移: state={state} trigger={trigger}");

    public static MentalState StateOf(Resident resident)
    {
        ArgumentNullException.ThrowIfNull(resident);
        return resident.Breakdown is null ? MentalState.Normal : MentalState.Breakdown;
    }
}
