# テスト仕様書: 神託の作り方（ルーンの組）

- 受け入れ条件: [docs/domain/oracle-pair.md](../domain/oracle-pair.md)
- 関連ADR: なし

## テスト観点

種別は「自動」「手動」「廃止」のどれか。自動テストの手順はテストコードに書き、ここには書かない。**第 A 段（コアに追加。画面・通信・セーブは変えない）の範囲だけ**を書いている。画面・スマホ・セーブの観点は、第 B 段で足す。

| ID | 受け入れ条件 | 観点 | 前提 | 操作 | 期待結果 | 種別 | 実装場所 |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| TS-oracle-pair-001 | AC-oracle-pair-001, 002, 003 | 2 枚を選ぶと、1 枚目が対象のドグマが加わる（極性の和で種別、衝撃度で強度） | 受け入れ条件のとおり | 神託を下す | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/OraclePairTests.cs` |
| TS-oracle-pair-002 | AC-oracle-pair-004 | 使った 2 枚が捨札へ移り、山札から 2 枚が補われる | 受け入れ条件のとおり | 神託を下す | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/OraclePairTests.cs` |
| TS-oracle-pair-003 | AC-oracle-pair-005, 006, 008 | 手札が 2 枚未満・崩壊・手札の番号が範囲外か同じ、のとき下せない（理由が返る） | 受け入れ条件のとおり | 神託を下そうとする | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/OraclePairTests.cs` |
| TS-oracle-pair-004 | AC-oracle-pair-007 | 依頼中に神託を下すと、依頼が済む | 受け入れ条件のとおり | 神託を下す | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/OraclePairTests.cs` |
| TS-oracle-pair-005 | AC-oracle-pair-009 | 同じシード・同じ操作の列で、世界・ドグマ・デッキ・神威が一致する | 受け入れ条件のとおり | 2 回計算する | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/OraclePairTests.cs` |
| TS-oracle-pair-006 | AC-oracle-pair-010, 011, 012 | 神威がコストで減り、足りないと下せず、100 年進めても上限で止まる | 受け入れ条件のとおり | 神託を下す・年を進める | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/OraclePairTests.cs` |
| TS-oracle-pair-007 | AC-oracle-pair-013, 014 | 10 年に 3 回下すと 3 回目から歪み（SAN 値 −20・カオス度 +5）が起き、20 年空ければ起きない。SAN 値は 0 を下回らない | 受け入れ条件のとおり | 神託を下す | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/OraclePairTests.cs` |
| TS-oracle-pair-008 | （仕様書の「神威の代償」） | 世界再創生で神威が最初の量に戻り、数えた神託が消える | 受け入れ条件のとおり | 世界再創生する | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/OraclePairTests.cs` |
| TS-oracle-pair-009 | （命令の待ち行列。[多人数プレイ](../domain/multiplayer.md)） | 命令（`PronouncePair`）で下しても同じ結果で、拒否の理由が返り、順番の番号が続く | 受け入れ条件のとおり | 命令を積む | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/OraclePairTests.cs` |

## 対象外

- 画面（手札から 2 枚を選ぶ欄）・スマホ・通信（`ORACLE_STATE`・`PRONOUNCE`）・セーブ（神威と直近の神託の年）: 第 B 段で観点を足す。
- 夢ノード・夢の生成・`TAMPER` の削除: 第 C 段。
