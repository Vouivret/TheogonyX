using System;
using Godot;
using TheogonyX.Core;

namespace TheogonyX.Scenes.Main;

/// <summary>
/// 新しい星（192 × 72 タイル。<see cref="RegionalWorld"/>）の地表を、今のゲームの地表（32 × 18）の下に描く（plan/feat-planet-grid-1b.md の段 1b-1）。
/// 東西はつながるので、同じ絵を左右にも 1 枚ずつ並べる。今のゲームの地表は、予言者の最初の地域のあたりに重ねる。
/// 環境の値は動かさず、起動時の値で決まる異常のタイル（氷河・焦土・砂漠・密林）だけを地域ごとに塗る。
/// </summary>
public sealed class PlanetView
{
    /// <param name="owner">ノードを探すためのシーンの根。</param>
    /// <param name="world">描く星。</param>
    /// <param name="terrainRules">異常のタイルの条件（今のゲームの地表と同じもの）。</param>
    /// <param name="gameTerrain">今のゲームの地表。星の絵は、これより奥に置く。</param>
    public PlanetView(RegionalWorld world, TerrainRules terrainRules, TileMapLayer gameTerrain)
    {
        var tileSet = WorldScreen.CreateTileSet();
        var tiles = ClassifyTiles(world, terrainRules);

        // 今のゲームの地表（左上が gameTerrain.Position）の中心が、予言者の最初の地域の中心に来るように星をずらす。
        // 端の外に出ないよう、中心は上下・東西の端から地表の半分以内には寄せない
        var size = WorldScreen.TileSize;
        var gameRect = gameTerrain.GetUsedRect();
        var halfColumns = gameRect.Size.X / 2;
        var halfRows = gameRect.Size.Y / 2;
        var region = world.ProphetRegion;
        var centerColumn = Math.Clamp(world.Grid.ColumnOf(region) * world.RegionSize + world.RegionSize / 2, halfColumns, world.Width - halfColumns);
        var centerRow = Math.Clamp(world.Grid.BandOf(region) * world.RegionSize + world.RegionSize / 2, halfRows, world.Height - halfRows);
        var gameCenter = gameTerrain.Position + ((Vector2)gameRect.Position + (Vector2)gameRect.Size / 2) * size;
        var origin = gameCenter - new Vector2(centerColumn, centerRow) * size;

        Bounds = new Rect2(origin, new Vector2(world.Width, world.Height) * size);
        var parent = gameTerrain.GetParent();
        // 左・中・右の 3 枚。今のゲームの地表より奥（子の並びの先頭）へ置く
        for (var copy = -1; copy <= 1; copy++)
        {
            var layer = new TileMapLayer
            {
                Name = $"Planet{copy + 1}",
                TileSet = tileSet,
                TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
                Position = origin + new Vector2(copy * world.Width * size, 0),
            };
            parent.AddChild(layer);
            parent.MoveChild(layer, copy + 1);
            Draw(layer, tiles);
        }
    }

    /// <summary>星の地表の範囲（世界の px。真ん中の 1 枚）。</summary>
    public Rect2 Bounds { get; }

    /// <summary>東西の 1 周の長さ（世界の px）。</summary>
    public float Circumference => Bounds.Size.X;

    private static void Draw(TileMapLayer layer, TileType[,] tiles)
    {
        for (var x = 0; x < tiles.GetLength(0); x++)
        {
            for (var y = 0; y < tiles.GetLength(1); y++)
            {
                layer.SetCell(new Vector2I(x, y), 0, new Vector2I(((int)tiles[x, y] * TerrainArt.Variants) + TerrainArt.VariantAt(x, y), 0));
            }
        }
    }

    // 海のタイルは水、陸は土台の地形（平地・森・山）。地域の値が異常の条件に当たる陸は、その地域ぜんぶを異常のタイルにする
    private static TileType[,] ClassifyTiles(RegionalWorld world, TerrainRules rules)
    {
        var tiles = new TileType[world.Width, world.Height];
        var regionAnomaly = new TileType?[world.Grid.Count];
        for (var region = 0; region < regionAnomaly.Length; region++)
        {
            regionAnomaly[region] = AnomalyOf(world.Values[region], rules);
        }
        for (var x = 0; x < world.Width; x++)
        {
            for (var y = 0; y < world.Height; y++)
            {
                if (!world.IsLandAt(x, y))
                {
                    tiles[x, y] = TileType.Water;
                    continue;
                }
                var region = world.RegionAt(x, y);
                tiles[x, y] = regionAnomaly[region] ?? world.Terrain[region] switch
                {
                    BaseTerrain.Forest => TileType.Jungle,
                    BaseTerrain.Mountain => TileType.Scorched,
                    _ => TileType.Grassland,
                };
            }
        }
        return tiles;
    }

    // 優先度の順に調べ、最初に当てはまった異常で決める（今のゲームの地表と同じ条件）
    private static TileType? AnomalyOf(EnvironmentValues values, TerrainRules rules)
    {
        foreach (var anomaly in rules.Anomalies)
        {
            var value = anomaly.Metric switch
            {
                WorldMetric.ForestAmount => values.ForestAmount,
                WorldMetric.WaterAmount => values.WaterAmount,
                WorldMetric.Temperature => values.Temperature,
                WorldMetric.ChaosLevel => values.ChaosLevel,
                WorldMetric.HarmonyLevel => values.HarmonyLevel,
                _ => throw new ArgumentOutOfRangeException(nameof(rules), anomaly.Metric, null),
            };
            if (anomaly.Comparison == ThresholdComparison.AtOrBelow ? value <= anomaly.Threshold : value >= anomaly.Threshold)
            {
                return anomaly.TileType;
            }
        }
        return null;
    }
}
