using TheogonyX.Core;

namespace TheogonyX.Core.Tests;

public class TerrainClassifierTests
{
    private static TerrainLayout Classify(float temperature, float forestAmount, float waterAmount) =>
        TerrainClassifier.Classify(
            new WorldState { Temperature = temperature, ForestAmount = forestAmount, WaterAmount = waterAmount },
            TestRules.Terrain);

    [Theory]
    [Trait("TS", "TS-planet-display-001")]
    [InlineData(-5.0f, 80.0f, 100.0f, TileType.Glacier)] // 氷河が密林より優先
    [InlineData(50.0f, 50.0f, 10.0f, TileType.Scorched)] // 焦土が砂漠より優先
    [InlineData(15.0f, 80.0f, 20.0f, TileType.Desert)] // 砂漠が密林より優先
    public void Classify_複数の条件に当てはまる_優先度の高い種別になる(float temperature, float forest, float water, TileType expected)
    {
        Assert.Equal(expected, Classify(temperature, forest, water).TileType);
    }

    [Theory]
    [Trait("TS", "TS-planet-display-002")]
    [InlineData(0.0f, 50.0f, 100.0f, TileType.Glacier)]
    [InlineData(15.0f, 70.0f, 100.0f, TileType.Jungle)]
    public void Classify_閾値ちょうど_条件に含まれる(float temperature, float forest, float water, TileType expected)
    {
        Assert.Equal(expected, Classify(temperature, forest, water).TileType);
    }

    [Theory]
    [Trait("TS", "TS-planet-display-003")]
    [InlineData(15.0f, 69.9f, 20.1f)]
    [InlineData(15.0f, 50.0f, 100.0f)]
    public void Classify_どの条件にも当てはまらない_すべて草地(float temperature, float forest, float water)
    {
        Assert.Equal(new TerrainLayout(TileType.Grassland, 0), Classify(temperature, forest, water));
    }

    [Theory]
    [Trait("TS", "TS-planet-display-004")]
    [InlineData(50.0f, 20.0f, 13)]
    [InlineData(0.0f, 0.0f, 16)]
    public void Classify_水資源が少ない_乾燥度に応じた列が砂漠になる(float forest, float water, int expectedColumns)
    {
        Assert.Equal(new TerrainLayout(TileType.Desert, expectedColumns), Classify(15.0f, forest, water));
    }

    [Fact]
    [Trait("TS", "TS-planet-display-005")]
    public void Classify_列数がちょうど14点5_切り上げて15列()
    {
        Assert.Equal(new TerrainLayout(TileType.Desert, 15), Classify(15.0f, 50.0f, 9.375f));
    }

    [Theory]
    [Trait("TS", "TS-planet-display-006")]
    [InlineData(0.0f, 50.0f, TileType.Glacier)]
    [InlineData(50.0f, 50.0f, TileType.Scorched)]
    [InlineData(15.0f, 70.0f, TileType.Jungle)]
    public void Classify_計算上0列_最小の1列になる(float temperature, float forest, TileType expected)
    {
        Assert.Equal(new TerrainLayout(expected, 1), Classify(temperature, forest, 100.0f));
    }

    [Theory]
    [Trait("TS", "TS-planet-display-007")]
    [InlineData(-25.0f, 8)]
    [InlineData(-50.0f, 16)]
    public void Classify_気温が0度以下_寒さに応じた列が氷河になる(float temperature, int expectedColumns)
    {
        Assert.Equal(new TerrainLayout(TileType.Glacier, expectedColumns), Classify(temperature, 50.0f, 100.0f));
    }

    [Theory]
    [Trait("TS", "TS-planet-display-008")]
    [InlineData(65.0f, 8)]
    [InlineData(80.0f, 16)]
    public void Classify_気温が50度以上_暑さに応じた列が焦土になる(float temperature, int expectedColumns)
    {
        Assert.Equal(new TerrainLayout(TileType.Scorched, expectedColumns), Classify(temperature, 50.0f, 100.0f));
    }

    [Theory]
    [Trait("TS", "TS-planet-display-009")]
    [InlineData(100.0f, 16)] // 世界の初期値
    [InlineData(85.0f, 8)]
    public void Classify_森林量が70以上_森林量に応じた列が密林になる(float forest, int expectedColumns)
    {
        Assert.Equal(new TerrainLayout(TileType.Jungle, expectedColumns), Classify(15.0f, forest, 100.0f));
    }
}
