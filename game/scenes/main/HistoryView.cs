using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Godot;
using TheogonyX.Core;

namespace TheogonyX.Scenes.Main;

/// <summary>
/// 星の年表（docs/domain/history.md）。下の帯のボタンで左の引き出しを開け閉めし、頭に今の教え（ドグマと信者の数）、
/// その下に新しい行を上にして年表を出す。
/// 年表はコアの <see cref="TimeControl.History"/> が持ち、ここでは文に組み立てて見せるだけにする。
/// 神話の文がある章の行は、見出しだけを出し、押すと本文を開け閉めする。
/// </summary>
public sealed class HistoryView
{
    private const float DrawerWidth = 300;

    private readonly TimeControl _timeControl;
    private readonly Button _button;
    private readonly PanelContainer _panel;
    private readonly VBoxContainer _rows;
    private readonly Label _teachings;
    private string _teachingsShown = string.Empty;

    // 本文を開いている章（始まりの年と終わりの年。神話の文が後から付いて行が入れ替わっても保てる）
    private readonly HashSet<(int FromYear, int ToYear)> _expanded = [];

    // 描いている年表の行（同じ行が同じ順にあれば組み立て直さない。神話の文は途中の章の行にも後から付く）
    private HistoryEntry[] _shown = [];
    private bool _dirty;

    /// <param name="owner">ノードを探すためのシーンの根。</param>
    public HistoryView(Node owner, TimeControl timeControl)
    {
        _timeControl = timeControl;
        _button = owner.GetNode<Button>("%HistoryButton");

        _rows = new VBoxContainer { Name = "HistoryList", SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        _teachings = new Label { Name = "Teachings", AutowrapMode = TextServer.AutowrapMode.WordSmart, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        var scroll = new ScrollContainer
        {
            Name = "HistoryScroll",
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
            SizeFlagsVertical = Control.SizeFlags.ExpandFill,
        };
        scroll.AddChild(_rows);
        // 今の教えが増えても年表を押しつぶさないよう、教えの欄は引き出しの 3 分の 1 までにしてスクロールさせ、
        // 増えたときはいちばん新しい教え（いちばん下）を見せる
        var teachingsScroll = new ScrollContainer
        {
            Name = "TeachingsScroll",
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
            SizeFlagsVertical = Control.SizeFlags.ExpandFill,
            SizeFlagsStretchRatio = 1,
        };
        teachingsScroll.AddChild(_teachings);
        var teachingsBar = teachingsScroll.GetVScrollBar();
        // 範囲が変わった直後はスクロールの幅（page）がまだ古いので、並べ終わってから合わせる
        teachingsBar.Changed += () => Callable.From(() => teachingsScroll.ScrollVertical = (int)teachingsBar.MaxValue).CallDeferred();
        scroll.SizeFlagsStretchRatio = 2;
        var column = new VBoxContainer { Name = "HistoryColumn" };
        column.AddChild(new Label { Name = "TeachingsTitle", Text = "HUD_DOGMAS" });
        column.AddChild(teachingsScroll);
        column.AddChild(new HSeparator());
        column.AddChild(new Label { Name = "HistoryTitle", Text = "HISTORY_TITLE" });
        column.AddChild(scroll);
        // 左の引き出し: 開いている間だけ、上の帯と下の帯の間に地図に重ねて出す
        _panel = new PanelContainer { Name = "HistoryPanel", Visible = false, ThemeTypeVariation = "Drawer" };
        _panel.AddChild(column);
        owner.GetNode<Node>("Hud").AddChild(_panel);
        HudLayout.PlaceDrawer(_panel, right: false, DrawerWidth);

        _button.Toggled += open =>
        {
            _panel.Visible = open;
            Refresh();
        };
        _button.Visible = timeControl.History is not null;
    }

    /// <summary>引き出しを閉じる（字を組む板を開いたとき）。</summary>
    public void Close() => _button.ButtonPressed = false;

    /// <summary>毎フレーム呼ぶ。開いているときだけ、行が増えたら組み立て直す。</summary>
    public void Process()
    {
        if (_panel.Visible)
        {
            Refresh();
        }
    }

    private void Refresh()
    {
        RefreshTeachings();
        var entries = _timeControl.History?.Entries ?? [];
        if (!_dirty && entries.Count == _shown.Length && entries.Select((entry, i) => ReferenceEquals(entry, _shown[i])).All(same => same))
        {
            return;
        }
        _dirty = false;
        _shown = [.. entries];
        foreach (var child in _rows.GetChildren())
        {
            _rows.RemoveChild(child);
            child.QueueFree();
        }
        if (entries.Count == 0)
        {
            _rows.AddChild(NewLabel(ChronicleText.Tr("HISTORY_EMPTY")));
            return;
        }
        foreach (var entry in entries.Reverse())
        {
            if (entry.Type == HistoryEventType.ChapterEnded && entry.Myth is { } myth)
            {
                AddChapterRow(entry, myth);
            }
            else
            {
                _rows.AddChild(NewLabel(ChronicleText.LineOf(entry)));
            }
        }
    }

    // 今の教えと信者の数（信者の数は年が進むと変わるので、年と教えの数が変わったときだけ数え直す）
    private void RefreshTeachings()
    {
        var signature = $"{_timeControl.World.Year}/{_timeControl.Dogmas.Count}";
        if (signature == _teachingsShown)
        {
            return;
        }
        _teachingsShown = signature;
        var culture = CultureInfo.CurrentCulture;
        _teachings.Text = _timeControl.Dogmas.Count == 0
            ? ChronicleText.Tr("HUD_NO_DOGMA")
            : string.Join("\n", _timeControl.Dogmas.Select(dogma => string.Format(
                culture,
                ChronicleText.Tr("DOGMA_ENTRY_BELIEVERS"),
                ChronicleText.TeachingOf(dogma.TargetSymbolId, dogma.Type, dogma.Reading),
                _timeControl.BelieversOf(dogma))));
    }

    // 神話のある章: 見出しの行を押すと、本文を開け閉めする
    private void AddChapterRow(HistoryEntry entry, MythText myth)
    {
        var key = (entry.FromYear ?? 0, entry.Year);
        var open = _expanded.Contains(key);
        var heading = new Button
        {
            Text = (open ? "▼ " : "▶ ") + ChronicleText.LineOf(entry),
            Flat = true,
            Alignment = HorizontalAlignment.Left,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
        };
        heading.Pressed += () =>
        {
            if (!_expanded.Remove(key))
            {
                _expanded.Add(key);
            }
            _dirty = true;
        };
        _rows.AddChild(heading);
        if (open)
        {
            _rows.AddChild(NewLabel(myth.Body));
        }
    }

    private static Label NewLabel(string text) => new()
    {
        Text = text,
        AutowrapMode = TextServer.AutowrapMode.WordSmart,
        SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
    };
}
