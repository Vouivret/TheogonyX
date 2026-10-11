# plan: M1 画面の視覚化（地表タイル・HUD・時間の進め方）

## 目的
ログでしか見えなかった星を画面に描き、100年跳躍・自動進行・リセットで変化を眺められるようにする（[ロードマップ](../docs/roadmap.md) の M1 のうち、TileMap と HUD）。

## スコープ
- 含む
  - 地表タイルの判定（[仕様書](../docs/domain/planet-display.md)）
  - 時間の進め方（[仕様書](../docs/domain/time-control.md)）
  - 画面の規格（[art-guide.md](../docs/art-guide.md)）を `project.godot` に反映
  - 単色の仮タイルで描く地表（16 × 9）と、HUD・操作ボタン
  - 翻訳ファイル（日本語・英語）
- 含まない: 2D ライティング、ドット絵の予言者、本番のタイル画像、夢を改ざんする画面

## 決定事項
| 決定 | 理由 | ADR化 |
| :--- | :--- | :--- |
| タイル種別と列数の計算はコアの `TerrainClassifier.Classify(世界, 定義)` に置き、`(TileType, 列数)` を返す | ゲームのルールを Godot 側に書かない。xUnit で確かめる | しない |
| 4つの異常（氷河・焦土・砂漠・密林）は、同じ形の定義で表す: 判定する項目・比較（以下 / 以上）・閾値・「0列の値」・「16列の値」。列数 = (値 − 0列の値) ÷ (16列の値 − 0列の値) × 16 を四捨五入し 1〜16 に収める | 仕様書の4つの式は、この形にそろう（例: 砂漠は 0列の値 100、16列の値 0） | しない |
| 四捨五入は `MidpointRounding.AwayFromZero` | `Math.Round` の既定は偶数丸めで、仕様書の「0.5 は切り上げ」と食い違う | しない |
| 地表の定義（優先度順の4行、列数 16）と時間の進め方の値（跳躍 100 年、通常 1 年/秒、高速 10 年/秒）は `game/data/rules.json` に足す | 調整値を直書きしない | しない |
| 時間の進め方はコアの `TimeControl` クラスにする。状態（`Stopped`・`AutoPlaying`・`Destroyed`）と操作（`Jump`・`StartAutoPlay`・`Pause`・`AdvanceOneYear`・`Reset`）を持ち、遷移は仕様書の状態遷移表と1対1の表（`(状態, 操作) → 遷移先` の辞書。表に無い組み合わせが「不可」）で持つ。ドグマを足す `AddDogma` も持つ（状態は変えない。今は初期のドグマとテストで使う）。「不可」の操作は `InvalidOperationException` | `project.md` 4章（状態遷移表は1か所の表で持つ） | しない |
| 自動進行の時計は Godot の `Timer` に持たせ、時間切れのたびに `AdvanceOneYear` を呼ぶ。速さの切り替えは `Timer.WaitTime` を変え、自動進行中ならタイマーを再スタートしてすぐ新しい速さにする。コアの状態は変えない | コアは時刻を持たない（決定性） | しない |
| リセット後の初期ドグマは、起動時に一度だけ初期の改ざんを解釈して作った一覧を `TimeControl` に渡し、リセットのたびにその写しに戻す | 解釈は決定的なので、毎回解釈し直すのと結果は同じ | しない |
| 仮のタイル画像は、起動時にコードで単色の 16×16 画像を作り、`TileSet` に組み立てる。色は DB16 から選ぶ | 画像ファイルと `.tres` の `uid` を手で作らずに済む。本番の絵が来たら差し替える | しない |
| `main.tscn` を作り直す（根: `Node2D`、子: `TileMapLayer`・HUD の `CanvasLayer`・`Timer`）。ノードの追加はテキストで行い、Godot で開いて保存してもらうまで「未確認」とする | `godot.md` | しない |
| UI のテーマ `game/ui/theme/main_theme.tres` を最小の形で作り、`project.godot` の `gui/theme/custom` に設定する。フォントは Godot の既定のまま | `ui.md` はテーマ経由を求めるが、テーマがまだ無い。ピクセルフォントは出典・ライセンスを決めてから入れる | しない |
| 翻訳は `game/locales/ui.csv`（`ja`・`en`）。HUD の数値は `CultureInfo` で整形する | `ui.md` | しない |
| ウィンドウの初期の大きさを 1440×810（480×270 の3倍）にする（`window_width_override`・`window_height_override`） | 480×270 のままだとウィンドウが小さすぎる | しない |

## 処理の流れ

### 地表の判定 `TerrainClassifier.Classify(世界, 地表の定義)`
- 定義の異常の行を、優先度の順に調べる。
  - 判定する項目の値が条件（閾値以下 / 以上）に当てはまったら、列数を求めて `(その種別, 列数)` を返す。下位の行は調べない。
- どれにも当てはまらなければ `(Grassland, 0)` を返す。

### 時間の進め方 `TimeControl`
- 作るとき: 世界の定義・初期ドグマを受け取り、世界を初期値で作り、ドグマの一覧を初期ドグマの写しにし、状態を `Stopped` にする。
- 各操作の最初に、遷移表で（今の状態, 操作）が「不可」なら `InvalidOperationException`（状態・世界は変えない）。
- `Jump`: 跳躍年数（定義の 100）だけ `SimulateTimeSkip`。崩壊したら `Destroyed`、そうでなければ状態はそのまま。
- `StartAutoPlay`: `AutoPlaying` にする。
- `Pause`: `Stopped` にする。
- `AdvanceOneYear`: 1 年だけ `SimulateTimeSkip`。崩壊したら `Destroyed`。
- `Reset`: 世界を初期値で作り直し、ドグマの一覧を初期ドグマの写しに戻し、`Stopped` にする。
- `CanDo(操作)` で、今の状態でその操作ができるかを返す。Godot 側はこれでボタンの有効・無効を決める。

### Godot のメインシーン（`WorldCore` と `WorldScreen`）
- `WorldCore`（シーンの根。既存のスクリプトの名前を変えずに使う）はデータを読み、初期のドグマと `TimeControl` を作り、`WorldScreen` を組み立てる。`WorldScreen`（ノードではない C# のクラス）が描画とボタンを受け持つ。ノードの参照を null にならないフィールドで持つため、こう分けた。
- `game/data/` の JSON を読む（読めなければ文脈を足した例外）。
- 初期の改ざん（木 → 雨 を 木 → 血 に）を解釈して初期ドグマを作り、`event=dogma_formed` を出す。
- `TimeControl` を作り、画面を更新する。
- ボタンを押したら `TimeControl` の操作を呼び、画面を更新する。跳躍の後は `event=time_skipped`、崩壊したら `event=world_destroyed`、リセットしたら `event=world_reset` を出す（自動進行の1年ごとにはログを出さない）。
- 画面の更新: `Classify` の結果で 16 × 9 のタイルを塗り、HUD の年・カオス度・調和度・ドグマの一覧を書き換え、ボタンの有効・無効を変え、崩壊していればリセットボタンを点滅させる。状態が `AutoPlaying` 以外になったら `Timer` を止める。

## テスト観点
- [docs/test/planet-display.md](../docs/test/planet-display.md)（自動 9件、手動 1件）
- [docs/test/time-control.md](../docs/test/time-control.md)（自動 8件、手動 1件）
- テスト用の定義は `tests/` の中で組み立て、`game/data/` を読まない。
- [docs/test/time-skip.md](../docs/test/time-skip.md) の手動テスト TS-time-skip-012 は、起動しただけでは100年跳躍しなくなるので、手順を「100年跳躍ボタンを押す」に直す。

## 未決
なし。
