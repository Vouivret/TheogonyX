using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Godot;
using TheogonyX.Core;

namespace TheogonyX.Scenes.Main;

/// <summary>
/// 字の神託を下した後の吹き出しと、予言者の祈りの字（docs/domain/script-decipher.md）。
/// 神託を下したら、掟の 1 行と人の読みを吹き出しに出す。祈りは字の列で出し、その字を手帳に載せる。
/// </summary>
public sealed class OracleView
{
    private readonly TimeControl _timeControl;
    private readonly DreamView _dreams;
    private readonly OracleAiRules _rules;
    private readonly ProphetDreams _prophet;
    private readonly PanelContainer _bubble;
    private readonly PanelContainer _prayerPanel;
    private readonly HBoxContainer _prayerGlyphs;
    private readonly Label _label;
    private double _hideInSeconds;
    private double _prayerHideInSeconds;

    /// <param name="owner">ノードを探すためのシーンの根。</param>
    /// <param name="rules">吹き出しを出しておく秒数に使う。</param>
    public OracleView(Node owner, TimeControl timeControl, DreamView dreams, OracleAiRules rules, ProphetDreams prophet)
    {
        _timeControl = timeControl;
        _dreams = dreams;
        _rules = rules;
        _prophet = prophet;
        _label = new Label { Name = "OracleText", AutowrapMode = TextServer.AutowrapMode.WordSmart, CustomMinimumSize = new Vector2(240, 0) };
        // 予言者の頭の上に、上から神託の吹き出し・祈りの字・依頼の吹き出しの順に積む（置き場所は HudLayout）
        var bubbles = owner.GetNode<Container>("%ProphetBubbles");
        _bubble = new PanelContainer
        {
            Name = "OracleBubble",
            Visible = false,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            ThemeTypeVariation = "Bubble",
            SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter,
        };
        _bubble.AddChild(_label);
        bubbles.AddChild(_bubble);
        bubbles.MoveChild(_bubble, 0);
        _prayerGlyphs = new HBoxContainer { Name = "PrayerGlyphs" };
        _prayerPanel = new PanelContainer
        {
            Name = "PrayerGlyphPanel",
            Visible = false,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            ThemeTypeVariation = "Bubble",
            SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter,
        };
        _prayerPanel.AddChild(_prayerGlyphs);
        bubbles.AddChild(_prayerPanel);
        bubbles.MoveChild(_prayerPanel, 1);
        dreams.Prayed += OnPrayed;
    }

    /// <summary>字の神託を下した（セーブし直すのに使う）。</summary>
    public event Action? Spoken;

    /// <summary>神託を下した（時間のボタンを描き直すのに使う）。</summary>
    public event Action? StateChanged;

    /// <summary>毎フレーム呼ぶ。吹き出しを出しておく時間を数える。</summary>
    public void Process(double delta)
    {
        if (_prayerHideInSeconds > 0)
        {
            _prayerHideInSeconds -= delta;
            if (_prayerHideInSeconds <= 0)
            {
                HidePrayerGlyphsIfAnswered();
            }
        }
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

    /// <summary>
    /// 神が書いた文で神託を下す（docs/domain/script-decipher.md の「7. 語の字と神託の碑」）。人の読みと、掟の 1 行・行き先を吹き出しに出す。
    /// </summary>
    /// <returns>下せたか（下せなければ理由をログに出す）。</returns>
    public bool SpeakScript(Sentence sentence)
    {
        var year = _timeControl.World.Year;
        var outcome = _prophet.SpeakScript(sentence);
        if (outcome.Result is not { } result || outcome.Reading is not { } reading)
        {
            GD.PushWarning(FormattableString.Invariant($"event=oracle_script_rejected year={year} reason={outcome.RejectedReason} count={sentence.Words.Count}"));
            return false;
        }
        if (result.Dogma is { } dogma)
        {
            GD.Print(FormattableString.Invariant(
                $"event=dogma_formed year={year} symbolId={dogma.TargetSymbolId} dogmaType={dogma.Type} intensity={dogma.Intensity}"));
        }
        else
        {
            GD.Print(FormattableString.Invariant($"event=oracle_unreadable year={year} count={sentence.Words.Count}"));
        }
        if (result.AnsweredRequest is { } request)
        {
            GD.Print(FormattableString.Invariant($"event=prayer_answered year={year} requestType={request.Id}"));
        }
        if (result.OverloadCount > 0)
        {
            GD.Print(FormattableString.Invariant($"event=oracle_overload year={year} count={result.OverloadCount} chaosLevel={_timeControl.World.ChaosLevel}"));
        }
        if (result.ResumedAutoPlay)
        {
            GD.Print(FormattableString.Invariant($"event=auto_play_resumed year={year} reason=oracle_pronounced"));
        }
        ShowText(ReadingText(reading));
        _dreams.Refresh();
        StateChanged?.Invoke();
        Spoken?.Invoke();
        return true;
    }

    // 吹き出しの文: 掟の 1 行、人が文をどう読んだか（組ごと・読み落とした語の数）、行き先
    private static string ReadingText(ScriptReading reading)
    {
        var culture = CultureInfo.CurrentCulture;
        var lines = new List<string>();
        if (reading.Teaching is { } teaching)
        {
            lines.Add(string.Format(culture, Tr($"ORACLE_LINE_{teaching.Type.ToString().ToUpperInvariant()}"), Tr($"SYMBOL_{teaching.TargetSymbolId}")));
        }
        lines.Add(PhrasesText(reading, "ORACLE_READ"));
        if (reading.Destination != TravelDirection.None)
        {
            lines.Add(Tr(reading.Destination == TravelDirection.East ? "ORACLE_DESTINATION_EAST" : "ORACLE_DESTINATION_WEST"));
        }
        return string.Join('\n', lines);
    }

    /// <summary>
    /// 人が文をどう読んだかの 1 行（組ごとの読み。読み落とした語があれば数を添える）。<paramref name="formatKey"/> は「{0}」に組を入れる文言のキー。
    /// </summary>
    public static string PhrasesText(ScriptReading reading, string formatKey)
    {
        var culture = CultureInfo.CurrentCulture;
        var phrases = reading.Phrases.Select(phrase => !phrase.IsReadable ? Tr("ORACLE_READ_UNREADABLE")
            : phrase.VerbRead ? Tr($"SYMBOL_{phrase.SymbolId}") + "・" + Tr($"NOTEBOOK_TYPE_{phrase.Type.ToString().ToUpperInvariant()}")
            : string.Format(culture, Tr("ORACLE_READ_NOUN_ONLY"), Tr($"SYMBOL_{phrase.SymbolId}")));
        var text = string.Format(culture, Tr(formatKey), string.Join(" ／ ", phrases));
        return reading.DroppedVerbs > 0 ? text + "\n" + string.Format(culture, Tr("ORACLE_READ_DROPPED"), reading.DroppedVerbs) : text;
    }

    // 予言者が祈った: 祈りの字を出す（依頼の祈りは依頼が済むまで、日々の祈りは吹き出しと同じ秒数だけ）
    private void OnPrayed(Prayer prayer)
    {
        if (prayer.Request is null)
        {
            GD.Print(FormattableString.Invariant($"event=prophet_whispered year={_timeControl.World.Year} count={prayer.SymbolIds.Count}"));
        }
        ShowPrayerGlyphs(prayer);
    }

    /// <summary>依頼が済んだら、祈りの字を隠す（時間が進んだ後に呼ぶ）。</summary>
    public void HidePrayerGlyphsIfAnswered()
    {
        if (_prophet.ActiveRequest is null && _prayerHideInSeconds <= 0)
        {
            _prayerPanel.Visible = false;
        }
    }

    // 祈りの文（人の字）を出し、その字を手帳に載せる
    private void ShowPrayerGlyphs(Prayer prayer)
    {
        if (_prophet.Script is not { } script)
        {
            return;
        }
        var sentence = ScriptReader.PrayerSentence(script.Script, prayer.Request, prayer.SymbolIds);
        foreach (var child in _prayerGlyphs.GetChildren())
        {
            _prayerGlyphs.RemoveChild(child);
            child.QueueFree();
        }
        var view = new GlyphView { CustomMinimumSize = new Vector2(32 * sentence.Words.Count, 32), MouseFilter = Control.MouseFilterEnum.Ignore };
        view.Show(script.Script, sentence, script.Stage);
        _prayerGlyphs.AddChild(view);
        var added = script.Discover(sentence);
        _prayerPanel.Visible = sentence.Words.Count > 0;
        _prayerHideInSeconds = prayer.Request is null ? _rules.BubbleSeconds : 0;
        if (added > 0)
        {
            GD.Print(FormattableString.Invariant($"event=glyph_discovered count={added}"));
        }
    }

    private void ShowText(string text)
    {
        _label.Text = text;
        _bubble.Visible = true;
        _hideInSeconds = _rules.BubbleSeconds;
    }

    private static string Tr(string key) => TranslationServer.Translate(key).ToString();
}
