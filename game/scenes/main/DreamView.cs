using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Godot;
using TheogonyX.Core;
using TheogonyX.Core.Protocol;

namespace TheogonyX.Scenes.Main;

/// <summary>
/// 神託の欄と依頼の吹き出しを描く（docs/domain/oracle-pair.md の「画面と通信」、prophet-request.md）。
/// 地表の下の帯の 1 段目に今の状態の案内と神威、2 段目に「語りかける」ボタンと、神の言葉を書く入力欄を出す。
/// 手札（予言者の語彙）は見せない。LLM が使えないときは、入力欄の代わりに、予言者の知っている名詞と動詞と行き先を選ぶ欄を出す。
/// 語りかける操作は <see cref="SpeakRequested"/>・<see cref="WordsChosen"/> で知らせ、聞き取りと実行は <see cref="OracleView"/> が受け持つ。
/// スマホ（<see cref="CompanionHost"/>）からの旧い形の神託（手札の 2 枚）は、小段 6 でスマホを替えるまで、ここを通す。
/// </summary>
public sealed class DreamView
{
    private readonly ProphetDreams _prophet;
    private readonly OracleCommandQueue _commands;
    private readonly TimeControl _timeControl;
    private readonly Label _dreamGuide;
    private readonly Button _oracleButton;
    private readonly HBoxContainer _dreamCards;
    private readonly Label _requestBubble;
    private readonly LineEdit _input;
    private readonly OptionButton _nounChoice;
    private readonly OptionButton _verbChoice;
    private readonly OptionButton _directionChoice;
    private readonly List<string?> _nounIds = [];
    private string _handShown = string.Empty;
    private bool _listening;
    private bool _choiceMode;

    /// <param name="owner">ノードを探すためのシーンの根。</param>
    public DreamView(Node owner, ProphetDreams prophet, OracleCommandQueue commands, TimeControl timeControl)
    {
        _prophet = prophet;
        _commands = commands;
        _timeControl = timeControl;
        _dreamGuide = owner.GetNode<Label>("%DreamGuide");
        _oracleButton = owner.GetNode<Button>("%OracleButton");
        _dreamCards = owner.GetNode<HBoxContainer>("%DreamCards");
        _requestBubble = owner.GetNode<Label>("%RequestBubble");

        // 夢のコマは無い。コマの段は使わず、下の帯の 1 行（入力欄・語りかける）に収める
        owner.GetNode<HBoxContainer>("%DreamNodes").Visible = false;
        owner.GetNode<HBoxContainer>("%DreamRow").Visible = false;
        _dreamCards.Visible = true;
        _dreamGuide.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _dreamGuide.CustomMinimumSize = new Vector2(440, 0);

        _input = new LineEdit
        {
            Name = "OracleInput",
            MaxLength = OracleListening.MaxGodTextLength,
            PlaceholderText = Tr("SPEAK_PLACEHOLDER"),
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
        };
        _nounChoice = new OptionButton { Name = "NounChoice", SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, Visible = false };
        _verbChoice = new OptionButton { Name = "VerbChoice", SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, Visible = false };
        _directionChoice = new OptionButton { Name = "DirectionChoice", SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, Visible = false };
        foreach (var control in new Control[] { _input, _nounChoice, _verbChoice, _directionChoice })
        {
            _dreamCards.AddChild(control);
        }
        _oracleButton.Reparent(_dreamCards);
        _oracleButton.Text = "BUTTON_SPEAK";
        _oracleButton.Pressed += OnOracleButtonPressed;
        _input.TextChanged += _ => UpdateControls();
        _input.TextSubmitted += _ => OnOracleButtonPressed();
        foreach (var choice in new[] { _nounChoice, _verbChoice, _directionChoice })
        {
            choice.ItemSelected += _ => UpdateControls();
        }
        BuildVerbAndDirectionChoices();

        Redraw();
    }

    /// <summary>旧い形の神託（スマホの手札の 2 枚）を下したとき。HUD のドグマの一覧と、時間のボタンを描き直すのに使う。</summary>
    public event Action? OraclePronounced;

    /// <summary>直近に下した旧い形の神託の、2 枚目の記号と五行の関係（予言者の語りに使う。保存しない）。</summary>
    public OraclePairing? LastPairing { get; private set; }

    /// <summary>予言者が祈った（新しい依頼の祈りか、日々の祈り）。祈りの文を作って <see cref="PrayerText"/> や吹き出しに出すのに使う。</summary>
    public event Action<Prayer>? Prayed;

    /// <summary>依頼中の吹き出しに出す祈りの文。無ければ（依頼の定型文を出す）null。依頼が済むと消える。</summary>
    public string? PrayerText { get; set; }

    /// <summary>神の言葉を書いて「語りかける」を押した（LLM が使えるとき）。</summary>
    public event Action<string>? SpeakRequested;

    /// <summary>名詞・動詞・行き先を選んで「語りかける」を押した（LLM が使えないとき）。選んだ神の言葉の代わりの文も渡す。</summary>
    public event Action<string?, string?, HeardDestination, string>? WordsChosen;

    /// <summary>描き直したとき。手札・神威・依頼・できる操作が変わったかもしれない。</summary>
    public event Action? Changed;

    /// <summary>予言者が聞いている途中か。入力欄とボタンを使えなくする。</summary>
    public bool Listening
    {
        get => _listening;
        set
        {
            _listening = value;
            Redraw();
        }
    }

    /// <summary>入力欄の代わりに、言葉を選ぶ欄を出すか（LLM が使えないとき）。</summary>
    public bool ChoiceMode
    {
        get => _choiceMode;
        set
        {
            _choiceMode = value;
            Redraw();
        }
    }

    /// <summary>入力欄の文。聞き取りに失敗したときも、書いた文を残す。</summary>
    public string InputText
    {
        get => _input.Text;
        set => _input.Text = value;
    }

    /// <summary>スマホに送る、今の手札・神威・依頼（<c>ORACLE_STATE</c>）。</summary>
    public string StateMessage() => HostMessageWriter.OracleState(_prophet, _timeControl.World.IsDestroyed);

    /// <summary>100 年跳躍の後と、自動進行の 1 年ごとに呼ぶ。</summary>
    public void OnTimeAdvanced()
    {
        var update = _prophet.OnTimeAdvanced();
        var year = _timeControl.World.Year;
        if (update.NewRequest is { } request)
        {
            GD.Print(FormattableString.Invariant($"event=prophet_prayed year={year} requestType={request.Id}"));
        }
        if (update.CorruptedAdded > 0)
        {
            GD.Print(FormattableString.Invariant($"event=deck_corrupted year={year} count={update.CorruptedAdded} chaosLevel={_timeControl.World.ChaosLevel}"));
        }
        if (update.RunesGained > 0)
        {
            GD.Print(FormattableString.Invariant($"event=deck_rune_gained year={year} count={update.RunesGained}"));
        }
        if (update.PausedAutoPlay)
        {
            GD.Print(FormattableString.Invariant($"event=auto_play_paused year={year} reason=prophet_prayer"));
        }
        PrayerText = null;
        if (update.Prayer is { } prayer)
        {
            Prayed?.Invoke(prayer);
        }
        Redraw();
    }

    /// <summary>世界再創生の後に呼ぶ。</summary>
    public void OnWorldReset()
    {
        _prophet.Reset();
        Redraw();
    }

    /// <summary>手札の <paramref name="first"/> 番・<paramref name="second"/> 番のルーンで神託を下す。スマホ（旧い形）から呼ぶ。</summary>
    /// <returns>下せなかったときの理由（ログの <c>reason</c>）。下せたら null。</returns>
    public string? TryPronouncePair(int first, int second)
    {
        var year = _timeControl.World.Year;
        var outcome = _commands.SubmitPronouncePair(first, second);
        if (outcome.Result is not { } result)
        {
            return outcome.RejectedReason ?? "cannot_pronounce";
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
            GD.Print(FormattableString.Invariant(
                $"event=oracle_corrupted year={year} count={result.CorruptedCount} chaosLevel={_timeControl.World.ChaosLevel}"));
        }
        if (result.OverloadCount > 0)
        {
            GD.Print(FormattableString.Invariant(
                $"event=oracle_overload year={year} count={result.OverloadCount} chaosLevel={_timeControl.World.ChaosLevel}"));
        }
        if (result.ResumedAutoPlay)
        {
            GD.Print(FormattableString.Invariant($"event=auto_play_resumed year={year} reason=oracle_pronounced"));
        }
        LastPairing = result.Pairing;
        Redraw();
        OraclePronounced?.Invoke();
        return null;
    }

    /// <summary>語りかけを終えた後（成功しても失敗しても）に、欄を描き直す。</summary>
    public void Refresh() => Redraw();

    private void OnOracleButtonPressed()
    {
        if (_oracleButton.Disabled)
        {
            return;
        }
        if (_choiceMode)
        {
            var noun = _nounChoice.Selected >= 0 && _nounChoice.Selected < _nounIds.Count ? _nounIds[_nounChoice.Selected] : null;
            var verb = _verbChoice.Selected > 0 ? _prophet.Dialect!.Rules.Verbs[_verbChoice.Selected - 1].Id : null;
            var destination = _directionChoice.Selected switch
            {
                1 => HeardDestination.East,
                2 => HeardDestination.West,
                _ => HeardDestination.None,
            };
            WordsChosen?.Invoke(noun, verb, destination, ChoiceSentence(noun, verb, destination));
        }
        else
        {
            SpeakRequested?.Invoke(_input.Text.Trim());
        }
    }

    // 選んだ言葉を並べた、神の文の代わりの文（記録に残す）
    private static string ChoiceSentence(string? noun, string? verb, HeardDestination destination)
    {
        var parts = new List<string>();
        if (noun is not null)
        {
            parts.Add(Tr($"WORD_{noun}"));
        }
        if (verb is not null)
        {
            parts.Add(Tr($"WORD_{verb}"));
        }
        if (destination != HeardDestination.None)
        {
            parts.Add(Tr(destination == HeardDestination.East ? "DIRECTION_EAST" : "DIRECTION_WEST"));
        }
        return string.Join(' ', parts);
    }

    // 動詞は全部知っている。行き先は東・西・なし。名詞は手札の記号のものだけなので、手札が変わるたびに作り直す
    private void BuildVerbAndDirectionChoices()
    {
        _verbChoice.Clear();
        _verbChoice.AddItem(Tr("CHOICE_NONE"));
        foreach (var verb in _prophet.Dialect?.Rules.Verbs ?? [])
        {
            _verbChoice.AddItem(Tr($"WORD_{verb.Id}"));
        }
        _directionChoice.Clear();
        _directionChoice.AddItem(Tr("DIRECTION_NONE"));
        _directionChoice.AddItem(Tr("DIRECTION_EAST"));
        _directionChoice.AddItem(Tr("DIRECTION_WEST"));
    }

    private void RebuildNounChoice()
    {
        var symbols = _prophet.Hand.Select(card => _prophet.SymbolOf(card)).ToHashSet();
        var signature = string.Join(',', symbols.Order());
        if (signature == _handShown)
        {
            return;
        }
        _handShown = signature;
        _nounChoice.Clear();
        _nounIds.Clear();
        _nounChoice.AddItem(Tr("CHOICE_NONE"));
        _nounIds.Add(null);
        foreach (var noun in (_prophet.Dialect?.Rules.Nouns ?? []).Where(noun => symbols.Contains(noun.SymbolId)))
        {
            _nounChoice.AddItem(Tr($"WORD_{noun.Id}"));
            _nounIds.Add(noun.Id);
        }
    }

    private void Redraw()
    {
        RebuildNounChoice();
        _input.Visible = !_choiceMode;
        _nounChoice.Visible = _directionChoice.Visible = _verbChoice.Visible = _choiceMode;
        UpdateControls();
        _dreamGuide.Text = GuideText();

        if (_prophet.ActiveRequest is { } request)
        {
            _requestBubble.Text = PrayerText ?? string.Format(
                CultureInfo.CurrentCulture, Tr("REQUEST_BUBBLE"), Tr($"TROUBLE_{request.Id}"), Tr($"SYMBOL_{request.SymbolId}"));
            _requestBubble.Visible = true;
        }
        else
        {
            PrayerText = null;
            _requestBubble.Visible = false;
        }

        Changed?.Invoke();
    }

    // 入力欄・選ぶ欄とボタンの、使えるかどうか
    private void UpdateControls()
    {
        var canSpeak = !_timeControl.World.IsDestroyed && !_listening && _prophet.Faith.CanAfford && _prophet.Dialect is not null;
        _input.Editable = !_listening;
        foreach (var choice in new[] { _nounChoice, _verbChoice, _directionChoice })
        {
            choice.Disabled = _listening;
        }
        var hasContent = _choiceMode
            ? _nounChoice.Selected > 0 || _verbChoice.Selected > 0 || _directionChoice.Selected > 0
            : OracleListening.IsValidGodText(_input.Text.Trim());
        _oracleButton.Disabled = !(canSpeak && hasContent);
    }

    // 今の状態の案内と、神威（今の量 / 上限）
    private string GuideText()
    {
        if (_timeControl.World.IsDestroyed)
        {
            return Tr("ORACLE_GUIDE_DESTROYED");
        }

        var faith = _prophet.Faith;
        var key = _listening ? "ORACLE_GUIDE_LISTENING"
            : !faith.CanAfford ? "ORACLE_GUIDE_NO_FAITH"
            : _choiceMode ? "ORACLE_GUIDE_CHOOSE_WORDS"
            : "ORACLE_GUIDE_SPEAK";
        return string.Format(CultureInfo.CurrentCulture, Tr("ORACLE_GUIDE_FAITH"), Tr(key), faith.Value, faith.Max);
    }

    private static string Tr(string key) => TranslationServer.Translate(key).ToString();
}
