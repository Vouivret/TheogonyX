using System;
using Godot;
using TheogonyX.Core;

namespace TheogonyX.Scenes.Main;

/// <summary>字の中で部品を置く場所。</summary>
public enum GlyphSlot
{
    First,
    Second,
    Direction,
}

/// <summary>
/// 組んでいる字を大きく描き、部品を置く 3 つの枠（並びに従って左右か上下の 2 つと、下の小さな枠）を見せる。
/// 枠を押すと <see cref="SlotPressed"/> を知らせる。選んでいる枠は明るい縁で囲む。置き方は <see cref="GlyphView"/> と同じ。
/// </summary>
public partial class GlyphEditorView : Control
{
    private static readonly Color EmptyColor = new(0.87f, 0.93f, 0.84f, 0.3f);
    private static readonly Color SelectedColor = new(1f, 0.85f, 0.3f);

    private readonly GlyphView[] _parts = new GlyphView[3];
    private GlyphArrangement _arrangement = GlyphArrangement.LeftRight;
    private GlyphSlot _selected = GlyphSlot.First;
    private readonly bool[] _filled = new bool[3];

    public GlyphEditorView()
    {
        MouseFilter = MouseFilterEnum.Stop;
        for (var i = 0; i < _parts.Length; i++)
        {
            _parts[i] = new GlyphView { Background = Colors.Transparent, MouseFilter = MouseFilterEnum.Ignore };
            AddChild(_parts[i]);
        }
        Resized += Layout;
    }

    /// <summary>枠を押した。</summary>
    public event Action<GlyphSlot>? SlotPressed;

    /// <summary>描く部品・並び・選んでいる枠を替える。部品が無い枠は null。</summary>
    public void Show(PlanetScript script, string? first, string? second, string? direction, GlyphArrangement arrangement, GlyphSlot selected, bool decayed)
    {
        _arrangement = arrangement;
        _selected = selected;
        string?[] ids = [first, second, direction];
        for (var i = 0; i < _parts.Length; i++)
        {
            _filled[i] = ids[i] is not null;
            _parts[i].Show(script, new Glyph(ids[i] ?? string.Empty, null, null, GlyphArrangement.LeftRight), decayed);
        }
        Layout();
    }

    public override void _GuiInput(InputEvent inputEvent)
    {
        if (inputEvent is not InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left } click)
        {
            return;
        }
        foreach (var slot in Enum.GetValues<GlyphSlot>())
        {
            if (Area(slot).HasPoint(click.Position))
            {
                SlotPressed?.Invoke(slot);
                AcceptEvent();
                return;
            }
        }
    }

    public override void _Draw()
    {
        DrawRect(new Rect2(Vector2.Zero, Size), new Color(0.08f, 0.05f, 0.11f, 0.95f));
        foreach (var slot in Enum.GetValues<GlyphSlot>())
        {
            var area = Area(slot).Grow(-1f);
            if (slot == _selected)
            {
                DrawRect(area, SelectedColor with { A = 0.12f });
                DrawRect(area, SelectedColor, false, 2f);
            }
            else if (!_filled[(int)slot])
            {
                DrawRect(area, EmptyColor, false, 1f);
            }
        }
    }

    // 枠の場所。方角の枠は、字の下の 4 分の 1 の真ん中（GlyphView と同じ）
    private Rect2 Area(GlyphSlot slot)
    {
        var strip = Size.Y * 0.25f;
        var main = new Vector2(Size.X, Size.Y - strip);
        return slot switch
        {
            GlyphSlot.Direction => new Rect2(new Vector2(Size.X * 0.3f, Size.Y - strip), new Vector2(Size.X * 0.4f, strip)),
            GlyphSlot.First => _arrangement == GlyphArrangement.LeftRight
                ? new Rect2(Vector2.Zero, new Vector2(main.X / 2, main.Y))
                : new Rect2(Vector2.Zero, new Vector2(main.X, main.Y / 2)),
            _ => _arrangement == GlyphArrangement.LeftRight
                ? new Rect2(new Vector2(main.X / 2, 0), new Vector2(main.X / 2, main.Y))
                : new Rect2(new Vector2(0, main.Y / 2), new Vector2(main.X, main.Y / 2)),
        };
    }

    private void Layout()
    {
        foreach (var slot in Enum.GetValues<GlyphSlot>())
        {
            var area = Area(slot);
            _parts[(int)slot].Position = area.Position;
            _parts[(int)slot].Size = area.Size;
        }
        QueueRedraw();
    }
}
