# テスト仕様書: 画面の効果（ライティング・ポストプロセス・水と大気）

- 受け入れ条件: [docs/domain/visual-effects.md](../domain/visual-effects.md)
- 関連ADR: なし

## テスト観点

種別は「自動」「手動」「廃止」のどれか。自動テストの手順はテストコードに書き、ここには書かない。今は焚き火だけ。

| ID | 受け入れ条件 | 観点 | 前提 | 操作 | 期待結果 | 種別 | 実装場所 |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| TS-visual-effects-001 | AC-visual-effects-001 | 夕焼けより前と朝より後は消えている（境目の 3 秒・9 秒を含む） | 受け入れ条件のとおり | 焚き火の明るさを求める | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/CampfireLightTests.cs` |
| TS-visual-effects-002 | AC-visual-effects-002 | 夕焼けから夜へ明るくなり、夜から朝へ暗くなる（線形補間） | 受け入れ条件のとおり | 焚き火の明るさを求める | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/CampfireLightTests.cs` |
| TS-visual-effects-003 | AC-visual-effects-003 | 12 秒を超えると周期が繰り返す | 受け入れ条件のとおり | 焚き火の明るさを求める | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/CampfireLightTests.cs` |
| TS-visual-effects-004 | AC-visual-effects-004 | カオス度では変わらない | 受け入れ条件のとおり | 焚き火の明るさを求める | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/CampfireLightTests.cs` |
| TS-visual-effects-005 | AC-visual-effects-005 | 崩壊していると、夜でも消えている | 受け入れ条件のとおり | 焚き火の明るさを求める | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/CampfireLightTests.cs` |
| TS-visual-effects-006 | AC-visual-effects-006, 007 | 焚き火の位置、夜だけ灯ること、揺らぎ、崩壊とリセットの見た目 | `game/` を Godot 4.7.2 で開く | メインシーンを実行して眺める | 下の手順のとおり | 手動 | — |

## 手動テストの手順

### TS-visual-effects-006
1. Godot 4.7.2（.NET 版）で `game/project.godot` を開き、F5 でメインシーンを実行する。
2. 予言者の左（地表の左から 12 列目の上）に薪があり、足元が地表の上端に接していることを確かめる。
3. 青空のあいだは炎も明かりも無く、夕焼けから炎と明かりが灯り、夜にいちばん明るくなり、朝に消えることを確かめる（12 秒で 1 周）。
4. 灯っているあいだ、明かりが揺らぐことを確かめる。
5. 星を崩壊させ、炎と明かりが消えて薪だけが残ることを確かめる。リセットを押し、夜になるとまた灯ることを確かめる。
6. 出力パネルにエラー・警告が無いことを確かめる。

## 対象外

- 焚き火以外の効果（ライト・ポストプロセス・水と大気）: 値が決まってから足す。
