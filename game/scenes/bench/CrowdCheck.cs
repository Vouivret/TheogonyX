using System;
using System.Globalization;
using Godot;
using TheogonyX.Core;
using TheogonyX.Data;
using TheogonyX.Scenes.Main;

namespace TheogonyX.Scenes.Bench;

/// <summary>
/// 群衆の GPU 版と CPU 版の突き合わせ（TS-crowd-009、plan/feat-m3-crowd-gpu.md）。
/// CPU 版を状態の移り変わりに沿って動かし、8 歩ごとに CPU 版の状態を GPU に送り直して同じ 8 歩を進め、位置の差を比べる。
/// 送り直すのは、浮動小数点の小さな差が地表の端やマスの境目での分岐を変え、長く動かすと差が広がる（計算の誤りではない）ため。
/// 区切りごとに差の最大値をログに出す。すべて許容誤差に収まれば終了コード 0、収まらなければ 1、GPU が使えなければ 2 で終わる。
/// 起動: Godot のコマンドに <c>-- --crowd-check</c> を付ける（<see cref="WorldCore"/> がゲームの代わりにこのノードを置く）。
/// </summary>
public partial class CrowdCheck : Node
{
    public const string CommandLineArg = "--crowd-check";

    // 許容誤差（タイルの単位）。同じ状態から 8 歩なら、差は浮動小数点の誤差（2026-10-12 の計測で 1e-5 未満）に収まる。
    // 1 px（1/16 タイル）より十分小さくし、式の小さな誤りも見逃さない
    private const float Tolerance = 1e-3f;
    private const int Seed = 12345;
    // ゲームと同じく、1 回に最大 8 歩ずつ GPU に投げる
    private const int StepsPerBatch = 8;

    public override void _Ready()
    {
        var exitCode = Run();
        GetTree().Quit(exitCode);
    }

    private static int Run()
    {
        var rulesJson = WorldCore.ReadText("res://data/rules.json");
        var rules = GameDataLoader.ParseCrowd(rulesJson);
        var terrainRules = GameDataLoader.ParseTerrain(rulesJson);
        var world = WorldState.CreateInitial(GameDataLoader.ParseWorldRules(rulesJson));
        var initialField = PotentialField.Build(TerrainClassifier.ClassifyTiles(world, terrainRules), rules.TileForces);
        var scorchedField = PotentialField.Build(LeftHalfScorched(terrainRules), rules.TileForces);

        var cpu = new Crowd(Seed, rules, terrainRules.Columns, terrainRules.Rows, holySiteX: 8f);
        using var gpu = GpuCrowd.TryCreate(cpu, rules, initialField, out var reason);
        if (gpu is null)
        {
            GD.PushError($"event=crowd_check_skipped reason={reason}");
            return 2;
        }
        var gpuX = new float[cpu.Count];
        var gpuY = new float[cpu.Count];
        var passed = true;

        void Phase(string name, CrowdMode mode, PotentialField field, int steps)
        {
            gpu.SetField(field);
            var maxDiff = 0f;
            for (var done = 0; done < steps; done += StepsPerBatch)
            {
                var batch = Math.Min(StepsPerBatch, steps - done);
                gpu.Upload(cpu);
                for (var i = 0; i < batch; i++)
                {
                    cpu.Step(mode, field);
                }
                gpu.Step(mode, batch);
                gpu.ReadPositions(gpuX, gpuY);
                for (var i = 0; i < cpu.Count; i++)
                {
                    maxDiff = Math.Max(maxDiff, Math.Max(Math.Abs(cpu.X[i] - gpuX[i]), Math.Abs(cpu.Y[i] - gpuY[i])));
                }
            }
            var ok = maxDiff <= Tolerance;
            passed &= ok;
            GD.Print(string.Format(
                CultureInfo.InvariantCulture, "event=crowd_check phase={0} steps={1} maxDiff={2:G6} result={3}",
                name, cpu.Steps, maxDiff, ok ? "pass" : "fail"));
        }

        Phase("wander_initial", CrowdMode.Wander, initialField, 600);
        Phase("wander_scorched", CrowdMode.Wander, scorchedField, 300);
        Phase("frenzy", CrowdMode.Frenzy, scorchedField, 300);
        Phase("stopped", CrowdMode.Stopped, scorchedField, 60);
        Phase("wander_after_frenzy", CrowdMode.Wander, initialField, 300);
        cpu.Reset();
        gpu.Upload(cpu);
        Phase("wander_after_reset", CrowdMode.Wander, initialField, 120);
        return passed ? 0 : 1;
    }

    private static TileType[,] LeftHalfScorched(TerrainRules terrainRules)
    {
        var tiles = new TileType[terrainRules.Columns, terrainRules.Rows];
        for (var column = 0; column < terrainRules.Columns; column++)
        {
            for (var row = 0; row < terrainRules.Rows; row++)
            {
                tiles[column, row] = column < terrainRules.Columns / 2 ? TileType.Scorched : TileType.Grassland;
            }
        }
        return tiles;
    }
}
