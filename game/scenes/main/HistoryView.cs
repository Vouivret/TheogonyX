using System.Linq;
using Godot;
using TheogonyX.Core;

namespace TheogonyX.Scenes.Main;

/// <summary>
/// 星の年表（docs/domain/history.md）。HUD のボタンで開け閉めし、新しい行を上にして欄に出す。
/// 年表はコアの <see cref="TimeControl.History"/> が持ち、ここでは文に組み立てて見せるだけにする。
/// </summary>
public sealed class HistoryView
{
    private readonly TimeControl _timeControl;
    private readonly Button _button;
    private readonly PanelContainer _panel;
    private readonly Label _label;

    // 描いている年表の行（同じ行が同じ順にあれば組み立て直さない。神話の文は途中の章の行にも後から付く）
    private HistoryEntry[] _shown = [];

    /// <param name="owner">ノードを探すためのシーンの根。</param>
    public HistoryView(Node owner, TimeControl timeControl)
    {
        _timeControl = timeControl;
        _button = new Button { Name = "HistoryButton", Text = "BUTTON_HISTORY" };
        owner.GetNode<Node>("Hud/Panel/Buttons").AddChild(_button);

        _label = new Label { Name = "HistoryList", AutowrapMode = TextServer.AutowrapMode.WordSmart };
        var scroll = new ScrollContainer
        {
            Name = "HistoryScroll",
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
            SizeFlagsVertical = Control.SizeFlags.ExpandFill,
        };
        _label.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        scroll.AddChild(_label);
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
        if (entries.Count == _shown.Length && entries.Select((entry, i) => ReferenceEquals(entry, _shown[i])).All(same => same))
        {
            return;
        }
        _shown = [.. entries];
        _label.Text = entries.Count == 0
            ? ChronicleText.Tr("HISTORY_EMPTY")
            : string.Join('\n', entries.Reverse().Select(ChronicleText.LineOf));
    }

}
