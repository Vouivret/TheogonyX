using System.Text.Json;

namespace TheogonyX.Core.Tests;

/// <summary>歴史の神話化（docs/domain/ai-presentation.md の受け入れ条件 AC-ai-presentation-007〜009）。</summary>
public class MythChronicleTests
{
    private static readonly OracleAiRules Rules = new(true, "http://127.0.0.1:11434/api/generate", "gemma2:9b", 60, 20);

    private static readonly MythChapter Chapter = new(
        200, 300, 10f, 40f,
        ["300 年　木の禁忌が生まれた", "305 年　誰かが死んだ"],
        [(100, "水の時代")]);

    // Ollama の生成 API の応答の形（response に、LLM が書いた JSON の文字列が入る）
    private static string Response(string inner) => JsonSerializer.Serialize(new { model = "gemma2:9b", response = inner, done = true });

    [Fact]
    [Trait("TS", "TS-ai-presentation-007")]
    public void BuildRequest_章の年とカオス度の変化と出来事と前の言い伝えを渡す()
    {
        var body = MythChronicle.BuildRequest(Rules, Chapter, "ja");

        using var request = JsonDocument.Parse(body);
        var root = request.RootElement;
        Assert.Equal("gemma2:9b", root.GetProperty("model").GetString());
        Assert.False(root.GetProperty("stream").GetBoolean());
        Assert.Equal("json", root.GetProperty("format").GetString());
        var prompt = root.GetProperty("prompt").GetString()!;
        Assert.Contains("時代: 200 年〜300 年", prompt);
        Assert.Contains("カオス度の変化: 10 → 40", prompt);
        Assert.Contains("- 300 年　木の禁忌が生まれた", prompt);
        Assert.Contains("100 年の時代「水の時代」", prompt);
        Assert.Contains("日本語で答えよ", prompt);
    }

    [Fact]
    [Trait("TS", "TS-ai-presentation-007")]
    public void BuildPrompt_日本語でない言語_英語で答えさせ前の言い伝えが無ければ触れない()
    {
        var prompt = MythChronicle.BuildPrompt(Chapter with { PreviousHeadings = [] }, "en");

        Assert.Contains("Age: years 200 to 300", prompt);
        Assert.Contains("Answer in English", prompt);
        Assert.DoesNotContain("Myths of earlier ages", prompt);
    }

    [Fact]
    [Trait("TS", "TS-ai-presentation-008")]
    public void ParseResponse_2つの文がある_前後の空白を除いて取り出す()
    {
        var body = Response("""{"heading": " 木の時代 ", "body": "木は人を見下ろした。"}""");

        Assert.Equal(new MythText("木の時代", "木は人を見下ろした。"), MythChronicle.ParseResponse(body));
    }

    [Theory]
    [Trait("TS", "TS-ai-presentation-008")]
    [InlineData("not json")] // 外側が JSON でない
    [InlineData("""{"model": "gemma2:9b"}""")] // response が無い
    public void ParseResponse_外側の形が崩れている_nullを返す(string body)
    {
        Assert.Null(MythChronicle.ParseResponse(body));
    }

    [Theory]
    [Trait("TS", "TS-ai-presentation-008")]
    [InlineData("木を語れ")] // 中身が JSON でない
    [InlineData("""{"heading": "見出し"}""")] // 本文が無い
    [InlineData("""{"heading": "  ", "body": "本文"}""")] // 見出しが空
    [InlineData("""{"heading": "一二三四五六七八九十一二三四五六七八九十一二三四五六七八九十一", "body": "本文"}""")] // 見出しが 31 字
    [InlineData("""{"heading": "見出し", "body": "本文\n改行"}""")] // 改行
    public void ParseResponse_中身が崩れている_nullを返す(string inner)
    {
        Assert.Null(MythChronicle.ParseResponse(Response(inner)));
    }

    [Fact]
    [Trait("TS", "TS-ai-presentation-008")]
    public void ParseResponse_本文200字_取り出す()
    {
        var body = new string('語', 200);

        Assert.NotNull(MythChronicle.ParseResponse(Response(JsonSerializer.Serialize(new { heading = "見出し", body }))));
    }

    [Fact]
    [Trait("TS", "TS-ai-presentation-009")]
    public void AttachMyth_章の行だけに添える_ドグマの行や同じ中身の別の行や2回目には添えない()
    {
        var myth = new MythText("木の時代", "木は人を見下ろした。");
        var history = new History([], TestRules.History, new ChapterStart(0, 0f));
        history.Add(new HistoryEntry(120, HistoryEventType.DogmaFormed, "SYM_TREE", DogmaType.Taboo));
        var dogmaEntry = history.Entries[0];
        history.CloseChapter(100, 0f);
        var chapterEntry = history.Entries[1];

        Assert.False(history.AttachMyth(dogmaEntry, myth));
        Assert.True(history.AttachMyth(chapterEntry, myth));
        Assert.Equal(myth, history.Entries[1].Myth);
        Assert.False(history.AttachMyth(history.Entries[1], myth));
        Assert.False(history.AttachMyth(new HistoryEntry(100, HistoryEventType.ChapterEnded, FromYear: 0, ChaosFrom: 0f, ChaosTo: 0f), myth));
    }
}
