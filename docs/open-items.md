# 未決・残作業の一覧

決まっていないこと、人間の手作業が要ることの一覧。解消したら行を消し、決まった内容は該当する仕様書・ルールに書く。

最終更新: 2026-10-12（M2 の画面の配信とペアリングを実装した時点）

## 人間の手作業

| 項目 | 内容 | 関連 |
| :--- | :--- | :--- |
| `main.tscn` をエディタで保存する | 手で足したノード（`Ambience`・`Prophet` に加え、`WorldView`・`WorldViewport`・`PairingQr`）に、Godot エディタが ID（`unique_id`）を付ける。新しいスクリプト（`CompanionHost.cs` など）の `.uid` ファイルも作られる。差分が出たらコミットする | `.agent/rules/godot.md` |
| 手動テスト TS-ambience-006 | 昼夜の色・予言者の位置とアニメーション・崩壊とリセットの見た目を確かめ、`docs/test/results/` に記録する。地表・予言者・環境光を `SubViewport` の下に移したので、見た目が変わっていないことも確かめる | [docs/test/ambience.md](test/ambience.md) |
| 崩壊の見た目の確認 | TS-time-control-009 の手順 5 と TS-ambience-006 の手順 5。今の画面の操作では崩壊を起こせないため未実施 | [実施記録](test/results/2026-10-11-feat-m1-screen.md) |
| 手動テスト TS-companion-pairing-009 | スマホで QR コードを読んでつながること、Wi-Fi を切っても自動で戻ること、ログにトークンが出ないこと。初回の起動で Windows ファイアウォールの許可を求められたら「プライベート ネットワーク」だけを許可する | [docs/test/companion-pairing.md](test/companion-pairing.md) |
| 手動テスト TS-frame-stream-011 | 映像が届くこと（昼夜の色が乗り、HUD が映らない）、画面を消すと止まり戻すと再開すること、3 台目は断られること、配信中も PC のフレームレートが落ちないこと | [docs/test/frame-stream.md](test/frame-stream.md) |

## 決めること

| 項目 | 内容 | 期限 |
| :--- | :--- | :--- |
| 予言者の絵の出典 | 生成 AI のツール名と、利用規約で商用利用・ゲームへの組み込みが認められているか。分かったら [credits.md](credits.md) に書く | 配布する前 |
| 英語の文言 | `game/locales/ui.csv` の `en` の列と、`companion/locales/en.json` は仮の訳。確認する | 英語版を出す前 |
| 用語集への追加 | タイル種別・自動進行・世界再創生・環境光・昼夜の周期、ペアリング・ワンタイムトークン・`sessionToken`・画面の配信を [用語集](domain/glossary.md) に足すか | 急がない |
| 崩壊を起こす手段 | 夢を改ざんする画面（コンパニオンの夢のカードの欄）を待つか、その前に PC 側にデバッグ用の改ざんボタンを置くか | 夢の改ざんを作る前 |
| 崩壊しやすさ | 今の記号の組み合わせでは、崩壊するドグマをほとんど作れない。ゲームとして崩壊をどの程度起こせるようにするか | 夢の改ざんを作る前 |
| 画面の配信の仕様書の承認 | [frame-stream.md](domain/frame-stream.md) は `draft` のまま | 次に配信を変える前 |
| ペアリング前の接続の数の上限 | 今は上限が無い（ペアリングを待つのは 60 秒、閉じる合図に応えない接続は 5 秒で切る）。入れるなら値を決める | 配布する前 |
| 書き出した後の `companion/` の置き場所 | 今はエディタから実行したときの、リポジトリ直下の `companion/` を読む（`WorldCore.CompanionDirectory`） | 書き出しの手順を作る前 |
| QR コードに使う LAN のアドレス | PC に LAN のアドレスが複数あるとき（仮想のネットワークアダプターなど）。今は見つかった順で最初のプライベートな IPv4 | 手動テストで困ったら |
| `sessionToken` の扱い | ホストを終了するまで増え続け、取り消せない。同じ `sessionToken` の接続が 2 つあるとき、古いほうを閉じるか | 配布する前 |

## 急がないもの

| 項目 | 内容 |
| :--- | :--- |
| CI の警告 | GitHub Actions の Node 20 の actions と、`ubuntu-latest` の移行に関する警告 |
| 空の背景 | 地表の外の灰色の部分には環境光がかからない（画面の地の色のため）。空の背景を置くか |
| WebSocket のポートの二重管理 | `game/data/companion.json` と `companion/js/protocol.js` の 2 か所にある。ホストからページに渡すなどして 1 か所にするか |
| 毎フレームの `ToList` | `CompanionHost.Process` で毎フレームリストを作っている（`csharp.md` では Phase 3 で直す） |
| HTTP の要求の読み直し | `CompanionHttpServer.ReadRequestHeadAsync` が、読むたびに先頭から文字列にしている |
