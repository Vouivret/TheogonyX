# テスト仕様書: 夢の解釈

- 受け入れ条件: [docs/domain/dream-interpretation.md](../domain/dream-interpretation.md)
- 関連ADR: なし

## テスト観点

種別は「自動」「手動」「廃止」のどれか。自動テストの手順はテストコードに書き、ここには書かない。

| ID | 受け入れ条件 | 観点 | 前提 | 操作 | 期待結果 | 種別 | 実装場所 |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| TS-dream-interpretation-001 | AC-dream-interpretation-001 | 衝撃度が 1.0 を超えると強度が 1.0 に抑えられる | 受け入れ条件のとおり | 解釈する | 受け入れ条件のとおり | 廃止 | — |
| TS-dream-interpretation-002 | AC-dream-interpretation-002 | 極性の和がちょうど −0.3 のとき禁忌になる（float の誤差で破壊にならない） | 受け入れ条件のとおり | 解釈する | 受け入れ条件のとおり | 廃止 | — |
| TS-dream-interpretation-003 | AC-dream-interpretation-003 | 極性の和が +0.3 以上で崇拝になる | 受け入れ条件のとおり | 解釈する | 受け入れ条件のとおり | 廃止 | — |
| TS-dream-interpretation-004 | AC-dream-interpretation-004 | 極性の和が −0.3 と +0.3 の間で破壊になる | 受け入れ条件のとおり | 解釈する | 受け入れ条件のとおり | 廃止 | — |
| TS-dream-interpretation-005 | AC-dream-interpretation-005 | 3個以上の夢で、衝撃度が最大のペアが選ばれる | 受け入れ条件のとおり | 解釈する | 受け入れ条件のとおり | 廃止 | — |
| TS-dream-interpretation-006 | AC-dream-interpretation-006 | 衝撃度が同点なら後ろのペアが選ばれる | 受け入れ条件のとおり | 解釈する | 受け入れ条件のとおり | 廃止 | — |
| TS-dream-interpretation-007 | AC-dream-interpretation-007, AC-dream-interpretation-008 | ペアが作れない夢（0個・1個）ではドグマ無し | 受け入れ条件のとおり | 解釈する | 受け入れ条件のとおり | 廃止 | — |

## 対象外

- 極性の和がちょうど +0.3 の境界: MVP の記号の組み合わせでは作れないため、受け入れ条件に無い。記号が増えたら追加する。
