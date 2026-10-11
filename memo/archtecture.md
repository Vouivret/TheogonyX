
プロトタイプ（MVP：最小実用プロダクト）を構築するにあたり、最初に手を動かすべき「データ定義」「基本アルゴリズム」「画面構成・入出力」の3点を完全に定義します。

  

## 1. 最小構成（MVP）で実装する範囲

最初のプロトタイプでは、グラフィックや通信処理を組み込む前に「C#（Godot/Unity）または Python のコンソール/簡易GUI」でコアロジックが破綻なく回るかを検証します。

  

- **検証ターゲット:**
    
      
    1. 夢のタイムライン（配列）を1コマ置き換える（大介入）
        
          
        
    2. 予言者がペア（因果）を抽出して「ドグマ（教義）」を生成する
        
          
        
    3. タイムスキップ（100年）して、ドグマが「地球環境（資源・気温）」に与えた影響を計算する
        
          
        
    4. 人間から「問解（問い合わせ）」が届き、カードで返答して誤解が変化する
        
          
        

## 2. 実装用の基本データ構造（C#定義案）

そのままプロジェクトに貼り付けて使えるデータクラス構造です。

  

C#

```
using System;
using System.Collections.Generic;

// 1. 象徴記号（Symbol）の定義
public enum ConceptCategory { Nature, Artifact, Action, Emotion, Universal }

[Serializable]
public class Symbol
{
    public string id;            // 例: "SYM_WATER", "SYM_BLOOD"
    public string name;          // 表示名 (解読前は記号)
    public ConceptCategory category;
    public float baseDanger;     // 破壊的影響度 (0.0 - 1.0)
}

// 2. 夢のタイムラインとノード
[Serializable]
public class DreamNode
{
    public string symbolId;
    public bool isEditable;      // プレイヤーが書き換え可能か
    public float emotionalWeight;// 感情強度
}

// 3. 形成される教義（Dogma）
public enum DogmaType { Taboo, Worship, Destruction }

[Serializable]
public class Dogma
{
    public string targetSymbolId;  // 対象（例: 水、木）
    public DogmaType type;          // 禁忌 / 崇拝 / 破壊
    public float intensity;         // 信仰度（0.0 - 1.0）
    public float spreadRate;        // 感染速度
}

// 4. 予言者（Prophet）データ
[Serializable]
public class Prophet
{
    public string name;
    public float fanaticism;       // 狂信度
    public List<DreamNode> currentDream = new List<DreamNode>();
    public List<Dogma> activeDogmas = new List<Dogma>();
}

// 5. 惑星環境ステート（16x16または単一グローバル値）
[Serializable]
public class WorldState
{
    public int currentYear = 0;
    public float temperature = 15.0f;  // 気温 (℃)
    public float forestAmount = 100.0f;// 森林量 (0 - 100)
    public float waterAmount = 100.0f; // 水資源量 (0 - 100)
    public float chaosLevel = 0.0f;    // カオス度
    public float harmonyLevel = 50.0f; // 調和度
    public bool isDestroyed = false;   // 星の崩壊フラグ
}
```

## 3. ドグマ生成 ＆ 環境影響アルゴリズム

### ① 夢からのドグマ抽出ロジック（Pseudo Code）

C#

```
public Dogma InterpretDream(Prophet prophet, List<DreamNode> dream)
{
    // 隣り合うノードのペアから「最も衝撃度の高い組み合わせ」を探す
    float maxImpact = 0f;
    DreamNode causeNode = null;
    DreamNode effectNode = null;

    for (int i = 0; i < dream.Count - 1; i++)
    {
        float impact = (dream[i].emotionalWeight + dream[i+1].emotionalWeight) * prophet.fanaticism;
        if (impact > maxImpact)
        {
            maxImpact = impact;
            causeNode = dream[i];
            effectNode = dream[i+1];
        }
    }

    // 例：原因が「雨」で結果が「死/血」なら [雨 = 禁忌(TABOO)] とする規則
    DogmaType generatedType = DetermineDogmaType(causeNode, effectNode);

    return new Dogma {
        targetSymbolId = causeNode.symbolId,
        type = generatedType,
        intensity = Math.Min(1.0f, maxImpact),
        spreadRate = prophet.fanaticism * 0.5f
    };
}
```

### ② タイムスキップ（100年経過）処理

C#

```
public void SimulateTimeSkip(WorldState world, List<Dogma> activeDogmas, int years)
{
    for (int i = 0; i < years; i++)
    {
        foreach (var dogma in activeDogmas)
        {
            // 教義に応じた環境の偏り計算
            switch (dogma.targetSymbolId)
            {
                case "SYM_TREE":
                    if (dogma.type == DogmaType.Taboo) {
                        world.forestAmount += 0.5f * dogma.intensity; // 木を切らないため森林爆増
                        world.waterAmount -= 0.3f * dogma.intensity;  // 地下水が吸われる
                    } else if (dogma.type == DogmaType.Destruction) {
                        world.forestAmount -= 1.0f * dogma.intensity; // 森林壊滅
                    }
                    break;

                case "SYM_WATER":
                    if (dogma.type == DogmaType.Destruction) {
                        world.waterAmount -= 0.8f * dogma.intensity;  // 海洋・水質汚染
                        world.temperature += 0.1f;                     // 砂漠化・温暖化
                    }
                    break;
            }
        }

        // 環境崩壊の判定
        if (world.forestAmount <= 0 || world.waterAmount <= 0 || world.temperature > 60.0f) {
            world.chaosLevel += 1.0f;
        }

        // 星の崩壊条件（限界突破）
        if (world.chaosLevel >= 100.0f) {
            world.isDestroyed = true;
            break;
        }
    }
}
```

## 4. 最初の一歩（開発ロードマップ）

プロトタイプ開発をスムーズにスタートするための **Day 1 〜 Day 5 の作業順序** です。

  

```
【Day 1: コアエンジンの作成（Console / Python または C#）】
  ・上記のデータクラスを作成。
  ・夢の改ざん（配列の要素置換）と、ドグマ抽出アルゴリズムをターミナル出力で動かす。

【Day 2: タイムスキップ ＆ パラメータシミュレーション】
  ・100年分のループ処理を組み、ドグマによって森林や水資源、気温がどうズレるかテスト。
  ・星の崩壊（限界値突破）フラグが正しく折れるか確認。

【Day 3: 神啓（問解）システムの組み込み】
  ・人間からの「[木] + [？]」という問いに対し、カードを1枚渡してドグマの強度が弱まる／変わる処理を追加。

【Day 4: Godot / Unity への移植（簡易2D画面化）】
  ・ターミナルで動いたロジックをゲームエンジンへ移行。
  ・16x16のカラータイルマップ（緑＝森、黄＝砂漠、青＝水）で地球環境のビジュアル化。

【Day 5: WebSocketによるスマホ接続テスト】
  ・PC側でローカルWebSocketサーバーを立て、スマホのブラウザ（HTML/JS）から「カード送信ボタン」を押してPC側の夢が書き換わるかテスト。
```


Godot 4 での本格開発決定ですね！2Dドット絵のピクセルパーフェクト描画や2Dライティング、標準の WebSocket サポートの軽快さは、このゲームの構想にまさにベストマッチです。

  

すぐに Godot 4 上でプロジェクトを立ち上げてコアロジックを検証できるよう、**プロトタイプのプロジェクト構成案**と**そのまま動かせる最小のC#スクリプト**を用意しました。

  

## 1. Godot 4 プロジェクトの基本構成案

Godot 4 でプロジェクト（C#プロジェクト）を新規作成し、以下のようなフォルダ構造をセットアップします。

  

Plaintext

```
res://
├── scenes/
│   ├── Main.tscn               # メインゲーム画面（2Dノード、カメラ、UI）
│   └── Prophet.tscn            # 予言者・NPCの2Dスプライト
├── scripts/
│   ├── models/
│   │   ├── Symbol.cs           # 象徴記号データ定義
│   │   ├── DreamNode.cs        # 夢のコマデータ
│   │   ├── Dogma.cs            # ドグマ（教義）データ
│   │   └── WorldState.cs       # 地球環境データ
│   ├── core/
│   │   ├── DreamInterpreter.cs # 夢の解析〜ドグマ変換エンジン
│   │   └── WorldSimulator.cs   # タイムスキップ（環境変化）シミュレーター
│   └── MainController.cs       # 全体を統括するノードスクリプト
```

## 2. そのまま動かせるプロトタイプコード（C#）

Godot 4 の `Node` にアタッチして、**「夢の改ざん」→「ドグマの自動抽出」→「100年間の地球環境変化」** をコンソール（Godotの出力ウィンドウ）で即座に検証できるスクリプトです。

  

### ① スクリプト: `scripts/core/WorldCore.cs`

C#

```
using Godot;
using System;
using System.Collections.Generic;

namespace GameCore
{
    // --- 1. データモデルの定義 ---
    public enum DogmaType { Taboo, Worship, Destruction }

    public class Symbol
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public float EmotionalPolarity { get; set; } // -1.0(恐怖/死) 〜 +1.0(歓喜/生命)
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

    // --- 2. メインのゲームロジックノード ---
    public partial class WorldCore : Node
    {
        private WorldState _world = new WorldState();
        private List<DreamNode> _prophetDream = new List<DreamNode>();
        private List<Dogma> _activeDogmas = new List<Dogma>();

        public override void _Ready()
        {
            GD.Print("=== Godot 4 歪んだ夢プロトタイプ開始 ===");

            // シンボル（象徴記号）データベースの初期化
            var symTree = new Symbol { Id = "SYM_TREE", Name = "木/森林", EmotionalPolarity = 0.2f };
            var symRain = new Symbol { Id = "SYM_RAIN", Name = "恵みの雨", EmotionalPolarity = 0.5f };
            var symBlood = new Symbol { Id = "SYM_BLOOD", Name = "血/死", EmotionalPolarity = -0.9f };

            // 元々の夢: [木] -> [雨]（平和な夢）
            _prophetDream.Add(new DreamNode { Symbol = symTree, Weight = 0.5f });
            _prophetDream.Add(new DreamNode { Symbol = symRain, Weight = 0.6f });

            GD.Print("\n--- 1. プレイヤーの改ざん前 ---");
            PrintDream();

            // プレイヤーの改ざん介入！ [雨] を [血/死] に差し替える
            GD.Print("\n>>> プレイヤーが夢の2コマ目を [血/死] カードにスワップ！ <<<");
            _prophetDream[1] = new DreamNode { Symbol = symBlood, Weight = 0.9f };

            GD.Print("\n--- 2. 改ざん後の夢 ---");
            PrintDream();

            // 予言者による夢の解釈とドグマ生成
            Dogma newDogma = InterpretDream(_prophetDream, 1.2f); // 狂信度1.2
            _activeDogmas.Add(newDogma);
            GD.Print($"\n【生成されたドグマ】 対象: {newDogma.TargetSymbolId} | 種類: {newDogma.Type} | 強度: {newDogma.Intensity:F2}");

            // 100年間のタイムスキップ実行！
            GD.Print("\n--- 3. 100年間の歴史跳躍（タイムスキップ）実行 ---");
            SimulateTimeSkip(100);

            GD.Print($"\n=== 100年後の地球 ===");
            GD.Print($"西暦/年代: {_world.Year}年");
            GD.Print($"森林量: {_world.ForestAmount:F1} | 水資源: {_world.WaterAmount:F1} | 気温: {_world.Temperature:F1}℃");
            GD.Print($"世界のカオス度: {_world.ChaosLevel:F1}%");
            GD.Print($"星の崩壊状況: {(_world.IsDestroyed ? "【木端微塵に破滅した】" : "無事に存続中")}");
        }

        private void PrintDream()
        {
            string output = "夢のタイムライン: ";
            foreach (var node in _prophetDream)
            {
                output += $"[{node.Symbol.Name}] -> ";
            }
            GD.Print(output.TrimEnd(' ', '-', '>'));
        }

        // 夢から「最もインパクトの強い因果」を抽出するアルゴリズム
        private Dogma InterpretDream(List<DreamNode> dream, float prophetFanaticism)
        {
            float maxImpact = 0f;
            DreamNode cause = dream[0];
            DreamNode effect = dream[1];

            for (int i = 0; i < dream.Count - 1; i++)
            {
                float impact = (dream[i].Weight + dream[i + 1].Weight) * prophetFanaticism;
                if (impact > maxImpact)
                {
                    maxImpact = impact;
                    cause = dream[i];
                    effect = dream[i + 1];
                }
            }

            // 原因のシンボルに対して、結果がネガティブ(血・死)なら「禁忌(TABOO)」を形成
            DogmaType type = (effect.Symbol.EmotionalPolarity < 0) ? DogmaType.Taboo : DogmaType.Worship;

            return new Dogma
            {
                TargetSymbolId = cause.Symbol.Id,
                Type = type,
                Intensity = Mathf.Min(1.0f, maxImpact)
            };
        }

        // タイムスキップ中の地球環境シミュレーション
        private void SimulateTimeSkip(int years)
        {
            for (int i = 0; i < years; i++)
            {
                _world.Year++;

                foreach (var dogma in _activeDogmas)
                {
                    if (dogma.TargetSymbolId == "SYM_TREE" && dogma.Type == DogmaType.Taboo)
                    {
                        // 「木を切るな」という禁忌により森林は増えるが、地下水が過剰消費される
                        _world.ForestAmount += 0.4f * dogma.Intensity;
                        _world.WaterAmount -= 0.3f * dogma.Intensity;
                    }
                }

                // 環境異常による影響
                if (_world.WaterAmount < 50.0f)
                {
                    _world.Temperature += 0.05f; // 乾燥化・気候変動
                    _world.ChaosLevel += 0.2f;
                }

                if (_world.ChaosLevel >= 100.0f || _world.WaterAmount <= 0)
                {
                    _world.IsDestroyed = true;
                    GD.Print($"[警告] 西暦 {_world.Year} 年、環境が限界突破して星が崩壊しました！");
                    break;
                }
            }
        }
    }
}
```

## 3. 次のステップ（開発ロードマップ）

このスクリプトを Godot 4 のノードにアタッチして実行（F5）するだけで、**「夢をいじると100年後の地球が乾燥してカオス度が上がる」** というゲームの核がコンソール上で動き出します。

  

ここから先、どの部分から肉付けしていきましょうか？

  

**1.2DスプライトとTileMapの配置:**画面の視覚化。

Godotの 2D TileMap ノードを使って、環境数値（`ForestAmount`, `WaterAmount`）に連動して地形タイルが自動で「緑→黄（砂漠）」に変色する見た目を作る。

  

**2.WebSocketサーバーノードの追加:**スマホ連携。

Godot内に `WebSocketPeer` を組み込み、スマホのブラウザからカード選択イベントを受信して夢を書き換えられるようにする。

  

**3.Compute Shader（GLSL）の組み込み:**大量NPC並列計算。

画面上に数百〜数千のドット絵NPCを生成し、ドグマに従ってワラワラと移動する計算を Compute Shader にオフロードする。

  

どれから着手するか、あるいは気になるコードのカスタマイズがあれば教えてください！