using System;
using System.Collections.Generic;
using System.Globalization;
using Godot;
using TheogonyX.Core;
using TheogonyX.Data;
using TheogonyX.Scenes.Main;

namespace TheogonyX.Scenes.Bench;

/// <summary>
/// 絵（<c>game/assets/sprites/*.png</c>）をパレットの色に減色する道具（plan/feat-palette-art.md、TS-palette-006）。
/// 画素ごとに、アルファが半分未満なら透明に、それ以外は不透明にして、パレットのいちばん近い色に置き換える（ディザは使わない）。
/// 元のファイルを上書きする（元の絵は Git の履歴に残る）。終わりに書き込んだ PNG を読み直し、
/// パレットの外の色と半透明の画素が無いことを数えて、ログに出す。すべて 0 なら終了コード 0、そうでなければ 1 で終わる。
/// 起動: Godot のコマンドに <c>-- --quantize-sprites</c> を付ける（<see cref="WorldCore"/> がゲームの代わりにこのノードを置く）。
/// 終わったら、Godot エディタで一度開いて取り込み（<c>.import</c>）をやり直させる。
/// </summary>
public partial class SpriteQuantizer : Node
{
    public const string CommandLineArg = "--quantize-sprites";

    private const string SpriteDirectory = "res://assets/sprites";
    private const float OpaqueAlphaAtOrAbove = 0.5f;

    public override void _Ready() => GetTree().Quit(Run());

    private static int Run()
    {
        var palette = GameDataLoader.ParsePalette(WorldCore.ReadText("res://data/palette.json"));
        var passed = true;
        foreach (var name in DirAccess.GetFilesAt(SpriteDirectory))
        {
            if (!name.EndsWith(".png", StringComparison.Ordinal))
            {
                continue;
            }
            var path = ProjectSettings.GlobalizePath($"{SpriteDirectory}/{name}");
            var image = Image.LoadFromFile(path);
            if (image is null)
            {
                GD.PushError($"event=sprite_quantize_failed file={name} reason=load_failed");
                passed = false;
                continue;
            }
            image.Convert(Image.Format.Rgba8);
            Quantize(image, palette);
            var error = image.SavePng(path);
            if (error != Error.Ok)
            {
                GD.PushError($"event=sprite_quantize_failed file={name} reason=save_failed");
                passed = false;
                continue;
            }

            var (colors, outside, translucent) = Inspect(Image.LoadFromFile(path), palette);
            GD.Print(string.Create(
                CultureInfo.InvariantCulture, $"event=sprite_quantized file={name} colors={colors} outside={outside} translucent={translucent}"));
            passed &= outside == 0 && translucent == 0;
        }
        GD.Print($"event=sprites_quantized result={(passed ? "pass" : "fail")}");
        return passed ? 0 : 1;
    }

    private static void Quantize(Image image, Palette palette)
    {
        for (var y = 0; y < image.GetHeight(); y++)
        {
            for (var x = 0; x < image.GetWidth(); x++)
            {
                var pixel = image.GetPixel(x, y);
                if (pixel.A < OpaqueAlphaAtOrAbove)
                {
                    image.SetPixel(x, y, new Color(0, 0, 0, 0));
                    continue;
                }
                var nearest = palette.Nearest(Byte(pixel.R), Byte(pixel.G), Byte(pixel.B));
                image.SetPixel(x, y, Color.Color8(nearest.R, nearest.G, nearest.B));
            }
        }
    }

    // 使っている色の数、パレットの外の色の画素の数、半透明の画素の数
    private static (int Colors, int Outside, int Translucent) Inspect(Image image, Palette palette)
    {
        image.Convert(Image.Format.Rgba8);
        var used = new HashSet<(byte, byte, byte)>();
        var outside = 0;
        var translucent = 0;
        for (var y = 0; y < image.GetHeight(); y++)
        {
            for (var x = 0; x < image.GetWidth(); x++)
            {
                var pixel = image.GetPixel(x, y);
                if (pixel.A == 0)
                {
                    continue;
                }
                if (pixel.A < 1f)
                {
                    translucent++;
                }
                var rgb = (Byte(pixel.R), Byte(pixel.G), Byte(pixel.B));
                used.Add(rgb);
                if (!palette.Contains(rgb.Item1, rgb.Item2, rgb.Item3))
                {
                    outside++;
                }
            }
        }
        return (used.Count, outside, translucent);
    }

    private static byte Byte(float channel) => (byte)Math.Round(channel * 255f);
}
