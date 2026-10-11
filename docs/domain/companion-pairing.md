---
status: approved   # draft | approved | as-is
---

# 仕様書: コンパニオンのペアリング

- テスト仕様書: [docs/test/companion-pairing.md](../test/companion-pairing.md)
- 関連ADR: なし
- 通信: [messages.md](../protocol/messages.md) の `PAIR`・`RESUME`・`PAIRED`・`ERROR`
- 画面デザイン: なし（PC の画面に QR コードを出す）

## 目的

同じ LAN にいる、PC の画面を見ている人のスマホだけを、ホストにつなげるようにする。一時的に切断しても、QR コードを読み直さずに戻れるようにする。

## ゲームのルール

用語は [用語集](glossary.md) に合わせる。

- ホストは起動時に、暗号用の乱数でワンタイムトークンを作り、`http://<LAN の IP>:8080/?token=<トークン>` を QR コードにして PC の画面に出す。
- ホストは、ポート 8080 からコンパニオンの HTML/JS を配る（`HttpListener` は LAN で待ち受けるのに管理者権限が要るため、`TcpListener` で応答する）。配るのは `companion/` の中の決まった拡張子のファイルだけで、要求のパスをファイルパスに使わない。
- コンパニオンは URL の `token` を読み取ったら、すぐに URL から消し、WebSocket でつないで `PAIR` を送る。
- トークンが合えば、ホストはその接続をペアリング済みにし、`sessionToken`（暗号用の乱数）を `PAIRED` で渡す。ワンタイムトークンは使用済みにし、すぐに新しいトークンを作って QR コードを替える。
- コンパニオンは `sessionToken` を `sessionStorage` に置く。切断の後は `RESUME` で `sessionToken` を送り、合えばペアリング済みに戻る。`sessionToken` は、ホストを終了するまで使える。
- トークンが合わない、または最初のメッセージが `PAIR`・`RESUME` でないときは、`ERROR`（`PAIRING_FAILED`）を返して接続を閉じる。
- WebSocket はポート 8081 で待ち受ける。接続してから 60 秒たっても未ペアリングの接続は閉じる（何も返さない）。
- 同じ LAN（IPv4 のプライベートアドレス・リンクローカル、IPv6 のリンクローカル・ユニークローカル、ループバック）以外からの接続は、すぐに閉じる。
- トークンと `sessionToken` の値はログに出さない。

### 状態遷移

接続ごとのペアリングの状態。接続したときは「未ペアリング」。

| 現在の状態 ＼ きっかけ | `PAIR` | `RESUME` | ほかのメッセージ |
| :--- | :--- | :--- | :--- |
| 未ペアリング | トークンが合えばペアリング済み、合わなければ切断 | `sessionToken` が合えばペアリング済み、合わなければ切断 | 切断 |
| ペアリング済み | 不可（捨てる。トークンは照合しない） | 不可（捨てる） | ペアリング済み（メッセージを処理する） |

## 受け入れ条件

| ID | Given（前提） | When（操作） | Then（結果） |
| :--- | :--- | :--- | :--- |
| AC-companion-pairing-001 | QR コードのトークンがある | 同じトークンで `PAIR` を送る | ペアリング済みになり、`sessionToken` を受け取る。QR コードのトークンが新しいものに替わる |
| AC-companion-pairing-002 | あるスマホが QR コードのトークンでペアリングした | 別のスマホが同じ（古い）トークンで `PAIR` を送る | `ERROR`（`PAIRING_FAILED`）を受け取って切断される |
| AC-companion-pairing-003 | ペアリングして `sessionToken` を受け取り、切断した | 再接続して `RESUME` でその `sessionToken` を送る | ペアリング済みに戻り、同じ `sessionToken` を受け取る |
| AC-companion-pairing-004 | 未ペアリングの接続 | 知らない `sessionToken` で `RESUME` を送る | `ERROR`（`PAIRING_FAILED`）を受け取って切断される |
| AC-companion-pairing-005 | 未ペアリングの接続 | `START_STREAM` を送る | `ERROR`（`PAIRING_FAILED`）を受け取って切断される |
| AC-companion-pairing-006 | ペアリング済みの接続 | `PAIR` を送る | 捨てられ、接続はそのまま。QR コードのトークンは替わらない |
| AC-companion-pairing-007 | PC で QR コードが表示されている | スマホで QR コードを読み、ページを開く | ページが表示され、ペアリングが済み、アドレス欄から `token` が消えている |
| AC-companion-pairing-008 | 同じ LAN に無い接続元 | 接続する | すぐに閉じられる |

## 対象外

- インターネット越しの接続: [nfr.md](../nfr.md) のセキュリティのとおり。
- `sessionToken` の有効期限・ホストを再起動した後の再接続: ホストを終了するまで使える、とした。再起動したら QR コードを読み直す。
- HTTPS・WSS: 同じ LAN の中だけで使うため。

## 未決

- 発行した `sessionToken` は、ホストを終了するまで増え続け、取り消せない。数の上限や、取り消す手段を設けるか。
- PC に LAN のアドレスが複数あるとき（仮想のネットワークアダプターなど）、QR コードにどれを使うか。今は、見つかった順で最初のプライベートな IPv4 を使う。
- 同じ `sessionToken` の接続が 2 つあるとき（古い接続が切れたと気付く前に `RESUME` した場合など）、古いほうを閉じるか。
