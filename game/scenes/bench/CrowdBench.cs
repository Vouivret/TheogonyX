using System.Diagnostics;
using System.Globalization;
using Godot;
using TheogonyX.Core;
using TheogonyX.Data;
using TheogonyX.Scenes.Main;

namespace TheogonyX.Scenes.Bench;

/// <summary>
/// 群衆の計測（TS-crowd-011、plan/feat-m3-crowd-gpu.md）。人数と計算の方式（CPU 版・GPU 版）を変えて、
/// 1 歩の計算（GPU 版は読み戻しを含む）・バッファの書き込み・1 フレームの時間を測り、ログに出して終わる。
/// 起動: Godot のコマンドに <c>-- --crowd-bench</c> を付ける（<see cref="WorldCore"/> がゲームの代わりにこのノードを置く）。
/// 計測の道具なので、時間は Stopwatch で直接測る（シミュレーションの結果には使わない）。
/// </summary>
public partial class CrowdBench : Node2D
{
    public const string CommandLineArg = "--crowd-bench";

    private static readonly int[] Counts = [10_000, 50_000, 100_000];
    private const double WarmupSeconds = 1.0;
    private const double MeasureSeconds = 5.0;
    private const int Seed = 1;

    private CrowdRules _rules = null!;
    private CrowdLook _look = null!;
    private TerrainRules _terrainRules = null!;
    private PotentialField _field = null!;
    private readonly Stopwatch _stopwatch = new();

    private int _runIndex = -1;
    private Crowd _crowd = null!;
    private GpuCrowd? _gpu;
    private float[] _gpuX = [];
    private float[] _gpuY = [];
    private MultiMeshInstance2D? _node;
    private float[] _buffer = [];
    private double _elapsedSeconds;
    private int _frames;
    private double _frameSeconds;
    private double _stepMs;
    private double _drawMs;

    public override void _Ready()
    {
        var rulesJson = WorldCore.ReadText("res://data/rules.json");
        _rules = GameDataLoader.ParseCrowd(rulesJson);
        _look = GameDataLoader.ParseCrowdLook(rulesJson);
        _terrainRules = GameDataLoader.ParseTerrain(rulesJson);
        var world = WorldState.CreateInitial(GameDataLoader.ParseWorldRules(rulesJson));
        _field = PotentialField.Build(TerrainClassifier.ClassifyTiles(world, _terrainRules), _rules.TileForces);

        // 描画の余力を見るため、垂直同期とフレームレートの上限を外す
        DisplayServer.WindowSetVsyncMode(DisplayServer.VSyncMode.Disabled);
        Engine.MaxFps = 0;
        Position = new Vector2(16, 63);
        StartNext();
    }

    public override void _Process(double delta)
    {
        _stopwatch.Restart();
        if (_gpu is null)
        {
            _crowd.Step(CrowdMode.Wander, _field);
        }
        else
        {
            _gpu.Step(CrowdMode.Wander, 1);
            _gpu.ReadPositions(_gpuX, _gpuY);
        }
        var stepMs = _stopwatch.Elapsed.TotalMilliseconds;

        _stopwatch.Restart();
        if (_gpu is null)
        {
            CrowdView.WriteBuffer(_crowd.X, _crowd.Y, _buffer);
        }
        else
        {
            CrowdView.WriteBuffer(_gpuX, _gpuY, _buffer);
        }
        _node!.Multimesh.Buffer = _buffer;
        var drawMs = _stopwatch.Elapsed.TotalMilliseconds;

        _elapsedSeconds += delta;
        if (_elapsedSeconds < WarmupSeconds)
        {
            return;
        }
        _frames++;
        _frameSeconds += delta;
        _stepMs += stepMs;
        _drawMs += drawMs;
        if (_elapsedSeconds < WarmupSeconds + MeasureSeconds)
        {
            return;
        }

        var culture = CultureInfo.InvariantCulture;
        GD.Print(string.Format(
            culture,
            "event=crowd_bench backend={0} count={1} stepMs={2:F3} drawMs={3:F3} fps={4:F1}",
            _gpu is null ? "cpu" : "gpu", _crowd.Count, _stepMs / _frames, _drawMs / _frames, _frames / _frameSeconds));
        StartNext();
    }

    private void StartNext()
    {
        _runIndex++;
        _gpu?.Dispose();
        _gpu = null;
        // 人数ごとに CPU 版 → GPU 版の順
        if (_runIndex >= Counts.Length * 2)
        {
            GetTree().Quit();
            return;
        }
        _node?.QueueFree();
        _crowd = new Crowd(Seed, _rules with { Count = Counts[_runIndex / 2] }, _terrainRules.Columns, _terrainRules.Rows, holySiteX: 8f);
        if (_runIndex % 2 == 1)
        {
            _gpu = GpuCrowd.TryCreate(_crowd, _rules, _field, out var reason);
            if (_gpu is null)
            {
                GD.PushError($"event=crowd_bench_skipped reason={reason}");
                StartNext();
                return;
            }
            _gpuX = new float[_crowd.Count];
            _gpuY = new float[_crowd.Count];
        }
        _buffer = new float[_crowd.Count * CrowdView.FloatsPerInstance];
        _node = new MultiMeshInstance2D
        {
            Multimesh = CrowdView.CreateMultiMesh(_crowd.Count, _look),
            SelfModulate = new Color(_look.DotColor.R, _look.DotColor.G, _look.DotColor.B),
        };
        AddChild(_node);
        _elapsedSeconds = 0;
        _frames = 0;
        _frameSeconds = 0;
        _stepMs = 0;
        _drawMs = 0;
    }
}
