#!/usr/bin/env bash
# build.sh — Compile SpaceMouseBridge native plugin for Unity on macOS
# Run from the NativePlugin/ directory or from the project root.
#
# Prerequisites:
#   - Xcode Command Line Tools (xcode-select --install)
#   - 3DxWareMac driver installed (provides 3DconnexionClient.framework)
#
# Output: ../Assets/Plugins/macOS/libSpaceMouseBridge.dylib (universal binary)

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
SOURCE="$SCRIPT_DIR/SpaceMouseBridge.c"
OUTPUT_DIR="$SCRIPT_DIR/../Assets/Plugins/macOS"
OUTPUT="$OUTPUT_DIR/libSpaceMouseBridge.dylib"

# Verify framework is installed
FRAMEWORK="/Library/Frameworks/3DconnexionClient.framework"
if [ ! -d "$FRAMEWORK" ]; then
    echo "ERROR: $FRAMEWORK not found."
    echo "Install 3DxWareMac from https://3dconnexion.com/us/drivers/"
    exit 1
fi

mkdir -p "$OUTPUT_DIR"

echo "Compiling SpaceMouseBridge (arm64 + x86_64 universal)..."

clang \
    -arch arm64 -arch x86_64 \
    -F/Library/Frameworks \
    -weak_framework 3DconnexionClient \
    -dynamiclib \
    -install_name "@loader_path/libSpaceMouseBridge.dylib" \
    -fvisibility=hidden \
    -O2 \
    -o "$OUTPUT" \
    "$SOURCE"

echo "Ad-hoc code signing..."
codesign --sign - --force "$OUTPUT"

echo ""
echo "Done! Output: $OUTPUT"
echo ""
file "$OUTPUT"
echo ""
echo "Next steps:"
echo "  1. Create a Unity 6 project in the SpaceMouseProDriver folder (via Unity Hub)"
echo "  2. Unity will pick up Assets/Plugins/macOS/libSpaceMouseBridge.dylib automatically"
echo "  3. Select the dylib in the Project view and ensure 'Editor' platform is checked"
echo "  4. Open Edit > Preferences > SpaceMouse Pro to verify device status"
