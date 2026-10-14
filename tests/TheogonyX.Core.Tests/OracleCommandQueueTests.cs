using TheogonyX.Core;

namespace TheogonyX.Core.Tests;

// 神託の命令の待ち行列（docs/domain/multiplayer.md）。
public class OracleCommandQueueTests
{
    private const int Seed = 42;

    private static (OracleCommandQueue Queue, ProphetDreams Prophet, TimeControl Time) Create()
    {
        var time = new TimeControl(TestRules.World, TestRules.JumpYears);
        var prophet = new ProphetDreams(time, TestRules.ProphetDreams, new OracleDeck(Seed, TestRules.Deck), TestRules.Oracle);
        return (new OracleCommandQueue(prophet, time), prophet, time);
    }

    [Fact]
    [Trait("TS", "TS-multiplayer-001")]
    public void SubmitPronouncePair_命令で下す_直接下したのと同じ結果になる()
    {
        var (queue, queued, queuedTime) = Create();
        var (_, direct, directTime) = Create();

        var outcome = queue.SubmitPronouncePair(0, 1);
        var result = direct.PronouncePair(0, 1);

        Assert.True(outcome.Accepted);
        Assert.Equal(result.Dogma, outcome.Result!.Dogma);
        Assert.Equal(directTime.Dogmas, queuedTime.Dogmas);
        Assert.Equal(direct.Hand, queued.Hand);
        Assert.Equal(direct.Faith.Value, queued.Faith.Value);
    }

    [Fact]
    [Trait("TS", "TS-multiplayer-002")]
    public void SubmitPronouncePair_続けて2つ積む_順番の番号の順に実行する()
    {
        var (queue, _, time) = Create();

        var first = queue.SubmitPronouncePair(0, 1);
        var second = queue.SubmitPronouncePair(0, 1);

        Assert.True(first.Accepted);
        Assert.True(second.Accepted);
        Assert.True(first.Command.Sequence < second.Command.Sequence);
        Assert.Equal(2, time.Dogmas.Count);
        Assert.Equal(first.Result!.Dogma, time.Dogmas[0]);
        Assert.Equal(second.Result!.Dogma, time.Dogmas[1]);
    }

    [Fact]
    [Trait("TS", "TS-multiplayer-003")]
    public void SubmitPronouncePair_下せない命令_何も変えずに捨てて理由を返す()
    {
        var (queue, prophet, time) = Create();
        var before = prophet.Deck.Capture();

        var sameCard = queue.SubmitPronouncePair(1, 1);
        var outOfHand = queue.SubmitPronouncePair(0, 9);
        time.World.IsDestroyed = true;
        var destroyed = queue.SubmitPronouncePair(0, 1);

        Assert.Equal("same_card", sameCard.RejectedReason);
        Assert.Equal("card_not_in_hand", outOfHand.RejectedReason);
        Assert.Equal("star_destroyed", destroyed.RejectedReason);
        Assert.Empty(time.Dogmas);
        Assert.Equal(before.Hand, prophet.Deck.Hand);
        Assert.Equal(before.DrawPile, prophet.Deck.DrawPile);
    }

    [Fact]
    [Trait("TS", "TS-multiplayer-004")]
    public void SubmitPronouncePair_同じシードと同じ命令の列を2回再生する_世界とドグマが一致する()
    {
        (int Year, IReadOnlyList<Dogma> Dogmas, float Chaos, List<string> Hand) Replay()
        {
            var (queue, prophet, time) = Create();
            foreach (var (first, second) in new[] { (0, 1), (2, 3), (1, 2) })
            {
                queue.SubmitPronouncePair(first, second);
                time.World.Year += 5;
                prophet.OnTimeAdvanced();
            }
            time.Jump();
            return (time.World.Year, [.. time.Dogmas], time.World.ChaosLevel, [.. prophet.Hand]);
        }

        var first = Replay();
        var second = Replay();

        Assert.NotEmpty(first.Dogmas);
        Assert.Equal(first.Year, second.Year);
        Assert.Equal(first.Dogmas, second.Dogmas);
        Assert.Equal(first.Chaos, second.Chaos);
        Assert.Equal(first.Hand, second.Hand);
    }

    [Fact]
    [Trait("TS", "TS-multiplayer-005")]
    public void SubmitPronouncePair_命令を積んで実行した後_待ち行列は空で順番の番号は続く()
    {
        var (queue, _, _) = Create();

        var a = queue.SubmitPronouncePair(0, 1);
        var b = queue.SubmitPronouncePair(0, 1);

        Assert.Equal(0, queue.PendingCount);
        Assert.Equal(1, a.Command.Sequence);
        Assert.Equal(2, b.Command.Sequence);
    }
}
