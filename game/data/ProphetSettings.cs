namespace TheogonyX.Data;

/// <summary>予言者の表示の調整値（docs/domain/ambience.md）。</summary>
/// <param name="Column">立たせる地表の列（左から 1 列目と数える）。</param>
/// <param name="FrameSeconds">待機アニメーションの 1 コマの秒数。</param>
public sealed record ProphetSettings(int Column, float FrameSeconds);
