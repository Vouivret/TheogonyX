# Theogony X

> **残りの初期導入**（済んだら行を消し、すべて済んだらこの囲みを削除する）
> - [ ] `TheogonyX.sln`・`src/TheogonyX.Core/`・`tests/TheogonyX.Core.Tests/`・`game/` を作る（最初のマイルストーン。`PROMPT_FOR_CLAUDE.md`）
> - [ ] コンパニオン（PWA）の技術を決め、言語のルールを `.agent/rules/` に足す
> - [ ] 配布の前までに、`docs/operations.md` の空欄を埋める

プレイヤーが「神」となり、予言者の夢を改ざんして人間の誤解を誘発し、そこから生まれる教義（ドグマ）と星の環境の激変を観察する、2D の箱庭歴史シミュレーター。PC（Godot）でシミュレーションと描画を行い、モバイルのコンパニオン（PWA）から神託を送る。

## 前提
| ツール | バージョン | 入手先 |
| :--- | :--- | :--- |
| Godot Engine（.NET 版） | 4.7.2 | https://godotengine.org/download |
| .NET SDK | 10.0.x（`TargetFramework` は `net10.0`） | https://dotnet.microsoft.com/download |
| Git for Windows（Git Bash） | 最新 | https://gitforwindows.org |

Claude Code のフック（`.claude/hooks/`）と `scripts/` の実行には `sh` が必要（Windows では Git Bash）。

## セットアップ
```bash
dotnet restore TheogonyX.sln
```

## 起動
Godot エディタ（`C:\Tools\Godot\Godot_v4.7.2-stable_mono_win64\Godot_v4.7.2-stable_mono_win64.exe`）で `game/project.godot` を開き、F5 で実行する。

Godot が作る `.csproj` は既定で `net8.0` になるので、`net10.0` に変える。C# 14 の新しい構文は、Godot のビルドで動くことを確かめるまで使わない。

## 確認
ビルド・lint・テスト・テスト仕様書のIDの照合・ツール用のルールファイルの生成を、1つのコマンドで実行する。AIエージェントも、コードを変更したら必ずこのコマンドで確認する。

```bash
sh scripts/check.sh
```

個別に実行する場合:
```bash
dotnet build TheogonyX.sln
dotnet format TheogonyX.sln --verify-no-changes
dotnet test TheogonyX.sln
```

## ディレクトリ構成
```
.agent/                     # AIエージェント向けのルールと手順書
docs/                       # 仕様書・テスト仕様書・ADR・通信の契約・運用手順
plan/                       # 作業中のチケットの計画（完了したものは plan/archive/）
src/TheogonyX.Core/         # シミュレーション（Godot に依存しない C#）
tests/TheogonyX.Core.Tests/ # コアの自動テスト（xUnit）
game/                       # Godot プロジェクト（シーン・描画・データ定義・翻訳）
companion/                  # モバイルのコンパニオン（技術は未定）
scripts/                    # 確認用のスクリプト
```

## ドキュメント
開発の進め方と文書の一覧は [docs/DEV_HANDBOOK.md](docs/DEV_HANDBOOK.md) にある。全体の構成は [docs/architecture.md](docs/architecture.md)。

## AIエージェントで開発する場合
ルールは [.agent/rules/project.md](.agent/rules/project.md) にある。外部のAIサービスに渡してよい情報は [AI利用ポリシー](docs/AI_USAGE_POLICY.md) を確認すること。
