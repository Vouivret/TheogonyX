using System.Linq;
using Godot;
using TheogonyX.Core;

namespace TheogonyX.Scenes.Main;

/// <summary>
/// 字 1 つを線で描く（docs/domain/script-decipher.md の「字のしくみ」）。部品の線は格子点を結ぶ線で、字の並びに従って、
/// 左右（または上下）に半分ずつ部品を置き、方角の部品を下に小さく置く。部品 1 つだけの字は枠いっぱいに描く。
/// 崩れた後の時代の字は、崩れた形で描く。アセットは使わない。
/// </summary>
public partial class GlyphView : Control
{
    private PlanetScript? _script;
    private Glyph? _glyph;
    private bool _decayed;

    // 格子の区切りの数（格子点の数 − 1）。どの部品も同じ格子なので、全部の部品の線のいちばん大きい座標から求める
    private int _cells = 1;

    /// <summary>線の色。</summary>
    public Color LineColor { get; set; } = new(0.87f, 0.93f, 0.84f);

    /// <summary>背景の色（透明なら塗らない）。</summary>
    public Color Background { get; set; } = new(0.08f, 0.05f, 0.11f, 0.85f);

    /// <summary>描く字を替える。</summary>
    public void Show(PlanetScript script, Glyph glyph, bool decayed)
    {
        _script = script;
        _glyph = glyph;
        _decayed = decayed;
        _cells = Mathf.Max(1, script.Radicals.SelectMany(radical => radical.Strokes)
            .Select(stroke => Mathf.Max(Mathf.Max(stroke.X1, stroke.X2), Mathf.Max(stroke.Y1, stroke.Y2))).DefaultIfEmpty(1).Max());
        QueueRedraw();
    }

    public override void _Draw()
    {
        if (Background.A > 0f)
        {
            DrawRect(new Rect2(Vector2.Zero, Size), Background);
        }
        if (_script is null || _glyph is null)
        {
            return;
        }
        var width = Mathf.Max(1f, Size.X / 16f);
        var main = new Rect2(Vector2.Zero, Size);
        if (_glyph.Direction is not null)
        {
            // 方角の部品は、字の下の 4 分の 1 に小さく置く
            var strip = Size.Y * 0.25f;
            main = new Rect2(Vector2.Zero, new Vector2(Size.X, Size.Y - strip));
            DrawRadical(_glyph.Direction, new Rect2(new Vector2(Size.X * 0.3f, Size.Y - strip), new Vector2(Size.X * 0.4f, strip)), width);
        }
        if (_glyph.Second is null)
        {
            DrawRadical(_glyph.First, main, width);
            return;
        }
        var (first, second) = _glyph.Arrangement == GlyphArrangement.LeftRight
            ? (new Rect2(main.Position, new Vector2(main.Size.X / 2, main.Size.Y)),
                new Rect2(main.Position + new Vector2(main.Size.X / 2, 0), new Vector2(main.Size.X / 2, main.Size.Y)))
            : (new Rect2(main.Position, new Vector2(main.Size.X, main.Size.Y / 2)),
                new Rect2(main.Position + new Vector2(0, main.Size.Y / 2), new Vector2(main.Size.X, main.Size.Y / 2)));
        DrawRadical(_glyph.First, first, width);
        DrawRadical(_glyph.Second, second, width);
    }

    // 部品の線を、枠の中に 1 割の余白を取って描く
    private void DrawRadical(string radicalId, Rect2 box, float width)
    {
        var radical = _script!.Find(radicalId);
        if (radical is null)
        {
            return;
        }
        var inner = box.Grow(-Mathf.Min(box.Size.X, box.Size.Y) * 0.1f);
        Vector2 Point(int x, int y) => inner.Position + new Vector2(inner.Size.X * x / _cells, inner.Size.Y * y / _cells);
        foreach (var stroke in radical.ShapeIn(_decayed))
        {
            DrawLine(Point(stroke.X1, stroke.Y1), Point(stroke.X2, stroke.Y2), LineColor, width, true);
        }
    }
}
