# JavaScript（コンパニオン）のルール

> `.agent/rules/project.md` の下位ルール。矛盾した場合は `project.md` を優先する。
> 対象: `companion/**`（コンパニオンの HTML・CSS・JavaScript）。通信のルールは `protocol.md` にある。

## バージョンと設定
- 素の HTML/CSS/JavaScript（フレームワーク・ビルドの工程・npm の依存を持たない）。ホストの `HttpListener` が `companion/` のファイルをそのまま配る。
- 対象のブラウザは `docs/nfr.md` の「対応環境」（iOS Safari / Android Chrome の最新版）。そこで動く ES2022 までの構文を使う。
- スクリプトは ES モジュール（`<script type="module">`、`import`／`export`）で書き、ファイルの先頭で strict モードになる前提で書く。
- 外部の CDN・フォント・画像を読み込まない（同じ LAN の中だけで動かし、URL のトークンを外に出さないため）。

## 命名
| 対象 | 規則 | 例 |
| :--- | :--- | :--- |
| ファイル | kebab-case | `frame-view.js` |
| 関数・変数 | camelCase | `startStream` |
| クラス | PascalCase | `CompanionSocket` |
| 定数 | UPPER_SNAKE_CASE | `FRAME_KIND` |
| HTML の id・class | kebab-case | `world-canvas` |

## 書き方
- メッセージの種類・項目・バイナリの種別は `companion/js/protocol.js` だけに書き、ほかのファイルでは文字列を直書きしない（`protocol.md`）。
- 受け取ったメッセージは、ホストからでも検証してから使う。知らない `type`・`version` は捨てる。
- 受け取った文字列を `innerHTML` に入れない。文字は `textContent` で入れる。
- 映像は `createImageBitmap` で `<canvas>` に描き、`imageSmoothingEnabled = false` でドット絵をにじませない。
- `sessionToken` は `sessionStorage` だけに置く。`localStorage`・Cookie・URL に置かない。URL の `token` は読み取ったらすぐに `history.replaceState` で URL から消す。
- 使わない書き方: `var`、`eval`・`new Function`、`document.write`。

## エラー処理とログ
AIはこの正解例の書き方に合わせる。キー名は `project.md` の「ログとエラー処理」の表に従う。トークンの値はログに出さない。

```js
// 正解例: 構造化ログ（キー名は表に合わせる）
console.info(`event=stream_started`);

// 正解例: 例外を捕まえて、文脈を足して投げ直す
try {
  bitmap = await createImageBitmap(blob);
} catch (error) {
  throw new Error("event=frame_decode_failed", { cause: error });
}

// 正解例: 回復できる失敗を Warn で記録して続行する
if (!isKnownMessage(message)) {
  console.warn(`event=message_rejected reason=unknown_type_or_version`);
  return;
}
```

使わない書き方: キーの無い文章だけのログ、`catch {}` で何もしない、トークン・URL 全体をログに出す。

## 多言語対応
- 画面の文言は `companion/locales/<言語>.json` にキーで置き、HTML に直接書かない。対応言語は `docs/nfr.md` の「対応言語」と同じにする。

## テスト
- 自動テストは持たない。コンパニオンの動作は、テスト仕様書の手動テストで確かめる（`testing.md`）。

## コマンド
変更後の確認は、`README.md` の「確認」にある1つのコマンドで行う。コンパニオンにはビルド・lint・自動テストが無い。
