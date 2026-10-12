using System;
using System.IO;
using Godot;
using TheogonyX.Core;
using TheogonyX.Data;

namespace TheogonyX.Scenes.Main;

/// <summary>
/// メインシーンの根。ゲームデータを読み、画面（<see cref="WorldScreen"/>）・予言者の夢（<see cref="DreamView"/>）・
/// コンパニオンとの通信（<see cref="CompanionHost"/>）を組み立てる。
/// </summary>
public partial class WorldCore : Node2D
{
    private WorldScreen? _screen;
    private AmbienceView? _ambience;
    private DreamView? _dreams;
    private CompanionHost? _companion;

    public override void _Ready()
    {
        var rulesJson = ReadText("res://data/rules.json");
        var dreamRules = GameDataLoader.ParseDreamRules(ReadText("res://data/symbols.json"), rulesJson);
        var worldRules = GameDataLoader.ParseWorldRules(rulesJson);
        var (terrainRules, mapRows) = GameDataLoader.ParseTerrain(rulesJson);
        var timeSettings = GameDataLoader.ParseTimeControl(rulesJson);
        var ambienceRules = GameDataLoader.ParseAmbience(rulesJson);
        var prophetSettings = GameDataLoader.ParseProphet(rulesJson);
        var prophetDreamRules = GameDataLoader.ParseProphetDreams(rulesJson);

        // シードは起動時の時刻から作り、記録する（同じシードで同じ夢を再現できる）
        var seed = (int)(TimeProvider.System.GetUtcNow().ToUnixTimeMilliseconds() & int.MaxValue);
        GD.Print($"event=game_started seed={seed}");
        var random = new Random(seed);

        var timeControl = new TimeControl(worldRules, timeSettings.JumpYears);
        var prophet = new ProphetDreams(
            timeControl, dreamRules, prophetDreamRules, () => DreamGenerator.Generate(random, dreamRules, prophetDreamRules));
        _screen = new WorldScreen(this, timeControl, terrainRules, mapRows, timeSettings);
        _ambience = new AmbienceView(this, timeControl, ambienceRules, prophetSettings);
        _dreams = new DreamView(this, prophet, timeControl, dreamRules);
        _screen.TimeAdvanced += _dreams.OnTimeAdvanced;
        _screen.WorldReset += _dreams.OnWorldReset;
        _dreams.OraclePronounced += _screen.Refresh;

        var companionSettings = GameDataLoader.ParseCompanion(ReadText("res://data/companion.json"));
        _companion = new CompanionHost(this, companionSettings, CompanionDirectory(), _dreams);
    }

    public override void _Process(double delta)
    {
        _ambience?.Process(delta);
        _companion?.Process(delta);
    }

    public override void _ExitTree()
    {
        _companion?.Dispose();
        _companion = null;
    }

    // 書き出した後は実行ファイル（.exe）の横の companion/、無ければエディタから実行したときのリポジトリ直下の companion/。
    // AppDomain の BaseDirectory は、書き出した後は .exe の横の data_ フォルダを指すため使わない。
    private static string CompanionDirectory()
    {
        var besideExecutable = Path.Combine(Path.GetDirectoryName(OS.GetExecutablePath()) ?? string.Empty, "companion");
        return Directory.Exists(besideExecutable)
            ? besideExecutable
            : Path.GetFullPath(Path.Combine(ProjectSettings.GlobalizePath("res://"), "..", "companion"));
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
}
