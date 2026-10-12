using System;
using Godot;
using TheogonyX.Core;
using TheogonyX.Data;

namespace TheogonyX.Scenes.Main;

/// <summary>
/// 群衆を点で描く（docs/domain/crowd.md）。計算は GPU 版（<see cref="GpuCrowd"/>）で行い、使えなければコアの <see cref="Crowd"/>（CPU 版）で行う。
/// </summary>
public sealed class CrowdView : IDisposable
{
    // 1 フレームで進める歩数の上限。遅れてたまった分は捨てる（plan/feat-m3-crowd-cpu.md）。高速（3 倍）でも 30fps まで追いつける。
    private const int MaxStepsPerFrame = 8;

    // 予言者の絵の幅（docs/art-guide.md の「予言者のスプライト」）。聖地は予言者の左右の中央。
    private const int ProphetWidthPixels = 32;

    // MultiMesh の 2D の変換 1 つ分の float の数
    internal const int FloatsPerInstance = 8;

    private readonly TimeControl _timeControl;
    private readonly AutoPlayClock _clock;
    private readonly CrowdRules _rules;
    private readonly Crowd _crowd;
    private readonly GpuCrowd? _gpu;
    private readonly MultiMesh _multiMesh;
    private readonly float[] _buffer;
    // GPU 版から読み戻した位置
    private readonly float[] _gpuX;
    private readonly float[] _gpuY;
    private PotentialField _field;
    private double _pendingSeconds;

    /// <param name="owner">ノードを探すためのシーンの根。</param>
    public CrowdView(
        Node owner, int seed, TimeControl timeControl, AutoPlayClock clock, TerrainRules terrainRules, CrowdRules rules, CrowdLook look, ProphetSettings prophetSettings)
    {
        _timeControl = timeControl;
        _clock = clock;
        _rules = rules;
        var holySiteX = prophetSettings.Column - 1 + ProphetWidthPixels / 2f / WorldScreen.TileSize;
        _crowd = new Crowd(seed, rules, terrainRules.Columns, terrainRules.Rows, holySiteX);
        _field = PotentialField.Build(TerrainClassifier.ClassifyTiles(timeControl.World, terrainRules), rules.TileForces);

        if (look.UseGpu)
        {
            _gpu = GpuCrowd.TryCreate(_crowd, rules, _field, out var reason);
            if (_gpu is null)
            {
                GD.PushWarning($"event=crowd_gpu_unavailable reason={reason}");
            }
        }
        GD.Print($"event=crowd_started backend={(_gpu is null ? "cpu" : "gpu")}");
        _gpuX = _gpu is null ? [] : new float[_crowd.Count];
        _gpuY = _gpu is null ? [] : new float[_crowd.Count];

        var node = owner.GetNode<MultiMeshInstance2D>("%Crowd");
        node.Position = owner.GetNode<TileMapLayer>("%Terrain").Position;
        node.SelfModulate = new Color(look.DotColor.R, look.DotColor.G, look.DotColor.B);
        _multiMesh = CreateMultiMesh(_crowd.Count, look);
        node.Multimesh = _multiMesh;
        _buffer = new float[_crowd.Count * FloatsPerInstance];
        Draw(_crowd.X, _crowd.Y);
    }

    /// <summary>毎フレーム呼ぶ。ログは出さない。</summary>
    public void Process(double delta)
    {
        var timeScale = Crowd.TimeScaleOf(_timeControl.State, _clock.Speed, _rules);
        if (timeScale == 0f)
        {
            return;
        }
        _pendingSeconds += delta * timeScale;
        var steps = 0;
        while (_pendingSeconds >= Crowd.StepSeconds && steps < MaxStepsPerFrame)
        {
            _pendingSeconds -= Crowd.StepSeconds;
            steps++;
        }
        if (steps == MaxStepsPerFrame)
        {
            _pendingSeconds = Math.Min(_pendingSeconds, Crowd.StepSeconds);
        }
        if (steps == 0)
        {
            return;
        }

        // 1 フレームの中では星の状態は変わらないので、状態は 1 回だけ判定する
        var mode = Crowd.ModeOf(_timeControl.World, _rules);
        if (_gpu is null)
        {
            for (var i = 0; i < steps; i++)
            {
                _crowd.Step(mode, _field);
            }
            Draw(_crowd.X, _crowd.Y);
            return;
        }
        _gpu.Step(mode, steps);
        _gpu.ReadPositions(_gpuX, _gpuY);
        Draw(_gpuX, _gpuY);
    }

    /// <summary>地表が描き直されたら、ポテンシャルフィールドを作り直す。</summary>
    public void OnTerrainDrawn(TileType[,] tiles)
    {
        _field = PotentialField.Build(tiles, _rules.TileForces);
        _gpu?.SetField(_field);
    }

    /// <summary>世界再創生で、始めの配置に戻す。</summary>
    public void OnWorldReset()
    {
        _crowd.Reset();
        _gpu?.Upload(_crowd);
        Draw(_crowd.X, _crowd.Y);
    }

    public void Dispose() => _gpu?.Dispose();

    /// <summary>群衆の点を描く MultiMesh を作る（計測用のシーンでも使う）。</summary>
    internal static MultiMesh CreateMultiMesh(int count, CrowdLook look) => new()
    {
        TransformFormat = MultiMesh.TransformFormatEnum.Transform2D,
        Mesh = new QuadMesh { Size = new Vector2(look.DotSizePixels, look.DotSizePixels) },
        InstanceCount = count,
    };

    /// <summary>位置（タイルの単位）を MultiMesh のバッファ（ピクセル）に書き込む。</summary>
    internal static void WriteBuffer(ReadOnlySpan<float> x, ReadOnlySpan<float> y, float[] buffer)
    {
        // 2D の変換の並び: (basis.x.x, basis.y.x, 空き, origin.x, basis.x.y, basis.y.y, 空き, origin.y)
        for (var i = 0; i < x.Length; i++)
        {
            var offset = i * FloatsPerInstance;
            buffer[offset] = 1f;
            buffer[offset + 3] = x[i] * WorldScreen.TileSize;
            buffer[offset + 5] = 1f;
            buffer[offset + 7] = y[i] * WorldScreen.TileSize;
        }
    }

    private void Draw(ReadOnlySpan<float> x, ReadOnlySpan<float> y)
    {
        WriteBuffer(x, y, _buffer);
        _multiMesh.Buffer = _buffer;
    }
}
