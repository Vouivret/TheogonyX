using TheogonyX.Core;

namespace TheogonyX.Core.Tests;

// 神威・神の歪み・依頼・世界再創生（docs/domain/oracle-pair.md）。神託を下す操作は、手札の先頭のカードの記号で語りかける
// （<see cref="SpeakSupport.SpeakFirst"/>）。ルーン 2 枚の組と五行による旧い神託は、自由文の神託に替えて削除した。
public class OraclePairTests
{
    private const int Seed = 42;
    private const float Tolerance = 0.001f;

    private static OracleRules WithFaith(float initial) =>
        TestRules.Oracle with { Faith = TestRules.Oracle.Faith with { Initial = initial } };

    private static (ProphetDreams Prophet, TimeControl Time, OracleDeck Deck) Create(DeckRules? deck = null, OracleRules? oracle = null, int seed = Seed)
    {
        var time = new TimeControl(
            TestRules.World, TestRules.JumpYears, new CivilizationSetup(TestRules.Civilization, TestRules.Terrain, seed, TestRules.Needs), TestRules.History);
        var oracleDeck = new OracleDeck(seed, deck ?? TestRules.Deck);
        var prophet = new ProphetDreams(time, TestRules.ProphetDreams, oracleDeck, oracle ?? TestRules.Oracle);
        return (prophet, time, oracleDeck);
    }

    [Fact]
    [Trait("TS", "TS-oracle-pair-004")]
    public void Speak_依頼中に下す_依頼が済む()
    {
        var (prophet, time, _) = Create();
        time.World.WaterAmount = 10f;
        prophet.OnTimeAdvanced();
        Assert.NotNull(prophet.ActiveRequest);

        var result = prophet.SpeakFirst();

        Assert.NotNull(result.AnsweredRequest);
        Assert.Null(prophet.ActiveRequest);
    }

    [Fact]
    [Trait("TS", "TS-oracle-pair-005")]
    public void Speak_同じシードと同じ操作を2回_世界とドグマとデッキが一致する()
    {
        (IReadOnlyList<Dogma> Dogmas, List<string> Hand, List<string> Pile, double Faith, float Chaos) Play()
        {
            var (prophet, time, deck) = Create();
            for (var i = 0; i < 4; i++)
            {
                if (prophet.Dialect is null || prophet.SpeakRejection() is null)
                {
                    prophet.SpeakFirst();
                }
                time.World.Year += 3;
                prophet.OnTimeAdvanced();
            }
            return ([.. time.Dogmas], [.. deck.Hand], [.. deck.DrawPile], prophet.Faith!.Value, time.World.ChaosLevel);
        }

        var first = Play();
        var second = Play();

        Assert.Equal(first.Dogmas, second.Dogmas);
        Assert.Equal(first.Hand, second.Hand);
        Assert.Equal(first.Pile, second.Pile);
        Assert.Equal(first.Faith, second.Faith);
        Assert.Equal(first.Chaos, second.Chaos);
    }

    [Fact]
    [Trait("TS", "TS-oracle-pair-006")]
    public void Speak_神威が上限_1回で1減る()
    {
        var (prophet, _, _) = Create();

        prophet.SpeakFirst();

        Assert.Equal(2.0, prophet.Faith!.Value, 3);
    }

    [Fact]
    [Trait("TS", "TS-oracle-pair-006")]
    public void SpeakRejection_神威がコスト未満_下せない()
    {
        var (prophet, time, _) = Create(oracle: WithFaith(0.5f));

        Assert.Equal("not_enough_faith", prophet.SpeakRejection());
        Assert.Empty(time.Dogmas);
    }

    [Fact]
    [Trait("TS", "TS-oracle-pair-006")]
    public void OnTimeAdvanced_100年進める_神威は上限で止まる()
    {
        var (prophet, time, _) = Create(oracle: WithFaith(0f));

        time.World.Year = 100;
        prophet.OnTimeAdvanced();

        Assert.Equal(3.0, prophet.Faith!.Value, 3);
    }

    private static Resident Person(double sanity) => new(
        0, 0, 0, 0.1, 0.1, 0.1, 0.1, 0.1, 100, sanity, null, 0, ResidentTask.Gather, new ResidentName("a", "b", "c", "d"), null);

    // 住人が 1 人（SAN 値 sanity）いる星
    private static (ProphetDreams Prophet, TimeControl Time) CreateWithResident(double sanity)
    {
        var (prophet, time, _) = Create();
        time.Restore(time.World, [], time.Civilization!, new Residents([Person(sanity)], [], 1));
        return (prophet, time);
    }

    [Fact]
    [Trait("TS", "TS-oracle-pair-007")]
    public void Speak_10年に3回_3回目から歪みが起きる()
    {
        var (prophet, time) = CreateWithResident(100);

        var r1 = prophet.SpeakFirst();
        var r2 = prophet.SpeakFirst();
        Assert.Equal(0, r1.OverloadCount + r2.OverloadCount);
        Assert.Equal(100.0, time.Residents!.People[0].Sanity);
        var chaosBefore = time.World.ChaosLevel;

        var r3 = prophet.SpeakFirst();

        Assert.Equal(1, r3.OverloadCount);
        Assert.Equal(80.0, time.Residents.People[0].Sanity);
        Assert.Equal(chaosBefore + 5f, time.World.ChaosLevel, Tolerance);
    }

    [Fact]
    [Trait("TS", "TS-oracle-pair-007")]
    public void Speak_2回下して20年後に3回目_歪みは起きない()
    {
        var (prophet, time) = CreateWithResident(100);
        prophet.SpeakFirst();
        prophet.SpeakFirst();

        time.World.Year = 20;
        prophet.OnTimeAdvanced();
        var result = prophet.SpeakFirst();

        Assert.Equal(0, result.OverloadCount);
        Assert.Equal(100.0, time.Residents!.People[0].Sanity);
    }

    [Fact]
    [Trait("TS", "TS-oracle-pair-007")]
    public void ApplyOverload_SAN値が足りない_0を下回らない()
    {
        var (_, time) = CreateWithResident(10);

        time.ApplyOverload(20, 5f);

        Assert.Equal(0.0, time.Residents!.People[0].Sanity);
    }

    [Fact]
    [Trait("TS", "TS-oracle-pair-008")]
    public void Reset_世界再創生_神威は最初の量に戻り数えた神託は消える()
    {
        var (prophet, time) = CreateWithResident(100);
        prophet.SpeakFirst();
        prophet.SpeakFirst();

        time.Reset();
        prophet.Reset();

        Assert.Equal(3.0, prophet.Faith!.Value, 3);
        Assert.Empty(prophet.Faith.RecentYears);
    }

    [Fact]
    [Trait("TS", "TS-oracle-pair-009")]
    public void SubmitSpeak_命令で下す_直接下したのと同じ結果で番号が続く()
    {
        var (prophet, time, deck) = Create();
        var queue = new OracleCommandQueue(prophet, time);
        prophet.SpeakFirst();
        var before = prophet.Faith.Value;
        var noun = prophet.Dialect!.Rules.Nouns.First(candidate => candidate.SymbolId == prophet.SymbolOf(prophet.Hand[0]));
        var hearing = prophet.Hear(new HeardAnswer(noun.Id, "VERB_PROTECT", HeardDestination.None, null, [])).Hearing!;
        time.World.IsDestroyed = true;
        var rejected = queue.SubmitSpeak(hearing, "森を守れ");
        time.World.IsDestroyed = false;

        var outcome = queue.SubmitSpeak(hearing, "森を守れ");

        Assert.Equal("star_destroyed", rejected.RejectedReason);
        Assert.Null(outcome.RejectedReason);
        Assert.Equal(2, time.Dogmas.Count);
        Assert.Equal(before - 1, prophet.Faith.Value, 3);
        Assert.Equal(1, rejected.Sequence);
        Assert.Equal(2, outcome.Sequence);
        Assert.NotEmpty(deck.DiscardPile);
    }
}
