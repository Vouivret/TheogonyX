using System;

namespace TheogonyX.Core;

/// <summary>
/// 地表の列と地域の対応（docs/domain/emergent-progression.md）。地域は、左（西）から決まった数の列ずつ区切ったもの。
/// 列と地域の番号は 0 から数える。
/// </summary>
public sealed record RegionMap : RegionLayout
{
    /// <param name="columns">地表の列の数。</param>
    /// <param name="columnsPerRegion">1 つの地域の列の数。<paramref name="columns"/> を割り切れること。</param>
    public RegionMap(int columns, int columnsPerRegion)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(columns);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(columnsPerRegion);
        if (columns % columnsPerRegion != 0)
        {
            throw new ArgumentException($"列の数が地域の列の数で割り切れない: columns={columns} columnsPerRegion={columnsPerRegion}", nameof(columnsPerRegion));
        }
        Columns = columns;
        ColumnsPerRegion = columnsPerRegion;
    }

    public int Columns { get; }
    public int ColumnsPerRegion { get; }

    /// <summary>地域の数。</summary>
    public override int Count => Columns / ColumnsPerRegion;

    public override IReadOnlyList<int> Neighbors(int region) =>
        new[] { region - 1, region + 1 }.Where(other => other >= 0 && other < Count).ToList();

    public override int Distance(int a, int b) => Math.Abs(a - b);

    // 東西に 1 列に並ぶので、動けるのは東（番号が大きい）と西だけ。端でつながらない
    public override int? Step(int region, TravelDirection direction)
    {
        var next = region + (direction == TravelDirection.East ? 1 : direction == TravelDirection.West ? -1 : 0);
        return direction != TravelDirection.None && next >= 0 && next < Count ? next : null;
    }

    public override (int Column, int Row, int Width, int Height) TileRect(int region, int totalRows) =>
        (FirstColumn(region), 0, ColumnsPerRegion, totalRows);

    public override int RegionAt(int column, int row) => RegionOf(column);

    public override bool IsBuildable(TileType tileType) => tileType == TileType.Grassland;

    /// <summary>列が入っている地域。</summary>
    public int RegionOf(int column)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(column);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(column, Columns);
        return column / ColumnsPerRegion;
    }

    /// <summary>地域のいちばん左（西）の列。地域の列は、ここから <see cref="ColumnsPerRegion"/> 列。</summary>
    public int FirstColumn(int region)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(region);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(region, Count);
        return region * ColumnsPerRegion;
    }
}
