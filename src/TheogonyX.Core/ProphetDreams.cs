namespace TheogonyX.Core;

/// <summary>
/// 予言者の依頼と、神託（docs/domain/oracle-pair.md、prophet-request.md）。
/// 手札のルーンを 2 枚選んで神託を下し、生まれたドグマを <see cref="TimeControl"/> に加える。神託は神威を使い、
/// 短い年数に重ねすぎると世界に歪みが起きる。星の環境が悪くなると予言者が依頼を出し、自動進行を止める。
/// 世界再創生では <see cref="TimeControl.Reset"/> の後に <see cref="Reset"/> を呼ぶ。
/// </summary>
public sealed class ProphetDreams
{
    private enum RequestTrigger
    {
        Matched,
        Pronounce,
        Reset,
    }

    // 仕様書（prophet-request.md）の状態遷移表と1対1。キーは（依頼中か, きっかけ）、値は遷移した後に依頼中か。
    private static readonly Dictionary<(bool IsRequesting, RequestTrigger), bool> RequestTransitions = new()
    {
        [(false, RequestTrigger.Matched)] = true,
        [(false, RequestTrigger.Pronounce)] = false,
        [(false, RequestTrigger.Reset)] = false,
        [(true, RequestTrigger.Matched)] = true,
        [(true, RequestTrigger.Pronounce)] = false,
        [(true, RequestTrigger.Reset)] = false,
    };

    private readonly TimeControl _timeControl;
    private readonly ProphetDreamRules _rules;
    private readonly OracleRules _oracleRules;
    private readonly Dictionary<string, int> _answeredYears = [];
    private int _faithYear;

    // 依頼のために自動進行を止めたか。神託を下したときに再開するのに使う。
    private bool _pausedForRequest;

    /// <param name="deck">神託のデッキ（手札・山札・捨札）。</param>
    /// <param name="oracleRules">神託の作り方（ルーンの組）と神威・歪みの定義。</param>
    public ProphetDreams(TimeControl timeControl, ProphetDreamRules rules, OracleDeck deck, OracleRules oracleRules)
    {
        ArgumentNullException.ThrowIfNull(timeControl);
        ArgumentNullException.ThrowIfNull(rules);
        ArgumentNullException.ThrowIfNull(deck);
        ArgumentNullException.ThrowIfNull(oracleRules);

        _timeControl = timeControl;
        _rules = rules;
        Deck = deck;
        _oracleRules = oracleRules;
        Faith = new OracleFaith(oracleRules.Faith, oracleRules.Overload);
        _faithYear = timeControl.World.Year;
    }

    /// <summary>神託のデッキ。</summary>
    public OracleDeck Deck { get; }

    /// <summary>神威と神の歪み。</summary>
    public OracleFaith Faith { get; }

    /// <summary>手札の記号（重複がありうる）。</summary>
    public IReadOnlyList<string> Hand => Deck.Hand;

    /// <summary>今の依頼。依頼なしなら null。</summary>
    public ProphetRequestRule? ActiveRequest { get; private set; }

    /// <summary>困りごとの ID ごとの、依頼が済んだ年。</summary>
    public IReadOnlyDictionary<string, int> AnsweredYears => _answeredYears;

    /// <summary>精査（ルーンの削除）の残りの回数。</summary>
    public int PruneCharges => Deck.PruneCharges;

    /// <summary>ルーンを 1 枚削除できない理由（<see cref="OracleDeck.PruneRejection"/>）。できるなら null。</summary>
    public string? PruneRejection(string cardId) => Deck.PruneRejection(cardId);

    /// <summary>ルーンを 1 枚削除する（精査）。</summary>
    /// <exception cref="InvalidOperationException">削除できない。</exception>
    public void Prune(string cardId) => Deck.Prune(cardId);

    /// <summary>基本ルーン <paramref name="cardId"/> を 2 枚融合してできる上位のルーン。融合できないルーンなら null。</summary>
    public GreaterRune? FusionOf(string cardId) => _oracleRules.FusionOf(cardId);

    /// <summary>
    /// 基本ルーン <paramref name="cardId"/> を 2 枚融合できない理由。できるなら null。
    /// 理由は <c>not_fusable</c>（基本ルーンでない・上位のルーンの定義が無い）と、<see cref="OracleDeck.FuseRejection"/> の理由。
    /// </summary>
    public string? FuseRejection(string cardId)
    {
        ArgumentNullException.ThrowIfNull(cardId);
        return _oracleRules.FusionOf(cardId) is null ? "not_fusable" : Deck.FuseRejection(cardId);
    }

    /// <summary>基本ルーン <paramref name="cardId"/> を 2 枚融合して、上位のルーンを捨札に加える。加えた上位のルーンを返す。</summary>
    /// <exception cref="InvalidOperationException">融合できない。</exception>
    public GreaterRune Fuse(string cardId)
    {
        if (FuseRejection(cardId) is { } reason)
        {
            throw new InvalidOperationException($"融合できない: reason={reason}");
        }
        var greater = _oracleRules.FusionOf(cardId)!;
        Deck.Fuse(cardId, greater.Id);
        return greater;
    }

    /// <summary>神託を下せるか。崩壊しておらず、手札が 2 枚以上あり、神威がコスト以上ある。</summary>
    public bool CanPronounce => !_timeControl.World.IsDestroyed && Deck.Hand.Count >= 2 && Faith.CanAfford;

    /// <summary>時間が進んだ後に呼ぶ。神威を溜め、依頼を出すかを決める。</summary>
    public ProphetUpdate OnTimeAdvanced()
    {
        var world = _timeControl.World;
        if (world.Year > _faithYear)
        {
            Faith.Advance(world.Year - _faithYear);
        }
        _faithYear = world.Year;
        var corrupted = InjectCorruption(world.ChaosLevel);
        var gained = Deck.GainFromBuildings(_timeControl.Civilization?.Buildings ?? []);
        if (world.IsDestroyed)
        {
            return new ProphetUpdate(null, false, corrupted, gained);
        }

        // 依頼のあいだにプレイヤーが自動進行を再開したら、神託のときに再開し直す必要は無い
        if (_timeControl.State == TimeControlState.AutoPlaying)
        {
            _pausedForRequest = false;
        }

        ProphetRequestRule? newRequest = null;
        if (ActiveRequest is null && FindRequest(world) is { } matched)
        {
            ActiveRequest = RequestTransitions[(false, RequestTrigger.Matched)] ? matched : null;
            newRequest = ActiveRequest;
        }

        var paused = false;
        if (newRequest is not null && _timeControl.CanDo(TimeControlAction.Pause))
        {
            _timeControl.Pause();
            _pausedForRequest = true;
            paused = true;
        }
        return new ProphetUpdate(newRequest, paused, corrupted, gained);
    }

    /// <summary>
    /// 手札の 2 枚（番号は手札の位置。1 枚目が対象）で神託を下したときの、五行の関係と種別の予告（画面の案内に使う）。
    /// 番号が正しくなければ null。
    /// </summary>
    public WuxingJudgement? PreviewPair(int first, int second)
    {
        if (first < 0 || second < 0 || first >= Deck.Hand.Count || second >= Deck.Hand.Count || first == second)
        {
            return null;
        }
        return WuxingInterpreter.Judge(Deck.Hand[first], Deck.Hand[second], _oracleRules);
    }

    /// <summary>
    /// 手札の 2 枚（番号は手札の位置）で神託を下せない理由。下せるなら null。
    /// 理由は <c>star_destroyed</c>・<c>not_enough_cards</c>・<c>card_not_in_hand</c>・<c>same_card</c>・<c>not_enough_faith</c>。
    /// </summary>
    public string? PairRejection(int first, int second)
    {
        if (_timeControl.World.IsDestroyed)
        {
            return "star_destroyed";
        }
        if (Deck.Hand.Count < 2)
        {
            return "not_enough_cards";
        }
        if (first < 0 || second < 0 || first >= Deck.Hand.Count || second >= Deck.Hand.Count)
        {
            return "card_not_in_hand";
        }
        if (first == second)
        {
            return "same_card";
        }
        return Faith.CanAfford ? null : "not_enough_faith";
    }

    /// <summary>
    /// 手札の 2 枚（1 枚目がドグマの対象）を選んで神託を下す。ドグマを加え、神威を使い、短い年数に重ねすぎたら神の歪みを起こし、
    /// 使った 2 枚を捨札へ移して 2 枚を補う（docs/domain/oracle-pair.md）。依頼は済み、止めていた自動進行は再開する。
    /// </summary>
    /// <exception cref="InvalidOperationException">下せない（<see cref="PairRejection"/> が null でない）。</exception>
    public OracleResult PronouncePair(int first, int second)
    {
        if (PairRejection(first, second) is { } reason)
        {
            throw new InvalidOperationException($"神託を下せない: reason={reason}");
        }

        var world = _timeControl.World;
        var corruptedCount = new[] { Deck.Hand[first], Deck.Hand[second] }.Count(_oracleRules.IsCorrupted);
        var dogma = WuxingInterpreter.Interpret(Deck.Hand[first], Deck.Hand[second], _oracleRules);
        var pairing = new OraclePairing(
            _oracleRules.SymbolOf(Deck.Hand[second]), WuxingInterpreter.Judge(Deck.Hand[first], Deck.Hand[second], _oracleRules).Relation);
        if (dogma is not null)
        {
            _timeControl.AddDogma(dogma);
        }
        Deck.SpendPair(first, second);

        if (corruptedCount > 0)
        {
            // 汚染ルーンは、使った枚数だけ、その年にカオス度を上げる
            _timeControl.AddChaos(_oracleRules.CorruptionOrNone.ChaosPerRune * corruptedCount);
        }

        var overload = Faith.Pronounce(world.Year);
        if (overload > 0)
        {
            _timeControl.ApplyOverload(_oracleRules.Overload.SanityLoss * overload, _oracleRules.Overload.ChaosGain * overload);
        }

        var answered = ActiveRequest;
        if (answered is not null)
        {
            _answeredYears[answered.Id] = world.Year;
        }
        ActiveRequest = RequestTransitions[(answered is not null, RequestTrigger.Pronounce)] ? answered : null;

        var resumed = false;
        if (_pausedForRequest && _timeControl.CanDo(TimeControlAction.StartAutoPlay))
        {
            _timeControl.StartAutoPlay();
            resumed = true;
        }
        _pausedForRequest = false;
        return new OracleResult(dogma, answered, resumed, overload, corruptedCount, pairing);
    }

    /// <summary>世界再創生: 依頼・同じ困りごとを待つ年数を消し、神威を最初の量に戻し、精査の持ち分を与える。デッキは保つ。</summary>
    public void Reset()
    {
        Faith.Reset();
        Deck.ResetCorruption();
        Deck.GrantPruneCharges();
        Deck.ResetBuildingCount();
        _faithYear = _timeControl.World.Year;
        ActiveRequest = RequestTransitions[(ActiveRequest is not null, RequestTrigger.Reset)] ? ActiveRequest : null;
        _answeredYears.Clear();
        _pausedForRequest = false;
    }

    /// <summary>セーブから読み込んだ神威と直近の神託の年に置き換える。神威を最後に見た年は、今の年に合わせる（一気に溜まらないように）。</summary>
    public void RestoreFaith(double value, IEnumerable<int> recentYears)
    {
        ArgumentNullException.ThrowIfNull(recentYears);
        Faith.Restore(value, recentYears);
        _faithYear = _timeControl.World.Year;
    }

    /// <summary>
    /// セーブから読み込んだ依頼に置き換える。状態遷移ではなく、保存した時点に戻す。
    /// 値の検証は読み込む側（<c>SaveDataSerializer</c>）で済ませておく。
    /// </summary>
    public void Restore(string? requestId, IReadOnlyDictionary<string, int> answeredYears)
    {
        ArgumentNullException.ThrowIfNull(answeredYears);
        ProphetRequestRule? request = null;
        if (requestId is not null)
        {
            request = _rules.Requests.FirstOrDefault(rule => rule.Id == requestId)
                ?? throw new ArgumentException($"定義に無い困りごと: requestId={requestId}", nameof(requestId));
        }

        ActiveRequest = request;
        _answeredYears.Clear();
        foreach (var (id, year) in answeredYears)
        {
            _answeredYears[id] = year;
        }
        _pausedForRequest = false;
    }

    // カオス度がまだ越えていない閾値を越えたら、越えた数だけ、次の汚染ルーンを山札に混ぜる
    private int InjectCorruption(float chaosLevel)
    {
        var corruption = _oracleRules.CorruptionOrNone;
        var added = 0;
        while (Deck.CorruptionStage < corruption.Thresholds.Count && chaosLevel >= corruption.Thresholds[Deck.CorruptionStage])
        {
            var rune = corruption.Runes[Deck.CorruptionStage % corruption.Runes.Count];
            Deck.InjectCorrupted(rune.Id);
            added++;
        }
        return added;
    }

    private ProphetRequestRule? FindRequest(WorldState world)
    {
        foreach (var request in _rules.Requests)
        {
            if (_answeredYears.TryGetValue(request.Id, out var answeredYear)
                && world.Year - answeredYear < _rules.RequestCooldownYears)
            {
                continue;
            }
            if (request.Matches(world, _timeControl.Civilization))
            {
                return request;
            }
        }
        return null;
    }
}
