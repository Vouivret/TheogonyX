# テスト仕様書: 神と人の文字（双方向の解読）

- 受け入れ条件: [docs/domain/script-decipher.md](../domain/script-decipher.md)
- 関連ADR: なし

## テスト観点

種別は「自動」「手動」「廃止」のどれか。自動テストの手順はテストコードに書き、ここには書かない。小段 1（字のしくみ）と小段 2（刻まれた字・手帳・セーブ）の観点。コアだけで、画面は小段 3 以降で足す。

| ID | 受け入れ条件 | 観点 | 前提 | 操作 | 期待結果 | 種別 | 実装場所 |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| TS-script-decipher-001 | AC-script-decipher-001 | 同じシードで同じ字、違うシードで違う字 | 受け入れ条件のとおり | 受け入れ条件のとおり | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/ScriptTests.cs` |
| TS-script-decipher-002 | AC-script-decipher-009 | 部品の数・線の数・形が重ならないこと・似た形の組 | 受け入れ条件のとおり | 受け入れ条件のとおり | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/ScriptTests.cs` |
| TS-script-decipher-003 | AC-script-decipher-003 | 決まりどおりの字が教えになる | 受け入れ条件のとおり | 受け入れ条件のとおり | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/ScriptTests.cs` |
| TS-script-decipher-004 | AC-script-decipher-004, 013, 014, 015 | 並びの入れ替わり・知らない記号・意味だけの字・方角 | 受け入れ条件のとおり | 受け入れ条件のとおり | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/ScriptTests.cs` |
| TS-script-decipher-005 | AC-script-decipher-005 | 崩れた後、似た形の組が一方に読まれる | 受け入れ条件のとおり | 受け入れ条件のとおり | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/ScriptTests.cs` |
| TS-script-decipher-006 | AC-script-decipher-010 | 字が無い・多すぎる・手帳に無い部品を拒む | 受け入れ条件のとおり | 受け入れ条件のとおり | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/ScriptTests.cs` |
| TS-script-decipher-007 | AC-script-decipher-002, 011 | 祈りの字の組み方 | 受け入れ条件のとおり | 受け入れ条件のとおり | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/ScriptTests.cs` |
| TS-script-decipher-008 | AC-script-decipher-012 | 字を部品から戻し、合わない字を拒む | 受け入れ条件のとおり | 受け入れ条件のとおり | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/ScriptTests.cs` |
| TS-script-decipher-009 | AC-script-decipher-006 | 地形の目印が、意味の部品だけの字で年 0 に刻まれる | 受け入れ条件のとおり | 受け入れ条件のとおり | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/ScriptStateTests.cs` |
| TS-script-decipher-010 | AC-script-decipher-007, 016 | 工房・家・祠の札と、祠の札の刻み直し・消え方 | 受け入れ条件のとおり | 受け入れ条件のとおり | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/ScriptStateTests.cs` |
| TS-script-decipher-011 | AC-script-decipher-017 | 禁忌の札の立つ・消える境（3 割） | 受け入れ条件のとおり | 受け入れ条件のとおり | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/ScriptStateTests.cs` |
| TS-script-decipher-012 | AC-script-decipher-008, 018 | 崩れは一度だけで、古い字は古い形のまま | 受け入れ条件のとおり | 受け入れ条件のとおり | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/ScriptStateTests.cs` |
| TS-script-decipher-013 | AC-script-decipher-019 | 手帳に部品が載り、書き込みは一覧の意味だけ | 受け入れ条件のとおり | 受け入れ条件のとおり | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/ScriptStateTests.cs` |
| TS-script-decipher-014 | AC-script-decipher-020 | 字・刻まれた字・手帳のセーブ（版 17）と、壊れた手帳の拒否 | 受け入れ条件のとおり | 受け入れ条件のとおり | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/ScriptStateTests.cs` |
| TS-script-decipher-015 | （画面。仕様書の 2.1「見え方」） | 刻まれた字の印・拓本の欄・祈りの字の並び | Godot 4.7.2 で動かす | 下の手順 | 下の手順のとおり | 手動 | — |

## 手動テストの手順

### TS-script-decipher-015
1. メインシーンを実行し、湖の岸・焚き火・地表の左右に、刻まれた字の小さな印が出ていることを確かめる。
2. 100 年跳躍を何回か押し、工房・祠・家（地域ごとに 1 軒）の上に札の印が増えることを確かめる。
3. 印を押すと、左上に拓本の欄が開き、字が大きく出て、「地形の目印　0 年に刻まれた」のような説明と、「手帳に新しく N 個の部品を写した」が出ることを確かめる。同じ印をもう一度押すと「もう手帳にある」になる。
4. 依頼が出ると、依頼の吹き出しの下に祈りの字（人の字）が並ぶことを確かめる。依頼が済むと消える。
5. 300 年を越えると、Godot の出力に `event=script_decayed` が出て、年表に「人の字が崩れた」が残ることを確かめる。崩れた後に刻まれた札の字は線が減り、前からある札の字はそのままであることを確かめる。

2026-10-17 に、`--script` で動かす確かめで、印が出ること・拓本の欄が開くこと・祈りの字が並ぶこと・300 年で崩れることを確かめた（画面のスクショ。手で押す操作は未確認）。
