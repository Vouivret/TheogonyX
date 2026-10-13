# テスト仕様書: セーブ・ロード

- 受け入れ条件: [docs/domain/save-data.md](../domain/save-data.md)
- 関連ADR: なし

## テスト観点

種別は「自動」「手動」「廃止」のどれか。自動テストの手順はテストコードに書き、ここには書かない。

| ID | 受け入れ条件 | 観点 | 前提 | 操作 | 期待結果 | 種別 | 実装場所 |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| TS-save-data-001 | AC-save-data-001, 002, 003 | 書き出して読み戻すと同じ状態になる | 受け入れ条件のとおり | 書き出して読み戻す | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/SaveDataTests.cs` |
| TS-save-data-002 | AC-save-data-004 | 読み込んだ後は止まっている | 自動進行中 | 書き出して読み戻す | 止まっている | 自動 | `tests/TheogonyX.Core.Tests/SaveDataTests.cs` |
| TS-save-data-003 | AC-save-data-005 | 読み込んだ後も、同じシードからの夢の続きになる | 受け入れ条件のとおり | 書き出して読み戻し、夢を見る | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/SaveDataTests.cs` |
| TS-save-data-004 | AC-save-data-006 | 壊れたセーブを読まない | 受け入れ条件の各入力 | 読む | 失敗し、理由が分かる | 自動 | `tests/TheogonyX.Core.Tests/SaveDataTests.cs` |
| TS-save-data-006 | AC-save-data-010, 011 | 自動進行の速さとその年の進みの読み戻しと、古い形・不正な値を読まないこと | 受け入れ条件のとおり | 書き出して読み戻す・読む | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/SaveDataTests.cs` |
| TS-save-data-007 | AC-save-data-012, 013, 014 | 地域の文明の値・建物・廃墟・遺物の読み戻しと、定義と合わない値を読まないこと | 受け入れ条件のとおり | 書き出して読み戻す・読む | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/SaveDataTests.cs` |
| TS-save-data-008 | AC-save-data-015 | 世界再創生をはさんだ廃墟・遺物の読み戻しと、続きの決定性 | 受け入れ条件のとおり | 保存して読み込み、続ける | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/SaveDataTests.cs` |
| TS-save-data-005 | AC-save-data-007, 008, 009 | 起動時の続き・壊れたセーブ・強制終了 | `game/` を Godot 4.7.2 で開く | 下の手順 | 下の手順のとおり | 手動 | — |

## 手動テストの手順

### TS-save-data-005
1. メインシーンを実行し、夢を改ざんして神託を下し、100 年跳躍する。HUD の年とドグマを覚えて、ウィンドウを閉じる。
2. もう一度実行し、同じ年・ドグマ・夢の欄で、止まった状態で始まることを確かめる。
3. 自動進行にしてから閉じ、もう一度実行して、止まった状態で始まることを確かめる。
4. 神託を下した直後に、Godot のエディタの停止ボタンで強制終了する。もう一度実行し、神託の後の状態から始まることを確かめる。
5. ゲームを閉じ、`%APPDATA%\Godot\app_userdata\<プロジェクト名>\save\world.json` を書き換えて壊す（例: 先頭の `{` を消す）。実行し、新しい星で始まり、HUD に読めなかったことが出て、`world.broken-<日時>.json` が残ることを確かめる。

## 対象外

- ファイルを読めない（権限など）とき: 手で起こすのが難しいので、コードを読んで確かめる。
