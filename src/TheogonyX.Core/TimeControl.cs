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

    /// <param name="jumpYears">100年跳躍ボタンで進める年数。</param>
    /// <param name="civilization">地域の文明の計算に使う定義。null なら文明の計算をしない。</param>
    public TimeControl(WorldRules worldRules, int jumpYears, CivilizationSetup? civilization = null)
    {
        ArgumentNullException.ThrowIfNull(worldRules);
        ArgumentOutOfRangeException.ThrowIfNegative(jumpYears);

        _worldRules = worldRules;
        _jumpYears = jumpYears;
        _civilizationSetup = civilization;
        World = WorldState.CreateInitial(worldRules);
        Civilization = civilization is null ? null : Civilization.CreateInitial(civilization.Rules);
        Residents = civilization?.Needs is null ? null : Residents.CreateInitial();
    }

    public TimeControlState State { get; private set; } = TimeControlState.Stopped;

    public WorldState World { get; private set; }

    public IReadOnlyList<Dogma> Dogmas => _dogmas;

    /// <summary>地域の文明の値と建物。文明の計算をしないなら null。</summary>
    public Civilization? Civilization { get; private set; }

    /// <summary>住人と墓。住人の計算をしないなら null。</summary>
    public Residents? Residents { get; private set; }

    /// <summary>直前の跳躍・1 年の進みで、住人に起きた出来事（ログに出す）。</summary>
    public IReadOnlyList<ResidentEvent> ResidentEvents { get; private set; } = [];

    public bool CanDo(TimeControlAction action) => Transitions.ContainsKey((State, action));

    /// <summary>ドグマを一覧に加える。状態は変えない。</summary>
    public void AddDogma(Dogma dogma)
    {
        ArgumentNullException.ThrowIfNull(dogma);
        _dogmas.Add(dogma);
    }

    public void Jump() => Skip(TimeControlAction.Jump, _jumpYears);

    public void AdvanceOneYear() => Skip(TimeControlAction.AdvanceOneYear, 1);

    public void StartAutoPlay() => State = NextState(TimeControlAction.StartAutoPlay);

    public void Pause() => State = NextState(TimeControlAction.Pause);

    /// <summary>
    /// 年 0・環境と文明の初期値に戻し、ドグマの一覧を空にして、停止中にする。
    /// 地上の建物と廃墟は、そのときの値と一緒に地下 1 層目に沈めて残す（docs/domain/strata.md）。
    /// 住人と墓は消し、次の住人の ID だけを残す（docs/domain/agent-needs.md）。
    /// </summary>
    public void Reset()
    {
        var next = NextState(TimeControlAction.Reset);
        Civilization = _civilizationSetup is null ? null : Civilization?.Recreate(World, _dogmas, _civilizationSetup.Rules);
        Residents = Residents?.Recreate();
        ResidentEvents = [];
        World = WorldState.CreateInitial(_worldRules);
        _dogmas.Clear();
        State = next;
    }

    /// <summary>セーブから読み込んだ世界・ドグマ・文明・住人に置き換える。崩壊していれば崩壊、そうでなければ止まった状態にする。</summary>
    /// <param name="civilization">文明の計算をしないなら無視する。</param>
    /// <param name="residents">住人の計算をしないなら無視する。</param>
    public void Restore(WorldState world, IEnumerable<Dogma> dogmas, Civilization civilization, Residents residents)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(dogmas);
        ArgumentNullException.ThrowIfNull(civilization);
        ArgumentNullException.ThrowIfNull(residents);
        World = world;
        Civilization = _civilizationSetup is null ? null : civilization.Copy();
        Residents = _civilizationSetup?.Needs is null ? null : residents.Copy();
        ResidentEvents = [];
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
        Action? afterYear = setup is null || civilization is null
            ? null
            : () =>
            {
                CivilizationSimulator.AdvanceYear(civilization, World, _dogmas, setup.Terrain, setup.Rules, setup.Seed);
                if (residents is not null && setup.Needs is not null)
                {
                    events.AddRange(ResidentSimulator.AdvanceYear(
                        residents, civilization, World, _dogmas, setup.Needs, setup.Rules, _worldRules, setup.Seed));
                }
            };
        WorldSimulator.SimulateTimeSkip(World, _dogmas, years, _worldRules, afterYear);
        ResidentEvents = events;
        State = World.IsDestroyed ? TimeControlState.Destroyed : next;
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
