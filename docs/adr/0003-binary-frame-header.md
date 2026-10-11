---
status: accepted   # accepted | superseded | deprecated
superseded_by:     # 置き換えたADRの番号（superseded のときのみ）
area: [protocol, companion]
date: 2026-10-12
---

# 0003: 画面の配信は WebSocket のバイナリフレームで送り、先頭 2 バイトに種別とバージョンを置く

## 背景
コンパニオンに星の画面を 8 fps で配信する（[plan](../../plan/feat-m2-frame-stream.md)）。通信の契約は JSON のテキストフレームだけを想定しており、`.agent/rules/protocol.md` は「すべてのメッセージに `type` と `version` を持たせる」と定めている。JPEG の画像をどう載せるかを決める必要があった。

## 決定
- 画像は WebSocket のバイナリフレームで送る。
- 1 バイト目に種別（`0x01` = `FRAME`）、2 バイト目にバージョン（今は 1）を置き、3 バイト目以降に JPEG を続ける。
- 受け取った側は、知らない種別・バージョンのバイナリフレームを捨てる。
- JSON のメッセージはこれまでどおりテキストフレームで送る。種類の一覧は [messages.md](../protocol/messages.md) に置く。

## 検討した他の案
- **JSON に Base64 で入れる**（`{"type":"FRAME","version":1,"jpeg":"..."}`）: 契約を JSON だけに保てるが、データが約 3 割増え、毎コマ文字列への変換が要る。
- **ヘッダーを付けずに JPEG だけを送る**: 一番単純だが、バイナリで送るものが増えたときやバージョンを上げたときに区別できず、`protocol.md` の `type`・`version` の決まりも守れない。

## 結果
- 画像を変換せずに送れ、コンパニオンは `ArrayBuffer` の先頭 2 バイトを見るだけで振り分けられる。
- 契約が JSON とバイナリの 2 種類になり、バイナリの種別は 1 バイト（256 種類まで）に限られる。
- バイナリのメッセージに JPEG 以外の項目（時刻・コマ番号など）が要るようになったら、ヘッダーの形を見直す（バージョンを上げる）。
