using TheogonyX.Core;
using TheogonyX.Core.Persistence;

namespace TheogonyX.Core.Tests;

// 汚染ルーン（docs/domain/oracle-deck.md の「汚染」）。
public class OracleCorruptionTests
{
    private const int Seed = 42;

    private static (ProphetDreams Prophet, TimeControl Time, OracleDeck Deck) Create(int seed = Seed)
    {
        var time = new TimeControl(
            TestRules.World, TestRules.JumpYears, new CivilizationSetup(TestRules.Civilization, TestRules.Terrain, seed, TestRules.Needs), TestRules.History);
        var deck = new OracleDeck(seed, TestRules.Deck);
        return (new ProphetDreams(time, TestRules.ProphetDreams, deck, TestRules.Oracle), time, deck);
    }

    private static int Count(OracleDeck deck, string cardId) =>
        deck.DrawPile.Count(id => id == cardId) + deck.Hand.Count(id => id == cardId) + deck.DiscardPile.Count(id => id == cardId);

    // 手札を指定したカードに置き換える（汚染ルーンを手札に持たせるため）
    private static void SetHand(OracleDeck deck, params string[] hand)
    {
        var state = deck.Capture();
        deck.Restore(state with { Hand = hand, DrawPile = [.. state.DrawPile, .. state.Hand] });
    }

    [Fact]
    [Trait("TS", "TS-oracle-deck-009")]
    public void OnTimeAdvanced_カオス度が50を越える_汚染ルーンが山札に1枚混ざる()
    {
        var (prophet, time, deck) = Create();
        time.World.ChaosLevel = 49.9f;
        Assert.Equal(0, prophet.OnTimeAdvanced().CorruptedAdded);
        Assert.Equal(15, deck.DrawPile.Count + deck.Hand.Count);

        time.World.ChaosLevel = 50f;
        var update = prophet.OnTimeAdvanced();

        Assert.Equal(1, update.CorruptedAdded);
        Assert.Equal(1, Count(deck, "RUNE_MADNESS"));
        Assert.Contains("RUNE_MADNESS", deck.DrawPile);
        Assert.Equal(12, deck.DrawPile.Count);
        Assert.Equal(1, deck.CorruptionStage);
    }

    [Fact]
    [Trait("TS", "TS-oracle-deck-009")]
    public void OnTimeAdvanced_同じ閾値を越えたまま_二度と混ざらない()
    {
        var (prophet, time, deck) = Create();
        time.World.ChaosLevel = 60f;
        prophet.OnTimeAdvanced();

        time.World.ChaosLevel = 40f;
        prophet.OnTimeAdvanced();
        time.World.ChaosLevel = 60f;
        var update = prophet.OnTimeAdvanced();

        Assert.Equal(0, update.CorruptedAdded);
        Assert.Equal(1, Count(deck, "RUNE_MADNESS"));
    }

    [Fact]
    [Trait("TS", "TS-oracle-deck-009")]
    public void OnTimeAdvanced_カオス度が80を越える_2枚目の汚染ルーンが混ざる()
    {
        var (prophet, time, deck) = Create();
        time.World.ChaosLevel = 60f;
        prophet.OnTimeAdvanced();

        time.World.ChaosLevel = 80f;
        var update = prophet.OnTimeAdvanced();

        Assert.Equal(1, update.CorruptedAdded);
        Assert.Equal(1, Count(deck, "RUNE_MADNESS"));
        Assert.Equal(1, Count(deck, "RUNE_DECAY"));
        Assert.Equal(2, deck.CorruptionStage);
    }

    [Fact]
    [Trait("TS", "TS-oracle-deck-009")]
    public void OnTimeAdvanced_一度に両方の閾値を越える_2枚混ざる()
    {
        var (prophet, time, deck) = Create();
        time.World.ChaosLevel = 90f;

        var update = prophet.OnTimeAdvanced();

        Assert.Equal(2, update.CorruptedAdded);
        Assert.Equal(1, Count(deck, "RUNE_MADNESS"));
        Assert.Equal(1, Count(deck, "RUNE_DECAY"));
    }

    [Fact]
    [Trait("TS", "TS-oracle-deck-009")]
    public void OnTimeAdvanced_同じシードと同じカオス度の列_山札の並びが一致する()
    {
        List<string> Play()
        {
            var (prophet, time, deck) = Create();
            time.World.ChaosLevel = 55f;
            prophet.OnTimeAdvanced();
            time.World.ChaosLevel = 85f;
            prophet.OnTimeAdvanced();
            return [.. deck.DrawPile];
        }

        Assert.Equal(Play(), Play());
    }

    [Fact]
    [Trait("TS", "TS-oracle-deck-010")]
    public void Speak_汚染ルーンの記号を教えにする_カオス度が10上がり汚染ルーンが捨札へ移る()
    {
        var (prophet, time, deck) = Create();
        SetHand(deck, "RUNE_MADNESS", "SYM_TREE", "SYM_WATER", "SYM_FIRE");
        time.World.ChaosLevel = 20f;

        var result = prophet.SpeakFirst();

        Assert.Equal(1, result.CorruptedCount);
        Assert.Equal(30f, time.World.ChaosLevel, 3);
        Assert.Contains("RUNE_MADNESS", deck.DiscardPile);
        Assert.Equal("SYM_BLOOD", Assert.Single(time.Dogmas).TargetSymbolId);
    }

    [Fact]
    [Trait("TS", "TS-oracle-deck-010")]
    public void Speak_汚染ルーンの記号ではない教え_汚染ルーンは手札に残りカオス度は上がらない()
    {
        var (prophet, time, deck) = Create();
        SetHand(deck, "SYM_TREE", "RUNE_MADNESS", "SYM_WATER", "SYM_FIRE");
        time.World.ChaosLevel = 20f;

        var result = prophet.SpeakFirst();

        Assert.Equal(0, result.CorruptedCount);
        Assert.Equal(20f, time.World.ChaosLevel, 3);
        Assert.Contains("RUNE_MADNESS", deck.Hand);
    }

    [Fact]
    [Trait("TS", "TS-oracle-deck-010")]
    public void Speak_カオス度が上限に近い_上限を越えない()
    {
        var (prophet, time, deck) = Create();
        SetHand(deck, "RUNE_MADNESS", "RUNE_DECAY", "SYM_WATER", "SYM_FIRE");
        time.World.ChaosLevel = 95f;

        prophet.SpeakFirst();

        Assert.Equal(100f, time.World.ChaosLevel, 3);
    }

    [Fact]
    [Trait("TS", "TS-oracle-deck-011")]
    public void Reset_世界再創生_混ぜた回数は0に戻り汚染ルーンはデッキに残る()
    {
        var (prophet, time, deck) = Create();
        time.World.ChaosLevel = 90f;
        prophet.OnTimeAdvanced();

        time.Reset();
        prophet.Reset();

        Assert.Equal(0, deck.CorruptionStage);
        Assert.Equal(1, Count(deck, "RUNE_MADNESS"));
        time.World.ChaosLevel = 55f;
        Assert.Equal(1, prophet.OnTimeAdvanced().CorruptedAdded);
        Assert.Equal(2, Count(deck, "RUNE_MADNESS"));
    }

    [Fact]
    [Trait("TS", "TS-oracle-deck-012")]
    public void RoundTrip_汚染ルーンと混ぜた回数_保存して読み込むと戻る()
    {
        var (prophet, time, deck) = Create();
        time.World.ChaosLevel = 90f;
        prophet.OnTimeAdvanced();
        var clock = new AutoPlayClock(new Dictionary<AutoPlaySpeed, double>
        {
            [AutoPlaySpeed.Normal] = 1.0,
            [AutoPlaySpeed.Fast] = 0.1,
            [AutoPlaySpeed.RealTime] = 86400.0,
        });
        var expected = deck.Capture();

        var json = SaveDataSerializer.Write(SaveGame.Capture(time, prophet, Seed, clock));
        Assert.True(
            SaveDataSerializer.TryParse(
                json, TestRules.Dream, TestRules.ProphetDreams, TestRules.Civilization, TestRules.Needs, TestRules.Oracle, out var data, out var reason),
            reason);
        var (loadedProphet, loadedTime, loadedDeck) = Create();
        SaveGame.Apply(data!, loadedTime, loadedProphet, clock);

        var actual = loadedDeck.Capture();
        Assert.Equal(expected.DrawPile, actual.DrawPile);
        Assert.Equal(expected.Hand, actual.Hand);
        Assert.Equal(expected.DiscardPile, actual.DiscardPile);
        Assert.Equal(2, actual.CorruptionStage);
    }

    [Fact]
    [Trait("TS", "TS-oracle-deck-012")]
    public void TryParse_汚染ルーンの定義を渡さない_汚染ルーンのカードは読めない()
    {
        var (prophet, time, _) = Create();
        time.World.ChaosLevel = 90f;
        prophet.OnTimeAdvanced();
        var clock = new AutoPlayClock(new Dictionary<AutoPlaySpeed, double> { [AutoPlaySpeed.Normal] = 1.0, [AutoPlaySpeed.Fast] = 0.1, [AutoPlaySpeed.RealTime] = 86400.0 });
        var json = SaveDataSerializer.Write(SaveGame.Capture(time, prophet, Seed, clock));

        var ok = SaveDataSerializer.TryParse(
            json, TestRules.Dream, TestRules.ProphetDreams, TestRules.Civilization, TestRules.Needs, out var data, out var reason);

        Assert.False(ok);
        Assert.Null(data);
        Assert.Equal("invalid_deck", reason);
    }

    [Fact]
    [Trait("TS", "TS-oracle-deck-012")]
    public void Validate_汚染ルーンの定義が正しくない_例外になる()
    {
        var symbols = new HashSet<string> { "SYM_TREE", "SYM_WATER", "SYM_BLOOD", "SYM_FIRE", "SYM_EARTH", "SYM_METAL" };

        void Check(CorruptionRules corruption) => (TestRules.Oracle with { Corruption = corruption }).Validate(symbols);

        Check(TestRules.Oracle.CorruptionOrNone);
        Assert.Throws<ArgumentException>(() => Check(new CorruptionRules([80f, 50f], TestRules.Oracle.CorruptionOrNone.Runes, 10f)));
        Assert.Throws<ArgumentException>(() => Check(new CorruptionRules([50f], [], 10f)));
        Assert.Throws<ArgumentException>(() => Check(new CorruptionRules([50f], [new CorruptedRune("SYM_TREE", "SYM_BLOOD")], 10f)));
        Assert.Throws<ArgumentException>(() => Check(new CorruptionRules([50f], [new CorruptedRune("RUNE_X", "SYM_UNKNOWN")], 10f)));
        Assert.Throws<ArgumentException>(() => Check(new CorruptionRules([50f], TestRules.Oracle.CorruptionOrNone.Runes, -1f)));
    }
}
