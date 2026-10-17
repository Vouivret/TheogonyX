# テスト仕様書: 方言の聞き違え（小さな MVP 小段 1）

- 受け入れ条件: [docs/domain/oracle-pair.md](../domain/oracle-pair.md)（AC-oracle-pair-015〜023）
- 関連ADR: なし

## テスト観点

種別は「自動」「手動」「廃止」のどれか。自動テストの手順はテストコードに書き、ここには書かない。小段 1 はコアだけで、画面・LLM・セーブ・通信は小段 4 以降のテスト仕様書に書く。

| ID | 受け入れ条件 | 観点 | 前提 | 操作 | 期待結果 | 種別 | 実装場所 |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| TS-oracle-dialect-001 | AC-oracle-pair-015 | 器の割合でずれ表の中身が決まる | 受け入れ条件のとおり | ずれ表を作る | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/OracleDialectTests.cs` |
| TS-oracle-dialect-002 | AC-oracle-pair-016 | 同じシードで同じ表になり、表の行から戻せ、合わない行は拒む | 受け入れ条件のとおり | 作る・戻す | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/OracleDialectTests.cs` |
| TS-oracle-dialect-003 | AC-oracle-pair-017 | ずれ表に当たらなければ言ったとおりの教えになる | 受け入れ条件のとおり | 聞き取る | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/OracleDialectTests.cs` |
| TS-oracle-dialect-004 | AC-oracle-pair-018 | ずれは 1 か所まで（動詞が先）。動詞が無ければ名詞がずれて既定の崇拝になる | 受け入れ条件のとおり | 聞き取る | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/OracleDialectTests.cs` |
| TS-oracle-dialect-005 | AC-oracle-pair-019, 020 | 知らない言葉が先に当たり、聞き違え先は手札の記号の名詞（隣が手札にあれば隣）に限る。LLM が選ばなければ、その中から選ぶ | 受け入れ条件のとおり | 聞き取る | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/OracleDialectTests.cs` |
| TS-oracle-dialect-006 | AC-oracle-pair-021 | 謎の言葉は 1 か所のずれに数えず、方言帳と一覧に残る | 受け入れ条件のとおり | 聞き取る | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/OracleDialectTests.cs` |
| TS-oracle-dialect-007 | AC-oracle-pair-022 | 方角は通じ、北・南・土地の名は「なし」に聞き違える。行き先だけの神託も聞ける | 受け入れ条件のとおり | 聞き取る | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/OracleDialectTests.cs` |
| TS-oracle-dialect-008 | AC-oracle-pair-023 | 定義に無い語は聞こえなかったことにし、壊れた答えを理由つきで拒む | 受け入れ条件のとおり | 聞き取る | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/OracleDialectTests.cs` |
| TS-oracle-dialect-009 | （仕様書の「方言帳」） | 方言帳は同じ行を 2 度足さない | 同じ行 | 2 度足す | 2 度目は足されず、1 行のまま | 自動 | `tests/TheogonyX.Core.Tests/OracleDialectTests.cs` |
| TS-oracle-dialect-010 | AC-oracle-pair-044 | 同じ神の言葉の行は、新しい行に置き換わる | 受け入れ条件のとおり | 受け入れ条件のとおり | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/OracleDialectTests.cs` |

## 対象外

- 決定性の結合テスト（同じシード・同じ命令の列）は、神託の実行を作る小段 4 で書く。表の決定性は TS-oracle-dialect-002 で確かめた。
