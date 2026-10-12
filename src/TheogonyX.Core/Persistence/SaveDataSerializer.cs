using System.Text.Json;

namespace TheogonyX.Core.Persistence;

/// <summary>
/// セーブデータを JSON に書き出し、読み込む（docs/domain/save-data.md）。
/// 読み込んだファイルは利用者が書き換えうる外部入力として扱い、形・値・定義との整合を検証する。
/// </summary>
public static class SaveDataSerializer
{
    /// <summary>今の形式のバージョン。形を変えたら上げる（.agent/rules/save-data.md）。</summary>
    public const int SaveVersion = 2;

    /// <summary>受け付ける「見た夢の数」の上限。読み込み時に夢を作り直すので、書き換えられた大きな値で止まらないようにする。</summary>
    public const int MaxDreamsGenerated = 1_000_000;

    private static readonly Dictionary<ProphetDreamState, string> DreamStateNames = new()
    {
        [ProphetDreamState.None] = "NONE",
        [ProphetDreamState.Untampered] = "UNTAMPERED",
        [ProphetDreamState.Tampered] = "TAMPERED",
    };

    private static readonly Dictionary<AutoPlaySpeed, string> SpeedNames = new()
    {
        [AutoPlaySpeed.Normal] = "NORMAL",
        [AutoPlaySpeed.Fast] = "FAST",
        [AutoPlaySpeed.RealTime] = "REAL_TIME",
    };

    public static string Write(SaveData data)
    {
        ArgumentNullException.ThrowIfNull(data);
        var world = data.World;
        return JsonSerializer.Serialize(new
        {
            saveVersion = SaveVersion,
            seed = data.Seed,
            dreamsGenerated = data.DreamsGenerated,
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
            dream = new
            {
                state = DreamStateNames[data.DreamState],
                nodes = data.Dream.Select(node => new { symbolId = node.SymbolId, weight = node.Weight }),
            },
            request = data.RequestId,
            answeredYears = data.AnsweredYears,
            lastOracleYear = data.LastOracleYear,
            autoPlay = new { speed = SpeedNames[data.Speed], elapsedSeconds = data.ElapsedSeconds },
        });
    }

    /// <param name="data">成功したときの中身。失敗したときは null。</param>
    /// <param name="reason">失敗したときの理由（ログの <c>reason</c>）。成功したときは空文字。</param>
    public static bool TryParse(string text, DreamRules dreamRules, ProphetDreamRules rules, out SaveData? data, out string reason)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(dreamRules);
        ArgumentNullException.ThrowIfNull(rules);
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
                data = Read(document.RootElement, dreamRules, rules);
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

    private static SaveData Read(JsonElement root, DreamRules dreamRules, ProphetDreamRules rules)
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
        var dreamsGenerated = ReadInt(root, "dreamsGenerated");
        if (dreamsGenerated is < 0 or > MaxDreamsGenerated)
        {
            throw new InvalidSaveDataException("invalid_dreams_generated");
        }

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

        var dreamElement = Property(root, "dream", JsonValueKind.Object);
        var stateName = ReadString(dreamElement, "state");
        var dreamState = DreamStateNames.FirstOrDefault(pair => pair.Value == stateName);
        if (dreamState.Value is null)
        {
            throw new InvalidSaveDataException("invalid_dream");
        }
        var dream = Property(dreamElement, "nodes", JsonValueKind.Array).EnumerateArray()
            .Select(element => ReadDreamNode(element, symbolIds, rules))
            .ToList();
        var expectedNodes = dreamState.Key == ProphetDreamState.None ? 0 : rules.NodeCount;
        if (dream.Count != expectedNodes)
        {
            throw new InvalidSaveDataException("invalid_dream");
        }

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
        var lastOracleYear = ReadYearUpTo(Property(root, "lastOracleYear", JsonValueKind.Number), world.Year);

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

        return new SaveData(
            seed, dreamsGenerated, world, dogmas, dreamState.Key, dream, requestId, answeredYears, lastOracleYear, speed.Key, elapsedSeconds);
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

    private static DreamNode ReadDreamNode(JsonElement element, HashSet<string> symbolIds, ProphetDreamRules rules)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            throw new InvalidSaveDataException("invalid_field");
        }
        var symbolId = ReadString(element, "symbolId");
        if (!symbolIds.Contains(symbolId))
        {
            throw new InvalidSaveDataException("unknown_symbol");
        }
        var weight = ReadFloat(element, "weight");
        if (weight < rules.MinWeight || weight > rules.MaxWeight)
        {
            throw new InvalidSaveDataException("invalid_dream");
        }
        return new DreamNode(symbolId, weight);
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
