# テスト仕様書: 人の欲求と健康（住人）

- 受け入れ条件: [docs/domain/agent-needs.md](../domain/agent-needs.md)
- 関連ADR: なし

## テスト観点

種別は「自動」「手動」「廃止」のどれか。自動テストの手順はテストコードに書き、ここには書かない。今は M5 で作る最初の範囲（移り住む・欲求・タスク・体力・SAN 値・精神崩壊・死・名前と称号・画面）だけ。

| ID | 受け入れ条件 | 観点 | 前提 | 操作 | 期待結果 | 種別 | 実装場所 |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| TS-agent-needs-001 | AC-agent-needs-001, 002 | 移り住む人数・上限・ID の振り方・初期値 | 受け入れ条件のとおり | 1 年ずつ進める | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/ResidentsTests.cs` |
| TS-agent-needs-002 | AC-agent-needs-003, 004 | 空腹（飢饉の閾値 30 の境目・自閉）と住居（建物の数 × 4 の境目） | 受け入れ条件のとおり | 1 年進める | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/ResidentsTests.cs` |
| TS-agent-needs-003 | AC-agent-needs-005, 006, 007 | タスクの選び方（L1・L2 の優先、閾値、同じ値のとき、祠・工房が要ること、何もしない）と欲求の減り | 受け入れ条件のとおり | 1 年ずつ進める | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/ResidentsTests.cs` |
| TS-agent-needs-004 | AC-agent-needs-008, 009 | 体力の増減・老衰の上限・老衰での死 | 受け入れ条件のとおり | 1 年ずつ進める | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/ResidentsTests.cs` |
| TS-agent-needs-005 | AC-agent-needs-010 | SAN 値の増減（カオス度・死・戻る量・上限） | 受け入れ条件のとおり | 1 年進める | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/ResidentsTests.cs` |
| TS-agent-needs-006 | AC-agent-needs-011, 012, 013, 014 | 精神崩壊の決定性、狂乱の行動（カオス度・廃墟・建物の無い地域）、崩壊の長さと戻り方、崩壊中の死 | 受け入れ条件のとおり | 1 年ずつ進める | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/ResidentsTests.cs` |
| TS-agent-needs-007 | AC-agent-needs-015 | 精神の状態遷移表の可と不可（9 通り） | 受け入れ条件のとおり | 遷移する | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/ResidentsTests.cs` |
| TS-agent-needs-008 | AC-agent-needs-016 | 墓の残る年数と、死による SAN 値の減り | 受け入れ条件のとおり | 1 年ずつ進める | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/ResidentsTests.cs` |
| TS-agent-needs-009 | AC-agent-needs-017, 018 | 名前の決定性とパーツの範囲、証人・長老・血を恐れぬ者の称号と、最初の称号を残すこと | 受け入れ条件のとおり | 名前を作る、1 年進める | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/ResidentsTests.cs` |
| TS-agent-needs-010 | AC-agent-needs-019, 020 | 世界再創生をはさんだ決定性、崩壊した年に計算しないこと、世界再創生で消えて ID が続くこと | 受け入れ条件のとおり | 2 回計算する、世界再創生する | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/ResidentsTests.cs` |
| TS-agent-needs-011 | AC-agent-needs-021 | 住人の見た目・1 日の暮らし・情報の欄・墓と記録 | `game/` を Godot 4.7.2 で開く | 下の手順 | 下の手順のとおり | 手動 | — |

## 手動テストの手順

### TS-agent-needs-011
1. Godot 4.7.2（.NET 版）で `game/project.godot` を開き、F5 でメインシーンを実行する。
2. 自動進行を始め、数年のうちに各地域に住人（群衆の点より大きい人形）が現れることを確かめる。
3. 昼は住人が自分の地域の中を歩き、夜は地域の建物のそば（建物の無い地域ではその場）で横になることを確かめる。地域の外に出ないことも見る。
4. 住人をクリックし、名前（名・家名 の 称号）・欲求・体力・SAN 値の欄が出ることを確かめる。
5. 夢の改ざんで飢饉（森林量か水資源 30 以下）を起こし、100年跳躍を何度か押して住人が死ぬまで進め、死んだ住人の地域に墓が出ること、出力パネルの記録に名前が出ること、10 年後に墓が消えることを確かめる。
6. 世界再創生を押し、住人と墓が消えることを確かめる。
7. 出力パネルにエラー・警告が無いことを確かめる。

## 対象外

- つがいと世代交代・異端の教祖・変異・負傷と疾患・歴史の記録: 仕様書で後に回した。
- セーブとロード: [save-data](save-data.md) のテスト仕様書に、住人を作るときに足す。
