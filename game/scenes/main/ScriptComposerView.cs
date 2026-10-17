using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using TheogonyX.Core;

namespace TheogonyX.Scenes.Main;

/// <summary>
/// 字を組んで神託を下す欄（docs/domain/script-decipher.md の「書く」）。組んでいる字を大きく描き、その中の光る枠に、
/// 押した手帳の部品が入る。枠を押すと入れる場所を選べ、選んだ枠をもう一度押すと空く。並べ方（横か縦）はボタンで選ぶ。
/// 下の「語る言葉」には字が 3 つまで並び、押した字を組み直せる。「語りかける」で <see cref="Submitted"/> を知らせる。
/// 部品の意味は見せない（神の手帳の書き込みだけを、部品の下に小さく出す）。
/// </summary>
public sealed class ScriptComposerView
{
    private sealed class Draft
    {
        public string? First;
        public string? Second;
        public string? Direction;
        public GlyphArrangement Arrangement = GlyphArrangement.LeftRight;

        // 2 つの枠の片方だけに部品があれば、部品 1 つの字にする。方角だけでは字にならない
        public Glyph? ToGlyph() => (First, Second) switch
        {
            ({ } first, var second) => new Glyph(first, second, Direction, Arrangement),
            (null, { } second) => new Glyph(second, null, Direction, Arrangement),
            _ => null,
        };
    }

    private readonly ProphetDreams _prophet;
    private readonly PanelContainer _panel;
    private readonly Label _hint;
    private readonly GlyphEditorView _editor;
    private readonly Button _sideBySide;
    private readonly Button _stacked;
    private readonly HFlowContainer _palette;
    private readonly GlyphView[] _sentence = new GlyphView[ScriptRules.MaxGlyphsPerOracle];
    private readonly Button _speakButton;
    private readonly Draft[] _drafts = Enumerable.Range(0, ScriptRules.MaxGlyphsPerOracle).Select(_ => new Draft()).ToArray();
    private int _editing;
    private GlyphSlot _slot = GlyphSlot.First;
    private string _paletteShown = string.Empty;

    /// <param name="owner">ノードを探すためのシーンの根。</param>
    public ScriptComposerView(Node owner, ProphetDreams prophet)
    {
        _prophet = prophet;
        var close = new Button { Text = "BUTTON_CLOSE" };
        close.Pressed += () => _panel!.Visible = false;
        var header = new HBoxContainer();
        header.AddChild(new Label { Text = "COMPOSE_TITLE", SizeFlagsHorizontal = Control.SizeFlags.ExpandFill });
        header.AddChild(close);
        _hint = new Label { Name = "ComposeHint", AutowrapMode = TextServer.AutowrapMode.WordSmart, Modulate = new Color(1f, 0.85f, 0.3f) };

        _editor = new GlyphEditorView { Name = "GlyphEditor", CustomMinimumSize = new Vector2(84, 84), SizeFlagsVertical = Control.SizeFlags.ShrinkBegin };
        _editor.SlotPressed += PressSlot;
        var group = new ButtonGroup();
        _sideBySide = new Button { Text = "COMPOSE_ARRANGE_LR", ToggleMode = true, ButtonGroup = group };
        _sideBySide.Pressed += () => Arrange(GlyphArrangement.LeftRight);
        _stacked = new Button { Text = "COMPOSE_ARRANGE_TB", ToggleMode = true, ButtonGroup = group };
        _stacked.Pressed += () => Arrange(GlyphArrangement.TopBottom);
        var empty = new Button { Text = "COMPOSE_CLEAR_GLYPH" };
        empty.Pressed += ClearEditing;
        var tools = new VBoxContainer();
        tools.AddChild(_sideBySide);
        tools.AddChild(_stacked);
        tools.AddChild(empty);

        _palette = new HFlowContainer { Name = "Palette", SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        var paletteColumn = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        paletteColumn.AddChild(new Label { Text = "COMPOSE_PALETTE" });
        paletteColumn.AddChild(_palette);
        var middle = new HBoxContainer();
        middle.AddChild(_editor);
        middle.AddChild(tools);
        middle.AddChild(new VSeparator());
        middle.AddChild(paletteColumn);

        var bottom = new HBoxContainer();
        bottom.AddChild(new Label { Text = "COMPOSE_SENTENCE" });
        for (var i = 0; i < _sentence.Length; i++)
        {
            var index = i;
            _sentence[i] = new GlyphView { CustomMinimumSize = new Vector2(28, 28), MouseFilter = Control.MouseFilterEnum.Stop };
            _sentence[i].GuiInput += input =>
            {
                if (input is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left })
                {
                    Edit(index);
                }
            };
            bottom.AddChild(_sentence[i]);
        }
        bottom.AddChild(new Control { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill });
        _speakButton = new Button { Text = "BUTTON_SPEAK" };
        _speakButton.Pressed += Submit;
        bottom.AddChild(_speakButton);

        var column = new VBoxContainer();
        column.AddChild(header);
        column.AddChild(_hint);
        column.AddChild(middle);
        column.AddChild(new HSeparator());
        column.AddChild(bottom);
        _panel = new PanelContainer { Name = "ComposerPanel", Visible = false, Position = new Vector2(8, 40), CustomMinimumSize = new Vector2(464, 0) };
        _panel.AddThemeStyleboxOverride("panel", new StyleBoxFlat
        {
            BgColor = new Color(0.12f, 0.1f, 0.1f, 0.97f),
            ContentMarginLeft = 6,
            ContentMarginRight = 6,
            ContentMarginTop = 4,
            ContentMarginBottom = 4,
        });
        _panel.AddChild(column);
        owner.GetNode<Node>("Hud").AddChild(_panel);
        Redraw();
    }

    /// <summary>組んだ字で「語りかける」を押した。</summary>
    public event Action<IReadOnlyList<Glyph>>? Submitted;

    /// <summary>欄を開け閉めする。</summary>
    public void Toggle()
    {
        _panel.Visible = !_panel.Visible;
        Redraw();
    }

    /// <summary>神託を下した後に呼ぶ。組んだ字を消し、欄を閉じる。</summary>
    public void Reset()
    {
        for (var i = 0; i < _drafts.Length; i++)
        {
            _drafts[i] = new Draft();
        }
        (_editing, _slot) = (0, GlyphSlot.First);
        _panel.Visible = false;
        Redraw();
    }

    /// <summary>手帳が変わったかもしれないときに呼ぶ（部品が増えた・書き込みが変わった）。</summary>
    public void Redraw()
    {
        if (_prophet.Script is not { } script)
        {
            return;
        }
        RebuildPalette(script);
        var draft = _drafts[_editing];
        _editor.Show(script.Script, draft.First, draft.Second, draft.Direction, draft.Arrangement, _slot, script.Decayed);
        _sideBySide.SetPressedNoSignal(draft.Arrangement == GlyphArrangement.LeftRight);
        _stacked.SetPressedNoSignal(draft.Arrangement == GlyphArrangement.TopBottom);
        for (var i = 0; i < _sentence.Length; i++)
        {
            var glyph = _drafts[i].ToGlyph();
            _sentence[i].Show(script.Script, glyph ?? new Glyph(string.Empty, null, null, GlyphArrangement.LeftRight), script.Decayed);
            _sentence[i].Outline = i == _editing ? new Color(1f, 0.85f, 0.3f) : CanEdit(i) ? new Color(0.87f, 0.93f, 0.84f, 0.3f) : Colors.Transparent;
            _sentence[i].QueueRedraw();
        }
        var glyphs = Glyphs();
        _speakButton.Disabled = glyphs.Count == 0 || _prophet.SpeakRejection() is not null;
        _hint.Text = script.Notebook.Count == 0 ? Tr("COMPOSE_HINT_EMPTY")
            : draft.ToGlyph() is null ? Tr("COMPOSE_HINT_PLACE")
            : _editing + 1 < _drafts.Length ? Tr("COMPOSE_HINT_READY")
            : Tr("COMPOSE_HINT_FULL");
    }

    // 前の字ができていれば、次の字を組める
    private bool CanEdit(int index) => index == 0 || _drafts[index - 1].ToGlyph() is not null;

    // できている字を、前から順に（空いた所で止めずに）集める
    private List<Glyph> Glyphs() => _drafts.Select(draft => draft.ToGlyph()).OfType<Glyph>().ToList();

    // 手帳に載った部品を並べる。部品の下に、神の書き込み（意味の推理）を小さく出す
    private void RebuildPalette(ScriptState script)
    {
        var signature = string.Join(',', script.Notebook.OrderBy(pair => pair.Key, StringComparer.Ordinal).Select(pair => $"{pair.Key}={pair.Value}")) + script.Decayed;
        if (signature == _paletteShown)
        {
            return;
        }
        _paletteShown = signature;
        foreach (var child in _palette.GetChildren())
        {
            _palette.RemoveChild(child);
            child.QueueFree();
        }
        foreach (var (id, label) in script.Notebook.OrderBy(pair => pair.Key, StringComparer.Ordinal))
        {
            var cell = new VBoxContainer();
            var view = new GlyphView { CustomMinimumSize = new Vector2(28, 28), MouseFilter = Control.MouseFilterEnum.Stop, TooltipText = NotebookView.LabelText(label) };
            view.Show(script.Script, new Glyph(id, null, null, GlyphArrangement.LeftRight), script.Decayed);
            var radicalId = id;
            view.GuiInput += input =>
            {
                if (input is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left })
                {
                    Place(radicalId);
                }
            };
            cell.AddChild(view);
            cell.AddChild(new Label { Text = NotebookView.LabelText(label), ThemeTypeVariation = "TooltipLabel", HorizontalAlignment = HorizontalAlignment.Center });
            _palette.AddChild(cell);
        }
    }

    // 光る枠に部品を入れる。左（上）の枠に入れたら、右（下）の枠が空いていれば次はそこを光らせる
    private void Place(string radicalId)
    {
        var draft = _drafts[_editing];
        switch (_slot)
        {
            case GlyphSlot.First:
                draft.First = radicalId;
                if (draft.Second is null)
                {
                    _slot = GlyphSlot.Second;
                }
                break;
            case GlyphSlot.Second:
                draft.Second = radicalId;
                break;
            default:
                draft.Direction = radicalId;
                break;
        }
        Redraw();
    }

    // 枠を押したら、そこを光らせる。光っている枠をもう一度押したら、その部品を外す
    private void PressSlot(GlyphSlot slot)
    {
        var draft = _drafts[_editing];
        if (slot == _slot && slot == GlyphSlot.First)
        {
            draft.First = null;
        }
        else if (slot == _slot && slot == GlyphSlot.Second)
        {
            draft.Second = null;
        }
        else if (slot == _slot)
        {
            draft.Direction = null;
        }
        _slot = slot;
        Redraw();
    }

    private void Arrange(GlyphArrangement arrangement)
    {
        _drafts[_editing].Arrangement = arrangement;
        Redraw();
    }

    private void Edit(int index)
    {
        if (!CanEdit(index))
        {
            return;
        }
        _editing = index;
        _slot = _drafts[index].First is null ? GlyphSlot.First : _drafts[index].Second is null ? GlyphSlot.Second : GlyphSlot.First;
        Redraw();
    }

    private void ClearEditing()
    {
        _drafts[_editing] = new Draft { Arrangement = _drafts[_editing].Arrangement };
        _slot = GlyphSlot.First;
        Redraw();
    }

    private void Submit()
    {
        var glyphs = Glyphs();
        if (glyphs.Count > 0)
        {
            Submitted?.Invoke(glyphs);
        }
    }

    private static string Tr(string key) => TranslationServer.Translate(key).ToString();
}
