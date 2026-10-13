using TheogonyX.Core;

namespace TheogonyX.Core.Tests;

public class WorldSimulatorTests
{
    private const float Tolerance = 0.001f;

    private static WorldState Skip(int years, params Dogma[] dogmas)
    {
        var world = WorldState.CreateInitial(TestRules.World);
        WorldSimulator.SimulateTimeSkip(world, dogmas, years, TestRules.World);
        return world;
    }

    [Fact]
    [Trait("TS", "TS-time-skip-001")]
    public void SimulateTimeSkip_木の禁忌で10年_森林量が上限で止まる()
    {
        var world = Skip(10, new Dogma("SYM_TREE", DogmaType.Taboo, 1.0f));

        Assert.Equal(10, world.Year);
        Assert.Equal(100.0f, world.ForestAmount, Tolerance);
        Assert.Equal(95.0f, world.WaterAmount, Tolerance);
        Assert.False(world.IsDestroyed);
    }

    [Fact]
    [Trait("TS", "TS-time-skip-002")]
    public void SimulateTimeSkip_雨の禁忌が強度0点5で100年_変化量が半分になる()
    {
        var world = Skip(100, new Dogma("SYM_RAIN", DogmaType.Taboo, 0.5f));

        Assert.Equal(100, world.Year);
        Assert.Equal(50.0f, world.WaterAmount, Tolerance);
        Assert.Equal(20.0f, world.Temperature, Tolerance);
        Assert.False(world.IsDestroyed);
    }

    [Fact]
    [Trait("TS", "TS-time-skip-003")]
    public void SimulateTimeSkip_木の破壊で100年_森林量が下限で止まり水資源があるので崩壊しない()
    {
        var world = Skip(100, new Dogma("SYM_TREE", DogmaType.Destruction, 1.0f));

        Assert.Equal(100, world.Year);
        Assert.Equal(0.0f, world.ForestAmount, Tolerance);
        Assert.Equal(80.0f, world.ChaosLevel, Tolerance);
        Assert.False(world.IsDestroyed);
    }

    [Fact]
    [Trait("TS", "TS-time-skip-004")]
    public void SimulateTimeSkip_血の破壊で100年_カオス度で年67に崩壊して中断する()
    {
        var world = Skip(100, new Dogma("SYM_BLOOD", DogmaType.Destruction, 1.0f));

        Assert.Equal(67, world.Year);
        Assert.True(world.IsDestroyed);
        Assert.Equal(100.0f, world.ChaosLevel, Tolerance);
        Assert.Equal(0.0f, world.HarmonyLevel, Tolerance);
    }

    [Fact]
    [Trait("TS", "TS-time-skip-005")]
    public void SimulateTimeSkip_火の禁忌で300年_気温で崩壊する()
    {
        var world = Skip(300, new Dogma("SYM_FIRE", DogmaType.Taboo, 1.0f));

        Assert.True(world.IsDestroyed);
        Assert.True(world.Temperature <= -40.0f);
        Assert.True(world.Year < 300);
    }

    [Fact]
    [Trait("TS", "TS-time-skip-006")]
    public void SimulateTimeSkip_表に無い火の崇拝で100年_環境は初期値のまま()
    {
        var world = Skip(100, new Dogma("SYM_FIRE", DogmaType.Worship, 1.0f));

        Assert.Equal(100, world.Year);
        Assert.Equal(100.0f, world.ForestAmount, Tolerance);
        Assert.Equal(100.0f, world.WaterAmount, Tolerance);
        Assert.Equal(15.0f, world.Temperature, Tolerance);
        Assert.Equal(0.0f, world.ChaosLevel, Tolerance);
        Assert.Equal(50.0f, world.HarmonyLevel, Tolerance);
        Assert.False(world.IsDestroyed);
    }

    [Fact]
    [Trait("TS", "TS-time-skip-007")]
    public void SimulateTimeSkip_木の破壊と雨の破壊で10年_変化量が合算される()
    {
        var world = Skip(10, new Dogma("SYM_TREE", DogmaType.Destruction, 1.0f), new Dogma("SYM_RAIN", DogmaType.Destruction, 1.0f));

        Assert.Equal(10, world.Year);
        Assert.Equal(80.0f, world.ForestAmount, Tolerance);
        Assert.Equal(85.0f, world.WaterAmount, Tolerance);
        Assert.Equal(13.0f, world.ChaosLevel, Tolerance);
    }

    [Fact]
    [Trait("TS", "TS-time-skip-008")]
    public void SimulateTimeSkip_木の破壊と雨の破壊で100年_不毛で年67に崩壊する()
    {
        var world = Skip(100, new Dogma("SYM_TREE", DogmaType.Destruction, 1.0f), new Dogma("SYM_RAIN", DogmaType.Destruction, 1.0f));

        Assert.Equal(67, world.Year);
        Assert.True(world.IsDestroyed);
        Assert.Equal(0.0f, world.WaterAmount, Tolerance);
        Assert.Equal(0.0f, world.ForestAmount, Tolerance);
        Assert.Equal(87.1f, world.ChaosLevel, Tolerance);
    }

    [Fact]
    [Trait("TS", "TS-time-skip-009")]
    public void SimulateTimeSkip_崩壊済みの世界をさらに100年_何も変わらない()
    {
        var dogmas = new[] { new Dogma("SYM_BLOOD", DogmaType.Destruction, 1.0f) };
        var world = Skip(100, dogmas);

        WorldSimulator.SimulateTimeSkip(world, dogmas, 100, TestRules.World);

        Assert.Equal(67, world.Year);
        Assert.True(world.IsDestroyed);
        Assert.Equal(100.0f, world.ForestAmount, Tolerance);
        Assert.Equal(100.0f, world.WaterAmount, Tolerance);
        Assert.Equal(15.0f, world.Temperature, Tolerance);
        Assert.Equal(100.0f, world.ChaosLevel, Tolerance);
        Assert.Equal(0.0f, world.HarmonyLevel, Tolerance);
    }

    [Fact]
    [Trait("TS", "TS-time-skip-010")]
    public void SimulateTimeSkip_50年を2回_ドグマが跳躍をまたいで効き続ける()
    {
        var dogmas = new[] { new Dogma("SYM_RAIN", DogmaType.Taboo, 0.5f) };
        var world = Skip(50, dogmas);

        WorldSimulator.SimulateTimeSkip(world, dogmas, 50, TestRules.World);

        Assert.Equal(100, world.Year);
        Assert.Equal(50.0f, world.WaterAmount, Tolerance);
        Assert.Equal(20.0f, world.Temperature, Tolerance);
    }

    // 自然なカオス度の減少（0.2）を入れた定義。ほかのテストは、減少を 0 とした定義（TestRules.World）で計算している
    private static readonly WorldRules DecayingRules = TestRules.World with { ChaosDecayPerYear = 0.2f };

    private static WorldState SkipWithDecay(WorldState world, int years, params Dogma[] dogmas)
    {
        WorldSimulator.SimulateTimeSkip(world, dogmas, years, DecayingRules);
        return world;
    }

    [Fact]
    [Trait("TS", "TS-time-skip-013")]
    public void SimulateTimeSkip_カオス度10でドグマなしの10年_カオス度が毎年0点2ずつ下がり8点0になる()
    {
        var world = WorldState.CreateInitial(TestRules.World);
        world.ChaosLevel = 10f;

        SkipWithDecay(world, 10);

        Assert.Equal(10, world.Year);
        Assert.Equal(8.0f, world.ChaosLevel, Tolerance);
    }

    [Fact]
    [Trait("TS", "TS-time-skip-014")]
    public void SimulateTimeSkip_カオス度0点1で1年_下限の0を下回らない()
    {
        var world = WorldState.CreateInitial(TestRules.World);
        world.ChaosLevel = 0.1f;

        SkipWithDecay(world, 1);

        Assert.Equal(0.0f, world.ChaosLevel, Tolerance);
    }

    [Fact]
    [Trait("TS", "TS-time-skip-015")]
    public void SimulateTimeSkip_木の破壊で10年_ドグマの上昇と自然な減少が合算され6点0になる()
    {
        var world = WorldState.CreateInitial(TestRules.World);

        SkipWithDecay(world, 10, new Dogma("SYM_TREE", DogmaType.Destruction, 1.0f));

        Assert.Equal(10, world.Year);
        Assert.Equal(6.0f, world.ChaosLevel, Tolerance);
    }

    [Fact]
    [Trait("TS", "TS-time-skip-011")]
    public void SimulateTimeSkip_同じ入力で2回実行_すべての値が一致する()
    {
        var dogmas = new[] { new Dogma("SYM_TREE", DogmaType.Destruction, 1.0f), new Dogma("SYM_RAIN", DogmaType.Destruction, 1.0f) };

        var first = Skip(100, dogmas);
        var second = Skip(100, dogmas);

        Assert.Equivalent(first, second, strict: true);
    }
}
