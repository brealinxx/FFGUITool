#!/usr/bin/env bash
set -euo pipefail

configuration="${CONFIGURATION:-Release}"
self_contained="${SELF_CONTAINED:-true}"
archive="zip"
all=false
runtime=""

usage() {
  echo "Usage: ./publish.sh [-windows|-macos|-all] [--archive zip|7z]"
  echo "Default: publish the current platform group and create a .zip archive."
  echo "  -windows  Build win-x64, win-x86, and win-arm64 packages"
  echo "  -macos    Build osx-x64 and osx-arm64 packages"
  echo "  -all      Build Windows and macOS packages"
}

while [[ $# -gt 0 ]]; do
  case "$1" in
    -windows|--windows)
      runtime="windows"
      shift
      ;;
    -macos|--macos)
      runtime="macos"
      shift
      ;;
    -all|--all)
      all=true
      shift
      ;;
    --archive|-a)
      archive="${2:-}"
      shift 2
      ;;
    --7z)
      archive="7z"
      shift
      ;;
    --zip)
      archive="zip"
      shift
      ;;
    -h|--help)
      usage
      exit 0
      ;;
    *)
      usage
      exit 1
      ;;
  esac
done

case "$archive" in
  zip|7z) ;;
  *)
    usage
    exit 1
    ;;
esac

script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
project_path="$script_dir/FFGUITool/FFGUITool.csproj"
publish_root="$script_dir/FFGUITool/bin/publish"
archive_root="$publish_root/archives"

current_platform_group() {
  local os
  os="$(uname -s)"

  case "$os" in
    Darwin)
      echo "macos"
      ;;
    MINGW*|MSYS*|CYGWIN*)
      echo "windows"
      ;;
    *)
      echo "Unsupported OS: $os. Use -windows, -macos, or -all explicitly." >&2
      exit 1
      ;;
  esac
}

archive_one() {
  local output_path="$1"
  local output_name="$2"

  mkdir -p "$archive_root"
  rm -f "$archive_root/$output_name.$archive"

  if [[ "$archive" == "zip" ]]; then
    (cd "$output_path" && zip -qr "$archive_root/$output_name.zip" .)
    return
  fi

  if ! command -v 7z >/dev/null 2>&1; then
    echo "7z was not found in PATH. Install p7zip/7-Zip or use --archive zip." >&2
    exit 1
  fi

  (cd "$output_path" && 7z a -t7z "$archive_root/$output_name.7z" . >/dev/null)
}

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

  archive_one "$output_path" "$output_name"
}

if [[ "$all" == true ]]; then
  targets=(win-x64 win-x86 win-arm64 osx-x64 osx-arm64)
else
  group="${runtime:-$(current_platform_group)}"
  case "$group" in
    windows)
      targets=(win-x64 win-x86 win-arm64)
      ;;
    macos)
      targets=(osx-x64 osx-arm64)
      ;;
    *)
      usage
      exit 1
      ;;
  esac
fi

echo "Publishing FFGUITool ($configuration)..."
echo "Targets: ${targets[*]}"
echo "Archive: .$archive"

for rid in "${targets[@]}"; do
  publish_one "$rid"
done

echo
echo "Publish complete. Outputs are in: $publish_root"
echo "Archives are in: $archive_root"
