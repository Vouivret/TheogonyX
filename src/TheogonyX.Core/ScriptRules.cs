namespace TheogonyX.Core;

/// <summary>
/// 星の字のゲームデータ定義（docs/domain/script-decipher.md）。値は仮置き（<c>TODO(AI_UNSURE)</c>）で、遊びながら決める。
/// </summary>
/// <param name="GridSize">部品の線を結ぶ格子の 1 辺の点の数。</param>
/// <param name="MinStrokes">1 つの部品の線の数の下限。</param>
/// <param name="MaxStrokes">1 つの部品の線の数の上限。</param>
/// <param name="DecayYears">星を作ってから字が崩れるまでの年数。</param>
/// <param name="TeachingIntensity">字の神託の教えの強度。</param>
/// <param name="MaxAttempts">部品の形を作り直す回数の上限（越えたら定義が悪い）。</param>
public sealed record ScriptRules(int GridSize, int MinStrokes, int MaxStrokes, int DecayYears, float TeachingIntensity, int MaxAttempts = 1000)
{
    /// <summary>神託 1 回の文に並べられる語の数の上限。</summary>
    public const int MaxGlyphsPerOracle = 4;

    /// <exception cref="ArgumentException">範囲外の値。</exception>
    public void Validate()
    {
        if (GridSize < 3 || MinStrokes < 3 || MaxStrokes < MinStrokes || MaxStrokes > GridSize * GridSize || DecayYears < 1
            || !float.IsFinite(TeachingIntensity) || TeachingIntensity is <= 0f or > 1f || MaxAttempts < 1)
        {
            throw new ArgumentException($"字の定義の値が範囲外: {this}");
        }
    }
}
