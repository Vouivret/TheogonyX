using System;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using TheogonyX.Core;
using HttpClient = System.Net.Http.HttpClient;

namespace TheogonyX.Scenes.Main;

/// <summary>
/// ローカルの LLM（Ollama の生成 API）への HTTP の通信。宛先はループバックだけ（<see cref="OracleAiRules.Validate"/>）。
/// 予言者の誤解と歴史の神話化で共有する。ゲームの計算には関わらない。
/// </summary>
public sealed class OllamaClient : IDisposable
{
    private readonly OracleAiRules _rules;
    private readonly HttpClient _http;

    public OllamaClient(OracleAiRules rules)
    {
        _rules = rules;
        _http = new HttpClient { Timeout = TimeSpan.FromSeconds(rules.TimeoutSeconds) };
    }

    /// <summary>頼みを送り、応答の本文を待つ。</summary>
    /// <returns>応答の本文（失敗したら null）・失敗の理由（<c>http_error</c>・<c>timeout</c>・<c>connection_failed</c>。成功なら空）・かかった時間（ミリ秒）。</returns>
    public async Task<(string? Body, string Reason, long DurationMs)> PostAsync(string requestBody)
    {
        var watch = Stopwatch.StartNew();
        try
        {
            using var content = new StringContent(requestBody, Encoding.UTF8, "application/json");
            using var response = await _http.PostAsync(_rules.Endpoint, content);
            if (!response.IsSuccessStatusCode)
            {
                return (null, "http_error", watch.ElapsedMilliseconds);
            }
            return (await response.Content.ReadAsStringAsync(), string.Empty, watch.ElapsedMilliseconds);
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

    public void Dispose() => _http.Dispose();
}
