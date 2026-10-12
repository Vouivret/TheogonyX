# テスト仕様書: 星の表示（地表タイルと HUD）

- 受け入れ条件: [docs/domain/planet-display.md](../domain/planet-display.md)
- 関連ADR: なし

## テスト観点

種別は「自動」「手動」「廃止」のどれか。自動テストの手順はテストコードに書き、ここには書かない。

| ID | 受け入れ条件 | 観点 | 前提 | 操作 | 期待結果 | 種別 | 実装場所 |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| TS-planet-display-001 | AC-planet-display-001, AC-planet-display-003, AC-planet-display-004 | 複数の条件に当てはまるとき、優先度の高い種別になる | 受け入れ条件のとおり | 判定する | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/TerrainClassifierTests.cs` |
| TS-planet-display-002 | AC-planet-display-002, AC-planet-display-005 | 条件の境界ちょうどの値を含む | 受け入れ条件のとおり | 判定する | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/TerrainClassifierTests.cs` |
| TS-planet-display-003 | AC-planet-display-006, AC-planet-display-016 | どの条件にも当てはまらなければ、16 列すべて草地 | 受け入れ条件のとおり | 判定する | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/TerrainClassifierTests.cs` |
| TS-planet-display-004 | AC-planet-display-007, AC-planet-display-008 | 砂漠の列数 | 受け入れ条件のとおり | 列を決める | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/TerrainClassifierTests.cs` |
| TS-planet-display-005 | AC-planet-display-009 | 0.5 ちょうどは切り上げる（偶数丸めにしない） | 受け入れ条件のとおり | 列を決める | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/TerrainClassifierTests.cs` |
| TS-planet-display-006 | AC-planet-display-010, AC-planet-display-013, AC-planet-display-020 | 計算上 0 列でも、条件に当てはまれば最小の 1 列にする | 受け入れ条件のとおり | 列を決める | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/TerrainClassifierTests.cs` |
| TS-planet-display-007 | AC-planet-display-011, AC-planet-display-012 | 氷河の列数（途中の値と、16 列全滅） | 受け入れ条件のとおり | 列を決める | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/TerrainClassifierTests.cs` |
| TS-planet-display-008 | AC-planet-display-014, AC-planet-display-015 | 焦土の列数（途中の値と、16 列全滅） | 受け入れ条件のとおり | 列を決める | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/TerrainClassifierTests.cs` |
| TS-planet-display-009 | AC-planet-display-018, AC-planet-display-019 | 密林の列数（初期値の世界は全面が密林） | 受け入れ条件のとおり | 列を決める | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/TerrainClassifierTests.cs` |
| TS-planet-display-010 | AC-planet-display-017 | HUD の年の表記と、地表の描画 | `game/` を Godot 4.7.2 で開く | メインシーンを実行し、100年跳躍ボタンを押す | 下の手順のとおり | 手動 | — |
| TS-planet-display-011 | AC-planet-display-021〜AC-planet-display-027 | 湖の広がり（水資源に比例、0.5 の切り上げ、干上がる）と異常の列との重なり（氷河・焦土は上書き、砂漠・密林は残る） | 受け入れ条件のとおり | マスごとのタイル種別を決める | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/TerrainClassifierTests.cs` |
| TS-planet-display-012 | AC-planet-display-028 | 画面での水場 | `game/` を Godot 4.7.2 で開く | 下の手順のとおり | 下の手順のとおり | 手動 | — |
| TS-planet-display-013 | — （データ定義の検証） | 湖のマスが地表の外・重複していたら、地表の定義を作れない | 湖のマスの定義 | 地表の定義を作る | 例外になる（起動時に読み込みが失敗する） | 自動 | `tests/TheogonyX.Core.Tests/TerrainClassifierTests.cs` |

## 手動テストの手順

### TS-planet-display-010
1. Godot 4.7.2（.NET 版）で `game/project.godot` を開き、F5 でメインシーンを実行する。
2. 起動直後に、HUD の年の欄が「創世紀 0 年」であること、地表の 16 × 9 タイルが、湖の場所の水場を除いてすべて密林の色であることを確かめる。
3. HUD に、カオス度・調和度・ドグマの一覧（初期の改ざんで生まれたドグマ）が出ていることを確かめる。
4. 100年跳躍ボタンを押し、年の欄が「創世紀 100 年」になること、地表が列ごと（縦の9行がそろって）に塗られていることを確かめる。
5. ウィンドウの大きさを変え、ドットの幅がそろったまま整数倍で拡大され、余りが黒い帯になることを確かめる。
6. 出力パネルにエラー・警告が無いことを確かめる。

### TS-planet-display-012
1. メインシーンを実行し、起動直後に湖の場所（`terrain.lake.cells`）のマスがすべて青い水場であることを確かめる。
2. 水資源が減るドグマを作って時間を進め、湖が並びの後ろのマスから草地・砂漠に戻って小さくなることを確かめる。
3. 気温が 0℃ 以下（または 50℃ 以上）になって異常の列が湖に届いたら、そのマスが氷河（焦土）になることを確かめる。

## 対象外

- 各タイル種別の色・絵の良し悪し: 仮の色で作るため、アセットを入れるときに確かめる。
