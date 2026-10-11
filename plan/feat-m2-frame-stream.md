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
| 地表・予言者・`CanvasModulate` を `SubViewportContainer`（`WorldView`）> `SubViewport`（`%WorldViewport`、480×270）の下にまとめ、その `SubViewport` のテクスチャだけを取る。HUD とボタンは映さない | スマホには純粋な世界の映像を見せる。`CanvasModulate` も同じ `SubViewport` に置くと、取った画像に環境光が乗る | しない |
| 8 fps、JPEG の品質 75%。送る間隔は `_Process` の delta で数える | 人間が決めた値。実時間の API を使わない（`project.md` 3章） | しない |
| 同時に配信する接続は 2 台まで。超えた `START_STREAM` には `ERROR`（`MAX_CONNECTIONS_REACHED`）を返して切断する | 人間が決めた | しない |
| 送信待ちのデータ（`WebSocketPeer.GetCurrentOutboundBufferedAmount`）が残っている接続には、そのコマを送らない | 回線の遅いスマホに古いコマがたまって遅れていくのを防ぐ | しない |
| 配信中の接続が無ければ、画面の取得も JPEG 変換もしない | PC の負荷を、見ている人がいるときだけにする | しない |
| ペアリングは、暗号用の乱数で作ったワンタイムトークンを QR コード（`http://<LAN の IP>:8080/?token=...`）で渡す。使ったらすぐに新しいトークンの QR コードに替える | 人間が決めた | しない |
| ペアリングが済んだら `sessionToken` を発行し、コンパニオンは `sessionStorage` に置く。切断の後は `RESUME` で戻る。同時に有効なのは 2 個まで（超えたら一番古いものを無効にし、その接続を切る）。ホストの終了か「全セッションリセット」ですべて無効になる | 人間が決めた。一時的な切断から QR コードを読み直さずに戻れる | しない |
| 同じ `sessionToken` の接続は後勝ち。古い接続には `ERROR`（`SESSION_REPLACED`）を送って切り、コンパニオンは再接続しない | 人間が決めた。何も送らずに切ると、古いタブが再接続して取り合いになる | しない |
| 未ペアリングの接続は同時に 5 つまで。超えた接続はすぐに切る | 人間が決めた。不正な接続でのリソースの枯渇を防ぐ | しない |
| QR コードの IP は、ゲートウェイのある物理の LAN を優先し、192.168 > 172.16〜31 > 10 の順で選ぶ。候補が 2 つ以上なら HUD のボタンで切り替えられる | 人間が決めた。仮想アダプターが選ばれたときに直せる | しない |
| 書き出した後は、実行ファイルの横の `companion/` を読み、無ければリポジトリ直下の `companion/` を読む。場所は `OS.GetExecutablePath()` から求める | 人間が決めた。`AppDomain.CurrentDomain.BaseDirectory` は書き出した後に `data_` フォルダを指すため使わない | しない |
| コンパニオンの HTML/JS は、ホストが `TcpListener` で作った GET だけの小さな HTTP サーバーでポート 8080 から配る。配るのは起動時に読んだ `companion/` の一覧だけ | `HttpListener` は LAN で待ち受けるのに管理者権限が要るため（人間が決めた） | しない |
| WebSocket はポート 8081、ペアリングを待つのは 60 秒。ポート・待つ時間・配信の値は `game/data/companion.json` に置く | 人間が決めた。運用手順のとおり、待ち受けポートは設定ファイルで渡す | しない |
| QR コードは NuGet の QRCoder 1.8.0（MIT）で PNG を作り、HUD の `%PairingQr` に表示する（1 モジュール = 1 ピクセル） | 人間が決めた。.NET にも Godot にも QR コードを作る機能が無い | しない |
| コンパニオンは素の HTML/JS（`.agent/rules/js.md`）。映像は `ArrayBuffer` の先頭 2 バイトを見て、残りを `createImageBitmap` で `<canvas>` に描く | 人間が決めた。依存を足さない | しない |
| メッセージの検証（`CompanionMessageParser`）、返すメッセージ（`HostMessageWriter`）、ペアリング（`PairingRegistry`・`CompanionConnection`）、接続元の判定（`LanAddress`）、配信（`FrameHeader`・`StreamSessions`・`FramePacer`）はコアの `src/TheogonyX.Core/Protocol/` に置く。トークンの乱数・ソケット・HTTP・画面の取得は `game/` に置く | xUnit で確かめる。コアを Godot に依存させず、乱数も持たせない | しない |

## 処理の流れ

### ホスト: 起動時
- `PairingRegistry` を、暗号用の乱数（`RandomNumberGenerator.GetBytes(32)` を Base64URL にしたもの）でトークンを作る関数で作る。
- `TcpListener` で 8080 から `companion/` のファイルを配る。URL のクエリ（`token`）はログに出さない。
- WebSocket サーバー（Godot の `TCPServer` + `WebSocketPeer`）を待ち受ける。
- LAN の IP を選び、`http://<IP>:8080/?token=<CurrentToken>` の QR コードを画面に出す。

### ホスト: 接続を受け付けたとき
- 接続元が `LanAddress.IsPrivate` でなければ、すぐに閉じて `Warn`（`event=connection_rejected reason=not_private_address`。IP は出さない）。
- 接続ごとに `clientId`（連番）と `CompanionConnection` を作り、ペアリングを待つ時間を数え始める。

### ホスト: テキストフレームを受け取ったとき
- `CompanionMessageParser.TryParse` が失敗したら: 未ペアリングなら `ERROR`（`PAIRING_FAILED`）を返して閉じる。ペアリング済みなら捨てる。どちらも `Warn`（`event=message_rejected clientId=... reason=...`）。
- `CompanionConnection.Handle` の結果で分ける:
  - `Paired` → `PAIRED` を返し、`Info`（`event=companion_paired clientId=...`）。`PAIR` だったら QR コードを `CurrentToken` で描き直す。
  - `Rejected` → `ERROR`（`PAIRING_FAILED`）を返して閉じ、`Warn`（`event=pairing_failed clientId=...`）。
  - `Ignored` → 捨てて `Warn`（`event=message_rejected clientId=... reason=already_paired`）。
  - `Accepted` → 下の配信の処理へ。
- `START_STREAM`: `StreamSessions.Start`。
  - `Started` → `Info`（`event=stream_started clientId=...`）。
  - `AlreadyStreaming` → 何もしない。
  - `LimitReached` → `ERROR`（`MAX_CONNECTIONS_REACHED`）を返して閉じ、`Warn`（`event=stream_rejected clientId=... reason=max_connections_reached`）。
- `STOP_STREAM`: `StreamSessions.Stop` が true なら `Info`（`event=stream_stopped clientId=...`）。
- バイナリフレームを受け取ったら、捨てて `Warn`（`reason=binary_from_companion`）。

### ホスト: 毎フレーム（`_Process`。ログは出さない）
- 待つ時間が過ぎても `CompanionConnection.IsPaired` が false の接続は閉じる。
- 配信中の接続が 1 つも無ければ、`FramePacer.Reset()` して終わる。
- `FramePacer.Advance(delta)` が false なら終わる。
- `%WorldViewport` のテクスチャの画像を取り、`SaveJpgToBuffer(0.75)` で JPEG にし、`FrameHeader.CreateFrame` でヘッダーを付ける。失敗したらそのコマを飛ばして `Warn`（`event=frame_capture_failed`。同じ失敗が続く間は 1 回だけ）。
- 配信中の接続ごとに: 送信待ちのデータが残っていれば送らない。残っていなければ送る。送信に失敗したら `StreamSessions.Stop` して `Warn`。

### ホスト: 接続が切れたとき
- `StreamSessions.Stop`。true なら `Info`（`event=stream_stopped`）。`CompanionConnection` を消す。

### コンパニオン
- 開いたとき: URL に `token` があれば読み取り、`history.replaceState` で URL から消す。WebSocket でつなぎ、`PAIR` を送る。`token` が無く、`sessionStorage` に `sessionToken` があれば `RESUME` を送る。どちらも無ければ「PC の QR コードを読んでください」と出す。
- `PAIRED` を受け取ったら `sessionToken` を `sessionStorage` に置き、画面が表示されていれば `START_STREAM` を送る。
- `ERROR` を受け取ったら、`reason` に応じた文言を出す（`PAIRING_FAILED` のときは `sessionStorage` の `sessionToken` を消す）。自動では再接続しない。
- `ERROR` 以外で切断したら、少し待って再接続し、`RESUME` を送る。
- 画面が隠れたら（`visibilitychange` で `hidden`）`STOP_STREAM`、表示されたら `START_STREAM` を送る。
- バイナリフレームは、1バイト目が `0x01`・2バイト目が 1 のときだけ、3バイト目以降を `createImageBitmap` で `<canvas>` に描く。描き終わる前に次のコマが来たら、古いほうを捨てる。
- レイアウト: 上部に星の映像（16:9、横幅いっぱい、`imageSmoothingEnabled = false`）、下部に夢のカードの欄（枠だけ）。

## テスト観点
- [docs/test/frame-stream.md](../docs/test/frame-stream.md)（自動 10件、手動 1件）
- [docs/test/companion-pairing.md](../docs/test/companion-pairing.md)（自動 9件、手動 1件）

## 未決
- WebSocket のポートは `game/data/companion.json` と `companion/js/protocol.js` の 2 か所にある。片方だけ変えると、つながらなくなる。
- `GetImage()` は GPU から画像を読み戻すため、PC 側が一瞬止まる可能性がある。手動テストでフレームレートが落ちたら、取り方を見直す。
