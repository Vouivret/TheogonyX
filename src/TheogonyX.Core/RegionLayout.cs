namespace TheogonyX.Core;

/// <summary>
/// 地域の並びの決まり。今の小さな星（列だけで区切る <see cref="RegionMap"/>）と、新しい星（<see cref="PlanetRegionLayout"/>。16 × 6 の格子）で、
/// 地域の数・隣り合い・距離・タイルの範囲・建てられるタイルを同じ呼び方で扱う（plan/feat-planet-grid-1b.md の段 1b-2）。
/// </summary>
public abstract record RegionLayout
{
    /// <summary>地域の数。</summary>
    public abstract int Count { get; }

    /// <summary>隣の地域（決まった順。<see cref="RegionMap"/> は西、東の順）。</summary>
    public abstract IReadOnlyList<int> Neighbors(int region);

    /// <summary>地域どうしの距離。</summary>
    public abstract int Distance(int a, int b);

    /// <summary>東か西へ 1 つ進んだ地域。進めなければ（端の外）null。</summary>
    public abstract int? Step(int region, TravelDirection direction);

    /// <summary>地域のタイルの範囲（左上の列・行と、幅・高さ）。</summary>
    /// <param name="totalRows">地表の行の数（地域が全行にまたがる <see cref="RegionMap"/> のとき、高さになる）。</param>
    public abstract (int Column, int Row, int Width, int Height) TileRect(int region, int totalRows);

    /// <summary>タイルのある地域。</summary>
    public abstract int RegionAt(int column, int row);

    /// <summary>建物を建てられるタイルか。</summary>
    public abstract bool IsBuildable(TileType tileType);
}
