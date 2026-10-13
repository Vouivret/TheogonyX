using System.Collections.Generic;
using System.Linq;
using Godot;
using TheogonyX.Core;

namespace TheogonyX.Scenes.Main;

/// <summary>
/// 星の年表（docs/domain/history.md）。HUD のボタンで開け閉めし、新しい行を上にして欄に出す。
/// 年表はコアの <see cref="TimeControl.History"/> が持ち、ここでは文に組み立てて見せるだけにする。
/// 神話の文がある章の行は、見出しだけを出し、押すと本文を開け閉めする。
/// </summary>
public sealed class HistoryView
{
    private readonly TimeControl _timeControl;
    private readonly Button _button;
    private readonly PanelContainer _panel;
    private readonly VBoxContainer _rows;

    // 本文を開いている章（始まりの年と終わりの年。神話の文が後から付いて行が入れ替わっても保てる）
    private readonly HashSet<(int FromYear, int ToYear)> _expanded = [];

    // 描いている年表の行（同じ行が同じ順にあれば組み立て直さない。神話の文は途中の章の行にも後から付く）
    private HistoryEntry[] _shown = [];
    private bool _dirty;

    /// <param name="owner">ノードを探すためのシーンの根。</param>
    public HistoryView(Node owner, TimeControl timeControl)
    {
        _timeControl = timeControl;
        _button = new Button { Name = "HistoryButton", Text = "BUTTON_HISTORY" };
        owner.GetNode<Node>("Hud/Panel/Buttons").AddChild(_button);

        _rows = new VBoxContainer { Name = "HistoryList", SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        var scroll = new ScrollContainer
        {
            Name = "HistoryScroll",
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
            SizeFlagsVertical = Control.SizeFlags.ExpandFill,
        };
        scroll.AddChild(_rows);
        var column = new VBoxContainer { Name = "HistoryColumn" };
        column.AddChild(new Label { Name = "HistoryTitle", Text = "HISTORY_TITLE" });
        column.AddChild(scroll);
        _panel = new PanelContainer
        {
            Name = "HistoryPanel",
            Visible = false,
            // QR コード・接続先のボタンの下から、夢の欄の上まで（住人の欄と同じ位置に重ねて出す）
            Position = new Vector2(16, 60),
            Size = new Vector2(264, 140),
        };
        _panel.AddChild(column);
        owner.GetNode<Node>("Hud").AddChild(_panel);

        _button.Pressed += () =>
        {
            _panel.Visible = !_panel.Visible;
            Refresh();
        };
        _button.Visible = timeControl.History is not null;
    }

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
