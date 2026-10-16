# テスト仕様書: 予言者の祈り（小さな MVP 小段 5）

- 受け入れ条件: [docs/domain/oracle-pair.md](../domain/oracle-pair.md)（AC-oracle-pair-037〜043）
- 関連ADR: なし

## テスト観点

種別は「自動」「手動」「廃止」のどれか。自動テストの手順はテストコードに書き、ここには書かない。

| ID | 受け入れ条件 | 観点 | 前提 | 操作 | 期待結果 | 種別 | 実装場所 |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| TS-oracle-prayer-001 | AC-oracle-pair-037 | 手札の記号の名詞だけで祈り、方言帳に知っている言葉として残る | 受け入れ条件のとおり | 受け入れ条件のとおり | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/OraclePrayerTests.cs` |
| TS-oracle-prayer-002 | AC-oracle-pair-038 | 新しい語が次の祈りに必ず入る | 受け入れ条件のとおり | 受け入れ条件のとおり | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/OraclePrayerTests.cs` |
| TS-oracle-prayer-003 | AC-oracle-pair-039 | 汚染ルーンの記号の語が祈りに混じる | 受け入れ条件のとおり | 受け入れ条件のとおり | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/OraclePrayerTests.cs` |
| TS-oracle-prayer-004 | AC-oracle-pair-040 | 日々の祈りは 20 年ごとで、自動進行を止めない | 受け入れ条件のとおり | 受け入れ条件のとおり | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/OraclePrayerTests.cs` |
| TS-oracle-prayer-005 | AC-oracle-pair-041 | 同じシード・同じ手札で同じ祈りの語になる（決定性） | 受け入れ条件のとおり | 受け入れ条件のとおり | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/OraclePrayerTests.cs` |
| TS-oracle-prayer-006 | AC-oracle-pair-042 | 次の祈りの年と知っている言葉がセーブで戻り、壊れた値は拒む | 受け入れ条件のとおり | 受け入れ条件のとおり | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/OraclePrayerTests.cs` |
| TS-oracle-prayer-007 | AC-oracle-pair-043 | 祈りの文の応答の検証と頼みの組み立て | 受け入れ条件のとおり | 受け入れ条件のとおり | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/OraclePrayerTests.cs` |
| TS-oracle-prayer-008 | （仕様書の「祈りの言葉と語彙の見せ方」） | 依頼の吹き出しに祈りの文が出て、日々の祈りが予言者の吹き出しに出る。方言帳に知っている言葉が並ぶ | Godot 4.7.2 と Ollama が動く | 下の手順 | 下の手順のとおり | 手動 | — |

## 手動テストの手順

### TS-oracle-prayer-008
1. メインシーンを実行し、自動進行で水資源が減るまで進める（またはリセット直後の星で 100 年跳躍を繰り返す）。依頼が出ると、依頼の吹き出しに「神よ、〈名詞〉は渇き、〈名詞〉も細っています」のような祈りが出て、数秒後に LLM の祈りの文に差し替わることを確かめる。祈りに出る名詞が、予言者の手札の記号の語（木・水・火・土・金のうち手札にあるもの）だけであることを確かめる。
2. 依頼の無い星で 20 年ほど進め、予言者の吹き出しに短い祈り（感謝）が出ることと、自動進行が止まらないことを確かめる。
3. 方言帳を開き、祈りに出た名詞が「（予言者が知っている言葉）」として並ぶことを確かめる。Ollama を止めて、定型の祈りだけが出ることも確かめる。

## 対象外

- 歴史の出来事（建物のルーンが手札に入る）で入った記号だけを「新しい語」とする判定。今は「まだ祈りに入れていない手札の記号」を新しい語とする（起動し直すと、手札の全部の記号が新しい語に戻る）。
