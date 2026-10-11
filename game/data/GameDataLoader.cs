using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
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

    public static AmbienceRules ParseAmbience(string rulesJson)
    {
        var ambience = Parse<RulesFile>(rulesJson, "rules.json").Ambience;
        return new AmbienceRules(
            ambience.CycleSeconds,
            ambience.CycleColors.Select(ParseColor).ToList(),
            ParseColor(ambience.ChaosColor),
            ParseColor(ambience.DestroyedColor));
    }

    public static ProphetSettings ParseProphet(string rulesJson) => Parse<RulesFile>(rulesJson, "rules.json").Prophet;

    public static CompanionSettings ParseCompanion(string companionJson)
    {
        var settings = Parse<CompanionSettings>(companionJson, "companion.json");
        if (!IsPort(settings.HttpPort) || !IsPort(settings.WebSocketPort) || settings.HttpPort == settings.WebSocketPort
            || settings.PairingTimeoutSeconds <= 0 || settings.MaxUnpairedConnections <= 0 || settings.MaxStreams <= 0 || settings.FramesPerSecond <= 0
            || settings.JpegQualityPercent is < 1 or > 100)
        {
            throw new InvalidDataException($"コンパニオンの設定の値が範囲外: file=companion.json value={settings}");
        }
        return settings;
    }

    private static bool IsPort(int port) => port is >= 1 and <= ushort.MaxValue;

    /// <param name="hex"><c>#RRGGBB</c> の形の色。</param>
    private static LightColor ParseColor(string hex)
    {
        if (hex.Length != 7 || hex[0] != '#'
            || !int.TryParse(hex.AsSpan(1), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var rgb))
        {
            throw new InvalidDataException($"色の形が #RRGGBB ではない: file=rules.json value={hex}");
        }
        return new LightColor(((rgb >> 16) & 0xFF) / 255f, ((rgb >> 8) & 0xFF) / 255f, (rgb & 0xFF) / 255f);
    }

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

    private sealed record AmbienceFile(float CycleSeconds, IReadOnlyList<string> CycleColors, string ChaosColor, string DestroyedColor);

    private sealed record RulesFile(
        DogmaTypeThresholds DogmaTypeThresholds,
        WorldRules World,
        TerrainFile Terrain,
        TimeControlSettings TimeControl,
        AmbienceFile Ambience,
        ProphetSettings Prophet);
}
