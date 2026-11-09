using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Godot;
using TheogonyX.Core;

namespace TheogonyX.Scenes.Main;

/// <summary>
/// 文を書いて神託を下す欄（docs/domain/script-decipher.md の「7. 語の字と神託の碑」）。手帳の字を押すと文の後ろに足され、
/// 文の字を押すとその字が消える。「語りかける」で <see cref="Submitted"/> を知らせる。
/// 字の意味は見せない（神の手帳の書き込みだけを、字の下に小さく出す）。
/// 予言者のいる地域の今の危機を出し、崩れで混ざった組の字は 1 つにまとめて出す（docs/domain/script-decipher.md の 9）。
/// </summary>
public sealed class ScriptComposerView
{
    private const float PanelWidth = 560;

    private readonly ProphetDreams _prophet;
    private readonly PanelContainer _panel;
    private readonly Label _hint;
    private readonly Label _crisis;
    private readonly HFlowContainer _palette;
    private readonly GlyphView[] _slots = new GlyphView[ScriptRules.MaxGlyphsPerOracle];
    private readonly Button _speakButton;
    private readonly List<string> _words = [];
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

        _crisis = new Label { Name = "ComposeCrisis" };

        _palette = new HFlowContainer { Name = "Palette", SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };

        var bottom = new HBoxContainer();
        bottom.AddChild(new Label { Text = "COMPOSE_SENTENCE" });
        for (var i = 0; i < _slots.Length; i++)
        {
            var index = i;
            _slots[i] = new GlyphView { CustomMinimumSize = new Vector2(40, 40), MouseFilter = Control.MouseFilterEnum.Stop };
            _slots[i].GuiInput += input =>
            {
                if (input is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left } && index < _words.Count)
                {
                    _words.RemoveAt(index);
                    Redraw();
                }
            };
            bottom.AddChild(_slots[i]);
        }
        bottom.AddChild(new Control { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill });
        _speakButton = new Button { Text = "BUTTON_SPEAK" };
        _speakButton.Pressed += () => Submitted?.Invoke(new Sentence([.. _words]));
        bottom.AddChild(_speakButton);

        var column = new VBoxContainer();
        column.AddChild(header);
        column.AddChild(_hint);
        column.AddChild(_crisis);
        column.AddChild(new Label { Text = "COMPOSE_PALETTE" });
        column.AddChild(_palette);
        column.AddChild(new HSeparator());
        column.AddChild(bottom);
        // 下の帯のすぐ上に、下から出す板（右の手帳の引き出しと重ならない幅）
        _panel = new PanelContainer { Name = "ComposerPanel", Visible = false, ThemeTypeVariation = "Sheet" };
        _panel.AddChild(column);
        owner.GetNode<Node>("Hud").AddChild(_panel);
        _panel.SetAnchorsPreset(Control.LayoutPreset.BottomLeft);
        _panel.GrowVertical = Control.GrowDirection.Begin;
        _panel.OffsetLeft = 8;
        _panel.OffsetRight = 8 + PanelWidth;
        _panel.OffsetBottom = -HudLayout.BottomBarHeight;
        _panel.OffsetTop = -HudLayout.BottomBarHeight;
        Redraw();
    }

    /// <summary>書いた文で「語りかける」を押した。</summary>
    public event Action<Sentence>? Submitted;

    /// <summary>欄を開け閉めする。</summary>
    public void Toggle()
    {
        _panel.Visible = !_panel.Visible;
        Redraw();
    }

    /// <summary>神託を下した後に呼ぶ。書いた文を消し、欄を閉じる。</summary>
    public void Reset()
    {
        _words.Clear();
        _panel.Visible = false;
        Redraw();
    }

    /// <summary>手帳が変わったかもしれないときに呼ぶ（字が増えた・書き込みが変わった）。</summary>
    public void Redraw()
    {
        if (_prophet.Script is not { } script)
        {
            return;
        }
        RebuildPalette(script);
        for (var i = 0; i < _slots.Length; i++)
        {
            _slots[i].Show(script.Script, new Sentence(i < _words.Count ? [_words[i]] : []), script.Stage);
            _slots[i].Outline = i <= _words.Count ? new Color(0.87f, 0.93f, 0.84f, 0.3f) : Colors.Transparent;
        }
        _speakButton.Disabled = _words.Count == 0 || _prophet.SpeakRejection() is not null;
        _hint.Text = script.Notebook.Count == 0 ? Tr("COMPOSE_HINT_EMPTY")
            : _words.Count == 0 ? Tr("COMPOSE_HINT_PLACE")
            : _words.Count < _slots.Length ? Tr("COMPOSE_HINT_READY")
            : Tr("COMPOSE_HINT_FULL");
        _crisis.Text = string.Format(
            CultureInfo.CurrentCulture, Tr("CRISIS_LABEL"), Tr($"CRISIS_{_prophet.CurrentCrisis().ToString().ToUpperInvariant()}"));
    }

    // 手帳に載った字を並べる。字の下に、神の書き込み（意味の推理）を小さく出す。
    // 混ざった組の字は形がもう同じなので 1 つにまとめ、書き込みを「/」でつなぐ。押すと組の中で手帳にある ID の小さい方を足す
    private void RebuildPalette(ScriptState script)
    {
        var signature = string.Join(',', script.Notebook.OrderBy(pair => pair.Key, StringComparer.Ordinal).Select(pair => $"{pair.Key}={pair.Value}")) + script.Stage;
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
        var groups = script.Notebook.OrderBy(pair => pair.Key, StringComparer.Ordinal)
            .GroupBy(pair => ScriptReader.ReadAs(script.Script, pair.Key, script.Stage));
        foreach (var group in groups)
        {
            var id = group.First().Key;
            var label = string.Join(" / ", group.Select(pair => NotebookView.LabelText(pair.Value)).Distinct());
            var cell = new VBoxContainer();
            var view = new GlyphView { CustomMinimumSize = new Vector2(32, 32), MouseFilter = Control.MouseFilterEnum.Stop, TooltipText = label };
            view.Show(script.Script, id, script.Stage);
            var radicalId = id;
            view.GuiInput += input =>
            {
                if (input is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left } && _words.Count < _slots.Length)
                {
                    _words.Add(radicalId);
                    Redraw();
                }
            };
            cell.AddChild(view);
            cell.AddChild(new Label { Text = label, ThemeTypeVariation = "TooltipLabel", HorizontalAlignment = HorizontalAlignment.Center });
            _palette.AddChild(cell);
        }
    }

    private static string Tr(string key) => TranslationServer.Translate(key).ToString();
}
