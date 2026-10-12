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

    // 夢を見るたびに、木(0.5) → 雨(0.6) → 火(0.4) を返す
    private static (ProphetDreams Dreams, TimeControl Time) CreateProphet()
    {
        var time = new TimeControl(TestRules.World, TestRules.JumpYears);
        DreamNode[] dream = [new("SYM_TREE", 0.5f), new("SYM_RAIN", 0.6f), new("SYM_FIRE", 0.4f)];
        return (new ProphetDreams(time, TestRules.Dream, TestRules.ProphetDreams, () => dream), time);
    }

    [Fact]
    [Trait("TS", "TS-dream-tampering-011")]
    public void DreamState_改ざん前の夢_夢ノードと手札と操作が入る()
    {
        var (prophet, _) = CreateProphet();

        using var json = JsonDocument.Parse(HostMessageWriter.DreamState(prophet, TestRules.Dream.Symbols, isDestroyed: false));
        var root = json.RootElement;

        Assert.Equal("DREAM_STATE", root.GetProperty("type").GetString());
        Assert.Equal(1, root.GetProperty("version").GetInt32());
        Assert.Equal("UNTAMPERED", root.GetProperty("state").GetString());
        Assert.Equal(
            [("SYM_TREE", 0.5f), ("SYM_RAIN", 0.6f), ("SYM_FIRE", 0.4f)],
            root.GetProperty("nodes").EnumerateArray().Select(node => (node.GetProperty("symbolId").GetString(), node.GetProperty("weight").GetSingle())));
        Assert.Equal(["SYM_TREE", "SYM_RAIN", "SYM_BLOOD", "SYM_FIRE"], root.GetProperty("cards").EnumerateArray().Select(card => card.GetString()));
        Assert.True(root.GetProperty("canTamper").GetBoolean());
        Assert.True(root.GetProperty("canPronounce").GetBoolean());
        Assert.False(root.GetProperty("isDestroyed").GetBoolean());
        Assert.Equal(JsonValueKind.Null, root.GetProperty("request").ValueKind);
    }

    [Fact]
    [Trait("TS", "TS-dream-tampering-011")]
    public void DreamState_改ざん済み_改ざんできない()
    {
        var (prophet, _) = CreateProphet();
        prophet.Tamper(1, "SYM_BLOOD");

        using var json = JsonDocument.Parse(HostMessageWriter.DreamState(prophet, TestRules.Dream.Symbols, isDestroyed: false));
        var root = json.RootElement;

        Assert.Equal("TAMPERED", root.GetProperty("state").GetString());
        Assert.Equal("SYM_BLOOD", root.GetProperty("nodes")[1].GetProperty("symbolId").GetString());
        Assert.False(root.GetProperty("canTamper").GetBoolean());
        Assert.True(root.GetProperty("canPronounce").GetBoolean());
    }

    [Fact]
    [Trait("TS", "TS-dream-tampering-011")]
    public void DreamState_夢なし_夢ノードが空で操作できない()
    {
        var (prophet, _) = CreateProphet();
        prophet.Pronounce();

        using var json = JsonDocument.Parse(HostMessageWriter.DreamState(prophet, TestRules.Dream.Symbols, isDestroyed: false));
        var root = json.RootElement;

        Assert.Equal("NONE", root.GetProperty("state").GetString());
        Assert.Empty(root.GetProperty("nodes").EnumerateArray());
        Assert.False(root.GetProperty("canTamper").GetBoolean());
        Assert.False(root.GetProperty("canPronounce").GetBoolean());
    }

    [Fact]
    [Trait("TS", "TS-dream-tampering-011")]
    public void DreamState_渇きの依頼中_困りごとと示す記号が入る()
    {
        var (prophet, time) = CreateProphet();
        time.World.WaterAmount = 10f;
        prophet.OnTimeAdvanced();

        using var json = JsonDocument.Parse(HostMessageWriter.DreamState(prophet, TestRules.Dream.Symbols, isDestroyed: false));
        var request = json.RootElement.GetProperty("request");

        Assert.Equal("THIRST", request.GetProperty("requestType").GetString());
        Assert.Equal("SYM_RAIN", request.GetProperty("symbolId").GetString());
    }

    [Fact]
    [Trait("TS", "TS-dream-tampering-011")]
    public void DreamState_崩壊している_崩壊が入り操作できない()
    {
        var (prophet, time) = CreateProphet();
        time.World.IsDestroyed = true;

        using var json = JsonDocument.Parse(HostMessageWriter.DreamState(prophet, TestRules.Dream.Symbols, isDestroyed: true));
        var root = json.RootElement;

        Assert.True(root.GetProperty("isDestroyed").GetBoolean());
        Assert.False(root.GetProperty("canTamper").GetBoolean());
        Assert.False(root.GetProperty("canPronounce").GetBoolean());
    }
}
