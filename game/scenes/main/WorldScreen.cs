using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Godot;
using TheogonyX.Core;
using TheogonyX.Data;

namespace TheogonyX.Scenes.Main;

/// <summary>
/// 星の地表と HUD を描き、時間の進め方のボタンをコアの <see cref="TimeControl"/> につなぐ
/// （docs/domain/planet-display.md、docs/domain/time-control.md）。
/// 自動進行は、<see cref="WorldCore"/> が毎フレーム呼ぶ <see cref="Process"/> で <see cref="AutoPlayClock"/> を進めて行う。
/// </summary>
public sealed class WorldScreen
{
    internal const int TileSize = 16;

    // 仮のタイルの色（DB16 から選ぶ。docs/art-guide.md）。本番のタイル画像が来たら差し替える。
    private static readonly Dictionary<TileType, Color> PlaceholderColors = new()
    {
        [TileType.Grassland] = Color.FromHtml("#6daa2c"),
        [TileType.Jungle] = Color.FromHtml("#346524"),
        [TileType.Desert] = Color.FromHtml("#dad45e"),
        [TileType.Scorched] = Color.FromHtml("#442434"),
        [TileType.Glacier] = Color.FromHtml("#deeed6"),
        [TileType.Water] = Color.FromHtml("#597dce"),
    };

    private static readonly Color ResetAlertColor = Color.FromHtml("#d04648");

    private readonly Node _owner;
    private readonly TimeControl _timeControl;
    private readonly TerrainRules _terrainRules;
    private readonly TimeControlSettings _timeSettings;
    private readonly AutoPlayClock _clock;

    private readonly TileMapLayer _terrain;
    private readonly Label _yearLabel;
    private readonly Label _nextYearLabel;
    private readonly Label _chaosLabel;
    private readonly Label _harmonyLabel;
    private readonly Label _dogmaList;
    private readonly Button _jumpButton;
    private readonly Button _autoPlayButton;
    private readonly Button _pauseButton;
    private readonly Button _speedButton;
    private readonly Button _resetButton;

    private Tween? _resetBlink;

    // 次の年までの残りとして表示している秒数。同じなら書き換えない。
    private int _shownRemainingSeconds = -1;

    /// <summary>100 年跳躍の後と、自動進行の 1 年ごと。</summary>
    public event Action? TimeAdvanced;

    /// <summary>世界再創生の後。</summary>
    public event Action? WorldReset;

    /// <summary>100 年跳躍の後。<see cref="TimeAdvanced"/> の後に呼ぶ。</summary>
    public event Action? Jumped;

    /// <summary>地表を描き直した（マスごとのタイル種別。添字は [列, 行]）。</summary>
    public event Action<TileType[,]>? TerrainDrawn;

    /// <param name="owner">ノードを探し、Tween を作るためのシーンの根。</param>
    public WorldScreen(
        Node owner, TimeControl timeControl, AutoPlayClock clock, TerrainRules terrainRules, TimeControlSettings timeSettings)
    {
        _clock = clock;
        _owner = owner;
        _timeControl = timeControl;
        _terrainRules = terrainRules;
        _timeSettings = timeSettings;

        _terrain = owner.GetNode<TileMapLayer>("%Terrain");
        _yearLabel = owner.GetNode<Label>("%YearLabel");
        _nextYearLabel = owner.GetNode<Label>("%NextYearLabel");
        _chaosLabel = owner.GetNode<Label>("%ChaosLabel");
        _harmonyLabel = owner.GetNode<Label>("%HarmonyLabel");
        _dogmaList = owner.GetNode<Label>("%DogmaList");
        // ドグマが増えて欄に収まらなくなったら、ボタンを押し下げずにスクロールさせ、いちばん新しいドグマ（いちばん下）を見せる
        var dogmaScroll = (ScrollContainer)_dogmaList.GetParent();
        var dogmaScrollBar = dogmaScroll.GetVScrollBar();
        // 範囲が変わった直後はスクロールの幅（page）がまだ古いので、並べ終わってから合わせる
        dogmaScrollBar.Changed += () => Callable.From(() => dogmaScroll.ScrollVertical = (int)dogmaScrollBar.MaxValue).CallDeferred();
        _jumpButton = owner.GetNode<Button>("%JumpButton");
        _autoPlayButton = owner.GetNode<Button>("%AutoPlayButton");
        _pauseButton = owner.GetNode<Button>("%PauseButton");
        _speedButton = owner.GetNode<Button>("%SpeedButton");
        _resetButton = owner.GetNode<Button>("%ResetButton");

        _terrain.TileSet = CreatePlaceholderTileSet();
        _jumpButton.Text = string.Format(CultureInfo.CurrentCulture, Tr("BUTTON_JUMP"), timeSettings.JumpYears);

        _jumpButton.Pressed += OnJumpPressed;
        _autoPlayButton.Pressed += OnAutoPlayPressed;
        _pauseButton.Pressed += OnPausePressed;
        _speedButton.Pressed += OnSpeedPressed;
        _resetButton.Pressed += OnResetPressed;

        DrawSpeed();
        Refresh();
    }

    /// <summary>毎フレーム呼ぶ。自動進行中なら時計を進め、1 年たったら 1 年進める。</summary>
    public void Process(double delta)
    {
        if (_timeControl.State == TimeControlState.AutoPlaying && _clock.Tick(delta))
        {
            _timeControl.AdvanceOneYear();
            AfterTimeAdvanced();
        }
        DrawNextYear();
    }

    private void OnJumpPressed()
    {
        _timeControl.Jump();
        var world = _timeControl.World;
        GD.Print(FormattableString.Invariant(
            $"event=time_skipped years={_timeSettings.JumpYears} year={world.Year} forestAmount={world.ForestAmount} waterAmount={world.WaterAmount} temperature={world.Temperature} chaosLevel={world.ChaosLevel} harmonyLevel={world.HarmonyLevel} isDestroyed={world.IsDestroyed}"));
        AfterTimeAdvanced();
        Jumped?.Invoke();
    }

    private void AfterTimeAdvanced()
    {
        if (_timeControl.State == TimeControlState.Destroyed)
        {
            GD.Print(FormattableString.Invariant($"event=world_destroyed year={_timeControl.World.Year}"));
        }
        TimeAdvanced?.Invoke();
        Refresh();
    }

    private void OnAutoPlayPressed()
    {
        _timeControl.StartAutoPlay();
        Refresh();
    }

    private void OnPausePressed()
    {
        _timeControl.Pause();
        Refresh();
    }

    private void OnSpeedPressed()
    {
        _clock.CycleSpeed();
        DrawSpeed();
    }

    private void OnResetPressed()
    {
        _timeControl.Reset();
        _clock.Reset();
        GD.Print("event=world_reset year=0");
        WorldReset?.Invoke();
        Refresh();
    }

    private void DrawSpeed()
    {
        _speedButton.Text = Tr(_clock.Speed switch
        {
            AutoPlaySpeed.Fast => "BUTTON_SPEED_FAST",
            AutoPlaySpeed.RealTime => "BUTTON_SPEED_REAL_TIME",
            _ => "BUTTON_SPEED_NORMAL",
        });
    }

    // 現実同期で自動進行しているときだけ、次の年までの残り時間を hh:mm:ss で出す
    private void DrawNextYear()
    {
        var visible = _clock.Speed == AutoPlaySpeed.RealTime && _timeControl.State == TimeControlState.AutoPlaying;
        _nextYearLabel.Visible = visible;
        if (!visible)
        {
            _shownRemainingSeconds = -1;
            return;
        }

        var seconds = (int)_clock.RemainingSeconds;
        if (seconds == _shownRemainingSeconds)
        {
            return;
        }
        _shownRemainingSeconds = seconds;
        var remaining = FormattableString.Invariant($"{seconds / 3600:00}:{seconds / 60 % 60:00}:{seconds % 60:00}");
        _nextYearLabel.Text = string.Format(CultureInfo.CurrentCulture, Tr("HUD_NEXT_YEAR"), remaining);
    }

    /// <summary>地表・HUD・ボタンを描き直す（ドグマが増えたときなど）。</summary>
    public void Refresh()
    {
        DrawTerrain();
        DrawHud();
        UpdateButtons();
    }

    private void DrawTerrain()
    {
        var tiles = TerrainClassifier.ClassifyTiles(_timeControl.World, _terrainRules);
        for (var x = 0; x < _terrainRules.Columns; x++)
        {
            for (var y = 0; y < _terrainRules.Rows; y++)
            {
                _terrain.SetCell(new Vector2I(x, y), 0, new Vector2I((int)tiles[x, y], 0));
            }
        }
        TerrainDrawn?.Invoke(tiles);
    }

    private void DrawHud()
    {
        var world = _timeControl.World;
        var culture = CultureInfo.CurrentCulture;
        _yearLabel.Text = string.Format(culture, Tr("HUD_YEAR"), world.Year);
        _chaosLabel.Text = string.Format(culture, Tr("HUD_CHAOS"), world.ChaosLevel);
        _harmonyLabel.Text = string.Format(culture, Tr("HUD_HARMONY"), world.HarmonyLevel);
        _dogmaList.Text = _timeControl.Dogmas.Count == 0
            ? Tr("HUD_NO_DOGMA")
            : string.Join("\n", _timeControl.Dogmas.Select(dogma => string.Format(
                culture,
                Tr("DOGMA_ENTRY_BELIEVERS"),
                Tr($"SYMBOL_{dogma.TargetSymbolId}"),
                Tr($"DOGMA_TYPE_{dogma.Type.ToString().ToUpperInvariant()}"),
                _timeControl.BelieversOf(dogma))));
    }

    private void UpdateButtons()
    {
        _jumpButton.Disabled = !_timeControl.CanDo(TimeControlAction.Jump);
        _autoPlayButton.Disabled = !_timeControl.CanDo(TimeControlAction.StartAutoPlay);
        _pauseButton.Disabled = !_timeControl.CanDo(TimeControlAction.Pause);
        _resetButton.Disabled = !_timeControl.CanDo(TimeControlAction.Reset);

        var destroyed = _timeControl.State == TimeControlState.Destroyed;
        if (destroyed && _resetBlink is null)
        {
            _resetBlink = _owner.CreateTween().SetLoops();
            _resetBlink.TweenProperty(_resetButton, "modulate", ResetAlertColor, 0.4);
            _resetBlink.TweenProperty(_resetButton, "modulate", Colors.White, 0.4);
        }
        else if (!destroyed && _resetBlink is not null)
        {
            _resetBlink.Kill();
            _resetBlink = null;
            _resetButton.Modulate = Colors.White;
        }
    }

    private static string Tr(string key) => TranslationServer.Translate(key).ToString();

    private static TileSet CreatePlaceholderTileSet()
    {
        var tileTypes = Enum.GetValues<TileType>();
        var image = Image.CreateEmpty(TileSize * tileTypes.Length, TileSize, false, Image.Format.Rgba8);
        foreach (var tileType in tileTypes)
        {
            image.FillRect(new Rect2I((int)tileType * TileSize, 0, TileSize, TileSize), PlaceholderColors[tileType]);
        }

        var source = new TileSetAtlasSource
        {
            Texture = ImageTexture.CreateFromImage(image),
            TextureRegionSize = new Vector2I(TileSize, TileSize),
        };
        var tileSet = new TileSet { TileSize = new Vector2I(TileSize, TileSize) };
        tileSet.AddSource(source, 0);
        foreach (var tileType in tileTypes)
        {
            source.CreateTile(new Vector2I((int)tileType, 0));
        }
        return tileSet;
    }
}
