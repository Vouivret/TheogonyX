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
