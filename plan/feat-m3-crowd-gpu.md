# plan: M3 群衆の GPU 版（Compute Shader）と計測

## 目的
群衆（[crowd.md](../docs/domain/crowd.md)）の計算を GPU（Compute Shader）でも行えるようにし、CPU 版（`Crowd`）と突き合わせる（TS-crowd-009）。10,000 人で 60fps を保つかを測る（TS-crowd-011、[nfr.md](../docs/nfr.md)）。[ロードマップ](../docs/roadmap.md) の M3。

## スコープ
- 含む
  - 計測の仕組み（`-- --crowd-bench` で起動）と、CPU 版の計測
  - GPU 版と、CPU 版との突き合わせのコマンド（計測の結果を見て、作るかを決める）
- 含まない
  - Boids・衝突回避（GPU 版の後に決める）
  - GPU の中で MultiMesh に直接書くこと（読み戻しで足りなくなったら）

## 決定事項
| 決定 | 理由 | ADR化 |
| :--- | :--- | :--- |
| GPU で計算した位置は CPU に読み戻し、MultiMesh に書く | 人間が決めた（2026-10-12）。作りが単純で、CPU 版と突き合わせやすい | しない |
| GPU を標準にし、Compute Shader が使えない環境では CPU 版に戻して Warn を出す。データ定義で CPU 版に固定もできる | 人間が決めた（2026-10-12） | しない |
| 突き合わせは検証用のコマンドで行い、差の最大値と合否（終了コード）を出す。CI では動かさない（headless では GPU が使えない） | 人間が決めた（2026-10-12） | しない |
| 先に CPU 版を計測し、その後で GPU 版を作って同じ仕組みで比べる | 人間が決めた（2026-10-12） | しない |
| 10,000 人では GPU 版のほうが遅いが、標準は GPU のままにする | 人間が決めた（2026-10-12）。人数を増やす・Boids を足すなら GPU が有利 | しない |
| ログのキー `backend`・`count`・`steps`・`phase`・`maxDiff`・`result`・`stepMs`・`drawMs`・`fps` を `project.md` の表に足す | 人間が決めた（2026-10-12） | しない |
| **AI の案**: 計測は新しいシーンではなく、起動の引数（`-- --crowd-bench`）で `WorldCore` がゲームの代わりに `CrowdBench` ノードを置く。セーブは読み書きしない | `.tscn` の `ext_resource` の `id` を手で作らずに済む（`godot.md`） | しない |

## 処理の流れ（計測）
- `WorldCore._Ready` の最初で引数を見て、`--crowd-bench` があれば、子のノードを消して `CrowdBench` を置き、ゲームは始めない。
- `CrowdBench`: 垂直同期とフレームレートの上限を外し、初期値の世界の地表で 10,000・50,000・100,000 人を順に、散策で 1 フレームに 1 歩ずつ動かす。1 秒の慣らしの後の 5 秒で、1 歩の計算・バッファの書き込み・フレームレートの平均をログに出し、最後に終わる。

## 計測の結果（CPU 版、2026-10-12、Debug ビルド、開発者の PC）

| 人数 | 1 歩の計算 | バッファの書き込み | フレームレート |
| :--- | :--- | :--- | :--- |
| 10,000 | 0.31 ms | 0.20 ms | 1,702 fps |
| 50,000 | 1.51 ms | 1.17 ms | 359 fps |
| 100,000 | 3.02 ms | 2.33 ms | 182 fps |

- 10,000 人では、CPU 版で 60fps の目標（16.7 ms）に対して約 0.5 ms しか使っていない。
- 高速（3 倍、1 フレームに 3 歩）でも、100,000 人で約 11.4 ms と、60fps に収まる見込み。

## 処理の流れ（GPU 版）
- `CrowdView` を作るとき、`crowd.useGpu` が true なら `GpuCrowd.TryCreate` を呼ぶ。描画用とは別の `RenderingDevice`（`CreateLocalRenderingDevice`）を作り、`crowd_step.glsl` を読む。作れない・シェーダーを読めないときは null と理由を返し、`CrowdView` は Warn（`event=crowd_gpu_unavailable reason=...`）を出して CPU 版を使う。
- GPU 版の始めの状態（位置・向き・狂乱で向かう点・歩数）は、CPU 版の `Crowd` から送る。世界再創生でも、`Crowd.Reset` の後に送り直す。地表が変わったら、ポテンシャルフィールドを送る。
- 毎フレーム: 進める歩数（0〜8）と状態を CPU で決め、GPU に歩数分の dispatch を 1 回の送り出しでまとめて投げる（歩の間に barrier）。終わるまで待ち、位置を読み戻して MultiMesh に書く。停止なら何もしない。
- 終了時（`WorldCore._ExitTree`）に、GPU の資源と `RenderingDevice` を解放する。
- 突き合わせ（`-- --crowd-check`）: 8 歩ごとに CPU 版の状態を GPU に送り直して同じ 8 歩を進め、差の最大値を比べる（長く動かすと分岐で差が広がるため。[実施記録](../docs/test/results/2026-10-12-feat-m3-crowd-gpu.md)）。

## GPU 版の計測の結果（2026-10-12）
- 10,000 人では GPU 版（0.69 ms）が CPU 版（0.31 ms）より遅く、50,000 人以上で速い（[実施記録](../docs/test/results/2026-10-12-feat-m3-crowd-gpu.md)）。どちらも 60fps の目標は満たす。

## テスト観点
- TS-crowd-011: `-- --crowd-bench`（CPU 版・GPU 版）。合格。
- TS-crowd-009: `-- --crowd-check`。合格。仕様書の AC-crowd-012 を廃止し、AC-crowd-021（同じ状態から 8 歩）に置き換えた。

## 未決
- 推奨スペックの PC（`nfr.md` の性能の目標が「例」のまま）。
