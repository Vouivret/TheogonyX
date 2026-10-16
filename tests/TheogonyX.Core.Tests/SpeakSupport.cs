using TheogonyX.Core;

namespace TheogonyX.Core.Tests;

/// <summary>
/// 神託を 1 回下す操作の代わり（旧い「手札の 2 枚で神託を下す」を使っていたテストが、神威・歪み・依頼・デッキの動きを確かめるための道具）。
/// 手札の先頭のカードの記号の名詞と「守る」を聞き取った答えで語りかける。方言の状態が無ければ、ずれない器で付ける。
/// </summary>
internal static class SpeakSupport
{
    public static SpeakResult SpeakFirst(this ProphetDreams prophet)
    {
        ArgumentNullException.ThrowIfNull(prophet);
        if (prophet.Dialect is null)
        {
            prophet.AttachDialect(new DialectState(OracleDialectTests.Rules, OracleDialectTests.Silent, 1));
        }
        var symbol = prophet.SymbolOf(prophet.Hand[0]);
        var noun = prophet.Dialect!.Rules.Nouns.First(candidate => candidate.SymbolId == symbol);
        var hearing = prophet.Hear(new HeardAnswer(noun.Id, "VERB_PROTECT", HeardDestination.None, null, [])).Hearing!;
        return prophet.Speak(hearing, "神の言葉");
    }
}
