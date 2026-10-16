using System.Globalization;
using System.Linq;
using Godot;
using TheogonyX.Core;

namespace TheogonyX.Scenes.Main;

/// <summary>
/// 予言者の方言帳（docs/domain/oracle-pair.md の「方言帳」）。HUD のボタンで開け閉めし、神託で分かった聞き違えを、新しい行を上にして出す。
/// 1 行は「神の言葉 → 予言者に聞こえた言葉（理由）」。中身はコアの <see cref="DialectState.Book"/> が持ち、ここでは見せるだけにする。
/// </summary>
public sealed class DialectBookView
{
    private readonly ProphetDreams _prophet;
    private readonly Button _button;
    private readonly PanelContainer _panel;
    private readonly VBoxContainer _rows;
    private int _shownCount = -1;

    /// <param name="owner">ノードを探すためのシーンの根。</param>
    public DialectBookView(Node owner, ProphetDreams prophet)
    {
        _prophet = prophet;
        _button = new Button { Name = "DialectButton", Text = "BUTTON_DIALECT", Visible = prophet.Dialect is not null };
        owner.GetNode<Node>("Hud/Panel/Buttons").AddChild(_button);

        _rows = new VBoxContainer { Name = "DialectList", SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        var scroll = new ScrollContainer
        {
            Name = "DialectScroll",
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
            SizeFlagsVertical = Control.SizeFlags.ExpandFill,
        };
        scroll.AddChild(_rows);
        var column = new VBoxContainer { Name = "DialectColumn" };
        column.AddChild(new Label { Name = "DialectTitle", Text = "BOOK_TITLE" });
        column.AddChild(scroll);
        _panel = new PanelContainer { Name = "DialectPanel", Visible = false, Position = new Vector2(16, 60), Size = new Vector2(264, 140) };
        _panel.AddChild(column);
        owner.GetNode<Node>("Hud").AddChild(_panel);

        _button.Pressed += () =>
        {
            _panel.Visible = !_panel.Visible;
            Refresh();
        };
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
        var entries = _prophet.Dialect?.Book.Entries ?? [];
        if (entries.Count == _shownCount)
        {
            return;
        }
        _shownCount = entries.Count;
        foreach (var child in _rows.GetChildren())
        {
            _rows.RemoveChild(child);
            child.QueueFree();
        }
        if (entries.Count == 0)
        {
            _rows.AddChild(NewLabel(Tr("BOOK_EMPTY")));
            return;
        }
        foreach (var entry in entries.Reverse())
        {
            _rows.AddChild(NewLabel(LineOf(entry)));
        }
    }

    private static string LineOf(DialectEntry entry)
    {
        var culture = CultureInfo.CurrentCulture;
        return entry.Reason switch
        {
            DialectReason.Mystery => string.Format(culture, Tr("BOOK_ROW_MYSTERY"), entry.GodWord),
            DialectReason.Unknown => string.Format(culture, Tr("BOOK_ROW_UNKNOWN"), Tr($"WORD_{entry.GodWord}"), Tr($"WORD_{entry.HeardWordId}")),
            _ => string.Format(culture, Tr("BOOK_ROW_DIALECT"), Tr($"WORD_{entry.GodWord}"), Tr($"WORD_{entry.HeardWordId}")),
        };
    }

    private static Label NewLabel(string text) => new()
    {
        Text = text,
        AutowrapMode = TextServer.AutowrapMode.WordSmart,
        SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
    };

    private static string Tr(string key) => TranslationServer.Translate(key).ToString();
}
