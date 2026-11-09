# クレジット

外部から取り込んだアドオン・アセット・ライブラリの出典とライセンス。取り込んだら1行追加する（`.agent/rules/project.md` 5.2）。

| 名前 | 種類 | 出典（URL） | ライセンス | 使っている場所 |
| :--- | :--- | :--- | :--- | :--- |
| Godot Engine | エンジン | https://godotengine.org | MIT | 全体（配布物にライセンス表記が必要） |
| QRCoder 1.8.0 | ライブラリ（NuGet） | https://github.com/Shane32/QRCoder | MIT | `game/scenes/main/CompanionHost.cs`（ペアリング用の QR コード） |
| 色のパレット（32 色） | 色の定義 | Claude Code が 2026-11-10 に自前で組んだ（外部のパレットは使っていない。DB16・DB32 はライセンスの記載が確認できないため、使うのをやめた） | 開発者の作成物 | `game/data/palette.json`（[アセットの規格](art-guide.md)の「パレット」） |
| Ponytail | Claude Code のスキル | https://github.com/DietrichGebert/ponytail | MIT | `.claude/skills/`（配布物には含めない） |
| 予言者のスプライト（座った姿・寝ている姿） | 画像 | 開発者が生成 AI で作成（2026-10-11。ツール名は未記録）。4コマの待機アニメーションへの分割と色の整理は Claude Code が行った。2026-11-10 に、人間の指示で 16 × 16 px に縮めたのち、同じ日に開発者が生成 AI（Gemini）で作った新しい参考画（`memo/Gemini_Generated_Image_kv4i1ykv4i1ykv4i.jpg`）から、Claude Code が切り出し直した（座った姿 3 枚を往復させた 4 コマと倒れた姿を、住人と同じ縮尺に縮め、パレットに減色した）。 | 生成 AI ツールの利用規約に従う（ツール名と規約は未確認） | `game/assets/sprites/prophet_idle.png`・`prophet_fallen.png` |
| 建物のスプライト（工房・祠・廃墟） | 画像 | 開発者が生成 AI（Gemini）で作った参考画（`memo/Gemini_Generated_Image_w1566rw1566rw156.jpg`、2026-10-12）から、Claude Code が 3 つを切り出し、背景（廃墟は草と土の色）を抜いて 32 × 32 px に縮めた | 生成 AI ツールの利用規約に従う（規約は未確認）。2026-11-10 に、新しい参考画（`memo/Gemini_Generated_Image_kv4i1ykv4i1ykv4i.jpg`）から切り出し直した（7 種を同じ縮尺で、最大の幅を 32 px に縮め、パレットに減色した） | `game/assets/sprites/building_workshop.png`・`building_shrine.png`・`building_ruin.png` |
| 家のスプライト（わらぶき小屋・木組みの家・れんがの家・石の土台の上の家）と住人のスプライト（7 人） | 画像 | 上と同じ参考画から、Claude Code が切り出した（2026-10-13）。家は外形の多角形で切り抜いて 32 × 32 px（石の土台の上の家は 2 階建てなので 32 × 48 px）に、人は輪郭線の外を抜いて 10 × 14 px に縮め、縁に残った草の色を抜いた | 生成 AI ツールの利用規約に従う（規約は未確認）。2026-11-10 に、新しい参考画（`memo/Gemini_Generated_Image_kv4i1ykv4i1ykv4i.jpg`）から切り出し直した（家は工房などと同じ縮尺、住人 7 人は背の高さを 14 px に縮め、パレットに減色した） | `game/assets/sprites/building_hut.png`・`building_house_medieval.png`・`building_house_brick.png`・`building_overbuild.png`・`resident_1.png`〜`resident_7.png`（家は `game/scenes/main/BuildingView.cs`、住人は `game/scenes/main/ResidentView.cs`） |
| 住人の欲求・状態の印（10 枚。12 × 12 px） | 画像 | 開発者が生成 AI で作った 1 枚の絵（5 × 3。ツール名は未記録、2026-10-14）から、Claude Code が切り出し、透過・縮小・縁の整理をした（自閉・絶望の顔は手で整えた） | 開発者の作成物（生成 AI の出力）。外部の著作物は含まない | `game/assets/sprites/icon_need_*.png`・`icon_state_*.png`（[アセットの規格](art-guide.md)） |
