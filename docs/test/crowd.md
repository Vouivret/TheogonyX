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
| TS-crowd-005 | AC-crowd-007 | 動きの決定性 | 同じシードの群衆 2 つ | 同じ時間・同じ状態の移り変わりで動かす | 全員の位置が同じ | 自動 | `tests/TheogonyX.Core.Tests/CrowdTests.cs` |
| TS-crowd-006 | AC-crowd-008, AC-crowd-009 | 狂乱で聖地へ向かい、留まる | 狂乱 | 1 歩・十分な時間動かす | 範囲外の人は距離が縮む。最後は全員が範囲の中 | 自動 | `tests/TheogonyX.Core.Tests/CrowdTests.cs` |
| TS-crowd-007 | AC-crowd-010 | 停止で動かない | 停止 | 動かす | 位置が変わらない | 自動 | `tests/TheogonyX.Core.Tests/CrowdTests.cs` |
| TS-crowd-008 | AC-crowd-011 | 世界再創生で始めの配置に戻る | 動かした後・崩壊 | 世界再創生 | 始めの配置と同じ、状態は散策 | 自動 | `tests/TheogonyX.Core.Tests/CrowdTests.cs` |
| TS-crowd-009 | AC-crowd-012 | GPU 版と CPU 版の一致 | 同じシード | 両方で同じ時間動かす | 位置の差が許容誤差の中 | 手動 | — （GPU 版を作る段階で決める） |
| TS-crowd-010 | AC-crowd-013, AC-crowd-014 | 画面での見た目 | メインシーン | 下の手順 | 下の手順 | 手動 | — |
| TS-crowd-011 | AC-crowd-015 | 10,000 人で 60fps | 計測用のシーン | 下の手順 | 下の手順 | 手動 | — （GPU 版を作る段階で決める） |

## 手動テストの手順

### TS-crowd-010
1. Godot でメインシーンを実行する。止まった状態で始まり、群衆も止まっていることを確かめる。自動進行を押す。
2. 地表の上を点の群衆がばらばらに歩き回り、水場・緑地に集まりやすいこと、地表の外に出ないこと、HUD の上に描かれないことを確かめる。
3. 12 秒の昼夜の周期で、群衆の色も地表と同じように変わることを確かめる。
4. 夢の改ざんと神託でカオス度を 50 以上にし、群衆が一斉に予言者の足元へ押し寄せ、周りに留まることを確かめる。
5. 一時停止を押すと群衆が止まり、自動進行で動き出すことを確かめる。速さを高速にすると、群衆も速く動くことを確かめる。
6. 星を崩壊させ、群衆がその場で止まることを確かめる。世界再創生で始めの配置に戻り、歩き出すことを確かめる。

### TS-crowd-011
1. 計測用のシーンで 10,000 人の群衆を動かす。
2. Godot のモニター（Debugger > Monitors）でフレームレートを見て、60fps を保つことを確かめる。

## 対象外

- 仮の調整値（速さ・範囲・点の色）での見た目の良し悪し: 値が決まってから見る。
