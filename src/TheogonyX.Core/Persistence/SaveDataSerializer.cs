using System.Text.Json;

namespace TheogonyX.Core.Persistence;

/// <summary>
/// セーブデータを JSON に書き出し、読み込む（docs/domain/save-data.md）。
/// 読み込んだファイルは利用者が書き換えうる外部入力として扱い、形・値・定義との整合を検証する。
/// </summary>
public static class SaveDataSerializer
{
    /// <summary>今の形式のバージョン。形を変えたら上げる（.agent/rules/save-data.md）。</summary>
    public const int SaveVersion = 15;

    /// <summary>受け付ける「直近の神託の年」の数の上限。</summary>
    public const int MaxRecentOracleYears = 1000;

    private static readonly Dictionary<AutoPlaySpeed, string> SpeedNames = new()
    {
        [AutoPlaySpeed.Normal] = "NORMAL",
        [AutoPlaySpeed.Fast] = "FAST",
        [AutoPlaySpeed.RealTime] = "REAL_TIME",
    };

    private static readonly Dictionary<BuildingType, string> BuildingTypeNames = new()
    {
        [BuildingType.Workshop] = "WORKSHOP",
        [BuildingType.Shrine] = "SHRINE",
        [BuildingType.HouseHut] = "HOUSE_HUT",
        [BuildingType.HouseTimber] = "HOUSE_TIMBER",
        [BuildingType.HouseBrick] = "HOUSE_BRICK",
    };

    private static readonly Dictionary<HistoryEventType, string> HistoryTypeNames = new()
    {
        [HistoryEventType.DogmaFormed] = "DOGMA_FORMED",
        [HistoryEventType.ResidentBrokeDown] = "RESIDENT_BROKE_DOWN",
        [HistoryEventType.NotableDied] = "NOTABLE_DIED",
        [HistoryEventType.WorldDestroyed] = "WORLD_DESTROYED",
        [HistoryEventType.WorldRecreated] = "WORLD_RECREATED",
        [HistoryEventType.ChapterEnded] = "CHAPTER_ENDED",
    };

    private static readonly Dictionary<BreakdownKind, string> BreakdownNames = new()
    {
        [BreakdownKind.Rampage] = "RAMPAGE",
        [BreakdownKind.Catatonia] = "CATATONIA",
    };

    private static readonly Dictionary<ResidentTask, string> TaskNames = new()
    {
        [ResidentTask.Idle] = "IDLE",
        [ResidentTask.SeekFood] = "SEEK_FOOD",
        [ResidentTask.SeekShelter] = "SEEK_SHELTER",
        [ResidentTask.Gather] = "GATHER",
        [ResidentTask.Pray] = "PRAY",
        [ResidentTask.Research] = "RESEARCH",
        [ResidentTask.Breakdown] = "BREAKDOWN",
    };

    private static readonly string[] EpithetIds = [Epithets.Bloodless, Epithets.Witness, Epithets.Elder, Epithets.Builder];

    public static string Write(SaveData data)
    {
        ArgumentNullException.ThrowIfNull(data);
        var world = data.World;
        return JsonSerializer.Serialize(new
        {
            saveVersion = SaveVersion,
            seed = data.Seed,
            world = new
            {
                year = world.Year,
                forestAmount = world.ForestAmount,
                waterAmount = world.WaterAmount,
                temperature = world.Temperature,
                chaosLevel = world.ChaosLevel,
                harmonyLevel = world.HarmonyLevel,
                isDestroyed = world.IsDestroyed,
            },
            dogmas = data.Dogmas.Select(dogma => new
            {
                targetSymbolId = dogma.TargetSymbolId,
                dogmaType = dogma.Type.ToString(),
                intensity = dogma.Intensity,
            }),
            request = data.RequestId,
            answeredYears = data.AnsweredYears,
            autoPlay = new { speed = SpeedNames[data.Speed], elapsedSeconds = data.ElapsedSeconds },
            civilization = new
            {
                regions = data.Civilization.Regions.Select(region => new
                {
                    tech = region.Tech,
                    culture = region.Culture,
                    techStage = region.TechStage,
                    cultureStage = region.CultureStage,
                }),
                buildings = data.Civilization.Buildings.Select(building => new
                {
                    id = building.Id,
                    type = BuildingTypeNames[building.Type],
                    region = building.Region,
                    column = building.Column,
                    row = building.Row,
                    collapseYear = building.CollapseYear,
                }),
                ruins = data.Civilization.Ruins.Select(ruin => new
                {
                    id = ruin.Id,
                    type = BuildingTypeNames[ruin.Type],
                    region = ruin.Region,
                    column = ruin.Column,
                    row = ruin.Row,
                    ruinedYear = ruin.RuinedYear,
                }),
                relics = data.Civilization.Relics.Select(relic => new
                {
                    id = relic.Id,
                    type = BuildingTypeNames[relic.Type],
                    region = relic.Region,
                    column = relic.Column,
                    row = relic.Row,
                    depth = relic.Depth,
                    buriedYear = relic.BuriedYear,
                    tech = relic.Tech,
                    culture = relic.Culture,
                    chaos = relic.Chaos,
                    dogmas = relic.Dogmas.Select(dogma => new
                    {
                        targetSymbolId = dogma.TargetSymbolId,
                        dogmaType = dogma.Type.ToString(),
                        intensity = dogma.Intensity,
                    }),
                }),
                nextBuildingId = data.Civilization.NextBuildingId,
            },
            residents = new
            {
                people = data.Residents.People.Select(person => new
                {
                    id = person.Id,
                    region = person.Region,
                    birthYear = person.BirthYear,
                    hunger = person.Hunger,
                    shelter = person.Shelter,
                    social = person.Social,
                    esteem = person.Esteem,
                    actualization = person.Actualization,
                    health = person.Health,
                    sanity = person.Sanity,
                    breakdown = person.Breakdown is { } kind ? BreakdownNames[kind] : null,
                    breakdownYear = person.BreakdownYear,
                    task = TaskNames[person.Task],
                    name = WriteName(person.Name),
                    epithet = WriteEpithet(person.Epithet),
                    partnerId = person.PartnerId,
                    housesBuilt = person.HousesBuilt,
                }),
                graves = data.Residents.Graves.Select(grave => new
                {
                    residentId = grave.ResidentId,
                    region = grave.Region,
                    deathYear = grave.DeathYear,
                    name = WriteName(grave.Name),
                    epithet = WriteEpithet(grave.Epithet),
                }),
                nextResidentId = data.Residents.NextResidentId,
            },
            history = data.History.Select(entry => new
            {
                year = entry.Year,
                type = HistoryTypeNames[entry.Type],
                symbolId = entry.SymbolId,
                dogmaType = entry.DogmaType?.ToString(),
                name = entry.Name is null ? null : WriteName(entry.Name),
                epithet = WriteEpithet(entry.Epithet),
                breakdown = entry.Breakdown is { } kind ? BreakdownNames[kind] : null,
                oracle = entry.Oracle is null ? null : new { heading = entry.Oracle.Heading, intent = entry.Oracle.Intent, doctrine = entry.Oracle.Doctrine },
                fromYear = entry.FromYear,
                chaosFrom = entry.ChaosFrom,
                chaosTo = entry.ChaosTo,
                myth = entry.Myth is null ? null : new { heading = entry.Myth.Heading, body = entry.Myth.Body },
            }),
            historyChapterStart = new { year = data.Chapter.Year, chaos = data.Chapter.ChaosLevel },
            deck = new
            {
                drawPile = data.Deck.DrawPile,
                hand = data.Deck.Hand,
                discardPile = data.Deck.DiscardPile,
                shuffleCount = data.Deck.ShuffleCount,
                corruptionStage = data.Deck.CorruptionStage,
                lastBuildingId = data.Deck.LastBuildingId,
                pruneCharges = data.Deck.PruneCharges,
            },
            faith = new { value = data.Faith.Value, recentYears = data.Faith.RecentYears },
        });
    }

    /// <param name="data">成功したときの中身。失敗したときは null。</param>
    /// <param name="reason">失敗したときの理由（ログの <c>reason</c>）。成功したときは空文字。</param>
    public static bool TryParse(
        string text, DreamRules dreamRules, ProphetDreamRules rules, CivilizationRules civilizationRules, NeedsRules needsRules,
        out SaveData? data, out string reason) =>
        TryParse(text, dreamRules, rules, civilizationRules, needsRules, null, out data, out reason);

    /// <param name="oracleRules">神託の定義。汚染ルーンの ID と、混ぜた回数の範囲を確かめるのに使う。null なら、汚染ルーンは無い定義として読む。</param>
    public static bool TryParse(
        string text, DreamRules dreamRules, ProphetDreamRules rules, CivilizationRules civilizationRules, NeedsRules needsRules,
        OracleRules? oracleRules, out SaveData? data, out string reason)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(dreamRules);
        ArgumentNullException.ThrowIfNull(rules);
        ArgumentNullException.ThrowIfNull(civilizationRules);
        ArgumentNullException.ThrowIfNull(needsRules);
        data = null;

        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(text);
        }
        catch (JsonException)
        {
            reason = "invalid_json";
            return false;
        }

        using (document)
        {
            try
            {
                data = Read(document.RootElement, dreamRules, rules, civilizationRules, needsRules, oracleRules);
            }
            catch (InvalidSaveDataException ex)
            {
                reason = ex.Reason;
                return false;
            }
        }

        reason = string.Empty;
        return true;
    }

    private static SaveData Read(
        JsonElement root, DreamRules dreamRules, ProphetDreamRules rules, CivilizationRules civilizationRules, NeedsRules needsRules,
        OracleRules? oracleRules)
    {
        if (root.ValueKind != JsonValueKind.Object)
        {
            throw new InvalidSaveDataException("invalid_field");
        }
        if (ReadInt(root, "saveVersion") != SaveVersion)
        {
            throw new InvalidSaveDataException("unsupported_save_version");
        }

        var seed = ReadInt(root, "seed");

        var worldElement = Property(root, "world", JsonValueKind.Object);
        var world = new WorldState
        {
            Year = ReadInt(worldElement, "year"),
            ForestAmount = ReadFloat(worldElement, "forestAmount"),
            WaterAmount = ReadFloat(worldElement, "waterAmount"),
            Temperature = ReadFloat(worldElement, "temperature"),
            ChaosLevel = ReadFloat(worldElement, "chaosLevel"),
            HarmonyLevel = ReadFloat(worldElement, "harmonyLevel"),
            IsDestroyed = ReadBool(worldElement, "isDestroyed"),
        };
        if (world.Year < 0)
        {
            throw new InvalidSaveDataException("invalid_year");
        }

        var symbolIds = dreamRules.Symbols.Select(symbol => symbol.Id).ToHashSet();
        var dogmas = Property(root, "dogmas", JsonValueKind.Array).EnumerateArray()
            .Select(element => ReadDogma(element, symbolIds))
            .ToList();

        string? requestId = null;
        var requestElement = Property(root, "request", null);
        if (requestElement.ValueKind != JsonValueKind.Null)
        {
            requestId = requestElement.ValueKind == JsonValueKind.String ? requestElement.GetString() : null;
            if (requestId is null || !rules.Requests.Any(rule => rule.Id == requestId))
            {
                throw new InvalidSaveDataException("unknown_request_type");
            }
        }

        var answeredYears = new Dictionary<string, int>();
        foreach (var property in Property(root, "answeredYears", JsonValueKind.Object).EnumerateObject())
        {
            if (!rules.Requests.Any(rule => rule.Id == property.Name))
            {
                throw new InvalidSaveDataException("unknown_request_type");
            }
            answeredYears[property.Name] = ReadYearUpTo(property.Value, world.Year);
        }

        var autoPlayElement = Property(root, "autoPlay", JsonValueKind.Object);
        var speedName = ReadString(autoPlayElement, "speed");
        var speed = SpeedNames.FirstOrDefault(pair => pair.Value == speedName);
        if (speed.Value is null)
        {
            throw new InvalidSaveDataException("unknown_speed");
        }
        var elapsedSeconds = ReadDouble(autoPlayElement, "elapsedSeconds");
        if (elapsedSeconds < 0)
        {
            throw new InvalidSaveDataException("invalid_number");
        }

        var civilization = ReadCivilization(Property(root, "civilization", JsonValueKind.Object), civilizationRules, world.Year, symbolIds);
        var residents = ReadResidents(Property(root, "residents", JsonValueKind.Object), needsRules, civilizationRules, world.Year, symbolIds);
        var history = Property(root, "history", JsonValueKind.Array).EnumerateArray()
            .Select(element => ReadHistoryEntry(element, needsRules, symbolIds))
            .ToList();
        var chapterElement = Property(root, "historyChapterStart", JsonValueKind.Object);
        var chapter = new ChapterStart(
            ReadYearUpTo(Property(chapterElement, "year", JsonValueKind.Number), world.Year), ReadChaos(chapterElement, "chaos", "invalid_number"));

        var corruption = oracleRules?.CorruptionOrNone ?? new CorruptionRules([], [], 0f);
        var deck = ReadDeck(Property(root, "deck", JsonValueKind.Object), symbolIds, corruption, oracleRules);
        var faith = ReadFaith(Property(root, "faith", JsonValueKind.Object), world.Year);

        return new SaveData(
            seed, world, dogmas, requestId, answeredYears, speed.Key, elapsedSeconds, civilization, residents, history, chapter, deck, faith);
    }

    // 神託のデッキ。カードは定義にある記号か、汚染ルーン・上位のルーン。シャッフルの回数は 0 以上、混ぜた回数は 0 以上で閾値の数以下
    private static OracleDeckState ReadDeck(JsonElement element, HashSet<string> symbolIds, CorruptionRules corruption, OracleRules? oracleRules)
    {
        List<string> ReadCards(string name) => Property(element, name, JsonValueKind.Array).EnumerateArray()
            .Select(card => card.ValueKind == JsonValueKind.String && card.GetString() is { } id
                && (symbolIds.Contains(id) || (oracleRules?.IsSpecialCard(id) ?? false))
                ? id
                : throw new InvalidSaveDataException("invalid_deck"))
            .ToList();

        var drawPile = ReadCards("drawPile");
        var hand = ReadCards("hand");
        var discardPile = ReadCards("discardPile");
        var shuffleCount = ReadInt(element, "shuffleCount");
        var corruptionStage = ReadInt(element, "corruptionStage");
        var lastBuildingId = ReadInt(element, "lastBuildingId");
        // 精査の持ち分は後から足した項目。無ければ 0（次の世界再創生で付与される）
        var pruneCharges = element.TryGetProperty("pruneCharges", out _) ? ReadInt(element, "pruneCharges") : 0;
        if (shuffleCount < 0 || corruptionStage < 0 || corruptionStage > corruption.Thresholds.Count || lastBuildingId < -1 || pruneCharges < 0)
        {
            throw new InvalidSaveDataException("invalid_deck");
        }
        return new OracleDeckState(drawPile, hand, discardPile, shuffleCount, corruptionStage, lastBuildingId, pruneCharges);
    }

    // 神威（0 以上）と、直近の神託を下した年（0 以上、今の年以下。古い順）
    private static OracleFaithState ReadFaith(JsonElement element, int currentYear)
    {
        var value = ReadFloat(element, "value");
        if (value < 0)
        {
            throw new InvalidSaveDataException("invalid_faith");
        }
        var years = Property(element, "recentYears", JsonValueKind.Array).EnumerateArray()
            .Select(year => ReadYearUpTo(year, currentYear))
            .ToList();
        if (years.Count > MaxRecentOracleYears || years.Zip(years.Skip(1), (a, b) => a > b).Any(descending => descending))
        {
            throw new InvalidSaveDataException("invalid_faith");
        }
        return new OracleFaithState(value, years);
    }

    // 年表の行。世界再創生の前の行は今の年より後になりうるので、年は 0 以上だけを見る。種類ごとに、使う項目があり、使わない項目は null
    private static HistoryEntry ReadHistoryEntry(JsonElement element, NeedsRules needsRules, HashSet<string> symbolIds)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            throw new InvalidSaveDataException("invalid_field");
        }
        var year = ReadInt(element, "year");
        var typeName = ReadString(element, "type");
        var type = HistoryTypeNames.FirstOrDefault(pair => pair.Value == typeName);
        var symbolElement = Property(element, "symbolId", null);
        var dogmaTypeElement = Property(element, "dogmaType", null);
        var nameElement = Property(element, "name", null);
        var epithetElement = Property(element, "epithet", null);
        var breakdownElement = Property(element, "breakdown", null);
        var oracleElement = Property(element, "oracle", null);
        var fromYearElement = Property(element, "fromYear", null);
        var chaosFromElement = Property(element, "chaosFrom", null);
        var chaosToElement = Property(element, "chaosTo", null);
        var mythElement = Property(element, "myth", null);
        if (year < 0 || type.Value is null)
        {
            throw new InvalidSaveDataException("invalid_history");
        }

        string? symbolId = null;
        DogmaType? dogmaType = null;
        if (type.Key == HistoryEventType.DogmaFormed)
        {
            symbolId = symbolElement.ValueKind == JsonValueKind.String ? symbolElement.GetString() : null;
            var dogmaTypeName = dogmaTypeElement.ValueKind == JsonValueKind.String ? dogmaTypeElement.GetString() : null;
            if (symbolId is null || !symbolIds.Contains(symbolId) || dogmaTypeName is null || !Enum.GetNames<DogmaType>().Contains(dogmaTypeName))
            {
                throw new InvalidSaveDataException("invalid_history");
            }
            dogmaType = Enum.Parse<DogmaType>(dogmaTypeName);
        }
        else if (symbolElement.ValueKind != JsonValueKind.Null || dogmaTypeElement.ValueKind != JsonValueKind.Null)
        {
            throw new InvalidSaveDataException("invalid_history");
        }

        ResidentName? name = null;
        ResidentEpithet? epithet = null;
        var aboutResident = type.Key is HistoryEventType.ResidentBrokeDown or HistoryEventType.NotableDied;
        if (aboutResident)
        {
            if (nameElement.ValueKind != JsonValueKind.Object)
            {
                throw new InvalidSaveDataException("invalid_history");
            }
            name = ReadName(nameElement, needsRules);
            epithet = ReadEpithet(epithetElement, symbolIds);
            if (type.Key == HistoryEventType.NotableDied && epithet is null)
            {
                throw new InvalidSaveDataException("invalid_history");
            }
        }
        else if (nameElement.ValueKind != JsonValueKind.Null || epithetElement.ValueKind != JsonValueKind.Null)
        {
            throw new InvalidSaveDataException("invalid_history");
        }

        BreakdownKind? breakdown = null;
        if (type.Key == HistoryEventType.ResidentBrokeDown)
        {
            var breakdownName = breakdownElement.ValueKind == JsonValueKind.String ? breakdownElement.GetString() : null;
            var kind = BreakdownNames.FirstOrDefault(pair => pair.Value == breakdownName);
            breakdown = kind.Value is null ? throw new InvalidSaveDataException("invalid_history") : kind.Key;
        }
        else if (breakdownElement.ValueKind != JsonValueKind.Null)
        {
            throw new InvalidSaveDataException("invalid_history");
        }

        // 神託の文は AI が作った外部入力なので、作ったときと同じ検証をする
        OracleText? oracle = null;
        if (oracleElement.ValueKind != JsonValueKind.Null)
        {
            if (type.Key != HistoryEventType.DogmaFormed || oracleElement.ValueKind != JsonValueKind.Object)
            {
                throw new InvalidSaveDataException("invalid_history");
            }
            oracle = new OracleText(
                ReadString(oracleElement, "heading"), ReadString(oracleElement, "intent"), ReadString(oracleElement, "doctrine"));
            if (!oracle.IsValid())
            {
                throw new InvalidSaveDataException("invalid_history");
            }
        }
        // 章の終わりの項目。始まりは終わりより前の年で、カオス度は 0〜100、神話の文は AI が作った外部入力なので検証する
        int? fromYear = null;
        float? chaosFrom = null;
        float? chaosTo = null;
        MythText? myth = null;
        if (type.Key == HistoryEventType.ChapterEnded)
        {
            fromYear = ReadInt(element, "fromYear");
            if (fromYear < 0 || fromYear >= year)
            {
                throw new InvalidSaveDataException("invalid_history");
            }
            chaosFrom = ReadChaos(element, "chaosFrom", "invalid_history");
            chaosTo = ReadChaos(element, "chaosTo", "invalid_history");
            if (mythElement.ValueKind == JsonValueKind.Object)
            {
                myth = new MythText(ReadString(mythElement, "heading"), ReadString(mythElement, "body"));
                if (!myth.IsValid())
                {
                    throw new InvalidSaveDataException("invalid_history");
                }
            }
            else if (mythElement.ValueKind != JsonValueKind.Null)
            {
                throw new InvalidSaveDataException("invalid_history");
            }
        }
        else if (fromYearElement.ValueKind != JsonValueKind.Null || chaosFromElement.ValueKind != JsonValueKind.Null
            || chaosToElement.ValueKind != JsonValueKind.Null || mythElement.ValueKind != JsonValueKind.Null)
        {
            throw new InvalidSaveDataException("invalid_history");
        }
        return new HistoryEntry(year, type.Key, symbolId, dogmaType, name, epithet, breakdown, oracle, fromYear, chaosFrom, chaosTo, myth);
    }

    private static object WriteName(ResidentName name) => new
    {
        firstPrefix = name.FirstPrefix,
        firstSuffix = name.FirstSuffix,
        lineagePrefix = name.LineagePrefix,
        lineageSuffix = name.LineageSuffix,
    };

    private static object? WriteEpithet(ResidentEpithet? epithet) =>
        epithet is null ? null : new { id = epithet.Id, symbolId = epithet.SymbolId };

    // 地域・値の範囲・年・名前のパーツ・称号を、定義と照らして検証する。ID は住人と墓を通して重ならない
    private static Residents ReadResidents(
        JsonElement element, NeedsRules rules, CivilizationRules civilizationRules, int year, HashSet<string> symbolIds)
    {
        var nextResidentId = ReadInt(element, "nextResidentId");
        if (nextResidentId < 0)
        {
            throw new InvalidSaveDataException("invalid_residents");
        }
        var ids = new HashSet<int>();
        void CheckIdentity(int id, int region)
        {
            if (id < 0 || id >= nextResidentId || !ids.Add(id) || region < 0 || region >= civilizationRules.Regions.Count)
            {
                throw new InvalidSaveDataException("invalid_residents");
            }
        }

        var people = new List<Resident>();
        foreach (var item in Property(element, "people", JsonValueKind.Array).EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object)
            {
                throw new InvalidSaveDataException("invalid_field");
            }
            var id = ReadInt(item, "id");
            var region = ReadInt(item, "region");
            CheckIdentity(id, region);
            var birthYear = ReadInt(item, "birthYear");
            double[] needs =
            [
                ReadDouble(item, "hunger"), ReadDouble(item, "shelter"), ReadDouble(item, "social"), ReadDouble(item, "esteem"),
                ReadDouble(item, "actualization"),
            ];
            var health = ReadDouble(item, "health");
            var sanity = ReadDouble(item, "sanity");
            var breakdownElement = Property(item, "breakdown", null);
            BreakdownKind? breakdown = null;
            if (breakdownElement.ValueKind != JsonValueKind.Null)
            {
                var breakdownName = breakdownElement.ValueKind == JsonValueKind.String ? breakdownElement.GetString() : null;
                var kind = BreakdownNames.FirstOrDefault(pair => pair.Value == breakdownName);
                breakdown = kind.Value is null ? throw new InvalidSaveDataException("invalid_residents") : kind.Key;
            }
            var breakdownYear = ReadInt(item, "breakdownYear");
            var partnerElement = Property(item, "partnerId", null);
            int? partnerId = partnerElement.ValueKind switch
            {
                JsonValueKind.Null => null,
                JsonValueKind.Number when partnerElement.TryGetInt32(out var value) => value,
                _ => throw new InvalidSaveDataException("invalid_field"),
            };
            var housesBuilt = ReadInt(item, "housesBuilt");
            var taskName = ReadString(item, "task");
            var task = TaskNames.FirstOrDefault(pair => pair.Value == taskName);
            if (birthYear > year || needs.Any(value => value is < 0 or > 1) || health <= 0 || health > rules.MaxHealth
                || sanity < 0 || sanity > rules.MaxSanity || (breakdown is not null && (breakdownYear < 0 || breakdownYear > year))
                || task.Value is null || housesBuilt < 0)
            {
                throw new InvalidSaveDataException("invalid_residents");
            }
            people.Add(new Resident(
                id, region, birthYear, needs[0], needs[1], needs[2], needs[3], needs[4], health, sanity, breakdown, breakdownYear, task.Key,
                ReadName(Property(item, "name", JsonValueKind.Object), rules), ReadEpithet(Property(item, "epithet", null), symbolIds),
                partnerId, housesBuilt));
        }
        // つがいの相手は、同じ地域にいる別の住人で、相手のパートナーも自分
        foreach (var person in people.Where(person => person.PartnerId is not null))
        {
            var partner = people.Find(other => other.Id == person.PartnerId);
            if (partner is null || partner.Id == person.Id || partner.Region != person.Region || partner.PartnerId != person.Id)
            {
                throw new InvalidSaveDataException("invalid_residents");
            }
        }

        var graves = new List<Grave>();
        foreach (var item in Property(element, "graves", JsonValueKind.Array).EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object)
            {
                throw new InvalidSaveDataException("invalid_field");
            }
            var id = ReadInt(item, "residentId");
            var region = ReadInt(item, "region");
            CheckIdentity(id, region);
            var deathYear = ReadInt(item, "deathYear");
            if (deathYear < 0 || deathYear > year)
            {
                throw new InvalidSaveDataException("invalid_residents");
            }
            graves.Add(new Grave(
                id, region, deathYear, ReadName(Property(item, "name", JsonValueKind.Object), rules),
                ReadEpithet(Property(item, "epithet", null), symbolIds)));
        }
        return new Residents(people.OrderBy(person => person.Id), graves, nextResidentId);
    }

    private static ResidentName ReadName(JsonElement element, NeedsRules rules)
    {
        string Part(string key, IReadOnlyList<string> parts)
        {
            var value = ReadString(element, key);
            return parts.Contains(value) ? value : throw new InvalidSaveDataException("invalid_residents");
        }
        return new ResidentName(
            Part("firstPrefix", rules.NamePrefixes), Part("firstSuffix", rules.NameSuffixes),
            Part("lineagePrefix", rules.NamePrefixes), Part("lineageSuffix", rules.NameSuffixes));
    }

    // 証人の称号だけが記号を持つ
    private static ResidentEpithet? ReadEpithet(JsonElement element, HashSet<string> symbolIds)
    {
        if (element.ValueKind == JsonValueKind.Null)
        {
            return null;
        }
        if (element.ValueKind != JsonValueKind.Object)
        {
            throw new InvalidSaveDataException("invalid_field");
        }
        var id = ReadString(element, "id");
        var symbolElement = Property(element, "symbolId", null);
        var symbolId = symbolElement.ValueKind == JsonValueKind.String ? symbolElement.GetString() : null;
        var valid = EpithetIds.Contains(id) && (id == Epithets.Witness
            ? symbolId is not null && symbolIds.Contains(symbolId)
            : symbolElement.ValueKind == JsonValueKind.Null);
        return valid ? new ResidentEpithet(id, symbolId) : throw new InvalidSaveDataException("invalid_residents");
    }

    // 地域の数・値の範囲・建物・廃墟・遺物の場所（地域の中の 2 × 2 の枠）と ID を、定義と照らして検証する。
    // ID は建物・廃墟・遺物を通して重ならない（同じ建物が、建物・廃墟・遺物のどれか 1 つとしてだけある）
    private static Civilization ReadCivilization(JsonElement element, CivilizationRules rules, int year, HashSet<string> symbolIds)
    {
        var regions = Property(element, "regions", JsonValueKind.Array).EnumerateArray()
            .Select(region => ReadRegion(region, rules))
            .ToList();
        if (regions.Count != rules.Regions.Count)
        {
            throw new InvalidSaveDataException("invalid_civilization");
        }

        var nextBuildingId = ReadInt(element, "nextBuildingId");
        var ids = new HashSet<int>();

        (int Id, BuildingType Type, int Region, int Column, int Row) ReadPlaced(JsonElement placed)
        {
            if (placed.ValueKind != JsonValueKind.Object)
            {
                throw new InvalidSaveDataException("invalid_field");
            }
            var id = ReadInt(placed, "id");
            var typeName = ReadString(placed, "type");
            var type = BuildingTypeNames.FirstOrDefault(pair => pair.Value == typeName);
            var region = ReadInt(placed, "region");
            var column = ReadInt(placed, "column");
            var row = ReadInt(placed, "row");
            if (type.Value is null || id < 0 || id >= nextBuildingId || !ids.Add(id)
                || region < 0 || region >= rules.Regions.Count || !IsSlot(column, row, region, rules))
            {
                throw new InvalidSaveDataException("invalid_civilization");
            }
            return (id, type.Key, region, column, row);
        }

        var buildingSlots = new HashSet<(int Column, int Row)>();
        var buildings = new List<Building>();
        foreach (var item in Property(element, "buildings", JsonValueKind.Array).EnumerateArray())
        {
            var (id, type, region, column, row) = ReadPlaced(item);
            var collapseYear = ReadInt(item, "collapseYear");
            if (!buildingSlots.Add((column, row)) || collapseYear < 0)
            {
                throw new InvalidSaveDataException("invalid_civilization");
            }
            buildings.Add(new Building(id, type, region, column, row, collapseYear));
        }

        var ruinSlots = new HashSet<(int Column, int Row)>();
        var ruins = new List<Ruin>();
        foreach (var item in Property(element, "ruins", JsonValueKind.Array).EnumerateArray())
        {
            var (id, type, region, column, row) = ReadPlaced(item);
            var ruinedYear = ReadInt(item, "ruinedYear");
            if (!ruinSlots.Add((column, row)) || ruinedYear < 0 || ruinedYear > year)
            {
                throw new InvalidSaveDataException("invalid_civilization");
            }
            ruins.Add(new Ruin(id, type, region, column, row, ruinedYear));
        }

        // 世界再創生の後は年が 0 に戻るので、埋もれた年は今の年より大きくてよい
        var relicSlots = new HashSet<(int Column, int Row, int Depth)>();
        var relics = new List<Relic>();
        foreach (var item in Property(element, "relics", JsonValueKind.Array).EnumerateArray())
        {
            var (id, type, region, column, row) = ReadPlaced(item);
            var depth = ReadInt(item, "depth");
            var buriedYear = ReadInt(item, "buriedYear");
            var tech = ReadDouble(item, "tech");
            var culture = ReadDouble(item, "culture");
            var chaos = ReadDouble(item, "chaos");
            var dogmas = Property(item, "dogmas", JsonValueKind.Array).EnumerateArray()
                .Select(dogma => ReadDogma(dogma, symbolIds))
                .ToList();
            if (depth < 1 || depth > rules.Strata.MaxDepth || !relicSlots.Add((column, row, depth)) || buriedYear < 0
                || tech is < 0 or > 1 || culture is < 0 or > 1 || chaos is < 0 or > 1)
            {
                throw new InvalidSaveDataException("invalid_civilization");
            }
            relics.Add(new Relic(id, type, region, column, row, depth, buriedYear, tech, culture, chaos, dogmas));
        }
        return new Civilization(regions, buildings, ruins, relics, nextBuildingId);
    }

    private static RegionCivilization ReadRegion(JsonElement element, CivilizationRules rules)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            throw new InvalidSaveDataException("invalid_field");
        }
        var tech = ReadDouble(element, "tech");
        var culture = ReadDouble(element, "culture");
        var techStage = ReadInt(element, "techStage");
        var cultureStage = ReadInt(element, "cultureStage");
        if (tech is < 0 or > 1 || culture is < 0 or > 1
            || techStage < 0 || techStage > rules.Thresholds.Count || cultureStage < 0 || cultureStage > rules.Thresholds.Count)
        {
            throw new InvalidSaveDataException("invalid_civilization");
        }
        return new RegionCivilization(tech, culture, techStage, cultureStage);
    }

    // 地域の中の、2 × 2 の枠の左上のマス
    private static bool IsSlot(int column, int row, int region, CivilizationRules rules)
    {
        var offset = column - rules.Regions.FirstColumn(region);
        return offset >= 0 && offset + 1 < rules.Regions.ColumnsPerRegion && offset % 2 == 0
            && row >= 0 && row + 1 < rules.Rows && row % 2 == 0;
    }

    private static Dogma ReadDogma(JsonElement element, HashSet<string> symbolIds)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            throw new InvalidSaveDataException("invalid_field");
        }
        var symbolId = ReadString(element, "targetSymbolId");
        if (!symbolIds.Contains(symbolId))
        {
            throw new InvalidSaveDataException("unknown_symbol");
        }
        // 数字の文字列（"0" など）も Enum.TryParse は受けるので、名前と完全に一致するものだけにする
        var typeName = ReadString(element, "dogmaType");
        if (!Enum.GetNames<DogmaType>().Contains(typeName))
        {
            throw new InvalidSaveDataException("unknown_dogma_type");
        }
        return new Dogma(symbolId, Enum.Parse<DogmaType>(typeName), ReadFloat(element, "intensity"));
    }

    // 0 以上、今の年以下の年
    private static int ReadYearUpTo(JsonElement element, int currentYear)
    {
        if (element.ValueKind != JsonValueKind.Number || !element.TryGetInt32(out var year))
        {
            throw new InvalidSaveDataException("invalid_field");
        }
        if (year < 0 || year > currentYear)
        {
            throw new InvalidSaveDataException("invalid_year");
        }
        return year;
    }

    /// <param name="kind">期待する型。null なら型を問わない。</param>
    private static JsonElement Property(JsonElement parent, string name, JsonValueKind? kind)
    {
        if (!parent.TryGetProperty(name, out var value) || (kind is { } expected && value.ValueKind != expected))
        {
            throw new InvalidSaveDataException("invalid_field");
        }
        return value;
    }

    // カオス度は 0 以上 100 以下（HUD と同じ百分率）
    private static float ReadChaos(JsonElement parent, string name, string reason)
    {
        var value = ReadFloat(parent, name);
        return value is < 0 or > 100 ? throw new InvalidSaveDataException(reason) : value;
    }

    private static int ReadInt(JsonElement parent, string name) =>
        Property(parent, name, JsonValueKind.Number).TryGetInt32(out var value)
            ? value
            : throw new InvalidSaveDataException("invalid_field");

    private static float ReadFloat(JsonElement parent, string name)
    {
        // JSON の数値は NaN・無限を書けないが、float に収まらない大きさは無限になる
        if (!Property(parent, name, JsonValueKind.Number).TryGetSingle(out var value) || !float.IsFinite(value))
        {
            throw new InvalidSaveDataException("invalid_number");
        }
        return value;
    }

    private static double ReadDouble(JsonElement parent, string name)
    {
        if (!Property(parent, name, JsonValueKind.Number).TryGetDouble(out var value) || !double.IsFinite(value))
        {
            throw new InvalidSaveDataException("invalid_number");
        }
        return value;
    }

    private static bool ReadBool(JsonElement parent, string name)
    {
        var value = Property(parent, name, null);
        return value.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            _ => throw new InvalidSaveDataException("invalid_field"),
        };
    }

    private static string ReadString(JsonElement parent, string name) =>
        Property(parent, name, JsonValueKind.String).GetString() ?? throw new InvalidSaveDataException("invalid_field");

    private sealed class InvalidSaveDataException(string reason) : Exception(reason)
    {
        public string Reason { get; } = reason;
    }
}
