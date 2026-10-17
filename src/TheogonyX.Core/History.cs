namespace TheogonyX.Core;

// TODO(AI_UNSURE): 残す出来事の種類・行の数の上限・世界再創生で消さないことは、AI が仮に置いた（docs/domain/history.md の「未決」）。人間が決め直す

/// <summary>年表に残す出来事の種類（docs/domain/history.md）。</summary>
public enum HistoryEventType
{
    /// <summary>神託でドグマが生まれた。</summary>
    DogmaFormed,

    /// <summary>信者がいなくなって、教えが忘れられた。</summary>
    DogmaForgotten,

    /// <summary>住人が精神崩壊した。</summary>
    ResidentBrokeDown,

    /// <summary>称号を持つ住人が死んだ。</summary>
    NotableDied,

    /// <summary>星が崩壊した。</summary>
    WorldDestroyed,

    /// <summary>世界再創生をした。</summary>
    WorldRecreated,

    /// <summary>星の字が崩れた（docs/domain/script-decipher.md）。</summary>
    ScriptDecayed,

    /// <summary>章が終わった（100 年ごと・崩壊・再創生）。始まりの年とカオス度を持つ（docs/domain/history.md）。</summary>
    ChapterEnded,
}

/// <summary>
/// 年表の 1 行。出来事の種類ごとに使う項目が決まっていて、使わない項目は null にする。
/// ドグマ: <paramref name="SymbolId"/>・<paramref name="DogmaType"/>・<paramref name="Oracle"/>（AI が神託の文を作れたときだけ）。精神崩壊: <paramref name="Name"/>・<paramref name="Epithet"/>・<paramref name="Breakdown"/>。
/// 称号を持つ住人の死: <paramref name="Name"/>・<paramref name="Epithet"/>。章の終わり: <paramref name="FromYear"/>・<paramref name="ChaosFrom"/>・<paramref name="ChaosTo"/>・<paramref name="Myth"/>（AI が神話の文を作れたときだけ）。星の崩壊・世界再創生: なし。
/// </summary>
/// <param name="Year">出来事が起きた年（世界再創生では、再創生する前の年。章の終わりでは、章の最後の年）。</param>
/// <param name="Oracle">予言者が誤解して語った神託の文（docs/domain/ai-presentation.md）。ドグマの行だけが持ち、後から添える。</param>
/// <param name="FromYear">章の始まりの年。</param>
/// <param name="ChaosFrom">章の始まりのカオス度。</param>
/// <param name="ChaosTo">章の終わりのカオス度。</param>
/// <param name="Myth">章の神話の文（docs/domain/ai-presentation.md の「歴史の神話化」）。章の行だけが持ち、後から添える。</param>
public sealed record HistoryEntry(
    int Year,
    HistoryEventType Type,
    string? SymbolId = null,
    DogmaType? DogmaType = null,
    ResidentName? Name = null,
    ResidentEpithet? Epithet = null,
    BreakdownKind? Breakdown = null,
    OracleText? Oracle = null,
    int? FromYear = null,
    float? ChaosFrom = null,
    float? ChaosTo = null,
    MythText? Myth = null);

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

/// <summary>今の章の始まり（年と、そのときのカオス度）。章は年表とセーブに残す。</summary>
public sealed record ChapterStart(int Year, float ChaosLevel);

/// <summary>星の年表。世界再創生をはさんでも消さず、上限を超えたら古い行から消す（docs/domain/history.md）。</summary>
public sealed class History
{
    /// <summary>章の長さ（年）。年が、この倍数になったら章を閉じる（docs/domain/ai-presentation.md の「歴史の神話化」）。</summary>
    public const int ChapterYears = 100;

    private readonly List<HistoryEntry> _entries;

    /// <param name="chapter">今の章の始まり。null なら年 0・カオス度 0 から始める。</param>
    public History(IEnumerable<HistoryEntry> entries, HistoryRules rules, ChapterStart? chapter = null)
    {
        ArgumentNullException.ThrowIfNull(entries);
        ArgumentNullException.ThrowIfNull(rules);
        Rules = rules;
        _entries = [.. entries];
        Chapter = chapter ?? new ChapterStart(0, 0f);
        Trim();
    }

    public HistoryRules Rules { get; }

    /// <summary>古い順の行。</summary>
    public IReadOnlyList<HistoryEntry> Entries => _entries;

    /// <summary>今の章の始まり。</summary>
    public ChapterStart Chapter { get; private set; }

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
        var index = IndexOf(entry);
        if (index < 0 || entry.Type != HistoryEventType.DogmaFormed || !oracle.IsValid())
        {
            return false;
        }
        _entries[index] = entry with { Oracle = oracle };
        return true;
    }

    /// <summary>
    /// 章の行に、後からできた神話の文を添える。扱いは <see cref="AttachOracle"/> と同じで、
    /// 神話の文がまだ無い章の行にだけ添える。
    /// </summary>
    /// <returns>添えられたか。</returns>
    public bool AttachMyth(HistoryEntry entry, MythText myth)
    {
        ArgumentNullException.ThrowIfNull(entry);
        ArgumentNullException.ThrowIfNull(myth);
        var index = IndexOf(entry);
        if (index < 0 || entry.Type != HistoryEventType.ChapterEnded || entry.Myth is not null || !myth.IsValid())
        {
            return false;
        }
        _entries[index] = entry with { Myth = myth };
        return true;
    }

    /// <summary>年が <see cref="ChapterYears"/> の倍数になったら、今の章をその年で閉じる。</summary>
    public void AdvanceChapters(int year, float chaosLevel)
    {
        if (year % ChapterYears == 0)
        {
            CloseChapter(year, chaosLevel);
        }
    }

    /// <summary>今の章を、この年で閉じる（星の崩壊・世界再創生・100 年ごと）。始まりより前・同じ年なら何もしない。</summary>
    public void CloseChapter(int year, float chaosLevel)
    {
        if (year <= Chapter.Year)
        {
            return;
        }
        Add(new HistoryEntry(year, HistoryEventType.ChapterEnded, FromYear: Chapter.Year, ChaosFrom: Chapter.ChaosLevel, ChaosTo: chaosLevel));
        Chapter = new ChapterStart(year, chaosLevel);
    }

    /// <summary>行は足さず、今の章の始まりだけを置く（世界再創生の後）。</summary>
    public void RestartChapter(int year, float chaosLevel) => Chapter = new ChapterStart(year, chaosLevel);

    /// <summary>今の行と同じ中身の、別の年表（セーブに書き出す写し）。</summary>
    public History Copy() => new(_entries, Rules, Chapter);

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

    // 参照で探す（中身が同じでも、年表から受け取っていない行は別の行として扱う）
    private int IndexOf(HistoryEntry entry) => _entries.FindIndex(candidate => ReferenceEquals(candidate, entry));

    private void Trim()
    {
        if (_entries.Count > Rules.MaxEntries)
        {
            _entries.RemoveRange(0, _entries.Count - Rules.MaxEntries);
        }
    }
}
