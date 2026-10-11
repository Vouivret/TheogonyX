using System.Security.Cryptography;
using System.Text;

namespace TheogonyX.Core.Protocol;

/// <summary>
/// QR コードのワンタイムトークンと、発行した <c>sessionToken</c> を持つ（docs/domain/companion-pairing.md）。
/// トークンの作り方（暗号用の乱数）は呼び出し側が渡す。照合は時間が一定の比較で行う。
/// <c>sessionToken</c> は上限まで持ち、超えたら一番古いものを無効にする。
/// </summary>
public sealed class PairingRegistry
{
    private readonly Func<string> _createToken;
    private readonly int _maxSessions;
    // 照合のたびにバイト列を作らないよう、UTF-8 にして持つ。古い順。
    private readonly List<byte[]> _sessionTokens = [];

    /// <param name="createToken">新しいトークンを返す。呼ぶたびに違う値を返すこと。</param>
    /// <param name="maxSessions">同時に有効な <c>sessionToken</c> の上限。</param>
    public PairingRegistry(Func<string> createToken, int maxSessions)
    {
        ArgumentNullException.ThrowIfNull(createToken);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxSessions);
        _createToken = createToken;
        _maxSessions = maxSessions;
        CurrentToken = createToken();
    }

    /// <summary>QR コードに入れる、今のワンタイムトークン。</summary>
    public string CurrentToken { get; private set; }

    /// <summary>
    /// トークンが合えば、<c>sessionToken</c> を発行し、ワンタイムトークンを新しいものに替えて true を返す。
    /// 上限に達していたら、一番古い <c>sessionToken</c> を無効にする。
    /// 呼び出し側は true のとき、QR コードを <see cref="CurrentToken"/> で描き直し、
    /// 無効になった <c>sessionToken</c> の接続を <see cref="TryResume"/> で見つけて切る。
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
        if (_sessionTokens.Count >= _maxSessions)
        {
            _sessionTokens.RemoveAt(0);
        }
        _sessionTokens.Add(Encoding.UTF8.GetBytes(sessionToken));
        CurrentToken = _createToken();
        return true;
    }

    /// <summary>発行したすべての <c>sessionToken</c> を無効にする（PC の画面の「全セッションリセット」）。</summary>
    public void RevokeAll() => _sessionTokens.Clear();

    /// <summary><c>sessionToken</c> が今も有効なら true。</summary>
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
