namespace TheogonyX.Core;

/// <summary>地表のマス。列・行は 0 から数える（左上が 0, 0）。</summary>
public readonly record struct TerrainCell(int Column, int Row);
