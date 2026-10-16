namespace TheogonyX.Core.Protocol;

/// <summary>検証を済ませた、コンパニオンからのメッセージ（docs/protocol/messages.md）。</summary>
/// <param name="Token"><c>PAIR</c> の <c>token</c>、<c>RESUME</c> の <c>sessionToken</c>。ほかの種類では空文字。</param>
/// <param name="Text"><c>SPEAK</c> の <c>text</c>。ほかの種類では空文字。</param>
/// <param name="NounId"><c>PRONOUNCE</c> の <c>noun</c>（名詞の ID。無ければ null）。語の一覧にあるかは、ゲームの側（コア）が確かめる。</param>
/// <param name="VerbId"><c>PRONOUNCE</c> の <c>verb</c>（動詞の ID。無ければ null）。</param>
/// <param name="Destination"><c>PRONOUNCE</c> の <c>destination</c>（東・西・なし）。</param>
public sealed record CompanionMessage(
    CompanionMessageType Type, string Token = "", string Text = "", string? NounId = null, string? VerbId = null,
    HeardDestination Destination = HeardDestination.None);
