
---

## 1. システムアーキテクチャ ＆ 責務分離

| コンポーネント | 技術スタック | 役割・責務 | 決定性 / 非同期 |
| --- | --- | --- | --- |
| **Game Core** | Godot 4.x (C#) | 環境数値計算、タイルマップ描画、カード・SAN値管理、判定ロジック | 100% 決定性（xUnit化） |
| **AI Orchestrator** | Python / C# Async Client | LLM (Ollama/vLLM) / ComfyUI API / LoRA 呼び出し、プロンプト管理 | 完全非同期 (`async/await`) |
| **Companion Web** | Vanilla JS / Canvas | スマホ画面へのストリーミング表示、暗黒議会ログ閲覧、カード選択 | クライアントUI |

---

## 2. 4大メカニクスのデータ構造 ＆ プロトコル仕様

### 2.1 予言者ハルシネーション（誤解メカニクス）

プレイヤーが投入したドグマのパラメータ変化に対し、予言者AIがSAN値と性格（Trait）に基づいて独自の解釈（誤解）を生成する。

```json
{
  "$schema": "http://json-schema.org/draft-07/schema#",
  "title": "ProphetInterpretation",
  "type": "object",
  "properties": {
    "dogma_id": { "type": "string", "example": "DOGMA_BLOOD_RAIN" },
    "prophet_san_level": { "type": "integer", "minimum": 0, "maximum": 100 },
    "raw_parameter_changes": {
      "type": "object",
      "properties": {
        "water_amount": { "type": "integer" },
        "chaos_level": { "type": "integer" }
      }
    },
    "interpretation": {
      "type": "object",
      "properties": {
        "headline": { "type": "string", "description": "予言者が叫ぶ神託の見出し" },
        "perceived_intent": { "type": "string", "description": "神の意図だと誤解した内容" },
        "distorted_doctrine": { "type": "string", "description": "人々に発布した歪んだ教義" },
        "san_impact": { "type": "integer", "description": "解釈によって生じた追加SAN減少値" }
      },
      "required": ["headline", "perceived_intent", "distorted_doctrine", "san_impact"]
    }
  }
}

```

---

### 2.2 暗黒議会（マルチエージェントシミュレーション）

予言者が発布した歪んだ教義に基づき、3人のAI代表者（教祖・軍事指導者・科学者）が自律的に議論し、事件を発生させる。

```json
{
  "$schema": "http://json-schema.org/draft-07/schema#",
  "title": "DarkCouncilEvent",
  "type": "object",
  "properties": {
    "year": { "type": "integer" },
    "active_agents": {
      "type": "array",
      "items": {
        "type": "object",
        "properties": {
          "agent_id": { "type": "string", "enum": ["PRIEST", "WARLORD", "SCHOLAR"] },
          "stance": { "type": "string", "description": "教義に対する姿勢（盲従/利用/反逆）" }
        }
      }
    },
    "dialogue_logs": {
      "type": "array",
      "items": {
        "type": "object",
        "properties": {
          "speaker": { "type": "string" },
          "message": { "type": "string" },
          "action_intent": { "type": "string", "description": "暗殺/暴動/秘密実験など" }
        }
      }
    },
    "consequence": {
      "type": "object",
      "properties": {
        "event_title": { "type": "string" },
        "game_effect_trigger": { "type": "string", "description": "C#側で実行する効果ID" }
      }
    }
  }
}

```

---

### 2.3 コンテクスト崩壊 ＆ 歴史要約パイプライン

100年ごとの節目に過去の歴史ログを要約・圧縮。コンテクスト上限に近づくにつれて事実が欠落し、「神話・都市伝説」へと変質する。

```text
[詳細な100年のログ (史実)]
  │
  ├─ 1. C# イベントログ (数値変化・発生事件のRAWデータ)
  │
  ▼ (LLM 1次要約: 史実の圧縮)
[100年史概要テキスト]
  │
  ▼ (LLM 2次要約: 300年経過時・歪曲フィルター適用)
[都市伝説・神話化テキスト (例: 「村の喧嘩」 ➔ 「巨人の討伐神話」)]
  │
  ▼ (ベクトルDB / RAGストア)
[神聖クロニクルデータベース] ➔ 次回の敵（旧神）やAI会話の参照データとして永続化

```

---

### 2.4 聖遺物LoRA（周回メタデータ）

1000年完走または世界崩壊時に、その周回で優勢だった属性・キーワード・生成画像を基に軽量LoRAの設定仕様を出力する。

```json
{
  "$schema": "http://json-schema.org/draft-07/schema#",
  "title": "RelicLoRAConfig",
  "type": "object",
  "properties": {
    "run_id": { "type": "string" },
    "dominant_element": { "type": "string", "example": "BLOOD_MACHINERY" },
    "key_prompts": {
      "type": "array",
      "items": { "type": "string" },
      "example": ["biomechanical nightmare", "crimson rust", "glowing runes", "pixel art DB16"]
    },
    "trigger_word": { "type": "string", "example": "relic_run_04" },
    "output_lora_name": { "type": "string", "example": "relic_blood_machinery.safetensors" }
  }
}

```

---

## 3. C# (Godot) 側モデルクラス設計案

Claudeへ実装を指示する際のコアデータ構造（`Models/`）。

```csharp
namespace TheogonyX.Core.Models;

public record DogmaCard(
    string Id,
    string Name,
    int SanCost,
    int ForestDelta,
    int WaterDelta,
    int TemperatureDelta,
    int ChaosDelta,
    int HarmonyDelta,
    string BasePromptKeywords
);

public record ProphetState(
    int Sanity,
    int Faith,
    bool IsMad,
    string CurrentDelusion
);

public interface IAIServiceClient
{
    Task<ProphetInterpretationDto> InterpretDogmaAsync(DogmaCard card, ProphetState prophetState, CancellationToken ct = default);
    Task<DarkCouncilEventDto> ExecuteCouncilSessionAsync(string doctrineText, CancellationToken ct = default);
    Task<string> CompressHistoryToMythAsync(string rawHistoryText, int elapsedCenturies, CancellationToken ct = default);
    Task<byte[]> GenerateCardArtAsync(string prompt, string negativePrompt, CancellationToken ct = default);
}

```

---

## 4. LLMプロンプトエンジニアリング（システムプロンプト定義）

### 予言者ハルシネーション用 System Prompt

```text
You are the Prophet in "Theogony X", a dark fantasy world-creation simulation.
Your SAN (Sanity) level is currently {{prophet_san_level}}/100.

[RULES]
1. You do NOT understand the God's true intention. You always distort and over-interpret the God's actions based on your current SAN level.
2. If SAN < 30, your interpretation MUST be paranoid, apocalyptic, or grotesquely ritualistic.
3. Output MUST strictly adhere to the requested JSON format.

[INPUT]
Applied Dogma: {{dogma_name}}
Parameter Shifts: {{parameter_shifts}}

Respond ONLY in valid JSON.

```

---

## 5. Claudeへの指示手順（作業ステップ）

上記ドキュメントを準備後、Claudeへ以下のステップで順番に実装依頼を行う：

1. **Step 1 (Core C#):** `DogmaCard` / `ProphetState` / `IAIServiceClient` のインターフェースとデータモデル実装
2. **Step 2 (Local AI Bridge):** Python/C# HTTPクライアントによる Ollama (LLM) ＆ ComfyUI (SD) 非同期接続クラス作成
3. **Step 3 (Dark Council Agent):** 代表者3人のエージェント対話ループおよび結果JSONパーサーの実装
4. **Step 4 (History Compressor):** 100年ごとの歴史ログ圧縮・神話化パイプラインの実装