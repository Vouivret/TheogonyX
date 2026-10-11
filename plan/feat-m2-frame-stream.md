# plan: M2 星の画面をスマホに配信する（フレームストリーミング）

## 目的
スマホのコンパニオンで、PC に映っている星の画面をリアルタイムに眺めながら、夢のカードを改ざんできるようにする（[ロードマップ](../docs/roadmap.md) の M2）。仕様は [frame-stream.md](../docs/domain/frame-stream.md)。

## スコープ
- 含む
  - ホスト: 世界の描画用の `SubViewport` の画像（480×270px）を JPEG にして、WebSocket のバイナリフレームで 8 fps で送る
  - `START_STREAM`・`STOP_STREAM` を受け取って、接続ごとに配信を始める・やめる（同時に 2 台まで。[メッセージ](../docs/protocol/messages.md)）
  - WebSocket サーバーと、QR コードのワンタイムトークンによるペアリング
  - コンパニオン（素の HTML/JS）: 画面の上部に星の映像（`<canvas>`）、下部に夢のカードの改ざん（入れ替え）の欄を置くレイアウト。画面が表示されている間だけ `START_STREAM` を送る
- 含まない
  - 夢のカードの改ざんのメッセージと処理（レイアウトの枠だけ置く）
  - 音声の配信、480×270px 以外の解像度での配信、インターネット越しの配信

## 決定事項
| 決定 | 理由 | ADR化 |
| :--- | :--- | :--- |
| 画像は WebSocket のバイナリフレームで送る。1 バイト目に種別（`0x01` = `FRAME`）、2 バイト目にバージョン、その後ろに JPEG | Base64 より小さい。`protocol.md` の `type`・`version` をバイナリでも守る | 済み（[ADR 0003](../docs/adr/0003-binary-frame-header.md)） |
| 取るのは世界の描画用の `SubViewport` のテクスチャだけ。HUD とボタンは映さない | スマホには純粋な世界の映像を見せる | しない |
| 8 fps、JPEG の品質 75%。送る間隔は `_Process` の delta で数える | 人間が決めた値。実時間の API を使わない（`project.md` 3章） | しない |
| 同時に配信する接続は 2 台まで | 人間が決めた値。PC の負荷を抑える | しない |
| 送信待ちのデータ（`WebSocketPeer.GetCurrentOutboundBufferedAmount`）が残っている接続には、そのコマを送らない | 回線の遅いスマホに古いコマがたまって遅れていくのを防ぐ | しない |
| 配信中の接続が無ければ、画面の取得も JPEG 変換もしない | PC の負荷を、見ている人がいるときだけにする | しない |
| ヘッダーの組み立て（`FrameHeader`）、メッセージの検証（`CompanionMessageParser`）、接続ごとの配信状態（`StreamSessions`）、送る間隔（`FramePacer`）はコアの `src/TheogonyX.Core/Protocol/` に置く。画面の取得・JPEG 変換・WebSocket は `game/` に置く | xUnit で確かめる。コアを Godot に依存させない | しない |
| コンパニオンは素の HTML/JS。映像は `ArrayBuffer` の先頭 2 バイトを見て、残りを `createImageBitmap` で `<canvas>` に描く | 人間が決めた。依存を足さない | しない |
| ペアリングは QR コードで渡すワンタイムトークンで行う | 人間が決めた | 下の「処理の流れ」が確定したら判断する |

## 処理の流れ

### ホスト: メッセージを受け取ったとき（コアは作成済み）
- ペアリングを済ませていない接続なら、捨てて `Warn`（`event=message_rejected clientId=... reason=not_paired`）。
- バイナリフレームなら、捨てて `Warn`（`reason=binary_from_companion`）。
- `CompanionMessageParser.TryParse` が失敗したら、捨てて `Warn`（`reason` はパーサーが返す理由）。
- `START_STREAM`: `StreamSessions.Start`。
  - `Started` → `Info`（`event=stream_started clientId=...`）。
  - `AlreadyStreaming` → 何もしない。
  - `LimitReached` → `Warn`（`event=stream_rejected clientId=... reason=limit_reached`）。コンパニオンに何を返すかは未決。
- `STOP_STREAM`: `StreamSessions.Stop` が true なら `Info`（`event=stream_stopped clientId=...`）。

### ホスト: 毎フレーム（`_Process`。ログは出さない）
- 配信中の接続が 1 つも無ければ、`FramePacer.Reset()` して終わる。
- `FramePacer.Advance(delta)` が false なら終わる。
- 世界の `SubViewport` の画像を取り、`SaveJpgToBuffer(0.75)` で JPEG にし、`FrameHeader.CreateFrame` でヘッダーを付ける。失敗したらそのコマを飛ばして `Warn`（`event=frame_capture_failed`。同じ失敗が続く間は 1 回だけ）。
- 配信中の接続ごとに: 送信待ちのデータが残っていれば送らない。残っていなければ送る。送信に失敗したら `StreamSessions.Stop` して `Warn`。

### ホスト: 接続が切れたとき
- `StreamSessions.Stop`。true なら `Info`（`event=stream_stopped`）。

### ホスト: メインシーンの組み替え（確認待ち）
- 今は地表・予言者・`CanvasModulate`（環境光）と HUD が同じビューポートにある。地表・予言者・`CanvasModulate` を `SubViewportContainer` > `SubViewport`（480×270）の下に移し、HUD は外に残す。
- `CanvasModulate` は同じ `SubViewport` の中に置かないと、取った画像に昼夜の色が乗らない。
- `WorldScreen`・`AmbienceView` がノードを探すパスを、移した先に合わせて直す。

### ペアリング（確認待ち。コードはまだ書いていない）
- ホストの起動時: 同じ LAN のアドレスで WebSocket サーバーを待ち受ける。プライベートアドレス以外からの接続は、すぐに閉じる。
- トークンを暗号用の乱数（`System.Security.Cryptography.RandomNumberGenerator`）で作り、コンパニオンの URL に入れて QR コードとして画面に出す。トークンはログに出さない。
- コンパニオンは QR コードの URL を開き、WebSocket でつないで、最初のメッセージでトークンを送る。
- ホストはトークンを比べる。合えばその接続をペアリング済みにし、トークンを使用済みにして新しいトークンの QR コードに替える。合わなければ接続を閉じて `Warn`（`event=pairing_failed clientId=...`。トークンの値は出さない）。
- 接続してから一定時間ペアリングしない接続は閉じる。

## テスト観点
- [docs/test/frame-stream.md](../docs/test/frame-stream.md)（自動 10件、手動 1件）
- ペアリングのテスト観点は、処理の流れが確定してから足す。

## 未決
- 3 台目の `START_STREAM` を断ったとき、コンパニオンに何かを返すか。
- コンパニオンの HTML/JS を誰が配るか。Godot には HTTP サーバーが無いため、ホストで小さな HTTP サーバーを持つか、別の方法で配るか。QR コードの URL の形もこれで決まる。
- トークンを URL のどこに入れるか（`#` の後ろに入れれば、サーバーのログや Referer に残らない）。
- ペアリングのメッセージの形（例: `PAIR` に `token`）と、成功・失敗をコンパニオンに返すか。
- ワンタイムトークンのため、切断した後に再接続するとき QR コードを読み直す必要があるか。読み直さないなら、ペアリングのときに再接続用のトークンを渡す仕組みが要る。
- WebSocket のポート番号、ペアリングを待つ時間。
- コンパニオンの言語のルール（`.agent/rules/` の JS 用のルール）と、`protocol.md` の「コンパニオン側の型の作り方」。ルールファイルは AI が書き換えないため、人間が足す。
- `GetImage()` は GPU から画像を読み戻すため、PC 側が一瞬止まる可能性がある。手動テストでフレームレートが落ちたら、取り方を見直す。
