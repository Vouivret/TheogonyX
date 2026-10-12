# テスト仕様書: 文明の創発的な発展（グラデーションと変種技術）

- 受け入れ条件: [docs/domain/emergent-progression.md](../domain/emergent-progression.md)
- 関連ADR: なし

## テスト観点

種別は「自動」「手動」「廃止」のどれか。自動テストの手順はテストコードに書き、ここには書かない。今は地域の計算だけ。

| ID | 受け入れ条件 | 観点 | 前提 | 操作 | 期待結果 | 種別 | 実装場所 |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| TS-emergent-progression-001 | AC-emergent-progression-001 | 地域の数、列から地域、地域のいちばん左の列（端と途中） | 受け入れ条件のとおり | 地域を求める | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/RegionMapTests.cs` |
| TS-emergent-progression-002 | AC-emergent-progression-002 | 地表の外の列と、無い地域はエラー | 受け入れ条件のとおり | 地域を求める | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/RegionMapTests.cs` |
| TS-emergent-progression-003 | AC-emergent-progression-003 | 割り切れない列の数はエラー | 受け入れ条件のとおり | 地域の区切りを作る | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/RegionMapTests.cs` |

## 対象外

- 技術・文化の値、建物、変種技術、極秘研究、パラドックス: 受け入れ条件が決まってから足す。
