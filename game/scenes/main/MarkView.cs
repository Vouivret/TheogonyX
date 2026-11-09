using System.Collections.Generic;
using System.Linq;
using Godot;
using TheogonyX.Core;

namespace TheogonyX.Scenes.Main;

/// <summary>
/// 読みの跡の印（祭壇・柵・切り株・壁。docs/domain/script-decipher.md の 9、docs/domain/structures.md の印）を、
/// 1 マス（16 × 16 px）の絵で地表の上に描く。建物と同じ Y ソートのレイヤーに置き、原点を足元（マスの下の辺の中央）にする。
/// 絵はまだ無いので、コードで描く仮の絵を使う（後で差し替える）。中身はコアの <see cref="Civilization.Marks"/> が持つ。
/// </summary>
public sealed class MarkView
{
    private const int Size = 16;

    private static readonly Color Clear = new(0, 0, 0, 0);
    private static readonly Color Outline = new("2b2b2b");
    private static readonly Color StoneLight = new("a3a3a3");
    private static readonly Color Stone = new("787878");
    private static readonly Color StoneDark = new("535353");
    private static readonly Color Wood = new("8a5a2b");
    private static readonly Color WoodDark = new("5c3a1a");
    private static readonly Color WoodRing = new("c99d5f");
    private static readonly Color Flame = new("e8742a");
    private static readonly Color FlameCore = new("f6d24a");
    private static readonly Color Earth = new("7a5c3a");
    private static readonly Color EarthDark = new("5e4630");

    private readonly TimeControl _timeControl;
    private readonly Node _actors;
    private readonly Vector2 _terrainOrigin;
    private readonly Dictionary<string, Texture2D> _textures;
    private readonly List<Sprite2D> _sprites = [];
    private string _shown = string.Empty;

    /// <param name="owner">ノードを探すためのシーンの根。</param>
    public MarkView(Node owner, TimeControl timeControl)
    {
        _timeControl = timeControl;
        _actors = owner.GetNode<Sprite2D>("%Prophet").GetParent();
        _terrainOrigin = owner.GetNode<TileMapLayer>("%Terrain").Position;
        _textures = new Dictionary<string, Texture2D>
        {
            ["altar"] = Draw(DrawAltar),
            ["fence"] = Draw(DrawFence),
            ["stump"] = Draw(DrawStump),
            ["cleared"] = Draw(DrawCleared),
            ["wall"] = Draw(DrawWall),
        };
        Refresh();
    }

    /// <summary>印の足元の位置（マスの下の辺の中央）。住人が印のそばへ歩くのにも使う。</summary>
    public static Vector2 FootPosition(Vector2 terrainOrigin, Mark mark) =>
        terrainOrigin + new Vector2((mark.Column + 0.5f) * WorldScreen.TileSize, (mark.Row + 1) * WorldScreen.TileSize);

    /// <summary>地表を描き直した後に呼ぶ（年が進んだ・リセットした・読み込んだ）。</summary>
    public void Refresh(TileType[,] _) => Refresh();

    private void Refresh()
    {
        var marks = _timeControl.Civilization?.Marks ?? [];
        var signature = string.Join('|', marks.Select(mark => $"{mark.Reading}{mark.SymbolId}{mark.Column},{mark.Row}"));
        if (signature == _shown)
        {
            return;
        }
        _shown = signature;
        foreach (var sprite in _sprites)
        {
            sprite.QueueFree();
        }
        _sprites.Clear();
        foreach (var mark in marks)
        {
            if (TextureOf(mark) is not { } texture)
            {
                continue;
            }
            var sprite = new Sprite2D
            {
                Name = $"Mark{mark.Region}{mark.Reading}{mark.SymbolId}",
                Texture = texture,
                Centered = false,
                Offset = new Vector2(-Size / 2, -Size),
                Position = FootPosition(_terrainOrigin, mark),
                LightMask = 1 | SpriteLighting.LitMask,
            };
            _actors.AddChild(sprite);
            _sprites.Add(sprite);
        }
    }

    // 読みごとの絵。切り開くは、木なら切り株、ほかの記号なら土の色の更地
    private Texture2D? TextureOf(Mark mark) => mark.Reading switch
    {
        OracleReading.Revere => _textures["altar"],
        OracleReading.Seal => _textures["fence"],
        OracleReading.Clear => _textures[mark.SymbolId == "SYM_TREE" ? "stump" : "cleared"],
        OracleReading.Arm => _textures["wall"],
        _ => null,
    };

    private static ImageTexture Draw(System.Action<Image> paint)
    {
        var image = Image.CreateEmpty(Size, Size, false, Image.Format.Rgba8);
        image.Fill(Clear);
        paint(image);
        return ImageTexture.CreateFromImage(image);
    }

    // 輪郭付きの長方形（両端を含む）
    private static void Box(Image image, int left, int top, int right, int bottom, Color fill, Color? outline = null)
    {
        image.FillRect(new Rect2I(left, top, right - left + 1, bottom - top + 1), outline ?? fill);
        if (outline is not null && right - left >= 2 && bottom - top >= 2)
        {
            image.FillRect(new Rect2I(left + 1, top + 1, right - left - 1, bottom - top - 1), fill);
        }
    }

    // 石の祭壇と、上で燃える小さな火
    private static void DrawAltar(Image image)
    {
        Box(image, 3, 7, 12, 9, StoneLight, Outline);
        Box(image, 5, 9, 10, 12, Stone, Outline);
        Box(image, 3, 12, 12, 15, StoneDark, Outline);
        image.FillRect(new Rect2I(6, 4, 4, 3), Flame);
        image.FillRect(new Rect2I(7, 3, 2, 3), FlameCore);
    }

    // 木の杭を 2 本の横木でつないだ柵
    private static void DrawFence(Image image)
    {
        foreach (var x in new[] { 1, 6, 11 })
        {
            Box(image, x, 4, x + 3, 15, Wood, Outline);
        }
        image.FillRect(new Rect2I(0, 7, Size, 2), WoodDark);
        image.FillRect(new Rect2I(0, 12, Size, 2), WoodDark);
    }

    // 年輪の見える切り株
    private static void DrawStump(Image image)
    {
        Box(image, 4, 9, 11, 15, Wood, Outline);
        Box(image, 4, 7, 11, 10, WoodRing, Outline);
        image.FillRect(new Rect2I(7, 8, 2, 2), WoodDark);
        image.FillRect(new Rect2I(2, 14, 2, 2), WoodDark);
        image.FillRect(new Rect2I(12, 14, 2, 2), WoodDark);
    }

    // 掘り返された土の更地
    private static void DrawCleared(Image image)
    {
        image.FillRect(new Rect2I(3, 11, 10, 5), Earth);
        image.FillRect(new Rect2I(1, 12, 14, 3), Earth);
        foreach (var (x, y) in new[] { (3, 12), (7, 13), (11, 12), (5, 14), (10, 14) })
        {
            image.SetPixel(x, y, EarthDark);
        }
    }

    // 石を積んだ壁（上に凹凸）
    private static void DrawWall(Image image)
    {
        Box(image, 0, 5, 15, 15, Stone, Outline);
        foreach (var x in new[] { 0, 6, 12 })
        {
            Box(image, x, 2, x + 3, 5, Stone, Outline);
        }
        foreach (var y in new[] { 8, 11 })
        {
            image.FillRect(new Rect2I(1, y, 14, 1), StoneDark);
        }
        foreach (var (x, y) in new[] { (5, 6), (10, 6), (3, 9), (8, 9), (13, 9), (5, 12), (10, 12) })
        {
            image.FillRect(new Rect2I(x, y, 1, 2), StoneDark);
        }
    }
}
