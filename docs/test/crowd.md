# テスト仕様書: 群衆（大量 NPC）

- 受け入れ条件: [docs/domain/crowd.md](../domain/crowd.md)
- 関連ADR: [0004 群衆の乱数](../adr/0004-crowd-counter-based-hash-random.md)

## テスト観点

種別は「自動」「手動」「廃止」のどれか。自動テストの手順はテストコードに書き、ここには書かない。

| ID | 受け入れ条件 | 観点 | 前提 | 操作 | 期待結果 | 種別 | 実装場所 |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| TS-crowd-001 | AC-crowd-001 | 始めの配置の決定性 | 同じシード | 始めの配置を 2 回作る | 全員の位置が同じ | 自動 | `tests/TheogonyX.Core.Tests/CrowdTests.cs` |
| TS-crowd-002 | AC-crowd-002 | 始めの配置の人数と範囲 | 複数のシード | 始めの配置を作る | 10,000 人、全員が地表の範囲の中 | 自動 | `tests/TheogonyX.Core.Tests/CrowdTests.cs` |
| TS-crowd-003 | AC-crowd-003, AC-crowd-004, AC-crowd-005 | 状態の判定（閾値の境界と崩壊） | カオス度 49.9 / 50 / 80（崩壊） | 状態を求める | 散策 / 狂乱 / 停止 | 自動 | `tests/TheogonyX.Core.Tests/CrowdTests.cs` |
| TS-crowd-004 | AC-crowd-006 | 散策で範囲から出ない | 散策 | 60 秒動かす | 全員が地表の範囲の中 | 自動 | `tests/TheogonyX.Core.Tests/CrowdTests.cs` |
| TS-crowd-012 | AC-crowd-016, AC-crowd-017 | ポテンシャルフィールド（散策で焦土から逃げる、狂乱ではタイルの力を受けない） | 受け入れ条件のとおり | 動かす | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/CrowdTests.cs` |
| TS-crowd-013 | AC-crowd-018, AC-crowd-019, AC-crowd-020 | 自動進行中だけ動き、高速では速い | 止まっている / 崩壊 / 通常・現実同期 / 高速 | 群衆の時間の進み方を求める | 0 / 0 / 1 / 高速のときの倍率 | 自動 | `tests/TheogonyX.Core.Tests/CrowdTests.cs` |
| TS-crowd-014 | AC-crowd-022, AC-crowd-023 | 群れの動き（範囲・決定性・動きが変わる） | 群れの動きを使う | 動かす | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/CrowdTests.cs` |
| TS-crowd-015 | AC-crowd-024 | 群れの集計（固定小数点・3 × 3 のマス） | 受け入れ条件のとおり | 集計する | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/FlockGridTests.cs` |
| TS-crowd-005 | AC-crowd-007 | 動きの決定性 | 同じシードの群衆 2 つ | 同じ時間・同じ状態の移り変わりで動かす | 全員の位置が同じ | 自動 | `tests/TheogonyX.Core.Tests/CrowdTests.cs` |
| TS-crowd-006 | AC-crowd-008, AC-crowd-009 | 狂乱で聖地へ向かい、留まる | 狂乱 | 1 歩・十分な時間動かす | 範囲外の人は距離が縮む。最後は全員が範囲の中 | 自動 | `tests/TheogonyX.Core.Tests/CrowdTests.cs` |
| TS-crowd-007 | AC-crowd-010 | 停止で動かない | 停止 | 動かす | 位置が変わらない | 自動 | `tests/TheogonyX.Core.Tests/CrowdTests.cs` |
| TS-crowd-008 | AC-crowd-011 | 世界再創生で始めの配置に戻る | 動かした後・崩壊 | 世界再創生 | 始めの配置と同じ、状態は散策 | 自動 | `tests/TheogonyX.Core.Tests/CrowdTests.cs` |
| TS-crowd-009 | AC-crowd-021 | GPU 版と CPU 版の一致 | GPU が使える PC | 下の手順（`--crowd-check`） | 下の手順 | 手動 | `game/scenes/bench/CrowdCheck.cs` |
| TS-crowd-010 | AC-crowd-013, AC-crowd-014 | 画面での見た目 | メインシーン | 下の手順 | 下の手順 | 手動 | — |
| TS-crowd-011 | AC-crowd-015 | 10,000 人で 60fps | GPU が使える PC | 下の手順（`--crowd-bench`） | 下の手順 | 手動 | `game/scenes/bench/CrowdBench.cs` |

## 手動テストの手順

### TS-crowd-010
1. Godot でメインシーンを実行する。止まった状態で始まり、群衆も止まっていることを確かめる。自動進行を押す。
2. 地表の上を点の群衆がばらばらに歩き回り、水場・緑地に集まりやすいこと、地表の外に出ないこと、HUD の上に描かれないことを確かめる。
3. 12 秒の昼夜の周期で、群衆の色も地表と同じように変わることを確かめる。
4. 夢の改ざんと神託でカオス度を 50 以上にし、群衆が一斉に予言者の足元へ押し寄せ、周りに留まることを確かめる。
5. 群れの動きを使うとき、群衆が向きをそろえた群れになって流れ、混み合いすぎず・まばらになりすぎないことを確かめる。`crowd.flock.enabled` を false にして見比べる。
6. 一時停止を押すと群衆が止まり、自動進行で動き出すことを確かめる。速さを高速にすると、群衆も速く動くことを確かめる。
7. 星を崩壊させ、群衆がその場で止まることを確かめる。世界再創生で始めの配置に戻り、歩き出すことを確かめる。

### TS-crowd-009
GPU を使うので、CI（headless）では動かせない。GPU のある PC で、ウィンドウ付きの Godot で実行する。
1. 次のコマンドを実行する（Godot のウィンドウが数秒開いて閉じる）。
   ```bash
   "/c/Tools/Godot/Godot_v4.7.2-stable_mono_win64/Godot_v4.7.2-stable_mono_win64_console.exe" --path game -- --crowd-check
   ```
2. 区切りごとに `event=crowd_check phase=... maxDiff=... result=pass` が出て（`rules.json` の `crowd.flock.enabled` が true なら群れの動きも比べる）、終了コードが 0 であることを確かめる（1 は不合格、2 は GPU が使えなかった）。
3. `crowd_step.glsl`・`GpuCrowd.cs`・`Crowd.cs` を変えたら、必ず実行する。

### TS-crowd-011
1. 次のコマンドを実行する（Godot のウィンドウが 30 秒ほど開いて閉じる）。
   ```bash
   "/c/Tools/Godot/Godot_v4.7.2-stable_mono_win64/Godot_v4.7.2-stable_mono_win64_console.exe" --path game -- --crowd-bench
   ```
2. CPU 版・GPU 版それぞれ 10,000・50,000・100,000 人の `event=crowd_bench ... fps=...` が出る。10,000 人の `fps` が 60 以上であることを確かめる（垂直同期を外して測るので、60 を大きく超えてよい）。
3. 結果を `docs/test/results/` に記録する。

## 対象外

- 仮の調整値（速さ・範囲・点の色）での見た目の良し悪し: 値が決まってから見る。
