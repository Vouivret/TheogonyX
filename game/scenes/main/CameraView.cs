using Godot;
using TheogonyX.Core;
using TheogonyX.Data;

namespace TheogonyX.Scenes.Main;

/// <summary>
/// 星の地表を映すカメラ。マウスホイールでズームの段を切り替え、ドラッグで動かす（docs/domain/camera-views.md）。
/// カメラは地表を描く <c>SubViewport</c> の中に置くので、HUD は動かない。計算はコアの <see cref="CameraZoom"/> が行う。
/// </summary>
public sealed class CameraView
{
    private readonly CameraSettings _settings;
    private readonly Camera2D _camera;
    private readonly Vector2 _viewSize;

    // 動かせる範囲（世界の px）。今は、ズームする前の画面と同じ広さ
    private readonly Rect2 _bounds;

    private double _zoom;
    private double _targetZoom;
    private double _centerX;
    private double _centerY;

    // ズームの中心にするマウスの位置（画面の中心から見た px）
    private Vector2 _anchorOffset;
    private bool _dragging;

    /// <param name="owner">ノードを探すためのシーンの根。</param>
    public CameraView(Node owner, CameraSettings settings)
    {
        _settings = settings;
        var viewport = owner.GetNode<SubViewport>("%WorldViewport");
        _viewSize = viewport.Size;
        _bounds = new Rect2(Vector2.Zero, _viewSize);
        _zoom = settings.StartZoom;
        _targetZoom = settings.StartZoom;
        _centerX = _bounds.GetCenter().X;
        _centerY = _bounds.GetCenter().Y;

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
                _targetZoom = CameraZoom.NextStep(_settings.ZoomSteps, _targetZoom, direction);
                _anchorOffset = wheel.Position - _viewSize / 2;
                return true;
            case InputEventMouseButton { ButtonIndex: MouseButton.Left or MouseButton.Middle } button:
                _dragging = button.Pressed;
                return true;
            case InputEventMouseMotion motion when _dragging:
                _centerX -= motion.Relative.X / _zoom;
                _centerY -= motion.Relative.Y / _zoom;
                Apply();
                return true;
            default:
                return false;
        }
    }

    /// <summary>毎フレーム呼ぶ。ズームを目標の段へ近づける。</summary>
    public void Process(double delta)
    {
        if (_zoom == _targetZoom)
        {
            return;
        }
        var next = CameraZoom.Approach(_zoom, _targetZoom, _settings.ZoomSmoothing, delta);
        _centerX = CameraZoom.ZoomAround(_centerX, _anchorOffset.X, _zoom, next);
        _centerY = CameraZoom.ZoomAround(_centerY, _anchorOffset.Y, _zoom, next);
        _zoom = next;
        Apply();
    }

    private void Apply()
    {
        _centerX = CameraZoom.ClampCenter(_centerX, _zoom, _viewSize.X, _bounds.Position.X, _bounds.End.X);
        _centerY = CameraZoom.ClampCenter(_centerY, _zoom, _viewSize.Y, _bounds.Position.Y, _bounds.End.Y);
        // 補間の途中は丸めない（整数倍に止まったときだけドットを画面の px にそろえる）
        var settled = _zoom == _targetZoom;
        var x = settled ? CameraZoom.SnapCenter(_centerX, _zoom) : _centerX;
        var y = settled ? CameraZoom.SnapCenter(_centerY, _zoom) : _centerY;
        _camera.Position = new Vector2((float)x, (float)y);
        _camera.Zoom = new Vector2((float)_zoom, (float)_zoom);
    }
}
