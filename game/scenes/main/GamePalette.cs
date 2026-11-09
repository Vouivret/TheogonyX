using Godot;
using TheogonyX.Core;
using TheogonyX.Data;

namespace TheogonyX.Scenes.Main;

/// <summary>
/// ゲーム全体のパレット（<c>game/data/palette.json</c>、docs/art-guide.md の「パレット」）を持ち、Godot の色を名前で引く。
/// 起動時に <see cref="Load"/> を 1 回呼ぶ。コードに色の値を直書きせず、ここから名前で引く。無い名前は例外になる（起動時に気付く）。
/// </summary>
public static class GamePalette
{
    private static Palette? _current;

    /// <summary>読み込み済みのパレット。</summary>
    public static Palette Current => _current ?? throw new System.InvalidOperationException("パレットを読み込んでいない");

    /// <summary>パレットを読み込む。読み込み済みなら何もしない。</summary>
    public static void Load() => _current ??= GameDataLoader.ParsePalette(WorldCore.ReadText("res://data/palette.json"));

    /// <summary>パレットの色を名前で引く。</summary>
    public static Color Of(string name)
    {
        var color = Current.Get(name);
        return Color.Color8(color.R, color.G, color.B);
    }

    /// <summary>パレットの色を、アルファを付けて引く。</summary>
    public static Color Of(string name, float alpha)
    {
        var color = Of(name);
        color.A = alpha;
        return color;
    }
}
