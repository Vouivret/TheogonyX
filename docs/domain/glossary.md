# 用語集

コード・仕様書・会話・画面で使う用語をここに揃える。クラス名や変数名は「コード上の名前」に、画面の表記は各言語の列に合わせる。コード上の名前が空欄の用語は、まだ決まっていない。

| 用語 | コード上の名前 | 意味 | 英語表記 | 使わない言い方 |
| :--- | :--- | :--- | :--- | :--- |
| 神 | — | プレイヤー。夢の改ざんなどで間接的に世界へ介入する | God | ユーザー |
| 予言者 | | 夢を見て、そこからドグマを形成する人間 | Prophet | 預言者 |
| 夢 | | （廃止。2026-10-14）予言者が見ていた、記号の並び。神託の作り方を手札のルーンの組に変えたので無くなった（[神託の作り方](oracle-pair.md)） | Dream | |
| 夢ノード | | （廃止。2026-10-14）夢の 1 コマ。夢と一緒に廃止した | Dream node | |
| 記号 | `Symbol` | 夢を構成する意味の単位（例: 木、血、雨） | Symbol | シンボル（画面上） |
| 感情の極性 | | （廃止。2026-10-14）記号が呼び起こす感情。ドグマの種別は[五行](oracle-wuxing.md)の関係で決めるようにしたので消した | Emotional polarity | |
| 概念カード（概念ルーン） | | プレイヤーが手札に持つカード。2 枚を選んで神託にする。1 つ以上の記号を持つ（[神託の作り方](oracle-pair.md)、[神託のデッキ](oracle-deck.md)） | Concept card (rune) | |
| 手札・山札・捨札 | `OracleDeck` | 概念カードを回す神託のデッキの 3 つの束。夢の改ざんで選べるのは手札のカードだけ。神託を下すと使ったカードが捨札へ移り、山札から 1 枚が手札に加わる（[神託のデッキ](oracle-deck.md)） | Hand / Draw pile / Discard pile | |
| 改ざん | | （廃止。2026-10-14）夢ノードを概念カードとスワップすること。夢と一緒に廃止した | Tampering | |
| 問解 | | 人間からの神への問い合わせ。記号カードで返答する。構想（[ロードマップ](../roadmap.md)の M4）で、今は予言者の依頼（祈り）が担う | Inquiry | |
| ドグマ | `Dogma` | 夢から抽出された因果に基づく教義 | Dogma | 教え、ルール |
| ドグマの種別 | `DogmaType` | 禁忌（`Taboo`）・崇拝（`Worship`）・破壊（`Destruction`） | Dogma type | |
| 時代跳躍 | `SimulateTimeSkip` | 50〜300年を一度に進めること | Time skip | |
| 世界の状態 | `WorldState` | 年・森林量・水資源・気温・カオス度など星の状態 | World state | |
| カオス度 | `ChaosLevel` | 星の混迷・破滅の度合い | Chaos level | |
| 調和度 | `HarmonyLevel` | 星の秩序・穏やかさの度合い | Harmony level | |
| 衝撃度 | `Impact` | 夢ノードのペアが予言者に与える印象の強さ。ドグマにするペアの選択と強度に使う | Impact | 感情強度 |
| 地層 | | 滅びた文明が遺跡として蓄積したもの | Strata | |
| 水場 | `TileType.Water` | 地表の湖のタイル。水資源が多いほど広がり、氷河・焦土の列では凍る・蒸発する（[星の表示](planet-display.md)） | Water | 水タイル、湖（タイル種別として） |
| 群衆 | `Crowd` | 地表を動き回る大勢の人間（NPC）。見た目だけで、シミュレーションの結果には影響しない（[群衆](crowd.md)） | Crowd | モブ、NPC（画面上） |
| 散策 | `CrowdMode.Wander` | 群衆の動きの状態の 1 つ。ポテンシャルフィールドの力を受けながら地表を歩く | Wander | 徘徊 |
| 狂乱 | `CrowdMode.Frenzy` | 群衆の動きの状態の 1 つ。カオス度が閾値以上のとき、聖地へ押し寄せる | Frenzy | 暴走、パニック |
| 聖地 | | 狂乱した群衆が押し寄せる場所。予言者の足元 | Holy site | 神殿 |
| ポテンシャルフィールド | `PotentialField` | 地表のタイル種別から作る、人を引き寄せる力・遠ざける力の場 | Potential field | ベクトル場、力場 |
| 住人 | `Resident` | 地域に属し、欲求と健康を持つ少数の人。群衆とは別で、死と精神崩壊でシミュレーションの結果を変える（[人の欲求と健康](agent-needs.md)） | Resident | 村人、NPC（画面上） |
| 欲求 | `Hunger`・`Shelter`・`Social`・`Esteem`・`Actualization` | 住人の空腹・住居・交流・承認・自己実現。0.0（満たされている）〜 1.0（切実） | Need | |
| SAN 値 | `Sanity` | 住人の精神の健康（0〜100）。0 になると精神崩壊する | Sanity | 正気度 |
| 精神崩壊 | `BreakdownKind` | SAN 値が 0 になった住人の状態。狂乱（`Rampage`）か自閉・絶望（`Catatonia`）で、3 年続く | Breakdown | 発狂 |
| 称号 | `ResidentEpithet` | 住人が行いで得る呼び名（例: 血を恐れぬ者）。1 人 1 つで、最初に得たものを残す | Epithet | 二つ名 |
| 家 | `BuildingType.HouseHut`・`HouseTimber`・`HouseBrick` | 屋根を探す住人が建てる建物。1 軒に 4 人が住める。種類は建った年の地域の技術の段階で決まる（わらぶき小屋・木組みの家・れんがの家） | House | 住居、民家 |
| 神の声 | `SPEAK`（通信） | プレイヤーが自由な文で書いて予言者に語りかける神託（[神託の作り方](oracle-pair.md)の「自由文の神託」） | Divine voice | |
| 予言者の語彙 | `OracleDeck` | 予言者が知っている言葉。手札のルーンの記号の名詞と、すべての動詞。画面には見せない（[神託の作り方](oracle-pair.md)） | Prophet vocabulary | |
| 語義のずれ表 | `DialectTable` | 星のシードと器の段から作る「神の言葉 → 予言者に聞こえる意味」の表（[神託の作り方](oracle-pair.md)） | Dialect table | |
| 方言帳 | `DialectBook` | 神託と祈りで分かった聞き違えと知っている言葉を、自動で書き留める欄（[神託の作り方](oracle-pair.md)） | Dialect book | |
| 謎の言葉 | `MysteryWords` | 神の文のうち、どの記号にも当たらない言葉。教えにならず、経典にだけ残る（[神託の作り方](oracle-pair.md)） | Mystery word | |
| 掟の 1 行 | `ORACLE_LINE_*` | 予言者が告げる吹き出しの頭に出す、記号と種別を決まった形で書いた 1 行（例:「〈木〉を崇めよ」）（[神託の作り方](oracle-pair.md)） | Law line | |
| 予言者の誤解 | `OracleProphecy`・`OracleText` | 神託でドグマが生まれたとき、予言者が神の意図を誤解して語る文（見出し・意図・教義）。ローカルの LLM が作る表現で、ゲームの計算には関わらない（[AI による表現](ai-presentation.md)） | Prophet's misreading | AI 神託 |
| 年表 | `History`・`HistoryEntry` | 星に起きた主な出来事（ドグマの誕生・住人の精神崩壊・名のある住人の死・星の崩壊・世界再創生）を年ごとに積んだもの。世界再創生をまたいで残る（[星の年表](history.md)） | Chronicle | 歴史ログ |
| つがい | `Resident.PartnerId` | 同じ地域で組んだ 2 人の住人。子供をもうけ、死ぬまで続く | Partner | 夫婦、カップル |
| 子供 | | 大人の年齢（16 歳）より若い住人。タスクを選ばず、つがいにならない | Child | |
| 家名 | `ResidentName.LineagePrefix`・`LineageSuffix` | 住人の名前の 2 つ目の部分 | Lineage | 苗字 |
| ホスト | | シミュレーションと描画を行う PC 側のゲーム | Host | サーバー（画面上） |
| コンパニオン | | ホストに接続するモバイルの神託インターフェース | Companion | リモコン |

対応言語を増やしたら、`docs/nfr.md` の「対応言語」に合わせて列を足す。
