# テスト仕様書: 画面の配信（フレームストリーミング）

- 受け入れ条件: [docs/domain/frame-stream.md](../domain/frame-stream.md)
- 関連ADR: [0003](../adr/0003-binary-frame-header.md)

## テスト観点

種別は「自動」「手動」「廃止」のどれか。自動テストの手順はテストコードに書き、ここには書かない。

| ID | 受け入れ条件 | 観点 | 前提 | 操作 | 期待結果 | 種別 | 実装場所 |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| TS-frame-stream-001 | AC-frame-stream-001 | 空きがあれば配信中になる | 受け入れ条件のとおり | `START_STREAM` | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/StreamSessionsTests.cs` |
| TS-frame-stream-002 | AC-frame-stream-002 | 3 台目は配信中にならない（`ERROR` と切断は TS-frame-stream-011 で確かめる） | 受け入れ条件のとおり | `START_STREAM` | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/StreamSessionsTests.cs` |
| TS-frame-stream-003 | AC-frame-stream-003 | 停止で枠が空く | 受け入れ条件のとおり | `STOP_STREAM` → `START_STREAM` | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/StreamSessionsTests.cs` |
| TS-frame-stream-004 | AC-frame-stream-004 | 切断で状態が消え、枠が空く | 受け入れ条件のとおり | 切断 → `START_STREAM` | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/StreamSessionsTests.cs` |
| TS-frame-stream-005 | — | 状態遷移表の「何もしない」マス（配信中の `START_STREAM`、配信していないときの `STOP_STREAM`） | 各状態 | 操作する | 状態が変わらない | 自動 | `tests/TheogonyX.Core.Tests/StreamSessionsTests.cs` |
| TS-frame-stream-006 | AC-frame-stream-005 | 間隔に達したら 1 コマ送る | 受け入れ条件のとおり | 経過時間を渡す | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/FramePacerTests.cs` |
| TS-frame-stream-007 | AC-frame-stream-006 | 処理落ちしてもコマをまとめて送らない | 受け入れ条件のとおり | 経過時間を渡す | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/FramePacerTests.cs` |
| TS-frame-stream-008 | AC-frame-stream-007 | `FRAME` のヘッダー | 受け入れ条件のとおり | 組み立てる | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/FrameHeaderTests.cs` |
| TS-frame-stream-009 | AC-frame-stream-008 | 受け付けるメッセージ（`START_STREAM`・`STOP_STREAM` の version 1） | — | 読み取る | 種類が分かる | 自動 | `tests/TheogonyX.Core.Tests/CompanionMessageParserTests.cs` |
| TS-frame-stream-010 | AC-frame-stream-008 | 捨てるメッセージ（`type` が無い、JSON でない、知らない `type`・`version`） | — | 読み取る | 失敗し、理由が分かる | 自動 | `tests/TheogonyX.Core.Tests/CompanionMessageParserTests.cs` |
| TS-frame-stream-012 | AC-frame-stream-005 | 経過時間が負・NaN・無限大なら例外にし、送る間隔の計算を壊さない | — | 経過時間を渡す | 例外になり、その後の 0.125 秒で 1 コマ送る | 自動 | `tests/TheogonyX.Core.Tests/FramePacerTests.cs` |
| TS-frame-stream-011 | AC-frame-stream-009, 010, 011, 012 | スマホでの見え方・止まり方 | ホストとコンパニオンが動く | 下の手順 | 下の手順のとおり | 手動 | — |

## 手動テストの手順

### TS-frame-stream-011
WebSocket サーバー・ペアリング・コンパニオンができてから書く。確かめること:
1. ペアリングせずに `START_STREAM` を送っても映像が届かない（AC-frame-stream-009）。3 台目のスマホで映像を開くと、エラーが出て切断される（AC-frame-stream-002）。
2. 回線を絞ったスマホと、絞っていないスマホで同時に見て、絞っていないほうの映像が遅れない（AC-frame-stream-010）。
3. スマホの画面を消す・戻すと、ホストのログに `stream_stopped`・`stream_started` が出る（AC-frame-stream-011）。
4. 映像が PC と同じように昼夜・予言者が変わり、HUD とボタンが映らない。配信中も PC のフレームレートが落ちない（AC-frame-stream-012）。

## 対象外

なし。
