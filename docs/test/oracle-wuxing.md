# テスト仕様書: 神託の五行

- 受け入れ条件: [docs/domain/oracle-wuxing.md](../domain/oracle-wuxing.md)
- 関連ADR: なし

## テスト観点

種別は「自動」「手動」「廃止」のどれか。自動テストの手順はテストコードに書き、ここには書かない。段階 1（土・金の記号を足す）と段階 2（種別を五行の関係で決める）を対象にする。段階 3（融合）も対象にする。

| ID | 受け入れ条件 | 観点 | 前提 | 操作 | 期待結果 | 種別 | 実装場所 |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| TS-oracle-wuxing-001 | AC-oracle-wuxing-014 | （廃止）段階 1 の極性の和による種別。段階 2c で旧い方式とともに廃止 | | | | 廃止 | — |
| TS-oracle-wuxing-002 | AC-oracle-wuxing-013, 015 | 最初のデッキは五行が 3 枚ずつで血が無い。工房で金が加わる | 受け入れ条件のとおり | デッキを作る・工房が建つ | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/OracleWuxingTests.cs` |
| TS-oracle-wuxing-003 | AC-oracle-wuxing-013 | 画面とスマホの手札に、大地・鉄の名前が出る。大地・鉄で神託を下すと、ドグマの一覧に名前が出る | Godot 4.7.2 で動かす。スマホはペアリング済み | 下の手順 | 下の手順のとおり | 手動 | — |
| TS-oracle-wuxing-004 | AC-oracle-wuxing-001〜007 | 五行の関係で、種別・強度・対象が決まる（穢れ・汚染ルーンを含む） | 受け入れ条件のとおり | 判定する | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/OracleWuxingTests.cs` |
| TS-oracle-wuxing-005 | AC-oracle-wuxing-008 | 順序付きの 25 通りの関係の数と、相生・相剋の並びの向き | 受け入れ条件のとおり | 判定する | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/OracleWuxingTests.cs` |
| TS-oracle-wuxing-006 | AC-oracle-wuxing-016 | 五行の定義の検証 | 受け入れ条件のとおり | 検証する | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/OracleWuxingTests.cs` |
| TS-oracle-wuxing-007 | AC-oracle-wuxing-018 | 2 枚の予告が、下した神託と一致する。番号が正しくなければ予告しない | 受け入れ条件のとおり | 予告して下す | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/OracleWuxingTests.cs` |
| TS-oracle-wuxing-008 | AC-oracle-wuxing-001〜006, 018 | 画面で、2 枚を選ぶと予告が出て、下した神託が予告どおりの種別になる。種別の名前に陰陽が添えられる | Godot 4.7.2 で動かす | 下の手順 | 下の手順のとおり | 手動 | — |
| TS-oracle-wuxing-009 | AC-oracle-wuxing-009, 021 | 上位のルーンは持つ記号で関係を決め、強度に上位のルーンの重みを使う | 受け入れ条件のとおり | 判定する | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/OracleFusionTests.cs` |
| TS-oracle-wuxing-010 | AC-oracle-wuxing-010 | 木 2 枚を融合すると、木が 2 枚減り、大樹が捨札に加わり、持ち分が減る。手札は補われる | 受け入れ条件のとおり | 融合する | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/OracleFusionTests.cs` |
| TS-oracle-wuxing-011 | AC-oracle-wuxing-011 | 融合できないとき（4 つの理由）は拒否され、何も変わらない | 受け入れ条件のとおり | 融合しようとする | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/OracleFusionTests.cs` |
| TS-oracle-wuxing-012 | AC-oracle-wuxing-019 | 上位のルーンの定義の検証 | 受け入れ条件のとおり | 検証する | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/OracleFusionTests.cs` |
| TS-oracle-wuxing-013 | AC-oracle-wuxing-020 | 上位のルーンを含むデッキの保存と読み込み | 受け入れ条件のとおり | 保存して読み込む | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/OracleFusionTests.cs` |
| TS-oracle-wuxing-014 | AC-oracle-wuxing-010 | 画面で、世界再創生の後に融合のボタンが出て、押すと上位のルーンがデッキに入り、手札とスマホに名前が出る | Godot 4.7.2 で動かす。スマホはペアリング済み | 下の手順 | 下の手順のとおり | 手動 | — |

## 手動テストの手順

### TS-oracle-wuxing-003
1. 前のセーブがあれば消してから、メインシーンを実行する（`saveVersion` 15 より前のセーブは読めない）。
2. 神託を何度か下して手札を入れ替え、手札に「大地」「鉄」が出ること、「血」が出ないことを確かめる。スマホの手札にも同じ名前が出る。
3. 「大地」「大地」で神託を下し、ドグマの一覧に「大地」の崇拝が出ることを確かめる（ログ `event=dogma_formed symbolId=SYM_EARTH dogmaType=Worship`）。

### TS-oracle-wuxing-008
1. メインシーンを実行し、手札から「聖なる火」と「木」を、この順に選ぶ。案内の行に「聖なる火 ← 木：相生 → 崇拝（陽）」と出ることを確かめる。
2. 選びを替えて、相剋（例: 聖なる火・恵みの雨 → 禁忌（陰））、逆らう（聖なる火・鉄 → 破壊（乱れ））、分流（聖なる火・大地 → 崇拝（陽））、比和（同じ記号 2 枚 → 崇拝（陽））の予告を確かめる。
3. 神託を下し、ドグマの一覧に、予告どおりの種別（例: 「崇拝（陽）」）が出ることを確かめる。
4. 汚染ルーンの狂乱が手札にあれば、狂乱を含む 2 枚で「穢れ → 破壊（乱れ）」と出ることを確かめる。

### TS-oracle-wuxing-014
1. メインシーンを実行し、「世界再創生」を押す。精査の欄に「融合: 木×2 → 大樹」のように、2 枚以上ある五行の基本ルーンごとに融合のボタンが出ることを確かめる。
2. 「融合: 木×2 → 大樹」を押す。木の枚数が 2 減り、「大樹 ×1」が並び、「残り 1 回」になることを確かめる（ログ `event=deck_fused runeId=RUNE_GREAT_TREE`）。
3. 神託を何度か下して、手札に「大樹」が出ることを確かめる。スマホの手札にも「大樹」と出る。大樹と木を選ぶと、案内に「比和 → 崇拝（陽）」と出る。
4. 終了して起動し直し、続きから始めても、大樹がデッキに残っていることを確かめる。

## 対象外

- 土・金の効果の値そのもの（仮置き。`rules.json` の値で、本番の値にテストを依存させない）。
