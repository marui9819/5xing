#!/usr/bin/env bash
set -euo pipefail

if [[ "$(uname)" != "Darwin" ]]; then
  echo "该脚本仅能在 macOS 上运行。" >&2
  exit 1
fi

if [[ $# -lt 2 ]]; then
  echo "用法: $0 <App路径> <输出DMG路径> [卷标名]" >&2
  exit 1
fi

APP_PATH="$1"
OUTPUT_DMG="$2"
VOL_NAME="${3:-Privacy Metadata Cleaner}"

if [[ ! -d "$APP_PATH" ]]; then
  echo "未找到应用目录: $APP_PATH" >&2
  exit 1
fi

ICON_PATH="artifacts/dmg-assets/PrivacyMetadataCleaner.icns"
BACKGROUND_PATH="artifacts/dmg-assets/dmg-background.png"

if [[ ! -f "$ICON_PATH" ]]; then
  echo "警告: 未找到 $ICON_PATH，将使用默认卷标图标。" >&2
fi

if [[ ! -f "$BACKGROUND_PATH" ]]; then
  echo "警告: 未找到 $BACKGROUND_PATH，DMG 将不包含自定义背景。" >&2
fi

WORK_DIR=$(mktemp -d)
DMG_DIR="$WORK_DIR/mount"
APP_NAME=$(basename "$APP_PATH")

mkdir -p "$DMG_DIR"

hdiutil create -size 300m -fs HFS+ -volname "$VOL_NAME" "$WORK_DIR/base.dmg"
ATTACH_OUTPUT=$(hdiutil attach "$WORK_DIR/base.dmg" -nobrowse -noverify)
MOUNT_POINT=$(echo "$ATTACH_OUTPUT" | tail -n1 | awk '{print $3}')

trap 'hdiutil detach "$MOUNT_POINT" -force >/dev/null 2>&1 || true; rm -rf "$WORK_DIR"' EXIT

cp -R "$APP_PATH" "$MOUNT_POINT/"
ln -s /Applications "$MOUNT_POINT/Applications"

if [[ -f "$BACKGROUND_PATH" ]]; then
  mkdir -p "$MOUNT_POINT/.background"
  cp "$BACKGROUND_PATH" "$MOUNT_POINT/.background/background.png"
fi

if [[ -f "$ICON_PATH" ]]; then
  cp "$ICON_PATH" "$MOUNT_POINT/.VolumeIcon.icns"
  /usr/bin/SetFile -a C "$MOUNT_POINT"
fi

/usr/bin/SetFile -a B "$MOUNT_POINT/$APP_NAME" || true

/usr/bin/osascript <<APPLESCRIPT
set volumeName to "$VOL_NAME"
tell application "Finder"
    tell disk volumeName
        open
        set current view of container window to icon view
        set toolbar visible of container window to false
        set status bar visible of container window to false
        set the bounds of container window to {100, 100, 840, 540}
        delay 0.5
        set viewOptions to the icon view options of container window
        set icon size of viewOptions to 128
        set arrangement of viewOptions to not arranged
        if "$BACKGROUND_PATH" is not "" then
            set background picture of viewOptions to file ".background:background.png"
        end if
        set position of item "$APP_NAME" to {160, 280}
        set position of item "Applications" to {520, 280}
        close
        open
        delay 0.5
        update without registering applications
        delay 0.5
    end tell
end tell
APPLESCRIPT

hdiutil detach "$MOUNT_POINT" -quiet

hdiutil convert "$WORK_DIR/base.dmg" -format UDZO -imagekey zlib-level=9 -o "$OUTPUT_DMG"

if [[ -n "${CODESIGN_IDENTITY:-}" ]]; then
  codesign --force --sign "$CODESIGN_IDENTITY" "$OUTPUT_DMG"
fi

if [[ -n "${NOTARIZE_PROFILE:-}" ]]; then
  xcrun notarytool submit "$OUTPUT_DMG" --keychain-profile "$NOTARIZE_PROFILE" --wait
  xcrun stapler staple "$OUTPUT_DMG"
fi

echo "已生成 DMG: $OUTPUT_DMG"
