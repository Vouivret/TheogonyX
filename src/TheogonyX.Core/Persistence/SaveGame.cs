namespace TheogonyX.Core.Persistence;

/// <summary>コアの今の状態とセーブデータを行き来する（docs/domain/save-data.md）。</summary>
public static class SaveGame
{
    public static SaveData Capture(TimeControl timeControl, ProphetDreams prophet, int seed, AutoPlayClock clock)
    {
        ArgumentNullException.ThrowIfNull(clock);
        ArgumentNullException.ThrowIfNull(timeControl);
        ArgumentNullException.ThrowIfNull(prophet);
        var deck = prophet.Deck ?? throw new InvalidOperationException("セーブには神託のデッキが要る");
        var faith = prophet.Faith ?? throw new InvalidOperationException("セーブには神威が要る");
        var world = timeControl.World;
        return new SaveData(
            seed,
            new WorldState
            {
                Year = world.Year,
                ForestAmount = world.ForestAmount,
                WaterAmount = world.WaterAmount,
                Temperature = world.Temperature,
                ChaosLevel = world.ChaosLevel,
                HarmonyLevel = world.HarmonyLevel,
                IsDestroyed = world.IsDestroyed,
            },
            [.. timeControl.Dogmas],
            prophet.ActiveRequest?.Id,
            new Dictionary<string, int>(prophet.AnsweredYears),
            clock.Speed,
            clock.ElapsedSeconds,
            timeControl.Civilization?.Copy() ?? new Civilization([], [], [], [], 0),
            timeControl.Residents?.Copy() ?? Residents.CreateInitial(),
            [.. timeControl.History?.Entries ?? []],
            timeControl.History?.Chapter ?? new ChapterStart(world.Year, world.ChaosLevel),
            deck.Capture(),
            faith.Capture());
    }

    /// <summary>
    /// セーブデータの状態に戻す。読み込んだ後は止まった状態（崩壊していれば崩壊）になる。
    /// デッキの手札・山札・捨札と神威も、セーブのとおりに戻す。
    /// </summary>
    public static void Apply(SaveData data, TimeControl timeControl, ProphetDreams prophet, AutoPlayClock clock)
    {
        ArgumentNullException.ThrowIfNull(clock);
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(timeControl);
        ArgumentNullException.ThrowIfNull(prophet);
        var deck = prophet.Deck ?? throw new InvalidOperationException("セーブには神託のデッキが要る");

        timeControl.Restore(data.World, data.Dogmas, data.Civilization, data.Residents, data.History, data.Chapter);
        // ルーンの組モードでは夢を見ない。最後の神託の年は、依頼の待ちに使わないので今の年にしておく
        prophet.Restore(ProphetDreamState.None, [], data.RequestId, data.AnsweredYears, data.World.Year);
        deck.Restore(data.Deck);
        prophet.RestoreFaith(data.Faith.Value, data.Faith.RecentYears);
        clock.Restore(data.Speed, data.ElapsedSeconds);
    }
}
