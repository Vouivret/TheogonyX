using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using TheogonyX.Core;

namespace TheogonyX.Data;

/// <summary>game/data/ の JSON から、コアに渡すゲームデータ定義を作る。</summary>
public static class GameDataLoader
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    public static DreamRules ParseDreamRules(string symbolsJson, string rulesJson)
    {
        var symbols = Parse<SymbolsFile>(symbolsJson, "symbols.json");
        var rules = Parse<RulesFile>(rulesJson, "rules.json");
        return new DreamRules(symbols.Symbols, rules.DogmaTypeThresholds.TabooAtOrBelow, rules.DogmaTypeThresholds.WorshipAtOrAbove);
    }

    public static WorldRules ParseWorldRules(string rulesJson) => Parse<RulesFile>(rulesJson, "rules.json").World;

    /// <returns>判定の定義と、描画するマップの行数。</returns>
    public static (TerrainRules Rules, int Rows) ParseTerrain(string rulesJson)
    {
        var terrain = Parse<RulesFile>(rulesJson, "rules.json").Terrain;
        return (new TerrainRules(terrain.Columns, terrain.Anomalies), terrain.Rows);
    }

    public static TimeControlSettings ParseTimeControl(string rulesJson) => Parse<RulesFile>(rulesJson, "rules.json").TimeControl;

    private static T Parse<T>(string json, string fileName)
    {
        try
        {
            return JsonSerializer.Deserialize<T>(json, Options)
                ?? throw new InvalidDataException($"ゲームデータが空: file={fileName}");
        }
        catch (JsonException ex)
        {
            throw new InvalidDataException($"ゲームデータを読めない: file={fileName}", ex);
        }
    }

    private sealed record SymbolsFile(IReadOnlyList<Symbol> Symbols);

    private sealed record DogmaTypeThresholds(float TabooAtOrBelow, float WorshipAtOrAbove);

    private sealed record TerrainFile(int Columns, int Rows, IReadOnlyList<TerrainAnomaly> Anomalies);

    private sealed record RulesFile(
        DogmaTypeThresholds DogmaTypeThresholds,
        WorldRules World,
        TerrainFile Terrain,
        TimeControlSettings TimeControl);
}
