Claudeからの問いかけは、MVP（最小プロトタイプ）のコア計算を明確化するための決定事項ですね。

  

Claudeが迷わずに単体実装へ入れるよう、そのまま `PROMPT_FOR_CLAUDE.md` やチャットにコピペして渡せる形式でルールと数値ロジックを整理しました。

  

### MVP ゲームルール＆数値仕様書（Claudeへの回答用）

以下の内容をClaudeに伝えて実装を進めてもらってください。

  

#### 1. 夢の解釈（InterpretDream）

- **感情強度（Impact）の計算式:**
    
      
    - $\text{Impact} = (\text{ノード1のWeight} \times \vert{}\text{ノード1のEmotionalPolarity}\vert{}) + (\text{ノード2のWeight} \times \vert{}\text{ノード2のEmotionalPolarity}\vert{})$
        
          
        
    - 重み（Weight）と極性の絶対値を掛け合わせた合算値とします。
        
          
        
- **対象ペアの範囲と同点時の優先度:**
    
      
    - **隣接するすべてのペア**（要素数 $N$ の配列なら $N-1$ 個のペア）を走査します。
        
          
        
    - 同点（Impactが同じ）の場合は、「配列の後ろ（未来）にあるペア」を優先します（直近の夢の記憶ほど強く残るため）。
        
          
        
- **教義の種別判定（Taboo / Worship / Destruction）:**
    
      
    - **Taboo（禁忌）:** 2つのノードの極性の和（$\text{Polarity}_1 + \text{Polarity}_2$）が **$-0.3$ 未満**（ネガティブ・恐怖）
        
          
        
    - **Worship（崇拝）:** 極性の和が **$+0.3$ 以上**（ポジティブ・聖なるもの）
        
          
        
    - **Destruction（破壊）:** 極性の和が **$-0.3$ 以上 $+0.3$ 未満**（無差別・混沌的領域）
        
          
        
    - ※MVPから `Destruction` を含めて挙動のバリエーションを出します。
        
          
        
- **ドグマの対象シンボルの選定ルール:**
    
      
    - ペアの1番目のノード（原因側のシンボル）を対象とします。
        
          
        
    - 例: `[雨] ➔ [血]` の場合、原因である `[雨]` が対象となり、「雨＝禁忌（雨を避ける/恐れる）」が発生します。
        
          
        

#### 2. MVP で使う記号（Symbol）定義（全4種）

MVPでは極性と属性がわかりやすい以下の4つの記号（Symbol）を基本セットとします。

  

|**ID**|**名前**|**極性（EmotionalPolarity）**|**概要**|
|---|---|---|---|
|`SYM_TREE`|木/森林|`+0.4`|生命・環境（中立〜ポジティブ）|
|`SYM_RAIN`|恵みの雨|`+0.6`|恵み・水資源（ポジティブ）|
|`SYM_BLOOD`|血/死|`-0.9`|破滅・恐怖（強力なネガティブ）|
|`SYM_FIRE`|聖なる火|`+0.1`|文明・エネルギー（ややポジティブ）|

#### 3. 時代跳躍（SimulateTimeSkip）の数値計算ルール

1年あたりの変化量は **`ドグマの強度（Intensity）`** に比例して加減算されます。

  

- **ドグマごとの年間環境変化率（1年あたり）:**
    
      
    - **`SYM_TREE`（木）**
        
          
        - **Taboo（禁忌 - 伐採禁止）:** `ForestAmount` $+1.0 \times \text{Intensity}$ / `WaterAmount` $-0.5 \times \text{Intensity}$（森林増えすぎで水消費）
            
              
            
        - **Worship（崇拝 - 植林）:** `ForestAmount` $+0.5 \times \text{Intensity}$ / `HarmonyLevel` $+0.2 \times \text{Intensity}$
            
              
            
        - **Destruction（破壊 - 焼き払い）:** `ForestAmount` $-2.0 \times \text{Intensity}$ / `ChaosLevel` $+0.8 \times \text{Intensity}$
            
              
            
    - **`SYM_RAIN`（雨）**
        
          
        - **Taboo（禁忌 - 祈祷拒否）:** `WaterAmount` $-1.0 \times \text{Intensity}$ / `Temperature` $+0.1 \times \text{Intensity}$（乾燥・砂漠化）
            
              
            
        - **Worship（崇拝 - 治水）:** `WaterAmount` $+0.5 \times \text{Intensity}$ / `Temperature` $-0.05 \times \text{Intensity}$
            
              
            
        - **Destruction（破壊 - 水質汚染）:** `WaterAmount` $-1.5 \times \text{Intensity}$ / `ChaosLevel` $+0.5 \times \text{Intensity}$
            
              
            
    - **`SYM_FIRE`（火）**
        
          
        - **Taboo（禁忌 - 火の封印）:** `Temperature` $-0.2 \times \text{Intensity}$（寒冷化）
            
              
            
        - **Destruction（破壊 - 焦土作戦）:** `ForestAmount` $-1.5 \times \text{Intensity}$ / `Temperature` $+0.3 \times \text{Intensity}$ / `ChaosLevel` $+1.0 \times \text{Intensity}$
            
              
            
    - **`SYM_BLOOD`（血）**
        
          
        - **Worship / Destruction（生贄・戦争）:** `ChaosLevel` $+1.5 \times \text{Intensity}$ / `HarmonyLevel` $-1.0 \times \text{Intensity}$
            
              
            
- **パラメータのクランプ（上限・下限）:**
    
      
    - `ForestAmount` （森林量）: `0.0` 〜 `100.0`
        
          
        
    - `WaterAmount` （水資源）: `0.0` 〜 `100.0`
        
          
        
    - `Temperature` （気温）: `-50.0℃` 〜 `80.0℃`（初期値 `15.0℃`）
        
          
        
    - `ChaosLevel` （カオス度）: `0.0` 〜 `100.0`
        
          
        
    - `HarmonyLevel`（調和度）: `0.0` 〜 `100.0`（初期値 `50.0`）
        
          
        
- **星の崩壊判定（IsDestroyed = true になる条件）:**
    
    以下のいずれかを満たした瞬間、星が崩壊（Game Over / Reset）します。
    
      
    1. **`ChaosLevel >= 100.0`**（社会・環境の完全破滅）
        
          
        
    2. **`WaterAmount <= 0.0`** かつ **`ForestAmount <= 0.0`**（不毛の死の星）
        
          
        
    3. **`Temperature >= 70.0℃`** または **`Temperature <= -40.0℃`**（生存不能の極熱/全球凍結）
        
          
        

### 

