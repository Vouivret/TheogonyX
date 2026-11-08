namespace TheogonyX.Core;

/// <summary>教え（記号 × 種別）。字の神託では、読んだときの危機で決まる読みが付く（docs/domain/script-decipher.md の 9）。</summary>
/// <param name="Reading">読み。自由文の神託・読みを当てない記号の教えは null。</param>
public sealed record Dogma(string TargetSymbolId, DogmaType Type, float Intensity, OracleReading? Reading = null)
{
    /// <summary>同じ教えか。記号・種別・読みで決める（強度は教えの効き目の大きさで、教えの同じさには入らない）。</summary>
    public bool IsSameTeaching(Dogma other) => TargetSymbolId == other.TargetSymbolId && Type == other.Type && Reading == other.Reading;
}
