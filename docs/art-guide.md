# アセットと画面の規格

ドット絵のアセットと、Godot の画面設定の規格。アセットを作る・取り込むときはこれに合わせる。取り込んだアセットの出典とライセンスは [credits.md](credits.md) に記録する。

出典: `memo/asset.md` の案を、2026-10-11 のチャットで確定。

## ドット絵
| 項目 | 規格 |
| :--- | :--- |
| タイル・スプライトの大きさ | 16 × 16 px |
| カラーパレット | DB16（Dawnbringer 16）。取り込むときは出典とライセンスを `credits.md` に記録する |
| テクスチャのフィルタ | Nearest（ミップマップなし。`.agent/rules/godot.md`） |

## 画面（`project.godot`）
| 設定 | 値 |
| :--- | :--- |
| ビューポートの幅（`display/window/size/viewport_width`） | 480 |
| ビューポートの高さ（`display/window/size/viewport_height`） | 270 |
| 拡大のモード（`display/window/stretch/mode`） | `viewport` |
| 縦横比（`display/window/stretch/aspect`） | `keep` |
| 拡大率（`display/window/stretch/scale_mode`） | `integer`（整数倍だけ） |

480×270 の画面を整数倍に拡大して表示し、ドットの幅をそろえる（ピクセルパーフェクト）。ウィンドウの大きさによっては、上下左右に黒い帯が出る。ドット絵の品質を優先し、これを受け入れる。
