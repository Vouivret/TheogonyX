namespace TheogonyX.Core;

/// <summary>（対象の記号, 種別）のドグマが、強度 1.0 のときに1年で環境を変える量。</summary>
public sealed record DogmaEffect(string SymbolId, DogmaType DogmaType, EnvironmentValues PerYear);
