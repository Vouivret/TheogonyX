---
status: accepted   # accepted | superseded | deprecated
superseded_by:     # 置き換えたADRの番号（superseded のときのみ）
area: [godot, build]
date: 2026-10-11
---

# 0002: Godot プロジェクトを `game/` に置き、`game/TheogonyX.csproj` からコアを `ProjectReference` で参照する

## 背景
[0001](0001-separate-core-library.md) でコア（`src/TheogonyX.Core/`）とテスト（`tests/`）を Godot から分けた。Godot.NET.Sdk は、プロジェクトのディレクトリの下にある `.cs` をすべてビルドに含める。

## 決定
- Godot プロジェクト（`project.godot`）は `game/` に置く。
- `game/TheogonyX.csproj`（`Godot.NET.Sdk/4.7.2`、`net10.0`）から、コアを `ProjectReference` で参照する。
- リポジトリ直下の `TheogonyX.sln` に、コア・テスト・`game/` の3つのプロジェクトを入れる。`project.godot` の `dotnet/project/solution_directory` を `..` にして、Godot もこのソリューションでビルドする。

## 検討した他の案
- **`project.godot` をリポジトリ直下に置く**: Godot が `src/`・`tests/` の `.cs` までビルドに含めてしまう（テストのコードと xUnit への依存がゲームに入る）。

## 結果
- `dotnet build TheogonyX.sln` で、コア・テスト・Godot 側をまとめてビルドできる。
- Godot エディタでビルドすると、テストのプロジェクトも一緒にビルドされる（数秒の差）。
- Godot が `.csproj` を作り直すと `net8.0` に戻ることがあるので、`net10.0` に直す（`README.md`）。
