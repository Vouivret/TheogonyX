# plan: M2 セーブ・ロード（現実同期モードの準備）

## 目的
ゲームを閉じても、次に起動したときに同じ星の続きから遊べるようにする。この後に作る現実同期モード（現実の 24 時間でゲーム内の 1 年。自動進行の速さの 1 つ）で、長く放置して育てる星を失わないための土台にする（[ロードマップ](../docs/roadmap.md) の M2）。

## スコープ
- 含む
  - 星の状態をファイルに保存し、起動時に読み込む（1 つだけ。スロットは作らない）
  - 保存するとき: 終了時、神託・100 年跳躍・世界再創生の後、自動進行中の定期的な保存
  - 壊れたセーブの扱い
- 含まない
  - 現実同期モード（次の作業）
  - 閉じていた間の時間を進めること（人間が決めた。2026-10-12。進めない）
  - タイトル画面、複数のセーブ、クラウドへの保存

## 決定事項
| 決定 | 理由 | ADR化 |
| :--- | :--- | :--- |
| 閉じていた間の時間は進めない。保存は続きから遊ぶためだけに使う | 人間が決めた（2026-10-12） | しない |
| 保存は、終了時、神託・100 年跳躍・世界再創生の後、自動進行中の定期的な保存 | 人間が決めた（2026-10-12） | しない |
| 起動時にセーブがあれば、自動で続きから始める。新しく始めるには世界再創生を押す | 人間が決めた（2026-10-12） | しない |
| 読み込んだ後は、終了時に自動進行中でも止まった状態（崩壊していれば崩壊）で始める | 人間が決めた（2026-10-12） | しない |
| 壊れたセーブは、画面に「セーブを読めなかった」と出して新しい星で始め、壊れたファイルは別の名前で残す（上書きしない） | 人間が決めた（2026-10-12） | しない |
| 自動進行中は、前の保存から 5 分（現実の時間）たったら、次の 1 年が進んだ後に保存する。値は `game/data/` に置く | 人間が決めた（2026-10-12） | しない |
| 処理の流れを、この plan のとおりにする | 人間が承認した（2026-10-12） | しない |
| **AI の案**: 置き場所は `user://save/world.json`（Windows では `%APPDATA%\Godot\app_userdata\<プロジェクト名>\`）。JSON にする | 人が読めて、差分を見やすい。ゲームデータ定義と同じ形式 | しない |
| **AI の案**: 書き込みは `world.json.tmp` に書いてから `world.json` に置き換える | `save-data.md`。書き込みの途中で終了しても前のセーブが残る | しない |
| **AI の案**: 夢の乱数は、シードと「これまでに見た夢の数」を保存し、読み込み時に同じシードから同じ数の夢を作り直して捨て、続きの乱数にする | 同じシード・同じ操作から同じ夢になる（`project.md` 3章）。`System.Random` の内部の状態は保存できない | しない |
| **AI の案**: 保存の形と読み書き・検証はコア（`src/TheogonyX.Core/Persistence/`）、ファイルの読み書きは Godot 側の新しいクラス `SaveStore` に置く | コアを Godot に依存させない。検証をテストできる | しない |

## セーブデータの形（案。`saveVersion` 1）
```json
{
  "saveVersion": 1,
  "seed": 123456789,
  "dreamsGenerated": 4,
  "world": { "year": 312, "forestAmount": 61.5, "waterAmount": 48.2, "temperature": 18.0,
             "chaosLevel": 22.0, "harmonyLevel": 70.1, "isDestroyed": false },
  "dogmas": [{ "targetSymbolId": "SYM_TREE", "dogmaType": "Taboo", "intensity": 0.74 }],
  "dream": { "state": "TAMPERED", "nodes": [{ "symbolId": "SYM_TREE", "weight": 0.5 }, { "symbolId": "SYM_BLOOD", "weight": 0.6 }, { "symbolId": "SYM_FIRE", "weight": 0.4 }] },
  "request": "THIRST",
  "answeredYears": { "FAMINE": 250 },
  "lastOracleYear": 300
}
```
- 保存しないもの: 時間の状態（読み込み後は止まった状態にするため）、夢で自動進行を止めたかどうか（同じ理由）、スマホの接続（`sessionToken` は起動ごとに作り直す）。

## 処理の流れ

### 起動時（`WorldCore._Ready`）
- `SaveStore.TryLoad` で `user://save/world.json` を読む。
  - ファイルが無い → 今までどおり新しい星で始める（新しいシードを作り、`event=game_started seed=...`）。
  - 読めた → `SaveDataReader.TryParse` で検証する（下）。
    - 正しい → シードと見た夢の数から乱数を作り直し、`TimeControl`・`ProphetDreams` を保存された状態に戻す。崩壊していれば崩壊、そうでなければ止まった状態。`event=game_loaded seed=... year=...`。
    - 正しくない → `world.json` を `world.broken-<日時>.json` に名前を変えて残し、新しい星で始める。HUD に「セーブを読めなかったので、新しい星で始めました」を出す。`Warn`（`event=save_load_failed reason=...`）。
  - ファイルを読む・名前を変えるのに失敗した（権限など） → `Error`（`event=save_load_failed reason=...`）を出し、新しい星で始める。この起動では保存しない（壊れていないかもしれないセーブを上書きしないため）。

### 検証（`SaveDataReader.TryParse`。外部入力として扱う）
- JSON でない、`saveVersion` が無い・1 でない、必須の項目が無い・型が違う → 失敗。
- 数値が有限でない（NaN・無限）、年が負、夢ノードの数が定義と違う、重みが範囲外 → 失敗。
- 定義に無い記号 ID・困りごとの ID・ドグマの種別 → 失敗。
- 夢の状態と夢ノードが合わない（`NONE` なのに夢ノードがある、など）、`dreamsGenerated` が負 → 失敗。
- 失敗したら、理由（`reason`。例: `invalid_json`・`unsupported_save_version`・`unknown_symbol`）を返す。

### 保存（`SaveStore.Save`）
- コアの今の状態から JSON を作る（`SaveDataWriter`）。
- `user://save/` が無ければ作る。`world.json.tmp` に書き、`world.json` に置き換える。
- 失敗したら `Warn`（`event=save_failed reason=...`）。ゲームは続ける。同じ理由が続くときは、成功するまで 1 回だけ記録する。
- 成功したら `Debug` だけ（`event=game_saved year=...`）。毎回 `Info` にすると、自動進行中に多すぎる。

### いつ保存するか
- 神託を下した後、100 年跳躍の後、世界再創生の後。
- 自動進行中は、前の保存から 5 分（現実の時間。`game/data/save.json` の `autoSaveIntervalSeconds`）たったら、次の 1 年が進んだ後に保存する。経過は Godot の `_Process` の `delta` を足して数える。
- 終了時（ウィンドウを閉じる `NOTIFICATION_WM_CLOSE_REQUEST`）。

## テスト観点
- 自動（コア）: 書き出して読み戻すと同じ状態になる（夢あり・夢なし・依頼あり・崩壊）。同じシードと見た夢の数から、次の夢が保存しなかった場合と同じになる。検証で失敗する入力（上の一覧）。
- 手動: 改ざん・神託・跳躍の後に終了して起動すると続きになる。自動進行中に終了すると、止まった状態で続きになる。セーブを手で壊すと、新しい星で始まり、壊れたファイルが残る。
- 仕様書は [save-data.md](../docs/domain/save-data.md)、テスト仕様書は [docs/test/save-data.md](../docs/test/save-data.md)。

## 未決
なし。
