namespace TheogonyX.Core;

/// <summary>神託に関わる操作の種類。</summary>
public enum OracleCommandKind
{
    /// <summary>夢のコマを概念カードと入れ替える。</summary>
    Tamper,

    /// <summary>神託を下す。</summary>
    Pronounce,
}

/// <summary>
/// 神託に関わる操作 1 つ（docs/domain/multiplayer.md）。PC の画面もスマホも、直接は実行せず、命令にして
/// <see cref="OracleCommandQueue"/> に積む。
/// </summary>
/// <param name="Kind">操作の種類。</param>
/// <param name="Index">改ざんする夢のコマの番号（0 から）。<see cref="OracleCommandKind.Pronounce"/> では使わない。</param>
/// <param name="SymbolId">入れ替える記号。<see cref="OracleCommandKind.Pronounce"/> では null。</param>
/// <param name="Year">積んだ年（今の星の年）。</param>
/// <param name="Sequence">順番の番号（積んだ順に 1 から振る通し番号）。</param>
public sealed record OracleCommand(OracleCommandKind Kind, int Index, string? SymbolId, int Year, long Sequence);

/// <summary>命令を実行した結果。</summary>
/// <param name="Command">実行した命令。</param>
/// <param name="RejectedReason">捨てたときの理由（ログの <c>reason</c>）。実行できたら null。</param>
/// <param name="Result">神託を下せたときの結果。改ざんと、捨てた命令では null。</param>
public sealed record OracleCommandOutcome(OracleCommand Command, string? RejectedReason, OracleResult? Result)
{
    /// <summary>実行できたか。</summary>
    public bool Accepted => RejectedReason is null;
}
