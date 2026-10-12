namespace TheogonyX.Core;

/// <summary>
/// 群衆の乱数。(シード, 人の番号, 歩数) から決まる値を返す（docs/adr/0004-crowd-counter-based-hash-random.md）。
/// GPU 版（GLSL）にも同じ式を書くので、32 bit の符号なし整数の演算だけを使う。
/// </summary>
public static class CrowdHash
{
    public static uint Hash(uint seed, uint index, uint step) => Pcg(seed ^ Pcg(index ^ Pcg(step)));

    /// <returns>0 以上 1 未満。</returns>
    public static float Next01(uint seed, uint index, uint step) => (Hash(seed, index, step) >> 8) * (1f / 16777216f);

    // PCG の 1 段のハッシュ（pcg_hash）。
    private static uint Pcg(uint value)
    {
        unchecked
        {
            var state = value * 747796405u + 2891336453u;
            var word = ((state >> (int)((state >> 28) + 4u)) ^ state) * 277803737u;
            return (word >> 22) ^ word;
        }
    }
}
