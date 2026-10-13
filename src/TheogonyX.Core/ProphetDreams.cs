namespace TheogonyX.Core;

/// <summary>
/// 予言者の夢と依頼（docs/domain/dream-tampering.md、prophet-request.md）。
/// 夢を見る・改ざん・神託・依頼を受け持ち、神託で生まれたドグマを <see cref="TimeControl"/> に加える。
/// 自動進行中に新しい夢か依頼が出たら自動進行を止め、神託を下したら再開する。
/// 夢の作り方（本番は <see cref="DreamGenerator"/> とシード付きの乱数）は呼び出し側が渡す。世界再創生では <see cref="TimeControl.Reset"/> の後に <see cref="Reset"/> を呼ぶ。
/// </summary>
public sealed class ProphetDreams
{
    private enum DreamTrigger
    {
        Dream,
        Tamper,
        Pronounce,
        Reset,
    }

    private enum RequestTrigger
    {
        Matched,
        Pronounce,
        Reset,
    }

    // 仕様書（dream-tampering.md）の状態遷移表と1対1。表に無い組み合わせは「不可」。
    // 崩壊しているあいだは、表に関わらず改ざんと神託を「不可」にする（CanDo）。
    private static readonly Dictionary<(ProphetDreamState, DreamTrigger), ProphetDreamState> DreamTransitions = new()
    {
        [(ProphetDreamState.None, DreamTrigger.Dream)] = ProphetDreamState.Untampered,
        [(ProphetDreamState.None, DreamTrigger.Reset)] = ProphetDreamState.Untampered,
        [(ProphetDreamState.Untampered, DreamTrigger.Dream)] = ProphetDreamState.Untampered,
        [(ProphetDreamState.Untampered, DreamTrigger.Tamper)] = ProphetDreamState.Tampered,
        [(ProphetDreamState.Untampered, DreamTrigger.Pronounce)] = ProphetDreamState.None,
        [(ProphetDreamState.Untampered, DreamTrigger.Reset)] = ProphetDreamState.Untampered,
        [(ProphetDreamState.Tampered, DreamTrigger.Dream)] = ProphetDreamState.Tampered,
        [(ProphetDreamState.Tampered, DreamTrigger.Pronounce)] = ProphetDreamState.None,
        [(ProphetDreamState.Tampered, DreamTrigger.Reset)] = ProphetDreamState.Untampered,
    };

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
    private readonly DreamRules _dreamRules;
    private readonly ProphetDreamRules _rules;
    private readonly Func<IReadOnlyList<DreamNode>> _createDream;
    private readonly List<DreamNode> _dream = [];
    private readonly Dictionary<string, int> _answeredYears = [];
    private int _lastOracleYear;

    // 夢か依頼のために自動進行を止めたか。神託を下したときに再開するのに使う。
    private bool _pausedForDream;

    /// <summary>作った時点で、起動時の夢を見る。</summary>
    /// <param name="createDream">新しい夢を返す。</param>
    /// <param name="deck">神託のデッキ。null なら、使う記号すべてが手札で、使ってもなくならない（デッキの前の動き）。</param>
    public ProphetDreams(
        TimeControl timeControl, DreamRules dreamRules, ProphetDreamRules rules, Func<IReadOnlyList<DreamNode>> createDream, OracleDeck? deck = null)
    {
        ArgumentNullException.ThrowIfNull(timeControl);
        ArgumentNullException.ThrowIfNull(dreamRules);
        ArgumentNullException.ThrowIfNull(rules);
        ArgumentNullException.ThrowIfNull(createDream);

        _timeControl = timeControl;
        _dreamRules = dreamRules;
        _rules = rules;
        _createDream = createDream;
        Deck = deck;
        Reset();
    }

    public ProphetDreamState State { get; private set; }

    /// <summary>神託のデッキ。持たなければ null。</summary>
    public OracleDeck? Deck { get; }

    /// <summary>手札の記号（重複がありうる）。デッキが無ければ、使う記号すべて。</summary>
    public IReadOnlyList<string> Hand => Deck?.Hand ?? [.. _dreamRules.Symbols.Select(symbol => symbol.Id)];

    /// <summary>今の夢。夢なしなら空。</summary>
    public IReadOnlyList<DreamNode> Dream => _dream;

    /// <summary>今の依頼。依頼なしなら null。</summary>
    public ProphetRequestRule? ActiveRequest { get; private set; }

    /// <summary>困りごとの ID ごとの、依頼が済んだ年。</summary>
    public IReadOnlyDictionary<string, int> AnsweredYears => _answeredYears;

    /// <summary>前の神託の年（起動・リセットからは、その年）。</summary>
    public int LastOracleYear => _lastOracleYear;

    public bool CanTamper => CanDo(DreamTrigger.Tamper);

    public bool CanPronounce => CanDo(DreamTrigger.Pronounce);

    /// <summary>時間が進んだ後に呼ぶ。依頼を出すか、夢を見るかを決める。</summary>
    public DreamUpdate OnTimeAdvanced()
    {
        var world = _timeControl.World;
        if (world.IsDestroyed)
        {
            return new DreamUpdate(false, null, false);
        }

        // 夢のあいだにプレイヤーが自動進行を再開したら、神託のときに再開し直す必要は無い
        if (_timeControl.State == TimeControlState.AutoPlaying)
        {
            _pausedForDream = false;
        }

        ProphetRequestRule? newRequest = null;
        if (ActiveRequest is null && FindRequest(world) is { } matched)
        {
            ActiveRequest = RequestTransitions[(false, RequestTrigger.Matched)] ? matched : null;
            newRequest = ActiveRequest;
        }

        var isDue = newRequest is not null || world.Year - _lastOracleYear >= _rules.YearsBetweenDreams;
        var hasNewDream = isDue && StartDream();

        var paused = false;
        if ((hasNewDream || newRequest is not null) && _timeControl.CanDo(TimeControlAction.Pause))
        {
            _timeControl.Pause();
            _pausedForDream = true;
            paused = true;
        }
        return new DreamUpdate(hasNewDream, newRequest, paused);
    }

    /// <summary>夢ノードの記号を、概念カードの記号に入れ替える。重みはそのまま。</summary>
    public void Tamper(int index, string symbolId)
    {
        ArgumentNullException.ThrowIfNull(symbolId);
        var next = NextDreamState(DreamTrigger.Tamper);
        ArgumentOutOfRangeException.ThrowIfNegative(index);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, _dream.Count);
        if (!_dreamRules.Symbols.Any(symbol => symbol.Id == symbolId))
        {
            throw new ArgumentException($"使う記号に無い: symbolId={symbolId}", nameof(symbolId));
        }
        if (Deck is not null && !Deck.Hand.Contains(symbolId))
        {
            throw new ArgumentException($"手札に無い: symbolId={symbolId}", nameof(symbolId));
        }

        _dream[index] = _dream[index] with { SymbolId = symbolId };
        Deck?.Use(symbolId);
        State = next;
    }

    /// <summary>今の夢を解釈してドグマを加え、夢を消す。依頼中なら依頼を済ませる。</summary>
    public OracleResult Pronounce()
    {
        var next = NextDreamState(DreamTrigger.Pronounce);
        var world = _timeControl.World;

        var dogma = DreamInterpreter.Interpret(_dream, _dreamRules);
        if (dogma is not null)
        {
            _timeControl.AddDogma(dogma);
        }

        var answered = ActiveRequest;
        if (answered is not null)
        {
            _answeredYears[answered.Id] = world.Year;
        }
        ActiveRequest = RequestTransitions[(answered is not null, RequestTrigger.Pronounce)] ? answered : null;

        _dream.Clear();
        _lastOracleYear = world.Year;
        State = next;
        Deck?.Settle();

        var resumed = false;
        if (_pausedForDream && _timeControl.CanDo(TimeControlAction.StartAutoPlay))
        {
            _timeControl.StartAutoPlay();
            resumed = true;
        }
        _pausedForDream = false;
        return new OracleResult(dogma, answered, resumed);
    }

    /// <summary>世界再創生: 夢・依頼・同じ困りごとを待つ年数を消し、新しい夢を見る。</summary>
    public void Reset()
    {
        ActiveRequest = RequestTransitions[(ActiveRequest is not null, RequestTrigger.Reset)] ? ActiveRequest : null;
        _answeredYears.Clear();
        _pausedForDream = false;
        _lastOracleYear = _timeControl.World.Year;
        Deck?.ClearUsed();
        State = DreamTransitions[(State, DreamTrigger.Reset)];
        _dream.Clear();
        _dream.AddRange(_createDream());
    }

    /// <summary>
    /// セーブから読み込んだ夢と依頼に置き換える。状態遷移ではなく、保存した時点に戻す。
    /// 値の検証は読み込む側（<c>SaveDataSerializer</c>）で済ませておく。
    /// </summary>
    public void Restore(
        ProphetDreamState state,
        IEnumerable<DreamNode> dream,
        string? requestId,
        IReadOnlyDictionary<string, int> answeredYears,
        int lastOracleYear)
    {
        ArgumentNullException.ThrowIfNull(dream);
        ArgumentNullException.ThrowIfNull(answeredYears);
        ProphetRequestRule? request = null;
        if (requestId is not null)
        {
            request = _rules.Requests.FirstOrDefault(rule => rule.Id == requestId)
                ?? throw new ArgumentException($"定義に無い困りごと: requestId={requestId}", nameof(requestId));
        }

        State = state;
        _dream.Clear();
        _dream.AddRange(dream);
        ActiveRequest = request;
        _answeredYears.Clear();
        foreach (var (id, year) in answeredYears)
        {
            _answeredYears[id] = year;
        }
        _lastOracleYear = lastOracleYear;
        _pausedForDream = false;
    }

    private bool StartDream()
    {
        var previous = State;
        State = DreamTransitions[(State, DreamTrigger.Dream)];
        if (previous != ProphetDreamState.None)
        {
            return false;
        }

        _dream.AddRange(_createDream());
        return true;
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
            if (request.Matches(world))
            {
                return request;
            }
        }
        return null;
    }

    private bool CanDo(DreamTrigger trigger) =>
        !_timeControl.World.IsDestroyed && DreamTransitions.ContainsKey((State, trigger));

    private ProphetDreamState NextDreamState(DreamTrigger trigger)
    {
        if (!CanDo(trigger))
        {
            throw new InvalidOperationException($"この状態ではできない: state={State} trigger={trigger} isDestroyed={_timeControl.World.IsDestroyed}");
        }
        return DreamTransitions[(State, trigger)];
    }
}
