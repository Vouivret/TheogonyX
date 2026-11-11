namespace TheogonyX.Core;

/// <summary>
/// 地域に分けた星（docs/domain/regional-environment.md）。土台の地形・タイルの標高と海陸・地域ごとの 5 つの値を持つ。
/// 今のゲームの <see cref="WorldState"/> とは別に並べる（plan/feat-planet-grid-1a.md）。
/// </summary>
public sealed class RegionalWorld
{
    private readonly int[] _elevation;
    private readonly bool[] _land;

    internal RegionalWorld(
        PlanetGrid grid, int regionSize, int[] plates, bool[] boundary, BaseTerrain[] terrain, int[] elevation, bool[] land,
        EnvironmentValues[] values, int prophetRegion)
    {
        Grid = grid;
        RegionSize = regionSize;
        Plates = plates;
        IsBoundary = boundary;
        Terrain = terrain;
        _elevation = elevation;
        _land = land;
        Values = values;
        ProphetRegion = prophetRegion;
    }

    public PlanetGrid Grid { get; }

    public int RegionSize { get; }

    /// <summary>地表の幅（タイル）。</summary>
    public int Width => Grid.Columns * RegionSize;

    /// <summary>地表の高さ（タイル）。</summary>
    public int Height => Grid.Bands * RegionSize;

    /// <summary>地域ごとのプレートの番号。</summary>
    public IReadOnlyList<int> Plates { get; }

    /// <summary>地域ごとに、隣にほかのプレートがあるか（プレートの境目）。</summary>
    public IReadOnlyList<bool> IsBoundary { get; }

    /// <summary>地域ごとの土台の地形。</summary>
    public IReadOnlyList<BaseTerrain> Terrain { get; }

    /// <summary>地域ごとの 5 つの値。</summary>
    public EnvironmentValues[] Values { get; }

    public int ProphetRegion { get; }

    public int Year { get; set; }

    public bool IsDestroyed { get; set; }

    /// <summary>タイルの標高（m）。海は負。</summary>
    public int ElevationAt(int x, int y) => _elevation[y * Width + x];

    /// <summary>タイルが陸か。</summary>
    public bool IsLandAt(int x, int y) => _land[y * Width + x];

    /// <summary>タイルのある地域の番号。</summary>
    public int RegionAt(int x, int y) => Grid.Index(x / RegionSize, y / RegionSize);

    /// <summary>全地域（海を含む）の平均。</summary>
    public EnvironmentValues Average()
    {
        double forest = 0, water = 0, temperature = 0, chaos = 0, harmony = 0;
        foreach (var value in Values)
        {
            forest += value.ForestAmount;
            water += value.WaterAmount;
            temperature += value.Temperature;
            chaos += value.ChaosLevel;
            harmony += value.HarmonyLevel;
        }
        var count = Values.Length;
        return new EnvironmentValues((float)(forest / count), (float)(water / count), (float)(temperature / count), (float)(chaos / count), (float)(harmony / count));
    }
}
