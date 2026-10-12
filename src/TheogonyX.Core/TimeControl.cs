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

    /// <param name="jumpYears">100年跳躍ボタンで進める年数。</param>
    public TimeControl(WorldRules worldRules, int jumpYears)
    {
        ArgumentNullException.ThrowIfNull(worldRules);
        ArgumentOutOfRangeException.ThrowIfNegative(jumpYears);

        _worldRules = worldRules;
        _jumpYears = jumpYears;
        World = WorldState.CreateInitial(worldRules);
    }

    public TimeControlState State { get; private set; } = TimeControlState.Stopped;

    public WorldState World { get; private set; }

    public IReadOnlyList<Dogma> Dogmas => _dogmas;

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

    /// <summary>年 0・環境の初期値に戻し、ドグマの一覧を空にして、停止中にする。</summary>
    public void Reset()
    {
        var next = NextState(TimeControlAction.Reset);
        World = WorldState.CreateInitial(_worldRules);
        _dogmas.Clear();
        State = next;
    }

    /// <summary>セーブから読み込んだ世界とドグマに置き換える。崩壊していれば崩壊、そうでなければ止まった状態にする。</summary>
    public void Restore(WorldState world, IEnumerable<Dogma> dogmas)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(dogmas);
        World = world;
        _dogmas.Clear();
        _dogmas.AddRange(dogmas);
        State = world.IsDestroyed ? TimeControlState.Destroyed : TimeControlState.Stopped;
    }

    private void Skip(TimeControlAction action, int years)
    {
        var next = NextState(action);
        WorldSimulator.SimulateTimeSkip(World, _dogmas, years, _worldRules);
        State = World.IsDestroyed ? TimeControlState.Destroyed : next;
    }

    private TimeControlState NextState(TimeControlAction action) =>
        Transitions.TryGetValue((State, action), out var next)
            ? next
            : throw new InvalidOperationException($"この状態ではできない操作: state={State} action={action}");
}
