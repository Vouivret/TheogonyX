#!/bin/sh
# README の「確認」コマンド。ビルド・lint・テスト・テスト仕様書のIDの照合・ルールファイルの生成をまとめて実行する。
set -eu
cd "$(dirname "$0")/.."

sh scripts/sync-agent-rules.sh

if [ -f TheogonyX.sln ]; then
  dotnet build TheogonyX.sln --nologo -v quiet
  dotnet format TheogonyX.sln --verify-no-changes
  dotnet test TheogonyX.sln --nologo --no-build
else
  echo "TheogonyX.sln がまだ無いので、ビルド・lint・テストを飛ばす"
fi

sh scripts/check-test-ids.sh
echo "確認: OK"
