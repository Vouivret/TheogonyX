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

    // 手札は 2 枚だけの山札（木・雨）。神威は上限 3。方言の状態を付ける（ずれない器）
    private static (ProphetDreams Prophet, TimeControl Time) CreateProphet()
    {
        var time = new TimeControl(TestRules.World, TestRules.JumpYears);
        var deck = new OracleDeck(1, new DeckRules([new DeckRuneCount("SYM_TREE", 1), new DeckRuneCount("SYM_WATER", 1)], HandSize: 2));
        var prophet = new ProphetDreams(time, TestRules.ProphetDreams, deck, TestRules.Oracle);
        prophet.AttachDialect(new DialectState(OracleDialectTests.Rules, OracleDialectTests.Silent, 1));
        return (prophet, time);
    }

    private static JsonDocument State(ProphetDreams prophet, bool isDestroyed = false, bool listening = false, bool choiceMode = false, string? prayer = null) =>
        JsonDocument.Parse(HostMessageWriter.OracleState(prophet, isDestroyed, listening, choiceMode, prayer));

    [Fact]
    [Trait("TS", "TS-oracle-pair-011")]
    public void OracleState_最初の状態_神威と語りかけ方が入り手札は入らない()
    {
        var (prophet, _) = CreateProphet();

        using var json = State(prophet);
        var root = json.RootElement;

        Assert.Equal("ORACLE_STATE", root.GetProperty("type").GetString());
        Assert.Equal(2, root.GetProperty("version").GetInt32());
        var faith = root.GetProperty("faith");
        Assert.Equal(3.0, faith.GetProperty("value").GetDouble(), 3);
        Assert.Equal(3.0, faith.GetProperty("max").GetDouble(), 3);
        Assert.Equal(1.0, faith.GetProperty("cost").GetDouble(), 3);
        Assert.True(root.GetProperty("canSpeak").GetBoolean());
        Assert.False(root.GetProperty("isDestroyed").GetBoolean());
        Assert.Equal("VOICE", root.GetProperty("mode").GetString());
        Assert.False(root.GetProperty("listening").GetBoolean());
        Assert.Equal(JsonValueKind.Null, root.GetProperty("request").ValueKind);
        Assert.Equal(JsonValueKind.Null, root.GetProperty("told").ValueKind);
        Assert.Empty(root.GetProperty("book").EnumerateArray());
        Assert.Empty(root.GetProperty("nouns").EnumerateArray());
        Assert.False(root.TryGetProperty("hand", out _));
        Assert.False(root.TryGetProperty("pairs", out _));
    }

    [Fact]
    [Trait("TS", "TS-oracle-pair-011")]
    public void OracleState_言葉を選ぶ形_手札の記号の名詞と全部の動詞が入る()
    {
        var (prophet, _) = CreateProphet();

        using var json = State(prophet, choiceMode: true);
        var root = json.RootElement;

        Assert.Equal("WORDS", root.GetProperty("mode").GetString());
        Assert.Equal(["NOUN_FOREST", "NOUN_RIVER"], root.GetProperty("nouns").EnumerateArray().Select(noun => noun.GetString()));
        Assert.Equal(
            ["VERB_PROTECT", "VERB_AVOID", "VERB_CUT"], root.GetProperty("verbs").EnumerateArray().Select(verb => verb.GetString()));
    }

    [Fact]
    [Trait("TS", "TS-oracle-pair-011")]
    public void OracleState_渇きの依頼中_困りごとと示す記号と祈りが入る()
    {
        var (prophet, time) = CreateProphet();
        time.World.WaterAmount = 10f;
        prophet.OnTimeAdvanced();

        using var json = State(prophet, prayer: "神よ、森は渇き、川も細っています");
        var request = json.RootElement.GetProperty("request");

        Assert.Equal("THIRST", request.GetProperty("requestType").GetString());
        Assert.Equal("SYM_WATER", request.GetProperty("symbolId").GetString());
        Assert.Equal("神よ、森は渇き、川も細っています", request.GetProperty("prayer").GetString());
    }

    [Fact]
    [Trait("TS", "TS-oracle-pair-011")]
    public void OracleState_崩壊している_崩壊が入り語りかけられない()
    {
        var (prophet, time) = CreateProphet();
        time.World.IsDestroyed = true;

        using var json = State(prophet, isDestroyed: true);
        var root = json.RootElement;

        Assert.True(root.GetProperty("isDestroyed").GetBoolean());
        Assert.False(root.GetProperty("canSpeak").GetBoolean());
    }

    [Fact]
    [Trait("TS", "TS-oracle-pair-011")]
    public void OracleState_神威がコスト未満か聞いている途中_語りかけられない()
    {
        var (prophet, _) = CreateProphet();
        using (var listening = State(prophet, listening: true))
        {
            Assert.True(listening.RootElement.GetProperty("listening").GetBoolean());
            Assert.False(listening.RootElement.GetProperty("canSpeak").GetBoolean());
        }
        prophet.Restore(null, new Dictionary<string, int>());
        prophet.RestoreFaith(0.5, []);

        using var json = State(prophet);

        Assert.Equal(0.5, json.RootElement.GetProperty("faith").GetProperty("value").GetDouble(), 3);
        Assert.False(json.RootElement.GetProperty("canSpeak").GetBoolean());
    }

    [Fact]
    [Trait("TS", "TS-oracle-pair-011")]
    public void OracleState_告げた神託と方言帳_教えと聞こえた言葉と聞き違えが入る()
    {
        var (prophet, _) = CreateProphet();
        var hearing = prophet.Hear(new HeardAnswer("NOUN_FOREST", "VERB_PROTECT", HeardDestination.East, null, ["電気"])).Hearing!;
        prophet.Speak(hearing, "森を守れ");

        using var json = State(prophet);
        var told = json.RootElement.GetProperty("told");

        Assert.Equal(0, told.GetProperty("year").GetInt32());
        Assert.Equal("SYM_TREE", told.GetProperty("teaching").GetProperty("symbolId").GetString());
        Assert.Equal("WORSHIP", told.GetProperty("teaching").GetProperty("dogmaType").GetString());
        Assert.Equal("NOUN_FOREST", told.GetProperty("heardNounId").GetString());
        Assert.Equal("EAST", told.GetProperty("destination").GetString());
        Assert.Equal(["電気"], told.GetProperty("mysteryWords").EnumerateArray().Select(word => word.GetString()));
        Assert.Equal(JsonValueKind.Null, told.GetProperty("heardText").ValueKind);
        var entry = Assert.Single(json.RootElement.GetProperty("book").EnumerateArray());
        Assert.Equal("電気", entry.GetProperty("godWord").GetString());
        Assert.Equal(JsonValueKind.Null, entry.GetProperty("heardWordId").ValueKind);
        Assert.Equal("MYSTERY", entry.GetProperty("reason").GetString());
    }
}
