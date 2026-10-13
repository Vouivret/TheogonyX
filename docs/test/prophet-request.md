# テスト仕様書: 予言者の依頼（祈り）

- 受け入れ条件: [docs/domain/prophet-request.md](../domain/prophet-request.md)
- 関連ADR: なし

## テスト観点

種別は「自動」「手動」「廃止」のどれか。自動テストの手順はテストコードに書き、ここには書かない。

| ID | 受け入れ条件 | 観点 | 前提 | 操作 | 期待結果 | 種別 | 実装場所 |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| TS-prophet-request-001 | AC-prophet-request-001, 002 | 困りごとで依頼が出る | 受け入れ条件のとおり | 年を進める | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/ProphetDreamsTests.cs` |
| TS-prophet-request-002 | AC-prophet-request-003 | 複数の困りごとは優先の順 | 受け入れ条件のとおり | 年を進める | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/ProphetDreamsTests.cs` |
| TS-prophet-request-003 | AC-prophet-request-004, 008 | 神託で依頼が済み、困りごとが消えても依頼は残る | 受け入れ条件のとおり | 神託を下す・年を進める | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/ProphetDreamsTests.cs` |
| TS-prophet-request-004 | AC-prophet-request-005, 006, 007 | 同じ困りごとは 50 年あける（49 年では出ず、50 年で出る。ほかの困りごとは出る） | 受け入れ条件のとおり | 年を進める | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/ProphetDreamsTests.cs` |
| TS-prophet-request-005 | AC-prophet-request-009, 010 | 崩壊中は出ず、世界再創生で依頼と待つ年数が消える | 受け入れ条件のとおり | 年を進める・リセットする | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/ProphetDreamsTests.cs` |
| TS-prophet-request-007 | AC-prophet-request-012 | 依頼が出ると自動進行が止まる。依頼が無ければ 100 年たっても止まらない | 受け入れ条件のとおり | 年を進める | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/ProphetDreamsTests.cs` |
| TS-prophet-request-009 | AC-prophet-request-012 | 依頼で止めた自動進行は、神託を下すと再開する。自分で再開していたら、止まっていたままなら、何も変わらない | 受け入れ条件のとおり | 神託を下す | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/ProphetDreamsTests.cs` |
| TS-prophet-request-006 | AC-prophet-request-011 | PC の画面の吹き出し | `game/` を Godot 4.7.2 で開く | 下の手順 | 下の手順のとおり | 手動 | — |
| TS-prophet-request-008 | AC-prophet-request-013 | スマホの吹き出し | ホストとコンパニオンが動き、ペアリングしている | 下の手順 | 下の手順のとおり | 手動 | — |

## 手動テストの手順

### TS-prophet-request-006
1. メインシーンを実行し、水資源が 30 以下になるまで進める（夢の改ざんで、雨→血 などの水資源を減らすドグマを作る）。
2. 予言者の上に「恵みの雨をお恵みください」の吹き出しが出ることを確かめる。
3. 「神託を下す」を押し、吹き出しが消えることを確かめる。

### TS-prophet-request-008
1. TS-prophet-request-006 の手順 1 のとおり、依頼が出るまで進める。
2. スマホの映像の上にも、PC と同じ吹き出しが出ることを確かめる。
3. スマホで「神託を下す」を押し、PC とスマホの両方で吹き出しが消えることを確かめる。

## 対象外

なし。
