using System;

namespace TheogonyX.Core;

/// <summary>
/// 地表の列と地域の対応（docs/domain/emergent-progression.md）。地域は、左（西）から決まった数の列ずつ区切ったもの。
/// 列と地域の番号は 0 から数える。
/// </summary>
public sealed record RegionMap
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
    public int Count => Columns / ColumnsPerRegion;

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
