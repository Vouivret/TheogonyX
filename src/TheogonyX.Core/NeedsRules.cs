namespace TheogonyX.Core;

/// <summary>
/// 住人の欲求と健康に使うゲームデータ定義（docs/domain/agent-needs.md の「M5 で作る最初の範囲と値」）。
/// 欲求は 0.0（満たされている）〜 1.0（切実）、体力と SAN 値は 0〜<see cref="MaxHealth"/>・<see cref="MaxSanity"/>。
/// </summary>
/// <param name="ImmigrantAge">よそから移り住む住人の年齢。</param>
/// <param name="BaseCapacity">建物の無い地域の住人の上限。</param>
/// <param name="CapacityPerBuilding">建物 1 つで増える住人の上限。住居の足りる人数でもある。</param>
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
/// <param name="RampageChaos">狂乱になった年に上がるカオス度。</param>
/// <param name="GraveYears">墓を残す年数。</param>
/// <param name="WitnessAge">証人の称号を判定する年齢。</param>
/// <param name="WitnessIntensityAtOrAbove">証人の称号になる、強いドグマの強度。</param>
/// <param name="ElderAge">長老の称号を得る年齢。</param>
/// <param name="NamePrefixes">名・家名の接頭辞の ID。</param>
/// <param name="NameSuffixes">名・家名の接尾辞の ID。</param>
public sealed record NeedsRules(
    int ImmigrantAge,
    int BaseCapacity,
    int CapacityPerBuilding,
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
    float RampageChaos,
    int GraveYears,
    int WitnessAge,
    float WitnessIntensityAtOrAbove,
    int ElderAge,
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
        ];
        double[] amounts =
        [
            MaxHealth, HealthLoss, HealthRecovery, AgingLossPerYear, MaxSanity, SanityLossPerDeath, SanityRecovery, RecoveredSanity, RampageChaos,
            FamineAtOrBelow, WitnessIntensityAtOrAbove,
        ];
        int[] counts = [ImmigrantAge, BaseCapacity, CapacityPerBuilding, AgingStartAge, BreakdownYears, GraveYears, WitnessAge, ElderAge];
        if (needs.Any(value => !double.IsFinite(value) || value is < 0 or > 1)
            || amounts.Any(value => !double.IsFinite(value) || value < 0)
            || counts.Any(value => value < 0)
            || !double.IsFinite(SanityChaosDivisor) || SanityChaosDivisor <= 0
            || MaxHealth <= 0 || MaxSanity <= 0 || RecoveredSanity > MaxSanity
            || BreakdownYears == 0 || GraveYears == 0)
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
