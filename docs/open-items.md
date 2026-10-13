# 未決・残作業の一覧

決まっていないこと、人間の手作業が要ることの一覧。解消したら行を消し、決まった内容は該当する仕様書・ルールに書く。

最終更新: 2026-10-13（M5 の住人・家・つがいと子供・年表・夜空の星を作った後）

## 人間の手作業

| 項目 | 内容 | 関連 |
| :--- | :--- | :--- |
| 手動テスト TS-crowd-010・TS-planet-display-012 | 群衆の動き（散策・水場と緑地に集まる・群れ・一時停止と高速・狂乱・崩壊と世界再創生）と、湖の見た目（縮む・凍る）。2026-10-12 に画面を見て「全体的にこれでよい」とした。手順どおりに確かめたら `docs/test/results/` に記録する | [docs/test/crowd.md](test/crowd.md)・[planet-display](test/planet-display.md) |
| 手動テスト TS-ambience-006 | 昼夜の色・予言者の位置とアニメーション・崩壊とリセットの見た目を確かめ、`docs/test/results/` に記録する。地表・予言者・環境光を `SubViewport` の下に移したので、見た目が変わっていないことも確かめる | [docs/test/ambience.md](test/ambience.md) |
| 崩壊の見た目の確認 | TS-time-control-009 の手順 5 と TS-ambience-006 の手順 5。夢の改ざんで崩壊を起こせるようになったので、確認できる | [実施記録](test/results/2026-10-11-feat-m1-screen.md) |
| 手動テスト TS-strata-008・TS-emergent-progression-010・TS-visual-effects-006・TS-visual-effects-007 | M4 で作った廃墟と建て増し・地下の表示、建物の見た目、焚き火、予言者と建物の陰影と落ち影。手順どおりに確かめたら `docs/test/results/` に記録する | [strata](test/strata.md)・[emergent-progression](test/emergent-progression.md)・[visual-effects](test/visual-effects.md) |
| 手動テスト TS-agent-needs-011 | 住人の絵・家が建つこと・子供と並んで寝るつがい・昼に歩き夜に建物のそばで横になること・クリックの欄（パートナー）・墓と記録・世界再創生で消えること。ヘッドレスで、住人・墓・家・つがい・子供のあるセーブを読み込んで動かし、エラーが無いことだけは確認済み | [agent-needs](test/agent-needs.md) |
| 手動テスト TS-ai-presentation-006 | 実際の Ollama で、神託の吹き出し・年表の見出し・止めたときの定型文。頼みの文は curl で `gemma2:9b` に送り、3 秒ほどで形の合った文が返ることを確かめた（ほかのアプリが別のモデルを使っていると、読み込み直しで 60 秒を超えることがある）。Godot の画面では未確認 | [ai-presentation](test/ai-presentation.md) |
| 手動テスト TS-history-008・TS-visual-effects-008 | 年表の欄の開け閉めと行の並び、夜空の星と空の色。Godot の動画の書き出し（`--write-movie`）で、年表の欄（欄を開いた状態に一時的に書き換えて）と夜の星の 1 コマを見て、表示されることだけは確認済み。クリックの操作と周期を通した見た目は未確認 | [history](test/history.md)・[visual-effects](test/visual-effects.md) |
| 手動テスト TS-camera-views-006 | ホイールのズーム・ドラッグ・HUD が動かないこと・詳細の視点でドットの幅がそろうこと | [docs/test/camera-views.md](test/camera-views.md) |
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
| 英語の文言 | `game/locales/ui.csv` の `en` の列は仮の訳。住人の名前（パーツの組み合わせ 36 通りの `NAME_*`。英語は綴りをつないだだけ）・称号・情報の欄（`EPITHET_*`・`RESIDENT_*`）も仮の訳。`companion/locales/en.json` は 2026-10-12 に確定した訳を当てたが、`TITLE`・`CARDS_*`・`STATUS_SCAN_QR`・`STATUS_RECONNECTING`・`ERROR_SESSION_REPLACED` はまだ仮の訳 | 英語版を出す前 |
| 群衆の仕様書の承認 | [crowd.md](domain/crowd.md) は `draft` のまま。湖を足した [planet-display.md](domain/planet-display.md) も `draft` | 次に群衆・地表を変える前 |
| 推奨スペックの PC | [nfr.md](nfr.md) の性能の目標は「例」のまま。群衆の計測（10,000 人で GPU 版 約 1,000 fps、開発者の PC）を目安に、目標と推奨スペックを決める | 配布する前 |
| 「狂乱」の言葉の重なり | 群衆の `CrowdMode.Frenzy`（聖地へ押し寄せる）と、住人の精神崩壊の `BreakdownKind.Rampage`（建物を壊す）が、どちらも「狂乱」。どちらかの言葉を変えるか | 急がない |
| 住人のログのキー | `residentId`・`region`・`name`・`breakdown`・`partnerId` を `project.md` 7 章のキーの表に足す（ルールファイルは人間が直す） | 次に住人を変える前 |
| AI が仮に置いた規則と値（`TODO(AI_UNSURE)`） | 2026-10-13 に「仮置きして進める」として作ったもの。[星の年表](domain/history.md)の「未決」すべて（残す出来事・上限 200 行・再創生で消さない・欄の置き場所）、称号「街を包む者」の軒数（2）と建てた人の決め方（[agent-needs](domain/agent-needs.md)）、夜空の星の数・大きさ・瞬き・空の地の色（[visual-effects](domain/visual-effects.md)）。予言者の誤解の待つ秒数（60）・吹き出しの秒数（20）・文字数の上限（30・80）（[ai-presentation](domain/ai-presentation.md)）。コードの `TODO(AI_UNSURE)` は `History.cs`・`ResidentSimulator.cs`・`SkyView.cs`・`OracleProphecy.cs` にある。人間が決め直すまで、`project.md` 2.1 によりマージしない扱い | 次に年表・称号・空を変える前 |
| 用語集への追加 | タイル種別・自動進行・世界再創生・環境光・昼夜の周期、ペアリング・ワンタイムトークン・`sessionToken`・画面の配信、神託・依頼（祈り）・困りごとを [用語集](domain/glossary.md) に足すか | 急がない |
| 画面の配信の仕様書の承認 | [frame-stream.md](domain/frame-stream.md) は `draft` のまま | 次に配信を変える前 |
| 夢の改ざん・依頼の仕様書の承認 | [dream-tampering.md](domain/dream-tampering.md)・[prophet-request.md](domain/prophet-request.md) は `draft` のまま（スマホでの操作も足した）。M2 は 2026-10-12 に閉じたので、人間が承認する | 次に夢・依頼を変える前 |
| AI 連携の仕様の未決 | [ai-presentation.md](domain/ai-presentation.md) の「未決」（予言者の誤解は 2026-10-13 に作った。残りは、イベントの ID、周回の終わり、創世図鑑、既定の絵と 32px のタイル、画像生成のライセンス、AI クライアントの置き場所、セーブの形など） | AI 連携（M5 以降の構想）に着手する前 |
| 地層の仕様の未決 | [strata.md](domain/strata.md) の「未決」（建物が崩れるきっかけ、大災害、ミクロ沈下の年数・マクロ沈下の条件、発掘と二次誤解の表、用語など） | 地層の発掘・二次誤解・マクロ沈下に着手する前 |
| カメラの視点の仕様の未決 | [camera-views.md](domain/camera-views.md) の「未決」（輪に見せる大陸の視点、詳細の視点の絵、画面の効果との関係、スマホへの配信など） | 3 段階のカメラの視点に着手する前 |
| 文明の創発的な発展の仕様の未決 | [emergent-progression.md](domain/emergent-progression.md) の「未決」（地域の環境の決め方、技術・文化の伸びの量と倍率、イベントの確率と種類、禁忌の規則、建物の種類と置き場所の残り、変種技術の表と足りない記号、極秘研究の一覧、パラドックスの条件、スマホへのログ、用語など） | 文明の発展の残りに着手する前 |
| 人の欲求と健康の仕様の未決 | [agent-needs.md](domain/agent-needs.md) の「未決」。住人・家・つがいと子供は 2026-10-13 に作った。残りは後に回した仕組み（異端の教祖と人が唱えるドグマ、繁殖・愛着の欲求と好感度・恋愛のもつれ・信心、変異、負傷・疾患、広場・祭壇・食料の建物）、戦争の称号。子供の確率（0.2）と移り住みを止める人数（2）は画面で見て調整する | 後に回した仕組みに着手する前 |
| 地域間の戦争の仕様の未決 | [warfare.md](domain/warfare.md) の「未決」（戦争に入る条件と関係の向きの食い違い、状態遷移表の空き、地域と国、攻め込める先、兵の数、スマホの提案、地域ごとの資源・記号など先に要るもの、値など） | 地域間の戦争（人の欲求と健康の後）に着手する前 |
| 画面の効果の仕様の未決 | [visual-effects.md](domain/visual-effects.md) の「未決」（光の向き・焚き火と星の置き場所・ぼかしやグローの強さ・粒子の切り替えの境目などの値、効果を切る設定の置き場所、法線マップのツールのライセンス、性能） | 画面の効果の残りに着手する前 |

## 急がないもの

| 項目 | 内容 |
| :--- | :--- |
| GPU 版の突き合わせ | CI（headless）では GPU が使えないので、`-- --crowd-check` は手元の PC でしか動かせない。`crowd_step.glsl`・`GpuCrowd.cs`・`Crowd.cs`・`PotentialField.cs`・`FlockGrid.cs` を変えたら手元で実行する（[TS-crowd-009](test/crowd.md)） |
| CI の警告 | GitHub Actions の Node 20 の actions と、`ubuntu-latest` の移行に関する警告 |
| 空の背景 | 2026-10-13 に、地表の外を空として、背景の灰色に環境光の色をかけ、夜に星を出した（[visual-effects](domain/visual-effects.md) の「夜空の星」。色は仮）。空らしい色にするかは、画面で見て決める |
| WebSocket のポートの二重管理 | `game/data/companion.json` と `companion/js/protocol.js` の 2 か所にある。ホストからページに渡すなどして 1 か所にするか |
| 毎フレームの `ToList` | `CompanionHost.Process` で毎フレームリストを作っている（`csharp.md` では Phase 3 で直す） |
| HTTP の要求の読み直し | `CompanionHttpServer.ReadRequestHeadAsync` が、読むたびに先頭から文字列にしている |
