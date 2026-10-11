using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Godot;
using TheogonyX.Core;
using TheogonyX.Data;

namespace TheogonyX.Scenes.Main;

/// <summary>MVP のコアループ（夢の改ざん → ドグマ生成 → 時代跳躍）を通しで動かし、結果をログに出す。</summary>
public partial class WorldCore : Node
{
    private const int TimeSkipYears = 100;

    public override void _Ready()
    {
        var rulesJson = ReadText("res://data/rules.json");
        var dreamRules = GameDataLoader.ParseDreamRules(ReadText("res://data/symbols.json"), rulesJson);
        var worldRules = GameDataLoader.ParseWorldRules(rulesJson);

        var dream = new List<DreamNode> { new("SYM_TREE", 0.5f), new("SYM_RAIN", 0.6f) };
        GD.Print(FormattableString.Invariant($"event=dream_created dreamNodes={Format(dream)}"));

        dream[1] = new DreamNode("SYM_BLOOD", 0.9f);
        GD.Print(FormattableString.Invariant($"event=dream_tampered dreamNodes={Format(dream)}"));

        var world = WorldState.CreateInitial(worldRules);
        var dogmas = new List<Dogma>();
        var dogma = DreamInterpreter.Interpret(dream, dreamRules);
        if (dogma is not null)
        {
            dogmas.Add(dogma);
            GD.Print(FormattableString.Invariant(
                $"event=dogma_formed year={world.Year} symbolId={dogma.TargetSymbolId} dogmaType={dogma.Type} intensity={dogma.Intensity}"));
        }

        WorldSimulator.SimulateTimeSkip(world, dogmas, TimeSkipYears, worldRules);
        GD.Print(FormattableString.Invariant(
            $"event=time_skipped years={TimeSkipYears} year={world.Year} forestAmount={world.ForestAmount} waterAmount={world.WaterAmount} temperature={world.Temperature} chaosLevel={world.ChaosLevel} harmonyLevel={world.HarmonyLevel} isDestroyed={world.IsDestroyed}"));
        if (world.IsDestroyed)
        {
            GD.Print(FormattableString.Invariant($"event=world_destroyed year={world.Year}"));
        }
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
