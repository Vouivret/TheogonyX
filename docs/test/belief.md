# テスト仕様書: 信仰としてのドグマ（小さな MVP 小段 2）

- 受け入れ条件: [docs/domain/dogma-belief.md](../domain/dogma-belief.md)（AC-dogma-belief-007〜015）
- 関連ADR: なし

## テスト観点

種別は「自動」「手動」「廃止」のどれか。自動テストの手順はテストコードに書き、ここには書かない。

| ID | 受け入れ条件 | 観点 | 前提 | 操作 | 期待結果 | 種別 | 実装場所 |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| TS-belief-001 | AC-dogma-belief-007 | 信じられる教えの上限と、いちばん弱い教えを捨てること | 受け入れ条件のとおり | 信じる | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/BeliefTests.cs` |
| TS-belief-002 | AC-dogma-belief-008 | 矛盾する教えは弱いほうを捨て、同じ強さなら古いほうを残す | 受け入れ条件のとおり | 信じる | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/BeliefTests.cs` |
| TS-belief-003 | AC-dogma-belief-009 | 最初の信者は予言者の近くの住人だけ | 受け入れ条件のとおり | 神託を告げる | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/BeliefTests.cs` |
| TS-belief-004 | AC-dogma-belief-010 | 広場とつがいでうつり、集まらなかった住人にはうつらない | 受け入れ条件のとおり | 1 年の広がりを計算する | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/BeliefTests.cs` |
| TS-belief-005 | AC-dogma-belief-011 | 子が親の信仰を弱めて継ぐ | 受け入れ条件のとおり | 子が生まれる | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/BeliefTests.cs` |
| TS-belief-006 | AC-dogma-belief-012 | 教えの効き目が信者の割合だけになる | 受け入れ条件のとおり | 1 年を計算する | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/BeliefTests.cs` |
| TS-belief-007 | AC-dogma-belief-013 | 信者のいない教えが忘れられ、年表に残る | 受け入れ条件のとおり | 1 年を計算する | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/BeliefTests.cs` |
| TS-belief-008 | AC-dogma-belief-014 | 同じシードで住人ごとの信仰が同じ（決定性） | 受け入れ条件のとおり | 30 年を 2 回計算する | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/BeliefTests.cs` |
| TS-belief-009 | AC-dogma-belief-015 | セーブの書き出し・読み戻しで信仰と教えの一覧が戻り、続きも同じ | 受け入れ条件のとおり | 保存して読み戻す | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/SaveDataTests.cs` |
| TS-belief-010 | AC-dogma-belief-016 | 同じ教えを 2 度告げても一覧と年表に 1 つだけ | 受け入れ条件のとおり | もう一度告げる | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/BeliefTests.cs` |
| TS-belief-010 | （HUD。仕様書の「見せ方」） | HUD の教えの一覧に信者の人数が出て、年表に「忘れられた」が出る | Godot 4.7.2 で動かす | 下の手順 | 下の手順のとおり | 手動 | — |

## 手動テストの手順

### TS-belief-010
1. メインシーンを実行し、自動進行で住人が各地域に移り住むまで数年待つ。
2. 神託を下す。HUD の教えの一覧に「〈記号〉の〈種別〉　信者 N 人」が出て、N が予言者の近くの住人の数であることを確かめる。
3. 自動進行を続け、広場に集まった住人やつがいを通して N が増えることを確かめる。
4. 住人がいないうちに神託を下した別の星（リセット直後）で数年進め、教えが一覧から消えて年表に「忘れられた」が出ることと、Godot の出力に `event=dogma_forgotten` が出ることを確かめる。

## 対象外

- 隣の地域への運ばれ方・司祭・宗派・親から子で種別が変わる確率は、この MVP の範囲の外（仕様書の「範囲の外」）。
