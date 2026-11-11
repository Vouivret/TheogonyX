# テスト仕様書: 地域の環境（星の格子と地域ごとの値）

- 受け入れ条件: [docs/domain/regional-environment.md](../domain/regional-environment.md)
- 関連ADR: なし
- plan: [plan/feat-planet-grid-1a.md](../../plan/feat-planet-grid-1a.md)（段 1a。コアだけで、今のゲームにはまだつながない）

## テスト観点

種別は「自動」「手動」「廃止」のどれか。自動テストの手順はテストコードに書き、ここには書かない。値の例は plan の「仮の値」から写す（`game/data/rules.json` の本番の値には依存させない）。

| ID | 受け入れ条件 | 観点 | 前提 | 操作 | 期待結果 | 種別 | 実装場所 |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| TS-regional-environment-001 | AC-regional-environment-010 | 96 地域。横は輪、縦はつながらない。角の隣の数。距離は横（輪）と縦の歩数の大きいほう | 16 列 × 6 段 | 隣と距離を求める | 列 15 の隣に列 0、段 0 と段 5 は隣でない、北の極の端の隣は 5、距離の例（2・3・8・0） | 自動 | `tests/TheogonyX.Core.Tests/PlanetTests.cs` |
| TS-regional-environment-002 | AC-regional-environment-006 | 同じシードなら同じ星（地形・プレート・標高・予言者の地域）、違うシードなら違う星 | 定義とシード | 星を 2 回作る | 同じシードで一致し、違うシードで標高が違う | 自動 | `tests/TheogonyX.Core.Tests/PlanetTests.cs` |
| TS-regional-environment-003 | AC-regional-environment-007 | 海の地域は 3〜4 割、予言者の最初の地域は陸、沿岸は海の隣のある陸だけ | シード 1〜30 | 星を作る | 海の地域が 29〜38、予言者の地域は海でない、沿岸 ⇔ 海の隣のある陸 | 自動 | `tests/TheogonyX.Core.Tests/PlanetTests.cs` |
| TS-regional-environment-004 | — | プレートの境目は隣にほかのプレートがある地域で、境目のほうが山岳の割合が高い | シード 1〜40 | 星を作る | 境目の判定が一致し、境目の山岳の割合 > 境目でない所 | 自動 | `tests/TheogonyX.Core.Tests/PlanetTests.cs` |
| TS-regional-environment-005 | — | 島なしの定義では、海の地域はすべて海のタイル、陸の地域はすべて陸のタイル、沿岸は両方。ホットスポットの島は小さく、海の地域の数は変わらない | 島の数を 0 にした定義、ホットスポットだけの定義 | 星を作る | 受け入れ条件のとおり（島のタイルは島の数 × 最大の大きさ以下） | 自動 | `tests/TheogonyX.Core.Tests/PlanetTests.cs` |
| TS-regional-environment-006 | — | タイルの標高は、陸が 1 m 以上、海が −1 m 以下（なめらかにした後も）。最初の値は段の気温の補正と土台の補正で決まる | 定義の補正（段 −30・−7・+10・+10・−7・−30、山岳 −10、森林の森林量 +20、海の森林量 −50） | 星を作る | 補正どおりの値 | 自動 | `tests/TheogonyX.Core.Tests/PlanetTests.cs` |
| TS-regional-environment-007 | AC-regional-environment-001, 002 | 96 地域の平均。ドグマが無く値がそろった星は 1 年で変わらない | 水資源が半分の地域で 0、残りで 100／全地域が同じ値 | 平均を見る／1 年進める | 50／変わらず年だけ 1 進む | 自動 | `tests/TheogonyX.Core.Tests/PlanetTests.cs` |
| TS-regional-environment-008 | AC-regional-environment-003 | ドグマは予言者の地域がいちばん効き、距離 8 以上には効かず、平均は効果どおり | 全地域の森林量 50、ならす速さ 0、森林量 +0.5 / 年のドグマ 1 つ | 1 年進める | 予言者の地域が最大の増え、距離 8 以上は 0、平均の増えは 0.5 | 自動 | `tests/TheogonyX.Core.Tests/PlanetTests.cs` |
| TS-regional-environment-010 | AC-regional-environment-011 | 海は水、極の陸は氷河、陸は水にならない | 仮の値の星（シード 1）、今の異常の条件 | タイルの種別を決める | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/PlanetCivilizationTests.cs` |
| TS-regional-environment-011 | AC-regional-environment-012 | 建物は陸の建てられる 2 × 2 の枠だけ。枠は地域の中に収まる | 同上。120 年進める | 建物を見る | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/PlanetCivilizationTests.cs` |
| TS-regional-environment-012 | AC-regional-environment-008 | 海だけの地域は技術が 0 のまま、建物が建たない（住人の側は段 1b-2b） | 海の地域がある星。120 年進める | 地域の値と建物を見る | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/PlanetCivilizationTests.cs` |
| TS-regional-environment-013 | AC-regional-environment-013 | ドグマの効きが格子の距離で弱まる（隣・輪・斜め・ゼロの距離・縦は輪でない） | ゼロの距離 4 | 係数を求める | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/PlanetCivilizationTests.cs` |
| TS-regional-environment-014 | AC-regional-environment-014 | 同じ入力なら同じ建物・地域の値 | シード 3 の星を 2 つ。150 年 | 比べる | 一致する | 自動 | `tests/TheogonyX.Core.Tests/PlanetCivilizationTests.cs` |
| TS-regional-environment-015 | AC-regional-environment-015 | 星全体の値として見ると平均・年・崩壊 | 年 42・崩壊 | 値を見る | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/PlanetCivilizationTests.cs` |
| TS-regional-environment-016 | AC-regional-environment-016 | 星全体の値が平均で、年が進み、文明が 96 地域 | 仮の値の星（シード 1） | 100 年跳躍 | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/PlanetTimeControlTests.cs` |
| TS-regional-environment-017 | AC-regional-environment-017 | 住人は最初の地域から始まり、隣へ広がり、海に住まない | 同上 | 1 年、続けて 400 年 | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/PlanetTimeControlTests.cs` |
| TS-regional-environment-018 | AC-regional-environment-018 | 建物は建てられる陸のタイルの枠だけ | 同上。400 年 | 建物を見る | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/PlanetTimeControlTests.cs` |
| TS-regional-environment-019 | AC-regional-environment-019 | カオス度の加算が全地域に足され、決定的 | シード 2 の星を 2 つ | カオス度 20 を足して 1 年 | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/PlanetTimeControlTests.cs` |
| TS-regional-environment-020 | AC-regional-environment-020 | 世界再創生でも同じ星で、年と住人は初期 | シード 3。100 年後 | 世界再創生 | 受け入れ条件のとおり | 自動 | `tests/TheogonyX.Core.Tests/PlanetTimeControlTests.cs` |
| TS-regional-environment-009 | — | 平均が閾値を越えたら崩壊する。1 地域だけが越えても崩壊しない。崩壊した星は変わらない | カオス度 100 が 1 地域／全地域 | 1 年進める | 崩壊しない／崩壊し、次の年は年が進まない | 自動 | `tests/TheogonyX.Core.Tests/PlanetTests.cs` |

## 確かめていないこと（1a の範囲）
- 地域の島・島弧・大陸棚の島の形（隣り合わないこと、海峡の幅）は、画面で見る段（1b）で確かめる。自動テストは数と標高の性質だけ。
- `rules.json` の `planet` の読み込み（`GameDataLoader.ParsePlanetRules`）の自動テストは、`game/` にテストが無いため置いていない。Godot の起動時の読み込みで確かめる。
