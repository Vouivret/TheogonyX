# テスト仕様書: AI による表現（予言者の誤解）

- 受け入れ条件: [docs/domain/ai-presentation.md](../domain/ai-presentation.md)
- 関連ADR: なし

## テスト観点

種別は「自動」「手動」「廃止」のどれか。自動テストの手順はテストコードに書き、ここには書かない。自動テストは LLM を呼ばない（応答の形を決めて与える）。

| ID | 受け入れ条件 | 観点 | 前提 | 操作 | 期待結果 | 種別 | 実装場所 |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| TS-ai-presentation-001 | AC-ai-presentation-001 | LLM への頼みの文と本文の形 | 受け入れ条件のとおり | 組み立てる | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/OracleProphecyTests.cs` |
| TS-ai-presentation-002 | AC-ai-presentation-002 | 応答の検証（外部入力） | 受け入れ条件のとおり | 取り出す | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/OracleProphecyTests.cs` |
| TS-ai-presentation-003 | AC-ai-presentation-003 | 宛先がループバックだけであること、定義の値の範囲 | 受け入れ条件のとおり | 検証する | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/OracleProphecyTests.cs` |
| TS-ai-presentation-004 | AC-ai-presentation-004 | 年表のドグマの行に添えること | 受け入れ条件のとおり | 添える | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/OracleProphecyTests.cs` |
| TS-ai-presentation-005 | AC-ai-presentation-005 | 神託の文の保存と読み込み、壊れた文 | 受け入れ条件のとおり | 書き出して読み戻す、読む | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/SaveDataTests.cs`（TS-save-data-004 の中で確かめる） |
| TS-ai-presentation-007 | AC-ai-presentation-007 | 章の神話の頼みの組み立て（年・カオス度の変化・出来事・前の言い伝え） | 受け入れ条件のとおり | 頼みを組み立てる | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/MythChronicleTests.cs` |
| TS-ai-presentation-008 | AC-ai-presentation-008 | 神話の応答の検証 | 受け入れ条件のとおり | 取り出す | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/MythChronicleTests.cs` |
| TS-ai-presentation-009 | AC-ai-presentation-009 | 神話の文を章の行にだけ添えること | 受け入れ条件のとおり | 添える | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/MythChronicleTests.cs` |
| TS-ai-presentation-006 | AC-ai-presentation-006 | 実際の Ollama での吹き出しと年表、止まっているときの定型文 | `game/` を Godot 4.7.2 で開く。Ollama に `gemma2:9b` がある | 下の手順 | 下の手順のとおり | 手動 | — |

## 手動テストの手順

### TS-ai-presentation-006
1. Ollama を動かし、`ollama list` に `gemma2:9b` があることを確かめる（無ければ `ollama pull gemma2:9b`）。ほかのモデルを同時に使っていると、読み込み直しで遅くなる。
2. Godot 4.7.2（.NET 版）で `game/project.godot` を開き、F5 でメインシーンを実行する。
3. 夢を改ざんして神託を下し、吹き出しに「予言者は神託を紡いでいる…」が出て、数秒〜数十秒で見出し（「」つき）・意図・教義に変わり、20 秒で消えることを確かめる。待っている間も自動進行や操作ができることを確かめる。
4. 出力に `event=oracle_generated ... durationMs=...` が出ること、HUD の「年表」でドグマの行に見出しが添えられていることを確かめる。
5. 閉じて開き直し、年表の見出しが残っていることを確かめる。
6. Ollama を止めて（タスクトレイから終了）神託を下し、種別ごとの定型文（例: 「木を恐れよ。触れる者に災いあれ。」）が出て、出力に `event=oracle_ai_failed ... reason=connection_failed` が出ること、年表のドグマの行には見出しが添えられないことを確かめる。
7. 続けて 2 回神託を下し、吹き出しには後のほうの文が出ることを確かめる。

## 対象外

- LLM の文の良し悪し（表現の質。画面で見て、頼みの文を直す）。
