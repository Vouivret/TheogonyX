namespace TheogonyX.Core;

/// <summary>
/// 住人の欲求と健康に使うゲームデータ定義（docs/domain/agent-needs.md の「M5 で作る最初の範囲と値」）。
/// 欲求は 0.0（満たされている）〜 1.0（切実）、体力と SAN 値は 0〜<see cref="MaxHealth"/>・<see cref="MaxSanity"/>。
/// </summary>
/// <param name="ImmigrantAge">よそから移り住む住人の年齢。</param>
/// <param name="ImmigrateBelowAdults">地域の正常な大人がこれより少なければ、1 人移り住む。</param>
/// <param name="BaseCapacity">家の無い地域の住人の上限（子供が生まれるのはこれより少ないときだけ）。</param>
/// <param name="CapacityPerHouse">家 1 軒で増える住人の上限。住居の足りる人数でもある。</param>
/// <param name="TimberHouseFromTechStage">住人が建てる家が木組みの家になる、地域の技術の段階（これより下はわらぶき小屋）。</param>
/// <param name="BrickHouseFromTechStage">住人が建てる家がれんがの家になる、地域の技術の段階。</param>
/// <param name="FamineAtOrBelow">森林量か水資源がこれ以下なら飢饉の年。</param>
/// <param name="HungerCatatonia">自閉・絶望の住人の空腹（食事を拒む）。</param>
/// <param name="LowerNeedsBelow">空腹・住居がどちらもこれ未満なら、交流・承認・自己実現のタスクを選べる。空腹がこれ未満なら SAN 値が戻る。</param>
/// <param name="TaskRelief">交流・承認・自己実現のタスクを選んだ年に、その欲求が減る量。</param>
/// <param name="HealthLoss">飢饉の年と自閉・絶望の住人の体力の減り。</param>
/// <param name="HealthRecovery">それ以外の年の体力の戻り。</param>
/// <param name="AgingStartAge">体力の上限が下がり始める年齢。</param>
/// <param name="AgingLossPerYear">体力の上限が 1 年に下がる量。</param>
/// <param name="SanityChaosDivisor">毎年、カオス度をこれで割った分だけ SAN 値が減る。</param>
/// <param name="SanityLossPerDeath">同じ地域の住人が 1 人死ぬごとの SAN 値の減り。</param>
/// <param name="RampageProbability">精神崩壊が狂乱になる確率。残りは自閉・絶望。</param>
/// <param name="BreakdownYears">精神崩壊が続く年数。</param>
/// <param name="RecoveredSanity">精神崩壊から戻ったときの SAN 値。</param>
/// <param name="GraveYears">墓を残す年数。</param>
/// <param name="WitnessAge">証人の称号を判定する年齢。</param>
/// <param name="WitnessIntensityAtOrAbove">証人の称号になる、強いドグマの強度。</param>
/// <param name="ElderAge">長老の称号を得る年齢。</param>
/// <param name="BuilderHouses">称号「街を包む者」を得る、建てた家の数。</param>
/// <param name="AdultAge">大人の年齢。これより若い住人はタスクを選ばず（何もしない）、つがいにならない。</param>
/// <param name="FertileUntilAge">子供をもうけられる年齢の上限（つがいの 2 人ともこれ以下）。</param>
/// <param name="BirthProbability">条件を満たすつがい 1 組から、1 年に子供が生まれる確率。</param>
/// <param name="NamePrefixes">名・家名の接頭辞の ID。</param>
/// <param name="NameSuffixes">名・家名の接尾辞の ID。</param>
public sealed record NeedsRules(
    int ImmigrantAge,
    int ImmigrateBelowAdults,
    int BaseCapacity,
    int CapacityPerHouse,
    int TimberHouseFromTechStage,
    int BrickHouseFromTechStage,
    float FamineAtOrBelow,
    double HungerFamine,
    double HungerNormal,
    double HungerCatatonia,
    double ShelterEnough,
    double ShelterLacking,
    double SocialPerYear,
    double EsteemPerYear,
    double ActualizationPerYear,
    double TaskRelief,
    double LowerNeedsBelow,
    double SocialAtOrAbove,
    double EsteemAtOrAbove,
    double ActualizationAtOrAbove,
    double MaxHealth,
    double HealthLoss,
    double HealthRecovery,
    int AgingStartAge,
    double AgingLossPerYear,
    double MaxSanity,
    double SanityChaosDivisor,
    double SanityLossPerDeath,
    double SanityRecovery,
    double RampageProbability,
    int BreakdownYears,
    double RecoveredSanity,
    int GraveYears,
    int WitnessAge,
    float WitnessIntensityAtOrAbove,
    int ElderAge,
    int BuilderHouses,
    int AdultAge,
    int FertileUntilAge,
    double BirthProbability,
    IReadOnlyList<string> NamePrefixes,
    IReadOnlyList<string> NameSuffixes)
{
    /// <summary>値が範囲の中にあることを確かめる。</summary>
    /// <exception cref="ArgumentException">範囲の外の値がある。</exception>
    public void Validate()
    {
        double[] needs =
        [
            HungerFamine, HungerNormal, HungerCatatonia, ShelterEnough, ShelterLacking, SocialPerYear, EsteemPerYear, ActualizationPerYear,
            TaskRelief, LowerNeedsBelow, SocialAtOrAbove, EsteemAtOrAbove, ActualizationAtOrAbove, RampageProbability,
            BirthProbability,
        ];
        double[] amounts =
        [
            MaxHealth, HealthLoss, HealthRecovery, AgingLossPerYear, MaxSanity, SanityLossPerDeath, SanityRecovery, RecoveredSanity,
            FamineAtOrBelow, WitnessIntensityAtOrAbove,
        ];
        int[] counts =
        [
            ImmigrantAge, ImmigrateBelowAdults, BaseCapacity, CapacityPerHouse, TimberHouseFromTechStage, BrickHouseFromTechStage, AgingStartAge, BreakdownYears,
            GraveYears, WitnessAge, ElderAge, BuilderHouses, AdultAge, FertileUntilAge,
        ];
        if (needs.Any(value => !double.IsFinite(value) || value is < 0 or > 1)
            || amounts.Any(value => !double.IsFinite(value) || value < 0)
            || counts.Any(value => value < 0)
            || !double.IsFinite(SanityChaosDivisor) || SanityChaosDivisor <= 0
            || MaxHealth <= 0 || MaxSanity <= 0 || RecoveredSanity > MaxSanity
            || BreakdownYears == 0 || GraveYears == 0 || BuilderHouses == 0 || TimberHouseFromTechStage > BrickHouseFromTechStage)
        {
            throw new ArgumentException($"欲求と健康の定義の値が範囲外: {this}");
        }
        foreach (var parts in new[] { NamePrefixes, NameSuffixes })
        {
            if (parts is null || parts.Count == 0 || parts.Any(string.IsNullOrEmpty) || parts.Distinct().Count() != parts.Count)
            {
                throw new ArgumentException("名前のパーツが空か、重なっている");
            }
        }
    }
}
