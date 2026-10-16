namespace TheogonyX.Core;

/// <summary>
/// 神託の命令の待ち行列（docs/domain/multiplayer.md）。語りかけは、ここで順番の番号を振ってから <see cref="ProphetDreams"/> へ実行する。
/// 語りかけられるかの検証（崩壊・神威）は <see cref="ProphetDreams.SpeakRejection"/> が行う。
/// </summary>
public sealed class OracleCommandQueue
{
    private readonly ProphetDreams _prophet;
    private long _nextSequence = 1;

    public OracleCommandQueue(ProphetDreams prophet, TimeControl timeControl)
    {
        ArgumentNullException.ThrowIfNull(prophet);
        ArgumentNullException.ThrowIfNull(timeControl);

        _prophet = prophet;
    }

    /// <summary>
    /// 聞き違えた結果（<see cref="ProphetDreams.Hear"/>）で神託を下す。順番の番号を振り、語りかけられるか確かめてから実行する。
    /// TODO(AI_UNSURE): 多人数では次の年の始めに実行する形にする（[multiplayer.md]）。今はその場で実行する。
    /// </summary>
    public SpeakOutcome SubmitSpeak(Hearing hearing, string godText)
    {
        var sequence = _nextSequence++;
        return _prophet.SpeakRejection() is { } reason
            ? new SpeakOutcome(sequence, reason, null)
            : new SpeakOutcome(sequence, null, _prophet.Speak(hearing, godText));
    }
}
