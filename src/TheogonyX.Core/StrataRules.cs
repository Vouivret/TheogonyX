namespace TheogonyX.Core;

/// <summary>廃墟化・建て増し・地層沈下に使うゲームデータ定義（docs/domain/strata.md）。</summary>
public sealed record StrataRules
{
    /// <param name="lifespanMin">建物の寿命の最短（年）。</param>
    /// <param name="lifespanMax">建物の寿命の最長（年。両端を含む）。</param>
    /// <param name="ruinBurialYears">上に建物の無い廃墟が、地下 1 層目に沈むまでの年数（ミクロ沈下）。</param>
    /// <param name="maxDepth">地下の層の数。これより深くなった遺物は消す。</param>
    public StrataRules(int lifespanMin, int lifespanMax, int ruinBurialYears, int maxDepth)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(lifespanMin);
        ArgumentOutOfRangeException.ThrowIfLessThan(lifespanMax, lifespanMin);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(ruinBurialYears);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxDepth);
        LifespanMin = lifespanMin;
        LifespanMax = lifespanMax;
        RuinBurialYears = ruinBurialYears;
        MaxDepth = maxDepth;
    }

    public int LifespanMin { get; }
    public int LifespanMax { get; }
    public int RuinBurialYears { get; }
    public int MaxDepth { get; }
}

/// <summary>1 つの枠の建物・遺物の状態（docs/domain/strata.md の状態遷移）。</summary>
public enum StrataState
{
    Building,
    Ruin,
    Buried,
    Excavated,
}

/// <summary>建物・遺物の状態を変えるきっかけ。</summary>
public enum StrataTrigger
{
    Collapse,
    MicroSubduction,
    MacroSubduction,
    Excavation,
    Overbuild,
}

/// <summary>建物・遺物の状態遷移表。docs/domain/strata.md の表と 1 対 1 で対応する。表に無い遷移は「不可」。</summary>
public static class StrataTransitions
{
    private static readonly Dictionary<(StrataState, StrataTrigger), StrataState> Table = new()
    {
        [(StrataState.Building, StrataTrigger.Collapse)] = StrataState.Ruin,
        [(StrataState.Building, StrataTrigger.MacroSubduction)] = StrataState.Buried,
        [(StrataState.Ruin, StrataTrigger.MicroSubduction)] = StrataState.Buried,
        [(StrataState.Ruin, StrataTrigger.MacroSubduction)] = StrataState.Buried,
        [(StrataState.Ruin, StrataTrigger.Overbuild)] = StrataState.Ruin,
        [(StrataState.Buried, StrataTrigger.MicroSubduction)] = StrataState.Buried,
        [(StrataState.Buried, StrataTrigger.MacroSubduction)] = StrataState.Buried,
        [(StrataState.Buried, StrataTrigger.Excavation)] = StrataState.Excavated,
        [(StrataState.Excavated, StrataTrigger.MicroSubduction)] = StrataState.Buried,
        [(StrataState.Excavated, StrataTrigger.MacroSubduction)] = StrataState.Buried,
    };

    /// <exception cref="InvalidOperationException">表で「不可」の遷移。</exception>
    public static StrataState Next(StrataState state, StrataTrigger trigger) =>
        Table.TryGetValue((state, trigger), out var next)
            ? next
            : throw new InvalidOperationException($"地層の状態でできない遷移: state={state} trigger={trigger}");
}
