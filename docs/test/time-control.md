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
| TS-time-control-004 | AC-time-control-004 | 崩壊からのリセットで、初期値に戻りドグマが空になる | 受け入れ条件のとおり | リセットする | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/TimeControlTests.cs` |
| TS-time-control-005 | AC-time-control-005 | 一時停止で年が進まなくなる | 受け入れ条件のとおり | 一時停止する | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/TimeControlTests.cs` |
| TS-time-control-006 | AC-time-control-006 | 自動進行中の100年跳躍で、自動進行が続く | 受け入れ条件のとおり | 跳躍する | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/TimeControlTests.cs` |
| TS-time-control-007 | AC-time-control-007 | 崩壊していなくてもリセットでき、自動進行も止まる | 受け入れ条件のとおり | リセットする | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/TimeControlTests.cs` |
| TS-time-control-008 | — | 状態遷移表で「不可」の操作は受け付けない（状態も世界も変わらない） | 各状態 | 「不可」の操作をする | 例外になり、状態・年は変わらない | 自動 | `tests/TheogonyX.Core.Tests/TimeControlTests.cs` |
| TS-time-control-010 | AC-time-control-009, 010, 011, 012, 013 | 自動進行の時計（速さごとの 1 年の秒数、一時停止しても進みが残る、切り替えで 0、1 フレームに 1 年まで、3 段で回る） | 受け入れ条件のとおり | 時間を進める・切り替える | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/AutoPlayClockTests.cs` |
| TS-time-control-011 | AC-time-control-009 | 残り時間の表示と、閉じて起動した後の続き | `game/` を Godot 4.7.2 で開く | 下の手順 | 下の手順のとおり | 手動 | — |
| TS-time-control-009 | AC-time-control-008 | 速さの切り替えと、ボタンの有効・無効の表示 | `game/` を Godot 4.7.2 で開く | メインシーンを実行して操作する | 下の手順のとおり | 手動 | — |

## 手動テストの手順

### TS-time-control-009
1. Godot 4.7.2（.NET 版）で `game/project.godot` を開き、F5 でメインシーンを実行する。
2. 自動進行をオンにし、HUD の「次の年まで」が 10:00 付近から 1 秒ずつ減り、0 になると年が 1 進んで地表が変わることを確かめる。
3. 速さのボタンを押して「2 倍」にし、1 年が約 5 分になる（残り時間が 05:00 から始まる）ことを確かめる。もう一度押すと「3 倍」（約 3 分 20 秒）、さらに押すと「1 倍」に戻ることを確かめる。
4. 一時停止を押し、年が止まることを確かめる。
5. 星が崩壊するまで進め、自動進行が止まること、100年跳躍と自動進行のボタンが押せなくなること、リセットボタンが赤く点滅することを確かめる。
6. リセットを押し、「創世紀 0 年」・湖のほかは草地・ドグマの一覧が空（「（なし）」）に戻ることを確かめる。
7. 出力パネルにエラー・警告が無いことを確かめる。

### TS-time-control-011
1. 自動進行を押し、HUD に「次の年まで」の残り時間が 1 秒ずつ減って出ることを確かめる。1 倍・2 倍・3 倍のどれでも出る。
2. 一時停止すると残り時間が隠れ、再開すると止めたところから続くことを確かめる。
3. 数分待ってからウィンドウを閉じ、もう一度実行する。止まった状態で始まり、速さが同じまま、自動進行を押すと残り時間が閉じる前の続きから減ることを確かめる。
4. 古い速さの名前（`NORMAL`・`FAST`・`REAL_TIME`）で保存したセーブを読むと、それぞれ 1 倍・2 倍・3 倍として読めることを確かめる（手で `world.json` を書き換える）。

## 対象外

- 自動進行の間隔の正確さ（フレームレートによる誤差）: 「およそ」で確かめる。
