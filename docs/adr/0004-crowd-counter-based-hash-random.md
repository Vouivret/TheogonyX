---
status: accepted   # accepted | superseded | deprecated
superseded_by:     # 置き換えたADRの番号（superseded のときのみ）
area: [crowd, simulation, gpu]
date: 2026-10-12
---

# 0004: 群衆の乱数は System.Random ではなく、(シード, 人の番号, 歩数) から作る整数のハッシュ関数にする

## 背景
群衆（[crowd.md](../domain/crowd.md)）は 10,000 人を GPU（Compute Shader）で動かし、同じ計算の CPU 版をコアに持って突き合わせる（`.agent/rules/godot.md`）。散策の向きや始めの配置に乱数が要る。`.agent/rules/csharp.md` は「乱数は `System.Random` をシード付きで生成する」と定めているが、`System.Random` は GLSL で同じ並びを出せず、また 1 つの生成器を順に引く作りは並列計算に向かない（[plan](../../plan/feat-m3-crowd-cpu.md)）。

## 決定
- 群衆の乱数は、(シード, 人の番号, 歩数) の 3 つの整数から作るハッシュ関数（32 bit の符号なし整数の乗算・xor・シフトだけ）で求める。
- 同じ関数を C#（コアの CPU 版）と GLSL（GPU 版）に書く。
- 群衆以外のシミュレーション（夢・ドグマなど）は、これまでどおり `System.Random` を使う。

## 検討した他の案
- **`System.Random` を人ごとに持つ**: 10,000 個の生成器が要り、GPU で同じ並びを出せないので突き合わせができない。
- **CPU で乱数を作って GPU に送る**: 毎フレーム 10,000 人分の乱数を転送することになり、GPU で計算する意味が薄れる。

## 結果
- 人ごと・歩ごとの乱数を、順番に依存せずに求められる。GPU と CPU で同じ乱数になり、位置の差は浮動小数点の誤差だけになる。
- `csharp.md` の乱数のルールの例外になる。ルールに「群衆の GPU と共有する乱数はこの ADR に従う」と足すことを、ルールの改善として提案する。
- ハッシュの質は暗号用ではないが、見た目だけの群衆には足りる。
