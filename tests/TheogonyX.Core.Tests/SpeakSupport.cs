using TheogonyX.Core;

namespace TheogonyX.Core.Tests;

/// <summary>
/// 神託を 1 回下す操作の代わり（旧い「手札の 2 枚で神託を下す」を使っていたテストが、神威・歪み・依頼・デッキの動きを確かめるための道具）。
/// 手札の先頭のカードの記号の崇拝（強度 0.7）を、行き先なしで下す。
/// </summary>
internal static class SpeakSupport
{
    public static SpeakResult SpeakFirst(this ProphetDreams prophet)
    {
        ArgumentNullException.ThrowIfNull(prophet);
        return prophet.Pronounce(new Dogma(prophet.SymbolOf(prophet.Hand[0]), DogmaType.Worship, 0.7f), TravelDirection.None);
    }
}
