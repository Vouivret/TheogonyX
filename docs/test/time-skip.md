# テスト仕様書: 時代跳躍

- 受け入れ条件: [docs/domain/time-skip.md](../domain/time-skip.md)
- 関連ADR: なし

## テスト観点

種別は「自動」「手動」「廃止」のどれか。自動テストの手順はテストコードに書き、ここには書かない。

| ID | 受け入れ条件 | 観点 | 前提 | 操作 | 期待結果 | 種別 | 実装場所 |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| TS-time-skip-001 | AC-time-skip-001 | 上限のクランプ（森林量 100.0 で止まる） | 受け入れ条件のとおり | 跳躍する | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/WorldSimulatorTests.cs` |
| TS-time-skip-002 | AC-time-skip-002 | 強度に比例した変化（強度 0.5） | 受け入れ条件のとおり | 跳躍する | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/WorldSimulatorTests.cs` |
| TS-time-skip-003 | AC-time-skip-003 | 下限のクランプ（森林量 0.0）と、片方だけ 0 では崩壊しないこと | 受け入れ条件のとおり | 跳躍する | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/WorldSimulatorTests.cs` |
| TS-time-skip-004 | AC-time-skip-004 | カオス度による崩壊で、その年に中断する | 受け入れ条件のとおり | 跳躍する | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/WorldSimulatorTests.cs` |
| TS-time-skip-005 | AC-time-skip-005 | 気温による崩壊 | 受け入れ条件のとおり | 跳躍する | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/WorldSimulatorTests.cs` |
| TS-time-skip-006 | AC-time-skip-006 | 表に無い組み合わせは環境を変えない | 受け入れ条件のとおり | 跳躍する | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/WorldSimulatorTests.cs` |
| TS-time-skip-007 | AC-time-skip-007 | 複数ドグマの変化量の合算 | 受け入れ条件のとおり | 跳躍する | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/WorldSimulatorTests.cs` |
| TS-time-skip-008 | AC-time-skip-008 | 不毛（水資源と森林量がともに 0）による崩壊 | 受け入れ条件のとおり | 跳躍する | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/WorldSimulatorTests.cs` |
| TS-time-skip-009 | AC-time-skip-009 | 崩壊後の跳躍は何も変えない | 受け入れ条件のとおり | 跳躍する | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/WorldSimulatorTests.cs` |
| TS-time-skip-010 | AC-time-skip-010 | ドグマが跳躍をまたいで効き続ける（分割しても結果が同じ） | 受け入れ条件のとおり | 跳躍する | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/WorldSimulatorTests.cs` |
| TS-time-skip-011 | — | 決定性: 同じ入力で2回実行すると、すべての値が一致する | 木の破壊と雨の破壊 | 100年跳躍を2回、別々の世界で行う | 2つの世界の状態が一致 | 自動 | `tests/TheogonyX.Core.Tests/WorldSimulatorTests.cs` |
| TS-time-skip-012 | — | Godot 上でコアループが動く（`WorldCore` の出力） | `game/` を Godot 4.7.2 で開く | メインシーンを実行する（F5） | 出力に改ざん前後の夢、生成されたドグマ、100年後の世界の状態が出て、エラーが無い | 手動 | — |

## 手動テストの手順

### TS-time-skip-012
1. Godot 4.7.2（.NET 版）で `game/project.godot` を開く。
2. F5 でメインシーンを実行する。
3. 出力パネルに、`event=` で始まるログ（夢の改ざん、`dogma_formed`、跳躍後の世界の状態）が出ることを確かめる。
4. エラー・警告が出ていないことを確かめる。

## 対象外

- 浮動小数点の累積誤差: 期待値の比較は許容誤差 0.001 で行う（`.agent/rules/testing.md`）。
