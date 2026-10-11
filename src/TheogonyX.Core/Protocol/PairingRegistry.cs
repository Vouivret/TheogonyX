using System.Security.Cryptography;
using System.Text;

namespace TheogonyX.Core.Protocol;

/// <summary>
/// QR コードのワンタイムトークンと、発行した <c>sessionToken</c> を持つ（docs/domain/companion-pairing.md）。
/// トークンの作り方（暗号用の乱数）は呼び出し側が渡す。照合は時間が一定の比較で行う。
/// </summary>
public sealed class PairingRegistry
{
    private readonly Func<string> _createToken;
    // 照合のたびにバイト列を作らないよう、UTF-8 にして持つ
    private readonly List<byte[]> _sessionTokens = [];

    /// <param name="createToken">新しいトークンを返す。呼ぶたびに違う値を返すこと。</param>
    public PairingRegistry(Func<string> createToken)
    {
        ArgumentNullException.ThrowIfNull(createToken);
        _createToken = createToken;
        CurrentToken = createToken();
    }

    /// <summary>QR コードに入れる、今のワンタイムトークン。</summary>
    public string CurrentToken { get; private set; }

    /// <summary>
    /// トークンが合えば、<c>sessionToken</c> を発行し、ワンタイムトークンを新しいものに替えて true を返す。
    /// 呼び出し側は true のとき、QR コードを <see cref="CurrentToken"/> で描き直す。
    /// </summary>
    public bool TryPair(string token, out string sessionToken)
    {
        ArgumentNullException.ThrowIfNull(token);
        if (!FixedTimeEquals(token, CurrentToken))
        {
            sessionToken = string.Empty;
            return false;
        }

        sessionToken = _createToken();
        _sessionTokens.Add(Encoding.UTF8.GetBytes(sessionToken));
        CurrentToken = _createToken();
        return true;
    }

    public bool TryResume(string sessionToken)
    {
        ArgumentNullException.ThrowIfNull(sessionToken);
        var received = Encoding.UTF8.GetBytes(sessionToken);
        var found = false;
        foreach (var issued in _sessionTokens)
        {
            // 見つかっても最後まで比べる（どれと合ったかを時間で漏らさない）
            found |= CryptographicOperations.FixedTimeEquals(received, issued);
        }

        return found;
    }

    private static bool FixedTimeEquals(string left, string right) =>
        CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(left), Encoding.UTF8.GetBytes(right));
}
