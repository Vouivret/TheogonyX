using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Godot;
using TheogonyX.Core;

namespace TheogonyX.Scenes.Main;

/// <summary>
/// 予言者の夢の欄と依頼の吹き出しを描き、改ざん・神託の操作をコアの <see cref="ProphetDreams"/> につなぐ
/// （docs/domain/dream-tampering.md、prophet-request.md）。
/// 地表の下の帯の 1 段目に今の状態の案内、2 段目に夢のコマと「神託を下す」を出す。
/// コマを選んでいるあいだは、2 段目を手札と「やめる」に替える。
/// </summary>
public sealed class DreamView
{
    private readonly ProphetDreams _prophet;
    private readonly TimeControl _timeControl;
    private readonly Label _dreamGuide;
    private readonly HBoxContainer _dreamRow;
    private readonly HBoxContainer _dreamNodes;
    private readonly Button _oracleButton;
    private readonly HBoxContainer _dreamCards;
    private readonly Label _requestBubble;

    // 夢のコマのボタン。作り直すと、押してから離すまでのあいだに時間が進んだときにクリックが消えるので、使い回す。
    private readonly List<Button> _nodeButtons = [];

    // 改ざんするために選んだ夢ノードの位置。選んでいなければ -1。
    private int _selectedIndex = -1;

    /// <param name="owner">ノードを探すためのシーンの根。</param>
    public DreamView(Node owner, ProphetDreams prophet, TimeControl timeControl, DreamRules dreamRules)
    {
        _prophet = prophet;
        _timeControl = timeControl;
        _dreamGuide = owner.GetNode<Label>("%DreamGuide");
        _dreamRow = owner.GetNode<HBoxContainer>("%DreamRow");
        _dreamNodes = owner.GetNode<HBoxContainer>("%DreamNodes");
        _oracleButton = owner.GetNode<Button>("%OracleButton");
        _dreamCards = owner.GetNode<HBoxContainer>("%DreamCards");
        _requestBubble = owner.GetNode<Label>("%RequestBubble");

        // 手札は使う記号すべて。使ってもなくならない。
        foreach (var symbol in dreamRules.Symbols)
        {
            var card = new Button { Text = Tr($"SYMBOL_{symbol.Id}") };
            var symbolId = symbol.Id;
            card.Pressed += () => OnCardChosen(symbolId);
            _dreamCards.AddChild(card);
        }
        var cancel = new Button { Text = Tr("BUTTON_CANCEL") };
        cancel.Pressed += () => OnNodePressed(_selectedIndex);
        _dreamCards.AddChild(cancel);
        _oracleButton.Pressed += OnOraclePressed;

        LogDream("dream_created");
        Redraw();
    }

    /// <summary>神託を下したとき。HUD のドグマの一覧と、時間のボタンを描き直すのに使う。</summary>
    public event Action? OraclePronounced;

    /// <summary>100 年跳躍の後と、自動進行の 1 年ごとに呼ぶ。</summary>
    public void OnTimeAdvanced()
    {
        var update = _prophet.OnTimeAdvanced();
        var year = _timeControl.World.Year;
        if (update.NewRequest is { } request)
        {
            GD.Print(FormattableString.Invariant($"event=prophet_prayed year={year} requestType={request.Id}"));
        }
        if (update.HasNewDream)
        {
            LogDream("dream_created");
        }
        if (update.PausedAutoPlay)
        {
            GD.Print(FormattableString.Invariant($"event=auto_play_paused year={year} reason=prophet_dream"));
        }
        Redraw();
    }

    /// <summary>世界再創生の後に呼ぶ。</summary>
    public void OnWorldReset()
    {
        _prophet.Reset();
        _selectedIndex = -1;
        LogDream("dream_created");
        Redraw();
    }

    // 同じコマをもう一度押すか「やめる」で、選ぶのをやめる
    private void OnNodePressed(int index)
    {
        if (!_prophet.CanTamper)
        {
            return;
        }
        _selectedIndex = _selectedIndex == index ? -1 : index;
        Redraw();
    }

    private void OnCardChosen(string symbolId)
    {
        if (_selectedIndex < 0 || !_prophet.CanTamper)
        {
            return;
        }
        _prophet.Tamper(_selectedIndex, symbolId);
        _selectedIndex = -1;
        LogDream("dream_tampered");
        Redraw();
    }

    private void OnOraclePressed()
    {
        if (!_prophet.CanPronounce)
        {
            return;
        }

        var year = _timeControl.World.Year;
        var result = _prophet.Pronounce();
        if (result.Dogma is { } dogma)
        {
            GD.Print(FormattableString.Invariant(
                $"event=dogma_formed year={year} symbolId={dogma.TargetSymbolId} dogmaType={dogma.Type} intensity={dogma.Intensity}"));
        }
        if (result.AnsweredRequest is { } request)
        {
            GD.Print(FormattableString.Invariant($"event=prayer_answered year={year} requestType={request.Id}"));
        }
        if (result.ResumedAutoPlay)
        {
            GD.Print(FormattableString.Invariant($"event=auto_play_resumed year={year} reason=oracle_pronounced"));
        }
        _selectedIndex = -1;
        Redraw();
        OraclePronounced?.Invoke();
    }

    private void Redraw()
    {
        var canTamper = _prophet.CanTamper;
        if (!canTamper)
        {
            _selectedIndex = -1;
        }

        while (_nodeButtons.Count < _prophet.Dream.Count)
        {
            var index = _nodeButtons.Count;
            var button = new Button();
            button.Pressed += () => OnNodePressed(index);
            _dreamNodes.AddChild(button);
            _nodeButtons.Add(button);
        }
        for (var i = 0; i < _nodeButtons.Count; i++)
        {
            var button = _nodeButtons[i];
            button.Visible = i < _prophet.Dream.Count;
            if (!button.Visible)
            {
                continue;
            }
            var node = _prophet.Dream[i];
            button.Text = Tr($"SYMBOL_{node.SymbolId}");
            button.TooltipText = string.Format(CultureInfo.CurrentCulture, Tr("DREAM_NODE_TOOLTIP"), node.Weight);
            button.Disabled = !canTamper;
        }

        var isChoosing = _selectedIndex >= 0;
        _dreamRow.Visible = !isChoosing;
        _dreamCards.Visible = isChoosing;
        _oracleButton.Disabled = !_prophet.CanPronounce;
        _dreamGuide.Text = Tr(GuideKey(isChoosing));

        if (_prophet.ActiveRequest is { } request)
        {
            _requestBubble.Text = string.Format(
                CultureInfo.CurrentCulture, Tr("REQUEST_BUBBLE"), Tr($"TROUBLE_{request.Id}"), Tr($"SYMBOL_{request.SymbolId}"));
            _requestBubble.Visible = true;
        }
        else
        {
            _requestBubble.Visible = false;
        }
    }

    // 今の状態の案内の文言のキー
    private string GuideKey(bool isChoosing)
    {
        if (_timeControl.World.IsDestroyed)
        {
            return "DREAM_GUIDE_DESTROYED";
        }
        if (isChoosing)
        {
            return "DREAM_GUIDE_CHOOSE_CARD";
        }
        return _prophet.State switch
        {
            ProphetDreamState.Untampered => _timeControl.State == TimeControlState.AutoPlaying
                ? "DREAM_GUIDE_UNTAMPERED"
                : "DREAM_GUIDE_UNTAMPERED_PAUSED",
            ProphetDreamState.Tampered => "DREAM_GUIDE_TAMPERED",
            _ => "DREAM_NONE",
        };
    }

    private void LogDream(string eventName)
    {
        if (_prophet.Dream.Count == 0)
        {
            return;
        }
        var nodes = string.Join(",", _prophet.Dream.Select(node => FormattableString.Invariant($"{node.SymbolId}:{node.Weight:F2}")));
        GD.Print($"event={eventName} year={_timeControl.World.Year} dreamNodes={nodes}");
    }

    private static string Tr(string key) => TranslationServer.Translate(key).ToString();
}
