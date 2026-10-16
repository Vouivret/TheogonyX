namespace TheogonyX.Core.Protocol;

/// <summary>コンパニオンからホストへのメッセージの種類（docs/protocol/messages.md）。</summary>
public enum CompanionMessageType
{
    Pair,
    Resume,
    StartStream,
    StopStream,
    Speak,
    Pronounce,
}
