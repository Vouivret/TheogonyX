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
| `ORACLE_STATE` | 3 | ホスト → コンパニオン | `faith`: `{value: 数, max: 数, cost: 数}`、`canSpeak`: 真偽、`isDestroyed`: 真偽、`mode`: `VOICE`・`WORDS`・`SCRIPT`、`listening`: 真偽、`request`: `{requestType: 文字列, symbolId: 文字列, prayer: 文字列か null}` か `null`、`nouns`: 名詞の ID の配列、`verbs`: 動詞の ID の配列、`told`: 直近に告げた神託（下の「告げた神託」）か `null`、`book`: 方言帳の行の配列（古い順。下の「方言帳」） | 神託の今の状態（下の「神託の状態」）。version 1（手札・`pairs`）と 2 は廃止した。version 3 で `mode` に `SCRIPT`（PC で字を組む神託）を足した |
| `SPEAK` | 1 | コンパニオン → ホスト | `text`: 文字列（1〜80 字。改行・制御文字を含まない） | 神の言葉を文で書いて予言者に語りかける（`mode` が `VOICE` のとき） |
| `PRONOUNCE` | 3 | コンパニオン → ホスト | `noun`: 名詞の ID の文字列か `null`、`verb`: 動詞の ID の文字列か `null`、`destination`: `NONE`・`EAST`・`WEST` | 名詞・動詞・行き先を選んで予言者に語りかける（`mode` が `WORDS` のとき）。3 つのうち少なくとも 1 つは「なし」でない。version 2（手札の 2 枚）と version 1 は廃止した |

例:

```json
{ "type": "PAIR", "version": 1, "token": "q9V3..." }
{ "type": "PAIRED", "version": 1, "sessionToken": "Xk2p..." }
{ "type": "START_STREAM", "version": 1 }
{ "type": "ERROR", "version": 1, "reason": "MAX_CONNECTIONS_REACHED" }
{ "type": "ORACLE_STATE", "version": 3,
  "faith": { "value": 2.0, "max": 3.0, "cost": 1.0 },
  "canSpeak": true, "isDestroyed": false, "mode": "VOICE", "listening": false,
  "request": { "requestType": "THIRST", "symbolId": "SYM_WATER", "prayer": "神よ、森は渇き、川も細っています" },
  "nouns": [], "verbs": [],
  "told": { "year": 312, "teaching": { "symbolId": "SYM_TREE", "dogmaType": "DESTRUCTION" },
    "heardNounId": "NOUN_FOREST", "heardVerbId": "VERB_CUT", "destination": "NONE", "mysteryWords": ["電気"],
    "heardText": null, "narration": null },
  "book": [ { "godWord": "VERB_PROTECT", "heardWordId": "VERB_CUT", "reason": "DIALECT" } ] }
{ "type": "SPEAK", "version": 1, "text": "森を守れ" }
{ "type": "PRONOUNCE", "version": 3, "noun": "NOUN_FOREST", "verb": "VERB_PROTECT", "destination": "EAST" }
```

### 神託の状態
- ホストは `ORACLE_STATE` を、ペアリングが済んだとき（`PAIRED` の次）と、内容が前に送ったものと変わったときに、ペアリングしたすべての接続に送る。配信（`FRAME`）をしているかどうかには関わらない。毎回、状態を丸ごと送る。
- 手札（予言者の語彙）は送らない。見せないため（[神託の作り方](../domain/oracle-pair.md)）。
- `mode` は語りかけ方。`SCRIPT`（字の神託。[神と人の文字](../domain/script-decipher.md)）のときは、字は PC の画面で組むので、スマホからは語りかけられない（`canSpeak` は偽）。コンパニオンは案内だけを出す。`VOICE`（LLM が使える）のときは `SPEAK` で文を送り、`WORDS`（LLM が使えない、または聞き取りに失敗した）のときは `PRONOUNCE` で言葉を選んで送る。`WORDS` のときだけ、`nouns`（予言者が知っている名詞の ID）と `verbs`（全部の動詞の ID）を入れる。`VOICE` のときは空の配列。語の名前は、コンパニオンの文言（`WORD_<ID>`）に持つ。
- `listening` が真のあいだは、予言者が聞いている途中なので、語りかけられない。`canSpeak` は、崩壊しておらず、神威がコスト以上で、聞いている途中でないときだけ真。
- PC とスマホは、同時に語りかけられる。先に届いた操作が勝つ。今は語りかけられない `SPEAK`・`PRONOUNCE`（崩壊・神威がコスト未満・聞いている途中・`mode` の違い（`SCRIPT` を含む）・選んだ言葉が定義に無い）は、ホストが捨てて `Warn` で記録し、その接続に今の `ORACLE_STATE` を送り直す。`ERROR` は送らず、接続も切らない。
- 受け取った `text`・`noun`・`verb` は検証していない外部入力として扱う。`text` は長さと制御文字を確かめ、ファイルのパスやコマンドに使わず、ログには長さだけを出す。`noun`・`verb` は、長さを確かめた上で、語の一覧にあるかをコアが確かめる。
- `request.prayer` は、予言者の祈りの文（ホストの言語。LLM が作った文か、定型文）。まだ無ければ `null` で、コンパニオンは依頼の定型の文を出す。`requestType` は困りごとの ID（`THIRST`・`FAMINE`・`HEAT`・`COLD`・`CHAOS`・`UNREST`・`RUIN`）。
- `faith.value` は今の神威、`faith.max` は上限、`faith.cost` は神託 1 回のコスト。

#### 告げた神託
`told` は、直近の神託の記録。`year`: 整数、`teaching`: `{symbolId, dogmaType}` か `null`（行き先だけの神託）、`heardNounId`・`heardVerbId`: 聞こえた名詞・動詞の ID か `null`、`destination`: `NONE`・`EAST`・`WEST`（聞こえた行き先）、`mysteryWords`: 謎の言葉の配列、`heardText`・`narration`: LLM が作った聞こえた文と語り（まだ無ければ `null`）。`dogmaType` は `WORSHIP`・`TABOO`・`DESTRUCTION`。コンパニオンは、掟の 1 行を `teaching` から、聞こえた言葉を ID から、コンパニオンの文言で組み立てる。`heardText`・`narration` は文字列としてそのまま出す（`textContent`）。

#### 方言帳
`book` の 1 行は `godWord`: 神の言葉（概念の ID。`MYSTERY` のときは文の言葉そのまま）、`heardWordId`: 予言者に聞こえた言葉の ID か `null`、`reason`: `DIALECT`（この予言者の言葉）・`UNKNOWN`（知らない言葉）・`KNOWN`（予言者が知っている言葉。`heardWordId` は `null`）・`MYSTERY`（謎の言葉。`heardWordId` は `null`）。

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
