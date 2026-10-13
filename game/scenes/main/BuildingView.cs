using System.Collections.Generic;
using Godot;
using TheogonyX.Core;
using TheogonyX.Data;

namespace TheogonyX.Scenes.Main;

/// <summary>
/// 地域に建った建物（docs/domain/emergent-progression.md）を、32 × 32 px の絵で地表の上に描く。
/// 予言者・焚き火と同じ Y ソートのレイヤーに置き、原点を足元（2 × 2 マスの下の辺の中央）にする。
/// 予言者と同じく、法線マップの陰影と足元の落ち影を付ける（docs/domain/visual-effects.md）。
/// </summary>
public sealed class BuildingView
{
    private static readonly Dictionary<BuildingType, string> TexturePaths = new()
    {
        [BuildingType.Workshop] = "res://assets/sprites/building_workshop.png",
        [BuildingType.Shrine] = "res://assets/sprites/building_shrine.png",
    };

    private readonly TimeControl _timeControl;
    private readonly LightingSettings _lightingSettings;
    private readonly Node _actors;
    private readonly Vector2 _terrainOrigin;
    private readonly Dictionary<BuildingType, Texture2D> _textures = [];
    private readonly List<Sprite2D> _sprites = [];

    // 描いている建物の最後の ID（建物は増えるだけなので、数と最後の ID が同じなら描き直さない）
    private int _shownLastId = -1;

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
        Refresh();
    }

    /// <summary>地表を描き直した後に呼ぶ（年が進んだ・リセットした・読み込んだ）。</summary>
    public void Refresh(TileType[,] _) => Refresh();

    private void Refresh()
    {
        var buildings = _timeControl.Civilization?.Buildings ?? [];
        var lastId = buildings.Count == 0 ? -1 : buildings[^1].Id;
        if (buildings.Count == _sprites.Count && lastId == _shownLastId)
        {
            return;
        }

        foreach (var sprite in _sprites)
        {
            sprite.QueueFree();
        }
        _sprites.Clear();
        var size = WorldScreen.TileSize;
        foreach (var building in buildings)
        {
            var texture = _textures[building.Type];
            var sprite = new Sprite2D
            {
                Name = $"Building{building.Id}",
                Texture = texture,
                Centered = false,
                Offset = new Vector2(-texture.GetWidth() / 2, -texture.GetHeight()),
                Position = _terrainOrigin + new Vector2((building.Column + 1) * size, (building.Row + 2) * size),
                LightMask = 1 | SpriteLighting.LitMask,
            };
            SpriteLighting.AddShadow(sprite, texture.GetWidth(), Vector2.Zero, _lightingSettings);
            _actors.AddChild(sprite);
            _sprites.Add(sprite);
        }
        _shownLastId = lastId;
    }
}
