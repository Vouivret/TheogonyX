namespace TheogonyX.Core;

/// <summary>
/// 星の字のゲームデータ定義（docs/domain/script-decipher.md）。値は仮置き（<c>TODO(AI_UNSURE)</c>）で、遊びながら決める。
/// </summary>
/// <param name="GridSize">部品の線を結ぶ格子の 1 辺の点の数。</param>
/// <param name="MinStrokes">1 つの部品の線の数の下限。</param>
/// <param name="MaxStrokes">1 つの部品の線の数の上限。</param>
/// <param name="DecayYears">字が崩れる年（星を作ってからの年数。小さい順。数が崩れの回数）。</param>
/// <param name="TeachingIntensity">字の神託の教えの強度。</param>
/// <param name="MaxStelesPerRegion">1 地域に残す神託の碑の数（新しいものから）。</param>
/// <param name="MisreadShare">碑の読み違いの教えを信じる、その地域の住人の割合。</param>
/// <param name="StyleJitter">刻まれた字の、石工の手の揺れの大きさ（格子 1 マスを 1 とする。見た目だけ。似た形の組の線 1 本の違いが消えないよう 0.25 まで）。</param>
/// <param name="MaxAttempts">部品の形を作り直す回数の上限（越えたら定義が悪い）。</param>
/// <param name="StrifeRequestIds">危機「争い」に当たる、予言者の依頼の困りごとの ID（docs/domain/script-decipher.md の 9）。無ければ空。</param>
/// <param name="WantRequestIds">危機「欠乏」に当たる、予言者の依頼の困りごとの ID。無ければ空。</param>
public sealed record ScriptRules(
    int GridSize, int MinStrokes, int MaxStrokes, IReadOnlyList<int> DecayYears, float TeachingIntensity, int MaxStelesPerRegion, float MisreadShare,
    float StyleJitter, int MaxAttempts = 1000, IReadOnlyList<string>? StrifeRequestIds = null, IReadOnlyList<string>? WantRequestIds = null)
{
    /// <summary>危機「争い」に当たる困りごとの ID。</summary>
    public IReadOnlyList<string> StrifeRequests => StrifeRequestIds ?? [];

    /// <summary>危機「欠乏」に当たる困りごとの ID。</summary>
    public IReadOnlyList<string> WantRequests => WantRequestIds ?? [];

    /// <summary>神託 1 回の文に並べられる語の数の上限。</summary>
    public const int MaxGlyphsPerOracle = 4;

    /// <exception cref="ArgumentException">範囲外の値。</exception>
    public void Validate()
    {
        if (GridSize < 3 || MinStrokes < 3 || MaxStrokes < MinStrokes || MaxStrokes > GridSize * GridSize
            || DecayYears is not { Count: > 0 } || DecayYears[0] < 1 || DecayYears.Zip(DecayYears.Skip(1)).Any(pair => pair.First >= pair.Second)
            || !float.IsFinite(TeachingIntensity) || TeachingIntensity is <= 0f or > 1f || MaxStelesPerRegion < 1
            || !float.IsFinite(MisreadShare) || MisreadShare is <= 0f or > 1f
            || !float.IsFinite(StyleJitter) || StyleJitter is < 0f or > 0.25f || MaxAttempts < 1)
        {
            throw new ArgumentException($"字の定義の値が範囲外: {this}");
        }
    }
}
