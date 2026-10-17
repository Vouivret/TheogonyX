using System;
using System.Linq;
using Godot;
using TheogonyX.Core;

namespace TheogonyX.Scenes.Main;

/// <summary>
/// 字の手帳（docs/domain/script-decipher.md の「読む」）。手帳に載った部品ごとに、神の推理（意味の一覧から 1 つか「分からない」）を
/// 選んで書き込む。正解は見せない。書き込みは計算には使わず、字を組む欄で部品の下に出すだけ。
/// </summary>
public sealed class NotebookView
{
    // 手帳に書き込める意味（記号・種別・方角）と「分からない」。表示の順
    private static readonly string[] Labels =
    [
        NotebookLabels.Unknown, "SYM_TREE", "SYM_WATER", "SYM_FIRE", "SYM_EARTH", "SYM_METAL", "SYM_BLOOD",
        nameof(DogmaType.Worship), nameof(DogmaType.Taboo), nameof(DogmaType.Destruction), nameof(TravelDirection.East), nameof(TravelDirection.West),
    ];

    private readonly ProphetDreams _prophet;
    private readonly PanelContainer _panel;
    private readonly VBoxContainer _rows;
    private string _shown = string.Empty;

    /// <param name="owner">ノードを探すためのシーンの根。</param>
    public NotebookView(Node owner, ProphetDreams prophet)
    {
        _prophet = prophet;
        var button = new Button { Name = "NotebookButton", Text = "BUTTON_NOTEBOOK", Visible = prophet.Script is not null };
        owner.GetNode<Node>("Hud/Panel/Buttons").AddChild(button);
        _rows = new VBoxContainer { Name = "NotebookList", SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        var scroll = new ScrollContainer
        {
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
            SizeFlagsVertical = Control.SizeFlags.ExpandFill,
            CustomMinimumSize = new Vector2(176, 180),
        };
        scroll.AddChild(_rows);
        var column = new VBoxContainer();
        column.AddChild(new Label { Text = "NOTEBOOK_TITLE" });
        column.AddChild(scroll);
        _panel = new PanelContainer { Name = "NotebookPanel", Visible = false, Position = new Vector2(288, 16) };
        _panel.AddThemeStyleboxOverride("panel", new StyleBoxFlat { BgColor = new Color(0.12f, 0.1f, 0.1f), ContentMarginLeft = 4, ContentMarginRight = 4 });
        _panel.AddChild(column);
        owner.GetNode<Node>("Hud").AddChild(_panel);
        button.Pressed += () =>
        {
            _panel.Visible = !_panel.Visible;
            Refresh();
        };
    }

    /// <summary>書き込みを変えた（保存し直す・字を組む欄を描き直すのに使う）。</summary>
    public event Action? Changed;

    /// <summary>手帳の書き込みの表示名。</summary>
    public static string LabelText(string label) => label switch
    {
        NotebookLabels.Unknown => Tr("NOTEBOOK_UNKNOWN"),
        nameof(DogmaType.Worship) or nameof(DogmaType.Taboo) or nameof(DogmaType.Destruction) => Tr($"NOTEBOOK_TYPE_{label.ToUpperInvariant()}"),
        nameof(TravelDirection.East) or nameof(TravelDirection.West) => Tr($"DIRECTION_{label.ToUpperInvariant()}"),
        _ => Tr($"SYMBOL_{label}"),
    };

    /// <summary>毎フレーム呼ぶ。開いているときだけ、載った部品が増えたら組み立て直す。</summary>
    public void Process()
    {
        if (_panel.Visible)
        {
            Refresh();
        }
    }

    private void Refresh()
    {
        if (_prophet.Script is not { } script)
        {
            return;
        }
        var signature = string.Join(',', script.Notebook.Keys.Order(StringComparer.Ordinal)) + script.Decayed;
        if (signature == _shown)
        {
            return;
        }
        _shown = signature;
        foreach (var child in _rows.GetChildren())
        {
            _rows.RemoveChild(child);
            child.QueueFree();
        }
        if (script.Notebook.Count == 0)
        {
            _rows.AddChild(new Label { Text = Tr("NOTEBOOK_EMPTY"), AutowrapMode = TextServer.AutowrapMode.WordSmart });
            return;
        }
        foreach (var (id, label) in script.Notebook.OrderBy(pair => pair.Key, StringComparer.Ordinal))
        {
            var row = new HBoxContainer();
            var view = new GlyphView { CustomMinimumSize = new Vector2(28, 28) };
            view.Show(script.Script, new Glyph(id, null, null, GlyphArrangement.LeftRight), script.Decayed);
            var choice = new OptionButton { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
            foreach (var candidate in Labels)
            {
                choice.AddItem(LabelText(candidate));
            }
            choice.Selected = Math.Max(0, Array.IndexOf(Labels, label));
            var radicalId = id;
            choice.ItemSelected += index =>
            {
                script.Label(radicalId, Labels[index]);
                Changed?.Invoke();
            };
            row.AddChild(view);
            row.AddChild(choice);
            _rows.AddChild(row);
        }
    }

    private static string Tr(string key) => TranslationServer.Translate(key).ToString();
}
