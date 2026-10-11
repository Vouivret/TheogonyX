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
/// </summary>
public sealed class WorldScreen
{
    private const int TileSize = 16;

    // 仮のタイルの色（DB16 から選ぶ。docs/art-guide.md）。本番のタイル画像が来たら差し替える。
    private static readonly Dictionary<TileType, Color> PlaceholderColors = new()
    {
        [TileType.Grassland] = Color.FromHtml("#6daa2c"),
        [TileType.Jungle] = Color.FromHtml("#346524"),
        [TileType.Desert] = Color.FromHtml("#dad45e"),
        [TileType.Scorched] = Color.FromHtml("#442434"),
        [TileType.Glacier] = Color.FromHtml("#deeed6"),
    };

    private static readonly Color ResetAlertColor = Color.FromHtml("#d04648");

    private readonly Node _owner;
    private readonly TimeControl _timeControl;
    private readonly TerrainRules _terrainRules;
    private readonly int _mapRows;
    private readonly TimeControlSettings _timeSettings;

    private readonly TileMapLayer _terrain;
    private readonly Timer _autoPlayTimer;
    private readonly Label _yearLabel;
    private readonly Label _chaosLabel;
    private readonly Label _harmonyLabel;
    private readonly Label _dogmaList;
    private readonly Button _jumpButton;
    private readonly Button _autoPlayButton;
    private readonly Button _pauseButton;
    private readonly Button _speedButton;
    private readonly Button _resetButton;

    private bool _fast;
    private Tween? _resetBlink;

    /// <param name="owner">ノードを探し、Tween を作るためのシーンの根。</param>
    public WorldScreen(Node owner, TimeControl timeControl, TerrainRules terrainRules, int mapRows, TimeControlSettings timeSettings)
    {
        _owner = owner;
        _timeControl = timeControl;
        _terrainRules = terrainRules;
        _mapRows = mapRows;
        _timeSettings = timeSettings;

        _terrain = owner.GetNode<TileMapLayer>("%Terrain");
        _autoPlayTimer = owner.GetNode<Timer>("%AutoPlayTimer");
        _yearLabel = owner.GetNode<Label>("%YearLabel");
        _chaosLabel = owner.GetNode<Label>("%ChaosLabel");
        _harmonyLabel = owner.GetNode<Label>("%HarmonyLabel");
        _dogmaList = owner.GetNode<Label>("%DogmaList");
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
        _autoPlayTimer.Timeout += OnAutoPlayTimeout;

        ApplySpeed();
        Refresh();
    }

    private void OnJumpPressed()
    {
        _timeControl.Jump();
        var world = _timeControl.World;
        GD.Print(FormattableString.Invariant(
            $"event=time_skipped years={_timeSettings.JumpYears} year={world.Year} forestAmount={world.ForestAmount} waterAmount={world.WaterAmount} temperature={world.Temperature} chaosLevel={world.ChaosLevel} harmonyLevel={world.HarmonyLevel} isDestroyed={world.IsDestroyed}"));
        AfterTimeAdvanced();
    }

    private void OnAutoPlayTimeout()
    {
        if (!_timeControl.CanDo(TimeControlAction.AdvanceOneYear))
        {
            _autoPlayTimer.Stop();
            return;
        }
        _timeControl.AdvanceOneYear();
        AfterTimeAdvanced();
    }

    private void AfterTimeAdvanced()
    {
        if (_timeControl.State == TimeControlState.Destroyed)
        {
            GD.Print(FormattableString.Invariant($"event=world_destroyed year={_timeControl.World.Year}"));
        }
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
        _fast = !_fast;
        ApplySpeed();
    }

    private void OnResetPressed()
    {
        _timeControl.Reset();
        GD.Print("event=world_reset year=0");
        Refresh();
    }

    private void ApplySpeed()
    {
        var yearsPerSecond = _fast ? _timeSettings.FastYearsPerSecond : _timeSettings.NormalYearsPerSecond;
        _autoPlayTimer.WaitTime = 1.0 / yearsPerSecond;
        if (!_autoPlayTimer.IsStopped())
        {
            // 待っている途中の間隔を捨て、新しい速さですぐ数え直す。
            _autoPlayTimer.Start();
        }
        _speedButton.Text = Tr(_fast ? "BUTTON_SPEED_FAST" : "BUTTON_SPEED_NORMAL");
    }

    private void Refresh()
    {
        DrawTerrain();
        DrawHud();
        UpdateButtons();

        if (_timeControl.State != TimeControlState.AutoPlaying)
        {
            _autoPlayTimer.Stop();
        }
        else if (_autoPlayTimer.IsStopped())
        {
            _autoPlayTimer.Start();
        }
    }

    private void DrawTerrain()
    {
        var layout = TerrainClassifier.Classify(_timeControl.World, _terrainRules);
        for (var x = 0; x < _terrainRules.Columns; x++)
        {
            var tileType = x < layout.Columns ? layout.TileType : TileType.Grassland;
            for (var y = 0; y < _mapRows; y++)
            {
                _terrain.SetCell(new Vector2I(x, y), 0, new Vector2I((int)tileType, 0));
            }
        }
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
                Tr("DOGMA_ENTRY"),
                Tr($"SYMBOL_{dogma.TargetSymbolId}"),
                Tr($"DOGMA_TYPE_{dogma.Type.ToString().ToUpperInvariant()}"))));
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
