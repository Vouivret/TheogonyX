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
    private SkyView? _sky;
    private CameraView? _camera;
    private BuildingView? _buildings;
    private StrataView? _strata;
    private ResidentView? _residents;
    private HistoryView? _history;
    private DialectBookView? _dialectBook;
    private CrowdView? _crowd;
    private DreamView? _dreams;
    private OracleView? _oracle;
    private MythView? _myth;
    private OllamaClient? _ollama;
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
        var needsRules = GameDataLoader.ParseNeeds(rulesJson);
        var historyRules = GameDataLoader.ParseHistory(rulesJson);
        var beliefRules = GameDataLoader.ParseBelief(rulesJson);
        var journeyRules = GameDataLoader.ParseJourney(rulesJson);
        var oracleAiRules = GameDataLoader.ParseOracleAi(rulesJson);
        var (dialectRules, dialectVessel) = GameDataLoader.ParseDialect(rulesJson);

        _saveSettings = GameDataLoader.ParseSave(ReadText("res://data/save.json"));
        _saveStore = new SaveStore("user://save", TimeProvider.System);
        var oracleRules = GameDataLoader.ParseOracleRules(rulesJson, dreamRules);
        var saved = LoadSave(_saveStore, dreamRules, prophetDreamRules, civilizationRules, needsRules, oracleRules, out var loadFailed, out var outdated);
        // 方言の状態が定義に合わないセーブは、何も当てはめる前に壊れたセーブとして扱う
        if (saved?.Dialect is { } savedDialect && !FitsDialect(dialectRules, dialectVessel, savedDialect))
        {
            GD.PushWarning("event=save_load_failed reason=invalid_dialect");
            loadFailed = true;
            _saveStore.KeepBroken();
            saved = null;
        }

        // シードはセーブのもの、無ければ起動時の時刻から作り、記録する（同じシードで同じデッキの並びを再現できる）
        var seed = saved?.Seed ?? (int)(TimeProvider.System.GetUtcNow().ToUnixTimeMilliseconds() & int.MaxValue);
        var timeControl = new TimeControl(
            worldRules, timeSettings.JumpYears, new CivilizationSetup(civilizationRules, terrainRules, seed, needsRules), historyRules, beliefRules, journeyRules);
        _timeControl = timeControl;
        var clock = new AutoPlayClock(new Dictionary<AutoPlaySpeed, double>
        {
            [AutoPlaySpeed.Normal] = 1.0 / timeSettings.NormalYearsPerSecond,
            [AutoPlaySpeed.Fast] = 1.0 / timeSettings.FastYearsPerSecond,
            [AutoPlaySpeed.RealTime] = timeSettings.RealTimeSecondsPerYear,
        });
        var deck = new OracleDeck(seed, GameDataLoader.ParseDeck(rulesJson, dreamRules));
        var prophet = new ProphetDreams(timeControl, prophetDreamRules, deck, oracleRules);
        prophet.AttachDialect(new DialectState(dialectRules, dialectVessel, seed));
        if (saved is not null)
        {
            SaveGame.Apply(saved, timeControl, prophet, clock);
            GD.Print($"event=game_loaded seed={seed} year={timeControl.World.Year}");
        }
        else
        {
            GD.Print($"event=game_started seed={seed}");
        }
        _writeSave = () => SaveDataSerializer.Write(SaveGame.Capture(timeControl, prophet, seed, clock));
        _screen = new WorldScreen(this, timeControl, clock, terrainRules, timeSettings);
        _ambience = new AmbienceView(this, timeControl, ambienceRules, prophetSettings, campfireSettings, lightingSettings, civilizationRules);
        _sky = new SkyView(this, _ambience, civilizationRules, seed);
        _camera = new CameraView(this, cameraSettings);
        _buildings = new BuildingView(this, timeControl, lightingSettings);
        _screen.TerrainDrawn += _buildings.Refresh;
        _strata = new StrataView(this, timeControl, civilizationRules);
        _screen.TerrainDrawn += _strata.Refresh;
        _residents = new ResidentView(this, timeControl, civilizationRules, needsRules, _ambience, seed, cameraSettings.ResidentIcons);
        _screen.TerrainDrawn += _residents.Refresh;
        _history = new HistoryView(this, timeControl);
        _dialectBook = new DialectBookView(this, prophet);
        _screen.TimeAdvanced += () => LogResidentEvents(timeControl.ResidentEvents);
        _screen.TimeAdvanced += () => LogForgottenDogmas(timeControl);
        _screen.TimeAdvanced += () => LogProphetMoves(timeControl);
        _crowd = new CrowdView(this, seed, timeControl, clock, terrainRules, crowdRules, crowdLook, prophetSettings);
        _screen.TerrainDrawn += _crowd.OnTerrainDrawn;
        // 詳細の視点は住人の絵、中拡大・大陸は群衆の点。境目は重ねて薄くしながら切り替える（docs/domain/camera-views.md の「群衆の描き方」）
        var blend = (float detail) =>
        {
            _residents.SetOpacity(detail);
            _crowd.SetOpacity(1f - detail);
        };
        _camera.DetailBlendChanged += blend;
        blend(_camera.DetailBlend);
        _screen.WorldReset += _crowd.OnWorldReset;
        var commands = new OracleCommandQueue(prophet, timeControl);
        _dreams = new DreamView(this, prophet, commands, timeControl);
        _screen.TimeAdvanced += _dreams.OnTimeAdvanced;
        _screen.WorldReset += _dreams.OnWorldReset;
        _dreams.OraclePronounced += _screen.Refresh;
        // 夢の欄が新しい夢を見た後に保存するよう、DreamView の後につなぐ
        _dreams.OraclePronounced += Save;
        // 予言者の誤解と歴史の神話化で、ローカルの LLM への通信を 1 つ共有する
        _ollama = new OllamaClient(oracleAiRules);
        _oracle = new OracleView(this, timeControl, _dreams, oracleAiRules, _ollama, prophet, commands);
        _oracle.OracleAttached += Save;
        _oracle.Spoken += Save;
        _oracle.StateChanged += _screen.Refresh;
        _myth = new MythView(timeControl, oracleAiRules, _ollama);
        _myth.MythAttached += Save;
        _screen.Jumped += Save;
        _screen.WorldReset += Save;
        if (loadFailed || outdated)
        {
            ShowSaveNotice(outdated ? "SAVE_OUTDATED" : "SAVE_LOAD_FAILED");
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
        _sky?.Process(delta);
        _camera?.Process(delta);
        _residents?.Process(delta);
        _history?.Process();
        _dialectBook?.Process();
        _oracle?.Process(delta);
        _myth?.Process(delta);
        _crowd?.Process(delta);
        _companion?.Process(delta);
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (_residents?.HandleInput(@event) == true || _camera?.HandleInput(@event) == true)
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
        _ollama?.Dispose();
        _ollama = null;
        _oracle = null;
        _myth = null;
        Save();
    }

    /// <param name="broken">セーブが壊れていて読めなかったか。</param>
    /// <returns>読めたセーブ。無いか、読めなかったら null。</returns>
    private static SaveData? LoadSave(
        SaveStore store, DreamRules dreamRules, ProphetDreamRules rules, CivilizationRules civilizationRules, NeedsRules needsRules,
        OracleRules oracleRules, out bool broken, out bool outdated)
    {
        broken = false;
        outdated = false;
        var text = store.Load();
        if (text is null)
        {
            return null;
        }
        if (SaveDataSerializer.TryParse(text, dreamRules, rules, civilizationRules, needsRules, oracleRules, out var data, out var reason))
        {
            return data;
        }

        GD.PushWarning($"event=save_load_failed reason={reason}");
        if (reason == "unsupported_save_version")
        {
            // 前の版のセーブは読めないが、消さずに別の名前で残し、新しい星で始める（1 度だけ知らせる）
            outdated = true;
            store.KeepOutdated();
            return null;
        }
        broken = true;
        store.KeepBroken();
        return null;
    }

    // 保存した方言の状態が、今の定義に合うか（合わなければ読み込まない）
    private static bool FitsDialect(DialectRules rules, string vesselId, DialectSave save)
    {
        try
        {
            new DialectState(rules, vesselId, 0).Restore(save);
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    // 死と精神崩壊はゲーム上の主要な出来事なので Info、移り住むのは毎年あるので開発時だけ出す
    private static void LogResidentEvents(IReadOnlyList<ResidentEvent> events)
    {
        foreach (var residentEvent in events)
        {
            var person = residentEvent.Resident;
            var name = $"{person.Name.FirstPrefix}+{person.Name.FirstSuffix}/{person.Name.LineagePrefix}+{person.Name.LineageSuffix}";
            switch (residentEvent.Type)
            {
                case ResidentEventType.Died:
                    GD.Print($"event=resident_died year={residentEvent.Year} residentId={person.Id} region={person.Region} name={name}");
                    break;
                case ResidentEventType.BrokeDown:
                    var breakdown = person.Breakdown == BreakdownKind.Rampage ? "rampage" : "catatonia";
                    GD.Print($"event=resident_breakdown year={residentEvent.Year} residentId={person.Id} region={person.Region} breakdown={breakdown}");
                    break;
                case ResidentEventType.Born:
                    GD.Print($"event=resident_born year={residentEvent.Year} residentId={person.Id} region={person.Region} name={name}");
                    break;
                case ResidentEventType.Paired:
                    GD.Print($"event=resident_paired year={residentEvent.Year} residentId={person.Id} partnerId={residentEvent.Partner?.Id} region={person.Region}");
                    break;
                case ResidentEventType.Arrived when OS.IsDebugBuild():
                    GD.Print($"event=resident_arrived year={residentEvent.Year} residentId={person.Id} region={person.Region}");
                    break;
            }
        }
    }

    // 教えが忘れられたのはゲーム上の主要な出来事なので Info
    private static void LogForgottenDogmas(TimeControl timeControl)
    {
        foreach (var dogma in timeControl.ForgottenDogmas)
        {
            GD.Print($"event=dogma_forgotten year={timeControl.World.Year} symbolId={dogma.TargetSymbolId} dogmaType={dogma.Type}");
        }
    }

    // 予言者の旅は、毎年あるわけではなく出来事なので Info
    private static void LogProphetMoves(TimeControl timeControl)
    {
        foreach (var move in timeControl.ProphetMoves)
        {
            var reason = move.Reason == ProphetMoveReason.Destination ? "destination" : "wander";
            GD.Print($"event=prophet_moved year={move.Year} region={move.To} reason={reason}");
        }
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

    private void ShowSaveNotice(string key)
    {
        var notice = GetNode<Label>("%SaveNotice");
        notice.Text = TranslationServer.Translate(key).ToString();
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
