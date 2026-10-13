using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using TheogonyX.Core;

namespace TheogonyX.Scenes.Main;

/// <summary>
/// 歴史の神話化（docs/domain/ai-presentation.md）。100 年ごとの章が閉じたら、ローカルの LLM（Ollama）に章の神話の文を頼み、
/// できたら年表の章の行に添える。できない・壊れていたら、章は記録だけを残す。頼むのは一度に 1 つで、ゲームの進行は止めない。
/// 章の行は、読み込み直すと参照が変わるので、神話の文が無い章は、読み込んだ後にもう一度頼む。
/// </summary>
public sealed class MythView
{
    private const double ScanIntervalSeconds = 0.5;

    // 頼む材料に入れる、章の中の行の数（新しい順に数える）と、前の章の言い伝えの数（TODO(AI_UNSURE)。docs/domain/ai-presentation.md の「未決」）
    private const int MaxEventLines = 20;
    private const int MaxPreviousHeadings = 3;

    private readonly TimeControl _timeControl;
    private readonly OracleAiRules _rules;
    private readonly OllamaClient _client;

    // 頼んだ章の行（参照で覚える）。失敗した章は、この世界の中では頼み直さない
    private readonly HashSet<HistoryEntry> _requested = new(ReferenceEqualityComparer.Instance);
    private readonly Queue<HistoryEntry> _queue = new();
    private bool _busy;
    private double _sinceScan;

    public MythView(TimeControl timeControl, OracleAiRules rules, OllamaClient client)
    {
        _timeControl = timeControl;
        _rules = rules;
        _client = client;
    }

    /// <summary>神話の文を年表に添えた（セーブし直すのに使う）。</summary>
    public event Action? MythAttached;

    /// <summary>毎フレーム呼ぶ。年表を一定の間隔で見て、神話の文が無い章を頼む列に積む。</summary>
    public void Process(double delta)
    {
        if (!_rules.Enabled || _timeControl.History is null)
        {
            return;
        }
        _sinceScan += delta;
        if (_sinceScan < ScanIntervalSeconds)
        {
            return;
        }
        _sinceScan = 0;
        foreach (var entry in _timeControl.History.Entries)
        {
            if (entry.Type == HistoryEventType.ChapterEnded && entry.Myth is null && _requested.Add(entry))
            {
                _queue.Enqueue(entry);
            }
        }
        if (_queue.Count > 0 && !_busy)
        {
            _ = DrainAsync();
        }
    }

    // 積んだ頼みを 1 つずつ送る。できた神話は年表の章の行に添える（年表が入れ替わっていたら捨てる）
    private async Task DrainAsync()
    {
        _busy = true;
        try
        {
            while (_queue.Count > 0)
            {
                var entry = _queue.Dequeue();
                var chapter = BuildChapter(entry);
                if (chapter is null)
                {
                    continue;
                }
                var (reply, failure, durationMs) = await _client.PostAsync(MythChronicle.BuildRequest(_rules, chapter, TranslationServer.GetLocale()));
                var myth = reply is null ? null : MythChronicle.ParseResponse(reply);
                if (myth is null)
                {
                    var reason = reply is null ? failure : "invalid_response";
                    GD.PushWarning(FormattableString.Invariant($"event=myth_ai_failed year={chapter.ToYear} reason={reason} durationMs={durationMs}"));
                    continue;
                }
                if (_timeControl.History?.AttachMyth(entry, myth) == true)
                {
                    GD.Print(FormattableString.Invariant($"event=myth_generated year={chapter.ToYear} durationMs={durationMs}"));
                    MythAttached?.Invoke();
                }
            }
        }
        finally
        {
            _busy = false;
        }
    }

    // 章の行の前の出来事（前の章の行・再創生の行まで）と、前の章の言い伝え（新しい順）を集める。年表に無い行なら null
    private MythChapter? BuildChapter(HistoryEntry entry)
    {
        var history = _timeControl.History;
        if (history is null)
        {
            return null;
        }
        var entries = history.Entries;
        var index = -1;
        for (var i = 0; i < entries.Count; i++)
        {
            if (ReferenceEquals(entries[i], entry))
            {
                index = i;
            }
        }
        if (index < 0)
        {
            return null;
        }

        var events = new List<string>();
        for (var i = index - 1; i >= 0 && events.Count < MaxEventLines; i--)
        {
            if (entries[i].Type is HistoryEventType.ChapterEnded or HistoryEventType.WorldRecreated)
            {
                break;
            }
            events.Add(ChronicleText.LineOf(entries[i]));
        }
        events.Reverse();

        var previous = new List<(int Year, string Heading)>();
        for (var i = index - 1; i >= 0 && previous.Count < MaxPreviousHeadings; i--)
        {
            if (entries[i].Type == HistoryEventType.ChapterEnded && entries[i].Myth is { } myth)
            {
                previous.Add((entries[i].Year, myth.Heading));
            }
        }
        return new MythChapter(entry.FromYear ?? 0, entry.Year, entry.ChaosFrom ?? 0f, entry.ChaosTo ?? 0f, events, previous);
    }
}
