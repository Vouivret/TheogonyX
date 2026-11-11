namespace TheogonyX.Core;

/// <summary>整数の範囲（両端を含む）。</summary>
public readonly record struct IntRange(int Min, int Max)
{
    /// <summary>範囲の中から一様に選ぶ。</summary>
    public int Pick(Random random) => random.Next(Min, Max + 1);
}
