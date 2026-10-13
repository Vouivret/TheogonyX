namespace TheogonyX.Core;

/// <summary>
/// 神託を下す操作 1 つ（docs/domain/multiplayer.md）。PC の画面もスマホも、直接は実行せず、命令にして
/// <see cref="OracleCommandQueue"/> に積む。
/// </summary>
/// <param name="First">1 枚目（ドグマの対象）の手札の番号（0 から）。</param>
/// <param name="Second">2 枚目の手札の番号。</param>
/// <param name="Year">積んだ年（今の星の年）。</param>
/// <param name="Sequence">順番の番号（積んだ順に 1 から振る通し番号）。</param>
public sealed record OracleCommand(int First, int Second, int Year, long Sequence);

/// <summary>命令を実行した結果。</summary>
/// <param name="Command">実行した命令。</param>
/// <param name="RejectedReason">捨てたときの理由（ログの <c>reason</c>）。実行できたら null。</param>
/// <param name="Result">神託を下せたときの結果。捨てた命令では null。</param>
public sealed record OracleCommandOutcome(OracleCommand Command, string? RejectedReason, OracleResult? Result)
{
    /// <summary>実行できたか。</summary>
    public bool Accepted => RejectedReason is null;
}
