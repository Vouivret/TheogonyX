using Godot;
using TheogonyX.Core;

namespace TheogonyX.Scenes.Main;

/// <summary>
/// 新しい星（192 × 72 タイル）の東西のつなぎ目の絵（plan/feat-planet-grid-1b.md）。地表本体（<c>%Terrain</c>）の左と右に、同じ絵をもう 1 枚ずつ並べて、
/// 東西に回し続けたときに絵が途切れないようにする。建物・住人は本体の 1 枚にだけ置く。
/// </summary>
public sealed class PlanetView
{
    private readonly TileMapLayer _west;
    private readonly TileMapLayer _east;
    private TileType[,]? _drawn;

    /// <param name="terrain">地表の本体。左右の絵は、これと同じタイルセットで、この両隣に置く。</param>
    /// <param name="tiles">最初のタイルの種別。星の大きさ（列 × 行）を決める。</param>
    public PlanetView(TileMapLayer terrain, TileType[,] tiles)
    {
        var size = WorldScreen.TileSize;
        var width = tiles.GetLength(0) * size;
        Bounds = new Rect2(terrain.Position, new Vector2(width, tiles.GetLength(1) * size));
        _west = AddCopy(terrain, "PlanetWest", new Vector2(-width, 0));
        _east = AddCopy(terrain, "PlanetEast", new Vector2(width, 0));
        Redraw(tiles);
    }

    /// <summary>星の地表の範囲（世界の px。本体の 1 枚）。</summary>
    public Rect2 Bounds { get; }

    /// <summary>東西の 1 周の長さ（世界の px）。</summary>
    public float Circumference => Bounds.Size.X;

    /// <summary>左右の絵を描き直す（前に描いたものと違うマスだけ）。</summary>
    public void Redraw(TileType[,] tiles)
    {
        for (var x = 0; x < tiles.GetLength(0); x++)
        {
            for (var y = 0; y < tiles.GetLength(1); y++)
            {
                if (_drawn is not null && _drawn[x, y] == tiles[x, y])
                {
                    continue;
                }
                var cell = new Vector2I(x, y);
                var atlas = new Vector2I(((int)tiles[x, y] * TerrainArt.Variants) + TerrainArt.VariantAt(x, y), 0);
                _west.SetCell(cell, 0, atlas);
                _east.SetCell(cell, 0, atlas);
            }
        }
        _drawn = tiles;
    }

    private static TileMapLayer AddCopy(TileMapLayer terrain, string name, Vector2 offset)
    {
        var layer = new TileMapLayer
        {
            Name = name,
            TileSet = terrain.TileSet,
            TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
            Position = terrain.Position + offset,
        };
        var parent = terrain.GetParent();
        parent.AddChild(layer);
        parent.MoveChild(layer, terrain.GetIndex());
        return layer;
    }
}
