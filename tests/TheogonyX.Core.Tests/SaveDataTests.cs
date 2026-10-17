using TheogonyX.Core.Persistence;

namespace TheogonyX.Core.Tests;

public class SaveDataTests
{
    private const int Seed = 42;

    // docs/domain/save-data.md の形の、正しいセーブデータ
    private const string ValidSave = """
        {
          "saveVersion": 17, "seed": 42,
          "world": { "year": 312, "forestAmount": 50, "waterAmount": 10, "temperature": 18,
                     "chaosLevel": 22, "harmonyLevel": 70, "isDestroyed": false },
          "dogmas": [{ "targetSymbolId": "SYM_TREE", "dogmaType": "Taboo", "intensity": 0.74 }],
          "request": "THIRST",
          "answeredYears": { "FAMINE": 250 },
          "autoPlay": { "speed": "REAL_TIME", "elapsedSeconds": 3600 },
          "civilization": {
            "regions": [
              { "tech": 0.25, "culture": 0.1, "techStage": 1, "cultureStage": 0 },
              { "tech": 0, "culture": 0, "techStage": 0, "cultureStage": 0 }, { "tech": 0, "culture": 0, "techStage": 0, "cultureStage": 0 },
              { "tech": 0, "culture": 0, "techStage": 0, "cultureStage": 0 }, { "tech": 0, "culture": 0, "techStage": 0, "cultureStage": 0 },
              { "tech": 0, "culture": 0, "techStage": 0, "cultureStage": 0 }, { "tech": 0, "culture": 0, "techStage": 0, "cultureStage": 0 },
              { "tech": 0, "culture": 0, "techStage": 0, "cultureStage": 0 }
            ],
            "buildings": [{ "id": 0, "type": "WORKSHOP", "region": 3, "column": 6, "row": 2, "collapseYear": 500 },
                          { "id": 3, "type": "HOUSE_TIMBER", "region": 3, "column": 6, "row": 0, "collapseYear": 600 }],
            "ruins": [{ "id": 1, "type": "SHRINE", "region": 3, "column": 6, "row": 4, "ruinedYear": 300 }],
            "relics": [{ "id": 2, "type": "SHRINE", "region": 3, "column": 6, "row": 6, "depth": 1, "buriedYear": 280,
                         "tech": 0.3, "culture": 0.2, "chaos": 0.1, "dogmas": [{ "targetSymbolId": "SYM_FIRE", "dogmaType": "Worship", "intensity": 0.5 }] }],
            "nextBuildingId": 4
          },
          "residents": {
            "people": [{ "id": 3, "region": 3, "birthYear": 290, "hunger": 0.1, "shelter": 0.1, "social": 0, "esteem": 0, "actualization": 0,
                         "health": 100, "sanity": 80, "breakdown": null, "breakdownYear": 0, "task": "IDLE",
                         "name": { "firstPrefix": "NAME_PREFIX_OR", "firstSuffix": "NAME_SUFFIX_MION", "lineagePrefix": "NAME_PREFIX_SELE", "lineageSuffix": "NAME_SUFFIX_PHIA" },
                         "epithet": null, "partnerId": 4, "housesBuilt": 0 },
                       { "id": 4, "region": 3, "birthYear": 280, "hunger": 0.1, "shelter": 0.1, "social": 0.4, "esteem": 0.3, "actualization": 0.2,
                         "health": 90, "sanity": 0, "breakdown": "RAMPAGE", "breakdownYear": 311, "task": "BREAKDOWN",
                         "name": { "firstPrefix": "NAME_PREFIX_ART", "firstSuffix": "NAME_SUFFIX_ORIUS", "lineagePrefix": "NAME_PREFIX_ILIO", "lineageSuffix": "NAME_SUFFIX_A" },
                         "epithet": { "id": "EPITHET_BLOODLESS", "symbolId": null }, "partnerId": 3, "housesBuilt": 1 }],
            "graves": [{ "residentId": 2, "region": 1, "deathYear": 305,
                         "name": { "firstPrefix": "NAME_PREFIX_VAL", "firstSuffix": "NAME_SUFFIX_US", "lineagePrefix": "NAME_PREFIX_EL", "lineageSuffix": "NAME_SUFFIX_GIS" },
                         "epithet": { "id": "EPITHET_WITNESS", "symbolId": "SYM_WATER" } }],
            "nextResidentId": 5
          },
          "historyChapterStart": { "year": 300, "chaos": 12.5 },
          "deck": { "drawPile": ["SYM_TREE", "SYM_WATER"], "hand": ["SYM_BLOOD", "SYM_FIRE", "SYM_TREE", "SYM_WATER"],
                    "discardPile": ["SYM_FIRE"], "shuffleCount": 2, "corruptionStage": 1, "lastBuildingId": 3, "pruneCharges": 1 },
          "faith": { "value": 1.5, "recentYears": [300, 310] },
          "history": [
            { "year": 200, "type": "DOGMA_FORMED", "symbolId": "SYM_TREE", "dogmaType": "Taboo", "name": null, "epithet": null, "breakdown": null,
              "oracle": { "heading": "木に触れるな", "intent": "神は木を憎んでおられる。", "doctrine": "木を見た者は目を閉じよ。" },
              "fromYear": null, "chaosFrom": null, "chaosTo": null, "myth": null },
            { "year": 300, "type": "CHAPTER_ENDED", "symbolId": null, "dogmaType": null, "name": null, "epithet": null, "breakdown": null, "oracle": null,
              "fromYear": 200, "chaosFrom": 10.0, "chaosTo": 40.0, "myth": { "heading": "木の時代", "body": "木は人を見下ろし、人は木を仰いだ。" } },
            { "year": 300, "type": "WORLD_RECREATED", "symbolId": null, "dogmaType": null, "name": null, "epithet": null, "breakdown": null, "oracle": null,
              "fromYear": null, "chaosFrom": null, "chaosTo": null, "myth": null },
            { "year": 305, "type": "NOTABLE_DIED", "symbolId": null, "dogmaType": null,
              "name": { "firstPrefix": "NAME_PREFIX_VAL", "firstSuffix": "NAME_SUFFIX_US", "lineagePrefix": "NAME_PREFIX_EL", "lineageSuffix": "NAME_SUFFIX_GIS" },
              "epithet": { "id": "EPITHET_BUILDER", "symbolId": null }, "breakdown": null, "oracle": null,
              "fromYear": null, "chaosFrom": null, "chaosTo": null, "myth": null },
            { "year": 311, "type": "RESIDENT_BROKE_DOWN", "symbolId": null, "dogmaType": null,
              "name": { "firstPrefix": "NAME_PREFIX_ART", "firstSuffix": "NAME_SUFFIX_ORIUS", "lineagePrefix": "NAME_PREFIX_ILIO", "lineageSuffix": "NAME_SUFFIX_A" },
              "epithet": null, "breakdown": "RAMPAGE", "oracle": null,
              "fromYear": null, "chaosFrom": null, "chaosTo": null, "myth": null }
          ]
        }
        """;

    private sealed record Game(TimeControl Time, ProphetDreams Prophet, AutoPlayClock Clock);

    private static Game CreateGame(int jumpYears = TestRules.JumpYears, BeliefRules? belief = null, JourneyRules? journey = null)
    {
        var time = new TimeControl(
            TestRules.World, jumpYears, new CivilizationSetup(TestRules.Civilization, TestRules.Terrain, Seed, TestRules.Needs), TestRules.History, belief, journey);
        var prophet = new ProphetDreams(time, TestRules.ProphetDreams, new OracleDeck(Seed, TestRules.Deck), TestRules.Oracle);
        var clock = new AutoPlayClock(new Dictionary<AutoPlaySpeed, double>
        {
            [AutoPlaySpeed.X1] = 600.0,
            [AutoPlaySpeed.X2] = 300.0,
            [AutoPlaySpeed.X3] = 200.0,
        });
        return new Game(time, prophet, clock);
    }

    // 書き出して読み戻し、新しく作ったゲームに当てはめる
    private static Game RoundTrip(Game game, int jumpYears = TestRules.JumpYears, BeliefRules? belief = null, JourneyRules? journey = null)
    {
        var json = SaveDataSerializer.Write(SaveGame.Capture(game.Time, game.Prophet, Seed, game.Clock));
        Assert.True(SaveDataSerializer.TryParse(json, TestRules.Dream, TestRules.ProphetDreams, TestRules.Civilization, TestRules.Needs, TestRules.Oracle, out var data, out var reason), reason);
        var loaded = CreateGame(jumpYears, belief, journey);
        SaveGame.Apply(data!, loaded.Time, loaded.Prophet, loaded.Clock);
        return loaded;
    }

    [Fact]
    [Trait("TS", "TS-belief-009")]
    public void RoundTrip_信仰を持つ住人と忘れられた教え_同じ状態に戻り続けた結果も同じ()
    {
        var belief = new BeliefRules(MaxBeliefs: 3, SeedRegions: 1, GatherTransmit: 0.5f, PartnerTransmit: 0.5f, Dampen: 0.8f, InheritDampen: 0.8f);
        var game = CreateGame(1, belief);
        game.Time.AddDogma(new Dogma("SYM_FIRE", DogmaType.Worship, 0.5f)); // 住人がいないので、次の年に忘れられる
        for (var i = 0; i < 5; i++)
        {
            game.Time.Jump();
        }
        game.Time.AddDogma(new Dogma("SYM_TREE", DogmaType.Worship, 0.8f));
        for (var i = 0; i < 20; i++)
        {
            game.Time.Jump();
        }

        var loaded = RoundTrip(game, 1, belief);
        game.Time.Jump();
        loaded.Time.Jump();

        Assert.Contains(game.Time.Residents!.People, person => person.Beliefs.Count > 0);
        Assert.Equal(game.Time.Residents.People, loaded.Time.Residents!.People);
        Assert.Equal(game.Time.Dogmas, loaded.Time.Dogmas);
        Assert.Contains(loaded.Time.History!.Entries, entry => entry.Type == HistoryEventType.DogmaForgotten && entry.SymbolId == "SYM_FIRE");
    }

    [Fact]
    [Trait("TS", "TS-prophet-journey-006")]
    public void RoundTrip_旅の途中の予言者_居場所と知っている土地と行き先が戻る()
    {
        var journey = new JourneyRules(0f);
        var game = CreateGame(1, null, journey);
        game.Time.Jump();
        game.Time.SetDestination(TravelDirection.East);
        game.Time.Jump();
        game.Time.Jump();
        var before = game.Time.Prophet!;

        var loaded = RoundTrip(game, 1, null, journey);

        Assert.Equal(5, before.Region);
        Assert.Equal(before.Region, loaded.Time.Prophet!.Region);
        Assert.Equal(before.KnownRegions, loaded.Time.Prophet.KnownRegions);
        Assert.Equal(TravelDirection.East, loaded.Time.Prophet.Destination);
    }

    [Fact]
    [Trait("TS", "TS-save-data-001")]
    [Trait("TS", "TS-oracle-deck-007")]
    public void RoundTrip_依頼中で神託を1回下した後_同じ状態に戻る()
    {
        var game = CreateGame();
        game.Time.AddDogma(new Dogma("SYM_TREE", DogmaType.Taboo, 0.74f));
        // 年 250 で飢えの依頼に応え、年 312 で渇きの依頼を受けた
        game.Time.World.Year = 250;
        game.Time.World.ForestAmount = 10f;
        game.Prophet.OnTimeAdvanced();
        game.Prophet.SpeakFirst();
        game.Time.World.Year = 312;
        game.Time.World.ForestAmount = 50f;
        game.Time.World.WaterAmount = 10f;
        game.Prophet.OnTimeAdvanced();
        var dogmasBefore = game.Time.Dogmas.ToList();
        var handBefore = game.Prophet.Deck.Hand.ToList();
        var pileBefore = game.Prophet.Deck.DrawPile.ToList();
        var discardBefore = game.Prophet.Deck.DiscardPile.ToList();
        var faithBefore = game.Prophet.Faith.Value;

        var loaded = RoundTrip(game);

        Assert.Equal(312, loaded.Time.World.Year);
        Assert.Equal(50f, loaded.Time.World.ForestAmount);
        Assert.Equal(10f, loaded.Time.World.WaterAmount);
        Assert.Equal(dogmasBefore, loaded.Time.Dogmas);
        Assert.Equal("THIRST", loaded.Prophet.ActiveRequest?.Id);
        Assert.Equal(new Dictionary<string, int> { ["FAMINE"] = 250 }, loaded.Prophet.AnsweredYears);
        Assert.Equal(handBefore, loaded.Prophet.Deck.Hand);
        Assert.Equal(pileBefore, loaded.Prophet.Deck.DrawPile);
        Assert.Equal(discardBefore, loaded.Prophet.Deck.DiscardPile);
        Assert.Equal(faithBefore, loaded.Prophet.Faith.Value, 6);
        Assert.Equal([250], loaded.Prophet.Faith.RecentYears);
    }

    [Fact]
    [Trait("TS", "TS-save-data-001")]
    public void RoundTrip_崩壊している_崩壊していて神託を下せない()
    {
        var game = CreateGame();
        game.Time.World.IsDestroyed = true;

        var loaded = RoundTrip(game);

        Assert.True(loaded.Time.World.IsDestroyed);
        Assert.Equal(TimeControlState.Destroyed, loaded.Time.State);
        Assert.NotNull(loaded.Prophet.SpeakRejection());
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
    public void RoundTrip_山札を使い切る途中で保存_続けた結果が保存しなかった場合と同じ()
    {
        static Game Start()
        {
            var game = CreateGame();
            for (var i = 0; i < 3; i++)
            {
                game.Prophet.SpeakFirst();
                game.Time.World.Year += 50;
                game.Prophet.OnTimeAdvanced();
            }
            return game;
        }
        var reference = Start();
        var loaded = RoundTrip(Start());

        for (var i = 0; i < 4; i++)
        {
            reference.Prophet.SpeakFirst();
            loaded.Prophet.SpeakFirst();
            reference.Time.World.Year += 50;
            loaded.Time.World.Year += 50;
            reference.Prophet.OnTimeAdvanced();
            loaded.Prophet.OnTimeAdvanced();
        }

        Assert.Equal(reference.Prophet.Deck.Hand, loaded.Prophet.Deck.Hand);
        Assert.Equal(reference.Prophet.Deck.DrawPile, loaded.Prophet.Deck.DrawPile);
        Assert.Equal(reference.Prophet.Deck.DiscardPile, loaded.Prophet.Deck.DiscardPile);
        Assert.Equal(reference.Time.Dogmas, loaded.Time.Dogmas);
    }

    [Fact]
    [Trait("TS", "TS-save-data-006")]
    public void RoundTrip_3倍で進み100秒_速さと進みが戻り止まっている()
    {
        var game = CreateGame();
        game.Clock.CycleSpeed();
        game.Clock.CycleSpeed();
        game.Clock.Tick(100.0);
        game.Time.StartAutoPlay();

        var loaded = RoundTrip(game);

        Assert.Equal(AutoPlaySpeed.X3, loaded.Clock.Speed);
        Assert.Equal(100.0, loaded.Clock.ElapsedSeconds);
        Assert.Equal(TimeControlState.Stopped, loaded.Time.State);
    }

    [Fact]
    [Trait("TS", "TS-save-data-007")]
    public void RoundTrip_200年の途中で保存_続けた結果が保存しなかった場合と同じ()
    {
        static Game Start()
        {
            var game = CreateGame();
            game.Time.World.ForestAmount = 50f;
            game.Time.AddDogma(new Dogma("SYM_FIRE", DogmaType.Worship, 0.4f));
            return game;
        }
        var reference = Start();
        reference.Time.Jump();
        reference.Time.Jump();

        var game = Start();
        game.Time.Jump();
        var loaded = RoundTrip(game);
        loaded.Time.Jump();

        Assert.NotEmpty(reference.Time.Civilization!.Buildings);
        Assert.Equal(reference.Time.Civilization.Regions, loaded.Time.Civilization!.Regions);
        Assert.Equal(reference.Time.Civilization.Buildings, loaded.Time.Civilization.Buildings);
        Assert.Equal(reference.Time.Civilization.Ruins, loaded.Time.Civilization.Ruins);
        Assert.Equal(reference.Time.Civilization.Relics, loaded.Time.Civilization.Relics);
        Assert.Equal(reference.Time.Civilization.NextBuildingId, loaded.Time.Civilization.NextBuildingId);
    }

    [Fact]
    [Trait("TS", "TS-save-data-008")]
    public void RoundTrip_再創生の後の300年目で保存_続けた結果が保存しなかった場合と同じ()
    {
        static Game Start()
        {
            var game = CreateGame();
            game.Time.World.ForestAmount = 50f;
            game.Time.AddDogma(new Dogma("SYM_FIRE", DogmaType.Worship, 0.4f));
            for (var i = 0; i < 4; i++)
            {
                game.Time.Jump();
            }
            game.Time.Reset();
            game.Time.World.ForestAmount = 50f;
            for (var i = 0; i < 3; i++)
            {
                game.Time.Jump();
            }
            return game;
        }
        var reference = Start();
        reference.Time.Jump();
        reference.Time.Jump();

        var loaded = RoundTrip(Start());
        loaded.Time.Jump();
        loaded.Time.Jump();

        Assert.NotEmpty(reference.Time.Civilization!.Ruins);
        Assert.NotEmpty(reference.Time.Civilization.Relics);
        Assert.Equal(reference.Time.Civilization.Buildings, loaded.Time.Civilization!.Buildings);
        Assert.Equal(reference.Time.Civilization.Ruins, loaded.Time.Civilization.Ruins);
        Assert.Equal(reference.Time.Civilization.Relics, loaded.Time.Civilization.Relics);
    }

    [Fact]
    [Trait("TS", "TS-save-data-004")]
    [Trait("TS", "TS-history-007")]
    [Trait("TS", "TS-ai-presentation-005")]
    public void TryParse_正しいセーブ_読める()
    {
        var ok = SaveDataSerializer.TryParse(ValidSave, TestRules.Dream, TestRules.ProphetDreams, TestRules.Civilization, TestRules.Needs, TestRules.Oracle, out var data, out var reason);

        Assert.True(ok, reason);
        Assert.Equal(312, data!.World.Year);
        Assert.Equal("THIRST", data.RequestId);
        Assert.Equal(["SYM_TREE", "SYM_WATER"], data.Deck.DrawPile);
        Assert.Equal(["SYM_BLOOD", "SYM_FIRE", "SYM_TREE", "SYM_WATER"], data.Deck.Hand);
        Assert.Equal(["SYM_FIRE"], data.Deck.DiscardPile);
        Assert.Equal(2, data.Deck.ShuffleCount);
        Assert.Equal(1, data.Deck.CorruptionStage);
        Assert.Equal(3, data.Deck.LastBuildingId);
        Assert.Equal(1.5, data.Faith.Value, 6);
        Assert.Equal([300, 310], data.Faith.RecentYears);
        Assert.Equal(new RegionCivilization(0.25, 0.1, 1, 0), data.Civilization.Regions[0]);
        Assert.Equal(
            [new Building(0, BuildingType.Workshop, 3, 6, 2, 500), new Building(3, BuildingType.HouseTimber, 3, 6, 0, 600)],
            data.Civilization.Buildings);
        Assert.Equal(new Ruin(1, BuildingType.Shrine, 3, 6, 4, 300), Assert.Single(data.Civilization.Ruins));
        Assert.Equal(
            new Relic(2, BuildingType.Shrine, 3, 6, 6, 1, 280, 0.3, 0.2, 0.1, [new Dogma("SYM_FIRE", DogmaType.Worship, 0.5f)]),
            Assert.Single(data.Civilization.Relics));
        Assert.Equal(
            new Resident(
                4, 3, 280, 0.1, 0.1, 0.4, 0.3, 0.2, 90, 0, BreakdownKind.Rampage, 311, ResidentTask.Breakdown,
                new ResidentName("NAME_PREFIX_ART", "NAME_SUFFIX_ORIUS", "NAME_PREFIX_ILIO", "NAME_SUFFIX_A"), new ResidentEpithet(Epithets.Bloodless), 3, 1),
            data.Residents.People[1]);
        Assert.Equal(4, data.Residents.People[0].PartnerId);
        Assert.Equal(
            new Grave(
                2, 1, 305, new ResidentName("NAME_PREFIX_VAL", "NAME_SUFFIX_US", "NAME_PREFIX_EL", "NAME_SUFFIX_GIS"),
                new ResidentEpithet(Epithets.Witness, "SYM_WATER")),
            Assert.Single(data.Residents.Graves));
        Assert.Equal(5, data.Residents.NextResidentId);
        Assert.Equal(
            [
                new HistoryEntry(
                    200, HistoryEventType.DogmaFormed, "SYM_TREE", DogmaType.Taboo,
                    Oracle: new OracleText("木に触れるな", "神は木を憎んでおられる。", "木を見た者は目を閉じよ。")),
                new HistoryEntry(
                    300, HistoryEventType.ChapterEnded, FromYear: 200, ChaosFrom: 10f, ChaosTo: 40f,
                    Myth: new MythText("木の時代", "木は人を見下ろし、人は木を仰いだ。")),
                new HistoryEntry(300, HistoryEventType.WorldRecreated),
                new HistoryEntry(
                    305, HistoryEventType.NotableDied,
                    Name: new ResidentName("NAME_PREFIX_VAL", "NAME_SUFFIX_US", "NAME_PREFIX_EL", "NAME_SUFFIX_GIS"),
                    Epithet: new ResidentEpithet(Epithets.Builder)),
                new HistoryEntry(
                    311, HistoryEventType.ResidentBrokeDown,
                    Name: new ResidentName("NAME_PREFIX_ART", "NAME_SUFFIX_ORIUS", "NAME_PREFIX_ILIO", "NAME_SUFFIX_A"),
                    Breakdown: BreakdownKind.Rampage),
            ],
            data.History);
        Assert.Equal(new ChapterStart(300, 12.5f), data.Chapter);
    }

    [Fact]
    [Trait("TS", "TS-save-data-009")]
    [Trait("TS", "TS-history-007")]
    public void RoundTrip_住人と墓がある_続けた結果が保存しなかった場合と同じ()
    {
        static Game Start()
        {
            var game = CreateGame();
            game.Time.World.ForestAmount = 50f;
            game.Time.AddDogma(new Dogma("SYM_TREE", DogmaType.Destruction, 0.2f));
            game.Time.Jump();
            return game;
        }
        var reference = Start();
        reference.Time.Jump();

        var loaded = RoundTrip(Start());
        loaded.Time.Jump();

        Assert.NotEmpty(reference.Time.Residents!.People);
        Assert.NotEmpty(reference.Time.Residents.Graves);
        Assert.Equal(reference.Time.Residents.People, loaded.Time.Residents!.People);
        Assert.Equal(reference.Time.Residents.Graves, loaded.Time.Residents.Graves);
        Assert.Equal(reference.Time.Residents.NextResidentId, loaded.Time.Residents.NextResidentId);
        Assert.NotEmpty(reference.Time.History!.Entries);
        Assert.Equal(reference.Time.History.Entries, loaded.Time.History!.Entries);
        Assert.Equal(reference.Time.World.ChaosLevel, loaded.Time.World.ChaosLevel);
    }

    [Theory]
    [Trait("TS", "TS-save-data-004")]
    [InlineData("{", "", "invalid_json")]
    [InlineData("\"saveVersion\": 17", "\"saveVersion\": 14", "unsupported_save_version")]
    [InlineData("\"saveVersion\": 17, ", "", "invalid_field")]
    [InlineData("\"seed\": 42", "\"seed\": \"42\"", "invalid_field")]
    [InlineData("\"year\": 312", "\"year\": -1", "invalid_year")]
    [InlineData("\"forestAmount\": 50", "\"forestAmount\": 1e999", "invalid_number")]
    [InlineData("\"isDestroyed\": false", "\"isDestroyed\": 0", "invalid_field")]
    [InlineData("\"targetSymbolId\": \"SYM_TREE\"", "\"targetSymbolId\": \"SYM_STONE\"", "unknown_symbol")]
    [InlineData("\"dogmaType\": \"Taboo\"", "\"dogmaType\": \"0\"", "unknown_dogma_type")]
    [InlineData("\"hand\": [\"SYM_BLOOD\"", "\"hand\": [\"SYM_STONE\"", "invalid_deck")] // 手札に定義に無い記号
    [InlineData("\"request\": \"THIRST\"", "\"request\": \"BOREDOM\"", "unknown_request_type")]
    [InlineData("\"FAMINE\": 250", "\"BOREDOM\": 250", "unknown_request_type")]
    [InlineData("\"FAMINE\": 250", "\"FAMINE\": 400", "invalid_year")]
    [InlineData("\"speed\": \"REAL_TIME\"", "\"speed\": \"SLOW\"", "unknown_speed")]
    [InlineData("\"elapsedSeconds\": 3600", "\"elapsedSeconds\": -1", "invalid_number")]
    [InlineData("\"elapsedSeconds\": 3600", "\"elapsedSeconds\": 1e999", "invalid_number")]
    [InlineData("\"elapsedSeconds\": 3600", "\"elapsed\": 3600", "invalid_field")]
    [InlineData("\"tech\": 0.25", "\"tech\": 1.5", "invalid_civilization")]
    [InlineData("\"techStage\": 1", "\"techStage\": 5", "invalid_civilization")]
    [InlineData("{ \"tech\": 0.25, \"culture\": 0.1, \"techStage\": 1, \"cultureStage\": 0 },", "", "invalid_civilization")] // 地域が 7 個
    [InlineData("\"type\": \"WORKSHOP\"", "\"type\": \"CASTLE\"", "invalid_civilization")]
    [InlineData("\"column\": 6", "\"column\": 8", "invalid_civilization")] // 地域 3 の外
    [InlineData("\"row\": 2", "\"row\": 3", "invalid_civilization")] // 枠の左上ではない
    [InlineData("\"row\": 2", "\"row\": 8", "invalid_civilization")] // 2 × 2 が地表からはみ出す
    [InlineData("\"id\": 0", "\"id\": 4", "invalid_civilization")] // 次の ID 以上
    [InlineData("\"id\": 1", "\"id\": 0", "invalid_civilization")] // 建物と廃墟で ID が重なる
    [InlineData("\"collapseYear\": 500", "\"collapseYear\": -1", "invalid_civilization")]
    [InlineData("\"ruinedYear\": 300", "\"ruinedYear\": 313", "invalid_civilization")] // 今の年より後
    [InlineData("\"depth\": 1", "\"depth\": 0", "invalid_civilization")]
    [InlineData("\"depth\": 1", "\"depth\": 4", "invalid_civilization")] // 3 層より深い
    [InlineData("\"chaos\": 0.1", "\"chaos\": 1.5", "invalid_civilization")]
    [InlineData("\"ruins\": [", "\"ruin\": [", "invalid_field")]
    [InlineData("\"intensity\": 0.5", "\"intensity\": \"0.5\"", "invalid_field")]
    [InlineData("\"nextBuildingId\": 4", "\"nextBuilding\": 4", "invalid_field")]
    [InlineData("\"hunger\": 0.1", "\"hunger\": 1.5", "invalid_residents")]
    [InlineData("\"health\": 90", "\"health\": 0", "invalid_residents")] // 体力 0 の住人は死んでいる
    [InlineData("\"sanity\": 0", "\"sanity\": 101", "invalid_residents")]
    [InlineData("\"breakdown\": \"RAMPAGE\"", "\"breakdown\": \"CULT_LEADER\"", "invalid_residents")]
    [InlineData("\"breakdownYear\": 311", "\"breakdownYear\": 313", "invalid_residents")]
    [InlineData("\"task\": \"BREAKDOWN\"", "\"task\": \"BUILD\"", "invalid_residents")]
    [InlineData("\"birthYear\": 280", "\"birthYear\": 313", "invalid_residents")]
    [InlineData("\"firstPrefix\": \"NAME_PREFIX_ART\"", "\"firstPrefix\": \"NAME_PREFIX_DAGO\"", "invalid_residents")]
    [InlineData("\"id\": \"EPITHET_BLOODLESS\"", "\"id\": \"EPITHET_CONQUEROR\"", "invalid_residents")]
    [InlineData("\"symbolId\": \"SYM_WATER\"", "\"symbolId\": \"SYM_STONE\"", "invalid_residents")]
    [InlineData("\"residentId\": 2", "\"residentId\": 4", "invalid_residents")] // 住人と墓で ID が重なる
    [InlineData("\"nextResidentId\": 5", "\"nextResidentId\": 4", "invalid_residents")] // 次の ID 以上
    [InlineData("\"region\": 1", "\"region\": 8", "invalid_residents")] // 地域の外
    [InlineData("\"deathYear\": 305", "\"deathYear\": 313", "invalid_residents")]
    [InlineData("\"graves\": [", "\"grave\": [", "invalid_field")]
    [InlineData("\"partnerId\": 4", "\"partnerId\": \"4\"", "invalid_field")]
    [InlineData("\"housesBuilt\": 1", "\"housesBuilt\": -1", "invalid_residents")]
    [InlineData("\"heading\": \"木に触れるな\"", "\"heading\": \"\"", "invalid_history")] // 神託の文が空
    [InlineData("\"heading\": \"木に触れるな\"", "\"heading\": \"一二三四五六七八九十一二三四五六七八九十一二三四五六七八九十一\"", "invalid_history")] // 31 字
    [InlineData("\"intent\": \"神は木を憎んでおられる。\"", "\"intent\": 3", "invalid_field")]
    [InlineData("\"type\": \"WORLD_RECREATED\", \"symbolId\": null, \"dogmaType\": null, \"name\": null, \"epithet\": null, \"breakdown\": null, \"oracle\": null", "\"type\": \"WORLD_RECREATED\", \"symbolId\": null, \"dogmaType\": null, \"name\": null, \"epithet\": null, \"breakdown\": null, \"oracle\": { \"heading\": \"a\", \"intent\": \"b\", \"doctrine\": \"c\" }", "invalid_history")] // ドグマの行でない
    [InlineData("\"history\": [", "\"histories\": [", "invalid_field")]
    [InlineData("\"type\": \"DOGMA_FORMED\"", "\"type\": \"WAR_STARTED\"", "invalid_history")]
    [InlineData("\"year\": 200", "\"year\": -1", "invalid_history")]
    [InlineData("\"chaosTo\": 40.0", "\"chaosTo\": 140.0", "invalid_history")] // 章のカオス度は 0〜100
    [InlineData("\"fromYear\": 200, \"chaosFrom\": 10.0", "\"fromYear\": 300, \"chaosFrom\": 10.0", "invalid_history")] // 章の始まりは終わりより前
    [InlineData("\"body\": \"木は人を見下ろし、人は木を仰いだ。\"", "\"body\": \"\"", "invalid_history")] // 神話の本文が空
    [InlineData("\"historyChapterStart\": { \"year\": 300", "\"historyChapterStart\": { \"year\": 400", "invalid_year")]
    [InlineData("\"chaos\": 12.5", "\"chaos\": -1", "invalid_number")]
    [InlineData("\"drawPile\": [\"SYM_TREE\"", "\"drawPile\": [\"SYM_STONE\"", "invalid_deck")] // 定義に無い記号のカード
    [InlineData("\"shuffleCount\": 2", "\"shuffleCount\": -1", "invalid_deck")]
    [InlineData("\"corruptionStage\": 1", "\"corruptionStage\": -1", "invalid_deck")]
    [InlineData("\"lastBuildingId\": 3", "\"lastBuildingId\": -2", "invalid_deck")]
    [InlineData("\"pruneCharges\": 1", "\"pruneCharges\": -1", "invalid_deck")]
    [InlineData("\"value\": 1.5", "\"value\": -1", "invalid_faith")]
    [InlineData("\"recentYears\": [300, 310]", "\"recentYears\": [300, 313]", "invalid_year")] // 今の年より後
    [InlineData("\"recentYears\": [300, 310]", "\"recentYears\": [310, 300]", "invalid_faith")] // 古い順でない
    [InlineData("\"recentYears\": [300, 310]", "\"recentYears\": \"none\"", "invalid_field")]
    [InlineData("\"symbolId\": \"SYM_TREE\", \"dogmaType\": \"Taboo\"", "\"symbolId\": \"SYM_STONE\", \"dogmaType\": \"Taboo\"", "invalid_history")]
    [InlineData("\"dogmaType\": \"Taboo\", \"name\"", "\"dogmaType\": \"Love\", \"name\"", "invalid_history")]
    [InlineData("\"epithet\": { \"id\": \"EPITHET_BUILDER\", \"symbolId\": null }, \"breakdown\": null", "\"epithet\": null, \"breakdown\": null", "invalid_history")] // 称号の無い住人の死は残さない
    [InlineData("\"epithet\": null, \"breakdown\": \"RAMPAGE\"", "\"epithet\": null, \"breakdown\": \"FRENZY\"", "invalid_history")]
    [InlineData("\"type\": \"WORLD_RECREATED\", \"symbolId\": null", "\"type\": \"WORLD_RECREATED\", \"symbolId\": \"SYM_TREE\"", "invalid_history")] // 使わない項目は null
    [InlineData("\"partnerId\": 4", "\"partnerId\": 2", "invalid_residents")] // 相手が住人にいない（墓の ID）
    [InlineData("\"partnerId\": 3", "\"partnerId\": null", "invalid_residents")] // 相手のパートナーが自分でない
    [InlineData("\"partnerId\": 3", "\"partnerId\": 4", "invalid_residents")] // 自分自身
    [Trait("TS", "TS-history-007")]
    [Trait("TS", "TS-ai-presentation-005")]
    public void TryParse_壊れたセーブ_失敗して理由が分かる(string original, string replacement, string expectedReason)
    {
        Assert.Contains(original, ValidSave);
        var text = original == "{" ? "{" : ValidSave.Replace(original, replacement, StringComparison.Ordinal);

        var ok = SaveDataSerializer.TryParse(text, TestRules.Dream, TestRules.ProphetDreams, TestRules.Civilization, TestRules.Needs, TestRules.Oracle, out var data, out var reason);

        Assert.False(ok);
        Assert.Null(data);
        Assert.Equal(expectedReason, reason);
    }
}
