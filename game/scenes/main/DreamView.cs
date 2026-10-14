using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Godot;
using TheogonyX.Core;
using TheogonyX.Core.Protocol;

namespace TheogonyX.Scenes.Main;

/// <summary>
/// 神託の欄と依頼の吹き出しを描き、神託を下す操作をコアの <see cref="ProphetDreams"/> につなぐ
/// （docs/domain/oracle-pair.md、prophet-request.md）。
/// 地表の下の帯の 1 段目に今の状態の案内と神威、2 段目に「神託を下す」、3 段目に手札を出す。
/// 手札のカードを 1 枚目・2 枚目の順に押して 2 枚を選び、「神託を下す」で神託にする。
/// スマホ（<see cref="CompanionHost"/>）からの神託も、ここを通す。
/// </summary>
public sealed class DreamView
{
    private readonly ProphetDreams _prophet;
    private readonly OracleCommandQueue _commands;
    private readonly TimeControl _timeControl;
    private readonly Label _dreamGuide;
    private readonly HBoxContainer _dreamRow;
    private readonly Button _oracleButton;
    private readonly HBoxContainer _dreamCards;
    private readonly Label _requestBubble;
    private readonly HFlowContainer _pruneRow;

    // 選んだ手札の番号（1 枚目・2 枚目の順）。
    private readonly List<int> _selected = [];
    private string[] _handShown = [];

    /// <param name="owner">ノードを探すためのシーンの根。</param>
    public DreamView(Node owner, ProphetDreams prophet, OracleCommandQueue commands, TimeControl timeControl)
    {
        _prophet = prophet;
        _commands = commands;
        _timeControl = timeControl;
        _dreamGuide = owner.GetNode<Label>("%DreamGuide");
        _dreamRow = owner.GetNode<HBoxContainer>("%DreamRow");
        _oracleButton = owner.GetNode<Button>("%OracleButton");
        _dreamCards = owner.GetNode<HBoxContainer>("%DreamCards");
        _requestBubble = owner.GetNode<Label>("%RequestBubble");

        // 夢のコマは無い（神託は手札のルーンの組で作る）。コマの段には「神託を下す」だけを残す。
        owner.GetNode<HBoxContainer>("%DreamNodes").Visible = false;
        _dreamRow.Visible = true;
        _dreamCards.Visible = true;
        _oracleButton.Pressed += OnOracleButtonPressed;

        // 精査（世界再創生の後に、ルーンを削除する）の欄。持ち分があるときだけ出す。シーンは変えず、手札の下に足す
        // 左の帯は幅が狭い（264px）ので、ボタンは折り返す
        _pruneRow = new HFlowContainer { Visible = false };
        _dreamCards.GetParent().AddChild(_pruneRow);
        _dreamCards.GetParent().MoveChild(_pruneRow, _dreamCards.GetIndex() + 1);

        Redraw();
    }

    /// <summary>神託を下したとき。HUD のドグマの一覧と、時間のボタンを描き直すのに使う。</summary>
    public event Action? OraclePronounced;

    /// <summary>精査でルーンを削除したとき。保存に使う。</summary>
    public event Action? DeckPruned;

    /// <summary>描き直したとき。手札・神威・依頼・できる操作が変わったかもしれない。</summary>
    public event Action? Changed;

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
        Redraw();
    }

    /// <summary>世界再創生の後に呼ぶ。</summary>
    public void OnWorldReset()
    {
        _prophet.Reset();
        _selected.Clear();
        Redraw();
    }

    /// <summary>手札の <paramref name="first"/> 番・<paramref name="second"/> 番のルーンで神託を下す。スマホからも呼ぶ。</summary>
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
        _selected.Clear();
        Redraw();
        OraclePronounced?.Invoke();
        return null;
    }

    /// <summary>デッキのルーンを 1 枚削除する（精査）。</summary>
    private void TryPrune(string cardId)
    {
        if (_prophet.PruneRejection(cardId) is { } reason)
        {
            GD.Print(FormattableString.Invariant(
                $"event=deck_prune_rejected year={_timeControl.World.Year} runeId={cardId} reason={reason}"));
            return;
        }

        _prophet.Prune(cardId);
        GD.Print(FormattableString.Invariant(
            $"event=deck_pruned year={_timeControl.World.Year} runeId={cardId}"));
        Redraw();
        DeckPruned?.Invoke();
    }

    // 精査の欄を作り直す。デッキのルーンを種類ごとにまとめて、ボタンにする
    private void RedrawPrune()
    {
        foreach (var child in _pruneRow.GetChildren())
        {
            _pruneRow.RemoveChild(child);
            child.QueueFree();
        }

        _pruneRow.Visible = _prophet.PruneCharges > 0;
        if (!_pruneRow.Visible)
        {
            return;
        }

        _pruneRow.AddChild(new Label
        {
            Text = string.Format(CultureInfo.CurrentCulture, Tr("PRUNE_LABEL"), _prophet.PruneCharges),
        });
        var cards = _prophet.Deck.DrawPile.Concat(_prophet.Hand).Concat(_prophet.Deck.DiscardPile);
        foreach (var group in cards.GroupBy(id => id))
        {
            var cardId = group.Key;
            var button = new Button
            {
                Text = string.Format(CultureInfo.CurrentCulture, Tr("PRUNE_CARD"), Tr($"SYMBOL_{cardId}"), group.Count()),
            };
            button.Pressed += () => TryPrune(cardId);
            _pruneRow.AddChild(button);
        }
    }

    private void OnOracleButtonPressed()
    {
        if (_selected.Count == 2)
        {
            TryPronouncePair(_selected[0], _selected[1]);
        }
    }

    // 同じカードをもう一度押すと選びを外す。3 枚目を押したら、1 枚目を外して 2 枚目と 3 枚目にする
    private void OnCardPressed(int index)
    {
        if (!_selected.Remove(index))
        {
            if (_selected.Count == 2)
            {
                _selected.RemoveAt(0);
            }
            _selected.Add(index);
        }
        Redraw();
    }

    // 手札のカードのボタンを作り直す。手札は神託を下すたびに変わる
    private void RebuildCards()
    {
        foreach (var child in _dreamCards.GetChildren())
        {
            _dreamCards.RemoveChild(child);
            child.QueueFree();
        }
        for (var i = 0; i < _prophet.Hand.Count; i++)
        {
            var card = new Button();
            var index = i;
            card.Pressed += () => OnCardPressed(index);
            _dreamCards.AddChild(card);
        }
        _handShown = [.. _prophet.Hand];
    }

    private void Redraw()
    {
        if (!_prophet.Hand.SequenceEqual(_handShown))
        {
            _selected.Clear();
            RebuildCards();
        }
        _selected.RemoveAll(index => index >= _prophet.Hand.Count);

        var buttons = _dreamCards.GetChildren().OfType<Button>().ToList();
        for (var i = 0; i < buttons.Count && i < _prophet.Hand.Count; i++)
        {
            // 選んだ順に「①」「②」を付ける
            var order = _selected.IndexOf(i);
            var mark = order == 0 ? "① " : order == 1 ? "② " : string.Empty;
            buttons[i].Text = mark + Tr($"SYMBOL_{_prophet.Hand[i]}");
            buttons[i].Disabled = _timeControl.World.IsDestroyed;
        }

        _oracleButton.Disabled = !(_selected.Count == 2 && _prophet.CanPronounce);
        _dreamGuide.Text = GuideText();
        RedrawPrune();

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

        Changed?.Invoke();
    }

    // 今の状態の案内と、神威（今の量 / 上限）
    private string GuideText()
    {
        if (_timeControl.World.IsDestroyed)
        {
            return Tr("ORACLE_GUIDE_DESTROYED");
        }

        var faith = _prophet.Faith;
        var key = _prophet.Hand.Count < 2 ? "ORACLE_GUIDE_NO_CARDS"
            : !faith.CanAfford ? "ORACLE_GUIDE_NO_FAITH"
            : _selected.Count == 2 ? "ORACLE_GUIDE_READY"
            : "ORACLE_GUIDE_CHOOSE";
        var guide = key == "ORACLE_GUIDE_READY" && PreviewText() is { } preview ? preview : Tr(key);
        return string.Format(
            CultureInfo.CurrentCulture, Tr("ORACLE_GUIDE_FAITH"), guide, faith.Value, faith.Max);
    }

    // 選んだ 2 枚の五行の関係と種別の予告（例: 「聖なる火 ← 木：相生 → 崇拝（陽）」）。予告できなければ null
    private string? PreviewText()
    {
        if (_prophet.PreviewPair(_selected[0], _selected[1]) is not { } judgement)
        {
            return null;
        }
        var relation = judgement.Relation is { } r ? $"WUXING_{r.ToString().ToUpperInvariant()}" : "WUXING_IMPURE";
        return string.Format(
            CultureInfo.CurrentCulture,
            Tr("ORACLE_PREVIEW"),
            Tr($"SYMBOL_{_prophet.Hand[_selected[0]]}"),
            Tr($"SYMBOL_{_prophet.Hand[_selected[1]]}"),
            Tr(relation),
            Tr($"DOGMA_TYPE_{judgement.Type.ToString().ToUpperInvariant()}"));
    }

    private static string Tr(string key) => TranslationServer.Translate(key).ToString();
}
