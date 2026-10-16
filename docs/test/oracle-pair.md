# テスト仕様書: 神託の作り方（ルーンの組）

- 受け入れ条件: [docs/domain/oracle-pair.md](../domain/oracle-pair.md)
- 関連ADR: なし

## テスト観点

種別は「自動」「手動」「廃止」のどれか。自動テストの手順はテストコードに書き、ここには書かない。第 A 段（コアに追加）と第 B 段（通信・セーブ・画面・スマホの切り替え）の観点を書く。

| ID | 受け入れ条件 | 観点 | 前提 | 操作 | 期待結果 | 種別 | 実装場所 |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| TS-oracle-pair-001 | AC-oracle-wuxing-001〜007 | 2 枚を選ぶと、1 枚目が対象のドグマが一覧に加わり、結果と一致する（種別と強度は五行の関係。2026-10-14 に極性の和から置き換え） | 受け入れ条件のとおり | 神託を下す | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/OraclePairTests.cs` |
| TS-oracle-pair-002 | AC-oracle-pair-004 | 使った 2 枚が捨札へ移り、山札から 2 枚が補われる | 受け入れ条件のとおり | 神託を下す | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/OraclePairTests.cs` |
| TS-oracle-pair-003 | AC-oracle-pair-005, 006, 008 | 手札が 2 枚未満・崩壊・手札の番号が範囲外か同じ、のとき下せない（理由が返る） | 受け入れ条件のとおり | 神託を下そうとする | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/OraclePairTests.cs` |
| TS-oracle-pair-004 | AC-oracle-pair-007 | 依頼中に神託を下すと、依頼が済む | 受け入れ条件のとおり | 神託を下す | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/OraclePairTests.cs` |
| TS-oracle-pair-005 | AC-oracle-pair-009 | 同じシード・同じ操作の列で、世界・ドグマ・デッキ・神威が一致する | 受け入れ条件のとおり | 2 回計算する | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/OraclePairTests.cs` |
| TS-oracle-pair-006 | AC-oracle-pair-010, 011, 012 | 神威がコストで減り、足りないと下せず、100 年進めても上限で止まる | 受け入れ条件のとおり | 神託を下す・年を進める | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/OraclePairTests.cs` |
| TS-oracle-pair-007 | AC-oracle-pair-013, 014 | 10 年に 3 回下すと 3 回目から歪み（SAN 値 −20・カオス度 +5）が起き、20 年空ければ起きない。SAN 値は 0 を下回らない | 受け入れ条件のとおり | 神託を下す | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/OraclePairTests.cs` |
| TS-oracle-pair-008 | （仕様書の「神威の代償」） | 世界再創生で神威が最初の量に戻り、数えた神託が消える | 受け入れ条件のとおり | 世界再創生する | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/OraclePairTests.cs` |
| TS-oracle-pair-009 | （命令の待ち行列。[多人数プレイ](../domain/multiplayer.md)） | 命令（`PronouncePair`）で下しても同じ結果で、拒否の理由が返り、順番の番号が続く | 受け入れ条件のとおり | 命令を積む | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/OraclePairTests.cs` |
| TS-oracle-pair-010 | （通信。[messages.md](../protocol/messages.md)） | `SPEAK` v1（文の長さ・制御文字・型）と `PRONOUNCE` v3（名詞・動詞・行き先の型と、3 つとも「なし」の拒否）の検証と、廃止した `PRONOUNCE` v1・v2・`TAMPER` が不明な種別として失敗すること | 受け入れ条件のとおり | パースする | 失敗し、理由が `invalid_speak`・`invalid_pronounce`（廃止した形は `unknown_type_or_version`） | 自動 | `tests/TheogonyX.Core.Tests/CompanionMessageParserTests.cs` |
| TS-oracle-pair-011 | （通信） | `ORACLE_STATE` v2 に、神威・語りかけられるか・崩壊・語りかけ方・聞いている途中・依頼と祈り・選べる言葉・告げた神託・方言帳が入り、手札は入らない | 依頼あり、崩壊、神威がコスト未満、聞いている途中、言葉を選ぶ形、告げた神託あり | 書き出す | messages.md の形のとおり | 自動 | `tests/TheogonyX.Core.Tests/HostMessageWriterTests.cs` |
| TS-oracle-pair-012 | （廃止。PC の画面の手札から 2 枚を選ぶ形は、自由文の神託に替えた） | [oracle-speak-ui](oracle-speak-ui.md) の TS-oracle-speak-ui-001〜004 に置き換え | — | — | — | 廃止 | — |
| TS-oracle-pair-013 | （廃止。スマホの手札から 2 枚を選ぶ形は、自由文の神託に替えた） | [oracle-speak-ui](oracle-speak-ui.md) の TS-oracle-speak-ui-005〜007 に置き換え | — | — | — | 廃止 | — |

## 手動テストの手順

## 対象外

- 夢ノード・夢の生成・旧い `Tamper`・旧テストの削除: 第 C 段。
- セーブの往復・不正値: [save-data](save-data.md) の観点（`SaveDataTests.cs`）で確かめる。
