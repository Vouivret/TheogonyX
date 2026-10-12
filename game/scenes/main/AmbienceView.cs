using System;
using Godot;
using TheogonyX.Core;
using TheogonyX.Data;

namespace TheogonyX.Scenes.Main;

/// <summary>昼夜の環境光と予言者（docs/domain/ambience.md）、焚き火（docs/domain/visual-effects.md）を描く。</summary>
public sealed class AmbienceView
{
    private const string IdleTexturePath = "res://assets/sprites/prophet_idle.png";
    private const string FallenTexturePath = "res://assets/sprites/prophet_fallen.png";

    // 仮の予言者の色（DB16 から選ぶ。docs/art-guide.md）。本番の絵が来たら使わなくなる。
    private static readonly Color PlaceholderRobe = Color.FromHtml("#597dce");
    private static readonly Color PlaceholderSkin = Color.FromHtml("#d2aa99");
    private static readonly Color PlaceholderEye = Color.FromHtml("#140c1c");
    private static readonly Color PlaceholderWood = Color.FromHtml("#854c30");
    private static readonly Color PlaceholderFlameOuter = Color.FromHtml("#d27d2c");
    private static readonly Color PlaceholderFlameInner = Color.FromHtml("#dad45e");

    // 明かりのテクスチャの一辺（px）。届く半径に合わせて TextureScale で拡大する。
    private const int LightTextureSize = 64;

    private readonly TimeControl _timeControl;
    private readonly AmbienceRules _rules;
    private readonly ProphetSettings _prophetSettings;
    private readonly CanvasModulate _ambience;
    private readonly Sprite2D _prophet;
    private readonly Texture2D _idleTexture;
    private readonly Texture2D _fallenTexture;
    private readonly CampfireSettings _campfireSettings;
    private readonly Sprite2D _flame;
    private readonly PointLight2D _campfireLight;
    // 揺らぎは描画だけの演出なので、乱数ではなく決まった形のノイズを時間で引く（.agent/rules/project.md の 3 章）
    private readonly FastNoiseLite _flicker = new() { NoiseType = FastNoiseLite.NoiseTypeEnum.Perlin };

    // 昼夜の周期と待機アニメーションの経過秒。崩壊している間は数えない。
    private double _cycleSeconds;
    private double _animationSeconds;
    private double _flickerSeconds;

    /// <param name="owner">ノードを探すためのシーンの根。</param>
    public AmbienceView(Node owner, TimeControl timeControl, AmbienceRules rules, ProphetSettings prophetSettings, CampfireSettings campfireSettings)
    {
        _timeControl = timeControl;
        _rules = rules;
        _prophetSettings = prophetSettings;
        _campfireSettings = campfireSettings;
        _ambience = owner.GetNode<CanvasModulate>("%Ambience");
        _prophet = owner.GetNode<Sprite2D>("%Prophet");
        var terrain = owner.GetNode<TileMapLayer>("%Terrain");

        _idleTexture = LoadOrPlaceholder(IdleTexturePath, CreatePlaceholderIdle);
        _fallenTexture = LoadOrPlaceholder(FallenTexturePath, CreatePlaceholderFallen);

        // 足元を地表の一番上の行の上端に合わせる。原点は足元（Y ソートで足元の高さを比べるため。ShowTexture で Offset を合わせる）。
        _prophet.Position = terrain.Position + new Vector2((prophetSettings.Column - 1) * WorldScreen.TileSize, 0);

        (_flame, _campfireLight) = CreateCampfire(_prophet.GetParent(), terrain.Position, campfireSettings);

        Process(0);
    }

    /// <summary>毎フレーム呼ぶ。ログは出さない。</summary>
    public void Process(double delta)
    {
        var world = _timeControl.World;
        if (!world.IsDestroyed)
        {
            _cycleSeconds += delta;
            _animationSeconds += delta;
        }
        _flickerSeconds += delta;

        var light = AmbientLight.Compute(_cycleSeconds, world, _rules);
        _ambience.Color = new Color(light.R, light.G, light.B);

        var brightness = CampfireLight.Brightness(_cycleSeconds, world, _rules);
        var flicker = 1f + (_campfireSettings.FlickerAmount * _flicker.GetNoise1D((float)(_flickerSeconds * _campfireSettings.FlickerSpeed)));
        _campfireLight.Energy = _campfireSettings.Energy * brightness * flicker;
        _campfireLight.Visible = brightness > 0f;
        _flame.Visible = brightness > 0f;
        _flame.Modulate = new Color(1f, 1f, 1f, brightness);

        if (world.IsDestroyed)
        {
            ShowTexture(_fallenTexture);
            _prophet.Frame = 0;
        }
        else
        {
            ShowTexture(_idleTexture);
            var frame = (long)(_animationSeconds / _prophetSettings.FrameSeconds) % _prophet.Hframes;
            _prophet.Frame = (int)frame;
        }
    }

    // 薪は昼も置いておき、炎と明かりは灯っている間だけ出す。足元は予言者と同じく地表の上端に合わせ、原点を足元にする。
    private static (Sprite2D Flame, PointLight2D Light) CreateCampfire(Node parent, Vector2 terrainPosition, CampfireSettings settings)
    {
        var size = WorldScreen.TileSize;
        var foot = terrainPosition + new Vector2((settings.Column - 1) * size, 0);

        var wood = new Sprite2D
        {
            Name = "CampfireWood",
            Centered = false,
            Offset = new Vector2(0, -size),
            Position = foot,
            Texture = ImageTexture.CreateFromImage(CreatePlaceholderWood()),
        };
        var flame = new Sprite2D
        {
            Name = "CampfireFlame",
            Centered = false,
            Offset = new Vector2(0, -size),
            Position = foot,
            Texture = ImageTexture.CreateFromImage(CreatePlaceholderFlame()),
        };

        var gradient = new Gradient();
        gradient.SetColor(0, Colors.White);
        gradient.SetColor(1, new Color(1f, 1f, 1f, 0f));
        var light = new PointLight2D
        {
            Name = "CampfireLight",
            Position = foot + new Vector2(size / 2f, -4),
            Color = new Color(settings.Color.R, settings.Color.G, settings.Color.B),
            Texture = new GradientTexture2D
            {
                Gradient = gradient,
                Width = LightTextureSize,
                Height = LightTextureSize,
                Fill = GradientTexture2D.FillEnum.Radial,
                FillFrom = new Vector2(0.5f, 0.5f),
                FillTo = new Vector2(1f, 0.5f),
            },
            TextureScale = settings.RadiusTiles * size * 2f / LightTextureSize,
        };

        parent.AddChild(wood);
        parent.AddChild(flame);
        parent.AddChild(light);
        return (flame, light);
    }

    private void ShowTexture(Texture2D texture)
    {
        if (_prophet.Texture == texture)
        {
            return;
        }
        _prophet.Texture = texture;
        // 横一列のスプライトシート。1 コマは縦の高さと同じ幅の正方形（docs/art-guide.md）。
        _prophet.Hframes = Mathf.Max(1, texture.GetWidth() / texture.GetHeight());
        _prophet.Offset = new Vector2(0, -texture.GetHeight());
        _prophet.Frame = 0;
    }

    private static Texture2D LoadOrPlaceholder(string path, Func<Image> createPlaceholder) =>
        ResourceLoader.Exists(path)
            ? GD.Load<Texture2D>(path)
            : ImageTexture.CreateFromImage(createPlaceholder());

    // 仮の待機アニメーション 2 コマ: 1 コマ目は目を開けて立つ、2 コマ目は息を吐いて 1 px 沈み、目を閉じる。
    private static Image CreatePlaceholderIdle()
    {
        var size = WorldScreen.TileSize;
        var image = Image.CreateEmpty(size * 2, size, false, Image.Format.Rgba8);
        for (var frame = 0; frame < 2; frame++)
        {
            var left = frame * size;
            var sink = frame;
            image.FillRect(new Rect2I(left + 6, 2 + sink, 4, 4), PlaceholderSkin);
            image.FillRect(new Rect2I(left + 5, 6 + sink, 6, 10 - sink), PlaceholderRobe);
            if (frame == 0)
            {
                image.SetPixel(left + 8, 3, PlaceholderEye);
            }
        }
        return image;
    }

    // 仮の薪: 交差した 2 本の丸太。
    private static Image CreatePlaceholderWood()
    {
        var size = WorldScreen.TileSize;
        var image = Image.CreateEmpty(size, size, false, Image.Format.Rgba8);
        image.FillRect(new Rect2I(3, 14, 10, 2), PlaceholderWood);
        image.FillRect(new Rect2I(5, 13, 6, 1), PlaceholderWood);
        return image;
    }

    // 仮の炎: 外側が橙、内側が黄。
    private static Image CreatePlaceholderFlame()
    {
        var size = WorldScreen.TileSize;
        var image = Image.CreateEmpty(size, size, false, Image.Format.Rgba8);
        image.FillRect(new Rect2I(6, 7, 4, 6), PlaceholderFlameOuter);
        image.FillRect(new Rect2I(7, 5, 2, 2), PlaceholderFlameOuter);
        image.FillRect(new Rect2I(7, 9, 2, 4), PlaceholderFlameInner);
        return image;
    }

    private static Image CreatePlaceholderFallen()
    {
        var size = WorldScreen.TileSize;
        var image = Image.CreateEmpty(size, size, false, Image.Format.Rgba8);
        image.FillRect(new Rect2I(1, 13, 11, 3), PlaceholderRobe);
        image.FillRect(new Rect2I(12, 12, 4, 4), PlaceholderSkin);
        return image;
    }
}
