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
        return (new ProphetDreams(time, TestRules.ProphetDreams, deck, TestRules.Oracle), time);
    }

    [Fact]
    [Trait("TS", "TS-oracle-pair-011")]
    public void OracleState_最初の状態_手札と神威と操作が入る()
    {
        var (prophet, _) = CreateProphet();

        using var json = JsonDocument.Parse(HostMessageWriter.OracleState(prophet, isDestroyed: false));
        var root = json.RootElement;

        Assert.Equal("ORACLE_STATE", root.GetProperty("type").GetString());
        Assert.Equal(1, root.GetProperty("version").GetInt32());
        Assert.Equal(
            prophet.Deck!.Hand,
            root.GetProperty("hand").EnumerateArray().Select(card => card.GetString()));
        var faith = root.GetProperty("faith");
        Assert.Equal(3.0, faith.GetProperty("value").GetDouble(), 3);
        Assert.Equal(3.0, faith.GetProperty("max").GetDouble(), 3);
        Assert.Equal(1.0, faith.GetProperty("cost").GetDouble(), 3);
        Assert.True(root.GetProperty("canPronounce").GetBoolean());
        Assert.False(root.GetProperty("isDestroyed").GetBoolean());
        Assert.Equal(JsonValueKind.Null, root.GetProperty("request").ValueKind);
        Assert.False(root.TryGetProperty("nodes", out _));
        Assert.False(root.TryGetProperty("canTamper", out _));
    }

    [Fact]
    [Trait("TS", "TS-oracle-pair-011")]
    public void OracleState_渇きの依頼中_困りごとと示す記号が入る()
    {
        var (prophet, time) = CreateProphet();
        time.World.WaterAmount = 10f;
        prophet.OnTimeAdvanced();

        using var json = JsonDocument.Parse(HostMessageWriter.OracleState(prophet, isDestroyed: false));
        var request = json.RootElement.GetProperty("request");

        Assert.Equal("THIRST", request.GetProperty("requestType").GetString());
        Assert.Equal("SYM_WATER", request.GetProperty("symbolId").GetString());
    }

    [Fact]
    [Trait("TS", "TS-oracle-pair-011")]
    public void OracleState_崩壊している_崩壊が入り神託を下せない()
    {
        var (prophet, time) = CreateProphet();
        time.World.IsDestroyed = true;

        using var json = JsonDocument.Parse(HostMessageWriter.OracleState(prophet, isDestroyed: true));
        var root = json.RootElement;

        Assert.True(root.GetProperty("isDestroyed").GetBoolean());
        Assert.False(root.GetProperty("canPronounce").GetBoolean());
    }

    [Fact]
    [Trait("TS", "TS-oracle-pair-011")]
    public void OracleState_神威がコスト未満_神託を下せない()
    {
        var (prophet, _) = CreateProphet();
        prophet.PronouncePair(0, 1);
        prophet.PronouncePair(0, 1);
        prophet.PronouncePair(0, 1);

        using var json = JsonDocument.Parse(HostMessageWriter.OracleState(prophet, isDestroyed: false));
        var root = json.RootElement;

        Assert.Equal(0.0, root.GetProperty("faith").GetProperty("value").GetDouble(), 3);
        Assert.False(root.GetProperty("canPronounce").GetBoolean());
    }
}
