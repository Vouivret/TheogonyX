using System.Linq;
using Godot;
using TheogonyX.Core;

namespace TheogonyX.Scenes.Main;

/// <summary>
/// 文を線で描く（docs/domain/script-decipher.md の「7. 語の字と神託の碑」）。枠を語の数で左右に等分し、語の字を 1 つずつ置く。
/// 語の字の線は格子点を結ぶ線。崩れた後の時代の字は、崩れた形で描く。アセットは使わない。
/// </summary>
public partial class GlyphView : Control
{
    private PlanetScript? _script;
    private Sentence? _sentence;
    private bool _decayed;

    // 格子の区切りの数（格子点の数 − 1）。どの語の字も同じ格子なので、全部の線のいちばん大きい座標から求める
    private int _cells = 1;

    /// <summary>線の色。</summary>
    public Color LineColor { get; set; } = new(0.87f, 0.93f, 0.84f);

    /// <summary>背景の色（透明なら塗らない）。</summary>
    public Color Background { get; set; } = new(0.08f, 0.05f, 0.11f, 0.85f);

    /// <summary>縁の色（透明なら描かない）。選んでいる字を目立たせるのに使う。</summary>
    public Color Outline { get; set; } = Colors.Transparent;

    /// <summary>描く文を替える。</summary>
    public void Show(PlanetScript script, Sentence sentence, bool decayed)
    {
        _script = script;
        _sentence = sentence;
        _decayed = decayed;
        _cells = Mathf.Max(1, script.Radicals.SelectMany(radical => radical.Strokes)
            .Select(stroke => Mathf.Max(Mathf.Max(stroke.X1, stroke.X2), Mathf.Max(stroke.Y1, stroke.Y2))).DefaultIfEmpty(1).Max());
        QueueRedraw();
    }

    /// <summary>語の字 1 つを描く。</summary>
    public void Show(PlanetScript script, string radicalId, bool decayed) => Show(script, new Sentence([radicalId]), decayed);

    public override void _Draw()
    {
        if (Background.A > 0f)
        {
            DrawRect(new Rect2(Vector2.Zero, Size), Background);
        }
        if (Outline.A > 0f)
        {
            DrawRect(new Rect2(Vector2.Zero, Size), Outline, false, 1f);
        }
        if (_script is null || _sentence is null || _sentence.Words.Count == 0)
        {
            return;
        }
        var cell = new Vector2(Size.X / _sentence.Words.Count, Size.Y);
        var width = Mathf.Max(1f, Mathf.Min(cell.X, cell.Y) / 16f);
        for (var i = 0; i < _sentence.Words.Count; i++)
        {
            DrawWord(_sentence.Words[i], new Rect2(new Vector2(cell.X * i, 0), cell), width);
        }
    }

    // 語の字の線を、枠の中の正方形に 1 割の余白を取って描く
    private void DrawWord(string radicalId, Rect2 box, float width)
    {
        var radical = _script!.Find(radicalId);
        if (radical is null)
        {
            return;
        }
        var side = Mathf.Min(box.Size.X, box.Size.Y);
        box = new Rect2(box.Position + (box.Size - new Vector2(side, side)) / 2, new Vector2(side, side));
        var inner = box.Grow(-side * 0.1f);
        Vector2 Point(int x, int y) => inner.Position + new Vector2(inner.Size.X * x / _cells, inner.Size.Y * y / _cells);
        foreach (var stroke in radical.ShapeIn(_decayed))
        {
            DrawLine(Point(stroke.X1, stroke.Y1), Point(stroke.X2, stroke.Y2), LineColor, width, true);
        }
    }
}
