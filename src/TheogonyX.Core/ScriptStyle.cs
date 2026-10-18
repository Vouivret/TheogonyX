namespace TheogonyX.Core;

/// <summary>字の線の端の形。</summary>
public enum StrokeCap
{
    Square,
    Round,
    Wedge,
}

/// <summary>字に添える点の飾り。</summary>
public enum GlyphOrnament
{
    None,

    /// <summary>線の終わりの端に点。</summary>
    EndDots,

    /// <summary>字の下に点 1 つ。</summary>
    BaseDot,
}

/// <summary>字の囲み。</summary>
public enum GlyphFrame
{
    None,
    Circle,
    Square,

    /// <summary>字の上に横棒。</summary>
    TopBar,
}

/// <summary>
/// 字の書きぶり（docs/domain/script-decipher.md の 7「字の書きぶり」）。字の線（格子点）は変えず、描き方だけを決める。見た目だけに使い、
/// シミュレーションの結果には効かない。星の字の形から決まった計算で求めるので、セーブには持たない（読み込めば同じ書きぶりに戻る）。
/// 値の幅は仮置き（TODO(AI_UNSURE): 遊んで決める）。
/// </summary>
/// <param name="Weight">線の太さの倍率。</param>
/// <param name="Bend">線の反り（線の長さに対する、真ん中の膨らみの割合。0 ならまっすぐ。正負で向き）。</param>
/// <param name="Slant">字の傾き（縦に 1 進むと横にずれる割合）。</param>
public sealed record ScriptStyle(StrokeCap Cap, float Weight, float Bend, float Slant, GlyphOrnament Ornament, GlyphFrame Frame)
{
    /// <summary>
    /// 星の字の、時代ごとの書きぶり。崩れた後は振り直し、線の端の形は必ず崩れる前と変える（昔の碑と今の札の見た目が違う）。
    /// 語ごとには変えない（飾りで字を見分けられないように）。
    /// </summary>
    public static ScriptStyle Of(PlanetScript script, bool decayed)
    {
        ArgumentNullException.ThrowIfNull(script);
        var before = Roll(new Random(Fingerprint(script)));
        if (!decayed)
        {
            return before;
        }
        var random = new Random(unchecked(Fingerprint(script) + 0x44454341)); // "DECA"
        var after = Roll(random);
        return after with { Cap = (StrokeCap)(((int)before.Cap + 1 + random.Next(2)) % 3) };
    }

    /// <summary>
    /// 石工の手の揺れ。刻まれた字の場所と年・線の番号・端（0 か 1）から決まる、線の端の位置のずれ（格子 1 マスを 1 とする）。
    /// x・y とも −<paramref name="amount"/>〜<paramref name="amount"/>。
    /// </summary>
    public static (float X, float Y) Jitter(float amount, int column, int row, int year, int stroke, int end)
    {
        float Axis(int axis) => (float)((Mix(column, row, year, (stroke * 4) + (end * 2) + axis) / (double)uint.MaxValue * 2) - 1) * amount;
        return (Axis(0), Axis(1));
    }

    private static ScriptStyle Roll(Random random)
    {
        var cap = (StrokeCap)random.Next(3);
        var weight = 0.7f + ((float)random.NextDouble() * 0.9f);
        var bend = random.Next(2) == 0 ? 0f : (0.1f + ((float)random.NextDouble() * 0.2f)) * (random.Next(2) == 0 ? -1 : 1);
        var slant = ((float)random.NextDouble() - 0.5f) * 0.3f;
        var ornament = (GlyphOrnament)random.Next(3);
        var frame = (GlyphFrame)random.Next(4);
        return new ScriptStyle(cap, weight, bend, slant, ornament, frame);
    }

    // 星の字の形から作る数（FNV-1a）。字の ID と線から求めるので、同じ星なら同じ数になる
    private static int Fingerprint(PlanetScript script)
    {
        var hash = 2166136261u;
        void Add(int value)
        {
            hash = unchecked((hash ^ (uint)value) * 16777619u);
        }
        foreach (var radical in script.Radicals)
        {
            foreach (var character in radical.Id)
            {
                Add(character);
            }
            foreach (var stroke in radical.Strokes)
            {
                Add(stroke.X1);
                Add(stroke.Y1);
                Add(stroke.X2);
                Add(stroke.Y2);
            }
        }
        Add((int)script.WordOrder);
        return unchecked((int)hash);
    }

    // 整数を混ぜて、偏りの少ない数にする（splitmix64 の混ぜ方）
    private static uint Mix(int a, int b, int c, int d)
    {
        var x = unchecked(((ulong)(uint)a << 32) ^ ((ulong)(uint)b * 0x9E3779B97F4A7C15UL) ^ ((ulong)(uint)c << 16) ^ (ulong)(uint)d);
        x = unchecked((x ^ (x >> 30)) * 0xBF58476D1CE4E5B9UL);
        x = unchecked((x ^ (x >> 27)) * 0x94D049BB133111EBUL);
        return (uint)(x ^ (x >> 31));
    }
}
