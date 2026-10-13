using System.Collections.Generic;
using System.Linq;
using Godot;
using TheogonyX.Core;
using TheogonyX.Data;

namespace TheogonyX.Scenes.Main;

/// <summary>
/// 地域に建った建物（docs/domain/emergent-progression.md）・住人が建てた家（docs/domain/agent-needs.md）と地上の廃墟（docs/domain/strata.md）を、
/// 32 × 32 px の絵で地表の上に描く。廃墟の上に建て増しした家だけは、石の土台の上の家（32 × 48 px）で描く。
/// 予言者・焚き火と同じ Y ソートのレイヤーに置き、原点を足元（2 × 2 マスの下の辺の中央）にする。
/// 予言者と同じく、建物には法線マップの陰影と足元の落ち影を付ける（docs/domain/visual-effects.md）。
/// </summary>
public sealed class BuildingView
{
    /// <summary>建物の種類ごとの絵。地下の遺物の表示（<see cref="StrataView"/>）でも使う。</summary>
    public static readonly IReadOnlyDictionary<BuildingType, string> TexturePaths = new Dictionary<BuildingType, string>
    {
        [BuildingType.Workshop] = "res://assets/sprites/building_workshop.png",
        [BuildingType.Shrine] = "res://assets/sprites/building_shrine.png",
        [BuildingType.HouseHut] = "res://assets/sprites/building_hut.png",
        [BuildingType.HouseTimber] = "res://assets/sprites/building_house_medieval.png",
        [BuildingType.HouseBrick] = "res://assets/sprites/building_house_brick.png",
    };

    private const string RuinTexturePath = "res://assets/sprites/building_ruin.png";
    private const string OverbuiltHouseTexturePath = "res://assets/sprites/building_overbuild.png";

    private readonly TimeControl _timeControl;
    private readonly LightingSettings _lightingSettings;
    private readonly Node _actors;
    private readonly Vector2 _terrainOrigin;
    private readonly Dictionary<BuildingType, Texture2D> _textures = [];
    private readonly Texture2D _ruinTexture;
    private readonly Texture2D _overbuiltHouseTexture;
    private readonly List<Sprite2D> _sprites = [];

    // 描いている廃墟と建物の ID の並び（変わっていなければ描き直さない）
    private int[] _shownIds = [];

    /// <param name="owner">ノードを探すためのシーンの根。</param>
    public BuildingView(Node owner, TimeControl timeControl, LightingSettings lightingSettings)
    {
        _timeControl = timeControl;
        _lightingSettings = lightingSettings;
        _actors = owner.GetNode<Sprite2D>("%Prophet").GetParent();
        _terrainOrigin = owner.GetNode<TileMapLayer>("%Terrain").Position;
        foreach (var (type, path) in TexturePaths)
        {
            var diffuse = GD.Load<Texture2D>(path);
            _textures[type] = SpriteLighting.WithNormalMap(diffuse, path, diffuse.GetWidth());
        }
        var ruin = GD.Load<Texture2D>(RuinTexturePath);
        _ruinTexture = SpriteLighting.WithNormalMap(ruin, RuinTexturePath, ruin.GetWidth());
        var overbuilt = GD.Load<Texture2D>(OverbuiltHouseTexturePath);
        _overbuiltHouseTexture = SpriteLighting.WithNormalMap(overbuilt, OverbuiltHouseTexturePath, overbuilt.GetWidth());
        Refresh();
    }

    /// <summary>建物の足元の位置（2 × 2 マスの下の辺の中央）。</summary>
    public static Vector2 FootPosition(Vector2 terrainOrigin, int column, int row) =>
        terrainOrigin + new Vector2((column + 1) * WorldScreen.TileSize, (row + 2) * WorldScreen.TileSize);

    /// <summary>地表を描き直した後に呼ぶ（年が進んだ・リセットした・読み込んだ）。</summary>
    public void Refresh(TileType[,] _) => Refresh();

    private void Refresh()
    {
        var civilization = _timeControl.Civilization;
        var ruins = civilization?.Ruins ?? [];
        var buildings = civilization?.Buildings ?? [];
        // 廃墟は ID を負にして、建物の ID と区別する
        int[] ids = [.. ruins.Select(ruin => -1 - ruin.Id), .. buildings.Select(building => building.Id)];
        if (ids.SequenceEqual(_shownIds))
        {
            return;
        }

        foreach (var sprite in _sprites)
        {
            sprite.QueueFree();
        }
        _sprites.Clear();
        // 建て増しの枠では、廃墟と建物の足元が同じ高さになる。Y ソートで同じ高さなら先に足したものが後ろになるので、廃墟を先に足す
        foreach (var ruin in ruins)
        {
            AddSprite($"Ruin{ruin.Id}", _ruinTexture, ruin.Column, ruin.Row, shadow: false);
        }
        foreach (var building in buildings)
        {
            var overbuilt = building.Type.IsHouse() && ruins.Any(ruin => ruin.Column == building.Column && ruin.Row == building.Row);
            AddSprite($"Building{building.Id}", overbuilt ? _overbuiltHouseTexture : _textures[building.Type], building.Column, building.Row, shadow: true);
        }
        _shownIds = ids;
    }

    private void AddSprite(string name, Texture2D texture, int column, int row, bool shadow)
    {
        var sprite = new Sprite2D
        {
            Name = name,
            Texture = texture,
            Centered = false,
            Offset = new Vector2(-texture.GetWidth() / 2, -texture.GetHeight()),
            Position = FootPosition(_terrainOrigin, column, row),
            LightMask = 1 | SpriteLighting.LitMask,
        };
        if (shadow)
        {
            SpriteLighting.AddShadow(sprite, texture.GetWidth(), Vector2.Zero, _lightingSettings);
        }
        _actors.AddChild(sprite);
        _sprites.Add(sprite);
    }
}
