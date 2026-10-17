using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using TheogonyX.Core;

namespace TheogonyX.Scenes.Main;

/// <summary>
/// 自由文の神託の流れと、予言者の吹き出し（docs/domain/oracle-pair.md の「自由文の神託」、ai-presentation.md）。
/// 神の言葉を受け取ったら、自動進行を止めて予言者に「耳を澄ませている」と言わせ、ローカルの LLM（Ollama）で文を語の ID に聞き取り、
/// コアが方言で聞き違えて神託を下す。下したら、掟の 1 行と聞こえた言葉を吹き出しに出し、続けて LLM が作る聞こえた文と語りに差し替える。
/// LLM が使えない・答えが壊れているときは、言葉を選ぶ形に替える。
/// 文は表現だけで、ゲームの計算には関わらない。
/// </summary>
public sealed class OracleView
{
    private readonly TimeControl _timeControl;
    private readonly DreamView _dreams;
    private readonly OracleAiRules _rules;
    private readonly OllamaClient _client;
    private readonly ProphetDreams _prophet;
    private readonly OracleCommandQueue _commands;
    private readonly PanelContainer _bubble;
    private readonly PanelContainer _prayerPanel;
    private readonly HBoxContainer _prayerGlyphs;
    private readonly Label _label;
    private double _hideInSeconds;
    private bool _listening;
    private OracleRecord? _latestSpeech;

    /// <param name="owner">ノードを探すためのシーンの根。</param>
    public OracleView(
        Node owner, TimeControl timeControl, DreamView dreams, OracleAiRules rules, OllamaClient client, ProphetDreams prophet, OracleCommandQueue commands)
    {
        _timeControl = timeControl;
        _dreams = dreams;
        _rules = rules;
        _client = client;
        _prophet = prophet;
        _commands = commands;
        _label = new Label { Name = "OracleText", AutowrapMode = TextServer.AutowrapMode.WordSmart, CustomMinimumSize = new Vector2(200, 0) };
        // 地表の上の真ん中寄りに重ねて出す（依頼の吹き出し・年表の欄より手前）
        _bubble = new PanelContainer { Name = "OracleBubble", Visible = false, Position = new Vector2(60, 64), MouseFilter = Control.MouseFilterEnum.Ignore };
        _bubble.AddChild(_label);
        owner.GetNode<Node>("Hud").AddChild(_bubble);
        dreams.SpeakRequested += OnSpeakRequested;
        _prayerGlyphs = new HBoxContainer { Name = "PrayerGlyphs" };
        _prayerPanel = new PanelContainer { Name = "PrayerGlyphPanel", Visible = false, Position = new Vector2(150, 92), MouseFilter = Control.MouseFilterEnum.Ignore };
        _prayerPanel.AddChild(_prayerGlyphs);
        owner.GetNode<Node>("Hud").AddChild(_prayerPanel);
        dreams.WordsChosen += OnWordsChosen;
        dreams.Prayed += OnPrayed;
        // LLM を使わない設定なら、最初から言葉を選ぶ形にする
        dreams.ChoiceMode = !rules.Enabled;
    }

    /// <summary>自由文の神託を下した、または聞こえた文と語りを添えた（セーブし直すのに使う）。</summary>
    public event Action? Spoken;

    /// <summary>聞いている間に自動進行を止めた・再開した、または聞き取りが終わった（時間のボタンを描き直すのに使う）。</summary>
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
    /// 神が組んだ字の列で神託を下す（docs/domain/script-decipher.md の「書く」）。人の読みと、掟の 1 行・行き先を吹き出しに出す。
    /// </summary>
    /// <returns>下せたか（下せなければ理由をログに出す）。</returns>
    public bool SpeakScript(IReadOnlyList<Glyph> glyphs)
    {
        var year = _timeControl.World.Year;
        var outcome = _prophet.SpeakScript(glyphs);
        if (outcome.Result is not { } result || outcome.Reading is not { } reading)
        {
            GD.PushWarning(FormattableString.Invariant($"event=oracle_script_rejected year={year} reason={outcome.RejectedReason} count={glyphs.Count}"));
            return false;
        }
        if (result.Dogma is { } dogma)
        {
            GD.Print(FormattableString.Invariant(
                $"event=dogma_formed year={year} symbolId={dogma.TargetSymbolId} dogmaType={dogma.Type} intensity={dogma.Intensity}"));
        }
        else
        {
            GD.Print(FormattableString.Invariant($"event=oracle_unreadable year={year} count={glyphs.Count}"));
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

    // 吹き出しの文: 掟の 1 行、人が字をどう読んだか（字ごと）、行き先
    private static string ReadingText(ScriptReading reading)
    {
        var culture = CultureInfo.CurrentCulture;
        var lines = new List<string>();
        if (reading.Teaching is { } teaching)
        {
            lines.Add(string.Format(culture, Tr($"ORACLE_LINE_{teaching.Type.ToString().ToUpperInvariant()}"), Tr($"SYMBOL_{teaching.TargetSymbolId}")));
        }
        var glyphs = reading.Glyphs.Select(glyph => glyph.IsReadable
            ? Tr($"SYMBOL_{glyph.SymbolId}")
                + (glyph.Type is { } type ? "・" + Tr($"NOTEBOOK_TYPE_{type.ToString().ToUpperInvariant()}") : string.Empty)
                + (glyph.Direction != TravelDirection.None ? "・" + Tr($"DIRECTION_{glyph.Direction.ToString().ToUpperInvariant()}") : string.Empty)
            : Tr("ORACLE_READ_UNREADABLE"));
        lines.Add(string.Format(culture, Tr("ORACLE_READ"), string.Join(" ／ ", glyphs)));
        if (reading.Destination != TravelDirection.None)
        {
            lines.Add(Tr(reading.Destination == TravelDirection.East ? "ORACLE_DESTINATION_EAST" : "ORACLE_DESTINATION_WEST"));
        }
        return string.Join('\n', lines);
    }

    // 予言者が祈った。まず定型の祈り（困りごとのひな形に知っている名詞を入れた文）を出し、LLM が使えれば、できた文に差し替える
    private void OnPrayed(Prayer prayer)
    {
        ShowPrayerGlyphs(prayer);
        var words = prayer.NounIds.Select(id => Tr($"WORD_{id}")).ToList();
        var troubleText = prayer.Request is null ? null : Tr($"TROUBLE_{prayer.Request.Id}");
        var fallback = FallbackPrayer(prayer, words);
        var year = _timeControl.World.Year;
        if (prayer.Request is null)
        {
            GD.Print(FormattableString.Invariant($"event=prophet_whispered year={year} count={words.Count}"));
            if (!_listening)
            {
                ShowText(fallback);
            }
        }
        else
        {
            _dreams.PrayerText = fallback;
        }
        if (_rules.Enabled)
        {
            _ = PrayAsync(prayer, troubleText, words, fallback);
        }
    }

    private async Task PrayAsync(Prayer prayer, string? troubleText, List<string> words, string fallback)
    {
        var body = PrayerWriter.BuildRequest(_rules, troubleText, words, TranslationServer.GetLocale());
        var (reply, failure, durationMs) = await _client.PostAsync(body);
        var text = reply is null ? null : PrayerWriter.ParseResponse(reply, words);
        if (text is null)
        {
            GD.PushWarning(FormattableString.Invariant(
                $"event=oracle_ai_failed year={_timeControl.World.Year} reason={(reply is null ? failure : "invalid_response")} durationMs={durationMs}"));
            return;
        }
        if (prayer.Request is not null)
        {
            // 依頼がまだ続いていて、定型の祈りのままのときだけ差し替える
            if (_prophet.ActiveRequest?.Id == prayer.Request.Id && _dreams.PrayerText == fallback)
            {
                _dreams.PrayerText = text;
                _dreams.Refresh();
            }
        }
        else if (!_listening)
        {
            ShowText(text);
        }
    }

    /// <summary>依頼が済んだら、祈りの字を隠す（時間が進んだ後に呼ぶ）。</summary>
    public void HidePrayerGlyphsIfAnswered()
    {
        if (_prophet.ActiveRequest is null && _prayerHideInSeconds <= 0)
        {
            _prayerPanel.Visible = false;
        }
    }

    private double _prayerHideInSeconds;

    // 祈りの字（人の字）を並べ、その部品を手帳に載せる。日々の祈りは、吹き出しと同じ秒数だけ出す
    private void ShowPrayerGlyphs(Prayer prayer)
    {
        if (_prophet.Script is not { } script || _prophet.Dialect is not { } dialect)
        {
            return;
        }
        var symbols = prayer.NounIds.Select(id => dialect.Rules.FindNoun(id)?.SymbolId).OfType<string>().ToList();
        var glyphs = ScriptReader.PrayerGlyphs(script.Script, prayer.Request, symbols);
        foreach (var child in _prayerGlyphs.GetChildren())
        {
            _prayerGlyphs.RemoveChild(child);
            child.QueueFree();
        }
        var added = 0;
        foreach (var glyph in glyphs)
        {
            var view = new GlyphView { CustomMinimumSize = new Vector2(32, 32), MouseFilter = Control.MouseFilterEnum.Ignore };
            view.Show(script.Script, glyph, script.Decayed);
            _prayerGlyphs.AddChild(view);
            added += script.Discover(glyph);
        }
        _prayerPanel.Visible = glyphs.Count > 0;
        _prayerHideInSeconds = prayer.Request is null ? _rules.BubbleSeconds : 0;
        if (added > 0)
        {
            GD.Print(FormattableString.Invariant($"event=glyph_discovered count={added}"));
        }
    }

    // 困りごとごとのひな形に、祈りに入れる名詞を入れた文（名詞が 2 つ未満でも書式が足りるよう、最後の語を繰り返して 3 つにそろえる）
    private static string FallbackPrayer(Prayer prayer, List<string> words)
    {
        object[] args = [words[0], words.Count > 1 ? words[1] : words[0], words.Count > 2 ? words[2] : words[^1]];
        return string.Format(CultureInfo.CurrentCulture, Tr(prayer.Request is null ? "PRAYER_DAILY" : $"PRAYER_{prayer.Request.Id}"), args);
    }

    // 神の言葉を受け取った（LLM が使えるとき）
    private void OnSpeakRequested(string text)
    {
        if (_listening || _prophet.Dialect is null)
        {
            return;
        }
        var year = _timeControl.World.Year;
        var rejected = !OracleListening.IsValidGodText(text) ? "invalid_text" : _prophet.SpeakRejection();
        if (rejected is not null)
        {
            GD.PushWarning(FormattableString.Invariant($"event=oracle_speak_rejected year={year} reason={rejected} textLength={text.Length}"));
            return;
        }
        _ = ListenAsync(text);
    }

    private async Task ListenAsync(string text)
    {
        var year = _timeControl.World.Year;
        GD.Print(FormattableString.Invariant($"event=oracle_speak_received year={year} textLength={text.Length}"));
        _listening = true;
        // 聞いている間は自動進行を止める（祈りで止めるのと同じ）。語りかけで止めたときだけ、終わったら再開する
        var pausedHere = _timeControl.CanDo(TimeControlAction.Pause);
        if (pausedHere)
        {
            _timeControl.Pause();
        }
        _dreams.Listening = true;
        ShowText(Tr("ORACLE_GUIDE_LISTENING"), keepUntilReplaced: true);
        StateChanged?.Invoke();

        var (hearing, reason, durationMs) = await HearAsync(text);
        _listening = false;
        _dreams.Listening = false;
        if (hearing is null)
        {
            GD.PushWarning(FormattableString.Invariant($"event=oracle_hear_failed year={year} reason={reason} durationMs={durationMs}"));
            // 書いた文は残したまま、言葉を選ぶ形に替えて選ばせる
            _dreams.ChoiceMode = true;
            ShowText(Tr("ORACLE_HEAR_FAILED"));
            ResumeIfPausedHere(pausedHere);
            StateChanged?.Invoke();
            return;
        }

        var announced = Announce(hearing, text);
        ResumeIfPausedHere(pausedHere);
        if (announced is not null)
        {
            _dreams.InputText = string.Empty;
        }
        _dreams.Refresh();
        StateChanged?.Invoke();
        if (announced is not null)
        {
            Spoken?.Invoke();
            await NarrateAsync(announced, hearing, text);
        }
    }

    // 自動進行を語りかけで止めていて、まだ止まったままなら再開する（神託を下して、依頼の分とあわせて再開済みなら何もしない）
    private void ResumeIfPausedHere(bool pausedHere)
    {
        if (pausedHere && _timeControl.CanDo(TimeControlAction.StartAutoPlay))
        {
            _timeControl.StartAutoPlay();
            GD.Print(FormattableString.Invariant($"event=auto_play_resumed year={_timeControl.World.Year} reason=oracle_heard"));
        }
    }

    // LLM に聞き取りを頼み、コアで方言に聞き違える。失敗の理由は、つながらない・時間切れ・HTTP のエラー・形の崩れ・答えが一覧に合わない
    private async Task<(Hearing? Hearing, string Reason, long DurationMs)> HearAsync(string text)
    {
        var dialect = _prophet.Dialect!;
        var known = _prophet.Hand.Select(card => _prophet.SymbolOf(card)).ToHashSet();
        static ListeningWord Word(string id) => new(id, Tr($"WORD_{id}"));
        var body = OracleListening.BuildRequest(
            _rules, text, [.. dialect.Rules.Nouns.Select(noun => Word(noun.Id))], [.. dialect.Rules.Verbs.Select(verb => Word(verb.Id))],
            [.. dialect.Rules.Nouns.Where(noun => known.Contains(noun.SymbolId)).Select(noun => Word(noun.Id))], TranslationServer.GetLocale());
        var (reply, failure, durationMs) = await _client.PostAsync(body);
        if (reply is null)
        {
            return (null, failure, durationMs);
        }
        var answer = OracleListening.ParseResponse(reply);
        if (answer is null)
        {
            return (null, "invalid_response", durationMs);
        }
        var outcome = _prophet.Hear(answer);
        return (outcome.Hearing, outcome.RejectedReason ?? string.Empty, durationMs);
    }

    // 言葉を選んで語りかけた（LLM が使えないとき・失敗したとき）
    private void OnWordsChosen(string? nounId, string? verbId, HeardDestination destination, string sentence)
    {
        if (_listening || _prophet.Dialect is null)
        {
            return;
        }
        var year = _timeControl.World.Year;
        if (_prophet.SpeakRejection() is { } rejected)
        {
            GD.PushWarning(FormattableString.Invariant($"event=oracle_speak_rejected year={year} reason={rejected} textLength={sentence.Length}"));
            return;
        }
        var outcome = _prophet.Hear(new HeardAnswer(nounId, verbId, destination, null, []));
        if (outcome.Hearing is not { } hearing)
        {
            GD.PushWarning(FormattableString.Invariant($"event=oracle_speak_rejected year={year} reason={outcome.RejectedReason} textLength={sentence.Length}"));
            return;
        }
        var announced = Announce(hearing, sentence);
        // 失敗の後に選ばせたときは、次の語りかけから LLM に戻す
        _dreams.ChoiceMode = !_rules.Enabled;
        _dreams.Refresh();
        StateChanged?.Invoke();
        if (announced is not null)
        {
            Spoken?.Invoke();
            _ = NarrateAsync(announced, hearing, sentence);
        }
    }

    // コアで神託を下し、掟の 1 行と聞こえた言葉を吹き出しに出す。下せなければ null
    private OracleRecord? Announce(Hearing hearing, string godText)
    {
        var year = _timeControl.World.Year;
        var spoken = _commands.SubmitSpeak(hearing, godText);
        if (spoken.Result is not { } result)
        {
            GD.PushWarning(FormattableString.Invariant($"event=oracle_speak_rejected year={year} reason={spoken.RejectedReason} textLength={godText.Length}"));
            return null;
        }
        if (result.Dogma is { } dogma)
        {
            GD.Print(FormattableString.Invariant(
                $"event=dogma_formed year={year} symbolId={dogma.TargetSymbolId} dogmaType={dogma.Type} intensity={dogma.Intensity}"));
        }
        if (result.AnsweredRequest is { } request)
        {
            GD.Print(FormattableString.Invariant($"event=prayer_answered year={year} requestType={request.Id}"));
        }
        if (result.CorruptedCount > 0)
        {
            GD.Print(FormattableString.Invariant($"event=oracle_corrupted year={year} count={result.CorruptedCount} chaosLevel={_timeControl.World.ChaosLevel}"));
        }
        if (result.OverloadCount > 0)
        {
            GD.Print(FormattableString.Invariant($"event=oracle_overload year={year} count={result.OverloadCount} chaosLevel={_timeControl.World.ChaosLevel}"));
        }
        if (result.ResumedAutoPlay)
        {
            GD.Print(FormattableString.Invariant($"event=auto_play_resumed year={year} reason=oracle_pronounced"));
        }
        _latestSpeech = result.Record;
        ShowText(SpeechText(result.Record, hearing, narration: null));
        return result.Record;
    }

    // LLM に聞こえた文と語りを頼む。できたら記録に添え、いちばん新しい神託なら吹き出しを差し替える。失敗したら規則の文のまま
    private async Task NarrateAsync(OracleRecord record, Hearing hearing, string godText)
    {
        if (!_rules.Enabled || (hearing.HeardNounId is null && hearing.HeardVerbId is null))
        {
            return;
        }
        static string? Word(string? id) => id is null ? null : Tr($"WORD_{id}");
        var verb = Word(hearing.HeardVerbId);
        var required = new[] { Word(hearing.HeardNounId), verb is null ? null : OracleNarrationWriter.VerbStem(verb) }.OfType<string>().ToList();
        var body = OracleNarrationWriter.BuildRequest(
            _rules, Word(hearing.HeardNounId), Word(hearing.HeardVerbId), _timeControl.World, TranslationServer.GetLocale());
        var (reply, failure, durationMs) = await _client.PostAsync(body);
        var narration = reply is null ? null : OracleNarrationWriter.ParseResponse(reply, required);
        if (narration is null)
        {
            GD.PushWarning(FormattableString.Invariant(
                $"event=oracle_ai_failed year={record.Year} reason={(reply is null ? failure : "invalid_response")} durationMs={durationMs}"));
            return;
        }
        GD.Print(FormattableString.Invariant($"event=oracle_generated year={record.Year} durationMs={durationMs}"));
        if (_prophet.Dialect!.AttachNarration(record.Year, record.GodText, narration.Heard, narration.Narration))
        {
            if (ReferenceEquals(_latestSpeech, record))
            {
                ShowText(SpeechText(record, hearing, narration));
            }
            _dreams.Refresh();
            Spoken?.Invoke();
        }
    }

    // 吹き出しの文: 掟の 1 行、聞こえた言葉（LLM の聞こえた文があればその文と語り）、行き先、謎の言葉
    private static string SpeechText(OracleRecord record, Hearing hearing, NarrationText? narration)
    {
        var culture = CultureInfo.CurrentCulture;
        var lines = new List<string>();
        if (record.Teaching is { } teaching)
        {
            lines.Add(string.Format(
                culture, Tr($"ORACLE_LINE_{teaching.Type.ToString().ToUpperInvariant()}"), Tr($"SYMBOL_{teaching.TargetSymbolId}")));
        }
        if (narration is not null)
        {
            lines.Add(narration.Heard);
            lines.Add(narration.Narration);
        }
        else if (hearing.HeardNounId is not null || hearing.HeardVerbId is not null)
        {
            lines.Add(string.Format(
                culture, Tr("ORACLE_HEARD_RULE"),
                hearing.HeardNounId is null ? "-" : Tr($"WORD_{hearing.HeardNounId}"),
                hearing.HeardVerbId is null ? "-" : Tr($"WORD_{hearing.HeardVerbId}")));
        }
        if (hearing.Destination != TravelDirection.None)
        {
            lines.Add(Tr(hearing.Destination == TravelDirection.East ? "ORACLE_DESTINATION_EAST" : "ORACLE_DESTINATION_WEST"));
        }
        else if (hearing.DestinationMisheard)
        {
            lines.Add(Tr("ORACLE_DESTINATION_LOST"));
        }
        if (hearing.MysteryWords.Count > 0)
        {
            lines.Add(string.Format(culture, Tr("ORACLE_MYSTERY"), string.Join("、", hearing.MysteryWords)));
        }
        return string.Join('\n', lines);
    }

    private void ShowText(string text, bool keepUntilReplaced = false)
    {
        _label.Text = text;
        _bubble.Visible = true;
        _hideInSeconds = keepUntilReplaced ? 0 : _rules.BubbleSeconds;
    }

    private static string Tr(string key) => TranslationServer.Translate(key).ToString();
}
