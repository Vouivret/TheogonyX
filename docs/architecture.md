# アーキテクチャ概要

システムの今の姿を書く。常に最新に書き換え、各項目から根拠のADRへリンクする。経緯はADRに任せ、ここには書かない。

> 現在はコードが無く、下は最初のマイルストーン（MVP）で作る予定の構成である。作ったら今の姿に書き換える。

## 全体構成

```mermaid
flowchart LR
    subgraph PC["PCホスト（Godot 4 / C#）"]
        Game["game/<br>ノード・描画・入力・WebSocketサーバー"]
        Core["src/TheogonyX.Core<br>シミュレーション（Godot非依存）"]
        Data[("game/data<br>ゲームデータ定義")]
        Save[("セーブデータ")]
        Game --> Core
        Game --> Data
        Game --> Save
    end
    subgraph Mobile["モバイル（PWA）"]
        Companion["companion/<br>神託インターフェース（技術は未定）"]
    end
    Companion <-- "WebSocket（同じLAN）" --> Game
```

## 主な処理の流れ

```mermaid
sequenceDiagram
    participant P as プレイヤー
    participant G as game/（Godot）
    participant C as Core
    P->>G: 夢の1コマを概念カードとスワップ
    G->>C: 改ざんした夢を渡す
    C->>C: InterpretDream（隣接ノードからドグマを生成）
    P->>G: 時代跳躍（N年）
    G->>C: SimulateTimeSkip(years)
    C-->>G: 新しい WorldState と出来事の一覧
    G->>P: 環境の変化を描画
```

## 主な設計方針
| 方針 | 根拠 |
| :--- | :--- |
| シミュレーションを Godot 非依存の C# ライブラリに分け、xUnit でテストする | 未記録（ADR化の候補） |
| 調整値はコードに書かず、ゲームデータ定義に置く | 未記録 |
| コンパニオンは同じLANの WebSocket でホストに接続する | 未記録 |

## 共通部品
新しく作る前に、ここにあるものを使う。共通部品を追加したら、この表にも追加する。

| 種類 | 置き場所 | 例 |
| :--- | :--- | :--- |
| UIのテーマ | `game/ui/theme/` | 色・余白・フォント |
| UIの共通シーン | `game/ui/common/` | （まだ無い） |

## ディレクトリと責務
| ディレクトリ | 責務 |
| :--- | :--- |
| `src/TheogonyX.Core/` | モデル・夢の解釈・ドグマ・時代跳躍・セーブ形式・通信メッセージの型。Godot に依存しない |
| `tests/TheogonyX.Core.Tests/` | コアの自動テスト（xUnit） |
| `game/` | Godot プロジェクト（`project.godot`）。シーン・描画・入力・ファイル入出力・WebSocket サーバー |
| `game/data/` | ゲームデータ定義（記号・閾値など） |
| `game/locales/` | 翻訳ファイル |
| `companion/` | モバイルのコンパニオン（技術は未定） |
| `docs/protocol/` | ホストとコンパニオンの通信の契約 |
