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
| 信仰 | `Belief` | 住人が信じている教えと、その強さ。1 人 3 つまで（[信仰としてのドグマ](dogma-belief.md)） | Belief | |
| 信者 | | ある教えを信じている住人 | Believer | |
| 信仰の強さ | `Belief.Strength` | 信仰ごとの強さ（0.0〜1.0）。画面には数字で出さない | Devotion | |
| 忘れられた教え | | 信者が 0 人になって一覧から消えた教え。年表に「忘れられた」と残る | Forgotten teaching | |
| 後継 | | 予言者が死んだとき、信者の中から選ばれて次の予言者になる住人（[予言者の暮らし](prophet-life.md)） | Successor | |
| 神の沈黙 | | 後継の候補がいなくて予言者が絶え、神託を下せない時期（[予言者の暮らし](prophet-life.md)） | Divine silence | |
| 地域の環境 | | 96 の地域それぞれが持つ森林量・水資源・気温・カオス度・調和度。星全体の値は、その平均（[地域の環境](regional-environment.md)） | Regional environment | |
| 土台の地形 | `BaseTerrain` | 地域ごとに 1 つ持つ固定の地形（海・沿岸・平野・森林・山岳）。星を作るときに決まる | Base terrain | |
| 環境の異常 | | 地域の森林量・水資源・気温から、土台の地形の上に塗るタイル（氷河・焦土・砂漠・密林・草地） | Terrain anomaly | |
| ならし | | 毎年、隣どうしの地域の値を少しずつ近づけること（拡散） | Diffusion | |
| 段 | | 地域の格子の横の並び（6 段）。上から北の極・北の中緯度・北の低緯度・南の低緯度・南の中緯度・南の極 | Latitude band | 緯度帯 |
| 大陸 | | 星を作るときに、大陸の種から広げた陸の地域の塊 | Continent | |
| 星の視点・地域の視点・詳細の視点 | | カメラの 3 段の視点。星全体／数地域／1 地域ほどが画面に入る（[カメラの視点](camera-views.md)） | Planet / Region / Detail view | 大陸の視点・中拡大の視点・マクロ・メゾ・ミクロ |
| 気候 | | 地域ごとの風・気温・湿りと雨・水資源・森の動き（[気候と天候](weather.md)） | Climate | |
| 湿り | | 地域の空気が持つ水の量。風下へ運ばれ、雨になる | Humidity | |
| 揺らぎ | | 年ごとの気温・雨の、平年からのずれ。大きいと名前の付いた天候になる | Anomaly | |
| 雨陰 | | 山の風下の、雨が少ない側 | Rain shadow | |
| 川 | | 水資源がいちばん低い隣の地域へ流れる道。タイルでは青い線で描く | River | |
| 砂漠化 | | 雨と森が減り、地域が砂漠のタイルに塗られていくこと | Desertification | |
| プレート・境目 | | 星を作るときに決める地殻の区切りと、その境。山と断層ができやすい | Plate / Plate boundary | |
| 大雨・日照り・寒波・熱波 | | 揺らぎが大きい年に付く、名前の付いた天候 | Heavy rain / Drought / Cold wave / Heat wave | |
| 嵐 | | 暖かい海の地域で生まれ、風下へ進んで大雨を降らせ、森林量を減らす天候 | Storm | |
| 地震 | | プレートの境目で起き、建物を壊す出来事 | Earthquake | |
| 木を切る・水を汲む | | 住人が暮らしと資材のために、地域の森林量・水資源を使うこと（[住人による森と水の利用](resource-use.md)） | Logging / Water drawing | |
| 資材 | | 建物を建てるときに使う森林量 | Material | |
| 人のまとまり | `Pop` | 地域の中で、信仰・職業・身分・文化が同じ人を、人数だけで持つかたまり。子・大人・老人の人数を持つ（[人の数](population.md)） | Pop | |
| 人口 | | 地域・集落・星に住む人の数（まとまりの人数の合計） | Population | |
| 集落 | | 人が住む場所。星の地図では人口で広がる記号、詳細の視点では街並みで見せる（[国と集落](polity.md)） | Settlement | |
| 名のある住人 | `Resident` | 住人（名前を持ち、一人ずつ動く人）を、人のまとまりと区別して呼ぶ言い方。まとまりの人数のうちの何人かとして数える | Named resident | |
| 街並み | | 詳細の視点で集落を家・道・畑・広場で描いた見た目。人口で家が増える（[地域の環境](regional-environment.md)） | Townscape | 街の拡大 |
| 食料の生産・採集・農耕・牧畜・漁 | | 地域が食料を得る手段（[食料の生産](food-production.md)） | Food production / Gathering / Agriculture / Livestock / Fishing | |
| 発明・発明の木・前提 | | 地域が知っている技と、その前提のつながり（[発明の木](invention-tree.md)） | Invention / Invention tree / Prerequisite | |
| 時代の名前 | | 知っている発明から付く名札（石器・青銅器・鉄器・中世） | Era label | |
| 失われた発明 | | 一度知られたが、伝える人がいなくなって失われた発明 | Lost invention | |
| 宗派 | | 同じ教えの一覧を信じる人の集まり。神の宗派・人が唱えた宗派・自然崇拝がある（[宗教](religion.md)） | Sect | |
| 神の宗派 | | プレイヤーの神託から生まれた教えを信じる宗派 | Sect of the god | |
| 人が唱えた宗派 | | 危機の中で人が唱えた教えから生まれた宗派 | Human-founded sect | |
| 自然崇拝 | | 星を作ったときから、住む土地の地形に合う記号を弱く崇める宗派 | Animism | |
| 教祖 | | 人が唱えた宗派を始めた人 | Founder | |
| 分裂 | | 宗派が 2 つに分かれること | Schism | |
| 宗派の長 | | 宗派をまとめる人。教祖や予言者の後を継ぐ | High priest | |
| 祭り | | 地域ごとに行う、宗派の儀礼 | Festival | |
| 聖地 | | 予言者の死んだ地など、巡礼の目的地になる場所（[聖地](holy-site.md)） | Holy site | |
| 巡礼 | | 信者が聖地を訪れること | Pilgrimage | |
| 霊廟・聖堂 | | 聖地に建つ 2 種類の建物 | Mausoleum / Sanctuary | |
| 奇跡 | | 信者の祈りで溜まる奇跡の力を使い、神が信者のいる地域に直接手を下すこと（[奇跡](miracle.md)） | Miracle | 神威の行い |
| 奇跡の力 | | 奇跡の種類ごとに溜まる力。満ちると 1 回使える | Miracle charge | |
| 天候の奇跡・一瞬の奇跡 | | その年の天候の揺らぎを動かす奇跡と、その場で効く奇跡 | Weather miracle / Instant miracle | |
| 恵みの雨・暖かさ・涼しさ・癒し・豊作・雷 | | 最初に入れる 6 種類の奇跡 | Blessed rain / Warmth / Coolness / Healing / Bountiful harvest / Lightning | |
| 戦の奇跡 | | 戦の中で語られる 8 種類の奇跡（神風など） | War miracle | |
| 植生 | | 気温と雨で決まる、地域の草木のまとまり（[自然の種類](natural-kinds.md)） | Biome | |
| 土 | | 地域の土の種類。森の量・水のもち・農耕の向きに効く | Soil order | |
| 鉱床 | | 地域・タイルに眠る鉱物 | Deposit | |
| 食物連鎖 | | 植生・動物・人の、食べる・食べられるつながり | Food chain | |
| 家畜にできる | | 人が飼い慣らせる動物の性質 | Domesticable | |
| 絶滅 | | 種類が星から絶えること | Extinction | |
| 文化 | | 地域の建築・言葉・文字・統治・衣服・料理・神話のまとまり（[文化の種類](culture-kinds.md)） | Culture | |
| 建築の様式 | | 集落の家の作り方と見た目 | Architectural style | |
| 言葉の系図 | | 言葉が分かれてできた親子のつながり | Language tree | |
| 文字 | | 地域が使う書き方 | Script | |
| 統治の形 | | 国の治め方（群れ・部族・王国・神権政治など） | Form of government | |
| 神話の型 | | 地域に生まれる神話の筋の型 | Myth type | |
| 服装・流行 | | 住人の服と、地域で広がって廃れる服の好み（[服装と流行](fashion.md)） | Clothing / Trend | |
| パーツ・色の差し替え | | 服の絵を組み立てる部品と、同じ絵の色を替えること | Outfit part / Palette swap | |
| 歴史の神話化 | `MythChronicle` | 100 年ごとの章に、ローカルの LLM が神話の見出しと本文を添えること（[AI による表現](ai-presentation.md)） | Mythologized history | |
| 知らせ | | 出来事をプレイヤーに伝えること。止める知らせ・手紙・流れの段階がある（[出来事の知らせ](notifications.md)） | Notification | |
| 止める知らせ | | 時間を止めて伝える、いちばん大きな知らせ | Pausing notification | |
| 手紙 | | 溜まって後で読める知らせ | Letter | |
| 流れ | | 画面を流れて消える、小さな知らせ | Feed | |
| 地図の印 | | 出来事の起きた地域に出す印 | Map marker | |
| 歴史の長さ | | 1 つの星の歴史の目安の長さ（およそ 1 万年。[歴史の長さと時間の速さ](time-scale.md)） | History length | |
| 速さの倍率 | | 出来事の多さで、自動進行の速さを変える倍率 | Pace multiplier | |
| 予言者の誤解 | `OracleProphecy`・`OracleText` | 神託でドグマが生まれたとき、予言者が神の意図を誤解して語る文（見出し・意図・教義）。ローカルの LLM が作る表現で、ゲームの計算には関わらない（[AI による表現](ai-presentation.md)） | Prophet's misreading | AI 神託 |
| 年表 | `History`・`HistoryEntry` | 星に起きた主な出来事（ドグマの誕生・住人の精神崩壊・称号を持つ住人の死・星の崩壊・世界再創生）を年ごとに積んだもの。世界再創生をまたいで残る（[星の年表](history.md)） | Chronicle | 歴史ログ |
| つがい | `Resident.PartnerId` | 同じ地域で組んだ 2 人の住人。子供をもうけ、死ぬまで続く | Partner | 夫婦、カップル |
| 子供 | | 大人の年齢（16 歳）より若い住人。タスクを選ばず、つがいにならない | Child | |
| 家名 | `ResidentName.LineagePrefix`・`LineageSuffix` | 住人の名前の 2 つ目の部分 | Lineage | 苗字 |
| ホスト | | シミュレーションと描画を行う PC 側のゲーム | Host | サーバー（画面上） |
| コンパニオン | | ホストに接続するモバイルの神託インターフェース | Companion | リモコン |

対応言語を増やしたら、`docs/nfr.md` の「対応言語」に合わせて列を足す。
