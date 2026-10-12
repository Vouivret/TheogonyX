# C# のルール

> `.agent/rules/project.md` の下位ルール。矛盾した場合は `project.md` を優先する。
> 対象: `**/*.cs`（コア `src/TheogonyX.Core/`、Godot 側 `game/`、テスト `tests/`）。Godot 固有の書き方は `godot.md` にある。

## バージョンと設定
- Godot 4.7.2（.NET 版）、.NET 10（`<TargetFramework>net10.0</TargetFramework>`）。すべてのプロジェクトでそろえる。バージョンは `docs/nfr.md` の「対応環境」に合わせる。
- C# 14 の新しい構文は、Godot エディタからのビルドで動くことを確かめるまで使わない。
- すべてのプロジェクトで `<Nullable>enable</Nullable>` にする。null 許容の警告を `!` や `#nullable disable` で黙らせない。
- フォーマットは `dotnet format` に従う（設定はリポジトリ直下の `.editorconfig`）。

## コアと Godot の境界（どのフェーズでも守る）
- `src/TheogonyX.Core/` は Godot に依存させない。`using Godot;`、`GD.*`、Godot の型（`Node`、`Vector2`、`Resource` など）を書かない。必要な値の型はコア側で定義する。
- コアに置くもの: モデル（`Symbol`・`DreamNode`・`Dogma`・`WorldState` など）、夢の解釈、ドグマの生成・感染、時代跳躍の計算、セーブデータの形式、通信メッセージの型。
- `game/` に置くもの: ノード、描画、入力、ファイルの読み書き、WebSocket サーバー、ログの出力。`game/` のコードはコアを呼び出して結果を表示するだけにし、ゲームのルールを書かない。
- コアはファイルを読まず、ログも出さない。データは引数で受け取り、起きた出来事は戻り値（イベントの一覧など）で返す。

## 命名
Godot の C# の規約（= .NET の規約）に合わせる。

| 対象 | 規則 | 例 |
| :--- | :--- | :--- |
| 名前空間 | `TheogonyX.<領域>` | `TheogonyX.Core`、`TheogonyX.Core.Protocol` |
| 型・メソッド・プロパティ・定数 | PascalCase | `InterpretDream`、`WorldState.ChaosLevel` |
| private フィールド | `_camelCase` | `_random` |
| 引数・ローカル変数 | camelCase | `years`、`dreamNodes` |
| ファイル名 | 型名と同じ（1ファイル1公開型） | `WorldCore.cs` |
| データ定義のID | 大文字スネークケース（接頭辞付き） | `SYM_TREE` |

## 書き方
- シミュレーションの計算は、引数と戻り値で完結するメソッドにする（静的な可変状態を持たない）。テストと決定性のため。
- 乱数は `System.Random` をシード付きで生成し、引数かコンストラクタで渡す（`project.md` 3章）。
  - 例外: 群衆のように GPU（Compute Shader）と同じ乱数が要る計算は、`System.Random` ではなく (シード, 番号, 歩数) から作る整数のハッシュ関数（`CrowdHash`）を使う（[ADR 0004](../../docs/adr/0004-crowd-counter-based-hash-random.md)）。
- 調整値を直書きしない（`save-data.md`）。
- 早すぎる抽象化をしない。1つの星・1人の予言者・100年のループが動く構造を優先し、インターフェースは2つ目の実装が必要になってから作る。
- 毎フレーム呼ばれるコードで LINQ やコレクションの生成を繰り返さない（Phase 1 では指摘のみ。直すのは Phase 3）。

## エラー処理とログ
AIはこの正解例の書き方に合わせる。キー名は `project.md` の「ログとエラー処理」の表に従う。ログを出すのは `game/` 側だけ。

```csharp
// 正解例: 構造化ログ（event とキーは表に合わせる）
GD.Print($"event=dogma_formed year={state.Year} symbolId={dogma.TargetSymbolId} dogmaType={dogma.Type}");

// 正解例: 例外を捕まえて、文脈を足して投げ直す
try
{
    definitions = SymbolDefinitionLoader.Parse(json);
}
catch (JsonException ex)
{
    throw new InvalidDataException($"記号定義を読めない: path={path}", ex);
}

// 正解例: 回復できる失敗を Warn で記録して続行する
if (!message.IsValid(out var reason))
{
    GD.PushWarning($"event=message_rejected clientId={clientId} reason={reason}");
    return;
}
```

使わない書き方: キーの無い文章だけのログ（`GD.Print("ドグマができた")`）、`catch { }` で何もしない、`_Process` の中での `Info` ログ。

## 多言語対応
- 文字列は `game/locales/` の翻訳ファイルに置き、`Tr("KEY")` で取り出す（ルールは `ui.md`）。コアは表示用の文字列を持たず、キーやIDを返す。

## テスト
- テストの置き場所: `tests/TheogonyX.Core.Tests/`（xUnit）。テストクラスは `<対象の型>Tests`、テストメソッドは `<メソッド>_<条件>_<結果>`（例: `InterpretDream_雨と血が隣接_雨が禁忌になる`）。
- テスト仕様書のIDは `[Trait("TS", "TS-<機能名>-NNN")]` で付ける。

## コマンド
変更後の確認は、`README.md` の「確認」にある1つのコマンドで行う。

この言語だけを個別に実行する場合:
```bash
# ビルド
dotnet build TheogonyX.sln
# lint（フォーマットの確認）
dotnet format TheogonyX.sln --verify-no-changes
# テスト
dotnet test TheogonyX.sln
```
