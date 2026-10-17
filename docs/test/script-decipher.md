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
