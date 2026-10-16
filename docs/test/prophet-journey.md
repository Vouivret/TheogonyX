# テスト仕様書: 予言者の旅（小さな MVP 小段 3）

- 受け入れ条件: [docs/domain/prophet-life.md](../domain/prophet-life.md)（AC-prophet-life-007〜012）
- 関連ADR: なし

## テスト観点

種別は「自動」「手動」「廃止」のどれか。自動テストの手順はテストコードに書き、ここには書かない。

| ID | 受け入れ条件 | 観点 | 前提 | 操作 | 期待結果 | 種別 | 実装場所 |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| TS-prophet-journey-001 | AC-prophet-life-007 | 行き先へ 1 年に 1 地域ずつ進み、知っている土地が増える | 受け入れ条件のとおり | 1 年を計算する | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/ProphetJourneyTests.cs` |
| TS-prophet-journey-002 | AC-prophet-life-008 | 端に着いたら行き先が消え、端でさらに進めと言われても動かない | 受け入れ条件のとおり | 1 年を計算する | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/ProphetJourneyTests.cs` |
| TS-prophet-journey-003 | AC-prophet-life-009 | 広める旅の行き先の選び方（住人 − 信者が多い隣） | 受け入れ条件のとおり | 1 年を計算する | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/ProphetJourneyTests.cs` |
| TS-prophet-journey-004 | AC-prophet-life-010 | 隣に誰もいない・確率 0 なら動かない | 受け入れ条件のとおり | 1 年を計算する | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/ProphetJourneyTests.cs` |
| TS-prophet-journey-005 | AC-prophet-life-011 | 動いた先の近くの住人が最初の信者になる | 受け入れ条件のとおり | 神託を告げる | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/ProphetJourneyTests.cs` |
| TS-prophet-journey-006 | AC-prophet-life-012 | 居場所・知っている土地・行き先がセーブで戻る | 受け入れ条件のとおり | 保存して読み戻す | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/SaveDataTests.cs` |
| TS-prophet-journey-007 | （画面。仕様書の「画面と語り」） | 予言者の絵が居場所の地域に立ち、動くと移る | Godot 4.7.2 で動かす | 下の手順 | 下の手順のとおり | 手動 | — |

## 手動テストの手順

### TS-prophet-journey-007
1. メインシーンを実行し、自動進行で数十年進める。予言者が西か東の隣の地域へ移ることがあり、Godot の出力に `event=prophet_moved` が出ることを確かめる。
2. 予言者が移った後に神託を下し、HUD の信者の人数が、移った先の近くの住人の数であることを確かめる。

## 対象外

- 神の声の行き先を神託の文から取り出すこと（小段 4）。この小段では `TimeControl.SetDestination` を直接呼ぶ。
- 「逃れる」「招かれる」「巡礼」の理由、海を渡ること、旅を年表に残すこと（ロードマップの範囲の外。年表には残さず、ログだけを出す）。
- 群衆の聖地（群衆が集まる位置）は、最初の予言者の位置のまま動かさない。
