using Godot;
using TheogonyX.Core;
using TheogonyX.Data;

namespace TheogonyX.Scenes.Main;

/// <summary>
/// 星の地表を映すカメラ。マウスホイールでズームの段を切り替え（切り替えの一瞬だけ、地図がモザイクから合焦する）、ドラッグか WASD キーで動かす（docs/domain/camera-views.md）。
/// カメラは地表を描く <c>SubViewport</c> の中に置くので、HUD は動かない。計算はコアの <see cref="CameraZoom"/> が行う。
/// </summary>
public sealed class CameraView
{
    private readonly CameraSettings _settings;
    private readonly Camera2D _camera;
    private readonly Vector2 _viewSize;

    // 地表の上に足す px。予言者（16 px）と焚き火の炎が、上の帯（地図の 12〜18 px ぶん）より下に映る高さ
    private const float AboveTerrain = 48f;

    // 動かせる範囲（世界の px）。地表の広さに、上へ AboveTerrain を足したもの
    private readonly Rect2 _bounds;

    // 切り替えの演出（ピクセル・フォーカス）。経過の秒ごとのモザイクの大きさ（画素）。最後の段が終わると元の絵に戻る
    private static readonly (double Until, float Block)[] FocusSteps = [(0.04, 8f), (0.08, 4f), (0.12, 2f)];

    private readonly SubViewportContainer _map;
    private readonly ShaderMaterial _focus;
    private double _focusElapsed = double.PositiveInfinity;

    private double _zoom;
    private double _centerX;
    private double _centerY;

    private bool _dragging;

    /// <summary>視点の段（星・街・暮らし）が変わったとき。</summary>
    public event System.Action<CameraViewLevel>? LevelChanged;

    /// <summary>今の視点の段。</summary>
    public CameraViewLevel Level => CameraZoom.LevelOf(_zoom);

    /// <param name="owner">ノードを探すためのシーンの根。</param>
    public CameraView(Node owner, CameraSettings settings)
    {
        _settings = settings;
        var viewport = owner.GetNode<SubViewport>("%WorldViewport");
        _viewSize = viewport.Size;
        // 動かせる範囲は地表の広さに、上へ AboveTerrain px を足したもの（地表を描いた後に作る）。
        // 予言者と焚き火は地表の上端に立つので、上の帯の下に頭まで映るよう、地表の上を少し見せる。
        // 見えている幅がそれ以上のときは、範囲の中央に置く
        var terrain = owner.GetNode<TileMapLayer>("%Terrain");
        var used = terrain.GetUsedRect();
        var terrainRect = new Rect2(terrain.Position + ((Vector2)used.Position * WorldScreen.TileSize), (Vector2)used.Size * WorldScreen.TileSize);
        _bounds = new Rect2(terrainRect.Position - new Vector2(0, AboveTerrain), terrainRect.Size + new Vector2(0, AboveTerrain));
        _map = owner.GetNode<SubViewportContainer>("WorldView");
        _focus = new ShaderMaterial { Shader = GD.Load<Shader>("res://scenes/main/pixel_focus.gdshader") };
        _zoom = settings.StartZoom;
        // 起動時は、範囲の上端に合わせて見せる（地表の上と、立っている予言者が映る）
        _centerX = _bounds.GetCenter().X;
        _centerY = _bounds.Position.Y + (_viewSize.Y / 2f / (float)_zoom);

        _camera = new Camera2D { Name = "Camera" };
        viewport.AddChild(_camera);
        _camera.MakeCurrent();
        Apply();
    }

    /// <summary>HUD が受け取らなかった入力。カメラの操作なら true を返す。</summary>
    public bool HandleInput(InputEvent inputEvent)
    {
        switch (inputEvent)
        {
            case InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.WheelUp or MouseButton.WheelDown } wheel:
                var direction = wheel.ButtonIndex == MouseButton.WheelUp ? 1 : -1;
                ChangeZoom(CameraZoom.NextStep(_settings.ZoomSteps, _zoom, direction), wheel.Position - _viewSize / 2);
                return true;
            case InputEventMouseButton { ButtonIndex: MouseButton.Left or MouseButton.Middle } button:
                _dragging = button.Pressed;
                return true;
            // ボタンを帯・引き出しの上で離すと、離したことが届かない。押しているボタンが無くなっていたら、ドラッグを終える
            case InputEventMouseMotion motion when _dragging && (motion.ButtonMask & (MouseButtonMask.Left | MouseButtonMask.Middle)) == 0:
                _dragging = false;
                return false;
            case InputEventMouseMotion motion when _dragging:
                _centerX -= motion.Relative.X / _zoom;
                _centerY -= motion.Relative.Y / _zoom;
                Apply();
                return true;
            default:
                return false;
        }
    }

    /// <summary>毎フレーム呼ぶ。WASD キーで動かし、切り替えの演出を進める。</summary>
    public void Process(double delta)
    {
        // 物理的なキーの位置で見るので、キーボードの配列によらず同じ場所のキーで動く
        var x = KeyAxis(Key.A, Key.D);
        var y = KeyAxis(Key.W, Key.S);
        if (x != 0 || y != 0)
        {
            _centerX = CameraZoom.Pan(_centerX, x, _settings.PanSpeed, _zoom, delta);
            _centerY = CameraZoom.Pan(_centerY, y, _settings.PanSpeed, _zoom, delta);
            Apply();
        }
        ProcessFocus(delta);
    }

    // ズームは一瞬で切り替える。マウスの下の世界の点は動かさず、段が変われば知らせる
    private void ChangeZoom(double next, Vector2 anchorOffset)
    {
        if (next == _zoom)
        {
            return;
        }
        var before = Level;
        _centerX = CameraZoom.ZoomAround(_centerX, anchorOffset.X, _zoom, next);
        _centerY = CameraZoom.ZoomAround(_centerY, anchorOffset.Y, _zoom, next);
        _zoom = next;
        Apply();
        _focusElapsed = 0;
        SetBlock(FocusSteps[0].Block);
        if (Level != before)
        {
            LevelChanged?.Invoke(Level);
        }
    }

    private void ProcessFocus(double delta)
    {
        if (double.IsPositiveInfinity(_focusElapsed))
        {
            return;
        }
        _focusElapsed += delta;
        foreach (var (until, block) in FocusSteps)
        {
            if (_focusElapsed < until)
            {
                SetBlock(block);
                return;
            }
        }
        _focusElapsed = double.PositiveInfinity;
        _map.Material = null;
    }

    private void SetBlock(float block)
    {
        _focus.SetShaderParameter("block", block);
        _map.Material = _focus;
    }

    private static int KeyAxis(Key negative, Key positive) =>
        (Input.IsPhysicalKeyPressed(positive) ? 1 : 0) - (Input.IsPhysicalKeyPressed(negative) ? 1 : 0);

    private void Apply()
    {
        _centerX = CameraZoom.ClampCenter(_centerX, _zoom, _viewSize.X, _bounds.Position.X, _bounds.End.X);
        _centerY = CameraZoom.ClampCenter(_centerY, _zoom, _viewSize.Y, _bounds.Position.Y, _bounds.End.Y);
        // 整数倍のときは、ドットを画面の px にそろえる
        var x = CameraZoom.SnapCenter(_centerX, _zoom);
        var y = CameraZoom.SnapCenter(_centerY, _zoom);
        _camera.Position = new Vector2((float)x, (float)y);
        _camera.Zoom = new Vector2((float)_zoom, (float)_zoom);
    }
}
