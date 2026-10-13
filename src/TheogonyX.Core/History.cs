namespace TheogonyX.Core;

// TODO(AI_UNSURE): 残す出来事の種類・行の数の上限・世界再創生で消さないことは、AI が仮に置いた（docs/domain/history.md の「未決」）。人間が決め直す

/// <summary>年表に残す出来事の種類（docs/domain/history.md）。</summary>
public enum HistoryEventType
{
    /// <summary>神託でドグマが生まれた。</summary>
    DogmaFormed,

    /// <summary>住人が精神崩壊した。</summary>
    ResidentBrokeDown,

    /// <summary>称号を持つ住人が死んだ。</summary>
    NotableDied,

    /// <summary>星が崩壊した。</summary>
    WorldDestroyed,

    /// <summary>世界再創生をした。</summary>
    WorldRecreated,
}

/// <summary>
/// 年表の 1 行。出来事の種類ごとに使う項目が決まっていて、使わない項目は null にする。
/// ドグマ: <paramref name="SymbolId"/>・<paramref name="DogmaType"/>・<paramref name="Oracle"/>（AI が神託の文を作れたときだけ）。精神崩壊: <paramref name="Name"/>・<paramref name="Epithet"/>・<paramref name="Breakdown"/>。
/// 称号を持つ住人の死: <paramref name="Name"/>・<paramref name="Epithet"/>。星の崩壊・世界再創生: なし。
/// </summary>
/// <param name="Year">出来事が起きた年（世界再創生では、再創生する前の年）。</param>
/// <param name="Oracle">予言者が誤解して語った神託の文（docs/domain/ai-presentation.md）。ドグマの行だけが持ち、後から添える。</param>
public sealed record HistoryEntry(
    int Year,
    HistoryEventType Type,
    string? SymbolId = null,
    DogmaType? DogmaType = null,
    ResidentName? Name = null,
    ResidentEpithet? Epithet = null,
    BreakdownKind? Breakdown = null,
    OracleText? Oracle = null);

/// <summary>年表の定義。</summary>
/// <param name="MaxEntries">年表に残す行の数の上限。超えたら古い行から消す。</param>
public sealed record HistoryRules(int MaxEntries)
{
    /// <summary>値が範囲の中にあることを確かめる。</summary>
    /// <exception cref="ArgumentException">範囲の外の値がある。</exception>
    public void Validate()
    {
        if (MaxEntries <= 0)
        {
            throw new ArgumentException($"年表の定義の値が範囲外: {this}");
        }
    }
}

/// <summary>星の年表。世界再創生をはさんでも消さず、上限を超えたら古い行から消す（docs/domain/history.md）。</summary>
public sealed class History
{
    private readonly List<HistoryEntry> _entries;

    public History(IEnumerable<HistoryEntry> entries, HistoryRules rules)
    {
        ArgumentNullException.ThrowIfNull(entries);
        ArgumentNullException.ThrowIfNull(rules);
        Rules = rules;
        _entries = [.. entries];
        Trim();
    }

    public HistoryRules Rules { get; }

    /// <summary>古い順の行。</summary>
    public IReadOnlyList<HistoryEntry> Entries => _entries;

    /// <summary>行を足す。上限を超えたら古い行を消す。</summary>
    public void Add(HistoryEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        _entries.Add(entry);
        Trim();
    }

    /// <summary>
    /// ドグマの行に、後からできた神託の文を添える。行は <see cref="Entries"/> から受け取ったものと同じ参照で探し、
    /// 世界再創生・読み込み・上限で消えていれば何もしない。
    /// </summary>
    /// <returns>添えられたか。</returns>
    public bool AttachOracle(HistoryEntry entry, OracleText oracle)
    {
        ArgumentNullException.ThrowIfNull(entry);
        ArgumentNullException.ThrowIfNull(oracle);
        var index = _entries.FindIndex(candidate => ReferenceEquals(candidate, entry));
        if (index < 0 || entry.Type != HistoryEventType.DogmaFormed || !oracle.IsValid())
        {
            return false;
        }
        _entries[index] = entry with { Oracle = oracle };
        return true;
    }

    /// <summary>今の行と同じ中身の、別の年表（セーブに書き出す写し）。</summary>
    public History Copy() => new(_entries, Rules);

    /// <summary>住人の出来事のうち、年表に残すもの（精神崩壊と、称号を持つ住人の死）を足す。</summary>
    public void AddResidentEvents(IEnumerable<ResidentEvent> events)
    {
        ArgumentNullException.ThrowIfNull(events);
        foreach (var residentEvent in events)
        {
            var person = residentEvent.Resident;
            switch (residentEvent.Type)
            {
                case ResidentEventType.BrokeDown:
                    Add(new HistoryEntry(
                        residentEvent.Year, HistoryEventType.ResidentBrokeDown, Name: person.Name, Epithet: person.Epithet, Breakdown: person.Breakdown));
                    break;
                case ResidentEventType.Died when person.Epithet is not null:
                    Add(new HistoryEntry(residentEvent.Year, HistoryEventType.NotableDied, Name: person.Name, Epithet: person.Epithet));
                    break;
            }
        }
    }

    private void Trim()
    {
        if (_entries.Count > Rules.MaxEntries)
        {
            _entries.RemoveRange(0, _entries.Count - Rules.MaxEntries);
        }
    }
}
