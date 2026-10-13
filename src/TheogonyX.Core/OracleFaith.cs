namespace TheogonyX.Core;

/// <summary>
/// 神威と神の歪み（docs/domain/oracle-pair.md の「神威の代償」）。神威は年が進むと溜まり、神託を下すたびに消費する。
/// 短い年数のうちに神託を重ねすぎたら、超えた回数を返す（世界への影響は呼び出し側が与える）。
/// </summary>
public sealed class OracleFaith
{
    private const double Epsilon = 1e-6;

    private readonly FaithRules _faith;
    private readonly OverloadRules _overload;
    private readonly List<int> _recentYears = [];

    public OracleFaith(FaithRules faith, OverloadRules overload)
    {
        ArgumentNullException.ThrowIfNull(faith);
        ArgumentNullException.ThrowIfNull(overload);
        _faith = faith;
        _overload = overload;
        Value = faith.Initial;
    }

    /// <summary>今の神威。</summary>
    public double Value { get; private set; }

    /// <summary>直近の神託を下した年（古い順）。窓の外の年は、次に神託を下したときに捨てる。</summary>
    public IReadOnlyList<int> RecentYears => _recentYears;

    /// <summary>神託 1 回のコストを払えるか。</summary>
    public bool CanAfford => Value + Epsilon >= _faith.Cost;

    /// <summary>年が進んだとき。1 年に <see cref="FaithRules.PerYear"/> ずつ、上限まで溜まる。</summary>
    public void Advance(int years)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(years);
        Value = Math.Min(_faith.Max, Value + (years * (double)_faith.PerYear));
    }

    /// <summary>神託を下した。コストを払い、その年を数えて、歪みの回数（無償の回数を超えた分）を返す。</summary>
    /// <exception cref="InvalidOperationException">神威が足りない。</exception>
    public int Pronounce(int year)
    {
        if (!CanAfford)
        {
            throw new InvalidOperationException($"神威が足りない: value={Value} cost={_faith.Cost}");
        }

        Value = Math.Max(0, Value - _faith.Cost);
        _recentYears.RemoveAll(past => year - past >= _overload.WindowYears);
        _recentYears.Add(year);
        return Math.Max(0, _recentYears.Count - _overload.FreeCount);
    }

    /// <summary>世界再創生: 神威を最初の量に戻し、数えた神託を捨てる。</summary>
    public void Reset()
    {
        Value = _faith.Initial;
        _recentYears.Clear();
    }
}
