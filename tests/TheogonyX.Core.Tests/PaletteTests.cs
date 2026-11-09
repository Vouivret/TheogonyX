namespace TheogonyX.Core.Tests;

/// <summary>パレット（docs/art-guide.md の「パレット」、plan/feat-palette-art.md）。</summary>
public class PaletteTests
{
    // 32 色にそろえる穴埋めの色（赤が 255 で、テストで調べる色から遠い）
    private static IEnumerable<(string, string)> Fillers(int count) =>
        Enumerable.Range(0, count).Select(i => ($"filler{i}", $"#FF{i * 8:X2}00"));

    private static Palette Build(params (string Name, string Hex)[] first) =>
        Palette.FromHex(first.Concat(Fillers(Palette.ColorCount - first.Length)));

    [Fact]
    [Trait("TS", "TS-palette-001")]
    public void FromHex_32色の定義_名前で引け色の値を読める()
    {
        var palette = Build(("ink", "#15121c"), ("snow", "#F1EDE2"));

        Assert.Equal(32, palette.Colors.Count);
        Assert.Equal(new PaletteColor("ink", 21, 18, 28), palette.Get("ink"));
        Assert.Equal(new PaletteColor("snow", 241, 237, 226), palette.Get("snow"));
        Assert.True(palette.Contains(21, 18, 28));
        Assert.False(palette.Contains(21, 18, 29));
    }

    [Fact]
    [Trait("TS", "TS-palette-002")]
    public void FromHex_定義が正しくない_作れない()
    {
        var thirtyOne = Fillers(31);
        var thirtyThree = Fillers(33);
        Assert.Throws<ArgumentException>(() => Palette.FromHex(thirtyOne));
        Assert.Throws<ArgumentException>(() => Palette.FromHex(thirtyThree));
        Assert.Throws<ArgumentException>(() => Build(("a", "#000000"), ("a", "#010101"))); // 名前の重複
        Assert.Throws<ArgumentException>(() => Build(("a", "#000000"), ("b", "#000000"))); // 色の値の重複
        Assert.Throws<ArgumentException>(() => Build(("a", "#00000"))); // 桁が足りない
        Assert.Throws<ArgumentException>(() => Build(("a", "000000X"))); // # が無い
        Assert.Throws<ArgumentException>(() => Build(("a", "#GG0000"))); // 16 進数ではない
        Assert.Throws<ArgumentException>(() => Build((" ", "#010203"))); // 名前が空
    }

    [Fact]
    [Trait("TS", "TS-palette-003")]
    public void Get_無い名前_例外になり黙って別の色にしない()
    {
        var palette = Build(("ink", "#15121c"));

        Assert.Throws<KeyNotFoundException>(() => palette.Get("inc"));
    }

    [Fact]
    [Trait("TS", "TS-palette-004")]
    public void Nearest_2色の間_差の二乗の和が小さい方を選ぶ()
    {
        // low と high は R が 40 離れている。中間は R = 120
        var palette = Build(("low", "#646464"), ("high", "#8C6464"));

        Assert.Equal("low", palette.Nearest(119, 100, 100).Name); // 差の二乗の和 361 < 441
        Assert.Equal("high", palette.Nearest(121, 100, 100).Name); // 441 > 361
        Assert.Equal("low", palette.Nearest(100, 100, 100).Name); // パレットの色はそのまま
        Assert.Equal("high", palette.Nearest(140, 100, 100).Name);
    }

    [Fact]
    [Trait("TS", "TS-palette-005")]
    public void Nearest_同じ近さ_定義の順で先の色を選ぶ()
    {
        var blackFirst = Build(("black", "#000000"), ("two", "#020000"));
        var twoFirst = Build(("two", "#020000"), ("black", "#000000"));

        // (1,0,0) は黒からも (2,0,0) からも差の二乗の和が 1
        Assert.Equal("black", blackFirst.Nearest(1, 0, 0).Name);
        Assert.Equal("two", twoFirst.Nearest(1, 0, 0).Name);
    }
}
