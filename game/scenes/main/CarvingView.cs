using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Godot;
using TheogonyX.Core;

namespace TheogonyX.Scenes.Main;

/// <summary>
/// 世界に刻まれた字（docs/domain/script-decipher.md の「世界に刻まれた字」）を、地表の上に小さな印で描く。
/// 印を押すと拓本の欄に大きく出し、その文の字を手帳に載せる。神託の碑なら、刻んだときの人の読みと今の人の読みも出す
/// （docs/domain/script-decipher.md の 7）。中身はコアの <see cref="ScriptState.Carvings"/> が持つ。
/// </summary>
public sealed class CarvingView
{
    private const float MarkSize = 12f;

    private readonly ProphetDreams _prophet;
    private readonly Node2D _marks;
    private readonly Vector2 _terrainOrigin;
    private readonly PanelContainer _panel;
    private readonly GlyphView _rubbing;
    private readonly Label _info;
    private readonly Label _found;
    private readonly Label _readings;
    private readonly SubViewport _viewport;
    private readonly List<(Rect2 Area, CarvedGlyph Carving)> _hits = [];
    private string _shown = string.Empty;

    /// <param name="owner">ノードを探すためのシーンの根。</param>
    public CarvingView(Node owner, ProphetDreams prophet)
    {
        _prophet = prophet;
        var terrain = owner.GetNode<TileMapLayer>("%Terrain");
        _terrainOrigin = terrain.Position;
        _viewport = owner.GetNode<SubViewport>("%WorldViewport");
        // 建物・予言者より手前に描く（印が隠れないように）
        _marks = new Node2D { Name = "Carvings", ZIndex = 10 };
        terrain.GetParent().AddChild(_marks);

        _rubbing = new GlyphView { Name = "Rubbing", CustomMinimumSize = new Vector2(64, 64), SizeFlagsHorizontal = Control.SizeFlags.ShrinkBegin };
        _info = new Label { Name = "RubbingInfo", AutowrapMode = TextServer.AutowrapMode.WordSmart, CustomMinimumSize = new Vector2(160, 0) };
        _found = new Label { Name = "RubbingFound", AutowrapMode = TextServer.AutowrapMode.WordSmart, CustomMinimumSize = new Vector2(160, 0) };
        _readings = new Label { Name = "RubbingReadings", AutowrapMode = TextServer.AutowrapMode.WordSmart, CustomMinimumSize = new Vector2(300, 0) };
        var close = new Button { Name = "RubbingClose", Text = "BUTTON_CLOSE" };
        var column = new VBoxContainer();
        column.AddChild(new Label { Text = "RUBBING_TITLE" });
        column.AddChild(_rubbing);
        column.AddChild(_info);
        column.AddChild(_readings);
        column.AddChild(_found);
        column.AddChild(close);
        // 吹き出しより手前に出し、重なっても読めるように背景を塗る（字を組む欄と同じ色）
        _panel = new PanelContainer { Name = "RubbingPanel", Visible = false, Position = new Vector2(8, 40), ZIndex = 1 };
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
        close.Pressed += () => _panel.Visible = false;
    }

    /// <summary>拓本の欄で字を開いて、部品を手帳に載せた（保存し直すのに使う）。</summary>
    public event Action? Discovered;

    /// <summary>
    /// 画面の位置の印を押したら、拓本の欄を開く。印は SubViewport の中にあり、SubViewportContainer がマウスを通さないので、
    /// Control の入力ではなく、HUD が受け取らなかったクリックの位置から探す。
    /// </summary>
    /// <returns>印を押したか。</returns>
    public bool HandleInput(InputEvent inputEvent)
    {
        if (inputEvent is not InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left } click)
        {
            return false;
        }
        var world = _viewport.CanvasTransform.AffineInverse() * click.Position;
        foreach (var (area, carving) in _hits)
        {
            if (area.HasPoint(world))
            {
                Open(carving);
                return true;
            }
        }
        return false;
    }

    /// <summary>地表を描き直した後に呼ぶ。刻まれた字が変わっていれば、印を作り直す。</summary>
    public void Refresh(TileType[,] _) => Refresh();

    public void Refresh()
    {
        var script = _prophet.Script;
        var carvings = script?.Carvings ?? [];
        var signature = string.Join('|', carvings.Select(carving => $"{carving.Kind}{carving.Column},{carving.Row},{carving.Year},{carving.Sentence}"));
        if (signature == _shown)
        {
            return;
        }
        _shown = signature;
        foreach (var child in _marks.GetChildren())
        {
            _marks.RemoveChild(child);
            child.QueueFree();
        }
        _hits.Clear();
        if (script is null)
        {
            return;
        }
        foreach (var carving in carvings)
        {
            var mark = new GlyphView
            {
                Size = new Vector2(MarkSize * carving.Sentence.Words.Count, MarkSize),
                Position = _terrainOrigin + new Vector2(carving.Column * WorldScreen.TileSize + 2, carving.Row * WorldScreen.TileSize - MarkSize),
                MouseFilter = Control.MouseFilterEnum.Ignore,
            };
            mark.Show(script.Script, carving.Sentence, carving.Decayed, Mason(script, carving));
            _hits.Add((new Rect2(mark.Position, mark.Size), carving));
            _marks.AddChild(mark);
        }
    }

    // 拓本の欄に文を大きく出し、その字を手帳に載せる
    private void Open(CarvedGlyph carving)
    {
        var script = _prophet.Script!;
        _rubbing.CustomMinimumSize = new Vector2(64 * carving.Sentence.Words.Count, 64);
        _rubbing.Show(script.Script, carving.Sentence, carving.Decayed, Mason(script, carving));
        _info.Text = string.Format(
            CultureInfo.CurrentCulture, Tr("RUBBING_INFO"), Tr($"RUBBING_KIND_{carving.Kind.ToString().ToUpperInvariant()}"), carving.Year);
        _readings.Text = carving.Kind == CarvingKind.Stele ? SteleReadings(script, carving) : string.Empty;
        _readings.Visible = _readings.Text.Length > 0;
        var added = script.Discover(carving.Sentence);
        _found.Text = added > 0 ? string.Format(CultureInfo.CurrentCulture, Tr("RUBBING_FOUND"), added) : Tr("RUBBING_KNOWN");
        _panel.Visible = true;
        if (added > 0)
        {
            GD.Print(FormattableString.Invariant($"event=glyph_discovered count={added}"));
            Discovered?.Invoke();
        }
    }

    // 刻まれた字は、場所と刻んだ年で決まる石工の手の揺れで描く（印と拓本で同じ揺れ）
    private static (int Column, int Row, int Year, float Amount) Mason(ScriptState script, CarvedGlyph carving) =>
        (carving.Column, carving.Row, carving.Year, script.Rules.StyleJitter);

    // 碑の読み: 刻んだ時代の人の読みと、今の時代の人の読み。読みが同じなら 1 行にまとめる
    private static string SteleReadings(ScriptState script, CarvedGlyph stele)
    {
        var then = script.ReadAsPeople(stele.Sentence, stele.Decayed);
        var now = script.ReadAsPeople(stele.Sentence, script.Decayed);
        if (then is null || now is null)
        {
            return string.Empty;
        }
        return then.Phrases.SequenceEqual(now.Phrases) && then.DroppedVerbs == now.DroppedVerbs
            ? OracleView.PhrasesText(now, "ORACLE_READ")
            : OracleView.PhrasesText(then, "RUBBING_STELE_THEN") + "\n" + OracleView.PhrasesText(now, "RUBBING_STELE_NOW");
    }

    private static string Tr(string key) => TranslationServer.Translate(key).ToString();
}
