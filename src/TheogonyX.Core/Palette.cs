using System.Globalization;

namespace TheogonyX.Core;

/// <summary>パレットの 1 色。</summary>
public readonly record struct PaletteColor(string Name, byte R, byte G, byte B);

/// <summary>
/// ゲーム全体で使う 32 色のパレット（docs/art-guide.md の「パレット」、plan/feat-palette-art.md）。
/// 色は名前で引き、絵の減色では、いちばん近い色（RGB の差の二乗の和が最小）を選ぶ。
/// 色の値は <c>game/data/palette.json</c> が持ち、ここは定義の検証と引き方だけを受け持つ。
/// </summary>
public sealed class Palette
{
    /// <summary>パレットの色の数。</summary>
    public const int ColorCount = 32;

    private readonly PaletteColor[] _colors;
    private readonly Dictionary<string, PaletteColor> _byName;

    /// <param name="colors">定義の順に並べた色。名前と色の値は重複できない。</param>
    /// <exception cref="ArgumentException">色が <see cref="ColorCount"/> 個でない、名前が空か重複、同じ色の値がある。</exception>
    public Palette(IReadOnlyList<PaletteColor> colors)
    {
        ArgumentNullException.ThrowIfNull(colors);
        if (colors.Count != ColorCount)
        {
            throw new ArgumentException($"パレットの色の数が {ColorCount} ではない: count={colors.Count}", nameof(colors));
        }
        _colors = [.. colors];
        _byName = new Dictionary<string, PaletteColor>(StringComparer.Ordinal);
        var values = new HashSet<(byte, byte, byte)>();
        foreach (var color in _colors)
        {
            if (string.IsNullOrWhiteSpace(color.Name) || !_byName.TryAdd(color.Name, color))
            {
                throw new ArgumentException($"パレットの色の名前が空か重複している: name={color.Name}", nameof(colors));
            }
            if (!values.Add((color.R, color.G, color.B)))
            {
                throw new ArgumentException($"パレットに同じ色の値がある: name={color.Name}", nameof(colors));
            }
        }
    }

    /// <summary>定義の順に並べた色。</summary>
    public IReadOnlyList<PaletteColor> Colors => _colors;

    /// <summary>名前と <c>#RRGGBB</c> の組から作る。</summary>
    /// <exception cref="ArgumentException">色の形が <c>#RRGGBB</c> ではない、または <see cref="Palette(IReadOnlyList{PaletteColor})"/> の条件を満たさない。</exception>
    public static Palette FromHex(IEnumerable<(string Name, string Hex)> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);
        return new Palette([.. entries.Select(entry => ParseHex(entry.Name, entry.Hex))]);
    }

    /// <summary>名前で色を引く。</summary>
    /// <exception cref="KeyNotFoundException">その名前の色が無い（起動時に気付けるよう、黙って別の色にしない）。</exception>
    public PaletteColor Get(string name) =>
        _byName.TryGetValue(name, out var color) ? color : throw new KeyNotFoundException($"パレットに無い色の名前: name={name}");

    /// <summary>この色の値がパレットにあるか。</summary>
    public bool Contains(byte r, byte g, byte b) => _colors.Any(color => color.R == r && color.G == g && color.B == b);

    /// <summary>
    /// いちばん近い色を返す。近さは RGB の差の二乗の和で測り、同じ近さなら定義の順で先の色を選ぶ。
    /// </summary>
    public PaletteColor Nearest(byte r, byte g, byte b)
    {
        var best = _colors[0];
        var bestDistance = int.MaxValue;
        foreach (var color in _colors)
        {
            var dr = color.R - r;
            var dg = color.G - g;
            var db = color.B - b;
            var distance = (dr * dr) + (dg * dg) + (db * db);
            if (distance < bestDistance)
            {
                best = color;
                bestDistance = distance;
            }
        }
        return best;
    }

    private static PaletteColor ParseHex(string name, string hex)
    {
        if (hex is null || hex.Length != 7 || hex[0] != '#'
            || !int.TryParse(hex.AsSpan(1), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out var rgb))
        {
            throw new ArgumentException($"色の形が #RRGGBB ではない: name={name} value={hex}", nameof(hex));
        }
        return new PaletteColor(name, (byte)((rgb >> 16) & 0xFF), (byte)((rgb >> 8) & 0xFF), (byte)(rgb & 0xFF));
    }
}
