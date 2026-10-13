namespace TheogonyX.Core;

/// <summary>セーブに残すデッキの状態。並びは先頭が山札の一番上。</summary>
/// <param name="DrawPile">山札。</param>
/// <param name="Hand">手札。</param>
/// <param name="DiscardPile">捨札。</param>
/// <param name="UsedSymbolId">夢の改ざんに使って、まだ手札にあるカード。使っていなければ null。</param>
/// <param name="ShuffleCount">これまでのシャッフルの回数。次のシャッフルの乱数を決める。</param>
public sealed record OracleDeckState(
    IReadOnlyList<string> DrawPile,
    IReadOnlyList<string> Hand,
    IReadOnlyList<string> DiscardPile,
    string? UsedSymbolId,
    int ShuffleCount);

/// <summary>
/// 神託のデッキ（docs/domain/oracle-deck.md）。山札・手札・捨札でルーンを回す。第 1 段では、ルーンの ID は記号の ID と同じ。
/// シャッフルの乱数は (シード, シャッフルの回数) から作るので、回数だけ残せば同じ並びを作り直せる。
/// </summary>
public sealed class OracleDeck
{
    private readonly int _seed;
    private readonly List<string> _drawPile = [];
    private readonly List<string> _hand = [];
    private readonly List<string> _discardPile = [];

    /// <summary>最初のデッキを作る。山札にルーンを入れてシャッフルし、手札を引く。</summary>
    public OracleDeck(int seed, DeckRules rules)
    {
        ArgumentNullException.ThrowIfNull(rules);
        _seed = seed;
        foreach (var rune in rules.InitialRunes)
        {
            for (var i = 0; i < rune.Count; i++)
            {
                _drawPile.Add(rune.SymbolId);
            }
        }
        Shuffle(_drawPile);
        Draw(rules.HandSize);
    }

    /// <summary>山札。先頭が一番上。</summary>
    public IReadOnlyList<string> DrawPile => _drawPile;

    public IReadOnlyList<string> Hand => _hand;

    public IReadOnlyList<string> DiscardPile => _discardPile;

    /// <summary>夢の改ざんに使って、まだ手札にあるカード。</summary>
    public string? UsedSymbolId { get; private set; }

    public int ShuffleCount { get; private set; }

    /// <summary>改ざんに使うカードを覚える（手札からはまだ動かさない）。手札に無いか、すでに使っていれば <see cref="InvalidOperationException"/>。</summary>
    public void Use(string symbolId)
    {
        ArgumentNullException.ThrowIfNull(symbolId);
        if (UsedSymbolId is not null || !_hand.Contains(symbolId))
        {
            throw new InvalidOperationException($"手札に無いか、すでに使っている: symbolId={symbolId}");
        }
        UsedSymbolId = symbolId;
    }

    /// <summary>神託を下したとき。使ったカードがあれば捨札へ移し、1 枚を山札から手札に引く。使っていなければ何もしない。</summary>
    public void Settle()
    {
        if (UsedSymbolId is null)
        {
            return;
        }
        _hand.Remove(UsedSymbolId);
        _discardPile.Add(UsedSymbolId);
        UsedSymbolId = null;
        Draw(1);
    }

    /// <summary>
    /// 手札の 2 枚（番号は手札の位置）を捨札へ移し、山札から 2 枚を手札に引く（神託の作り方「ルーンの組」）。
    /// 番号が手札の範囲外か同じなら <see cref="ArgumentException"/>。
    /// </summary>
    public void SpendPair(int first, int second)
    {
        if (first < 0 || second < 0 || first >= _hand.Count || second >= _hand.Count || first == second)
        {
            throw new ArgumentException($"手札の番号が正しくない: first={first} second={second} handCount={_hand.Count}");
        }

        var cards = new[] { _hand[first], _hand[second] };
        // 後ろの番号から消して、前の番号がずれないようにする
        foreach (var index in new[] { first, second }.OrderByDescending(index => index))
        {
            _hand.RemoveAt(index);
        }
        _discardPile.AddRange(cards);
        Draw(2);
    }

    /// <summary>世界再創生など、夢を捨てたとき。使ったカードは手札に残る。</summary>
    public void ClearUsed() => UsedSymbolId = null;

    public OracleDeckState Capture() => new([.. _drawPile], [.. _hand], [.. _discardPile], UsedSymbolId, ShuffleCount);

    /// <summary>セーブの状態に戻す。値の検証は読み込む側で済ませておく。</summary>
    public void Restore(OracleDeckState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        _drawPile.Clear();
        _drawPile.AddRange(state.DrawPile);
        _hand.Clear();
        _hand.AddRange(state.Hand);
        _discardPile.Clear();
        _discardPile.AddRange(state.DiscardPile);
        UsedSymbolId = state.UsedSymbolId;
        ShuffleCount = state.ShuffleCount;
    }

    // 山札が足りなければ、捨札をシャッフルして山札に戻す。捨札も空なら、引けるだけ引く
    private void Draw(int count)
    {
        for (var i = 0; i < count; i++)
        {
            if (_drawPile.Count == 0)
            {
                if (_discardPile.Count == 0)
                {
                    return;
                }
                _drawPile.AddRange(_discardPile);
                _discardPile.Clear();
                Shuffle(_drawPile);
            }
            _hand.Add(_drawPile[0]);
            _drawPile.RemoveAt(0);
        }
    }

    private void Shuffle(List<string> cards)
    {
        var random = new Random(unchecked(_seed + ((ShuffleCount + 1) * (int)0x9E3779B1)));
        ShuffleCount++;
        for (var i = cards.Count - 1; i > 0; i--)
        {
            var j = random.Next(i + 1);
            (cards[i], cards[j]) = (cards[j], cards[i]);
        }
    }
}
