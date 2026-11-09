# 古い神託と方言の仕組みを消す（字の神託の段 6）

## 目的
MVP の範囲を決め直した（[ロードマップ](../docs/roadmap.md)の「MVP の範囲を決め直す」）。字の神託だけを残し、自由文の神託と方言の仕組みを消して、コードと画面を軽くする。画面の割り付けを作り直す前に済ませる。

## スコープ
- 消す: 自由文の神託（入力欄・言葉を選ぶ欄）、LLM の聞き取り（`OracleListening`）と語り（`OracleNarration`）と祈りの文（`PrayerWriter`）、聞き違え（`OracleHearing`）、方言の状態（`Dialect*`・ずれ表・器・方言帳と `DialectBookView`）、神託の記録（`OracleRecord`）、`OracleCommandQueue`・`SpeakOutcome`、スマホからの `SPEAK`・`PRONOUNCE`、`rules.json` の `dialect`・`dialectVessel`。
- 残す: 字の神託、祈りの字、LLM の歴史の神話化（`MythView`・`OllamaClient`・`OracleAiRules`・`AiText`）、五行・デッキの設定の残り（MVP の外。触らない）。
- 範囲の外: 画面の割り付けの作り直し、スマホの画面の作り直し。

## 決定事項
| 決定 | 理由 | ADR化 |
| :--- | :--- | :--- |
| 日々の祈りの年と祈りの数の決まりを、方言から字の状態（`ScriptState.NextPrayerYear`、`ScriptRules` の `prayerMinWords`・`prayerMaxWords`・`dailyPrayerYears`）へ移す。祈りは名詞の ID ではなく記号の ID を持つ | 字の神託で祈りを出すのに要る。記号の選び方は今と同じなので、同じシードなら同じ記号の祈りになる | 不要 |
| 神託の記録（`OracleRecord`）は移さずに消す | 読むのはスマホの `told` と LLM の語りだけで、どちらも消す。下した神託は年表の「教えが生まれた」と碑に残る | 不要 |
| 文の吹き出しの祈り（定型の文・LLM の文）を消し、祈りは字だけにする | 仕様書（script-decipher.md の 2）で、祈りは字の列で届き吹き出しの文の代わりになると決めている。文で出すと答えを明かす | 不要 |
| セーブを版 23 にし、版 22 以前は変換せずに別の名前で残して新しい星で始める | 今までの決まり（save-data.md）に合わせる | 不要 |
| `ORACLE_STATE` を版 4 にし、`listening`・`nouns`・`verbs`・`told`・`book` を外す。`mode` は `SCRIPT` だけ | スマホは MVP の外。壊さず最小限で直す | 不要 |

## 処理の流れ
1. 時間が進んだ後（`ProphetDreams.OnTimeAdvanced`）: 字の状態があれば、次に祈る年が無ければ「今の年 + 日々の祈りの間隔」にする。新しい依頼が出たか、依頼が無く次に祈る年に届いたら、祈りを作り、次に祈る年を進める。
2. 祈りを作る: 手札の記号から、汚染の記号を 1 つ、まだ祈りに入れていない記号、残りを乱数（シード・年・依頼か）で選ぶ（今と同じ並び）。記号の ID の列を返す。手札が空なら祈らない。
3. 字の神託を下す（`SpeakScript`）: 人が文を読み、教えに危機の読みを付け、教えを加える・ルーンを使う・行き先・神威・歪み・依頼・自動進行の再開（今の `Speak` の中身から、方言帳と記録を除いたもの）。碑を刻む。
4. セーブ: `script.nextPrayerYear` を書く。`dialect` は書かない。版 22 以前は `unsupported_save_version`（別の名前で残す）。
5. スマホ: `SPEAK`・`PRONOUNCE` は知らない種類として捨てる（`ERROR` は返さない。今の知らない種類の扱い）。

## テスト観点
- 祈り: 依頼の祈りと日々の祈りが記号の列で出る・同じシードで同じ記号・次に祈る年がセーブから戻る。
- 字の神託: 今のテスト（ScriptSpeakTests・SteleTests・OracleReadingTests）が方言なしで通る。
- セーブ: 版 23 の読み戻し・版 22 を拒む。
- 通信: `ORACLE_STATE` 版 4 の形・`SPEAK`・`PRONOUNCE` を受け付けない。
- 消すテスト（人間が 2026-11-09 に承認）: OracleDialectTests・OracleListeningTests・OracleSpeakTests・OracleCommandQueueTests と、方言を確かめる部分。

## 未決事項
- なし
