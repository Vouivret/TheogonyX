# ホストとコンパニオンのメッセージ

通信の契約の原本。ルールは `.agent/rules/protocol.md` にある。C# の型（`src/TheogonyX.Core/Protocol/`）と1対1で対応させる。

- 方式: WebSocket（JSON テキスト）
- 共通の項目: すべてのメッセージに `type`（文字列）と `version`（整数）を持たせる。

## メッセージの一覧
| type | version | 向き | 項目と型 | 意味 |
| :--- | :--- | :--- | :--- | :--- |
| （まだ無い） | | | | |
