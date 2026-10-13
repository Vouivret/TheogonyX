using Godot;
using TheogonyX.Core;

namespace TheogonyX.Scenes.Main;

/// <summary>
/// 地表の外の空（docs/domain/visual-effects.md の「夜空の星」）。地表の後ろに、環境光の色をかけた空の地と、夜に瞬く星を描く。
/// 描画だけで、シミュレーションの結果に影響しない（星の位置は <see cref="CrowdHash"/> で決める）。
/// </summary>
public sealed class SkyView
{
    // TODO(AI_UNSURE): 星の数・大きさ・瞬きの速さ・空の広さは AI が仮に置いた（visual-effects.md の「未決」）。画面で見て人間が決め直す
    private const int TileSize = 128;        // 星の模様の 1 枚の大きさ（px）。空全体に繰り返して敷く
    private const int StarsPerTile = 14;
    private const float SkyMargin = 1024f;   // 地表の外に空を広げる幅（最も引いたズームでも端が見えない広さ）
    private const float TwinkleSpeed = 1.3f; // 瞬きの速さ（ラジアン / 秒）
    private const uint StarSalt = 0x53544152; // "STAR"

    private readonly AmbienceView _ambience;
    private readonly ColorRect _backdrop;
    private readonly Sprite2D[] _stars;
    private readonly Color _clearColor;
    private double _seconds;

    /// <param name="owner">ノードを探すためのシーンの根。</param>
    public SkyView(Node owner, AmbienceView ambience, CivilizationRules rules, int seed)
    {
        _ambience = ambience;
        _clearColor = RenderingServer.GetDefaultClearColor();
        var terrain = owner.GetNode<TileMapLayer>("%Terrain");
        var terrainSize = new Vector2(rules.Regions.Columns, rules.Rows) * WorldScreen.TileSize;
        var skyRect = new Rect2(terrain.Position - new Vector2(SkyMargin, SkyMargin), terrainSize + new Vector2(SkyMargin * 2, SkyMargin * 2));

        // 環境光（CanvasModulate）は地表と同じ層にしかかからないので、空は地表の後ろの別の層に置き、色は自分でかける。
        // カメラに合わせて動くよう、ビューポートに追従させる
        var layer = new CanvasLayer { Name = "Sky", Layer = -1, FollowViewportEnabled = true };
        owner.GetNode<SubViewport>("%WorldViewport").AddChild(layer);
        _backdrop = new ColorRect
        {
            Name = "SkyBackdrop",
            Position = skyRect.Position,
            Size = skyRect.Size,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        layer.AddChild(_backdrop);

        // 瞬きの位相をずらした 2 枚の模様を重ねる
        _stars = new Sprite2D[2];
        for (var i = 0; i < _stars.Length; i++)
        {
            _stars[i] = new Sprite2D
            {
                Name = $"Stars{i}",
                Texture = CreateStars(unchecked((uint)seed) ^ StarSalt, (uint)i),
                Centered = false,
                Position = skyRect.Position,
                RegionEnabled = true,
                RegionRect = new Rect2(Vector2.Zero, skyRect.Size),
                TextureRepeat = CanvasItem.TextureRepeatEnum.Enabled,
                TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
            };
            layer.AddChild(_stars[i]);
        }
        Process(0);
    }

    /// <summary>毎フレーム呼ぶ。空の色を環境光に合わせ、夜の深さで星を出す。</summary>
    public void Process(double delta)
    {
        _seconds += delta;
        var light = _ambience.LightColor;
        _backdrop.Color = new Color(_clearColor.R * light.R, _clearColor.G * light.G, _clearColor.B * light.B);
        var night = _ambience.NightLevel;
        for (var i = 0; i < _stars.Length; i++)
        {
            var twinkle = 0.65f + (0.35f * Mathf.Sin((float)(_seconds * TwinkleSpeed) + (i * Mathf.Pi)));
            _stars[i].Modulate = new Color(1f, 1f, 1f, night * twinkle);
        }
    }

    // 透明な 1 枚に、白い 1 px の星と、明るい星（十字の 5 px）を散らす
    private static ImageTexture CreateStars(uint seed, uint layer)
    {
        var image = Image.CreateEmpty(TileSize, TileSize, false, Image.Format.Rgba8);
        for (uint star = 0; star < StarsPerTile; star++)
        {
            var x = (int)(CrowdHash.Next01(seed, (layer * StarsPerTile) + star, 0) * TileSize);
            var y = (int)(CrowdHash.Next01(seed, (layer * StarsPerTile) + star, 1) * TileSize);
            var bright = CrowdHash.Next01(seed, (layer * StarsPerTile) + star, 2) < 0.15f;
            image.SetPixel(x, y, Colors.White);
            if (bright)
            {
                var dim = new Color(1f, 1f, 1f, 0.5f);
                image.SetPixel((x + 1) % TileSize, y, dim);
                image.SetPixel((x + TileSize - 1) % TileSize, y, dim);
                image.SetPixel(x, (y + 1) % TileSize, dim);
                image.SetPixel(x, (y + TileSize - 1) % TileSize, dim);
            }
        }
        return ImageTexture.CreateFromImage(image);
    }
}
