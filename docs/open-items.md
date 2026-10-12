# 未決・残作業の一覧

決まっていないこと、人間の手作業が要ることの一覧。解消したら行を消し、決まった内容は該当する仕様書・ルールに書く。

最終更新: 2026-10-12（M2 の画面の配信とペアリングの手動テストの後）

## 人間の手作業

| 項目 | 内容 | 関連 |
| :--- | :--- | :--- |
| 手動テスト TS-ambience-006 | 昼夜の色・予言者の位置とアニメーション・崩壊とリセットの見た目を確かめ、`docs/test/results/` に記録する。地表・予言者・環境光を `SubViewport` の下に移したので、見た目が変わっていないことも確かめる | [docs/test/ambience.md](test/ambience.md) |
| 崩壊の見た目の確認 | TS-time-control-009 の手順 5 と TS-ambience-006 の手順 5。夢の改ざんで崩壊を起こせるようになったので、確認できる | [実施記録](test/results/2026-10-11-feat-m1-screen.md) |
| 手動テスト TS-prophet-request-006 | 依頼の吹き出し（予言者の右）が出て、画面に収まり、神託で消えること。TS-dream-tampering-008 は合格（[実施記録](test/results/2026-10-12-feat-m2-dream-tampering.md)） | [dream-tampering](test/dream-tampering.md)・[prophet-request](test/prophet-request.md) |
| 手動テスト TS-time-control-009 の手順 3 | 速さのボタンが 通常 → 高速 → 現実同期 → 通常 と回ること。TS-time-control-011 は合格（[実施記録](test/results/2026-10-12-feat-m2-real-time-sync.md)） | [plan](../plan/feat-m2-real-time-sync.md) |
| 手動テスト TS-save-data-005 の残り | 自動進行中に閉じても止まって始まる・強制終了・壊れたセーブ。続きから始まることは確認済み（[実施記録](test/results/2026-10-12-feat-m2-save-load.md)） | [plan](../plan/feat-m2-save-load.md) |
| 手動テスト TS-dream-tampering-012・TS-prophet-request-008 | スマホからの改ざん・神託、PC との同時の操作、スマホの依頼の吹き出し、切断したら押せなくなること。ブラウザで偽のホストを使った表示と送信は確認済み。実機と Godot では未確認 | [plan](../plan/feat-m2-companion-dream.md) |
| 手動テスト TS-companion-pairing-015 | 接続の上限・後勝ち・古い `sessionToken` の接続を切る・全セッションリセット・IP の切り替え | [docs/test/companion-pairing.md](test/companion-pairing.md) |
| `main.tscn` をエディタで保存する | 手で足したノード（`HostAddressButton`・`ResetSessionsButton`、夢の欄の `RequestBubble`・`DreamPanel` とその子）に、Godot エディタが ID（`unique_id`）を付ける。差分が出たらコミットする | `.agent/rules/godot.md` |
| 手動テスト TS-frame-stream-011 の残り | 画面を消すと止まり戻すと再開すること、3 台目は断られること、回線の遅いスマホと同時に見たときの遅れ、配信中の PC のフレームレート。映像が見られることと HUD が映らないことは確認済み | [実施記録](test/results/2026-10-12-feat-m2-frame-stream.md) |

## 決めること

| 項目 | 内容 | 期限 |
| :--- | :--- | :--- |
| 予言者の絵の出典 | 生成 AI のツール名と、利用規約で商用利用・ゲームへの組み込みが認められているか。分かったら [credits.md](credits.md) に書く | 配布する前 |
| 英語の文言 | `game/locales/ui.csv` の `en` の列は仮の訳。`companion/locales/en.json` は 2026-10-12 に確定した訳を当てたが、`TITLE`・`CARDS_*`・`STATUS_SCAN_QR`・`STATUS_RECONNECTING`・`ERROR_SESSION_REPLACED` はまだ仮の訳 | 英語版を出す前 |
| 用語集への追加 | タイル種別・自動進行・世界再創生・環境光・昼夜の周期、ペアリング・ワンタイムトークン・`sessionToken`・画面の配信、神託・依頼（祈り）・困りごとを [用語集](domain/glossary.md) に足すか | 急がない |
| 画面の配信の仕様書の承認 | [frame-stream.md](domain/frame-stream.md) は `draft` のまま | 次に配信を変える前 |
| 夢の改ざん・依頼の仕様書の承認 | [dream-tampering.md](domain/dream-tampering.md)・[prophet-request.md](domain/prophet-request.md) は `draft` のまま（スマホでの操作も足した） | M2 を閉じる前 |

## 急がないもの

| 項目 | 内容 |
| :--- | :--- |
| CI の警告 | GitHub Actions の Node 20 の actions と、`ubuntu-latest` の移行に関する警告 |
| 空の背景 | 地表の外の灰色の部分には環境光がかからない（画面の地の色のため）。空の背景を置くか |
| WebSocket のポートの二重管理 | `game/data/companion.json` と `companion/js/protocol.js` の 2 か所にある。ホストからページに渡すなどして 1 か所にするか |
| 毎フレームの `ToList` | `CompanionHost.Process` で毎フレームリストを作っている（`csharp.md` では Phase 3 で直す） |
| HTTP の要求の読み直し | `CompanionHttpServer.ReadRequestHeadAsync` が、読むたびに先頭から文字列にしている |
