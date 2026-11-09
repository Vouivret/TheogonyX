using System.Collections.Generic;
using Godot;
using TheogonyX.Core;

namespace TheogonyX.Scenes.Main;

/// <summary>
/// 左下の出来事の行（docs/art-guide.md の「画面」）。年表に新しく載った行を、新しいものを下にして 3 行まで出し、
/// 数秒で薄れて消す。セーブの知らせもここに出す。年表の文は <see cref="ChronicleText"/> を使い回す。
/// </summary>
public sealed class EventTicker
{
    private const int MaxLines = 3;
    private const double HoldSeconds = 6.0;
    private const double FadeSeconds = 1.0;

    private readonly TimeControl _timeControl;
    private readonly Container _lines;

    // 最後に見た年表の行（年表は上限を超えると古い行から消えるので、数ではなく行で覚える）
    private HistoryEntry? _last;

    /// <param name="owner">ノードを探すためのシーンの根。</param>
    public EventTicker(Node owner, TimeControl timeControl)
    {
        _timeControl = timeControl;
        _lines = owner.GetNode<Container>("%Ticker");
        // 起動したときにすでに年表にある行（読み込んだ星の行）は出さない
        _last = LastOf(timeControl.History?.Entries);
    }

    /// <summary>毎フレーム呼ぶ。年表に行が増えていれば、増えた行のうち新しい 3 行を出す。</summary>
    public void Process()
    {
        var entries = _timeControl.History?.Entries;
        var last = LastOf(entries);
        if (entries is null || last is null || SameLine(last, _last))
        {
            _last = last;
            return;
        }
        // 前に見た行の次から。見つからなければ（一度に多く増えて古い行が消えた）、新しい 3 行だけ
        var from = entries.Count - MaxLines;
        for (var i = entries.Count - 1; i >= 0; i--)
        {
            if (SameLine(entries[i], _last))
            {
                from = i + 1;
                break;
            }
        }
        _last = last;
        for (var i = System.Math.Max(from, entries.Count - MaxLines); i < entries.Count; i++)
        {
            Show(ChronicleText.LineOf(entries[i]), HoldSeconds);
        }
    }

    /// <summary>1 行を出す（セーブの知らせなど）。</summary>
    public void Show(string text, double holdSeconds)
    {
        var label = new Label { Text = text, ThemeTypeVariation = "TickerLabel", MouseFilter = Control.MouseFilterEnum.Ignore };
        _lines.AddChild(label);
        while (_lines.GetChildCount() > MaxLines)
        {
            var oldest = _lines.GetChild(0);
            _lines.RemoveChild(oldest);
            oldest.QueueFree();
        }
        var tween = label.CreateTween();
        tween.TweenInterval(holdSeconds);
        tween.TweenProperty(label, "modulate:a", 0.0, FadeSeconds);
        tween.TweenCallback(Callable.From(label.QueueFree));
    }

    // 同じ行か。年表は神託の記録・神話の文を後から添えるときに行を差し替えるので、添えたものは比べない
    private static bool SameLine(HistoryEntry a, HistoryEntry? b) =>
        b is not null && a with { Oracle = null, Myth = null } == b with { Oracle = null, Myth = null };

    private static HistoryEntry? LastOf(IReadOnlyList<HistoryEntry>? entries) => entries is { Count: > 0 } ? entries[^1] : null;
}
