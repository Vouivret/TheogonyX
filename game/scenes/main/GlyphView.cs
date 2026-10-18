using System.Collections.Generic;
using System.Linq;
using Godot;
using TheogonyX.Core;

namespace TheogonyX.Scenes.Main;

/// <summary>
/// 文を線で描く（docs/domain/script-decipher.md の「7. 語の字と神託の碑」）。枠を語の数で左右に等分し、語の字を 1 つずつ置く。
/// 語の字の線は格子点を結ぶ線で、星の書きぶり（<see cref="ScriptStyle"/>。線の端・太さ・反り・傾き・飾り・囲み）で描く。
/// 刻まれた字は、石工の手の揺れで線の端を少しずらす。崩れた後の時代の字は、崩れた形と崩れた後の書きぶりで描く。アセットは使わない。
/// </summary>
public partial class GlyphView : Control
{
    // 反った線を折れ線で描くときの区切りの数
    private const int CurveSegments = 6;

    private PlanetScript? _script;
    private Sentence? _sentence;
    private bool _decayed;
    private ScriptStyle? _style;
    private (int Column, int Row, int Year, float Amount)? _mason;

    // 格子の区切りの数（格子点の数 − 1）。どの語の字も同じ格子なので、全部の線のいちばん大きい座標から求める
    private int _cells = 1;

    /// <summary>線の色。</summary>
    public Color LineColor { get; set; } = new(0.87f, 0.93f, 0.84f);

    /// <summary>背景の色（透明なら塗らない）。</summary>
    public Color Background { get; set; } = new(0.08f, 0.05f, 0.11f, 0.85f);

    /// <summary>縁の色（透明なら描かない）。選んでいる字を目立たせるのに使う。</summary>
    public Color Outline { get; set; } = Colors.Transparent;

    /// <summary>描く文を替える。</summary>
    /// <param name="mason">刻まれた字なら、その場所・年と揺れの大きさ（格子 1 マスを 1 とする）。手帳や欄の字は揺らさないので null。</param>
    public void Show(PlanetScript script, Sentence sentence, bool decayed, (int Column, int Row, int Year, float Amount)? mason = null)
    {
        _script = script;
        _sentence = sentence;
        _decayed = decayed;
        _mason = mason;
        _style = ScriptStyle.Of(script, decayed);
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
        if (_script is null || _sentence is null || _style is null || _sentence.Words.Count == 0)
        {
            return;
        }
        var cell = new Vector2(Size.X / _sentence.Words.Count, Size.Y);
        for (var i = 0; i < _sentence.Words.Count; i++)
        {
            DrawWord(_sentence.Words[i], i, new Rect2(new Vector2(cell.X * i, 0), cell));
        }
    }

    // 語の字を、枠の中の正方形に描く。囲みの分だけ余白を取る
    private void DrawWord(string radicalId, int wordIndex, Rect2 box)
    {
        var radical = _script!.Find(radicalId);
        if (radical is null)
        {
            return;
        }
        var style = _style!;
        var side = Mathf.Min(box.Size.X, box.Size.Y);
        box = new Rect2(box.Position + (box.Size - new Vector2(side, side)) / 2, new Vector2(side, side));
        var inner = box.Grow(-side * 0.18f);
        var width = Mathf.Max(1f, side / 16f * style.Weight);
        var thin = Mathf.Max(1f, side / 32f);
        switch (style.Frame)
        {
            case GlyphFrame.Circle:
                DrawArc(box.GetCenter(), side * 0.46f, 0f, Mathf.Tau, 32, LineColor, thin, true);
                break;
            case GlyphFrame.Square:
                DrawRect(box.Grow(-side * 0.04f), LineColor, false, thin);
                break;
            case GlyphFrame.TopBar:
                DrawLine(box.Position + new Vector2(side * 0.08f, side * 0.07f), box.Position + new Vector2(side * 0.92f, side * 0.07f), LineColor, width, true);
                break;
        }
        var center = inner.GetCenter();
        Vector2 Point(float x, float y)
        {
            var point = inner.Position + new Vector2(inner.Size.X * x / _cells, inner.Size.Y * y / _cells);
            return point + new Vector2(-(point.Y - center.Y) * style.Slant, 0f);
        }
        (float X, float Y) Shake(int stroke, int end) => _mason is { } mason
            ? ScriptStyle.Jitter(mason.Amount, mason.Column, mason.Row, mason.Year, (wordIndex * 16) + stroke, end)
            : (0f, 0f);
        var shape = radical.ShapeIn(_decayed);
        for (var i = 0; i < shape.Count; i++)
        {
            var stroke = shape[i];
            var (dx1, dy1) = Shake(i, 0);
            var (dx2, dy2) = Shake(i, 1);
            DrawStroke(Point(stroke.X1 + dx1, stroke.Y1 + dy1), Point(stroke.X2 + dx2, stroke.Y2 + dy2), width, style);
        }
        if (style.Ornament == GlyphOrnament.BaseDot)
        {
            DrawCircle(new Vector2(center.X, inner.End.Y + (side * 0.09f)), width * 0.8f, LineColor);
        }
    }

    // 線 1 本を、反り・端の形・点の飾りで描く
    private void DrawStroke(Vector2 from, Vector2 to, float width, ScriptStyle style)
    {
        var control = ((from + to) / 2) + ((to - from).Orthogonal() * style.Bend);
        var points = new List<Vector2>();
        for (var i = 0; i <= CurveSegments; i++)
        {
            var t = (float)i / CurveSegments;
            points.Add(((1 - t) * (1 - t) * from) + (2 * (1 - t) * t * control) + (t * t * to));
        }
        if (style.Cap == StrokeCap.Wedge)
        {
            // 楔: 始まりを太く、終わりを細くした多角形
            var left = new List<Vector2>();
            var right = new List<Vector2>();
            for (var i = 0; i < points.Count; i++)
            {
                var direction = (points[Mathf.Min(i + 1, points.Count - 1)] - points[Mathf.Max(i - 1, 0)]).Normalized();
                var half = width * Mathf.Lerp(0.9f, 0.15f, (float)i / (points.Count - 1));
                left.Add(points[i] + (direction.Orthogonal() * half));
                right.Add(points[i] - (direction.Orthogonal() * half));
            }
            right.Reverse();
            DrawColoredPolygon([.. left, .. right], LineColor);
        }
        else
        {
            DrawPolyline([.. points], LineColor, width, true);
            if (style.Cap == StrokeCap.Round)
            {
                DrawCircle(from, width / 2, LineColor);
                DrawCircle(to, width / 2, LineColor);
            }
        }
        if (style.Ornament == GlyphOrnament.EndDots)
        {
            DrawCircle(to, width * 0.9f, LineColor);
        }
    }
}
