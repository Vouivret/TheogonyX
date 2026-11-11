namespace TheogonyX.Core;

/// <summary>新しい星の地域の並び（<see cref="PlanetGrid"/> の格子。1 つの地域は <paramref name="RegionSize"/> × <paramref name="RegionSize"/> タイル）。</summary>
public sealed record PlanetRegionLayout(PlanetGrid Grid, int RegionSize) : RegionLayout
{
    public override int Count => Grid.Count;

    public override IReadOnlyList<int> Neighbors(int region) => Grid.Neighbors(region);

    public override int Distance(int a, int b) => Grid.Distance(a, b);

    // 横は輪なので、東と西へはいつも進める
    public override int? Step(int region, TravelDirection direction) =>
        direction == TravelDirection.None
            ? null
            : Grid.TryNeighbor(region, direction == TravelDirection.East ? 1 : -1, 0, out var next) ? next : null;

    public override (int Column, int Row, int Width, int Height) TileRect(int region, int totalRows) =>
        (Grid.ColumnOf(region) * RegionSize, Grid.BandOf(region) * RegionSize, RegionSize, RegionSize);

    public override int RegionAt(int column, int row) => Grid.Index(column / RegionSize, row / RegionSize);

    // TODO(AI_UNSURE): 今の地表は草地にしか建てない。新しい星では森の地域（密林のタイル）にも建てられるようにした（そうしないと森の地域に家が建たない）。山・氷河・砂漠・焦土には建てない。
    public override bool IsBuildable(TileType tileType) => tileType is TileType.Grassland or TileType.Jungle;
}
