namespace TheogonyX.Core.Protocol;

/// <summary>検証を済ませた、コンパニオンからのメッセージ（docs/protocol/messages.md）。</summary>
/// <param name="Token"><c>PAIR</c> の <c>token</c>、<c>RESUME</c> の <c>sessionToken</c>。ほかの種類では空文字。</param>
/// <param name="Index"><c>TAMPER</c> の <c>index</c>（0 以上）。ほかの種類では 0。</param>
/// <param name="SymbolId"><c>TAMPER</c> の <c>symbolId</c>。ほかの種類では空文字。</param>
public sealed record CompanionMessage(CompanionMessageType Type, string Token = "", int First = 0, int Second = 0);
