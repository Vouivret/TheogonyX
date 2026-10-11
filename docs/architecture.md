# アーキテクチャ概要

システムの今の姿を書く。常に最新に書き換え、各項目から根拠のADRへリンクする。経緯はADRに任せ、ここには書かない。

> 現在あるのは MVP のコアループ（夢の解釈と時代跳躍）と、それをログに出す Godot のシーン（`game/scenes/main/`）だけである。WebSocket サーバー・コンパニオン・セーブデータ・描画は、まだ無い（下の図では予定として描いている）。

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
    G->>C: DreamInterpreter.Interpret(改ざんした夢, DreamRules)
    C-->>G: Dogma（ペアが作れなければ null）
    P->>G: 時代跳躍（N年）
    G->>C: WorldSimulator.SimulateTimeSkip(WorldState, ドグマの一覧, N, WorldRules)
    C-->>G: 書き換えた WorldState
    G->>P: 環境の変化を描画
```

## 主な設計方針
| 方針 | 根拠 |
| :--- | :--- |
| シミュレーションを Godot 非依存の C# ライブラリに分け、xUnit でテストする | [ADR 0001](adr/0001-separate-core-library.md) |
| Godot プロジェクトを `game/` に置き、コアを `ProjectReference` で参照する | [ADR 0002](adr/0002-godot-project-in-game-dir.md) |
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
| `game/data/` | ゲームデータ定義（`symbols.json`・`rules.json`）と、それをコアの定義（`DreamRules`・`WorldRules`）に変換する `GameDataLoader` |
| `game/scenes/` | シーンとそのスクリプト（シーンごとにディレクトリを分ける） |
| `game/locales/` | 翻訳ファイル |
| `companion/` | モバイルのコンパニオン（技術は未定） |
| `docs/protocol/` | ホストとコンパニオンの通信の契約 |
