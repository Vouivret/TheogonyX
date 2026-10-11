#!/bin/sh
# .agent/rules/project.md を、他のファイルを取り込めないツール用のルールファイルにコピーする。
set -eu
cd "$(dirname "$0")/.."
{ echo "<!-- 自動生成。編集禁止。原本は .agent/rules/project.md。scripts/sync-agent-rules.sh で生成する -->"; echo; cat .agent/rules/project.md; } > .github/copilot-instructions.md
