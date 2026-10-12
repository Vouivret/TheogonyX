# plan: M2 スマホでの夢の改ざんと依頼の表示

## 目的
PC の画面と同じ夢の改ざん・神託・依頼の吹き出しを、スマホのコンパニオンからも使えるようにする（[ロードマップ](../docs/roadmap.md) の M2。M2 の最初に決めた「スマホは上に映像、下でカードを入れ替える」の下半分）。仕様は [dream-tampering.md](../docs/domain/dream-tampering.md) と [prophet-request.md](../docs/domain/prophet-request.md) に足す。

## スコープ
- 含む
  - メッセージ: ホスト → コンパニオン `DREAM_STATE`、コンパニオン → ホスト `TAMPER`・`PRONOUNCE`（[messages.md](../docs/protocol/messages.md) を先に直す）
  - ホスト: 夢の状態を、ペアリングしたすべての接続に送る。スマホからの改ざん・神託を受けて、PC と同じコアの操作を呼ぶ
  - コンパニオン: 下の欄に、状態の案内・夢のコマ・手札・「神託を下す」、映像の上に依頼の吹き出し
- 含まない
  - 時間の操作（100 年跳躍・自動進行・世界再創生）をスマホからすること
  - 夢や依頼が出たときの振動・通知（人間が決めた。2026-10-12。今回はしない）

## 決定事項
| 決定 | 理由 | ADR化 |
| :--- | :--- | :--- |
| メッセージと処理の流れを、この plan の案のとおりにする | 人間が承認した（2026-10-12） | しない |
| **AI の案**: スマホには、夢の状態を丸ごと送る（差分ではない）。送るのは、ペアリングしたときと、送る内容が前に送ったものと変わったとき | 取りこぼしても次で揃う。PC の描き直しは自動進行の 1 年ごとにあるので、同じ内容は送らない | しない |
| **AI の案**: PC とスマホは同時に操作できる。早く届いた操作が勝つ。遅れた操作は捨てて、その接続に今の状態を送り直す（`ERROR` は送らず、接続も切らない） | Godot のメインスレッドで順に処理するので、同時に書き換わることはない。行き違いはふつうに起こるので、切断はしない | しない |
| **AI の案**: スマホでのコマ選びは、スマホの中だけの状態にし、カードを選んだときに `TAMPER` を送る。PC で選んでいるコマとは別 | 選んでいる途中はコアの状態ではない | しない |
| **AI の案**: スマホからの改ざん・神託も、PC と同じ `DreamView` の処理を通し、ログ（`dream_tampered`・`dogma_formed` など）も同じものを出す | 操作の入口を 1 つにする | しない |
| **AI の案**: 手札（記号の ID の一覧）も `DREAM_STATE` で送る。記号と困りごとの名前は、コンパニオンの `locales/*.json` に置く | スマホに記号の一覧を直書きしない | しない |
| **AI の案**: 夢の状態は配信（`START_STREAM`）とは別。配信していなくても、ペアリングした接続には送る | 映像を止めても、カードは使える | しない |

## メッセージ（案）
| type | version | 向き | 項目と型 | 意味 |
| :--- | :--- | :--- | :--- | :--- |
| `DREAM_STATE` | 1 | ホスト → コンパニオン | `state`: `"NONE"` \| `"UNTAMPERED"` \| `"TAMPERED"`、`nodes`: `[{symbolId: 文字列, weight: 数}]`（夢なしなら空）、`cards`: 記号 ID の配列、`canTamper`: 真偽、`canPronounce`: 真偽、`isDestroyed`: 真偽、`request`: `{requestType: 文字列, symbolId: 文字列}` か `null` | 夢と依頼の今の状態 |
| `TAMPER` | 1 | コンパニオン → ホスト | `index`: 整数（0 から）、`symbolId`: 文字列 | 夢の `index` 番目を `symbolId` のカードと入れ替える |
| `PRONOUNCE` | 1 | コンパニオン → ホスト | なし | 神託を下す |

```json
{ "type": "DREAM_STATE", "version": 1, "state": "UNTAMPERED",
  "nodes": [{ "symbolId": "SYM_TREE", "weight": 0.5 }, { "symbolId": "SYM_RAIN", "weight": 0.6 }, { "symbolId": "SYM_FIRE", "weight": 0.4 }],
  "cards": ["SYM_TREE", "SYM_RAIN", "SYM_BLOOD", "SYM_FIRE"],
  "canTamper": true, "canPronounce": true, "isDestroyed": false,
  "request": { "requestType": "THIRST", "symbolId": "SYM_RAIN" } }
{ "type": "TAMPER", "version": 1, "index": 1, "symbolId": "SYM_BLOOD" }
{ "type": "PRONOUNCE", "version": 1 }
```

## 処理の流れ

### ホスト: 夢の状態を送る
- `DreamView` が描き直すたびに、`CompanionHost` に今の状態を渡す（`DreamView` のイベント）。
- `CompanionHost` は `DREAM_STATE` の JSON を作り、前に送った JSON と同じなら何もしない。違えば、ペアリングしたすべての接続に送り、覚えておく。
- ペアリング（`PAIR`・`RESUME`）が済んだら、`PAIRED` の次に、その接続へ今の `DREAM_STATE` を送る。
- 送るのに失敗した接続は、配信（`FRAME`）と同じく `Warn`（`event=message_send_failed clientId=... reason=...`）で記録し、接続の切断の処理に任せる。

### ホスト: `TAMPER`・`PRONOUNCE` を受け取る
- ペアリング前に受け取ったら、ほかのメッセージと同じく `PAIRING_FAILED` で切る（今のまま）。
- パーサーで検証する: `index` は 0 以上の整数、`symbolId` は 1〜64 文字の文字列。型が違う・項目が無い・余計に大きいものは、今の不正なメッセージと同じく捨てて `Warn`（`event=message_rejected reason=invalid_tamper`。同じ接続・同じ理由では間引く）。
- `TAMPER`: 今は改ざんできない（夢なし・改ざん済み・崩壊）、`index` が夢の範囲外、`symbolId` が手札に無い、のどれかなら、捨てて `Warn`（`reason=cannot_tamper`・`invalid_dream_index`・`unknown_symbol`。間引く）。その接続に今の `DREAM_STATE` を送り直す。当てはまらなければ、`DreamView` の改ざんを呼ぶ（PC で選んでいたコマは選んでいないことにする）。
- `PRONOUNCE`: 今は神託を下せない（夢なし・崩壊）なら、捨てて `Warn`（`reason=cannot_pronounce`。間引く）し、その接続に今の `DREAM_STATE` を送り直す。そうでなければ、`DreamView` の神託を呼ぶ（HUD のドグマの一覧と時間のボタンも描き直す。自動進行の再開も PC と同じ）。
- どちらも、`DreamView` が描き直すので、変わった `DREAM_STATE` がすべての接続に届く。

### コンパニオン
- `DREAM_STATE` を受け取ったら、下の欄を描き直す。項目が足りない・型が違うものは捨てる（今の `parseHostMessage` と同じ）。
- 1 段目に状態の案内（夢なし・改ざん前・コマを選んでいる・改ざん済み・崩壊）。2 段目に夢のコマ 3 つと「神託を下す」。コマを押すと、2 段目を手札と「やめる」に替える。カードを押したら `TAMPER` を送り、選んでいない状態に戻す。
- `canTamper` が偽なら、コマを押せなくし、選んでいる途中ならやめる。`canPronounce` が偽なら「神託を下す」を押せなくする。
- `request` があれば、映像の上に吹き出し（`{困りごと}に苦しんでいます。{記号}をお恵みください`）を出す。
- 切断したら、下の欄を押せなくする（再接続して `DREAM_STATE` を受け取ったら戻す）。
- 文言は `locales/ja.json`・`en.json` に足す（記号・困りごとの名前、案内、ボタン）。英語は仮。

## テスト観点
- 自動（`tests/TheogonyX.Core.Tests/`）: `TAMPER`・`PRONOUNCE` のパース（正常・`index` が負・小数・文字列・`symbolId` が長すぎる・項目が無い）、`DREAM_STATE` の書き出し（夢あり・夢なし・依頼あり・崩壊）。
- 手動: スマホでコマを選んでカードを入れ替え、神託を下すと、PC の画面とスマホの両方に反映されること。PC で神託を下すと、スマホの欄も変わること。依頼の吹き出しがスマホにも出ること。
- 受け入れ条件は AC-dream-tampering-015・016、AC-prophet-request-013。テスト仕様書は TS-dream-tampering-010〜012、TS-prophet-request-008。

## 未決
なし。
