# ホストとコンパニオンのメッセージ

通信の契約の原本。ルールは `.agent/rules/protocol.md` にある。C# の型（`src/TheogonyX.Core/Protocol/`）とコンパニオンの定数（`companion/js/protocol.js`）を、この表と1対1で対応させる。

- 方式: WebSocket。メッセージはテキストフレーム（JSON）で送る。画像だけはバイナリフレームで送る（下の「バイナリフレーム」。[ADR 0003](../adr/0003-binary-frame-header.md)）。
- 共通の項目: すべてのメッセージに `type`（文字列）と `version`（整数）を持たせる。バイナリフレームでは、先頭のヘッダーに種別とバージョンを持たせる。
- ペアリング: 接続して最初のメッセージは `PAIR` か `RESUME` でなければならない。ペアリングを済ませるまで、ほかのメッセージは受け付けない（[仕様書](../domain/companion-pairing.md)）。
- トークン（`token`・`sessionToken`）の値はログに出さない。

## メッセージの一覧（テキストフレーム・JSON）
| type | version | 向き | 項目と型 | 意味 |
| :--- | :--- | :--- | :--- | :--- |
| `PAIR` | 1 | コンパニオン → ホスト | `token`: 文字列 | QR コードの URL にあったワンタイムトークンでペアリングする |
| `RESUME` | 1 | コンパニオン → ホスト | `sessionToken`: 文字列 | 前に受け取った `sessionToken` で、切断の後にペアリングをやり直す |
| `PAIRED` | 1 | ホスト → コンパニオン | `sessionToken`: 文字列 | ペアリングが済んだ。`RESUME` で使う `sessionToken` を渡す（`RESUME` のときは同じ値を返す） |
| `ERROR` | 1 | ホスト → コンパニオン | `reason`: 文字列（下の表） | 受け付けられない。ホストは送った後に接続を閉じる |
| `START_STREAM` | 1 | コンパニオン → ホスト | なし | この接続への画面の配信（`FRAME`）を始めてほしい。配信中に受け取っても何もしない |
| `STOP_STREAM` | 1 | コンパニオン → ホスト | なし | この接続への画面の配信をやめてほしい。配信していないときに受け取っても何もしない |
| `DREAM_STATE` | 1 | ホスト → コンパニオン | `state`: `"NONE"`・`"UNTAMPERED"`・`"TAMPERED"` のどれか、`nodes`: `{symbolId: 文字列, weight: 数}` の配列（夢なしなら空）、`cards`: 記号 ID の配列、`canTamper`: 真偽、`canPronounce`: 真偽、`isDestroyed`: 真偽、`request`: `{requestType: 文字列, symbolId: 文字列}` か `null` | 予言者の夢と依頼の今の状態（下の「夢の状態」） |
| `TAMPER` | 1 | コンパニオン → ホスト | `index`: 0 以上の整数、`symbolId`: 1〜64 文字の文字列 | 夢の `index` 番目（0 から）を `symbolId` のカードと入れ替える |
| `PRONOUNCE` | 1 | コンパニオン → ホスト | なし | 神託を下す |

例:

```json
{ "type": "PAIR", "version": 1, "token": "q9V3..." }
{ "type": "PAIRED", "version": 1, "sessionToken": "Xk2p..." }
{ "type": "START_STREAM", "version": 1 }
{ "type": "ERROR", "version": 1, "reason": "MAX_CONNECTIONS_REACHED" }
{ "type": "DREAM_STATE", "version": 1, "state": "UNTAMPERED",
  "nodes": [{ "symbolId": "SYM_TREE", "weight": 0.5 }, { "symbolId": "SYM_RAIN", "weight": 0.6 }, { "symbolId": "SYM_FIRE", "weight": 0.4 }],
  "cards": ["SYM_TREE", "SYM_RAIN", "SYM_BLOOD", "SYM_FIRE"],
  "canTamper": true, "canPronounce": true, "isDestroyed": false,
  "request": { "requestType": "THIRST", "symbolId": "SYM_RAIN" } }
{ "type": "TAMPER", "version": 1, "index": 1, "symbolId": "SYM_BLOOD" }
{ "type": "PRONOUNCE", "version": 1 }
```

### 夢の状態
- ホストは `DREAM_STATE` を、ペアリングが済んだとき（`PAIRED` の次）と、内容が前に送ったものと変わったときに、ペアリングしたすべての接続に送る。配信（`FRAME`）をしているかどうかには関わらない。毎回、状態を丸ごと送る。
- PC とスマホは、同じ夢を同時に操作できる。先に届いた操作が勝つ。今はできない `TAMPER`・`PRONOUNCE`（夢なし・改ざん済み・崩壊）、夢の範囲外の `index`、`cards` に無い `symbolId` は、ホストが捨てて `Warn` で記録し、その接続に今の `DREAM_STATE` を送り直す。`ERROR` は送らず、接続も切らない。
- コンパニオンは、夢のコマを選んでいる途中の状態を自分だけで持ち、カードを選んだときに `TAMPER` を送る。
- `requestType` は困りごとの ID（`THIRST`・`FAMINE`・`HEAT`・`COLD`・`CHAOS`）。記号と困りごとの名前は、コンパニオンの文言に持つ。

### `ERROR` の `reason`
| reason | いつ |
| :--- | :--- |
| `MAX_CONNECTIONS_REACHED` | すでに 2 台に配信しているときに `START_STREAM` を受け取った |
| `PAIRING_FAILED` | `PAIR` の `token`、`RESUME` の `sessionToken` が合わない。または最初のメッセージが `PAIR`・`RESUME` でない |
| `SESSION_REPLACED` | 同じ `sessionToken` で新しい接続がペアリングしたので、古いこの接続を切る（後勝ち）。コンパニオンは再接続しない |

## バイナリフレーム
| 種別（1バイト目） | 名前 | version（2バイト目） | 向き | 3バイト目以降 | 意味 |
| :--- | :--- | :--- | :--- | :--- | :--- |
| `0x01` | `FRAME` | 1 | ホスト → コンパニオン | JPEG の画像（480×270px。HUD・ボタンを含まない世界の描画だけ） | 星の画面の1コマ |

- `FRAME` は、`START_STREAM` を受け取ってから、`STOP_STREAM` を受け取るか接続が切れるまで、その接続にだけ送る。配信の状態は接続ごとに持ち、接続が切れたら消える（再接続したら、コンパニオンは `START_STREAM` を送り直す）。
- 同時に配信する接続は 2 つまで。
- 送る間隔はホストが決める（既定は 8 fps）。コンパニオンは間隔を当てにしない。送信待ちのデータが残っている接続には、そのコマを送らない。
- 知らない種別・バージョンのバイナリフレームを受け取ったら、受け取った側は捨てる。
- ホストは、コンパニオンからのバイナリフレームを受け付けない（捨てて `Warn` で記録する）。
