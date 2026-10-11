using System;
using Godot;
using TheogonyX.Core;
using TheogonyX.Data;

namespace TheogonyX.Scenes.Main;

/// <summary>昼夜の環境光と予言者を描く（docs/domain/ambience.md）。</summary>
public sealed class AmbienceView
{
    private const string IdleTexturePath = "res://assets/sprites/prophet_idle.png";
    private const string FallenTexturePath = "res://assets/sprites/prophet_fallen.png";

    // 仮の予言者の色（DB16 から選ぶ。docs/art-guide.md）。本番の絵が来たら使わなくなる。
    private static readonly Color PlaceholderRobe = Color.FromHtml("#597dce");
    private static readonly Color PlaceholderSkin = Color.FromHtml("#d2aa99");
    private static readonly Color PlaceholderEye = Color.FromHtml("#140c1c");

    private readonly TimeControl _timeControl;
    private readonly AmbienceRules _rules;
    private readonly ProphetSettings _prophetSettings;
    private readonly CanvasModulate _ambience;
    private readonly Sprite2D _prophet;
    private readonly Texture2D _idleTexture;
    private readonly Texture2D _fallenTexture;

    // 昼夜の周期と待機アニメーションの経過秒。崩壊している間は数えない。
    private double _cycleSeconds;
    private double _animationSeconds;

    /// <param name="owner">ノードを探すためのシーンの根。</param>
    public AmbienceView(Node owner, TimeControl timeControl, AmbienceRules rules, ProphetSettings prophetSettings)
    {
        _timeControl = timeControl;
        _rules = rules;
        _prophetSettings = prophetSettings;
        _ambience = owner.GetNode<CanvasModulate>("%Ambience");
        _prophet = owner.GetNode<Sprite2D>("%Prophet");
        var terrain = owner.GetNode<TileMapLayer>("%Terrain");

        _idleTexture = LoadOrPlaceholder(IdleTexturePath, CreatePlaceholderIdle);
        _fallenTexture = LoadOrPlaceholder(FallenTexturePath, CreatePlaceholderFallen);

        // 足元を地表の一番上の行の上端に合わせる（centered = false なので左上が原点）。
        var frameHeight = _idleTexture.GetHeight();
        _prophet.Position = terrain.Position + new Vector2((prophetSettings.Column - 1) * WorldScreen.TileSize, -frameHeight);

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

        var light = AmbientLight.Compute(_cycleSeconds, world, _rules);
        _ambience.Color = new Color(light.R, light.G, light.B);

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

    private void ShowTexture(Texture2D texture)
    {
        if (_prophet.Texture == texture)
        {
            return;
        }
        _prophet.Texture = texture;
        // 横一列のスプライトシート。1 コマは縦の高さと同じ幅の正方形（docs/art-guide.md）。
        _prophet.Hframes = Mathf.Max(1, texture.GetWidth() / texture.GetHeight());
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

    private static Image CreatePlaceholderFallen()
    {
        var size = WorldScreen.TileSize;
        var image = Image.CreateEmpty(size, size, false, Image.Format.Rgba8);
        image.FillRect(new Rect2I(1, 13, 11, 3), PlaceholderRobe);
        image.FillRect(new Rect2I(12, 12, 4, 4), PlaceholderSkin);
        return image;
    }
}
