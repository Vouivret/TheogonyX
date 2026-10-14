using TheogonyX.Core;

namespace TheogonyX.Core.Tests;

public class OraclePairTests
{
    private const int Seed = 42;
    private const float Tolerance = 0.001f;

    private static DeckRules Deck(int handSize, params (string SymbolId, int Count)[] runes) =>
        new([.. runes.Select(rune => new DeckRuneCount(rune.SymbolId, rune.Count))], handSize);

    private static OracleRules WithFaith(float initial) =>
        TestRules.Oracle with { Faith = TestRules.Oracle.Faith with { Initial = initial } };

    private static (ProphetDreams Prophet, TimeControl Time, OracleDeck Deck) Create(DeckRules? deck = null, OracleRules? oracle = null, int seed = Seed)
    {
        var time = new TimeControl(
            TestRules.World, TestRules.JumpYears, new CivilizationSetup(TestRules.Civilization, TestRules.Terrain, seed, TestRules.Needs), TestRules.History);
        var oracleDeck = new OracleDeck(seed, deck ?? TestRules.Deck);
        var prophet = new ProphetDreams(time, TestRules.Dream, TestRules.ProphetDreams, oracleDeck, oracle ?? TestRules.Oracle);
        return (prophet, time, oracleDeck);
    }

    // 手札にある記号の番号（重なっていれば、後ろから）
    private static int IndexOf(OracleDeck deck, string symbolId, int skip = -1)
    {
        for (var i = deck.Hand.Count - 1; i >= 0; i--)
        {
            if (deck.Hand[i] == symbolId && i != skip)
            {
                return i;
            }
        }
        throw new InvalidOperationException($"手札に無い: {symbolId}");
    }

    // 2 枚だけの山札（記号は a, b）を、手札に引いた状態で作る
    private static (ProphetDreams Prophet, TimeControl Time, OracleDeck Deck) CreatePair(string a, string b, OracleRules? oracle = null) =>
        Create(Deck(2, (a, 1), (b, 1)), oracle);

    [Theory]
    [Trait("TS", "TS-oracle-pair-001")]
    [InlineData("SYM_TREE", "SYM_BLOOD", "SYM_TREE", DogmaType.Taboo, 1.0f)]
    [InlineData("SYM_BLOOD", "SYM_TREE", "SYM_BLOOD", DogmaType.Taboo, 1.0f)]
    [InlineData("SYM_TREE", "SYM_WATER", "SYM_TREE", DogmaType.Worship, 0.5f)]
    public void PronouncePair_2枚を選ぶ_1枚目が対象のドグマが加わる(string first, string second, string target, DogmaType type, float intensity)
    {
        // 旧い方式（極性の和）。段階 2c で旧い方式とともに消す（docs/domain/oracle-wuxing.md）
        var (prophet, time, deck) = CreatePair(first, second, TestRules.Oracle with { TypeRule = OracleTypeRule.Polarity });

        var result = prophet.PronouncePair(IndexOf(deck, first), IndexOf(deck, second));

        var dogma = Assert.Single(time.Dogmas);
        Assert.Equal(target, dogma.TargetSymbolId);
        Assert.Equal(type, dogma.Type);
        Assert.Equal(intensity, dogma.Intensity, Tolerance);
        Assert.Equal(dogma, result.Dogma);
    }

    [Fact]
    [Trait("TS", "TS-oracle-pair-002")]
    public void PronouncePair_2枚を選ぶ_使った2枚が捨札に移り2枚が補われる()
    {
        var (prophet, _, deck) = Create();
        var first = deck.Hand[0];
        var second = deck.Hand[1];
        var nextTwo = deck.DrawPile.Take(2).ToList();

        prophet.PronouncePair(0, 1);

        Assert.Equal(4, deck.Hand.Count);
        Assert.Equal(9, deck.DrawPile.Count);
        Assert.Equal([first, second], deck.DiscardPile);
        Assert.Equal(nextTwo, deck.Hand.Skip(2).ToList());
    }

    [Fact]
    [Trait("TS", "TS-oracle-pair-003")]
    public void PairRejection_手札が1枚_下せない()
    {
        var (prophet, time, deck) = Create(Deck(1, ("SYM_TREE", 1)));

        Assert.Equal("not_enough_cards", prophet.PairRejection(0, 1));
        Assert.Throws<InvalidOperationException>(() => prophet.PronouncePair(0, 1));
        Assert.Single(deck.Hand);
        Assert.Empty(time.Dogmas);
    }

    [Fact]
    [Trait("TS", "TS-oracle-pair-003")]
    public void PairRejection_星が崩壊している_下せない()
    {
        var (prophet, time, _) = Create();
        time.World.IsDestroyed = true;

        Assert.Equal("star_destroyed", prophet.PairRejection(0, 1));
    }

    [Theory]
    [Trait("TS", "TS-oracle-pair-003")]
    [InlineData(0, 9, "card_not_in_hand")]
    [InlineData(-1, 1, "card_not_in_hand")]
    [InlineData(2, 2, "same_card")]
    public void PairRejection_手札の番号が正しくない_下せない(int first, int second, string reason)
    {
        var (prophet, _, _) = Create();

        Assert.Equal(reason, prophet.PairRejection(first, second));
    }

    [Fact]
    [Trait("TS", "TS-oracle-pair-004")]
    public void PronouncePair_依頼中に下す_依頼が済む()
    {
        var (prophet, time, _) = Create();
        time.World.WaterAmount = 10f;
        prophet.OnTimeAdvanced();
        Assert.NotNull(prophet.ActiveRequest);

        var result = prophet.PronouncePair(0, 1);

        Assert.NotNull(result.AnsweredRequest);
        Assert.Null(prophet.ActiveRequest);
    }

    [Fact]
    [Trait("TS", "TS-oracle-pair-005")]
    public void PronouncePair_同じシードと同じ操作を2回_世界とドグマとデッキが一致する()
    {
        (IReadOnlyList<Dogma> Dogmas, List<string> Hand, List<string> Pile, double Faith, float Chaos) Play()
        {
            var (prophet, time, deck) = Create();
            for (var i = 0; i < 4; i++)
            {
                if (prophet.PairRejection(0, 1) is null)
                {
                    prophet.PronouncePair(0, 1);
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
    public void PronouncePair_神威が上限_1回で1減る()
    {
        var (prophet, _, _) = Create();

        prophet.PronouncePair(0, 1);

        Assert.Equal(2.0, prophet.Faith!.Value, 3);
    }

    [Fact]
    [Trait("TS", "TS-oracle-pair-006")]
    public void PairRejection_神威がコスト未満_下せない()
    {
        var (prophet, time, _) = Create(oracle: WithFaith(0.5f));

        Assert.Equal("not_enough_faith", prophet.PairRejection(0, 1));
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
    public void PronouncePair_10年に3回_3回目から歪みが起きる()
    {
        var (prophet, time) = CreateWithResident(100);

        var r1 = prophet.PronouncePair(0, 1);
        var r2 = prophet.PronouncePair(0, 1);
        Assert.Equal(0, r1.OverloadCount + r2.OverloadCount);
        Assert.Equal(100.0, time.Residents!.People[0].Sanity);
        var chaosBefore = time.World.ChaosLevel;

        var r3 = prophet.PronouncePair(0, 1);

        Assert.Equal(1, r3.OverloadCount);
        Assert.Equal(80.0, time.Residents.People[0].Sanity);
        Assert.Equal(chaosBefore + 5f, time.World.ChaosLevel, Tolerance);
    }

    [Fact]
    [Trait("TS", "TS-oracle-pair-007")]
    public void PronouncePair_2回下して20年後に3回目_歪みは起きない()
    {
        var (prophet, time) = CreateWithResident(100);
        prophet.PronouncePair(0, 1);
        prophet.PronouncePair(0, 1);

        time.World.Year = 20;
        prophet.OnTimeAdvanced();
        var result = prophet.PronouncePair(0, 1);

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
        prophet.PronouncePair(0, 1);
        prophet.PronouncePair(0, 1);

        time.Reset();
        prophet.Reset();

        Assert.Equal(3.0, prophet.Faith!.Value, 3);
        Assert.Empty(prophet.Faith.RecentYears);
    }

    [Fact]
    [Trait("TS", "TS-oracle-pair-009")]
    public void SubmitPronouncePair_命令で下す_直接下したのと同じ結果で番号が続く()
    {
        var (prophet, time, deck) = Create();
        var queue = new OracleCommandQueue(prophet, time);

        var outcome = queue.SubmitPronouncePair(0, 1);
        var rejected = queue.SubmitPronouncePair(0, 0);

        Assert.True(outcome.Accepted);
        Assert.Single(time.Dogmas);
        Assert.Equal("same_card", rejected.RejectedReason);
        Assert.Equal(2, deck.DiscardPile.Count);
        Assert.Equal(1, outcome.Command.Sequence);
        Assert.Equal(2, rejected.Command.Sequence);
    }
}
