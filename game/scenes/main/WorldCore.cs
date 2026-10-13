using System;
using System.Collections.Generic;
using System.IO;
using Godot;
using TheogonyX.Core;
using TheogonyX.Core.Persistence;
using TheogonyX.Data;
using TheogonyX.Scenes.Bench;

namespace TheogonyX.Scenes.Main;

/// <summary>
/// メインシーンの根。ゲームデータを読み、画面（<see cref="WorldScreen"/>）・カメラ（<see cref="CameraView"/>）・群衆（<see cref="CrowdView"/>）・予言者の夢（<see cref="DreamView"/>）・
/// コンパニオンとの通信（<see cref="CompanionHost"/>）を組み立てる。
/// 起動時にセーブがあれば続きから始め、神託・100 年跳躍・世界再創生の後、自動進行中の一定の間隔、終了時に保存する（docs/domain/save-data.md）。
/// </summary>
public partial class WorldCore : Node2D
{
    private WorldScreen? _screen;
    private AmbienceView? _ambience;
    private CameraView? _camera;
    private BuildingView? _buildings;
    private StrataView? _strata;
    private CrowdView? _crowd;
    private DreamView? _dreams;
    private CompanionHost? _companion;
    private SaveStore? _saveStore;
    private Func<string>? _writeSave;
    private SaveSettings? _saveSettings;
    private double _secondsSinceSave;
    private TimeControl? _timeControl;

    // 読めなかったことを HUD に出しておく秒数
    private const double SaveNoticeSeconds = 10.0;

    public override void _Ready()
    {
        // 群衆の計測・GPU 版の突き合わせ（plan/feat-m3-crowd-gpu.md）。ゲームは始めず、セーブも読み書きしない
        var args = OS.GetCmdlineUserArgs();
        Node? tool = Array.IndexOf(args, CrowdBench.CommandLineArg) >= 0 ? new CrowdBench()
            : Array.IndexOf(args, CrowdCheck.CommandLineArg) >= 0 ? new CrowdCheck()
            : null;
        if (tool is not null)
        {
            foreach (var child in GetChildren())
            {
                child.QueueFree();
            }
            AddChild(tool);
            return;
        }

        var rulesJson = ReadText("res://data/rules.json");
        var dreamRules = GameDataLoader.ParseDreamRules(ReadText("res://data/symbols.json"), rulesJson);
        var worldRules = GameDataLoader.ParseWorldRules(rulesJson);
        var terrainRules = GameDataLoader.ParseTerrain(rulesJson);
        var timeSettings = GameDataLoader.ParseTimeControl(rulesJson);
        var ambienceRules = GameDataLoader.ParseAmbience(rulesJson);
        var prophetSettings = GameDataLoader.ParseProphet(rulesJson);
        var campfireSettings = GameDataLoader.ParseCampfire(rulesJson);
        var lightingSettings = GameDataLoader.ParseLighting(rulesJson);
        var cameraSettings = GameDataLoader.ParseCamera(rulesJson);
        var crowdRules = GameDataLoader.ParseCrowd(rulesJson);
        var crowdLook = GameDataLoader.ParseCrowdLook(rulesJson);
        var prophetDreamRules = GameDataLoader.ParseProphetDreams(rulesJson);
        var civilizationRules = GameDataLoader.ParseCivilization(rulesJson);

        _saveSettings = GameDataLoader.ParseSave(ReadText("res://data/save.json"));
        _saveStore = new SaveStore("user://save", TimeProvider.System);
        var saved = LoadSave(_saveStore, dreamRules, prophetDreamRules, civilizationRules, out var loadFailed);

        // シードはセーブのもの、無ければ起動時の時刻から作り、記録する（同じシードで同じ夢を再現できる）
        var seed = saved?.Seed ?? (int)(TimeProvider.System.GetUtcNow().ToUnixTimeMilliseconds() & int.MaxValue);
        var dreamSource = new DreamSource(seed, dreamRules, prophetDreamRules);
        var timeControl = new TimeControl(worldRules, timeSettings.JumpYears, new CivilizationSetup(civilizationRules, terrainRules, seed));
        _timeControl = timeControl;
        var clock = new AutoPlayClock(new Dictionary<AutoPlaySpeed, double>
        {
            [AutoPlaySpeed.Normal] = 1.0 / timeSettings.NormalYearsPerSecond,
            [AutoPlaySpeed.Fast] = 1.0 / timeSettings.FastYearsPerSecond,
            [AutoPlaySpeed.RealTime] = timeSettings.RealTimeSecondsPerYear,
        });
        var prophet = new ProphetDreams(timeControl, dreamRules, prophetDreamRules, dreamSource.Next);
        if (saved is not null)
        {
            SaveGame.Apply(saved, timeControl, prophet, dreamSource, clock);
            GD.Print($"event=game_loaded seed={seed} year={timeControl.World.Year}");
        }
        else
        {
            GD.Print($"event=game_started seed={seed}");
        }
        _writeSave = () => SaveDataSerializer.Write(SaveGame.Capture(timeControl, prophet, dreamSource, clock));
        _screen = new WorldScreen(this, timeControl, clock, terrainRules, timeSettings);
        _ambience = new AmbienceView(this, timeControl, ambienceRules, prophetSettings, campfireSettings, lightingSettings);
        _camera = new CameraView(this, cameraSettings);
        _buildings = new BuildingView(this, timeControl, lightingSettings);
        _screen.TerrainDrawn += _buildings.Refresh;
        _strata = new StrataView(this, timeControl, civilizationRules);
        _screen.TerrainDrawn += _strata.Refresh;
        _crowd = new CrowdView(this, seed, timeControl, clock, terrainRules, crowdRules, crowdLook, prophetSettings);
        _screen.TerrainDrawn += _crowd.OnTerrainDrawn;
        _screen.WorldReset += _crowd.OnWorldReset;
        _dreams = new DreamView(this, prophet, timeControl, dreamRules);
        _screen.TimeAdvanced += _dreams.OnTimeAdvanced;
        _screen.WorldReset += _dreams.OnWorldReset;
        _dreams.OraclePronounced += _screen.Refresh;
        // 夢の欄が新しい夢を見た後に保存するよう、DreamView の後につなぐ
        _dreams.OraclePronounced += Save;
        _screen.Jumped += Save;
        _screen.WorldReset += Save;
        if (loadFailed)
        {
            ShowSaveNotice();
        }

        var companionSettings = GameDataLoader.ParseCompanion(ReadText("res://data/companion.json"));
        _companion = new CompanionHost(this, companionSettings, CompanionDirectory(), _dreams);
    }

    public override void _Process(double delta)
    {
        _screen?.Process(delta);
        _secondsSinceSave += delta;
        // 自動進行中は一定の間隔で保存する（現実同期では年が進むのに 24 時間かかるので、年が進むのを待たない）
        if (_timeControl?.State == TimeControlState.AutoPlaying && _secondsSinceSave >= _saveSettings!.AutoSaveIntervalSeconds)
        {
            Save();
        }
        _ambience?.Process(delta);
        _camera?.Process(delta);
        _crowd?.Process(delta);
        _companion?.Process(delta);
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (_camera?.HandleInput(@event) == true)
        {
            GetViewport().SetInputAsHandled();
        }
    }

    public override void _ExitTree()
    {
        _companion?.Dispose();
        _companion = null;
        _crowd?.Dispose();
        _crowd = null;
        Save();
    }

    /// <param name="broken">セーブが壊れていて読めなかったか。</param>
    /// <returns>読めたセーブ。無いか、読めなかったら null。</returns>
    private static SaveData? LoadSave(
        SaveStore store, DreamRules dreamRules, ProphetDreamRules rules, CivilizationRules civilizationRules, out bool broken)
    {
        broken = false;
        var text = store.Load();
        if (text is null)
        {
            return null;
        }
        if (SaveDataSerializer.TryParse(text, dreamRules, rules, civilizationRules, out var data, out var reason))
        {
            return data;
        }

        GD.PushWarning($"event=save_load_failed reason={reason}");
        broken = true;
        store.KeepBroken();
        return null;
    }

    private void Save()
    {
        if (_saveStore is null || _writeSave is null)
        {
            return;
        }
        _saveStore.Save(_writeSave());
        _secondsSinceSave = 0;
        if (OS.IsDebugBuild())
        {
            GD.Print("event=game_saved");
        }
    }

    private void ShowSaveNotice()
    {
        var notice = GetNode<Label>("%SaveNotice");
        notice.Text = TranslationServer.Translate("SAVE_LOAD_FAILED").ToString();
        notice.Visible = true;
        GetTree().CreateTimer(SaveNoticeSeconds).Timeout += () => notice.Visible = false;
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

    internal static string ReadText(string path)
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
