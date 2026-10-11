# Project Overview: Theogony X

## 1. Executive Summary
**Theogony X** は、プレイヤーが「神」となり、予言者の夢を改ざんすることで人間の誤解を誘発し、それによって生まれる教義（ドグマ）と地球環境の激変を観察・手綱引きする「ダブルブラインド（多重誤解）・箱庭歴史シミュレーター」です。

- **開発環境:** Godot Engine 4.x (C#)
- **ターゲットプラットフォーム:** PC (Host / Simulation / High-density Rendering) + PWA/Mobile Browser (Companion Interface)
- **コアテーマ:** 「カオス（混迷・破滅）」と「調和（秩序・鑑賞）」の天秤

---

## 2. Core Game Loop & Mechanics

1. **ミクロ観察 & 介入 (1日スケール):**
   - 予言者の夢（記号のタイムライン）の1コマをプレイヤーが持つ「概念カード」とスワップ（改ざん）。
   - 人間からの「問解（神啓の問い合わせ）」に対し、記号カードで返答して解釈をズラす。
   - 局所的な天候変化や耳鳴りで微調整を行う。

2. **マクロ跳躍 (50〜300年スキップ):**
   - 改ざんされた夢から予言者が「因果（例: [雨]＋[血/死] ➔ 雨は禁忌）」を抽出してドグマを形成。
   - ドグマが集団に感染・固着し、環境（森林・水質・気温・文明・戦争）に極端な偏りを与える。

3. **歴史と地層の蓄積:**
   - 原始時代 ➔ 古代 ➔ 中世 ➔ 現代 ➔ SF未来 ➔ 超越時代へ推移。
   - 滅びた文明は「地層（遺跡）」として蓄積し、後の時代の人間や地底人が発掘して新たな誤解を生む。
   - **終了条件:** 星が物理的に崩壊・消滅するまで（制限なし・タブーなし）。

---

## 3. Architecture & Technical Design

### A. Role Division (Host & Companion)
- **PC (Godot Host):**
  - 重い環境シミュレーション、NPCの思考・移動計算、2Dピクセルアート＆ライティング描画。
  - Compute Shader (GLSL) による大量NPCの並列更新・GPU Instancing描画。
  - ローカルWebSocketサーバーの常時起動。
- **Mobile (Web / PWA Companion):**
  - PC上のWebSocketサーバーに接続するリモート神託インターフェース。
  - 夢改ざんUI（スワップ）、問解への返答、地球バイタル（カオス/調和メーター）のリアルタイム監視。

### B. Core Data Models (C#)

```csharp
namespace TheogonyX.Core
{
    public enum DogmaType { Taboo, Worship, Destruction }

    public class Symbol
    {
        public string Id { get; set; }           // 例: "SYM_TREE", "SYM_BLOOD"
        public string Name { get; set; }         // 表示名
        public float EmotionalPolarity { get; set; } // -1.0 (恐怖) 〜 +1.0 (歓喜)
    }

    public class DreamNode
    {
        public Symbol Symbol { get; set; }
        public float Weight { get; set; }
    }

    public class Dogma
    {
        public string TargetSymbolId { get; set; }
        public DogmaType Type { get; set; }
        public float Intensity { get; set; }
    }

    public class WorldState
    {
        public int Year { get; set; } = 0;
        public float ForestAmount { get; set; } = 100.0f;
        public float WaterAmount { get; set; } = 100.0f;
        public float Temperature { get; set; } = 15.0f;
        public float ChaosLevel { get; set; } = 0.0f;
        public bool IsDestroyed { get; set; } = false;
    }
}
````

## 4. Initial MVP Development Goal (First Milestone)

Claudeとのファーストステップとして、以下の最小実行ユニット（MVP）を構築してください。

  

### Objective:

Godot 4 (C#) のコンソール出力および簡易2Dノード上で、**「夢の書き換え ➔ ドグマ自動生成 ➔ 100年シミュレーション実行 ➔ 地球パラメータの変化」** のループが正しく回るコアエンジンを実装する。

  

### Implementation Tasks for Claude:

1. `TheogonyX.Core` ネームスペース下に上記モデルクラスを作成。
    
      
    
2. 予言者の夢配列から「最も感情強度の高い隣接ノード」を抽出し、`DogmaType`（Taboo/Worship）を割り分ける `InterpretDream` ロジックを実装。
    
      
    
3. `SimulateTimeSkip(int years)` 関数を実装し、ドグマの強度と種別に応じて `WorldState`（森林量・水資源・気温・カオス度）を段階的に変化させる。
    
      
    
4. `Node` を継承した `WorldCore.cs` を作成し、`_Ready()` 内で改ざん前後の環境変化ログを `GD.Print` で出力するテストシナリオを組む。
    
      
    

## 5. Development Style Guidelines

- **KISS原則:** 早期の過度な抽象化は避け、まずは1つの星・1名の予言者・100年ループのロジックが確実に動く構造を優先すること。
    
      
    
- **データ分離:** シンボル定義やドグマ判定の閾値は、後でJSON/Resource化できるようハードコードを避ける設計を意識すること。
    
      
    
- **Godot 4.x C# 規格:** Godot 4 の C# 命名規則・ノードアクセス様式に準拠すること。
    


