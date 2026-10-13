using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Godot;
using TheogonyX.Core;

namespace TheogonyX.Scenes.Main;

/// <summary>
/// HUD のボタンで、地上と地下の層（docs/domain/strata.md）の表示を切り替える。
/// 地下では、地表と同じ格子を層の色で塗り、その層の遺物を暗くした建物の絵で描く。群衆・予言者・焚き火・建物は隠す。
/// </summary>
public sealed class StrataView
{
    // 層の色（DB16 から選ぶ。docs/art-guide.md）。1 層 粘土・2 層 岩盤・3 層 溶岩。4 層より深い定義なら最後の色を使う
    private static readonly Color[] LayerColors = [Color.FromHtml("#854c30"), Color.FromHtml("#4e4a4e"), Color.FromHtml("#d04648")];
    private static readonly Color RelicModulate = new(0.55f, 0.55f, 0.55f);

    private readonly TimeControl _timeControl;
    private readonly CivilizationRules _rules;
    private readonly Button _button;
    private readonly Node2D _actors;
    private readonly CanvasItem _crowd;
    private readonly Node2D _underground;
    private readonly ColorRect _ground;
    private readonly Vector2 _terrainOrigin;
    private readonly Dictionary<BuildingType, Texture2D> _textures = [];
    private readonly List<Sprite2D> _sprites = [];

    // 0 が地上、1 以上が地下の層
    private int _layer;

    /// <param name="owner">ノードを探すためのシーンの根。</param>
    public StrataView(Node owner, TimeControl timeControl, CivilizationRules rules)
    {
        _timeControl = timeControl;
        _rules = rules;
        _button = owner.GetNode<Button>("%LayerButton");
        _actors = (Node2D)owner.GetNode<Sprite2D>("%Prophet").GetParent();
        _crowd = owner.GetNode<CanvasItem>("%Crowd");
        var terrain = owner.GetNode<TileMapLayer>("%Terrain");
        _terrainOrigin = terrain.Position;
        foreach (var (type, path) in BuildingView.TexturePaths)
        {
            _textures[type] = GD.Load<Texture2D>(path);
        }

        // 地表のすぐ上（群衆・予言者より後ろ）に置く。地下の絵は Y ソートしない（遺物は重ならない）
        _underground = new Node2D { Name = "Underground", Visible = false };
        _ground = new ColorRect
        {
            Position = _terrainOrigin,
            Size = new Vector2(rules.Regions.Columns, rules.Rows) * WorldScreen.TileSize,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        _underground.AddChild(_ground);
        terrain.GetParent().AddChild(_underground);
        terrain.GetParent().MoveChild(_underground, terrain.GetIndex() + 1);

        _button.Pressed += OnPressed;
        Show();
    }

    /// <summary>地表を描き直した後に呼ぶ（年が進んだ・リセットした・読み込んだ）。遺物が変わっていれば描き直す。</summary>
    public void Refresh(TileType[,] _)
    {
        if (_layer > 0)
        {
            Show();
        }
    }

    private void OnPressed()
    {
        _layer = (_layer + 1) % (_rules.Strata.MaxDepth + 1);
        Show();
    }

    private void Show()
    {
        var surface = _layer == 0;
        _actors.Visible = surface;
        _crowd.Visible = surface;
        _underground.Visible = !surface;
        _button.Text = surface
            ? Tr("BUTTON_LAYER_SURFACE")
            : string.Format(CultureInfo.CurrentCulture, Tr("BUTTON_LAYER_UNDERGROUND"), _layer);

        foreach (var sprite in _sprites)
        {
            sprite.QueueFree();
        }
        _sprites.Clear();
        if (surface)
        {
            return;
        }
        _ground.Color = LayerColors[Mathf.Min(_layer, LayerColors.Length) - 1];
        foreach (var relic in (_timeControl.Civilization?.Relics ?? []).Where(relic => relic.Depth == _layer))
        {
            var texture = _textures[relic.Type];
            var sprite = new Sprite2D
            {
                Name = $"Relic{relic.Id}",
                Texture = texture,
                Centered = false,
                Offset = new Vector2(-texture.GetWidth() / 2, -texture.GetHeight()),
                Position = BuildingView.FootPosition(_terrainOrigin, relic.Column, relic.Row),
                Modulate = RelicModulate,
            };
            _underground.AddChild(sprite);
            _sprites.Add(sprite);
        }
    }

    private string Tr(string key) => _button.Tr(key);
}
