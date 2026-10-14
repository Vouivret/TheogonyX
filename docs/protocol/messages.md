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
| `ORACLE_STATE` | 1 | ホスト → コンパニオン | `hand`: 手札の記号 ID の配列（同じ記号が重なりうる。順番は手札の順）、`faith`: `{value: 数, max: 数, cost: 数}`、`canPronounce`: 真偽、`isDestroyed`: 真偽、`request`: `{requestType: 文字列, symbolId: 文字列}` か `null` | 神託の今の状態（下の「神託の状態」） |
| `PRONOUNCE` | 2 | コンパニオン → ホスト | `first`: 0 以上の整数、`second`: 0 以上の整数 | 手札の `first` 番（1 枚目。ドグマの対象）と `second` 番（2 枚目）のルーンで神託を下す。番号は 0 から数える。version 1（引数なし）と `TAMPER` は廃止した |

例:

```json
{ "type": "PAIR", "version": 1, "token": "q9V3..." }
{ "type": "PAIRED", "version": 1, "sessionToken": "Xk2p..." }
{ "type": "START_STREAM", "version": 1 }
{ "type": "ERROR", "version": 1, "reason": "MAX_CONNECTIONS_REACHED" }
{ "type": "ORACLE_STATE", "version": 1,
  "hand": ["SYM_FIRE", "SYM_TREE", "SYM_TREE", "SYM_WATER"],
  "faith": { "value": 2.0, "max": 3.0, "cost": 1.0 },
  "canPronounce": true, "isDestroyed": false,
  "request": { "requestType": "THIRST", "symbolId": "SYM_WATER" } }
{ "type": "PRONOUNCE", "version": 2, "first": 1, "second": 3 }
```

### 神託の状態
- ホストは `ORACLE_STATE` を、ペアリングが済んだとき（`PAIRED` の次）と、内容が前に送ったものと変わったときに、ペアリングしたすべての接続に送る。配信（`FRAME`）をしているかどうかには関わらない。毎回、状態を丸ごと送る。
- PC とスマホは、同じ手札から同時に神託を下せる。先に届いた操作が勝つ。今は下せない `PRONOUNCE`（崩壊・手札が 2 枚未満・神威がコスト未満）、手札の範囲外の番号、同じ番号 2 つは、ホストが捨てて `Warn` で記録し、その接続に今の `ORACLE_STATE` を送り直す。`ERROR` は送らず、接続も切らない。
- コンパニオンは、手札から 2 枚を選んでいる途中の状態を自分だけで持ち、「神託を下す」を押したときに `PRONOUNCE` を送る。
- `requestType` は困りごとの ID（`THIRST`・`FAMINE`・`HEAT`・`COLD`・`CHAOS`）。記号と困りごとの名前は、コンパニオンの文言に持つ。
- `hand` には、基本ルーン（記号の ID。例: `SYM_TREE`）に加えて、汚染ルーンの ID（例: `RUNE_MADNESS`）と上位のルーンの ID（例: `RUNE_GREAT_TREE`）が入りうる。名前は、コンパニオンの文言（`SYMBOL_<ID>`）に持つ。`PRONOUNCE` の `first`・`second` は、手札の番号なので変わらない。
- `faith.value` は今の神威、`faith.max` は上限、`faith.cost` は神託 1 回のコスト。`canPronounce` は、崩壊しておらず、手札が 2 枚以上あり、`faith.value` が `faith.cost` 以上のときだけ真。

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
