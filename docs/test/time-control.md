# テスト仕様書: 時間の進め方（跳躍ボタン・自動進行・リセット）

- 受け入れ条件: [docs/domain/time-control.md](../domain/time-control.md)
- 関連ADR: なし

## テスト観点

種別は「自動」「手動」「廃止」のどれか。自動テストの手順はテストコードに書き、ここには書かない。

| ID | 受け入れ条件 | 観点 | 前提 | 操作 | 期待結果 | 種別 | 実装場所 |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| TS-time-control-001 | AC-time-control-001 | 停止中に100年跳躍すると、停止中のまま 100 年進む | 受け入れ条件のとおり | 跳躍する | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/TimeControlTests.cs` |
| TS-time-control-002 | AC-time-control-002 | 跳躍の途中で崩壊すると崩壊の状態になり、リセット以外は押せない | 受け入れ条件のとおり | 跳躍する | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/TimeControlTests.cs` |
| TS-time-control-003 | AC-time-control-003 | 自動進行中に崩壊すると、自動進行が止まる | 受け入れ条件のとおり | 1年経過させる | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/TimeControlTests.cs` |
| TS-time-control-004 | AC-time-control-004 | 崩壊からのリセットで、初期値と初期のドグマに戻る | 受け入れ条件のとおり | リセットする | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/TimeControlTests.cs` |
| TS-time-control-005 | AC-time-control-005 | 一時停止で年が進まなくなる | 受け入れ条件のとおり | 一時停止する | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/TimeControlTests.cs` |
| TS-time-control-006 | AC-time-control-006 | 自動進行中の100年跳躍で、自動進行が続く | 受け入れ条件のとおり | 跳躍する | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/TimeControlTests.cs` |
| TS-time-control-007 | AC-time-control-007 | 崩壊していなくてもリセットでき、自動進行も止まる | 受け入れ条件のとおり | リセットする | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/TimeControlTests.cs` |
| TS-time-control-008 | — | 状態遷移表で「不可」の操作は受け付けない（状態も世界も変わらない） | 各状態 | 「不可」の操作をする | 例外になり、状態・年は変わらない | 自動 | `tests/TheogonyX.Core.Tests/TimeControlTests.cs` |
| TS-time-control-009 | AC-time-control-008 | 速さの切り替えと、ボタンの有効・無効の表示 | `game/` を Godot 4.7.2 で開く | メインシーンを実行して操作する | 下の手順のとおり | 手動 | — |

## 手動テストの手順

### TS-time-control-009
1. Godot 4.7.2（.NET 版）で `game/project.godot` を開き、F5 でメインシーンを実行する。
2. 自動進行をオンにし、HUD の年が1秒に1ずつ進み、地表がそれに合わせて変わることを確かめる。
3. 高速に切り替え、年が1秒におよそ 10 進むことを確かめる。
4. 一時停止を押し、年が止まることを確かめる。
5. 星が崩壊するまで進め、自動進行が止まること、100年跳躍と自動進行のボタンが押せなくなること、リセットボタンが赤く点滅することを確かめる。
6. リセットを押し、「創世紀 0 年」・全面の密林・初期のドグマに戻ることを確かめる。
7. 出力パネルにエラー・警告が無いことを確かめる。

## 対象外

- 自動進行の間隔の正確さ（フレームレートによる誤差）: 「およそ」で確かめる。
