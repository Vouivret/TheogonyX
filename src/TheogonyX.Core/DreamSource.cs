namespace TheogonyX.Core;

/// <summary>
/// シード付きの乱数で夢を作り、見た夢の数を数える（docs/domain/save-data.md）。
/// セーブから読み込んだときは、同じシードから同じ数の夢を作り直して、乱数を同じところまで進める。
/// </summary>
public sealed class DreamSource
{
    private readonly DreamRules _dreamRules;
    private readonly ProphetDreamRules _rules;
    private Random _random;

    public DreamSource(int seed, DreamRules dreamRules, ProphetDreamRules rules)
    {
        ArgumentNullException.ThrowIfNull(dreamRules);
        ArgumentNullException.ThrowIfNull(rules);
        Seed = seed;
        _dreamRules = dreamRules;
        _rules = rules;
        _random = new Random(seed);
    }

    public int Seed { get; }

    /// <summary>これまでに作った夢の数。</summary>
    public int DreamsGenerated { get; private set; }

    public IReadOnlyList<DreamNode> Next()
    {
        DreamsGenerated++;
        return DreamGenerator.Generate(_random, _dreamRules, _rules);
    }

    /// <summary>シードから作り直し、<paramref name="dreamsGenerated"/> 個の夢を作って捨てる。</summary>
    public void Rewind(int dreamsGenerated)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(dreamsGenerated);
        _random = new Random(Seed);
        DreamsGenerated = 0;
        while (DreamsGenerated < dreamsGenerated)
        {
            Next();
        }
    }
}
