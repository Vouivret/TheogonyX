using System;
using System.Globalization;
using Godot;
using TheogonyX.Core;
using TheogonyX.Core.Protocol;

namespace TheogonyX.Scenes.Main;

/// <summary>
/// 神託の欄と依頼の吹き出しを描く（docs/domain/script-decipher.md、prophet-request.md）。
/// 下の帯の左に神威を出し、今の状態の案内は「文を書く」ボタンの説明に出す。手札（予言者の語彙）は見せない。
/// 依頼の吹き出しは予言者の頭の上に出す（置き場所は <see cref="HudLayout"/>）。
/// 字を組んで下すのは <see cref="ScriptComposerView"/> と <see cref="OracleView"/> が受け持つ。
/// </summary>
public sealed class DreamView
{
    private readonly ProphetDreams _prophet;
    private readonly TimeControl _timeControl;
    private readonly Label _faith;
    private readonly Button _composeButton;
    private readonly Label _requestBubble;
    private readonly Control _requestPanel;

    /// <param name="owner">ノードを探すためのシーンの根。</param>
    public DreamView(Node owner, ProphetDreams prophet, TimeControl timeControl)
    {
        _prophet = prophet;
        _timeControl = timeControl;
        _faith = owner.GetNode<Label>("%DreamGuide");
        _requestBubble = owner.GetNode<Label>("%RequestBubble");
        _requestPanel = owner.GetNode<Control>("%RequestPanel");
        _composeButton = owner.GetNode<Button>("%ComposeButton");
        _composeButton.Pressed += () => ComposeRequested?.Invoke();

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
        // 語りかけられないとき（崩壊・神威が足りない）は、案内を神威の横に出して見えるようにする
        var faith = _prophet.Faith;
        var guide = GuideKey();
        _faith.Text = guide == "ORACLE_GUIDE_COMPOSE"
            ? string.Format(CultureInfo.CurrentCulture, Tr("HUD_FAITH"), faith.Value, faith.Max)
            : string.Format(CultureInfo.CurrentCulture, Tr("HUD_FAITH_WARNING"), Tr(guide), faith.Value, faith.Max);
        _composeButton.TooltipText = Tr(guide);
        if (_prophet.ActiveRequest is { } request)
        {
            _requestBubble.Text = string.Format(
                CultureInfo.CurrentCulture, Tr("REQUEST_BUBBLE"), Tr($"TROUBLE_{request.Id}"), Tr($"SYMBOL_{request.SymbolId}"));
            _requestPanel.Visible = true;
        }
        else
        {
            _requestPanel.Visible = false;
        }
        Changed?.Invoke();
    }

    // 今の状態の案内
    private string GuideKey() =>
        _timeControl.World.IsDestroyed ? "ORACLE_GUIDE_DESTROYED"
        : _prophet.Faith.CanAfford ? "ORACLE_GUIDE_COMPOSE"
        : "ORACLE_GUIDE_NO_FAITH";

    private static string Tr(string key) => TranslationServer.Translate(key).ToString();
}
