using System;
using Godot;

namespace TheogonyX.Scenes.Main;

/// <summary>
/// 地図と HUD の割り付け（docs/art-guide.md の「画面」）。文字と操作は 960 × 540 を基準に窓に合わせて拡大し（<c>canvas_items</c>）、
/// 地図（480 × 270 px の <c>SubViewport</c>）は、画面のピクセルで整数倍になる倍率で窓を覆うように置く（はみ出す端は切る）。
/// 地図への入力を地図の座標に直し、予言者の吹き出しを予言者の頭の上に付けて動かす。
/// </summary>
public sealed class HudLayout
{
    /// <summary>上の帯の下端（main.tscn の TopBar の高さ + 隙間）。引き出しと吹き出しはこれより下に出す。</summary>
    public const float TopBarHeight = 38;

    /// <summary>下の帯の上端までの高さ（main.tscn の BottomBar の高さ + 隙間）。引き出しと板はこれより上に出す。</summary>
    public const float BottomBarHeight = 38;

    private const float SideMargin = 4;

    private readonly Node _owner;
    private readonly SubViewportContainer _map;
    private readonly SubViewport _viewport;
    private readonly Node2D _prophet;
    private readonly Control _bubbles;

    /// <param name="owner">ノードを探すためのシーンの根。</param>
    public HudLayout(Node owner)
    {
        _owner = owner;
        _map = owner.GetNode<SubViewportContainer>("WorldView");
        _viewport = owner.GetNode<SubViewport>("%WorldViewport");
        _prophet = owner.GetNode<Node2D>("%Prophet");
        _bubbles = owner.GetNode<Control>("%ProphetBubbles");
        owner.GetViewport().SizeChanged += Fit;
        Fit();
    }

    /// <summary>窓の大きさから地図の倍率と位置を決め直す。</summary>
    public void Fit()
    {
        var screen = (Vector2)_owner.GetWindow().Size;
        var logical = _owner.GetViewport().GetVisibleRect().Size;
        var map = (Vector2)_viewport.Size;
        if (screen.X <= 0 || screen.Y <= 0 || logical.X <= 0)
        {
            return;
        }
        // 文字の拡大（画面のピクセル / 基準の座標）。地図の側ではこれを打ち消して、画面のピクセルで整数倍にする
        var stretch = screen.X / logical.X;
        var scale = Math.Max(1, (int)Math.Ceiling(Math.Max(screen.X / map.X, screen.Y / map.Y)));
        var offset = new Vector2(Mathf.Floor((screen.X - (map.X * scale)) / 2), Mathf.Floor((screen.Y - (map.Y * scale)) / 2));
        _map.Scale = Vector2.One * (scale / stretch);
        _map.Position = offset / stretch;
    }

    /// <summary>引き出しを、画面の左か右の端の、上の帯と下の帯の間に置く。</summary>
    public static void PlaceDrawer(Control panel, bool right, float width)
    {
        panel.SetAnchorsPreset(right ? Control.LayoutPreset.RightWide : Control.LayoutPreset.LeftWide);
        panel.OffsetLeft = right ? -width : 0;
        panel.OffsetRight = right ? 0 : width;
        panel.OffsetTop = TopBarHeight;
        panel.OffsetBottom = -BottomBarHeight;
    }

    /// <summary>HUD の座標の入力を、地図（<c>SubViewport</c>）の座標に直す。</summary>
    public InputEvent ToMap(InputEvent inputEvent) => inputEvent.XformedBy(_map.GetTransform().AffineInverse());

    /// <summary>毎フレーム呼ぶ。予言者の吹き出しを頭の上に置く（画面の外や帯の上にははみ出させない）。</summary>
    public void Process()
    {
        if (!_bubbles.Visible)
        {
            return;
        }
        // 子の表示を切り替えた直後は Container の大きさの更新が遅れるので、最小の大きさをその場で求めて使う
        var size = _bubbles.GetCombinedMinimumSize();
        _bubbles.Size = size;
        var sprite = _prophet as Sprite2D;
        var width = sprite?.Texture is { } texture ? texture.GetWidth() / Math.Max(1, sprite.Hframes) : 0;
        var head = _map.GetTransform() * (_viewport.CanvasTransform * (_prophet.GlobalPosition + new Vector2(width / 2f, 0)));
        var logical = _owner.GetViewport().GetVisibleRect().Size;
        var x = Math.Clamp(head.X - (size.X / 2), SideMargin, Math.Max(SideMargin, logical.X - size.X - SideMargin));
        var y = Math.Clamp(head.Y - size.Y - 2, TopBarHeight, Math.Max(TopBarHeight, logical.Y - BottomBarHeight - size.Y));
        _bubbles.Position = new Vector2(x, y);
    }
}
