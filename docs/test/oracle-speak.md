# テスト仕様書: 自由文の神託の実行（小さな MVP 小段 4）

- 受け入れ条件: [docs/domain/oracle-pair.md](../domain/oracle-pair.md)（AC-oracle-pair-024〜030）
- 関連ADR: なし

## テスト観点

種別は「自動」「手動」「廃止」のどれか。自動テストの手順はテストコードに書き、ここには書かない。小段 4 のうち、LLM・画面・古いセーブの扱いは別のテスト仕様書に書く。

| ID | 受け入れ条件 | 観点 | 前提 | 操作 | 期待結果 | 種別 | 実装場所 |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| TS-oracle-speak-001 | AC-oracle-pair-024 | 語りかけて教えが生まれ、神威・手札・捨札・記録が受け入れ条件のとおりになる | 受け入れ条件のとおり | 受け入れ条件のとおり | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/OracleSpeakTests.cs` |
| TS-oracle-speak-002 | AC-oracle-pair-025 | ずれ表に当たると聞き違えた教えが生まれ、方言帳に残る | 受け入れ条件のとおり | 受け入れ条件のとおり | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/OracleSpeakTests.cs` |
| TS-oracle-speak-003 | AC-oracle-pair-026 | 行き先だけの神託で、教えは生まれず予言者が行き先に向かう | 受け入れ条件のとおり | 受け入れ条件のとおり | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/OracleSpeakTests.cs` |
| TS-oracle-speak-004 | AC-oracle-pair-027 | 神威が足りない・崩壊しているとき語りかけられない（理由と順番の番号） | 受け入れ条件のとおり | 受け入れ条件のとおり | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/OracleSpeakTests.cs` |
| TS-oracle-speak-005 | AC-oracle-pair-028 | 聞こえた文と語りを、同じ年・同じ神の文の記録に添える | 受け入れ条件のとおり | 受け入れ条件のとおり | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/OracleSpeakTests.cs` |
| TS-oracle-speak-006 | AC-oracle-pair-029 | 方言の状態がセーブで戻る | 受け入れ条件のとおり | 受け入れ条件のとおり | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/OracleSpeakTests.cs` |
| TS-oracle-speak-007 | AC-oracle-pair-030 | 定義に合わない方言の状態を、何も変えずに拒む | 受け入れ条件のとおり | 受け入れ条件のとおり | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/OracleSpeakTests.cs` |
