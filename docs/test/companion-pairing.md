# テスト仕様書: コンパニオンのペアリング

- 受け入れ条件: [docs/domain/companion-pairing.md](../domain/companion-pairing.md)
- 関連ADR: なし

## テスト観点

種別は「自動」「手動」「廃止」のどれか。自動テストの手順はテストコードに書き、ここには書かない。

| ID | 受け入れ条件 | 観点 | 前提 | 操作 | 期待結果 | 種別 | 実装場所 |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| TS-companion-pairing-001 | AC-companion-pairing-001 | 合うトークンでペアリング済みになり、トークンが替わる | 受け入れ条件のとおり | `PAIR` | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/CompanionConnectionTests.cs` |
| TS-companion-pairing-002 | AC-companion-pairing-002 | 使用済みのトークンでは切断される | 受け入れ条件のとおり | `PAIR` | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/CompanionConnectionTests.cs` |
| TS-companion-pairing-003 | AC-companion-pairing-003 | `sessionToken` で戻れる | 受け入れ条件のとおり | `RESUME` | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/CompanionConnectionTests.cs` |
| TS-companion-pairing-004 | AC-companion-pairing-004 | 知らない `sessionToken` では切断される | 受け入れ条件のとおり | `RESUME` | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/CompanionConnectionTests.cs` |
| TS-companion-pairing-005 | AC-companion-pairing-005 | 未ペアリングのほかのメッセージで切断される | 受け入れ条件のとおり | `START_STREAM` | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/CompanionConnectionTests.cs` |
| TS-companion-pairing-006 | AC-companion-pairing-006 | ペアリング済みの `PAIR`・`RESUME` は捨て、トークンを照合しない | 受け入れ条件のとおり | `PAIR`・`RESUME` | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/CompanionConnectionTests.cs` |
| TS-companion-pairing-007 | AC-companion-pairing-008 | 同じ LAN の判定（10/8・172.16/12・192.168/16・169.254/16、IPv6 の fe80::/10・fc00::/7、ループバックを受け付け、それ以外を断る） | — | 判定する | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/LanAddressTests.cs` |
| TS-companion-pairing-008 | AC-companion-pairing-001, 003, 004, 005 | ホストが返すメッセージ（`PAIRED`・`ERROR`）の形 | — | 組み立てる | [messages.md](../protocol/messages.md) のとおり | 自動 | `tests/TheogonyX.Core.Tests/HostMessageWriterTests.cs` |
| TS-companion-pairing-009 | AC-companion-pairing-001, 003, 007 | スマホで QR コードを読んでつながり、切断から戻れる | ホストとコンパニオンが動く | 下の手順 | 下の手順のとおり | 手動 | — |
| TS-companion-pairing-010 | AC-companion-pairing-002, 004 | `PAIR`・`RESUME` の読み取り（トークンが無い・空・文字列でない・128 文字を超えるものを捨てる） | — | 読み取る | 失敗し、理由が `invalid_token` | 自動 | `tests/TheogonyX.Core.Tests/CompanionMessageParserTests.cs` |
| TS-companion-pairing-011 | — | HTTP の要求の読み取りと応答（GET だけ、クエリを除いたパスで一覧を引く、配る拡張子だけ、トークンを外に出さないヘッダー） | — | 読み取る・組み立てる | GET 以外は 405、形が違えば 400。パスはファイルパスに使わない | 自動 | `tests/TheogonyX.Core.Tests/CompanionHttpTests.cs` |
| TS-companion-pairing-012 | AC-companion-pairing-007 | QR コードに使う PC のアドレスの選び方（先頭から、リンクローカルでないプライベートな IPv4。ループバック・IPv6・リンクローカルは使わない） | — | 選ぶ | 表のとおり | 自動 | `tests/TheogonyX.Core.Tests/LanAddressTests.cs` |

## 手動テストの手順

### TS-companion-pairing-009
WebSocket サーバー・HTTP 配信・コンパニオンができてから書く。確かめること:
1. QR コードを読んでページを開くと、ペアリングが済み、アドレス欄に `token` が残っていない。PC の QR コードが別のものに替わる（AC-companion-pairing-001, 007）。
2. Wi-Fi を一度切ってつなぎ直すと、QR コードを読み直さずに戻る（AC-companion-pairing-003）。
3. ホストのログに、トークン・`sessionToken` の値が出ていない。

## 対象外

なし。
