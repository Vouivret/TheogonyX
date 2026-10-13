# テスト仕様書: 多人数プレイ（神託の命令の待ち行列）

- 受け入れ条件: [docs/domain/multiplayer.md](../domain/multiplayer.md)
- 関連ADR: なし

## テスト観点

種別は「自動」「手動」「廃止」のどれか。自動テストの手順はテストコードに書き、ここには書かない。

| ID | 受け入れ条件 | 観点 | 前提 | 操作 | 期待結果 | 種別 | 実装場所 |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| TS-multiplayer-001 | AC-multiplayer-001 | 改ざんと神託の命令を順に積むと、直接操作と同じ結果になる | 受け入れ条件のとおり | 命令を積む | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/OracleCommandQueueTests.cs` |
| TS-multiplayer-002 | AC-multiplayer-002 | 同じフレームの競合は、順番の番号が小さい命令だけが効く | 受け入れ条件のとおり | 命令を 2 つ積む | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/OracleCommandQueueTests.cs` |
| TS-multiplayer-003 | AC-multiplayer-003 | 今はできない命令（夢なし・範囲外の番号・知らない記号・崩壊後）は、何も変えずに捨てて理由を返す | 受け入れ条件のとおり | 命令を積む | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/OracleCommandQueueTests.cs` |
| TS-multiplayer-004 | AC-multiplayer-004 | 同じ命令の列を 2 回再生すると、世界の状態とドグマが一致する | 受け入れ条件のとおり | 列を再生する | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/OracleCommandQueueTests.cs` |
| TS-multiplayer-005 | AC-multiplayer-005 | 命令を積んだ呼び出しのうちに待ち行列が空になり、順番の番号が続く | 受け入れ条件のとおり | 命令を積む | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/OracleCommandQueueTests.cs` |
| TS-multiplayer-006 | AC-multiplayer-001, 002 | 画面で、改ざんと神託の動きが命令の導入の前と変わらない（PC の画面とスマホ。却下の理由の返信を含む） | Godot 4.7.2 で動かす。スマホはペアリング済み | [TS-dream-tampering-008](dream-tampering.md)・[TS-dream-tampering-012](dream-tampering.md) の手順に従う | 手順どおり（命令の導入の前と同じ） | 手動 | — |

## 対象外

- AC-multiplayer-005 の「保存して読み込む」の往復: 待ち行列はセーブの形式に入れていないので、待ち行列が空であること（TS-multiplayer-005）で確かめる。セーブの往復そのものは [TS-save-data](save-data.md) が確かめている。
