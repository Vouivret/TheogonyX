using TheogonyX.Core.Protocol;

namespace TheogonyX.Core.Tests;

public class StreamSessionsTests
{
    private const int MaxStreams = 2;
    private const int ClientA = 1;
    private const int ClientB = 2;
    private const int ClientC = 3;

    private static StreamSessions CreateWithAAndBStreaming()
    {
        var sessions = new StreamSessions(MaxStreams);
        sessions.Start(ClientA);
        sessions.Start(ClientB);
        return sessions;
    }

    [Fact]
    [Trait("TS", "TS-frame-stream-001")]
    public void Start_空きがある_配信中になる()
    {
        var sessions = new StreamSessions(MaxStreams);

        var result = sessions.Start(ClientA);

        Assert.Equal(StreamStartResult.Started, result);
        Assert.True(sessions.IsStreaming(ClientA));
    }

    [Fact]
    [Trait("TS", "TS-frame-stream-002")]
    public void Start_2台が配信中_3台目は配信中にならない()
    {
        var sessions = CreateWithAAndBStreaming();

        var result = sessions.Start(ClientC);

        Assert.Equal(StreamStartResult.LimitReached, result);
        Assert.False(sessions.IsStreaming(ClientC));
        Assert.Equal([ClientA, ClientB], sessions.StreamingClients);
    }

    [Fact]
    [Trait("TS", "TS-frame-stream-003")]
    public void Stop_2台が配信中_枠が空いて3台目が配信中になる()
    {
        var sessions = CreateWithAAndBStreaming();

        var wasStreaming = sessions.Stop(ClientA);
        var result = sessions.Start(ClientC);

        Assert.True(wasStreaming);
        Assert.False(sessions.IsStreaming(ClientA));
        Assert.Equal(StreamStartResult.Started, result);
        Assert.Equal([ClientB, ClientC], sessions.StreamingClients);
    }

    [Fact]
    [Trait("TS", "TS-frame-stream-004")]
    public void Stop_配信中の接続が切断_状態が消えて3台目が配信中になる()
    {
        var sessions = CreateWithAAndBStreaming();

        // 切断したときも、ホストは Stop を呼ぶ
        sessions.Stop(ClientB);
        var result = sessions.Start(ClientC);

        Assert.Equal(StreamStartResult.Started, result);
        Assert.Equal([ClientA, ClientC], sessions.StreamingClients);
    }

    [Fact]
    [Trait("TS", "TS-frame-stream-005")]
    public void StartとStop_状態が変わらない操作_何も変わらない()
    {
        var sessions = CreateWithAAndBStreaming();

        var restart = sessions.Start(ClientA);
        var stopNotStreaming = sessions.Stop(ClientC);

        Assert.Equal(StreamStartResult.AlreadyStreaming, restart);
        Assert.False(stopNotStreaming);
        Assert.Equal([ClientA, ClientB], sessions.StreamingClients);
    }
}
