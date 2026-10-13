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

    public static TerrainRules ParseTerrain(string rulesJson)
    {
        var terrain = Parse<RulesFile>(rulesJson, "rules.json").Terrain;
        try
        {
            return new TerrainRules(terrain.Columns, terrain.Rows, terrain.Anomalies,
                terrain.Lake?.Cells ?? throw new InvalidDataException("湖の定義が無い: file=rules.json key=terrain.lake.cells"));
        }
        catch (ArgumentException ex)
        {
            throw new InvalidDataException("地表の定義の値が範囲外: file=rules.json", ex);
        }
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

    public static CrowdRules ParseCrowd(string rulesJson)
    {
        var crowd = Parse<RulesFile>(rulesJson, "rules.json").Crowd
            ?? throw new InvalidDataException("群衆の定義が無い: file=rules.json key=crowd");
        float[] positives = [crowd.WalkSpeed, crowd.TurnIntervalSeconds, crowd.FrenzySpeed, crowd.HolySiteRadius, crowd.FastTimeScale];
        if (crowd.Count <= 0 || positives.Any(value => !float.IsFinite(value) || value <= 0)
            || !float.IsFinite(crowd.FrenzyChaosAtOrAbove) || !float.IsFinite(crowd.FieldScale)
            || crowd.Flock is null || !float.IsFinite(crowd.Flock.CellSize) || crowd.Flock.CellSize <= 0
            || !float.IsFinite(crowd.Flock.AlignWeight) || !float.IsFinite(crowd.Flock.CrowdWeight)
            || !float.IsFinite(crowd.Flock.TargetNeighbors) || crowd.Flock.TargetNeighbors <= 0
            || crowd.TileForces is null || Enum.GetValues<TileType>().Any(tileType => !crowd.TileForces.ContainsKey(tileType))
            || crowd.TileForces.Values.Any(force => !float.IsFinite(force)))
        {
            throw new InvalidDataException($"群衆の定義の値が範囲外か、タイル種別の力が足りない: file=rules.json value={crowd}");
        }
        return new CrowdRules(
            crowd.Count, crowd.WalkSpeed, crowd.TurnIntervalSeconds, crowd.FrenzySpeed, crowd.HolySiteRadius,
            crowd.FrenzyChaosAtOrAbove, crowd.FieldScale, crowd.FastTimeScale, crowd.Flock!, crowd.TileForces);
    }

    public static CrowdLook ParseCrowdLook(string rulesJson)
    {
        var crowd = Parse<RulesFile>(rulesJson, "rules.json").Crowd
            ?? throw new InvalidDataException("群衆の定義が無い: file=rules.json key=crowd");
        if (crowd.DotSizePixels is < 1 or > 2)
        {
            throw new InvalidDataException($"群衆の点の大きさは 1〜2 px: file=rules.json value={crowd.DotSizePixels}");
        }
        var color = ParseColor(crowd.DotColor);
        return new CrowdLook(crowd.DotSizePixels, color, crowd.UseGpu);
    }

    public static ProphetSettings ParseProphet(string rulesJson) => Parse<RulesFile>(rulesJson, "rules.json").Prophet;

    public static CampfireSettings ParseCampfire(string rulesJson)
    {
        var rules = Parse<RulesFile>(rulesJson, "rules.json");
        var campfire = rules.Campfire ?? throw new InvalidDataException("焚き火の定義が無い: file=rules.json key=campfire");
        if (campfire.Column < 1 || campfire.Column > rules.Terrain.Columns
            || !float.IsFinite(campfire.Energy) || campfire.Energy <= 0
            || !float.IsFinite(campfire.RadiusTiles) || campfire.RadiusTiles <= 0
            || !float.IsFinite(campfire.FlickerAmount) || campfire.FlickerAmount is < 0 or > 1
            || !float.IsFinite(campfire.FlickerSpeed) || campfire.FlickerSpeed < 0)
        {
            throw new InvalidDataException($"焚き火の定義の値が範囲外: file=rules.json value={campfire}");
        }
        return new CampfireSettings(
            campfire.Column, ParseColor(campfire.Color), campfire.Energy, campfire.RadiusTiles, campfire.FlickerAmount, campfire.FlickerSpeed);
    }

    public static LightingSettings ParseLighting(string rulesJson)
    {
        var lighting = Parse<RulesFile>(rulesJson, "rules.json").Lighting
            ?? throw new InvalidDataException("光と影の定義が無い: file=rules.json key=lighting");
        if (!float.IsFinite(lighting.DirectionDegrees)
            || !float.IsFinite(lighting.Height) || lighting.Height is < 0 or > 1
            || !float.IsFinite(lighting.Energy) || lighting.Energy < 0
            || !float.IsFinite(lighting.ShadowOpacity) || lighting.ShadowOpacity is < 0 or > 1
            || !float.IsFinite(lighting.ShadowWidthRatio) || lighting.ShadowWidthRatio is <= 0 or > 1
            || lighting.ShadowHeight < 1)
        {
            throw new InvalidDataException($"光と影の定義の値が範囲外: file=rules.json value={lighting}");
        }
        return lighting;
    }

    public static CameraSettings ParseCamera(string rulesJson)
    {
        var camera = Parse<RulesFile>(rulesJson, "rules.json").Camera
            ?? throw new InvalidDataException("カメラの定義が無い: file=rules.json key=camera");
        var steps = camera.ZoomSteps ?? [];
        var stepsValid = steps.Count > 0;
        for (var i = 0; i < steps.Count && stepsValid; i++)
        {
            // 1.0 より大きい段は、ドットの幅がそろう整数倍だけ（docs/domain/camera-views.md）
            stepsValid = double.IsFinite(steps[i]) && steps[i] > 0
                && (i == 0 || steps[i] > steps[i - 1])
                && (steps[i] <= 1 || steps[i] == Math.Floor(steps[i]));
        }
        if (!stepsValid || !steps.Contains(camera.StartZoom) || !double.IsFinite(camera.ZoomSmoothing) || camera.ZoomSmoothing <= 0
            || !double.IsFinite(camera.PanSpeed) || camera.PanSpeed <= 0)
        {
            throw new InvalidDataException(
                $"カメラの定義の値が範囲外: file=rules.json zoomSteps={string.Join(',', steps)} startZoom={camera.StartZoom} zoomSmoothing={camera.ZoomSmoothing} panSpeed={camera.PanSpeed}");
        }
        return new CameraSettings(steps, camera.StartZoom, camera.ZoomSmoothing, camera.PanSpeed);
    }

    public static CivilizationRules ParseCivilization(string rulesJson)
    {
        var rules = Parse<RulesFile>(rulesJson, "rules.json");
        var civilization = rules.Civilization
            ?? throw new InvalidDataException("文明の定義が無い: file=rules.json key=civilization");
        var strata = rules.Strata
            ?? throw new InvalidDataException("地層の定義が無い: file=rules.json key=strata");
        try
        {
            return new CivilizationRules(
                new RegionMap(rules.Terrain.Columns, civilization.ColumnsPerRegion),
                rules.Terrain.Rows,
                rules.Prophet.Column - 1, // rules.json の列は 1 から数える
                civilization.InitialTech,
                civilization.InitialCulture,
                civilization.NaturalGrowthPerYear,
                civilization.WorshipCulturePerYear,
                civilization.DestructionTechPerYear,
                civilization.TabooGrowthFactor,
                civilization.ZeroInfluenceDistance,
                civilization.EnvironmentMultipliers ?? [],
                civilization.Thresholds ?? [],
                new StrataRules(strata.LifespanMin, strata.LifespanMax, strata.RuinBurialYears, strata.MaxDepth));
        }
        catch (ArgumentException ex)
        {
            throw new InvalidDataException($"文明・地層の定義の値が範囲外: file=rules.json civilization={civilization} strata={strata}", ex);
        }
    }

    public static ProphetDreamRules ParseProphetDreams(string rulesJson)
    {
        var rules = Parse<RulesFile>(rulesJson, "rules.json");
        var dream = rules.Dream;
        var requests = rules.ProphetRequests;
        return new ProphetDreamRules(
            dream.NodeCount, dream.MinWeight, dream.MaxWeight, dream.YearsBetweenDreams, requests.CooldownYears, requests.Requests);
    }

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

    public static SaveSettings ParseSave(string saveJson)
    {
        var settings = Parse<SaveSettings>(saveJson, "save.json");
        if (!double.IsFinite(settings.AutoSaveIntervalSeconds) || settings.AutoSaveIntervalSeconds <= 0)
        {
            throw new InvalidDataException($"セーブの設定の値が範囲外: file=save.json value={settings}");
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

    private sealed record LakeFile(IReadOnlyList<TerrainCell> Cells);

    private sealed record TerrainFile(int Columns, int Rows, IReadOnlyList<TerrainAnomaly> Anomalies, LakeFile Lake);

    private sealed record AmbienceFile(float CycleSeconds, IReadOnlyList<string> CycleColors, string ChaosColor, string DestroyedColor);

    private sealed record CampfireFile(int Column, string Color, float Energy, float RadiusTiles, float FlickerAmount, float FlickerSpeed);

    private sealed record CameraFile(IReadOnlyList<double>? ZoomSteps, double StartZoom, double ZoomSmoothing, double PanSpeed);

    private sealed record CivilizationFile(
        int ColumnsPerRegion,
        double InitialTech,
        double InitialCulture,
        double NaturalGrowthPerYear,
        double WorshipCulturePerYear,
        double DestructionTechPerYear,
        double TabooGrowthFactor,
        int ZeroInfluenceDistance,
        Dictionary<TileType, double>? EnvironmentMultipliers,
        IReadOnlyList<double>? Thresholds);

    private sealed record StrataFile(int LifespanMin, int LifespanMax, int RuinBurialYears, int MaxDepth);

    private sealed record CrowdFile(
        int Count,
        float WalkSpeed,
        float TurnIntervalSeconds,
        float FrenzySpeed,
        float HolySiteRadius,
        float FrenzyChaosAtOrAbove,
        float FieldScale,
        float FastTimeScale,
        FlockRules? Flock,
        Dictionary<TileType, float>? TileForces,
        int DotSizePixels,
        string DotColor,
        bool UseGpu);

    private sealed record DreamFile(int NodeCount, float MinWeight, float MaxWeight, int YearsBetweenDreams);

    private sealed record ProphetRequestsFile(int CooldownYears, IReadOnlyList<ProphetRequestRule> Requests);

    private sealed record RulesFile(
        DogmaTypeThresholds DogmaTypeThresholds,
        WorldRules World,
        TerrainFile Terrain,
        TimeControlSettings TimeControl,
        AmbienceFile Ambience,
        ProphetSettings Prophet,
        CampfireFile? Campfire,
        LightingSettings? Lighting,
        CivilizationFile? Civilization,
        StrataFile? Strata,
        CameraFile? Camera,
        CrowdFile? Crowd,
        DreamFile Dream,
        ProphetRequestsFile ProphetRequests);
}
