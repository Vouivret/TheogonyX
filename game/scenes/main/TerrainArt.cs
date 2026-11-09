using System;
using System.Collections.Generic;
using Godot;
using TheogonyX.Core;

namespace TheogonyX.Scenes.Main;

/// <summary>
/// 地表のタイルの絵（16 × 16 px）と、上の帯の世界の値のアイコン（10 × 10 px）を、パレットの色でコードから描く
/// （docs/art-guide.md の「パレット」、plan/feat-palette-art.md）。
/// タイルは種類ごとに <see cref="Variants"/> 通りの模様を持ち、マスの位置で使う模様を変えるので、同じ種類が並んでも模様が揃わない。
/// 見た目だけの絵で、シミュレーションの結果には影響しない。
/// </summary>
public static class TerrainArt
{
    /// <summary>タイルの 1 種類あたりの模様の数。</summary>
    public const int Variants = 4;

    // 地の色と、模様の描き方（名前はパレットの色）
    private static readonly Dictionary<TileType, string> BaseColors = new()
    {
        [TileType.Grassland] = "grass",
        [TileType.Jungle] = "forest",
        [TileType.Desert] = "dune",
        [TileType.Scorched] = "soil",
        [TileType.Glacier] = "ice",
        [TileType.Water] = "water",
    };

    // アイコンの絵。'.' は透明、ほかは文字ごとにパレットの色
    private static readonly Dictionary<char, string> IconColors = new()
    {
        ['o'] = "ink",
        ['L'] = "leaf",
        ['D'] = "forest",
        ['B'] = "bark",
        ['W'] = "water",
        ['S'] = "sky",
        ['N'] = "snow",
        ['E'] = "ember",
        ['R'] = "red",
        ['Y'] = "sun",
    };

    private static readonly Dictionary<WorldMetric, string[]> Icons = new()
    {
        [WorldMetric.ForestAmount] =
        [
            "...oooo...",
            "..oLLLLo..",
            ".oLLLLLLo.",
            ".oLLLDLLo.",
            ".oLDLLLLo.",
            "..oLLLLo..",
            "...oooo...",
            "....oBo...",
            "....oBo...",
            "...ooBoo..",
        ],
        [WorldMetric.WaterAmount] =
        [
            "....oo....",
            "....oo....",
            "...oWWo...",
            "..oWWWWo..",
            ".oWWSWWWo.",
            ".oWSWWWWo.",
            ".oWWWWWWo.",
            "..oWWWWo..",
            "...oooo...",
            "..........",
        ],
        [WorldMetric.Temperature] =
        [
            "....oo....",
            "...oNNo...",
            "...oNNo...",
            "...oEEo...",
            "...oEEo...",
            "..oEEEEo..",
            "..oEEEEo..",
            "..oEEEEo..",
            "...oooo...",
            "..........",
        ],
        [WorldMetric.ChaosLevel] =
        [
            ".....oooo.",
            "....oRRRo.",
            "...oRRRo..",
            "..oRRRo...",
            ".oRRRRRRo.",
            "..oooRRo..",
            "....oRRo..",
            "...oRRo...",
            "...oRo....",
            "....o.....",
        ],
        [WorldMetric.HarmonyLevel] =
        [
            "...oooo...",
            "..oYYYYo..",
            ".oYYYYYYo.",
            ".oYYooYYo.",
            ".oYYooYYo.",
            ".oYYYYYYo.",
            "..oYYYYo..",
            "...oooo...",
            "..........",
            "..........",
        ],
    };

    /// <summary>
    /// 地表のタイルの絵を、種類ごとに <see cref="Variants"/> 通りずつ横に並べた 1 枚（タイルの列は <c>種類 × Variants + 模様</c>）。
    /// </summary>
    public static Image CreateTileAtlas(int tileSize)
    {
        var tileTypes = Enum.GetValues<TileType>();
        var image = Image.CreateEmpty(tileSize * tileTypes.Length * Variants, tileSize, false, Image.Format.Rgba8);
        foreach (var tileType in tileTypes)
        {
            for (var variant = 0; variant < Variants; variant++)
            {
                var origin = ((int)tileType * Variants + variant) * tileSize;
                DrawTile(image, origin, tileSize, tileType, variant);
            }
        }
        return image;
    }

    /// <summary>マスの (列, 行) で、使う模様を決める（同じマスはいつも同じ模様）。</summary>
    public static int VariantAt(int column, int row) =>
        (int)(unchecked((uint)((column * 73856093) ^ (row * 19349663))) % Variants);

    /// <summary>世界の値のアイコン（10 × 10 px）。</summary>
    public static Texture2D CreateIcon(WorldMetric metric)
    {
        var rows = Icons[metric];
        var image = Image.CreateEmpty(rows[0].Length, rows.Length, false, Image.Format.Rgba8);
        for (var y = 0; y < rows.Length; y++)
        {
            for (var x = 0; x < rows[y].Length; x++)
            {
                if (rows[y][x] != '.')
                {
                    image.SetPixel(x, y, GamePalette.Of(IconColors[rows[y][x]]));
                }
            }
        }
        return ImageTexture.CreateFromImage(image);
    }

    private static void DrawTile(Image image, int originX, int size, TileType type, int variant)
    {
        image.FillRect(new Rect2I(originX, 0, size, size), GamePalette.Of(BaseColors[type]));
        var rng = new Random(((int)type * 1009) + (variant * 31) + 7);
        switch (type)
        {
            case TileType.Grassland:
                Tufts(image, originX, size, rng, "leaf", 5);
                Specks(image, originX, size, rng, "meadow", 4);
                break;
            case TileType.Jungle:
                Blobs(image, originX, size, rng, "deep_forest", 4);
                Specks(image, originX, size, rng, "leaf", 6);
                break;
            case TileType.Desert:
                Dashes(image, originX, size, rng, "sand", 5, 3);
                Specks(image, originX, size, rng, "bone", 3);
                break;
            case TileType.Scorched:
                Blobs(image, originX, size, rng, "ink", 3);
                Dashes(image, originX, size, rng, "bark", 4, 2);
                Specks(image, originX, size, rng, "ember", 1);
                break;
            case TileType.Glacier:
                Dashes(image, originX, size, rng, "sky", 4, 3);
                Specks(image, originX, size, rng, "snow", 5);
                break;
            case TileType.Water:
                Dashes(image, originX, size, rng, "sea", 5, 3);
                Dashes(image, originX, size, rng, "sky", 3, 2);
                break;
        }
    }

    private static void Put(Image image, int originX, int x, int y, Color color) => image.SetPixel(originX + x, y, color);

    private static void Specks(Image image, int originX, int size, Random rng, string name, int count)
    {
        var color = GamePalette.Of(name);
        for (var i = 0; i < count; i++)
        {
            Put(image, originX, rng.Next(size), rng.Next(size), color);
        }
    }

    // 縦 2 画素の草の穂
    private static void Tufts(Image image, int originX, int size, Random rng, string name, int count)
    {
        var color = GamePalette.Of(name);
        for (var i = 0; i < count; i++)
        {
            var x = rng.Next(size);
            var y = rng.Next(size - 1);
            Put(image, originX, x, y, color);
            Put(image, originX, x, y + 1, color);
        }
    }

    // 2 × 2 の塊
    private static void Blobs(Image image, int originX, int size, Random rng, string name, int count)
    {
        var color = GamePalette.Of(name);
        for (var i = 0; i < count; i++)
        {
            var x = rng.Next(size - 1);
            var y = rng.Next(size - 1);
            for (var dy = 0; dy < 2; dy++)
            {
                for (var dx = 0; dx < 2; dx++)
                {
                    Put(image, originX, x + dx, y + dy, color);
                }
            }
        }
    }

    // 横の短い線
    private static void Dashes(Image image, int originX, int size, Random rng, string name, int count, int length)
    {
        var color = GamePalette.Of(name);
        for (var i = 0; i < count; i++)
        {
            var x = rng.Next(size - length + 1);
            var y = rng.Next(size);
            for (var dx = 0; dx < length; dx++)
            {
                Put(image, originX, x + dx, y, color);
            }
        }
    }
}
