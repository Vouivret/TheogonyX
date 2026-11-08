namespace TheogonyX.Core;

/// <summary>
/// 住人が信じている教え（記号 × 種別 × 読み）と、信仰の強さ（0 より大きく 1 以下）。
/// 教えの同じさは記号・種別・読みで決める（強度は教えの効き目の大きさで、信仰の強さとは別）。
/// </summary>
public sealed record Belief(string SymbolId, DogmaType Type, double Strength, OracleReading? Reading = null)
{
    public bool IsSameTeaching(Belief other) => SymbolId == other.SymbolId && Type == other.Type && Reading == other.Reading;

    public bool IsSameTeaching(Dogma dogma) => SymbolId == dogma.TargetSymbolId && Type == dogma.Type && Reading == dogma.Reading;
}
