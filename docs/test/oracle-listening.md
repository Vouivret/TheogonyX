# テスト仕様書: 神の文の聞き取りと語り（小さな MVP 小段 4）

- 受け入れ条件: [docs/domain/oracle-pair.md](../domain/oracle-pair.md)（AC-oracle-pair-031〜036）
- 関連ADR: なし

## テスト観点

種別は「自動」「手動」「廃止」のどれか。自動テストの手順はテストコードに書き、ここには書かない。LLM は呼ばない。

| ID | 受け入れ条件 | 観点 | 前提 | 操作 | 期待結果 | 種別 | 実装場所 |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| TS-oracle-listening-001 | AC-oracle-pair-031 | 神の文をデータとして囲んで渡し、指示に従わせない | 受け入れ条件のとおり | 頼みを組み立てる | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/OracleListeningTests.cs` |
| TS-oracle-listening-002 | AC-oracle-pair-032 | 正しい答えを読める | 受け入れ条件のとおり | 答えを読む | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/OracleListeningTests.cs` |
| TS-oracle-listening-003 | AC-oracle-pair-033 | 壊れた答えを使わない | 受け入れ条件のとおり | 答えを読む | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/OracleListeningTests.cs` |
| TS-oracle-listening-004 | AC-oracle-pair-034 | 神の文の長さ・制御文字の検証 | 受け入れ条件のとおり | 確かめる | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/OracleListeningTests.cs` |
| TS-oracle-listening-005 | AC-oracle-pair-035 | 聞こえた文に聞き違えた語が入っていなければ使わない | 受け入れ条件のとおり | 答えを読む | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/OracleListeningTests.cs` |
| TS-oracle-listening-006 | AC-oracle-pair-036 | 語りの頼みに聞こえた言葉などを渡す | 受け入れ条件のとおり | 頼みを組み立てる | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/OracleListeningTests.cs` |

## 対象外

- 実際の Ollama での聞き取りの当たり方（小さいモデルが ID を正しく選べるか）は、小段 4 の手動テスト（`docs/test/oracle-speak-ui.md`）で確かめる。
