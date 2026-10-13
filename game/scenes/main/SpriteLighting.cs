using System;
using Godot;
using TheogonyX.Data;

namespace TheogonyX.Scenes.Main;

/// <summary>
/// 予言者と建物の絵に、法線マップの陰影と足元の落ち影を付ける（docs/domain/visual-effects.md）。
/// 法線マップは <c>&lt;名前&gt;_normal.png</c> があればそれを使い、無ければ元の絵の不透明度と明るさから作る。
/// </summary>
public static class SpriteLighting
{
    /// <summary>法線マップの陰影を受ける絵の光のマスク。<c>DirectionalLight2D</c> はこの絵だけに当てる。</summary>
    public const int LitMask = 2;

    // 縁から何 px までを丸く盛り上げるか。明るさは高さにどれだけ混ぜるか。傾きを何倍に強めるか。
    private const int Bevel = 3;
    private const float LuminanceWeight = 0.4f;
    private const float Strength = 2f;

    /// <summary>
    /// 元の絵と法線マップを <c>CanvasTexture</c> にまとめる。
    /// </summary>
    /// <param name="frameWidth">横一列のスプライトシートの 1 コマの幅。コマの境目を絵の縁として扱う。</param>
    public static Texture2D WithNormalMap(Texture2D diffuse, string diffusePath, int frameWidth)
    {
        var normalPath = diffusePath.Replace(".png", "_normal.png");
        var normal = ResourceLoader.Exists(normalPath)
            ? GD.Load<Texture2D>(normalPath)
            : ImageTexture.CreateFromImage(CreateNormalImage(diffuse.GetImage(), frameWidth));
        return new CanvasTexture { DiffuseTexture = diffuse, NormalTexture = normal };
    }

    /// <summary>
    /// <paramref name="owner"/> の足元に、半透明の黒い楕円の影を置く。影は絵の後ろ（地表の上）に描く。
    /// </summary>
    /// <param name="footCenter">絵の原点から見た、足元の中央の位置。</param>
    public static Sprite2D AddShadow(Sprite2D owner, int frameWidth, Vector2 footCenter, LightingSettings settings)
    {
        var width = Math.Max(1, (int)MathF.Round(frameWidth * settings.ShadowWidthRatio));
        var height = settings.ShadowHeight;
        var shadow = new Sprite2D
        {
            Name = "Shadow",
            Texture = ImageTexture.CreateFromImage(CreateEllipse(width, height, settings.ShadowOpacity)),
            Centered = false,
            // 楕円の中心を足元の 1 px 上に置き、下半分が地面にはみ出すようにする
            Offset = footCenter - new Vector2(width / 2, height / 2 + 1),
            ShowBehindParent = true,
            LightMask = 0,
        };
        owner.AddChild(shadow);
        return shadow;
    }

    // ドットの縁を残すため、楕円は塗るか塗らないかの 2 値にする（なめらかにしない）
    private static Image CreateEllipse(int width, int height, float opacity)
    {
        var image = Image.CreateEmpty(width, height, false, Image.Format.Rgba8);
        var color = new Color(0f, 0f, 0f, opacity);
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var dx = (x + 0.5f - width / 2f) / (width / 2f);
                var dy = (y + 0.5f - height / 2f) / (height / 2f);
                if (dx * dx + dy * dy <= 1f)
                {
                    image.SetPixel(x, y, color);
                }
            }
        }
        return image;
    }

    // 高さ = 縁からの距離（Bevel px で頭打ち）と明るさを混ぜたもの。法線は高さの傾きから求める。
    // 緑は上向き（OpenGL の向き。Godot の法線マップと同じ）。
    private static Image CreateNormalImage(Image source, int frameWidth)
    {
        var image = (Image)source.Duplicate();
        if (image.IsCompressed())
        {
            image.Decompress();
        }
        var width = image.GetWidth();
        var height = image.GetHeight();

        var heights = new float[width, height];
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var color = image.GetPixel(x, y);
                if (color.A < 0.5f)
                {
                    continue;
                }
                var edge = DistanceToEdge(image, x, y, frameWidth);
                heights[x, y] = ((1f - LuminanceWeight) * edge / Bevel) + (LuminanceWeight * color.Luminance);
            }
        }

        var normal = Image.CreateEmpty(width, height, false, Image.Format.Rgba8);
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var frameLeft = x / frameWidth * frameWidth;
                float At(int px, int py) =>
                    px < frameLeft || px >= frameLeft + frameWidth || px >= width || py < 0 || py >= height ? 0f : heights[px, py];

                var n = new Vector3(
                    (At(x - 1, y) - At(x + 1, y)) * Strength,
                    (At(x, y + 1) - At(x, y - 1)) * Strength,
                    1f).Normalized();
                normal.SetPixel(x, y, new Color((n.X * 0.5f) + 0.5f, (n.Y * 0.5f) + 0.5f, (n.Z * 0.5f) + 0.5f));
            }
        }
        return normal;
    }

    // 透明な点・コマの外までの距離（チェビシェフ距離。Bevel で頭打ち）
    private static int DistanceToEdge(Image image, int x, int y, int frameWidth)
    {
        var frameLeft = x / frameWidth * frameWidth;
        for (var d = 1; d <= Bevel; d++)
        {
            for (var oy = -d; oy <= d; oy++)
            {
                for (var ox = -d; ox <= d; ox++)
                {
                    var px = x + ox;
                    var py = y + oy;
                    if (px < frameLeft || px >= frameLeft + frameWidth || px >= image.GetWidth()
                        || py < 0 || py >= image.GetHeight() || image.GetPixel(px, py).A < 0.5f)
                    {
                        return d - 1;
                    }
                }
            }
        }
        return Bevel;
    }
}
