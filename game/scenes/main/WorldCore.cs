using System;
using System.IO;
using Godot;
using TheogonyX.Core;
using TheogonyX.Core.Persistence;
using TheogonyX.Data;

namespace TheogonyX.Scenes.Main;

/// <summary>
/// メインシーンの根。ゲームデータを読み、画面（<see cref="WorldScreen"/>）・予言者の夢（<see cref="DreamView"/>）・
/// コンパニオンとの通信（<see cref="CompanionHost"/>）を組み立てる。
/// 起動時にセーブがあれば続きから始め、神託・100 年跳躍・世界再創生の後、自動進行中の一定の間隔、終了時に保存する（docs/domain/save-data.md）。
/// </summary>
public partial class WorldCore : Node2D
{
    private WorldScreen? _screen;
    private AmbienceView? _ambience;
    private DreamView? _dreams;
    private CompanionHost? _companion;
    private SaveStore? _saveStore;
    private Func<string>? _writeSave;
    private SaveSettings? _saveSettings;
    private double _secondsSinceSave;

    // 読めなかったことを HUD に出しておく秒数
    private const double SaveNoticeSeconds = 10.0;

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

        _saveSettings = GameDataLoader.ParseSave(ReadText("res://data/save.json"));
        _saveStore = new SaveStore("user://save", TimeProvider.System);
        var saved = LoadSave(_saveStore, dreamRules, prophetDreamRules, out var loadFailed);

        // シードはセーブのもの、無ければ起動時の時刻から作り、記録する（同じシードで同じ夢を再現できる）
        var seed = saved?.Seed ?? (int)(TimeProvider.System.GetUtcNow().ToUnixTimeMilliseconds() & int.MaxValue);
        var dreamSource = new DreamSource(seed, dreamRules, prophetDreamRules);
        var timeControl = new TimeControl(worldRules, timeSettings.JumpYears);
        var prophet = new ProphetDreams(timeControl, dreamRules, prophetDreamRules, dreamSource.Next);
        if (saved is not null)
        {
            SaveGame.Apply(saved, timeControl, prophet, dreamSource);
            GD.Print($"event=game_loaded seed={seed} year={timeControl.World.Year}");
        }
        else
        {
            GD.Print($"event=game_started seed={seed}");
        }
        _writeSave = () => SaveDataSerializer.Write(SaveGame.Capture(timeControl, prophet, dreamSource));
        _screen = new WorldScreen(this, timeControl, terrainRules, mapRows, timeSettings);
        _ambience = new AmbienceView(this, timeControl, ambienceRules, prophetSettings);
        _dreams = new DreamView(this, prophet, timeControl, dreamRules);
        _screen.TimeAdvanced += _dreams.OnTimeAdvanced;
        _screen.WorldReset += _dreams.OnWorldReset;
        _dreams.OraclePronounced += _screen.Refresh;
        // 夢の欄が新しい夢を見た後に保存するよう、DreamView の後につなぐ
        _dreams.OraclePronounced += Save;
        _screen.Jumped += Save;
        _screen.WorldReset += Save;
        _screen.TimeAdvanced += AutoSave;
        if (loadFailed)
        {
            ShowSaveNotice();
        }

        var companionSettings = GameDataLoader.ParseCompanion(ReadText("res://data/companion.json"));
        _companion = new CompanionHost(this, companionSettings, CompanionDirectory(), _dreams);
    }

    public override void _Process(double delta)
    {
        _secondsSinceSave += delta;
        _ambience?.Process(delta);
        _companion?.Process(delta);
    }

    public override void _ExitTree()
    {
        _companion?.Dispose();
        _companion = null;
        Save();
    }

    /// <param name="broken">セーブが壊れていて読めなかったか。</param>
    /// <returns>読めたセーブ。無いか、読めなかったら null。</returns>
    private static SaveData? LoadSave(SaveStore store, DreamRules dreamRules, ProphetDreamRules rules, out bool broken)
    {
        broken = false;
        var text = store.Load();
        if (text is null)
        {
            return null;
        }
        if (SaveDataSerializer.TryParse(text, dreamRules, rules, out var data, out var reason))
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

    // 自動進行中は、前の保存から間隔がたった後の、次の 1 年が進んだときに保存する
    private void AutoSave()
    {
        if (_secondsSinceSave >= _saveSettings!.AutoSaveIntervalSeconds)
        {
            Save();
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
