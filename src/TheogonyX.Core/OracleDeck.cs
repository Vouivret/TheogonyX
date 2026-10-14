namespace TheogonyX.Core;

/// <summary>セーブに残すデッキの状態。並びは先頭が山札の一番上。</summary>
/// <param name="DrawPile">山札。</param>
/// <param name="Hand">手札。</param>
/// <param name="DiscardPile">捨札。</param>
/// <param name="ShuffleCount">これまでのシャッフルの回数。次のシャッフルの乱数を決める。</param>
/// <param name="CorruptionStage">カオス度の閾値を越えて、汚染ルーンを混ぜた回数（世界再創生まで、同じ閾値では混ぜない）。</param>
/// <param name="LastBuildingId">建物のルーンを加えるために数えた、一番大きい建物の ID（まだ数えていなければ -1）。</param>
public sealed record OracleDeckState(
    IReadOnlyList<string> DrawPile,
    IReadOnlyList<string> Hand,
    IReadOnlyList<string> DiscardPile,
    int ShuffleCount,
    int CorruptionStage = 0,
    int LastBuildingId = -1);

/// <summary>
/// 神託のデッキ（docs/domain/oracle-deck.md）。山札・手札・捨札でルーンを回す。第 1 段では、ルーンの ID は記号の ID と同じ。
/// シャッフルの乱数は (シード, シャッフルの回数) から作るので、回数だけ残せば同じ並びを作り直せる。
/// </summary>
public sealed class OracleDeck
{
    private readonly int _seed;
    private readonly int _handSize;
    private readonly int _pruneChargesPerReset;
    private readonly IReadOnlyList<BuildingRune> _buildingRunes;
    private readonly int _maxSize;
    private readonly List<string> _drawPile = [];
    private readonly List<string> _hand = [];
    private readonly List<string> _discardPile = [];

    /// <summary>最初のデッキを作る。山札にルーンを入れてシャッフルし、手札を引く。</summary>
    public OracleDeck(int seed, DeckRules rules)
    {
        ArgumentNullException.ThrowIfNull(rules);
        _seed = seed;
        _handSize = rules.HandSize;
        _pruneChargesPerReset = rules.PruneChargesPerReset;
        _buildingRunes = rules.BuildingRunes ?? [];
        _maxSize = rules.MaxSize;
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

    public int ShuffleCount { get; private set; }

    /// <summary>カオス度の閾値を越えて、汚染ルーンを混ぜた回数。</summary>
    public int CorruptionStage { get; private set; }

    /// <summary>建物のルーンを加えるために数えた、一番大きい建物の ID。まだ数えていなければ -1。</summary>
    public int LastBuildingId { get; private set; } = -1;

    /// <summary>デッキの総数（山札・手札・捨札）。</summary>
    public int TotalCount => _drawPile.Count + _hand.Count + _discardPile.Count;

    /// <summary>
    /// 前に数えてから建った建物（ID が <see cref="LastBuildingId"/> より大きいもの）について、対応表のルーンを 1 棟 1 枚、捨札に加える。
    /// デッキの総数が上限に達していたら加えない（数えたことにはなる）。加えた枚数を返す。
    /// </summary>
    public int GainFromBuildings(IEnumerable<Building> buildings)
    {
        ArgumentNullException.ThrowIfNull(buildings);
        var gained = 0;
        foreach (var building in buildings.Where(building => building.Id > LastBuildingId).OrderBy(building => building.Id).ToList())
        {
            var rune = _buildingRunes.FirstOrDefault(rune => rune.BuildingType == building.Type.ToString());
            if (rune is not null && (_maxSize == 0 || TotalCount < _maxSize))
            {
                _discardPile.Add(rune.SymbolId);
                gained++;
            }
            LastBuildingId = building.Id;
        }
        return gained;
    }

    /// <summary>世界再創生: 建物は消えて ID が 0 から振り直されるので、数えた ID を戻す。</summary>
    public void ResetBuildingCount() => LastBuildingId = -1;

    /// <summary>精査（ルーンの削除）の残りの回数。セーブしない（起動し直すと 0）。</summary>
    public int PruneCharges { get; private set; }

    /// <summary>世界再創生: 精査の持ち分を、決まった回数に戻す。</summary>
    public void GrantPruneCharges() => PruneCharges = _pruneChargesPerReset;

    /// <summary>
    /// ルーンを 1 枚削除できない理由。できるなら null。
    /// 理由は <c>no_prune_charges</c>・<c>card_not_in_deck</c>・<c>deck_too_small</c>（削除するとデッキが 2 枚未満になる）。
    /// </summary>
    public string? PruneRejection(string cardId)
    {
        ArgumentNullException.ThrowIfNull(cardId);
        if (PruneCharges < 1)
        {
            return "no_prune_charges";
        }
        var total = _drawPile.Count + _hand.Count + _discardPile.Count;
        if (!_drawPile.Contains(cardId) && !_hand.Contains(cardId) && !_discardPile.Contains(cardId))
        {
            return "card_not_in_deck";
        }
        return total <= 2 ? "deck_too_small" : null;
    }

    /// <summary>
    /// ルーンを 1 枚、デッキから消す（精査の削除）。同じルーンが複数あるときは、捨札・山札・手札の順に 1 枚を選ぶ。
    /// 手札から消えたら、山札から補う。
    /// </summary>
    /// <exception cref="InvalidOperationException">削除できない（<see cref="PruneRejection"/> が null でない）。</exception>
    public void Prune(string cardId)
    {
        if (PruneRejection(cardId) is { } reason)
        {
            throw new InvalidOperationException($"削除できない: reason={reason}");
        }

        RemoveOne(cardId);
        PruneCharges--;
        if (_hand.Count < _handSize)
        {
            Draw(_handSize - _hand.Count);
        }
    }

    /// <summary>
    /// <paramref name="cardId"/> を 2 枚融合できない理由（デッキの側で分かるもの）。できるなら null。
    /// 理由は <c>no_prune_charges</c>・<c>card_not_in_deck</c>（2 枚に満たない）・<c>deck_too_small</c>（融合するとデッキが 2 枚未満になる）。
    /// </summary>
    public string? FuseRejection(string cardId)
    {
        ArgumentNullException.ThrowIfNull(cardId);
        if (PruneCharges < 1)
        {
            return "no_prune_charges";
        }
        if (_drawPile.Count(id => id == cardId) + _hand.Count(id => id == cardId) + _discardPile.Count(id => id == cardId) < 2)
        {
            return "card_not_in_deck";
        }
        return TotalCount - 1 < 2 ? "deck_too_small" : null;
    }

    /// <summary>
    /// <paramref name="cardId"/> を 2 枚消して、<paramref name="resultId"/> を 1 枚捨札に加える（精査の融合）。
    /// 消すカードは、削除と同じく捨札・山札・手札の順に選ぶ。精査の持ち分を 1 使い、手札から消えたら山札から補う。
    /// </summary>
    /// <exception cref="InvalidOperationException">融合できない（<see cref="FuseRejection"/> が null でない）。</exception>
    public void Fuse(string cardId, string resultId)
    {
        ArgumentNullException.ThrowIfNull(resultId);
        if (FuseRejection(cardId) is { } reason)
        {
            throw new InvalidOperationException($"融合できない: reason={reason}");
        }

        for (var i = 0; i < 2; i++)
        {
            RemoveOne(cardId);
        }
        _discardPile.Add(resultId);
        PruneCharges--;
        if (_hand.Count < _handSize)
        {
            Draw(_handSize - _hand.Count);
        }
    }

    /// <summary>
    /// 汚染ルーンを山札の中に混ぜる。位置は (シード, 混ぜた回数) から決める（同じ並びを再現できる）。
    /// </summary>
    public void InjectCorrupted(string cardId)
    {
        ArgumentNullException.ThrowIfNull(cardId);
        var random = new Random(unchecked(_seed + ((CorruptionStage + 1) * (int)0x85EBCA6B)));
        _drawPile.Insert(random.Next(_drawPile.Count + 1), cardId);
        CorruptionStage++;
    }

    /// <summary>世界再創生: 汚染ルーンを混ぜた回数を 0 に戻す（混ざった汚染ルーンは、デッキに残る）。</summary>
    public void ResetCorruption() => CorruptionStage = 0;

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

    public OracleDeckState Capture() => new([.. _drawPile], [.. _hand], [.. _discardPile], ShuffleCount, CorruptionStage, LastBuildingId);

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
        ShuffleCount = state.ShuffleCount;
        CorruptionStage = state.CorruptionStage;
        LastBuildingId = state.LastBuildingId;
    }

    // 同じカードが複数あるときは、捨札・山札・手札の順に 1 枚を消す
    private void RemoveOne(string cardId)
    {
        if (!_discardPile.Remove(cardId) && !_drawPile.Remove(cardId))
        {
            _hand.Remove(cardId);
        }
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
