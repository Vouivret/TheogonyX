# テスト仕様書: 神と人の文字（双方向の解読）

- 受け入れ条件: [docs/domain/script-decipher.md](../domain/script-decipher.md)
- 関連ADR: なし

## テスト観点

種別は「自動」「手動」「廃止」のどれか。自動テストの手順はテストコードに書き、ここには書かない。小段 1（字のしくみ。コアだけ）の観点。刻まれた字・画面は、小段 2 以降で足す。

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
