namespace TheogonyX.Core;

/// <summary>
/// 時間の進め方（docs/domain/time-control.md）。100年跳躍・自動進行・一時停止・リセットを受け付け、世界とドグマの一覧を持つ。
/// 自動進行の時計は持たない。呼び出し側が間隔ごとに <see cref="AdvanceOneYear"/> を呼ぶ。
/// </summary>
public sealed class TimeControl
{
    // 仕様書の状態遷移表と1対1。表に無い組み合わせは「不可」。
    // 跳躍した結果、星が崩壊したら、表の遷移先に関わらず Destroyed になる。
    private static readonly Dictionary<(TimeControlState, TimeControlAction), TimeControlState> Transitions = new()
    {
        [(TimeControlState.Stopped, TimeControlAction.Jump)] = TimeControlState.Stopped,
        [(TimeControlState.Stopped, TimeControlAction.StartAutoPlay)] = TimeControlState.AutoPlaying,
        [(TimeControlState.Stopped, TimeControlAction.Reset)] = TimeControlState.Stopped,
        [(TimeControlState.AutoPlaying, TimeControlAction.Jump)] = TimeControlState.AutoPlaying,
        [(TimeControlState.AutoPlaying, TimeControlAction.Pause)] = TimeControlState.Stopped,
        [(TimeControlState.AutoPlaying, TimeControlAction.AdvanceOneYear)] = TimeControlState.AutoPlaying,
        [(TimeControlState.AutoPlaying, TimeControlAction.Reset)] = TimeControlState.Stopped,
        [(TimeControlState.Destroyed, TimeControlAction.Reset)] = TimeControlState.Stopped,
    };

    private readonly WorldRules _worldRules;
    private readonly int _jumpYears;
    private readonly List<Dogma> _dogmas = [];
    private readonly CivilizationSetup? _civilizationSetup;
    private readonly BeliefRules? _belief;
    private readonly JourneyRules? _journey;

    /// <param name="jumpYears">100年跳躍ボタンで進める年数。</param>
    /// <param name="civilization">地域の文明の計算に使う定義。null なら文明の計算をしない。</param>
    /// <param name="history">年表の定義。null なら年表を残さない。</param>
    /// <param name="belief">住人の信仰の定義。null なら信仰を使わず、ドグマはいつも全員に効く。住人の計算をしないときは無視する。</param>
    /// <param name="journey">予言者の旅の定義。null なら予言者は動かない。住人の計算をしないときは無視する。</param>
    public TimeControl(
        WorldRules worldRules, int jumpYears, CivilizationSetup? civilization = null, HistoryRules? history = null, BeliefRules? belief = null,
        JourneyRules? journey = null)
    {
        ArgumentNullException.ThrowIfNull(worldRules);
        ArgumentOutOfRangeException.ThrowIfNegative(jumpYears);

        _worldRules = worldRules;
        _jumpYears = jumpYears;
        _civilizationSetup = civilization;
        _belief = civilization?.Needs is null ? null : belief;
        _journey = civilization?.Needs is null ? null : journey;
        Prophet = InitialProphet();
        World = WorldState.CreateInitial(worldRules);
        Civilization = civilization is null ? null : Civilization.CreateInitial(civilization.Rules);
        Residents = civilization?.Needs is null ? null : Residents.CreateInitial();
        History = history is null ? null : new History([], history, new ChapterStart(World.Year, World.ChaosLevel));
    }

    public TimeControlState State { get; private set; } = TimeControlState.Stopped;

    public WorldState World { get; private set; }

    public IReadOnlyList<Dogma> Dogmas => _dogmas;

    /// <summary>地域の文明の値と建物。文明の計算をしないなら null。</summary>
    public Civilization? Civilization { get; private set; }

    /// <summary>住人と墓。住人の計算をしないなら null。</summary>
    public Residents? Residents { get; private set; }

    /// <summary>星の年表。年表を残さないなら null。</summary>
    public History? History { get; private set; }

    /// <summary>直前の跳躍・1 年の進みで、住人に起きた出来事（ログに出す）。</summary>
    public IReadOnlyList<ResidentEvent> ResidentEvents { get; private set; } = [];

    /// <summary>予言者の居場所・知っている土地・行き先。文明の計算をしないなら null。</summary>
    public ProphetState? Prophet { get; private set; }

    /// <summary>直前の跳躍・1 年の進みで、予言者が動いた記録（ログに出す）。</summary>
    public IReadOnlyList<ProphetMove> ProphetMoves { get; private set; } = [];

    /// <summary>予言者の行き先を決める（神の声の行き先）。予言者がいなければ何もしない。</summary>
    public void SetDestination(TravelDirection destination)
    {
        if (Prophet is not null)
        {
            Prophet = Prophet with { Destination = destination };
        }
    }

    /// <summary>直前の跳躍・1 年の進みで、信者がいなくなって忘れられた教え（ログに出す）。</summary>
    public IReadOnlyList<Dogma> ForgottenDogmas { get; private set; } = [];

    /// <summary>教えを信じている住人の数。信仰を使わないなら 0。</summary>
    public int BelieversOf(Dogma dogma) => _belief is null || Residents is null ? 0 : BeliefSimulator.BelieversOf(dogma, Residents);

    public bool CanDo(TimeControlAction action) => Transitions.ContainsKey((State, action));

    /// <summary>ドグマを一覧に加え、年表に残す。状態は変えない。</summary>
    public void AddDogma(Dogma dogma)
    {
        ArgumentNullException.ThrowIfNull(dogma);
        _dogmas.Add(dogma);
        History?.Add(new HistoryEntry(World.Year, HistoryEventType.DogmaFormed, dogma.TargetSymbolId, dogma.Type));
        if (_belief is not null && Residents is not null && _civilizationSetup is { } setup)
        {
            BeliefSimulator.Seed(Residents, dogma, Prophet?.Region ?? setup.Rules.Regions.RegionOf(setup.Rules.ProphetColumn), _belief);
        }
    }

    /// <summary>カオス度を <paramref name="amount"/> 上げる（上限を越えない）。崩壊の判定は次の年の計算で行う。</summary>
    public void AddChaos(float amount)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(amount);
        World.ChaosLevel = Math.Min(_worldRules.Max.ChaosLevel, World.ChaosLevel + amount);
    }

    /// <summary>
    /// 神の歪み: すべての住人の SAN 値を <paramref name="sanityLoss"/> 下げ（0 を下回らない）、カオス度を <paramref name="chaosGain"/> 上げる
    /// （上限を越えない）。状態は変えない。SAN 値が 0 になった住人の精神崩壊は、次の年の計算で起きる。
    /// </summary>
    public void ApplyOverload(double sanityLoss, float chaosGain)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(sanityLoss);
        ArgumentOutOfRangeException.ThrowIfNegative(chaosGain);

        AddChaos(chaosGain);
        if (Residents is { } residents && sanityLoss > 0)
        {
            var people = residents.People.Select(person => person with { Sanity = Math.Max(0, person.Sanity - sanityLoss) }).ToList();
            residents.Replace(people, [.. residents.Graves], residents.NextResidentId);
        }
    }

    public void Jump() => Skip(TimeControlAction.Jump, _jumpYears);

    public void AdvanceOneYear() => Skip(TimeControlAction.AdvanceOneYear, 1);

    public void StartAutoPlay() => State = NextState(TimeControlAction.StartAutoPlay);

    public void Pause() => State = NextState(TimeControlAction.Pause);

    /// <summary>
    /// 年 0・環境と文明の初期値に戻し、ドグマの一覧を空にして、停止中にする。
    /// 地上の建物と廃墟は、そのときの値と一緒に地下 1 層目に沈めて残す（docs/domain/strata.md）。
    /// 住人と墓は消し、次の住人の ID だけを残す（docs/domain/agent-needs.md）。年表は消さず、再創生した年を残す（docs/domain/history.md）。
    /// </summary>
    public void Reset()
    {
        var next = NextState(TimeControlAction.Reset);
        var initial = WorldState.CreateInitial(_worldRules);
        History?.CloseChapter(World.Year, World.ChaosLevel);
        History?.Add(new HistoryEntry(World.Year, HistoryEventType.WorldRecreated));
        History?.RestartChapter(initial.Year, initial.ChaosLevel);
        Civilization = _civilizationSetup is null ? null : Civilization?.Recreate(World, _dogmas, _civilizationSetup.Rules);
        Residents = Residents?.Recreate();
        ResidentEvents = [];
        ForgottenDogmas = [];
        ProphetMoves = [];
        Prophet = InitialProphet();
        World = initial;
        _dogmas.Clear();
        State = next;
    }

    /// <summary>セーブから読み込んだ世界・ドグマ・文明・住人に置き換える。崩壊していれば崩壊、そうでなければ止まった状態にする。</summary>
    /// <param name="civilization">文明の計算をしないなら無視する。</param>
    /// <param name="residents">住人の計算をしないなら無視する。</param>
    /// <param name="history">年表の行（古い順）。年表を残さないなら無視する。null なら年表を空にする。</param>
    /// <param name="chapter">年表の今の章の始まり。年表を残さないなら無視する。null なら今の年・カオス度から始める。</param>
    public void Restore(
        WorldState world, IEnumerable<Dogma> dogmas, Civilization civilization, Residents residents, IEnumerable<HistoryEntry>? history = null,
        ChapterStart? chapter = null, ProphetState? prophet = null)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(dogmas);
        ArgumentNullException.ThrowIfNull(civilization);
        ArgumentNullException.ThrowIfNull(residents);
        World = world;
        Civilization = _civilizationSetup is null ? null : civilization.Copy();
        Residents = _civilizationSetup?.Needs is null ? null : residents.Copy();
        History = History is null ? null : new History(history ?? [], History.Rules, chapter ?? new ChapterStart(world.Year, world.ChaosLevel));
        ResidentEvents = [];
        ForgottenDogmas = [];
        ProphetMoves = [];
        Prophet = _civilizationSetup is null ? null : prophet ?? InitialProphet();
        _dogmas.Clear();
        _dogmas.AddRange(dogmas);
        State = world.IsDestroyed ? TimeControlState.Destroyed : TimeControlState.Stopped;
    }

    private void Skip(TimeControlAction action, int years)
    {
        var next = NextState(action);
        var setup = _civilizationSetup;
        var civilization = Civilization;
        var residents = Residents;
        var events = new List<ResidentEvent>();
        var forgotten = new List<Dogma>();
        var moves = new List<ProphetMove>();
        var history = History;
        var belief = _belief;
        Func<Dogma, double>? share = belief is null || residents is null ? null : dogma => BeliefSimulator.Share(dogma, residents);
        // 1 年ごとに、文明・住人の計算、年表（住人の出来事・章）の順に進める。年表の行は年の順に並ぶ
        Action? afterYear = history is null && (setup is null || civilization is null)
            ? null
            : () =>
            {
                var yearEvents = new List<ResidentEvent>();
                if (setup is not null && civilization is not null)
                {
                    CivilizationSimulator.AdvanceYear(civilization, World, _dogmas, setup.Terrain, setup.Rules, setup.Seed, share, Prophet?.Region);
                    if (residents is not null && setup.Needs is not null)
                    {
                        yearEvents.AddRange(ResidentSimulator.AdvanceYear(
                            residents, civilization, World, _dogmas, setup.Needs, setup.Rules, setup.Terrain, setup.Seed, belief));
                        if (belief is not null)
                        {
                            BeliefSimulator.Spread(residents, World.Year, setup.Seed, belief);
                            ForgetDogmas(residents, forgotten);
                        }
                        if (_journey is not null && Prophet is not null)
                        {
                            var (state, move) = ProphetJourney.AdvanceYear(Prophet, residents, World.Year, setup.Seed, setup.Rules.Regions.Count, _journey);
                            Prophet = state;
                            if (move is not null)
                            {
                                moves.Add(move);
                            }
                        }
                    }
                }
                events.AddRange(yearEvents);
                history?.AddResidentEvents(yearEvents);
                history?.AdvanceChapters(World.Year, World.ChaosLevel);
            };
        var wasDestroyed = World.IsDestroyed;
        WorldSimulator.SimulateTimeSkip(World, _dogmas, years, _worldRules, afterYear, share);
        ResidentEvents = events;
        ForgottenDogmas = forgotten;
        ProphetMoves = moves;
        if (World.IsDestroyed && !wasDestroyed)
        {
            history?.Add(new HistoryEntry(World.Year, HistoryEventType.WorldDestroyed));
            history?.CloseChapter(World.Year, World.ChaosLevel);
        }
        State = World.IsDestroyed ? TimeControlState.Destroyed : next;
    }

    private ProphetState? InitialProphet() =>
        _civilizationSetup is null
            ? null
            : ProphetJourney.Initial(
                _civilizationSetup.Rules.Regions.RegionOf(_civilizationSetup.Rules.ProphetColumn), _civilizationSetup.Rules.Regions.Count);

    // 信者が 0 人になった教えを一覧から消し、年表に残す
    private void ForgetDogmas(Residents residents, List<Dogma> forgotten)
    {
        foreach (var dogma in _dogmas.Where(dogma => BeliefSimulator.BelieversOf(dogma, residents) == 0).ToList())
        {
            _dogmas.Remove(dogma);
            forgotten.Add(dogma);
            History?.Add(new HistoryEntry(World.Year, HistoryEventType.DogmaForgotten, dogma.TargetSymbolId, dogma.Type));
        }
    }

    private TimeControlState NextState(TimeControlAction action) =>
        Transitions.TryGetValue((State, action), out var next)
            ? next
            : throw new InvalidOperationException($"この状態ではできない操作: state={State} action={action}");
}

/// <summary>地域の文明（と住人）の計算に使う定義と乱数のシード。</summary>
/// <param name="Terrain">毎年のタイル種別（建てられる場所と環境の倍率）を求めるのに使う。</param>
/// <param name="Needs">住人の欲求と健康の定義。null なら住人の計算をしない。</param>
public sealed record CivilizationSetup(CivilizationRules Rules, TerrainRules Terrain, int Seed, NeedsRules? Needs = null);
