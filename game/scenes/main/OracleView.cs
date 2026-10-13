using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Godot;
using HttpClient = System.Net.Http.HttpClient;
using TheogonyX.Core;

namespace TheogonyX.Scenes.Main;

/// <summary>
/// 予言者の誤解（docs/domain/ai-presentation.md）。神託でドグマが生まれたら、ローカルの LLM（Ollama）に神託の文を頼み、
/// できたら予言者の吹き出しに出して、年表のドグマの行に添える。LLM が使えない・応答が壊れているときは、種別ごとの定型文を出す。
/// 文は表現だけで、ゲームの計算には関わらない。頼むのは一度に 1 つで、ゲームの進行は止めない。
/// </summary>
public sealed class OracleView : IDisposable
{
    private readonly TimeControl _timeControl;
    private readonly OracleAiRules _rules;
    private readonly HttpClient _http;
    private readonly PanelContainer _bubble;
    private readonly Label _label;
    private readonly Queue<(HistoryEntry? Entry, Dogma Dogma, Request Request)> _queue = new();
    private bool _busy;
    private Request? _latest;
    private double _hideInSeconds;

    /// <param name="owner">ノードを探すためのシーンの根。</param>
    public OracleView(Node owner, TimeControl timeControl, DreamView dreams, OracleAiRules rules)
    {
        _timeControl = timeControl;
        _rules = rules;
        _http = new HttpClient { Timeout = TimeSpan.FromSeconds(rules.TimeoutSeconds) };
        _label = new Label { Name = "OracleText", AutowrapMode = TextServer.AutowrapMode.WordSmart, CustomMinimumSize = new Vector2(200, 0) };
        // 地表の上の真ん中寄りに重ねて出す（依頼の吹き出し・年表の欄より手前）
        _bubble = new PanelContainer { Name = "OracleBubble", Visible = false, Position = new Vector2(60, 64), MouseFilter = Control.MouseFilterEnum.Ignore };
        _bubble.AddChild(_label);
        owner.GetNode<Node>("Hud").AddChild(_bubble);
        dreams.OraclePronounced += OnOraclePronounced;
    }

    /// <summary>神託の文を年表に添えた（セーブし直すのに使う）。</summary>
    public event Action? OracleAttached;

    /// <summary>毎フレーム呼ぶ。吹き出しを出しておく時間を数える。</summary>
    public void Process(double delta)
    {
        if (!_bubble.Visible || _hideInSeconds <= 0)
        {
            return;
        }
        _hideInSeconds -= delta;
        if (_hideInSeconds <= 0)
        {
            _bubble.Visible = false;
        }
    }

    public void Dispose() => _http.Dispose();

    // 神託を下した直後に呼ばれる。ドグマが生まれていれば、そのドグマの年表の行（最後の行）と一緒に頼みを積む
    private void OnOraclePronounced()
    {
        var dogmas = _timeControl.Dogmas;
        var entries = _timeControl.History?.Entries;
        if (dogmas.Count == 0 || entries is null || entries.Count == 0 || entries[^1].Type != HistoryEventType.DogmaFormed)
        {
            return;
        }
        var dogma = dogmas[^1];
        var entry = entries[^1];
        var request = new Request(dogma, _timeControl.World.Year);
        _latest = request;
        if (!_rules.Enabled)
        {
            ShowFallback(dogma);
            return;
        }
        ShowText(Tr("ORACLE_THINKING"), keepUntilReplaced: true);
        _queue.Enqueue((entry, dogma, request));
        if (!_busy)
        {
            _ = DrainAsync();
        }
    }

    // 積んだ頼みを 1 つずつ送る。結果は、いちばん新しい神託のものだけを吹き出しに出し、どれも年表に添える
    private async Task DrainAsync()
    {
        _busy = true;
        try
        {
            while (_queue.Count > 0)
            {
                var (entry, dogma, request) = _queue.Dequeue();
                var (text, reason, durationMs) = await GenerateAsync(dogma);
                if (text is not null)
                {
                    GD.Print(FormattableString.Invariant(
                        $"event=oracle_generated year={request.Year} symbolId={dogma.TargetSymbolId} dogmaType={dogma.Type} durationMs={durationMs}"));
                    if (entry is not null && _timeControl.History?.AttachOracle(entry, text) == true)
                    {
                        OracleAttached?.Invoke();
                    }
                }
                else
                {
                    GD.PushWarning(FormattableString.Invariant(
                        $"event=oracle_ai_failed year={request.Year} symbolId={dogma.TargetSymbolId} reason={reason} durationMs={durationMs}"));
                }
                if (ReferenceEquals(request, _latest))
                {
                    if (text is null)
                    {
                        ShowFallback(dogma);
                    }
                    else
                    {
                        ShowText(string.Join('\n', string.Format(CultureInfo.CurrentCulture, Tr("ORACLE_HEADING"), text.Heading), text.Intent, text.Doctrine));
                    }
                }
            }
        }
        finally
        {
            _busy = false;
        }
    }

    // LLM に頼む。失敗の理由は、つながらない・時間切れ・HTTP のエラー・形の崩れ
    private async Task<(OracleText? Text, string Reason, long DurationMs)> GenerateAsync(Dogma dogma)
    {
        var body = OracleProphecy.BuildRequest(
            _rules, dogma, Tr($"SYMBOL_{dogma.TargetSymbolId}"), Tr($"DOGMA_TYPE_{dogma.Type.ToString().ToUpperInvariant()}"),
            _timeControl.World, TranslationServer.GetLocale());
        var watch = Stopwatch.StartNew();
        try
        {
            using var content = new StringContent(body, Encoding.UTF8, "application/json");
            using var response = await _http.PostAsync(_rules.Endpoint, content);
            if (!response.IsSuccessStatusCode)
            {
                return (null, "http_error", watch.ElapsedMilliseconds);
            }
            var text = OracleProphecy.ParseResponse(await response.Content.ReadAsStringAsync());
            return (text, text is null ? "invalid_response" : string.Empty, watch.ElapsedMilliseconds);
        }
        catch (TaskCanceledException)
        {
            return (null, "timeout", watch.ElapsedMilliseconds);
        }
        catch (HttpRequestException)
        {
            return (null, "connection_failed", watch.ElapsedMilliseconds);
        }
    }

    private void ShowFallback(Dogma dogma) =>
        ShowText(string.Format(
            CultureInfo.CurrentCulture, Tr($"ORACLE_FALLBACK_{dogma.Type.ToString().ToUpperInvariant()}"), Tr($"SYMBOL_{dogma.TargetSymbolId}")));

    private void ShowText(string text, bool keepUntilReplaced = false)
    {
        _label.Text = text;
        _bubble.Visible = true;
        _hideInSeconds = keepUntilReplaced ? 0 : _rules.BubbleSeconds;
    }

    private static string Tr(string key) => TranslationServer.Translate(key).ToString();

    // 頼みの印（いちばん新しい神託か見分けるのに参照を使う）
    private sealed record Request(Dogma Dogma, int Year);
}
