# テスト仕様書: 文明の創発的な発展（グラデーションと変種技術）

- 受け入れ条件: [docs/domain/emergent-progression.md](../domain/emergent-progression.md)
- 関連ADR: なし

## テスト観点

種別は「自動」「手動」「廃止」のどれか。自動テストの手順はテストコードに書き、ここには書かない。

| ID | 受け入れ条件 | 観点 | 前提 | 操作 | 期待結果 | 種別 | 実装場所 |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| TS-emergent-progression-001 | AC-emergent-progression-001 | 地域の数、列から地域、地域のいちばん左の列（端と途中） | 受け入れ条件のとおり | 地域を求める | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/RegionMapTests.cs` |
| TS-emergent-progression-002 | AC-emergent-progression-002 | 地表の外の列と、無い地域はエラー | 受け入れ条件のとおり | 地域を求める | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/RegionMapTests.cs` |
| TS-emergent-progression-003 | AC-emergent-progression-003 | 割り切れない列の数はエラー | 受け入れ条件のとおり | 地域の区切りを作る | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/RegionMapTests.cs` |
| TS-emergent-progression-004 | AC-emergent-progression-004 | 自然な伸び | 受け入れ条件のとおり | 1 年進める | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/CivilizationSimulatorTests.cs` |
| TS-emergent-progression-005 | AC-emergent-progression-005 | ドグマの補正・距離による弱まり・禁忌・1 で止まる | 受け入れ条件のとおり | 1 年進める | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/CivilizationSimulatorTests.cs` |
| TS-emergent-progression-006 | AC-emergent-progression-006 | 環境の倍率（悪いほう・水場） | 受け入れ条件のとおり | 倍率を求める・1 年進める | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/CivilizationSimulatorTests.cs` |
| TS-emergent-progression-007 | AC-emergent-progression-007 | 閾値を越えると建つ（1 つ・2 つ・2 回目は建たない） | 受け入れ条件のとおり | 1 年進める | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/CivilizationSimulatorTests.cs` |
| TS-emergent-progression-008 | AC-emergent-progression-008 | 建てられない場所（水場・密林・埋まった枠） | 受け入れ条件のとおり | 1 年進める | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/CivilizationSimulatorTests.cs` |
| TS-emergent-progression-009 | AC-emergent-progression-009 | 決定性（10,000 年） | 受け入れ条件のとおり | 100 年跳躍を 100 回 | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/CivilizationSimulatorTests.cs` |
| TS-emergent-progression-010 | — | 建物の見た目 | `game/` を Godot 4.7.2 で開く | 下の手順 | 下の手順のとおり | 手動 | — |

### TS-emergent-progression-010

1. セーブを消して起動する。森林量 50 で、湖のほかは草地になっている。
2. 100 年跳躍を続ける。ドグマが無ければ 1,000 年ほどで、どの地域にも工房（歯車のレンガの建物）と祠（石の神殿）が建ち始める。崇拝・破壊のドグマを下すと、予言者の近くの地域から早く建つ。
3. 建物は 2 × 2 マスに収まり、地表の外・水場の上に建たない。
4. 建物の下の辺が、予言者・焚き火と同じように Y ソートされる（下にあるものが手前）。
5. 終了して起動し直すと、同じ建物が同じ場所にある。
6. 世界再創生を押すと、建物が消える。

## 対象外

- 変種技術、極秘研究、パラドックス、イベント: 受け入れ条件が決まってから足す。
