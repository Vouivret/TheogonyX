#!/bin/sh
# Claude Code のフック。セルフレビューのコマンドが実行されたら、.git/ai-review-log.jsonl に1行追記する。
# ユーザーが入力した場合は UserPromptExpansion の command_name、Claude がスキルを呼んだ場合は PostToolUse の tool_input.skill に名前が入る。
# 記録できなくても作業は止めない（常に終了コード0）。
targets=" code-review ponytail-review "

input=$(cat)
name=$(printf '%s' "$input" | sed -n 's/.*"command_name"[[:space:]]*:[[:space:]]*"\([^"]*\)".*/\1/p')
[ -n "$name" ] || name=$(printf '%s' "$input" | sed -n 's/.*"skill"[[:space:]]*:[[:space:]]*"\([^"]*\)".*/\1/p')
[ -n "$name" ] || exit 0
case "$targets" in *" $name "*) ;; *) exit 0 ;; esac

cd "${CLAUDE_PROJECT_DIR:-.}" 2>/dev/null || exit 0
gitdir=$(git rev-parse --git-common-dir 2>/dev/null) || exit 0
[ -n "$(git status --porcelain 2>/dev/null)" ] && uncommitted=true || uncommitted=false

printf '{"time":"%s","command":"%s","branch":"%s","commit":"%s","uncommitted":%s}\n' \
  "$(date -u +%Y-%m-%dT%H:%M:%SZ)" "$name" "$(git branch --show-current)" \
  "$(git rev-parse --short HEAD 2>/dev/null)" "$uncommitted" >> "$gitdir/ai-review-log.jsonl"
exit 0
