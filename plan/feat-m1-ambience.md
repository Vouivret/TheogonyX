# plan: M1 画面の演出（昼夜の環境光と予言者）

## 目的
昼夜の環境光と、待機アニメーションをする予言者で、星を「生きたアクアリウム」として眺められるようにする（[ロードマップ](../docs/roadmap.md) の M1「2D ライティングとドット絵の予言者」）。

## スコープ
- 含む
  - 昼夜の環境光、カオス度による色の変化、崩壊時の赤黒（[仕様書](../docs/domain/ambience.md)）
  - 予言者 1 体（待機アニメーション、崩壊時の倒れた姿）
  - 予言者のスプライトの規格を [art-guide.md](../docs/art-guide.md) に追加
  - 本番の絵が届くまでの、コードで作る仮の絵
- 含まない: 焚き火の明かり（`PointLight2D`）、夜空の星、予言者の移動、本番の予言者の絵

## 決定事項
| 決定 | 理由 | ADR化 |
| :--- | :--- | :--- |
| 環境光の色の計算はコアの `AmbientLight.Compute(経過秒, 世界, 定義)` に置き、`LightColor`（RGB を 0〜1 の float で持つ）を返す | 補間とカオス度・崩壊の分岐を xUnit で確かめる。地表の判定（`TerrainClassifier`）と同じ置き方 | しない |
| 昼夜の経過時間は Godot 側（`AmbienceView`）が `_Process` の delta で数え、崩壊している間は数えない | コアは時刻を持たない（決定性）。止めた時間から続けるのでリセット後の「そのまま継続」になる | しない |
| 色・周期・予言者の列・1コマの秒数は `game/data/rules.json` の `ambience`・`prophet` に置く。色は `#RRGGBB` の文字列で書き、`GameDataLoader` で `LightColor` に直す | 調整値を直書きしない。DB16 の色と見比べやすい | しない |
| 予言者は `Sprite2D`（`Hframes` = 横幅 ÷ 縦の高さ）で描き、コマ送りは `AmbienceView` が経過時間から決める。絵は `res://assets/sprites/prophet_idle.png`・`prophet_fallen.png` があれば読み、無ければコードで単色の仮の絵を作る | 絵を差し替えるだけで反映できる。`.tres` の `uid` を手で作らない | しない |
| `main.tscn` に `CanvasModulate`（`Ambience`）と `Sprite2D`（`Prophet`）をテキストで足す。Godot で開いて保存してもらうまで「未確認」とする | `godot.md` | しない |

## 処理の流れ

### 環境光 `AmbientLight.Compute(経過秒, 世界, 定義)`
- 世界が崩壊していれば、赤黒の色を返す。
- 経過秒を周期（12 秒）で割った余りを求める。
- 1 区間の長さ = 周期 ÷ 色の数（4 → 3 秒）。余りから区間の番号 i と区間の中の割合 f を求める。
- 昼夜の色 = 色[i] から 色[(i+1) mod 4] へ、割合 f で線形補間。
- カオス度 ÷ 100（0〜1 に収める）の割合で、昼夜の色から紫赤へ線形補間して返す。

### Godot のメインシーン（`AmbienceView`）
- 演出は、地表と HUD を描く `WorldScreen` とは別の、ノードではない C# のクラス `AmbienceView` にする。`WorldCore` が作り、`_Process` から呼ぶ。
- 起動時: 予言者の絵（無ければ仮の絵）を読み、地表の左から 8 列目・一番上の行のすぐ上に置く。
- 毎フレーム（`WorldCore._Process` から呼ぶ。ログは出さない）:
  - 崩壊していなければ、昼夜の経過時間と予言者のコマの経過時間を delta だけ進める。
  - `AmbientLight.Compute` の結果を `CanvasModulate.Color` にする。
  - 予言者: 崩壊していれば倒れた絵、そうでなければ立った絵の（経過時間 ÷ 0.5 秒）mod コマ数 のコマを表示する。

## テスト観点
- [docs/test/ambience.md](../docs/test/ambience.md)（自動 5件、手動 1件）
- テスト用の定義は `tests/` の中で組み立て、`game/data/` を読まない。

## 未決
なし。
