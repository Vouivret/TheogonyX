using System.Text.Json;
using TheogonyX.Core.Protocol;

namespace TheogonyX.Core.Tests;

public class HostMessageWriterTests
{
    [Fact]
    [Trait("TS", "TS-companion-pairing-008")]
    public void Paired_sessionToken_PAIREDのJSONになる()
    {
        using var json = JsonDocument.Parse(HostMessageWriter.Paired("Xk2pSession"));
        var root = json.RootElement;

        Assert.Equal("PAIRED", root.GetProperty("type").GetString());
        Assert.Equal(1, root.GetProperty("version").GetInt32());
        Assert.Equal("Xk2pSession", root.GetProperty("sessionToken").GetString());
    }

    [Fact]
    [Trait("TS", "TS-companion-pairing-008")]
    public void Error_上限に達した_ERRORのJSONになる()
    {
        using var json = JsonDocument.Parse(HostMessageWriter.Error(HostMessageWriter.MaxConnectionsReached));
        var root = json.RootElement;

        Assert.Equal("ERROR", root.GetProperty("type").GetString());
        Assert.Equal(1, root.GetProperty("version").GetInt32());
        Assert.Equal("MAX_CONNECTIONS_REACHED", root.GetProperty("reason").GetString());
    }

    // 手札は 2 枚だけの山札（木・雨）。神威は上限 3
    private static (ProphetDreams Prophet, TimeControl Time) CreateProphet()
    {
        var time = new TimeControl(TestRules.World, TestRules.JumpYears);
        var deck = new OracleDeck(1, new DeckRules([new DeckRuneCount("SYM_TREE", 1), new DeckRuneCount("SYM_WATER", 1)], HandSize: 2));
        var prophet = new ProphetDreams(time, TestRules.ProphetDreams, deck, TestRules.Oracle);
        return (prophet, time);
    }

    private static JsonDocument State(ProphetDreams prophet, bool isDestroyed = false) =>
        JsonDocument.Parse(HostMessageWriter.OracleState(prophet, isDestroyed));

    [Fact]
    [Trait("TS", "TS-oracle-pair-011")]
    public void OracleState_最初の状態_神威と字の神託の形が入り手札と語りかけの項目は入らない()
    {
        var (prophet, _) = CreateProphet();

        using var json = State(prophet);
        var root = json.RootElement;

        Assert.Equal("ORACLE_STATE", root.GetProperty("type").GetString());
        Assert.Equal(4, root.GetProperty("version").GetInt32());
        var faith = root.GetProperty("faith");
        Assert.Equal(3.0, faith.GetProperty("value").GetDouble(), 3);
        Assert.Equal(3.0, faith.GetProperty("max").GetDouble(), 3);
        Assert.Equal(1.0, faith.GetProperty("cost").GetDouble(), 3);
        Assert.False(root.GetProperty("isDestroyed").GetBoolean());
        Assert.Equal("SCRIPT", root.GetProperty("mode").GetString());
        Assert.Equal(JsonValueKind.Null, root.GetProperty("request").ValueKind);
        foreach (var removed in new[] { "hand", "pairs", "canSpeak", "listening", "nouns", "verbs", "told", "book" })
        {
            Assert.False(root.TryGetProperty(removed, out _), removed);
        }
    }

    [Fact]
    [Trait("TS", "TS-oracle-pair-011")]
    public void OracleState_渇きの依頼中_困りごとと示す記号が入る()
    {
        var (prophet, time) = CreateProphet();
        time.World.WaterAmount = 10f;
        prophet.OnTimeAdvanced();

        using var json = State(prophet);
        var request = json.RootElement.GetProperty("request");

        Assert.Equal("THIRST", request.GetProperty("requestType").GetString());
        Assert.Equal("SYM_WATER", request.GetProperty("symbolId").GetString());
        Assert.False(request.TryGetProperty("prayer", out _));
    }

    [Fact]
    [Trait("TS", "TS-oracle-pair-011")]
    public void OracleState_崩壊している_崩壊が入る()
    {
        var (prophet, time) = CreateProphet();
        time.World.IsDestroyed = true;

        using var json = State(prophet, isDestroyed: true);

        Assert.True(json.RootElement.GetProperty("isDestroyed").GetBoolean());
    }
}
