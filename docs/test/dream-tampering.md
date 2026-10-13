# テスト仕様書: 夢の改ざん

- 受け入れ条件: [docs/domain/dream-tampering.md](../domain/dream-tampering.md)
- 関連ADR: なし

## テスト観点

種別は「自動」「手動」「廃止」のどれか。自動テストの手順はテストコードに書き、ここには書かない。

| ID | 受け入れ条件 | 観点 | 前提 | 操作 | 期待結果 | 種別 | 実装場所 |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| TS-dream-tampering-001 | AC-dream-tampering-001, 011 | 夢の生成（3 つ、記号は 4 種類のどれか、重みは範囲内。同じシードなら同じ夢） | 受け入れ条件のとおり | 夢を作る | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/DreamGeneratorTests.cs` |
| TS-dream-tampering-002 | AC-dream-tampering-002, 003 | 改ざんで記号だけが替わり、2 回目はできない | 受け入れ条件のとおり | 入れ替える | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/ProphetDreamsTests.cs` |
| TS-dream-tampering-003 | AC-dream-tampering-004, 005 | 神託でドグマが加わり、夢なしになる（改ざん前でも下せる） | 受け入れ条件のとおり | 神託を下す | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/ProphetDreamsTests.cs` |
| TS-dream-tampering-004 | AC-dream-tampering-006, 007, 008 | 100 年ごとの夢と、夢があるあいだは新しい夢を見ないこと | 受け入れ条件のとおり | 年を進める | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/ProphetDreamsTests.cs` |
| TS-dream-tampering-005 | AC-dream-tampering-009 | 崩壊しているあいだは改ざんも神託もできない | 受け入れ条件のとおり | 操作する | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/ProphetDreamsTests.cs` |
| TS-dream-tampering-006 | AC-dream-tampering-010 | 世界再創生でドグマが空になり、新しい夢を見る | 受け入れ条件のとおり | リセットする | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/ProphetDreamsTests.cs` |
| TS-dream-tampering-007 | — | 状態遷移表で「不可」の操作（夢なしでの改ざん・神託、改ざん済みでの改ざん）は受け付けない | 各状態 | 操作する | 例外になり、状態・夢・ドグマは変わらない | 自動 | `tests/TheogonyX.Core.Tests/ProphetDreamsTests.cs` |
| TS-dream-tampering-009 | AC-dream-tampering-013, 014 | 夢で自動進行が止まり、神託で再開する。止まっていたなら止まったまま。自分で再開していたら神託では何もしない | 受け入れ条件のとおり | 年を進める・神託を下す | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/ProphetDreamsTests.cs` |
| TS-dream-tampering-008 | AC-dream-tampering-012 | PC の画面での改ざんと神託 | `game/` を Godot 4.7.2 で開く | 下の手順 | 下の手順のとおり | 手動 | — |
| TS-dream-tampering-010 | AC-dream-tampering-015, 016 | `TAMPER`・`PRONOUNCE` の検証（`index` が負・小数・文字列、`symbolId` が空・長すぎる・文字列でない、項目が無い） | 受け入れ条件のとおり | パースする | 失敗し、理由が `invalid_tamper` | 廃止 | — |
| TS-dream-tampering-011 | AC-dream-tampering-015, 016 | `DREAM_STATE` に、状態・夢ノード・手札・できる操作・崩壊・依頼が入る | 夢あり（改ざん前・改ざん済み）、夢なし、依頼あり、崩壊 | 書き出す | messages.md の形のとおり | 廃止 | — |
| TS-dream-tampering-012 | AC-dream-tampering-015, 016 | スマホからの改ざんと神託、PC との同時の操作 | ホストとコンパニオンが動き、ペアリングしている | 下の手順 | 下の手順のとおり | 手動 | — |

## 手動テストの手順

### TS-dream-tampering-008
1. メインシーンを実行し、地表の下に案内の一文と夢の 3 コマが出ていることを確かめる。
2. 2 番目のコマを押し、夢のコマの段が手札の 4 枚（デッキの手札。同じ記号が重なることがある）と「やめる」に替わることを確かめる。手札にある記号を 1 つ選び、2 番目のコマがその記号に替わること、ほかのコマが押せなくなること、案内が「神託を下せ」に替わることを確かめる。
3. 「神託を下す」を押し、HUD のドグマの一覧に加わり、案内が「予言者は眠っている」になることを確かめる。
4. 自動進行（高速）にし、100 年たって夢が出ると自動進行が止まること、神託を下すと再開することを確かめる。自動進行中でも、コマと手札が毎回選べることを確かめる。
5. 崩壊させ、夢のコマと「神託を下す」が押せず、案内が「星は滅びた」になることを確かめる。

### TS-dream-tampering-012
1. スマホの映像の下に、PC と同じ案内と夢の 3 コマが出ることを確かめる。
2. スマホで 2 番目のコマを押し、PC と同じ手札の 4 枚と「やめる」に替わることを確かめる。カードを選び、PC とスマホの両方で 2 番目のコマが替わることを確かめる。
3. スマホで「神託を下す」を押し、PC の HUD のドグマの一覧に加わり、PC とスマホの両方の夢の欄が「予言者は眠っている」になることを確かめる。
4. 次の夢で、PC で改ざんしてから、スマホで別のコマを選んでカードを押す。改ざんされず、スマホの欄が PC で改ざんした夢になることを確かめる。
5. PC で神託を下し、スマホの欄も「予言者は眠っている」になることを確かめる。
6. PC のゲームを終了し、スマホの夢の欄が押せなくなることを確かめる。

## 対象外

なし。
