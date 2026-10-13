# テスト仕様書: 星の年表

- 受け入れ条件: [docs/domain/history.md](../domain/history.md)
- 関連ADR: なし

## テスト観点

種別は「自動」「手動」「廃止」のどれか。自動テストの手順はテストコードに書き、ここには書かない。

| ID | 受け入れ条件 | 観点 | 前提 | 操作 | 期待結果 | 種別 | 実装場所 |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| TS-history-001 | AC-history-001 | ドグマの誕生の行 | 受け入れ条件のとおり | ドグマを加える | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/HistoryTests.cs` |
| TS-history-002 | AC-history-002 | 住人の出来事のうち残すもの | 受け入れ条件のとおり | 出来事を足す | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/HistoryTests.cs` |
| TS-history-003 | AC-history-003 | 星の崩壊と世界再創生の行、再創生で消えないこと | 受け入れ条件のとおり | 跳躍して世界再創生する | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/HistoryTests.cs` |
| TS-history-004 | AC-history-004 | 行の数の上限 | 受け入れ条件のとおり | 行を足す | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/HistoryTests.cs` |
| TS-history-005 | AC-history-005 | 決定性 | 受け入れ条件のとおり | 2 回計算する | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/HistoryTests.cs` |
| TS-history-006 | AC-history-006 | 年表を持たない場合 | 受け入れ条件のとおり | ドグマを加える | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/HistoryTests.cs` |
| TS-history-007 | AC-history-007 | 年表の保存と読み込み、壊れた年表 | 受け入れ条件のとおり | 書き出して読み戻す、読む | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/SaveDataTests.cs`（TS-save-data-004・009 の中で確かめる） |
| TS-history-008 | AC-history-008 | 年表の欄の見た目と開け閉め | `game/` を Godot 4.7.2 で開く | 下の手順 | 下の手順のとおり | 手動 | — |

## 手動テストの手順

### TS-history-008
1. Godot 4.7.2（.NET 版）で `game/project.godot` を開き、F5 でメインシーンを実行する。
2. HUD の「年表」を押し、左上に「星の年表」の欄が開き、「（まだ何も起きていない）」と出ることを確かめる。もう一度押すと閉じる。
3. 夢を改ざんして神託を下し、欄の一番上に「{年} 年　{記号}の{種別}が生まれた」が出ることを確かめる。
4. 血を含む夢で神託を下し、100 年跳躍を何度か押して、住人の狂乱・自閉・絶望や名のある住人の死、星の崩壊の行が新しい順に並ぶこと、欄がスクロールできることを確かめる。
5. 世界再創生を押し、年表が消えずに「世界が再創生された」の行が足されることを確かめる。
6. ゲームを閉じて開き直し、年表が残っていることを確かめる。
7. 出力パネルにエラー・警告が無いことを確かめる。

## 対象外

- 文の言い回しと英語の訳（仮の訳）。
