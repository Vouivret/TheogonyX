using TheogonyX.Core;

namespace TheogonyX.Core.Tests;

public class RegionMapTests
{
    private static readonly RegionMap Map = new(columns: 32, columnsPerRegion: 2);

    [Fact]
    [Trait("TS", "TS-emergent-progression-001")]
    public void Count_32列を2列ずつ_16地域()
    {
        Assert.Equal(16, Map.Count);
    }

    [Theory]
    [Trait("TS", "TS-emergent-progression-001")]
    [InlineData(0, 0)]
    [InlineData(1, 0)]
    [InlineData(2, 1)]
    [InlineData(15, 7)]
    [InlineData(31, 15)]
    public void RegionOf_列_2列ごとに次の地域になる(int column, int expected)
    {
        Assert.Equal(expected, Map.RegionOf(column));
    }

    [Theory]
    [Trait("TS", "TS-emergent-progression-001")]
    [InlineData(0, 0)]
    [InlineData(7, 14)]
    [InlineData(15, 30)]
    public void FirstColumn_地域_いちばん左の列(int region, int expected)
    {
        Assert.Equal(expected, Map.FirstColumn(region));
    }

    [Theory]
    [Trait("TS", "TS-emergent-progression-002")]
    [InlineData(-1)]
    [InlineData(32)]
    public void RegionOf_地表の外の列_エラー(int column)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Map.RegionOf(column));
    }

    [Theory]
    [Trait("TS", "TS-emergent-progression-002")]
    [InlineData(-1)]
    [InlineData(16)]
    public void FirstColumn_無い地域_エラー(int region)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Map.FirstColumn(region));
    }

    [Fact]
    [Trait("TS", "TS-emergent-progression-003")]
    public void Constructor_列の数が割り切れない_エラー()
    {
        Assert.Throws<ArgumentException>(() => new RegionMap(columns: 31, columnsPerRegion: 2));
    }
}
