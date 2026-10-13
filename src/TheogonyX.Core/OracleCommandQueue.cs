namespace TheogonyX.Core;

/// <summary>
/// 神託の命令の待ち行列（docs/domain/multiplayer.md）。夢の改ざんと神託は、ここに積んでから、積んだ順（順番の番号の順）に
/// <see cref="ProphetDreams"/> へ実行する。番号・記号の検証もここで行う。
/// 積んだらすぐ、積んだ命令を含めて待ち行列が空になるまで実行する（再入した命令は、実行中の命令の後ろに積む）。
/// </summary>
public sealed class OracleCommandQueue
{
    private readonly ProphetDreams _prophet;
    private readonly TimeControl _timeControl;
    private readonly DreamRules _dreamRules;
    private readonly Queue<OracleCommand> _pending = new();
    private long _nextSequence = 1;
    private bool _draining;

    public OracleCommandQueue(ProphetDreams prophet, TimeControl timeControl, DreamRules dreamRules)
    {
        ArgumentNullException.ThrowIfNull(prophet);
        ArgumentNullException.ThrowIfNull(timeControl);
        ArgumentNullException.ThrowIfNull(dreamRules);

        _prophet = prophet;
        _timeControl = timeControl;
        _dreamRules = dreamRules;
    }

    /// <summary>実行を待っている命令の数。積んだ呼び出しのうちに空になるので、呼び出しの外では 0。</summary>
    public int PendingCount => _pending.Count;

    /// <summary>夢の <paramref name="index"/> 番目を <paramref name="symbolId"/> のカードと入れ替える命令を積んで実行する。</summary>
    public OracleCommandOutcome SubmitTamper(int index, string symbolId)
    {
        ArgumentNullException.ThrowIfNull(symbolId);
        return Submit(OracleCommandKind.Tamper, index, symbolId);
    }

    /// <summary>神託を下す命令を積んで実行する。</summary>
    public OracleCommandOutcome SubmitPronounce() => Submit(OracleCommandKind.Pronounce, 0, null);

    private OracleCommandOutcome Submit(OracleCommandKind kind, int index, string? symbolId)
    {
        var command = new OracleCommand(kind, index, symbolId, _timeControl.World.Year, _nextSequence++);
        _pending.Enqueue(command);

        // 実行中に積まれた命令は、実行中のループが、積まれた順に実行する
        if (_draining)
        {
            return new OracleCommandOutcome(command, null, null);
        }

        _draining = true;
        try
        {
            OracleCommandOutcome? mine = null;
            while (_pending.TryDequeue(out var next))
            {
                var outcome = Execute(next);
                if (next.Sequence == command.Sequence)
                {
                    mine = outcome;
                }
            }
            return mine!;
        }
        finally
        {
            _draining = false;
        }
    }

    private OracleCommandOutcome Execute(OracleCommand command)
    {
        switch (command.Kind)
        {
            case OracleCommandKind.Tamper:
                if (!_prophet.CanTamper)
                {
                    return Reject(command, "cannot_tamper");
                }
                if (command.Index < 0 || command.Index >= _prophet.Dream.Count)
                {
                    return Reject(command, "invalid_dream_index");
                }
                if (!_dreamRules.Symbols.Any(symbol => symbol.Id == command.SymbolId))
                {
                    return Reject(command, "unknown_symbol");
                }
                if (!_prophet.Hand.Contains(command.SymbolId))
                {
                    return Reject(command, "card_not_in_hand");
                }
                _prophet.Tamper(command.Index, command.SymbolId!);
                return new OracleCommandOutcome(command, null, null);

            default:
                if (!_prophet.CanPronounce)
                {
                    return Reject(command, "cannot_pronounce");
                }
                return new OracleCommandOutcome(command, null, _prophet.Pronounce());
        }
    }

    private static OracleCommandOutcome Reject(OracleCommand command, string reason) => new(command, reason, null);
}
