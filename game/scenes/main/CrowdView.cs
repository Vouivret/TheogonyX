using System;
using Godot;
using TheogonyX.Core;
using TheogonyX.Data;

namespace TheogonyX.Scenes.Main;

/// <summary>群衆を点で描く（docs/domain/crowd.md）。計算はコアの <see cref="Crowd"/> が行う。</summary>
public sealed class CrowdView
{
    // 1 フレームで進める歩数の上限。遅れてたまった分は捨てる（plan/feat-m3-crowd-cpu.md）。高速（3 倍）でも 30fps まで追いつける。
    private const int MaxStepsPerFrame = 8;

    // 予言者の絵の幅（docs/art-guide.md の「予言者のスプライト」）。聖地は予言者の左右の中央。
    private const int ProphetWidthPixels = 32;

    // MultiMesh の 2D の変換 1 つ分の float の数
    private const int FloatsPerInstance = 8;

    private readonly TimeControl _timeControl;
    private readonly AutoPlayClock _clock;
    private readonly CrowdRules _rules;
    private readonly Crowd _crowd;
    private readonly MultiMesh _multiMesh;
    private readonly float[] _buffer;
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

        var node = owner.GetNode<MultiMeshInstance2D>("%Crowd");
        node.Position = owner.GetNode<TileMapLayer>("%Terrain").Position;
        node.SelfModulate = new Color(look.DotColor.R, look.DotColor.G, look.DotColor.B);
        _multiMesh = new MultiMesh
        {
            TransformFormat = MultiMesh.TransformFormatEnum.Transform2D,
            Mesh = new QuadMesh { Size = new Vector2(look.DotSizePixels, look.DotSizePixels) },
            InstanceCount = _crowd.Count,
        };
        node.Multimesh = _multiMesh;
        _buffer = new float[_crowd.Count * FloatsPerInstance];
        Draw();
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
            _crowd.Step(Crowd.ModeOf(_timeControl.World, _rules), _field);
            _pendingSeconds -= Crowd.StepSeconds;
            steps++;
        }
        if (steps == MaxStepsPerFrame)
        {
            _pendingSeconds = Math.Min(_pendingSeconds, Crowd.StepSeconds);
        }
        Draw();
    }

    /// <summary>地表が描き直されたら、ポテンシャルフィールドを作り直す。</summary>
    public void OnTerrainDrawn(TileType[,] tiles) => _field = PotentialField.Build(tiles, _rules.TileForces);

    /// <summary>世界再創生で、始めの配置に戻す。</summary>
    public void OnWorldReset()
    {
        _crowd.Reset();
        Draw();
    }

    private void Draw()
    {
        var x = _crowd.X;
        var y = _crowd.Y;
        // 2D の変換の並び: (basis.x.x, basis.y.x, 空き, origin.x, basis.x.y, basis.y.y, 空き, origin.y)
        for (var i = 0; i < _crowd.Count; i++)
        {
            var offset = i * FloatsPerInstance;
            _buffer[offset] = 1f;
            _buffer[offset + 3] = x[i] * WorldScreen.TileSize;
            _buffer[offset + 5] = 1f;
            _buffer[offset + 7] = y[i] * WorldScreen.TileSize;
        }
        _multiMesh.Buffer = _buffer;
    }
}
