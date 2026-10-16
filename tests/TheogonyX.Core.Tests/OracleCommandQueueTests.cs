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
        prophet.AttachDialect(new DialectState(OracleDialectTests.Rules, OracleDialectTests.Silent, Seed));
        return (new OracleCommandQueue(prophet, time), prophet, time);
    }

    // 手札の先頭の記号の名詞と「守る」を聞き取った結果
    private static Hearing HearFirst(ProphetDreams prophet)
    {
        var symbol = prophet.SymbolOf(prophet.Hand[0]);
        var noun = prophet.Dialect!.Rules.Nouns.First(candidate => candidate.SymbolId == symbol);
        return prophet.Hear(new HeardAnswer(noun.Id, "VERB_PROTECT", HeardDestination.None, null, [])).Hearing!;
    }

    [Fact]
    [Trait("TS", "TS-multiplayer-001")]
    public void SubmitSpeak_命令で下す_直接下したのと同じ結果になる()
    {
        var (queue, queued, queuedTime) = Create();
        var (_, direct, directTime) = Create();

        var outcome = queue.SubmitSpeak(HearFirst(queued), "神の言葉");
        var result = direct.Speak(HearFirst(direct), "神の言葉");

        Assert.Null(outcome.RejectedReason);
        Assert.Equal(result.Dogma, outcome.Result!.Dogma);
        Assert.Equal(directTime.Dogmas, queuedTime.Dogmas);
        Assert.Equal(direct.Hand, queued.Hand);
        Assert.Equal(direct.Faith.Value, queued.Faith.Value);
    }

    [Fact]
    [Trait("TS", "TS-multiplayer-002")]
    public void SubmitSpeak_続けて2つ積む_順番の番号の順に実行する()
    {
        var (queue, prophet, time) = Create();

        var first = queue.SubmitSpeak(HearFirst(prophet), "神の言葉");
        var second = queue.SubmitSpeak(HearFirst(prophet), "神の言葉");

        Assert.Null(first.RejectedReason);
        Assert.Null(second.RejectedReason);
        Assert.True(first.Sequence < second.Sequence);
        Assert.Equal(2, time.Dogmas.Count);
        Assert.Equal(first.Result!.Dogma, time.Dogmas[0]);
        Assert.Equal(second.Result!.Dogma, time.Dogmas[1]);
    }

    [Fact]
    [Trait("TS", "TS-multiplayer-003")]
    public void SubmitSpeak_下せない命令_何も変えずに捨てて理由を返す()
    {
        var (queue, prophet, time) = Create();
        var hearing = HearFirst(prophet);
        prophet.RestoreFaith(0.5, []);
        var before = prophet.Deck.Capture();

        var noFaith = queue.SubmitSpeak(hearing, "神の言葉");
        prophet.RestoreFaith(3, []);
        time.World.IsDestroyed = true;
        var destroyed = queue.SubmitSpeak(hearing, "神の言葉");

        Assert.Equal("not_enough_faith", noFaith.RejectedReason);
        Assert.Equal("star_destroyed", destroyed.RejectedReason);
        Assert.Null(noFaith.Result);
        Assert.Empty(time.Dogmas);
        Assert.Equal(before.Hand, prophet.Deck.Hand);
        Assert.Equal(before.DrawPile, prophet.Deck.DrawPile);
    }

    [Fact]
    [Trait("TS", "TS-multiplayer-004")]
    public void SubmitSpeak_同じシードと同じ命令の列を2回再生する_世界とドグマが一致する()
    {
        (int Year, IReadOnlyList<Dogma> Dogmas, float Chaos, List<string> Hand) Replay()
        {
            var (queue, prophet, time) = Create();
            for (var i = 0; i < 3; i++)
            {
                queue.SubmitSpeak(HearFirst(prophet), "神の言葉");
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
    public void SubmitSpeak_命令を積んで実行した後_順番の番号は続く()
    {
        var (queue, prophet, _) = Create();

        var a = queue.SubmitSpeak(HearFirst(prophet), "神の言葉");
        var b = queue.SubmitSpeak(HearFirst(prophet), "神の言葉");

        Assert.Equal(1, a.Sequence);
        Assert.Equal(2, b.Sequence);
    }
}
