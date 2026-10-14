# テスト仕様書: 神託の五行

- 受け入れ条件: [docs/domain/oracle-wuxing.md](../domain/oracle-wuxing.md)
- 関連ADR: なし

## テスト観点

種別は「自動」「手動」「廃止」のどれか。自動テストの手順はテストコードに書き、ここには書かない。段階 1（土・金の記号を足す）と段階 2a（五行の判定をコアに足す。データ定義の `typeRule` はまだ `Polarity`）を対象にする。段階 2b（切り替え）・段階 3（融合）は、実装するときに観点を足す。

| ID | 受け入れ条件 | 観点 | 前提 | 操作 | 期待結果 | 種別 | 実装場所 |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| TS-oracle-wuxing-001 | AC-oracle-wuxing-014 | 土・金のルーンで神託を下すと、極性の和で種別が決まる（段階 1） | 受け入れ条件のとおり | 神託を下す | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/OracleWuxingTests.cs` |
| TS-oracle-wuxing-002 | AC-oracle-wuxing-013, 015 | 最初のデッキは五行が 3 枚ずつで血が無い。工房で金が加わる | 受け入れ条件のとおり | デッキを作る・工房が建つ | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/OracleWuxingTests.cs` |
| TS-oracle-wuxing-003 | AC-oracle-wuxing-013 | 画面とスマホの手札に、大地・鉄の名前が出る。大地・鉄で神託を下すと、ドグマの一覧に名前が出る | Godot 4.7.2 で動かす。スマホはペアリング済み | 下の手順 | 下の手順のとおり | 手動 | — |
| TS-oracle-wuxing-004 | AC-oracle-wuxing-001〜007, 017 | 五行の関係で、種別・強度・対象が決まる（穢れ・汚染ルーンを含む）。種別の決め方が極性なら、今までどおり | 受け入れ条件のとおり | 判定する | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/OracleWuxingTests.cs` |
| TS-oracle-wuxing-005 | AC-oracle-wuxing-008 | 順序付きの 25 通りの関係の数と、相生・相剋の並びの向き | 受け入れ条件のとおり | 判定する | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/OracleWuxingTests.cs` |
| TS-oracle-wuxing-006 | AC-oracle-wuxing-016 | 五行の定義の検証 | 受け入れ条件のとおり | 検証する | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/OracleWuxingTests.cs` |

## 手動テストの手順

### TS-oracle-wuxing-003
1. 前のセーブがあれば消してから、メインシーンを実行する（`saveVersion` 15 より前のセーブは読めない）。
2. 神託を何度か下して手札を入れ替え、手札に「大地」「鉄」が出ること、「血」が出ないことを確かめる。スマホの手札にも同じ名前が出る。
3. 「大地」「大地」で神託を下し、ドグマの一覧に「大地」の崇拝が出ることを確かめる（ログ `event=dogma_formed symbolId=SYM_EARTH dogmaType=Worship`）。

## 対象外

- 土・金の効果の値そのもの（仮置き。`rules.json` の値で、本番の値にテストを依存させない）。
