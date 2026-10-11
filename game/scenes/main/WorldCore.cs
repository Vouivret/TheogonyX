using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Godot;
using TheogonyX.Core;
using TheogonyX.Data;

namespace TheogonyX.Scenes.Main;

/// <summary>メインシーンの根。ゲームデータを読み、初期のドグマを作り、画面（<see cref="WorldScreen"/>）を組み立てる。</summary>
public partial class WorldCore : Node2D
{
    private WorldScreen? _screen;
    private AmbienceView? _ambience;

    public override void _Ready()
    {
        var rulesJson = ReadText("res://data/rules.json");
        var dreamRules = GameDataLoader.ParseDreamRules(ReadText("res://data/symbols.json"), rulesJson);
        var worldRules = GameDataLoader.ParseWorldRules(rulesJson);
        var (terrainRules, mapRows) = GameDataLoader.ParseTerrain(rulesJson);
        var timeSettings = GameDataLoader.ParseTimeControl(rulesJson);
        var ambienceRules = GameDataLoader.ParseAmbience(rulesJson);
        var prophetSettings = GameDataLoader.ParseProphet(rulesJson);

        var timeControl = new TimeControl(worldRules, CreateInitialDogmas(dreamRules), timeSettings.JumpYears);
        _screen = new WorldScreen(this, timeControl, terrainRules, mapRows, timeSettings);
        _ambience = new AmbienceView(this, timeControl, ambienceRules, prophetSettings);
    }

    public override void _Process(double delta)
    {
        _ambience?.Process(delta);
    }

    // 初期の夢の改ざん。夢を改ざんする画面ができるまでの仮の仕組み（docs/domain/time-control.md）。
    private static List<Dogma> CreateInitialDogmas(DreamRules dreamRules)
    {
        var dream = new List<DreamNode> { new("SYM_TREE", 0.5f), new("SYM_RAIN", 0.6f) };
        GD.Print(FormattableString.Invariant($"event=dream_created dreamNodes={Format(dream)}"));

        dream[1] = new DreamNode("SYM_BLOOD", 0.9f);
        GD.Print(FormattableString.Invariant($"event=dream_tampered dreamNodes={Format(dream)}"));

        var dogma = DreamInterpreter.Interpret(dream, dreamRules);
        if (dogma is null)
        {
            return [];
        }
        GD.Print(FormattableString.Invariant(
            $"event=dogma_formed year=0 symbolId={dogma.TargetSymbolId} dogmaType={dogma.Type} intensity={dogma.Intensity}"));
        return [dogma];
    }

    private static string ReadText(string path)
    {
        var text = Godot.FileAccess.GetFileAsString(path);
        var error = Godot.FileAccess.GetOpenError();
        if (error != Error.Ok)
        {
            throw new IOException($"ゲームデータを開けない: path={path} error={error}");
        }
        return text;
    }

    private static string Format(IEnumerable<DreamNode> dream) =>
        string.Join(",", dream.Select(node => FormattableString.Invariant($"{node.SymbolId}:{node.Weight}")));
}
