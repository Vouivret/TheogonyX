using System;
using System.Collections.Generic;
using System.Globalization;
using Godot;
using TheogonyX.Core;
using TheogonyX.Data;

namespace TheogonyX.Scenes.Main;

/// <summary>
/// 星の地表と上の帯（年と世界の 5 つの値の目盛り）を描き、時間の進め方のボタンとメニューをコアの <see cref="TimeControl"/> につなぐ
/// （docs/domain/planet-display.md、docs/domain/time-control.md）。
/// 自動進行は、<see cref="WorldCore"/> が毎フレーム呼ぶ <see cref="Process"/> で <see cref="AutoPlayClock"/> を進めて行う。
/// </summary>
public sealed class WorldScreen
{
    internal const int TileSize = 16;

    private static readonly Color ResetAlertColor = GamePalette.Of("red");

    // 上の帯の世界の値: 指標と、説明の文言のキー（アイコンは TerrainArt が描く）
    private static readonly (WorldMetric Metric, string Key)[] Meters =
    [
        (WorldMetric.ForestAmount, "HUD_FOREST"),
        (WorldMetric.WaterAmount, "HUD_WATER"),
        (WorldMetric.Temperature, "HUD_TEMPERATURE"),
        (WorldMetric.ChaosLevel, "HUD_CHAOS"),
        (WorldMetric.HarmonyLevel, "HUD_HARMONY"),
    ];

    private readonly Node _owner;
    private readonly TimeControl _timeControl;
    private readonly TerrainRules _terrainRules;
    private readonly TimeControlSettings _timeSettings;
    private readonly AutoPlayClock _clock;
    private readonly WorldRules _worldRules;

    private readonly TileMapLayer _terrain;
    private readonly Label _yearLabel;
    private readonly Label _nextYearLabel;
    private readonly ProgressBar[] _meters;
    private readonly TextureRect[] _meterIcons;
    private readonly Button _jumpButton;
    private readonly Button _autoPlayButton;
    private readonly Button _speedButton;
    private readonly Button _resetButton;
    private readonly Button _menuButton;
    private readonly Control _menu;

    private Tween? _resetBlink;
    private TileType[,]? _drawn;

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
    /// <param name="worldRules">世界の値の目盛りの下限と上限に使う。</param>
    public WorldScreen(
        Node owner, TimeControl timeControl, AutoPlayClock clock, TerrainRules terrainRules, TimeControlSettings timeSettings, WorldRules worldRules)
    {
        _clock = clock;
        _owner = owner;
        _timeControl = timeControl;
        _terrainRules = terrainRules;
        _timeSettings = timeSettings;
        _worldRules = worldRules;

        _terrain = owner.GetNode<TileMapLayer>("%Terrain");
        _yearLabel = owner.GetNode<Label>("%YearLabel");
        _nextYearLabel = owner.GetNode<Label>("%NextYearLabel");
        (_meterIcons, _meters) = CreateMeters(owner.GetNode<Container>("%WorldValues"));
        _jumpButton = owner.GetNode<Button>("%JumpButton");
        _autoPlayButton = owner.GetNode<Button>("%AutoPlayButton");
        _speedButton = owner.GetNode<Button>("%SpeedButton");
        _resetButton = owner.GetNode<Button>("%ResetButton");
        _menuButton = owner.GetNode<Button>("%MenuButton");
        _menu = owner.GetNode<Control>("%Menu");

        _terrain.TileSet = CreateTileSet();
        _jumpButton.Text = string.Format(CultureInfo.CurrentCulture, Tr("BUTTON_JUMP"), timeSettings.JumpYears);

        _jumpButton.Pressed += OnJumpPressed;
        _autoPlayButton.Pressed += OnAutoPlayPressed;
        _speedButton.Pressed += OnSpeedPressed;
        _resetButton.Pressed += OnResetPressed;
        _menuButton.Pressed += () =>
        {
            _menu.Visible = !_menu.Visible;
            // 後から足した引き出し・板より手前に出す（描く順と、クリックを受ける順）
            _menu.MoveToFront();
        };

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

    // 自動進行と一時停止を 1 つのボタンで切り替える（押せるかどうかは状態遷移表のまま）
    private void OnAutoPlayPressed()
    {
        if (_timeControl.State == TimeControlState.AutoPlaying)
        {
            _timeControl.Pause();
        }
        else
        {
            _timeControl.StartAutoPlay();
        }
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
        _menu.Visible = false;
        GD.Print("event=world_reset year=0");
        WorldReset?.Invoke();
        Refresh();
    }

    private void DrawSpeed()
    {
        _speedButton.Text = Tr(_clock.Speed switch
        {
            AutoPlaySpeed.X2 => "BUTTON_SPEED_X2",
            AutoPlaySpeed.X3 => "BUTTON_SPEED_X3",
            _ => "BUTTON_SPEED_X1",
        });
    }

    // 自動進行しているときだけ、次の年までの残り時間を hh:mm:ss で出す
    private void DrawNextYear()
    {
        var visible = _timeControl.State == TimeControlState.AutoPlaying;
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

    /// <summary>地表・上の帯・ボタンを描き直す（ドグマが増えたときなど）。</summary>
    public void Refresh()
    {
        DrawTerrain();
        DrawHud();
        UpdateButtons();
    }

    private void DrawTerrain()
    {
        // 新しい星ならその地域ごとのタイル、今の小さな星なら星全体の値から決めたタイル。前に描いたものと違うマスだけ描き直す
        var tiles = _timeControl.Tiles ?? TerrainClassifier.ClassifyTiles(_timeControl.World, _terrainRules);
        for (var x = 0; x < tiles.GetLength(0); x++)
        {
            for (var y = 0; y < tiles.GetLength(1); y++)
            {
                if (_drawn is not null && _drawn.GetLength(0) == tiles.GetLength(0) && _drawn[x, y] == tiles[x, y])
                {
                    continue;
                }
                _terrain.SetCell(new Vector2I(x, y), 0, new Vector2I(((int)tiles[x, y] * TerrainArt.Variants) + TerrainArt.VariantAt(x, y), 0));
            }
        }
        _drawn = tiles;
        TerrainDrawn?.Invoke(tiles);
    }

    private void DrawHud()
    {
        var world = _timeControl.World;
        var culture = CultureInfo.CurrentCulture;
        _yearLabel.Text = string.Format(culture, Tr("HUD_YEAR"), world.Year);
        // 目盛りは定義の下限から上限までの割合。数値はアイコンと目盛りの説明に出す
        for (var i = 0; i < Meters.Length; i++)
        {
            var (metric, key) = Meters[i];
            var min = ValueOf(_worldRules.Min, metric);
            var max = ValueOf(_worldRules.Max, metric);
            var value = world.ValueOf(metric);
            _meters[i].Value = max > min ? (value - min) / (max - min) : 0;
            var tooltip = string.Format(culture, Tr(key), value);
            _meters[i].TooltipText = tooltip;
            _meterIcons[i].TooltipText = tooltip;
        }
    }

    private static float ValueOf(EnvironmentValues values, WorldMetric metric) => metric switch
    {
        WorldMetric.ForestAmount => values.ForestAmount,
        WorldMetric.WaterAmount => values.WaterAmount,
        WorldMetric.Temperature => values.Temperature,
        WorldMetric.ChaosLevel => values.ChaosLevel,
        WorldMetric.HarmonyLevel => values.HarmonyLevel,
        _ => throw new ArgumentOutOfRangeException(nameof(metric), metric, null),
    };

    // 値ごとに、仮のアイコン（単色の四角）と短い目盛りを並べる
    private static (TextureRect[] Icons, ProgressBar[] Meters) CreateMeters(Container parent)
    {
        var icons = new TextureRect[Meters.Length];
        var meters = new ProgressBar[Meters.Length];
        for (var i = 0; i < Meters.Length; i++)
        {
            icons[i] = new TextureRect
            {
                Texture = TerrainArt.CreateIcon(Meters[i].Metric),
                TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
                // 10 × 10 px の絵を 2 倍（20 × 20）で出す
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                StretchMode = TextureRect.StretchModeEnum.Scale,
                CustomMinimumSize = new Vector2(20, 20),
                SizeFlagsVertical = Control.SizeFlags.ShrinkCenter,
                MouseFilter = Control.MouseFilterEnum.Stop,
            };
            meters[i] = new ProgressBar
            {
                ThemeTypeVariation = "ValueMeter",
                MinValue = 0,
                MaxValue = 1,
                Step = 0,
                ShowPercentage = false,
                CustomMinimumSize = new Vector2(48, 6),
                SizeFlagsVertical = Control.SizeFlags.ShrinkCenter,
            };
            var pair = new HBoxContainer { Name = Meters[i].Metric.ToString() };
            pair.AddChild(icons[i]);
            pair.AddChild(meters[i]);
            parent.AddChild(pair);
        }
        return (icons, meters);
    }

    private void UpdateButtons()
    {
        _jumpButton.Disabled = !_timeControl.CanDo(TimeControlAction.Jump);
        var autoPlaying = _timeControl.State == TimeControlState.AutoPlaying;
        _autoPlayButton.Text = autoPlaying ? "BUTTON_PAUSE" : "BUTTON_AUTO_PLAY";
        _autoPlayButton.Disabled = !_timeControl.CanDo(autoPlaying ? TimeControlAction.Pause : TimeControlAction.StartAutoPlay);
        _resetButton.Disabled = !_timeControl.CanDo(TimeControlAction.Reset);

        // 崩壊したら、メニューのボタンと、メニューの中の世界再創生を赤く点滅させる
        var destroyed = _timeControl.State == TimeControlState.Destroyed;
        if (destroyed && _resetBlink is null)
        {
            _resetBlink = _owner.CreateTween().SetLoops().SetParallel();
            _resetBlink.TweenProperty(_menuButton, "modulate", ResetAlertColor, 0.4);
            _resetBlink.TweenProperty(_resetButton, "modulate", ResetAlertColor, 0.4);
            _resetBlink.Chain().TweenProperty(_menuButton, "modulate", Colors.White, 0.4);
            _resetBlink.TweenProperty(_resetButton, "modulate", Colors.White, 0.4);
        }
        else if (!destroyed && _resetBlink is not null)
        {
            _resetBlink.Kill();
            _resetBlink = null;
            _menuButton.Modulate = Colors.White;
            _resetButton.Modulate = Colors.White;
        }
    }

    private static string Tr(string key) => TranslationServer.Translate(key).ToString();

    internal static TileSet CreateTileSet()
    {
        var source = new TileSetAtlasSource
        {
            Texture = ImageTexture.CreateFromImage(TerrainArt.CreateTileAtlas(TileSize)),
            TextureRegionSize = new Vector2I(TileSize, TileSize),
        };
        var tileSet = new TileSet { TileSize = new Vector2I(TileSize, TileSize) };
        tileSet.AddSource(source, 0);
        for (var column = 0; column < Enum.GetValues<TileType>().Length * TerrainArt.Variants; column++)
        {
            source.CreateTile(new Vector2I(column, 0));
        }
        return tileSet;
    }
}
