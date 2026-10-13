namespace TheogonyX.Core;

/// <summary>
/// 神託の命令の待ち行列（docs/domain/multiplayer.md）。神託を下す操作は、ここに積んでから、積んだ順（順番の番号の順）に
/// <see cref="ProphetDreams"/> へ実行する。手札の番号・神威などの検証は <see cref="ProphetDreams.PairRejection"/> が行う。
/// 積んだらすぐ、積んだ命令を含めて待ち行列が空になるまで実行する（再入した命令は、実行中の命令の後ろに積む）。
/// </summary>
public sealed class OracleCommandQueue
{
    private readonly ProphetDreams _prophet;
    private readonly TimeControl _timeControl;
    private readonly Queue<OracleCommand> _pending = new();
    private long _nextSequence = 1;
    private bool _draining;

    public OracleCommandQueue(ProphetDreams prophet, TimeControl timeControl)
    {
        ArgumentNullException.ThrowIfNull(prophet);
        ArgumentNullException.ThrowIfNull(timeControl);

        _prophet = prophet;
        _timeControl = timeControl;
    }

    /// <summary>実行を待っている命令の数。積んだ呼び出しのうちに空になるので、呼び出しの外では 0。</summary>
    public int PendingCount => _pending.Count;

    /// <summary>手札の <paramref name="first"/> 番・<paramref name="second"/> 番のルーン（1 枚目が対象）で神託を下す命令を積んで実行する。</summary>
    public OracleCommandOutcome SubmitPronouncePair(int first, int second)
    {
        var command = new OracleCommand(first, second, _timeControl.World.Year, _nextSequence++);
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
        if (_prophet.PairRejection(command.First, command.Second) is { } reason)
        {
            return new OracleCommandOutcome(command, reason, null);
        }
        return new OracleCommandOutcome(command, null, _prophet.PronouncePair(command.First, command.Second));
    }
}
