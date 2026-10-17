using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using TheogonyX.Core;

namespace TheogonyX.Scenes.Main;

/// <summary>
/// 字を組んで神託を下す欄（docs/domain/script-decipher.md の「書く」）。手帳に載った部品だけを並べ、押した部品を、選んだ場所
/// （左か上・右か下・下の小さな部品）に置く。並び（左右か上下）も選べる。組んだ字は 3 つまで足せ、「語りかける」で <see cref="Submitted"/> を知らせる。
/// 部品の意味は見せない（神の手帳の書き込みだけを、部品の下に小さく出す）。
/// </summary>
public sealed class ScriptComposerView
{
    private enum Slot
    {
        First,
        Second,
        Direction,
    }

    private readonly ProphetDreams _prophet;
    private readonly PanelContainer _panel;
    private readonly HBoxContainer _composed;
    private readonly GlyphView _current;
    private readonly HFlowContainer _palette;
    private readonly Button _arrangementButton;
    private readonly Dictionary<Slot, Button> _slotButtons = [];
    private readonly Button _addButton;
    private readonly Button _speakButton;
    private readonly List<Glyph> _glyphs = [];
    private string? _first;
    private string? _second;
    private string? _direction;
    private GlyphArrangement _arrangement = GlyphArrangement.LeftRight;
    private Slot _slot = Slot.First;
    private string _paletteShown = string.Empty;

    /// <param name="owner">ノードを探すためのシーンの根。</param>
    public ScriptComposerView(Node owner, ProphetDreams prophet)
    {
        _prophet = prophet;
        _composed = new HBoxContainer { Name = "ComposedGlyphs" };
        _current = new GlyphView { Name = "CurrentGlyph", CustomMinimumSize = new Vector2(40, 40) };
        var top = new HBoxContainer();
        top.AddChild(_composed);
        top.AddChild(new Label { Text = "COMPOSE_CURRENT" });
        top.AddChild(_current);

        var slots = new HBoxContainer();
        foreach (var (slot, key) in new[] { (Slot.First, "COMPOSE_SLOT_FIRST"), (Slot.Second, "COMPOSE_SLOT_SECOND"), (Slot.Direction, "COMPOSE_SLOT_DIRECTION") })
        {
            var button = new Button { Text = key, ToggleMode = true };
            button.Pressed += () =>
            {
                _slot = slot;
                Redraw();
            };
            _slotButtons[slot] = button;
            slots.AddChild(button);
        }
        _arrangementButton = new Button();
        _arrangementButton.Pressed += () =>
        {
            _arrangement = _arrangement == GlyphArrangement.LeftRight ? GlyphArrangement.TopBottom : GlyphArrangement.LeftRight;
            Redraw();
        };
        slots.AddChild(_arrangementButton);

        _palette = new HFlowContainer { Name = "Palette", CustomMinimumSize = new Vector2(420, 0) };

        var actions = new HBoxContainer();
        _addButton = new Button { Text = "COMPOSE_ADD" };
        _addButton.Pressed += AddCurrent;
        var clear = new Button { Text = "COMPOSE_CLEAR" };
        clear.Pressed += Clear;
        _speakButton = new Button { Text = "BUTTON_SPEAK" };
        _speakButton.Pressed += Submit;
        var close = new Button { Text = "BUTTON_CLOSE" };
        close.Pressed += () => _panel!.Visible = false;
        actions.AddChild(_addButton);
        actions.AddChild(clear);
        actions.AddChild(_speakButton);
        actions.AddChild(close);

        var column = new VBoxContainer();
        column.AddChild(new Label { Text = "COMPOSE_TITLE" });
        column.AddChild(top);
        column.AddChild(slots);
        column.AddChild(_palette);
        column.AddChild(actions);
        _panel = new PanelContainer { Name = "ComposerPanel", Visible = false, Position = new Vector2(16, 96) };
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
        Clear();
        _panel.Visible = false;
    }

    /// <summary>手帳が変わったかもしれないときに呼ぶ（部品が増えた・書き込みが変わった）。</summary>
    public void Redraw()
    {
        if (_prophet.Script is not { } script)
        {
            return;
        }
        RebuildPalette(script);
        foreach (var (slot, button) in _slotButtons)
        {
            button.ButtonPressed = slot == _slot;
        }
        _arrangementButton.Text = _arrangement == GlyphArrangement.LeftRight ? "COMPOSE_ARRANGE_LR" : "COMPOSE_ARRANGE_TB";
        if (CurrentGlyph() is { } current)
        {
            _current.Show(script.Script, current, script.Decayed);
        }
        else
        {
            _current.Show(script.Script, new Glyph(string.Empty, null, null, _arrangement), script.Decayed);
        }
        foreach (var child in _composed.GetChildren())
        {
            _composed.RemoveChild(child);
            child.QueueFree();
        }
        foreach (var glyph in _glyphs)
        {
            var view = new GlyphView { CustomMinimumSize = new Vector2(32, 32) };
            view.Show(script.Script, glyph, script.Decayed);
            _composed.AddChild(view);
        }
        _addButton.Disabled = CurrentGlyph() is null || _glyphs.Count >= ScriptRules.MaxGlyphsPerOracle;
        _speakButton.Disabled = (_glyphs.Count == 0 && CurrentGlyph() is null) || _prophet.SpeakRejection() is not null;
    }

    private Glyph? CurrentGlyph() => _first is null ? null : new Glyph(_first, _second, _direction, _arrangement);

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
            cell.AddChild(new Label { Text = NotebookView.LabelText(label), ThemeTypeVariation = "TooltipLabel" });
            _palette.AddChild(cell);
        }
    }

    // 選んだ場所に部品を置き、左か上に置いたら、次は右か下にする
    private void Place(string radicalId)
    {
        switch (_slot)
        {
            case Slot.First:
                _first = radicalId;
                _slot = Slot.Second;
                break;
            case Slot.Second:
                _second = radicalId;
                break;
            default:
                _direction = radicalId;
                break;
        }
        Redraw();
    }

    private void AddCurrent()
    {
        if (CurrentGlyph() is { } glyph && _glyphs.Count < ScriptRules.MaxGlyphsPerOracle)
        {
            _glyphs.Add(glyph);
            (_first, _second, _direction, _slot) = (null, null, null, Slot.First);
            Redraw();
        }
    }

    private void Clear()
    {
        _glyphs.Clear();
        (_first, _second, _direction, _slot) = (null, null, null, Slot.First);
        Redraw();
    }

    // 組みかけの字があれば、それも足して送る
    private void Submit()
    {
        var glyphs = _glyphs.ToList();
        if (CurrentGlyph() is { } current && glyphs.Count < ScriptRules.MaxGlyphsPerOracle)
        {
            glyphs.Add(current);
        }
        if (glyphs.Count > 0)
        {
            Submitted?.Invoke(glyphs);
        }
    }
}
