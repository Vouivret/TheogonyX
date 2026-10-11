# Godot のルール

> `.agent/rules/project.md` の下位ルール。矛盾した場合は `project.md` を優先する。
> 対象: `game/**`（シーン `.tscn`、リソース `.tres`、シェーダー `.gdshader`・`.glsl`、`project.godot`、ノードのスクリプト）。C# の書き方は `csharp.md` にある。

## バージョン
- Godot 4.x の .NET 版を使う。バージョンは `docs/nfr.md` の「対応環境」に書いたものに合わせ、`project.godot` の `config/features` を勝手に上げない。

## 編集してよいもの・しないもの
- `.godot/`、`*.import`、`export/` は編集しない（エディタが生成する）。
- `.tscn`・`.tres` はテキストで編集してよいが、`uid=`、`ext_resource` の `id`、`load_steps` は手で作らない。新しいシーンの骨組みやリソースの参照の追加は、エディタで作ることを人間に頼むか、作った場合は Godot でシーンを開いてエラーが出ないことを確かめるまで「未確認」と報告する。
- `*.uid` ファイルは対応するスクリプト・シェーダーと一緒にコミットする。ファイルを移動・改名するときは、エディタの FileSystem で行うことを人間に頼む（参照が壊れるため）。
- `project.godot` の変更（入力マップ、オートロード、描画の設定、レイヤー名など）は、作業の報告に変更点を必ず書く。描画方式（レンダラー）の変更は確認する。

## 構成
- ディレクトリとファイルは snake_case（例: `game/scenes/world/world_core.tscn`）。ただし C# スクリプトは型名と同じ PascalCase（`WorldCore.cs`）。
- ノード名は PascalCase。
- シーンごとにディレクトリを分け、シーンとそのスクリプトを同じ場所に置く。
- オートロード（シングルトン）を増やすときは確認する。

## ノードのスクリプト
- ノードへの参照は `[Export]` か、シーン固有名（`%Name`）と `GetNode<T>("%Name")` で取り、`_Ready()` で一度だけ取得する。`_Process` の中で `GetNode` しない。
- ノード同士の連絡はシグナルを使う。子から親や兄弟を `GetParent()` でたどって直接呼ばない。
- C# のシグナルは `[Signal] public delegate void <名前>EventHandler(...)` で宣言する。
- ゲームのルールをノードに書かない。コア（`src/TheogonyX.Core/`）を呼び、結果を描画する（`csharp.md`）。

## 描画とシェーダー
- ピクセルアートのテクスチャはフィルタを Nearest にし、ミップマップを使わない。
- 大量NPCの更新・描画（Compute Shader、MultiMesh などのインスタンシング）は、同じ計算の CPU 版をコアに持ち、結果を突き合わせられるようにする。CPU 版を持たずに GPU 版だけを書くときは確認する。
- シェーダーの uniform・バッファの並びを変えたら、C# 側の書き込みも同じ変更で直す。

## 確認
- シーンやシェーダーを変えたら、Godot で実際に開いて（できれば実行して）確かめる。確かめられない環境では、その旨を報告に書く（`project.md` 2.5）。
