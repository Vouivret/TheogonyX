using TheogonyX.Core.Persistence;

namespace TheogonyX.Core.Tests;

public class SaveDataTests
{
    private const int Seed = 42;

    // docs/domain/save-data.md の形の、正しいセーブデータ
    private const string ValidSave = """
        {
          "saveVersion": 2, "seed": 42, "dreamsGenerated": 2,
          "world": { "year": 312, "forestAmount": 50, "waterAmount": 10, "temperature": 18,
                     "chaosLevel": 22, "harmonyLevel": 70, "isDestroyed": false },
          "dogmas": [{ "targetSymbolId": "SYM_TREE", "dogmaType": "Taboo", "intensity": 0.74 }],
          "dream": { "state": "TAMPERED", "nodes": [{ "symbolId": "SYM_TREE", "weight": 0.5 }, { "symbolId": "SYM_BLOOD", "weight": 0.6 }, { "symbolId": "SYM_FIRE", "weight": 0.4 }] },
          "request": "THIRST",
          "answeredYears": { "FAMINE": 250 },
          "lastOracleYear": 250,
          "autoPlay": { "speed": "REAL_TIME", "elapsedSeconds": 3600 }
        }
        """;

    private sealed record Game(TimeControl Time, ProphetDreams Prophet, DreamSource Dreams, AutoPlayClock Clock);

    private static Game CreateGame()
    {
        var time = new TimeControl(TestRules.World, TestRules.JumpYears);
        var dreams = new DreamSource(Seed, TestRules.Dream, TestRules.ProphetDreams);
        var prophet = new ProphetDreams(time, TestRules.Dream, TestRules.ProphetDreams, dreams.Next);
        var clock = new AutoPlayClock(new Dictionary<AutoPlaySpeed, double>
        {
            [AutoPlaySpeed.Normal] = 1.0,
            [AutoPlaySpeed.Fast] = 0.1,
            [AutoPlaySpeed.RealTime] = 86400.0,
        });
        return new Game(time, prophet, dreams, clock);
    }

    // 書き出して読み戻し、新しく作ったゲームに当てはめる
    private static Game RoundTrip(Game game)
    {
        var json = SaveDataSerializer.Write(SaveGame.Capture(game.Time, game.Prophet, game.Dreams, game.Clock));
        Assert.True(SaveDataSerializer.TryParse(json, TestRules.Dream, TestRules.ProphetDreams, out var data, out var reason), reason);
        var loaded = CreateGame();
        SaveGame.Apply(data!, loaded.Time, loaded.Prophet, loaded.Dreams, loaded.Clock);
        return loaded;
    }

    [Fact]
    [Trait("TS", "TS-save-data-001")]
    public void RoundTrip_依頼中で改ざん済み_同じ状態に戻る()
    {
        var game = CreateGame();
        game.Time.AddDogma(new Dogma("SYM_TREE", DogmaType.Taboo, 0.74f));
        // 年 250 で飢えの依頼に応え、年 312 で渇きの依頼を受けて夢を改ざんした
        game.Time.World.Year = 250;
        game.Time.World.ForestAmount = 10f;
        game.Prophet.OnTimeAdvanced();
        game.Prophet.Pronounce();
        game.Time.World.Year = 312;
        game.Time.World.ForestAmount = 50f;
        game.Time.World.WaterAmount = 10f;
        game.Prophet.OnTimeAdvanced();
        game.Prophet.Tamper(1, "SYM_BLOOD");
        var dreamBefore = game.Prophet.Dream.ToList();
        var dogmasBefore = game.Time.Dogmas.ToList();

        var loaded = RoundTrip(game);

        Assert.Equal(312, loaded.Time.World.Year);
        Assert.Equal(50f, loaded.Time.World.ForestAmount);
        Assert.Equal(10f, loaded.Time.World.WaterAmount);
        Assert.Equal(new Dogma("SYM_TREE", DogmaType.Taboo, 0.74f), loaded.Time.Dogmas[0]);
        Assert.Equal(dogmasBefore, loaded.Time.Dogmas);
        Assert.Equal(ProphetDreamState.Tampered, loaded.Prophet.State);
        Assert.Equal(dreamBefore, loaded.Prophet.Dream);
        Assert.Equal("SYM_BLOOD", loaded.Prophet.Dream[1].SymbolId);
        Assert.Equal("THIRST", loaded.Prophet.ActiveRequest?.Id);
        Assert.Equal(new Dictionary<string, int> { ["FAMINE"] = 250 }, loaded.Prophet.AnsweredYears);
        Assert.Equal(250, loaded.Prophet.LastOracleYear);
        Assert.Equal(2, loaded.Dreams.DreamsGenerated);
    }

    [Fact]
    [Trait("TS", "TS-save-data-001")]
    public void RoundTrip_夢なし_夢なしのまま()
    {
        var game = CreateGame();
        game.Prophet.Pronounce();

        var loaded = RoundTrip(game);

        Assert.Equal(ProphetDreamState.None, loaded.Prophet.State);
        Assert.Empty(loaded.Prophet.Dream);
        Assert.Null(loaded.Prophet.ActiveRequest);
    }

    [Fact]
    [Trait("TS", "TS-save-data-001")]
    public void RoundTrip_崩壊している_崩壊していて操作できない()
    {
        var game = CreateGame();
        game.Time.World.IsDestroyed = true;

        var loaded = RoundTrip(game);

        Assert.True(loaded.Time.World.IsDestroyed);
        Assert.Equal(TimeControlState.Destroyed, loaded.Time.State);
        Assert.False(loaded.Prophet.CanTamper);
        Assert.False(loaded.Prophet.CanPronounce);
    }

    [Fact]
    [Trait("TS", "TS-save-data-002")]
    public void RoundTrip_自動進行中_止まっている()
    {
        var game = CreateGame();
        game.Time.StartAutoPlay();

        var loaded = RoundTrip(game);

        Assert.Equal(TimeControlState.Stopped, loaded.Time.State);
    }

    [Fact]
    [Trait("TS", "TS-save-data-003")]
    public void RoundTrip_夢を2つ見た後_次の夢は保存しなかった場合の3つ目と同じ()
    {
        var reference = new DreamSource(Seed, TestRules.Dream, TestRules.ProphetDreams);
        reference.Next();
        reference.Next();
        var third = reference.Next();

        var game = CreateGame();
        game.Prophet.Pronounce();
        game.Time.World.Year = 100;
        game.Prophet.OnTimeAdvanced();
        game.Prophet.Pronounce();
        var loaded = RoundTrip(game);
        loaded.Time.World.Year = 200;
        loaded.Prophet.OnTimeAdvanced();

        Assert.Equal(third, loaded.Prophet.Dream);
    }

    [Fact]
    [Trait("TS", "TS-save-data-006")]
    public void RoundTrip_現実同期で進み3600秒_速さと進みが戻り止まっている()
    {
        var game = CreateGame();
        game.Clock.CycleSpeed();
        game.Clock.CycleSpeed();
        game.Clock.Tick(3600.0);
        game.Time.StartAutoPlay();

        var loaded = RoundTrip(game);

        Assert.Equal(AutoPlaySpeed.RealTime, loaded.Clock.Speed);
        Assert.Equal(3600.0, loaded.Clock.ElapsedSeconds);
        Assert.Equal(TimeControlState.Stopped, loaded.Time.State);
    }

    [Fact]
    [Trait("TS", "TS-save-data-004")]
    public void TryParse_正しいセーブ_読める()
    {
        var ok = SaveDataSerializer.TryParse(ValidSave, TestRules.Dream, TestRules.ProphetDreams, out var data, out var reason);

        Assert.True(ok, reason);
        Assert.Equal(312, data!.World.Year);
        Assert.Equal("THIRST", data.RequestId);
    }

    [Theory]
    [Trait("TS", "TS-save-data-004")]
    [InlineData("{", "", "invalid_json")]
    [InlineData("\"saveVersion\": 2", "\"saveVersion\": 1", "unsupported_save_version")]
    [InlineData("\"saveVersion\": 2, ", "", "invalid_field")]
    [InlineData("\"seed\": 42", "\"seed\": \"42\"", "invalid_field")]
    [InlineData("\"dreamsGenerated\": 2", "\"dreamsGenerated\": -1", "invalid_dreams_generated")]
    [InlineData("\"dreamsGenerated\": 2", "\"dreamsGenerated\": 1000001", "invalid_dreams_generated")]
    [InlineData("\"year\": 312", "\"year\": -1", "invalid_year")]
    [InlineData("\"forestAmount\": 50", "\"forestAmount\": 1e999", "invalid_number")]
    [InlineData("\"isDestroyed\": false", "\"isDestroyed\": 0", "invalid_field")]
    [InlineData("\"targetSymbolId\": \"SYM_TREE\"", "\"targetSymbolId\": \"SYM_STONE\"", "unknown_symbol")]
    [InlineData("\"dogmaType\": \"Taboo\"", "\"dogmaType\": \"0\"", "unknown_dogma_type")]
    [InlineData("\"symbolId\": \"SYM_BLOOD\"", "\"symbolId\": \"SYM_STONE\"", "unknown_symbol")]
    [InlineData("\"weight\": 0.6", "\"weight\": 1.5", "invalid_dream")]
    [InlineData("\"state\": \"TAMPERED\"", "\"state\": \"NONE\"", "invalid_dream")]
    [InlineData("\"state\": \"TAMPERED\"", "\"state\": \"DREAMING\"", "invalid_dream")]
    [InlineData(", { \"symbolId\": \"SYM_FIRE\", \"weight\": 0.4 }", "", "invalid_dream")]
    [InlineData("\"request\": \"THIRST\"", "\"request\": \"BOREDOM\"", "unknown_request_type")]
    [InlineData("\"FAMINE\": 250", "\"BOREDOM\": 250", "unknown_request_type")]
    [InlineData("\"FAMINE\": 250", "\"FAMINE\": 400", "invalid_year")]
    [InlineData("\"lastOracleYear\": 250", "\"lastOracleYear\": 313", "invalid_year")]
    [InlineData("\"lastOracleYear\": 250", "\"lastOracle\": 250", "invalid_field")]
    [InlineData("\"speed\": \"REAL_TIME\"", "\"speed\": \"SLOW\"", "unknown_speed")]
    [InlineData("\"elapsedSeconds\": 3600", "\"elapsedSeconds\": -1", "invalid_number")]
    [InlineData("\"elapsedSeconds\": 3600", "\"elapsedSeconds\": 1e999", "invalid_number")]
    [InlineData("\"elapsedSeconds\": 3600", "\"elapsed\": 3600", "invalid_field")]
    public void TryParse_壊れたセーブ_失敗して理由が分かる(string original, string replacement, string expectedReason)
    {
        Assert.Contains(original, ValidSave);
        var text = original == "{" ? "{" : ValidSave.Replace(original, replacement, StringComparison.Ordinal);

        var ok = SaveDataSerializer.TryParse(text, TestRules.Dream, TestRules.ProphetDreams, out var data, out var reason);

        Assert.False(ok);
        Assert.Null(data);
        Assert.Equal(expectedReason, reason);
    }
}
