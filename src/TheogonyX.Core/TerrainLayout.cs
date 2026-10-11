namespace TheogonyX.Core;

/// <summary>左から <see cref="Columns"/> 列が <see cref="TileType"/>、残りは草地。</summary>
public readonly record struct TerrainLayout(TileType TileType, int Columns);
