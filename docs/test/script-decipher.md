# テスト仕様書: 神と人の文字（双方向の解読）

- 受け入れ条件: [docs/domain/script-decipher.md](../domain/script-decipher.md)
- 関連ADR: なし

## テスト観点

種別は「自動」「手動」「廃止」のどれか。自動テストの手順はテストコードに書き、ここには書かない。小段 1（字のしくみ）と小段 2（刻まれた字・手帳・セーブ）の観点。コアだけで、画面は小段 3 以降で足す。

| ID | 受け入れ条件 | 観点 | 前提 | 操作 | 期待結果 | 種別 | 実装場所 |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| TS-script-decipher-001 | AC-script-decipher-001 | 同じシードで同じ字、違うシードで違う字 | 受け入れ条件のとおり | 受け入れ条件のとおり | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/ScriptTests.cs` |
| TS-script-decipher-002 | AC-script-decipher-009 | 部品の数・線の数・形が重ならないこと・似た形の組 | 受け入れ条件のとおり | 受け入れ条件のとおり | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/ScriptTests.cs` |
| TS-script-decipher-003 | AC-script-decipher-003 | 星の語順どおりの文が教えになる（語順の両方） | 受け入れ条件のとおり | 受け入れ条件のとおり | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/ScriptTests.cs` |
| TS-script-decipher-004 | AC-script-decipher-004, 013, 014, 015 | 語順が逆の文の読み落とし・方角の語の位置・知らない記号・名詞だけの語 | 受け入れ条件のとおり | 受け入れ条件のとおり | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/ScriptTests.cs` |
| TS-script-decipher-005 | AC-script-decipher-005 | 崩れた後、似た形の組が一方に読まれる | 受け入れ条件のとおり | 受け入れ条件のとおり | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/ScriptTests.cs` |
| TS-script-decipher-006 | AC-script-decipher-010 | 語が無い・5 語・手帳に無い字を拒み、4 語は受け付ける | 受け入れ条件のとおり | 受け入れ条件のとおり | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/ScriptTests.cs` |
| TS-script-decipher-007 | AC-script-decipher-002, 011 | 祈りの文の並べ方 | 受け入れ条件のとおり | 受け入れ条件のとおり | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/ScriptTests.cs` |
| TS-script-decipher-008 | AC-script-decipher-012 | 字を語の字から戻し、合わない字を拒む | 受け入れ条件のとおり | 受け入れ条件のとおり | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/ScriptTests.cs` |
| TS-script-decipher-009 | AC-script-decipher-006 | 地形の目印が、名詞 1 語の文で年 0 に刻まれる | 受け入れ条件のとおり | 受け入れ条件のとおり | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/ScriptStateTests.cs` |
| TS-script-decipher-010 | AC-script-decipher-007, 016 | 工房・家・祠の札と、祠の札の刻み直し・消え方 | 受け入れ条件のとおり | 受け入れ条件のとおり | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/ScriptStateTests.cs` |
| TS-script-decipher-011 | AC-script-decipher-017 | 禁忌の札の立つ・消える境（3 割） | 受け入れ条件のとおり | 受け入れ条件のとおり | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/ScriptStateTests.cs` |
| TS-script-decipher-012 | AC-script-decipher-008, 018 | 崩れは一度だけで、古い字は古い形のまま | 受け入れ条件のとおり | 受け入れ条件のとおり | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/ScriptStateTests.cs` |
| TS-script-decipher-013 | AC-script-decipher-019 | 手帳に語の字が載り、書き込みは一覧の意味だけ | 受け入れ条件のとおり | 受け入れ条件のとおり | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/ScriptStateTests.cs` |
| TS-script-decipher-014 | AC-script-decipher-020 | 字・刻まれた文・手帳のセーブ（版 18）と、壊れた手帳の拒否 | 受け入れ条件のとおり | 受け入れ条件のとおり | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/ScriptStateTests.cs` |
| TS-script-decipher-015 | （画面。仕様書の 2.1「見え方」） | 刻まれた字の印・拓本の欄・祈りの字の並び | Godot 4.7.2 で動かす | 下の手順 | 下の手順のとおり | 手動 | — |
| TS-script-decipher-016 | AC-script-decipher-021 | 字の神託: 読めた組から教えと行き先・謎の語だけでも神威を使う・手帳に無い字と神威不足を拒む | 受け入れ条件のとおり | 受け入れ条件のとおり | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/ScriptSpeakTests.cs` |
| TS-script-decipher-018 | AC-script-decipher-022 | 碑は 1 地域に新しい 3 つまで、左端の列の 1〜3 行目 | 受け入れ条件のとおり | 受け入れ条件のとおり | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/SteleTests.cs` |
| TS-script-decipher-019 | AC-script-decipher-023 | 神託を下すと予言者の地域に碑が刻まれる | 受け入れ条件のとおり | 受け入れ条件のとおり | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/SteleTests.cs` |
| TS-script-decipher-020 | AC-script-decipher-024 | 崩れた年の読み直しで、似た形の組を含む崩れる前の碑だけが別の教えになる | 受け入れ条件のとおり | 受け入れ条件のとおり | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/SteleTests.cs` |
| TS-script-decipher-021 | AC-script-decipher-025 | 読み違いを信じる住人の数と決まり方・年表の行・セーブ | 受け入れ条件のとおり | 受け入れ条件のとおり | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/SteleTests.cs` |
| TS-script-decipher-022 | AC-script-decipher-026 | 書きぶりは星と時代で決まり、揺れは場所と年で決まって上限を越えない | 受け入れ条件のとおり | 受け入れ条件のとおり | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/ScriptStyleTests.cs` |
| TS-script-decipher-017 | （画面。仕様書の「書く」） | 手帳の書き込みと、文を書いて語りかける欄 | Godot 4.7.2 で動かす | 下の手順 | 下の手順のとおり | 手動 | — |

## 手動テストの手順

### TS-script-decipher-015
1. メインシーンを実行し、湖の岸・焚き火・地表の左右に、刻まれた字の小さな印が出ていることを確かめる。
2. 100 年跳躍を何回か押し、工房・祠・家（地域ごとに 1 軒）の上に札の印が増えることを確かめる。
3. 印を押すと、左上に拓本の欄が開き、文が大きく出て、「地形の目印　0 年に刻まれた」のような説明と、「手帳に新しく N 個の字を写した」が出ることを確かめる。工房・祠・禁忌の札は 2 語の文に見える。同じ印をもう一度押すと「もう手帳にある」になる。
4. 依頼が出ると、依頼の吹き出しの下に祈りの文（人の字）が並ぶことを確かめる。依頼が済むと消える。
5. 文を書いて語りかけると、予言者のいる地域の左端の列に碑の印が増えることを確かめる。押すと拓本の欄に「神託の碑　〇〇年に刻まれた」と「人の読み：…」が出る。拓本の欄は吹き出しより手前に出て、重なっても読める。
6. 300 年を越えると、Godot の出力に `event=script_decayed` が出て、年表に「人の字が崩れた」が残ることを確かめる。崩れた後に刻まれた札の字は線が減り、前からある札の字はそのままであることを確かめる。

2026-10-17 に、`--script` で動かす確かめで、印が出ること・拓本の欄が開くこと・祈りの字が並ぶこと・300 年で崩れることを確かめた（画面のスクショ。手で押す操作は未確認）。

### TS-script-decipher-017
1. 拓本をいくつか取った後、「手帳」を押す。右の欄に、写した字が並び、それぞれに「？」の選ぶ欄があることを確かめる。意味を選ぶと、文を書く欄の字の下の書き込みも変わる。
2. 下の帯が「文を書く」ボタンになっていることを確かめる（文の入力欄は出ない）。
3. 「文を書く」を押し、手帳の字を押すと「語る文」の後ろに足され、文の字を押すとその字が消えることを確かめる。4 字まで足せる。
4. 「語りかける」で神託が下り、吹き出しに掟の 1 行と「人の読み：〇〇・〇〇／謎の語」が出ることを確かめる。語順を逆にすると「〇〇（名だけ。崇めると読んだ）」と「読み落とした語：1 個」が出る。神威が 1 減る。

2026-10-17 に、`--script` で動かす確かめで、手帳の欄・字を組む欄・語りかけて「人の読み：読めない字」が出ることを確かめた（手で押す操作は未確認）。
