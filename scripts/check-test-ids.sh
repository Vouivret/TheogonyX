#!/bin/sh
# テスト仕様書（docs/test/*.md）で種別が「自動」のIDと、テストコードの [Trait("TS", "...")] を突き合わせる。
set -eu
cd "$(dirname "$0")/.."

tmp=$(mktemp -d)
trap 'rm -rf "$tmp"' EXIT

# 仕様書側: 表の行のうち、種別の列が「自動」のID（テンプレートは除く）
find docs/test -name '*.md' ! -name '_template.md' -exec grep -h '^| TS-' {} + 2>/dev/null \
  | awk -F'|' '{ gsub(/ /, "", $2); gsub(/ /, "", $8); if ($8 == "自動") print $2 }' | sort -u > "$tmp/spec"

# テストコード側
if [ -d tests ]; then
  grep -rhoE 'Trait\("TS", *"TS-[^"]+"\)' tests --include='*.cs' | grep -oE 'TS-[^"]+' | sort -u > "$tmp/code"
else
  : > "$tmp/code"
fi

missing=$(comm -23 "$tmp/spec" "$tmp/code")
unknown=$(comm -13 "$tmp/spec" "$tmp/code")
[ -n "$missing" ] && echo "テストコードに無い「自動」のID:" && echo "$missing"
[ -n "$unknown" ] && echo "テスト仕様書に「自動」として無いID:" && echo "$unknown"
[ -z "$missing$unknown" ]
