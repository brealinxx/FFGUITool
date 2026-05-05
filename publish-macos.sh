#!/usr/bin/env bash
set -euo pipefail

runtime="${1:-all}"
configuration="${CONFIGURATION:-Release}"
self_contained="${SELF_CONTAINED:-true}"

case "$runtime" in
  all|osx-x64|osx-arm64) ;;
  *)
    echo "Usage: ./publish-macos.sh [all|osx-x64|osx-arm64]"
    exit 1
    ;;
esac

script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
project_path="$script_dir/FFGUITool/FFGUITool.csproj"
publish_root="$script_dir/FFGUITool/bin/publish"

publish_one() {
  local rid="$1"
  local output_name="FFGUITool-$rid"
  local output_path="$publish_root/$output_name"

  echo
  echo "=> $rid -> $output_path"

  dotnet publish "$project_path" \
    --configuration "$configuration" \
    --runtime "$rid" \
    --self-contained "$self_contained" \
    --output "$output_path" \
    -p:PublishSingleFile=false \
    -p:DebugType=None \
    -p:DebugSymbols=false
}

echo "Publishing FFGUITool macOS builds ($configuration)..."

if [[ "$runtime" == "all" ]]; then
  publish_one "osx-x64"
  publish_one "osx-arm64"
else
  publish_one "$runtime"
fi

echo
echo "Publish complete. Outputs are in: $publish_root"
