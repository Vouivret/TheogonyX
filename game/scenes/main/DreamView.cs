using System;
using System.Globalization;
using Godot;
using TheogonyX.Core;
using TheogonyX.Core.Protocol;

namespace TheogonyX.Scenes.Main;

/// <summary>
/// 神託の欄と依頼の吹き出しを描く（docs/domain/script-decipher.md、prophet-request.md）。
/// 地表の下の帯の 1 段目に今の状態の案内と神威、2 段目に「字を組む」ボタンを出す。手札（予言者の語彙）は見せない。
/// 字を組んで下すのは <see cref="ScriptComposerView"/> と <see cref="OracleView"/> が受け持つ。
/// </summary>
public sealed class DreamView
{
    private readonly ProphetDreams _prophet;
    private readonly TimeControl _timeControl;
    private readonly Label _dreamGuide;
    private readonly Label _requestBubble;

    /// <param name="owner">ノードを探すためのシーンの根。</param>
    public DreamView(Node owner, ProphetDreams prophet, TimeControl timeControl)
    {
        _prophet = prophet;
        _timeControl = timeControl;
        _dreamGuide = owner.GetNode<Label>("%DreamGuide");
        _requestBubble = owner.GetNode<Label>("%RequestBubble");
        var dreamCards = owner.GetNode<HBoxContainer>("%DreamCards");

        // 夢のコマは無い。コマの段は使わず、下の帯の 1 行（字を組む）に収める
        owner.GetNode<HBoxContainer>("%DreamNodes").Visible = false;
        owner.GetNode<HBoxContainer>("%DreamRow").Visible = false;
        owner.GetNode<Button>("%OracleButton").Visible = false;
        dreamCards.Visible = true;
        _dreamGuide.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _dreamGuide.CustomMinimumSize = new Vector2(440, 0);

        var composeButton = new Button { Name = "ComposeButton", Text = "BUTTON_COMPOSE", SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        composeButton.Pressed += () => ComposeRequested?.Invoke();
        dreamCards.AddChild(composeButton);

        Redraw();
    }

    /// <summary>予言者が祈った（新しい依頼の祈りか、日々の祈り）。祈りの字を出すのに使う。</summary>
    public event Action<Prayer>? Prayed;

    /// <summary>描き直したとき。手札・神威・依頼が変わったかもしれない。</summary>
    public event Action? Changed;

    /// <summary>「字を組む」を押した。</summary>
    public event Action? ComposeRequested;

    /// <summary>スマホに送る、今の神託の状態（<c>ORACLE_STATE</c>）。</summary>
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
        if (update.ScriptDecayed)
        {
            GD.Print(FormattableString.Invariant($"event=script_decayed year={year}"));
        }
        if (update.SteleMisreadings > 0)
        {
            GD.Print(FormattableString.Invariant($"event=stele_misread year={year} count={update.SteleMisreadings}"));
        }
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

    /// <summary>神託を下した後に、欄を描き直す。</summary>
    public void Refresh() => Redraw();

    private void Redraw()
    {
        _dreamGuide.Text = GuideText();
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
        var key = faith.CanAfford ? "ORACLE_GUIDE_COMPOSE" : "ORACLE_GUIDE_NO_FAITH";
        return string.Format(CultureInfo.CurrentCulture, Tr("ORACLE_GUIDE_FAITH"), Tr(key), faith.Value, faith.Max);
    }

    private static string Tr(string key) => TranslationServer.Translate(key).ToString();
}
