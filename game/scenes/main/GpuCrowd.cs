using System;
using System.Runtime.InteropServices;
using Godot;
using TheogonyX.Core;

namespace TheogonyX.Scenes.Main;

/// <summary>
/// 群衆の 1 歩を GPU（Compute Shader、<c>crowd_step.glsl</c>）で計算する。コアの <see cref="Crowd"/> と同じ計算で、
/// 始めの状態は <see cref="Crowd"/> から受け取る（plan/feat-m3-crowd-gpu.md）。計算した位置は CPU に読み戻す。
/// </summary>
public sealed class GpuCrowd : IDisposable
{
    private const string ShaderPath = "res://scenes/main/crowd_step.glsl";
    private const uint GroupSize = 64;

    // crowd_step.glsl の binding の順
    private const int PositionXBinding = 0;
    private const int PositionYBinding = 1;
    private const int HeadingBinding = 2;
    private const int TargetXBinding = 3;
    private const int TargetYBinding = 4;
    private const int ForceXBinding = 5;
    private const int ForceYBinding = 6;
    private const int FlockBinding = 7;
    private const int BindingCount = 8;

    // crowd_step.glsl の mode
    private const uint FlockClearMode = 2;
    private const uint FlockAccumulateMode = 3;

    // 群れの集計のマスごとの整数の数（人数・x の和・y の和・向き x の和・向き y の和）
    private const int FlockValuesPerCell = 5;

    private readonly RenderingDevice _device;
    private readonly CrowdRules _rules;
    private readonly int _count;
    private readonly uint _columns;
    private readonly uint _rows;
    private readonly Rid _shader;
    private readonly Rid _pipeline;
    private readonly Rid[] _buffers = new Rid[BindingCount];
    private readonly Rid _uniformSet;
    private readonly uint _gridColumns;
    private readonly uint _gridRows;
    private uint _seed;
    private uint _turnSteps;

    private GpuCrowd(RenderingDevice device, Rid shader, Crowd crowd, CrowdRules rules, PotentialField field)
    {
        _device = device;
        _shader = shader;
        _rules = rules;
        _count = crowd.Count;
        _columns = (uint)crowd.Columns;
        _rows = (uint)crowd.Rows;
        _pipeline = device.ComputePipelineCreate(shader);
        // 群れの動きを使わないときも、binding を埋めるために 1 マス分のバッファを作る
        _gridColumns = (uint)(crowd.Flock?.GridColumns ?? 1);
        _gridRows = (uint)(crowd.Flock?.GridRows ?? 1);

        var cells = (int)(_columns * _rows);
        var uniforms = new Godot.Collections.Array<RDUniform>();
        for (var binding = 0; binding < BindingCount; binding++)
        {
            var values = binding switch
            {
                ForceXBinding or ForceYBinding => cells,
                FlockBinding => (int)(_gridColumns * _gridRows) * FlockValuesPerCell,
                _ => _count,
            };
            // float も int も 4 バイト
            _buffers[binding] = device.StorageBufferCreate((uint)(values * sizeof(float)));
            var uniform = new RDUniform { UniformType = RenderingDevice.UniformType.StorageBuffer, Binding = binding };
            uniform.AddId(_buffers[binding]);
            uniforms.Add(uniform);
        }
        _uniformSet = device.UniformSetCreate(uniforms, shader, 0);
        Upload(crowd);
        SetField(field);
    }

    /// <summary>今の CPU 版が持つ歩数。<see cref="Upload"/> で合わせ、<see cref="Step"/> で進む。</summary>
    public uint Steps { get; private set; }

    /// <summary>GPU 版を作る。Compute Shader が使えない環境（Compatibility の描画方式、headless など）では null と理由を返す。</summary>
    public static GpuCrowd? TryCreate(Crowd crowd, CrowdRules rules, PotentialField field, out string reason)
    {
        var device = RenderingServer.CreateLocalRenderingDevice();
        if (device is null)
        {
            reason = "no_rendering_device";
            return null;
        }
        var spirV = GD.Load<RDShaderFile>(ShaderPath)?.GetSpirV();
        if (spirV is null || !string.IsNullOrEmpty(spirV.CompileErrorCompute))
        {
            device.Free();
            reason = "shader_compile_failed";
            return null;
        }
        var shader = device.ShaderCreateFromSpirV(spirV);
        if (!shader.IsValid)
        {
            device.Free();
            reason = "shader_create_failed";
            return null;
        }
        reason = "";
        return new GpuCrowd(device, shader, crowd, rules, field);
    }

    /// <summary>CPU 版の今の状態（位置・向き・向かう点・歩数）を GPU に送る（作ったとき、世界再創生）。</summary>
    public void Upload(Crowd crowd)
    {
        if (crowd.Count != _count)
        {
            throw new ArgumentException($"人数が違う: expected={_count} actual={crowd.Count}", nameof(crowd));
        }
        _seed = crowd.Seed;
        _turnSteps = crowd.TurnSteps;
        Steps = crowd.Steps;
        Update(PositionXBinding, crowd.X);
        Update(PositionYBinding, crowd.Y);
        Update(HeadingBinding, crowd.Heading);
        Update(TargetXBinding, crowd.TargetX);
        Update(TargetYBinding, crowd.TargetY);
    }

    /// <summary>ポテンシャルフィールドを GPU に送る（地表が変わったとき）。</summary>
    public void SetField(PotentialField field)
    {
        Update(ForceXBinding, field.ForceX);
        Update(ForceYBinding, field.ForceY);
    }

    /// <summary><paramref name="steps"/> 歩進めて、終わるまで待つ。停止なら何もしない。</summary>
    public void Step(CrowdMode mode, int steps)
    {
        if (mode == CrowdMode.Stopped || steps <= 0)
        {
            return;
        }
        var crowdGroups = Groups((uint)_count);
        var flockGroups = Groups(_gridColumns * _gridRows);
        var flocking = mode == CrowdMode.Wander && _rules.Flock.Enabled;
        var list = _device.ComputeListBegin();
        _device.ComputeListBindComputePipeline(list, _pipeline);
        _device.ComputeListBindUniformSet(list, _uniformSet, 0);
        for (var i = 0; i < steps; i++)
        {
            if (flocking)
            {
                // 全員の集計を終えてから動かす（Crowd.Wander と同じ順）
                Dispatch(list, FlockClearMode, flockGroups);
                Dispatch(list, FlockAccumulateMode, crowdGroups);
            }
            Dispatch(list, (uint)mode, crowdGroups);
            Steps++;
        }
        _device.ComputeListEnd();
        _device.Submit();
        _device.Sync();
    }

    /// <summary>GPU の位置を読み戻す。</summary>
    public void ReadPositions(float[] x, float[] y)
    {
        MemoryMarshal.Cast<byte, float>(_device.BufferGetData(_buffers[PositionXBinding])).CopyTo(x);
        MemoryMarshal.Cast<byte, float>(_device.BufferGetData(_buffers[PositionYBinding])).CopyTo(y);
    }

    public void Dispose()
    {
        // 依存の逆順に消す（uniform set → バッファ → パイプライン → シェーダー）
        _device.FreeRid(_uniformSet);
        foreach (var buffer in _buffers)
        {
            _device.FreeRid(buffer);
        }
        _device.FreeRid(_pipeline);
        _device.FreeRid(_shader);
        _device.Free();
    }

    private static uint Groups(uint invocations) => (invocations + GroupSize - 1) / GroupSize;

    /// <summary>今の歩数で 1 回の dispatch を積み、次の dispatch が結果を読めるよう間で待つ。</summary>
    private void Dispatch(long list, uint mode, uint groups)
    {
        var flock = _rules.Flock;
        var constants = new PushConstants
        {
            Count = (uint)_count,
            Seed = _seed,
            Step = Steps,
            Mode = mode,
            TurnSteps = _turnSteps,
            Columns = _columns,
            Rows = _rows,
            StepSeconds = Crowd.StepSeconds,
            WalkSpeed = _rules.WalkSpeed,
            FieldScale = _rules.FieldScale,
            FrenzySpeed = _rules.FrenzySpeed,
            FlockEnabled = flock.Enabled ? 1u : 0u,
            GridColumns = _gridColumns,
            GridRows = _gridRows,
            CellSize = flock.CellSize,
            AlignWeight = flock.AlignWeight,
            CrowdWeight = flock.CrowdWeight,
            TargetNeighbors = flock.TargetNeighbors,
        };
        var bytes = MemoryMarshal.AsBytes(new ReadOnlySpan<PushConstants>(in constants));
        _device.ComputeListSetPushConstant(list, bytes, (uint)bytes.Length);
        _device.ComputeListDispatch(list, groups, 1, 1);
        _device.ComputeListAddBarrier(list);
    }

    private void Update(int binding, ReadOnlySpan<float> values)
    {
        var bytes = MemoryMarshal.AsBytes(values);
        _device.BufferUpdate(_buffers[binding], 0, (uint)bytes.Length, bytes);
    }

    // crowd_step.glsl の Params と同じ並び（80 バイト）
    [StructLayout(LayoutKind.Sequential)]
    private struct PushConstants
    {
        public uint Count;
        public uint Seed;
        public uint Step;
        public uint Mode;
        public uint TurnSteps;
        public uint Columns;
        public uint Rows;
        public float StepSeconds;
        public float WalkSpeed;
        public float FieldScale;
        public float FrenzySpeed;
        public uint FlockEnabled;
        public uint GridColumns;
        public uint GridRows;
        public float CellSize;
        public float AlignWeight;
        public float CrowdWeight;
        public float TargetNeighbors;
        public float Padding0;
        public float Padding1;
    }
}
