#!/usr/bin/env bash
set -euo pipefail

configuration="${CONFIGURATION:-Release}"
self_contained="${SELF_CONTAINED:-true}"
archive="zip"
all=false
runtime=""
create_dmg=false

usage() {
  echo "Usage: ./publish.sh [-windows|-macos|-linux|-all] [--archive zip|7z] [--dmg]"
  echo "Default: publish the current platform group and create a .zip archive."
  echo "  -windows  Build win-x64, win-x86, and win-arm64 packages"
  echo "  -macos    Build osx-x64 and osx-arm64 packages"
  echo "  -linux    Build linux-x64 and linux-arm64 packages"
  echo "  -all      Build Windows, macOS, and Linux packages"
  echo "  --dmg     On macOS, wrap osx-* outputs into .app bundles and .dmg images"
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
    -linux|--linux)
      runtime="linux"
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
    --dmg)
      create_dmg=true
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
dmg_root="$publish_root/dmg"

project_version() {
  local version
  version="$(grep -m1 '<Version>' "$project_path" | sed -E 's/.*<Version>([^<]+)<\/Version>.*/\1/' || true)"

  if [[ -z "$version" || "$version" == *"<Version>"* ]]; then
    version="$(grep -m1 '<InformationalVersion>' "$project_path" | sed -E 's/.*<InformationalVersion>([^<]+)<\/InformationalVersion>.*/\1/' || true)"
  fi

  if [[ -z "$version" || "$version" == *"<InformationalVersion>"* ]]; then
    version="$(grep -m1 '<AssemblyVersion>' "$project_path" | sed -E 's/.*<AssemblyVersion>([^<]+)<\/AssemblyVersion>.*/\1/; s/\.0$//' || true)"
  fi

  if [[ -z "$version" || "$version" == *"<AssemblyVersion>"* ]]; then
    echo "Could not find Version, InformationalVersion, or AssemblyVersion in $project_path." >&2
    exit 1
  fi

  echo "$version"
}

package_version="$(project_version)"

package_platform_name() {
  case "$1" in
    win-x64) echo "windows-x64" ;;
    win-x86) echo "windows-x86" ;;
    win-arm64) echo "windows-arm64" ;;
    osx-x64) echo "macos-intel" ;;
    osx-arm64) echo "macos-arm64" ;;
    linux-x64) echo "linux-x64" ;;
    linux-arm64) echo "linux-arm64" ;;
    *) echo "$1" ;;
  esac
}

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
    Linux)
      echo "linux"
      ;;
    *)
      echo "Unsupported OS: $os. Use -windows, -macos, -linux, or -all explicitly." >&2
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

create_macos_app_bundle() {
  local output_path="$1"
  local rid="$2"
  local app_name="FFGUITool.app"
  local app_path="$publish_root/FFGUITool-$rid-app/$app_name"
  local contents_path="$app_path/Contents"
  local macos_path="$contents_path/MacOS"
  local resources_path="$contents_path/Resources"

  rm -rf "$publish_root/FFGUITool-$rid-app"
  mkdir -p "$macos_path" "$resources_path"
  cp -R "$output_path"/. "$macos_path/"

  if [[ -f "$script_dir/FFGUITool/Resources/AppIcon.icns" ]]; then
    cp "$script_dir/FFGUITool/Resources/AppIcon.icns" "$resources_path/FFGUITool.icns"
  fi

  cat > "$contents_path/Info.plist" <<PLIST
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
  <key>CFBundleName</key>
  <string>FFGUITool</string>
  <key>CFBundleDisplayName</key>
  <string>FFGUITool</string>
  <key>CFBundleIdentifier</key>
  <string>com.brealin.ffguitool</string>
  <key>CFBundleVersion</key>
  <string>$package_version</string>
  <key>CFBundleShortVersionString</key>
  <string>$package_version</string>
  <key>CFBundleExecutable</key>
  <string>FFGUITool</string>
  <key>CFBundlePackageType</key>
  <string>APPL</string>
  <key>CFBundleIconFile</key>
  <string>FFGUITool.icns</string>
  <key>NSHighResolutionCapable</key>
  <true/>
</dict>
</plist>
PLIST

  chmod +x "$macos_path/FFGUITool" 2>/dev/null || true
  echo "$app_path"
}

create_dmg_for_app() {
  local app_path="$1"
  local rid="$2"

  if [[ "$(uname -s)" != "Darwin" ]]; then
    echo "--dmg requires macOS because it uses hdiutil. Skipping $rid." >&2
    return
  fi

  if ! command -v hdiutil >/dev/null 2>&1; then
    echo "hdiutil was not found. Skipping DMG creation for $rid." >&2
    return
  fi

  mkdir -p "$dmg_root"
  local staging="$publish_root/FFGUITool-$rid-dmg"
  local package_platform
  package_platform="$(package_platform_name "$rid")"
  local dmg_path="$dmg_root/FFGUITool-v$package_version-$package_platform-Installer.dmg"

  rm -rf "$staging"
  rm -f "$dmg_path"
  mkdir -p "$staging"
  cp -R "$app_path" "$staging/"
  ln -s /Applications "$staging/Applications"

  hdiutil create \
    -volname "FFGUITool" \
    -srcfolder "$staging" \
    -ov \
    -format UDZO \
    "$dmg_path"
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

  local package_platform
  package_platform="$(package_platform_name "$rid")"
  archive_one "$output_path" "FFGUITool-v$package_version-$package_platform-Portable"

  if [[ "$create_dmg" == true && "$rid" == osx-* ]]; then
    app_path="$(create_macos_app_bundle "$output_path" "$rid")"
    create_dmg_for_app "$app_path" "$rid"
  fi
}

if [[ "$all" == true ]]; then
  targets=(win-x64 win-x86 win-arm64 osx-x64 osx-arm64 linux-x64 linux-arm64)
else
  group="${runtime:-$(current_platform_group)}"
  case "$group" in
    windows)
      targets=(win-x64 win-x86 win-arm64)
      ;;
    macos)
      targets=(osx-x64 osx-arm64)
      ;;
    linux)
      targets=(linux-x64 linux-arm64)
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
if [[ "$create_dmg" == true ]]; then
  echo "DMG outputs are in: $dmg_root"
fi
