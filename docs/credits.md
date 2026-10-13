# クレジット

外部から取り込んだアドオン・アセット・ライブラリの出典とライセンス。取り込んだら1行追加する（`.agent/rules/project.md` 5.2）。

| 名前 | 種類 | 出典（URL） | ライセンス | 使っている場所 |
| :--- | :--- | :--- | :--- | :--- |
| Godot Engine | エンジン | https://godotengine.org | MIT | 全体（配布物にライセンス表記が必要） |
| QRCoder 1.8.0 | ライブラリ（NuGet） | https://github.com/Shane32/QRCoder | MIT | `game/scenes/main/CompanionHost.cs`（ペアリング用の QR コード） |
| Ponytail | Claude Code のスキル | https://github.com/DietrichGebert/ponytail | MIT | `.claude/skills/`（配布物には含めない） |
| 予言者のスプライト（座った姿・寝ている姿） | 画像 | 開発者が生成 AI で作成（2026-10-11。ツール名は未記録）。4コマの待機アニメーションへの分割と色の整理は Claude Code が行った | 生成 AI ツールの利用規約に従う（ツール名と規約は未確認） | `game/assets/sprites/prophet_idle.png`・`prophet_fallen.png` |
| 建物のスプライト（工房・祠・廃墟） | 画像 | 開発者が生成 AI（Gemini）で作った参考画（`memo/Gemini_Generated_Image_w1566rw1566rw156.jpg`、2026-10-12）から、Claude Code が 3 つを切り出し、背景（廃墟は草と土の色）を抜いて 32 × 32 px に縮めた | 生成 AI ツールの利用規約に従う（規約は未確認） | `game/assets/sprites/building_workshop.png`・`building_shrine.png`・`building_ruin.png` |
| 家のスプライト（わらぶき小屋・木組みの家・れんがの家・石の土台の上の家）と住人のスプライト（7 人） | 画像 | 上と同じ参考画から、Claude Code が切り出した（2026-10-13）。家は外形の多角形で切り抜いて 32 × 32 px（石の土台の上の家は 2 階建てなので 32 × 48 px）に、人は輪郭線の外を抜いて 10 × 14 px に縮め、縁に残った草の色を抜いた | 生成 AI ツールの利用規約に従う（規約は未確認） | `game/assets/sprites/building_hut.png`・`building_house_medieval.png`・`building_house_brick.png`・`building_overbuild.png`・`resident_1.png`〜`resident_7.png`（家は `game/scenes/main/BuildingView.cs`、住人は `game/scenes/main/ResidentView.cs`） |
