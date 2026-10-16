namespace TheogonyX.Core;

/// <summary>
/// 神託の記録（経典の元。docs/domain/ai-presentation.md の「経典」）。神の文そのままと、聞き違えた結果を残す。
/// 聞こえた文と語りは LLM が作れたときだけ入る（作れなければ null で、画面は聞き違えた語から規則の文を作る）。
/// </summary>
/// <param name="Teaching">教え。名詞が聞こえなかった（行き先だけの神託）ときは null。</param>
/// <param name="MysteryWords">どの記号にも当たらなかった言葉（神の文の言葉そのもの）。</param>
public sealed record OracleRecord(
    int Year, string GodText, string? HeardNounId, string? HeardVerbId, Dogma? Teaching, TravelDirection Destination,
    IReadOnlyList<string> MysteryWords, string? HeardText = null, string? Narration = null);

/// <summary>セーブに残す方言の状態。</summary>
public sealed record DialectSave(
    string VesselId, IReadOnlyList<DialectShift> Shifts, IReadOnlyList<DialectEntry> Book, IReadOnlyList<OracleRecord> Records,
    int? NextPrayerYear = null);

/// <summary>
/// 予言者の方言の状態（語義のずれ表・方言帳・神託の記録）。ずれ表は星を作るときに決め、途中で変えない。
/// </summary>
public sealed class DialectState
{
    /// <summary>残す神託の記録の数（古いものから捨てる）。TODO(AI_UNSURE): AI が仮に置いた</summary>
    public const int MaxRecords = 100;

    // 神の文・聞こえた文・語りの長さの上限（セーブを読むときの検証にも使う）
    public const int MaxTextLength = 400;

    private readonly List<OracleRecord> _records = [];

    public DialectState(DialectRules rules, string vesselId, int seed)
    {
        ArgumentNullException.ThrowIfNull(rules);
        Rules = rules;
        Seed = seed;
        Table = DialectTable.Create(rules, vesselId, seed);
    }

    /// <summary>星のシード。祈りに入れる語を選ぶ乱数を作るのに使う。</summary>
    public int Seed { get; }

    /// <summary>次に日々の祈りをささやく年。まだ決めていなければ null（最初に時間が進んだときに決める）。</summary>
    public int? NextPrayerYear { get; set; }

    public DialectRules Rules { get; }

    public DialectTable Table { get; private set; }

    public DialectBook Book { get; private set; } = new();

    /// <summary>神託の記録（古い順）。</summary>
    public IReadOnlyList<OracleRecord> Records => _records;

    public void AddRecord(OracleRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);
        _records.Add(record);
        if (_records.Count > MaxRecords)
        {
            _records.RemoveAt(0);
        }
    }

    /// <summary>その年の、その神の文の記録（いちばん新しいもの）に、LLM が作った聞こえた文と語りを添える。見つからなければ false。</summary>
    public bool AttachNarration(int year, string godText, string heardText, string narration)
    {
        var index = _records.FindLastIndex(record => record.Year == year && record.GodText == godText);
        if (index < 0 || !AiText.IsPlain(heardText, MaxTextLength) || !AiText.IsPlain(narration, MaxTextLength))
        {
            return false;
        }
        _records[index] = _records[index] with { HeardText = heardText, Narration = narration };
        return true;
    }

    /// <summary>世界再創生: 神託の記録を捨てる。ずれ表と方言帳は、予言者の言葉の知識なので残す。</summary>
    public void ClearRecords() => _records.Clear();

    public DialectSave Capture() => new(Table.VesselId, [.. Table.Shifts], [.. Book.Entries], [.. _records], NextPrayerYear);

    /// <summary>セーブから読み込んだ状態に置き換える。検証を全部済ませてから置き換える。</summary>
    /// <exception cref="ArgumentException">定義に合わない値。</exception>
    public void Restore(DialectSave save)
    {
        ArgumentNullException.ThrowIfNull(save);
        var table = DialectTable.Restore(Rules, save.VesselId, save.Shifts);
        var book = new DialectBook();
        foreach (var entry in save.Book)
        {
            var valid = entry.Reason switch
            {
                DialectReason.Mystery => entry.HeardWordId is null && AiText.IsPlain(entry.GodWord, OracleHearer.MaxMysteryWordLength),
                DialectReason.Known => entry.HeardWordId is null && Rules.FindNoun(entry.GodWord) is not null,
                _ => IsWord(entry.GodWord) && entry.HeardWordId is not null && IsWord(entry.HeardWordId),
            };
            if (!valid || !book.Add(entry))
            {
                throw new ArgumentException($"方言帳の行が定義に合わない: {entry}", nameof(save));
            }
        }
        foreach (var record in save.Records)
        {
            var valid = AiText.IsPlain(record.GodText, MaxTextLength)
                && (record.HeardNounId is null || Rules.FindNoun(record.HeardNounId) is not null)
                && (record.HeardVerbId is null || Rules.FindVerb(record.HeardVerbId) is not null)
                && (record.HeardText is null || AiText.IsPlain(record.HeardText, MaxTextLength))
                && (record.Narration is null || AiText.IsPlain(record.Narration, MaxTextLength))
                && record.MysteryWords.All(word => AiText.IsPlain(word, OracleHearer.MaxMysteryWordLength));
            if (!valid)
            {
                throw new ArgumentException($"神託の記録が定義に合わない: year={record.Year}", nameof(save));
            }
        }
        if (save.NextPrayerYear is < 0)
        {
            throw new ArgumentException($"次の祈りの年が正しくない: {save.NextPrayerYear}", nameof(save));
        }
        Table = table;
        Book = book;
        NextPrayerYear = save.NextPrayerYear;
        _records.Clear();
        _records.AddRange(save.Records.TakeLast(MaxRecords));
    }

    private bool IsWord(string id) => Rules.FindNoun(id) is not null || Rules.FindVerb(id) is not null;
}
