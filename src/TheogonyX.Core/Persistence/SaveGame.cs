namespace TheogonyX.Core.Persistence;

/// <summary>コアの今の状態とセーブデータを行き来する（docs/domain/save-data.md）。</summary>
public static class SaveGame
{
    public static SaveData Capture(TimeControl timeControl, ProphetDreams prophet, DreamSource dreams, AutoPlayClock clock)
    {
        ArgumentNullException.ThrowIfNull(clock);
        ArgumentNullException.ThrowIfNull(timeControl);
        ArgumentNullException.ThrowIfNull(prophet);
        ArgumentNullException.ThrowIfNull(dreams);
        var world = timeControl.World;
        return new SaveData(
            dreams.Seed,
            dreams.DreamsGenerated,
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
            prophet.State,
            [.. prophet.Dream],
            prophet.ActiveRequest?.Id,
            new Dictionary<string, int>(prophet.AnsweredYears),
            prophet.LastOracleYear,
            clock.Speed,
            clock.ElapsedSeconds,
            timeControl.Civilization?.Copy() ?? new Civilization([], [], [], [], 0),
            timeControl.Residents?.Copy() ?? Residents.CreateInitial());
    }

    /// <summary>
    /// セーブデータの状態に戻す。<paramref name="dreams"/> は、セーブデータと同じシードで作ったもの。
    /// 読み込んだ後は止まった状態（崩壊していれば崩壊）になる。
    /// </summary>
    public static void Apply(SaveData data, TimeControl timeControl, ProphetDreams prophet, DreamSource dreams, AutoPlayClock clock)
    {
        ArgumentNullException.ThrowIfNull(clock);
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(timeControl);
        ArgumentNullException.ThrowIfNull(prophet);
        ArgumentNullException.ThrowIfNull(dreams);
        if (dreams.Seed != data.Seed)
        {
            throw new ArgumentException($"シードが違う: dreams={dreams.Seed} data={data.Seed}", nameof(dreams));
        }

        timeControl.Restore(data.World, data.Dogmas, data.Civilization, data.Residents);
        prophet.Restore(data.DreamState, data.Dream, data.RequestId, data.AnsweredYears, data.LastOracleYear);
        dreams.Rewind(data.DreamsGenerated);
        clock.Restore(data.Speed, data.ElapsedSeconds);
    }
}
