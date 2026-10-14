using System.Text.Json;

namespace TheogonyX.Core.Tests;

/// <summary>予言者の誤解（docs/domain/ai-presentation.md の受け入れ条件 AC-ai-presentation-001〜005）。</summary>
public class OracleProphecyTests
{
    private static readonly OracleAiRules Rules = new(true, "http://127.0.0.1:11434/api/generate", "gemma2:9b", 60, 20);

    private static WorldState World(float chaos, float harmony) => new()
    {
        Year = 120,
        ForestAmount = 50f,
        WaterAmount = 50f,
        Temperature = 15f,
        ChaosLevel = chaos,
        HarmonyLevel = harmony,
    };

    // Ollama の生成 API の応答の形（response に、LLM が書いた JSON の文字列が入る）
    private static string Response(string inner) => JsonSerializer.Serialize(new { model = "gemma2:9b", response = inner, done = true });

    [Fact]
    [Trait("TS", "TS-ai-presentation-001")]
    public void BuildRequest_木の禁忌とカオス度72_モデルとJSONの形と記号と種別と強度とカオス度と調和度を渡す()
    {
        var body = OracleProphecy.BuildRequest(Rules, new Dogma("SYM_TREE", DogmaType.Taboo, -0.8f), "木", "禁忌", World(72f, 30f), "ja");

        using var request = JsonDocument.Parse(body);
        var root = request.RootElement;
        Assert.Equal("gemma2:9b", root.GetProperty("model").GetString());
        Assert.False(root.GetProperty("stream").GetBoolean());
        Assert.Equal("json", root.GetProperty("format").GetString());
        var prompt = root.GetProperty("prompt").GetString()!;
        Assert.Contains("「木」の禁忌", prompt);
        Assert.Contains("強さ -0.80", prompt);
        Assert.Contains("カオス度 72", prompt);
        Assert.Contains("調和度 30", prompt);
        Assert.Contains("日本語で答えよ", prompt);
    }

    [Fact]
    [Trait("TS", "TS-ai-presentation-001")]
    public void BuildPrompt_日本語でない言語_英語で答えさせる()
    {
        var prompt = OracleProphecy.BuildPrompt(new Dogma("SYM_WATER", DogmaType.Worship, 0.9f), "Blessed Rain", "Worship", World(5f, 80f), "en");

        Assert.Contains("Worship of \"Blessed Rain\" (strength 0.90", prompt);
        Assert.Contains("chaos 5", prompt);
        Assert.Contains("Answer in English", prompt);
    }

    [Fact]
    [Trait("TS", "TS-ai-presentation-001")]
    public void BuildPrompt_2枚目と関係がある_2枚目の記号と間柄の雰囲気を渡し関係の名前は渡さない()
    {
        var dogma = new Dogma("SYM_FIRE", DogmaType.Worship, 0.85f);

        var ja = OracleProphecy.BuildPrompt(dogma, "火", "崇拝", World(5f, 80f), "ja", "木", WuxingRelation.Generated);
        var en = OracleProphecy.BuildPrompt(dogma, "Fire", "Worship", World(5f, 80f), "en", "Tree", WuxingRelation.Overcome);
        var none = OracleProphecy.BuildPrompt(dogma, "火", "崇拝", World(5f, 80f), "ja");

        Assert.Contains("「火」に「木」が重なった", ja);
        Assert.Contains("調和した間柄", ja);
        Assert.DoesNotContain("相生", ja);
        Assert.Contains("\"Tree\" was laid over \"Fire\"", en);
        Assert.Contains("in tension", en);
        Assert.DoesNotContain("啓示の組", none);
    }

    [Fact]
    [Trait("TS", "TS-ai-presentation-002")]
    public void ParseResponse_3つの文がある_前後の空白を除いて取り出す()
    {
        var body = Response("""{"heading": " 木に触れるな ", "intent": "神は木を憎んでおられる。", "doctrine": "木を見た者は目を閉じよ。"}""");

        Assert.Equal(new OracleText("木に触れるな", "神は木を憎んでおられる。", "木を見た者は目を閉じよ。"), OracleProphecy.ParseResponse(body));
    }

    [Theory]
    [Trait("TS", "TS-ai-presentation-002")]
    [InlineData("not json")] // 外側が JSON でない
    [InlineData("""{"model": "gemma2:9b"}""")] // response が無い
    [InlineData("""{"response": 3}""")] // response が文字列でない
    public void ParseResponse_外側の形が崩れている_nullを返す(string body)
    {
        Assert.Null(OracleProphecy.ParseResponse(body));
    }

    [Theory]
    [Trait("TS", "TS-ai-presentation-002")]
    [InlineData("木を恐れよ")] // 中身が JSON でない
    [InlineData("""["a", "b", "c"]""")] // オブジェクトでない
    [InlineData("""{"heading": "見出し", "intent": "意図"}""")] // 教義が無い
    [InlineData("""{"heading": "見出し", "intent": "意図", "doctrine": 1}""")] // 文字列でない
    [InlineData("""{"heading": "  ", "intent": "意図", "doctrine": "教義"}""")] // 空
    [InlineData("""{"heading": "一二三四五六七八九十一二三四五六七八九十一二三四五六七八九十一", "intent": "意図", "doctrine": "教義"}""")] // 見出しが 31 字
    [InlineData("""{"heading": "見出し", "intent": "意図\n改行", "doctrine": "教義"}""")] // 改行
    public void ParseResponse_中身が崩れている_nullを返す(string inner)
    {
        Assert.Null(OracleProphecy.ParseResponse(Response(inner)));
    }

    [Fact]
    [Trait("TS", "TS-ai-presentation-002")]
    public void ParseResponse_見出し30字と意図と教義80字_取り出す()
    {
        var heading = new string('神', 30);
        var body = new string('誤', 80);

        Assert.NotNull(OracleProphecy.ParseResponse(Response(JsonSerializer.Serialize(new { heading, intent = body, doctrine = body }))));
    }

    [Theory]
    [Trait("TS", "TS-ai-presentation-003")]
    [InlineData("http://127.0.0.1:11434/api/generate", true)]
    [InlineData("http://localhost:11434/api/generate", true)]
    [InlineData("http://[::1]:11434/api/generate", true)]
    [InlineData("http://192.168.1.13:11434/api/generate", false)] // ループバックでない
    [InlineData("https://api.example.com/v1/generate", false)]
    [InlineData("file:///C:/ollama", false)]
    [InlineData("127.0.0.1:11434", false)] // 絶対の URL でない
    public void Validate_宛先_ループバックのhttpだけを許す(string endpoint, bool valid)
    {
        var rules = Rules with { Endpoint = endpoint };

        if (valid)
        {
            rules.Validate();
        }
        else
        {
            Assert.Throws<ArgumentException>(rules.Validate);
        }
    }

    [Fact]
    [Trait("TS", "TS-ai-presentation-003")]
    public void Validate_待つ秒数0か吹き出しの秒数が負かモデル名が空_失敗する()
    {
        Assert.Throws<ArgumentException>((Rules with { TimeoutSeconds = 0 }).Validate);
        Assert.Throws<ArgumentException>((Rules with { BubbleSeconds = -1 }).Validate);
        Assert.Throws<ArgumentException>((Rules with { Model = " " }).Validate);
    }

    [Fact]
    [Trait("TS", "TS-ai-presentation-004")]
    public void AttachOracle_ドグマの行と消えた行と崩壊の行_ドグマの行だけに添える()
    {
        var oracle = new OracleText("木に触れるな", "神は木を憎んでおられる。", "木を見た者は目を閉じよ。");
        var history = new History([], TestRules.History);
        history.Add(new HistoryEntry(120, HistoryEventType.DogmaFormed, "SYM_TREE", DogmaType.Taboo));
        var dogmaEntry = history.Entries[0];
        history.Add(new HistoryEntry(130, HistoryEventType.WorldDestroyed));
        var destroyedEntry = history.Entries[1];

        Assert.True(history.AttachOracle(dogmaEntry, oracle));
        Assert.Equal(oracle, history.Entries[0].Oracle);
        Assert.False(history.AttachOracle(destroyedEntry, oracle));
        // 同じ中身でも、年表から受け取った行でなければ（世界再創生・読み込みで入れ替わった行）添えない
        Assert.False(history.AttachOracle(new HistoryEntry(120, HistoryEventType.DogmaFormed, "SYM_TREE", DogmaType.Taboo), oracle));
        Assert.False(history.AttachOracle(history.Entries[0], oracle with { Heading = "" }));
    }
}
