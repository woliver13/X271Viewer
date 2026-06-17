#!/usr/bin/env bash
# build-msix.sh — publish X12Viewer.Wpf and pack it as a per-user MSIX.
#
# Usage:
#   ./installer/build-msix.sh                                # unsigned
#   ./installer/build-msix.sh --thumbprint <SHA1>            # sign with installed cert
#   ./installer/build-msix.sh --pfx cert.pfx --pfx-pass s3  # sign with PFX
#
# Output: installer/out/X12Viewer_<version>.msix
#
# Prerequisites:
#   • .NET 9 SDK
#   • Windows 10/11 SDK  (makeappx.exe, signtool.exe)
#   • Python 3 in PATH   (for placeholder asset generation)
#
# Per-user install:  Add-AppxPackage ./installer/out/X12Viewer_*.msix
#   (Run in PowerShell after installing a trusted signing cert — see new-dev-cert.sh)

set -euo pipefail

# ── Parse arguments ────────────────────────────────────────────────────────────

THUMBPRINT=""
PFX_PATH=""
PFX_PASS=""

while [[ $# -gt 0 ]]; do
    case "$1" in
        --thumbprint) THUMBPRINT="$2"; shift 2 ;;
        --pfx)        PFX_PATH="$2";   shift 2 ;;
        --pfx-pass)   PFX_PASS="$2";   shift 2 ;;
        *) echo "Unknown option: $1"; exit 1 ;;
    esac
done

# ── Paths ─────────────────────────────────────────────────────────────────────

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"

WPF_PROJ="$REPO_ROOT/src/X12Viewer.Wpf/X12Viewer.Wpf.csproj"
BUILD_PROPS="$REPO_ROOT/Directory.Build.props"
MANIFEST_SRC="$SCRIPT_DIR/AppxManifest.xml"
ASSETS_SRC="$SCRIPT_DIR/Assets"
OUT_DIR="$SCRIPT_DIR/out"
LAYOUT_DIR="$OUT_DIR/layout"
PUBLISH_DIR="$OUT_DIR/publish"

# ── Version ────────────────────────────────────────────────────────────────────

VERSION=$(python3 -c "
import xml.etree.ElementTree as ET
tree = ET.parse(r'$(cygpath -w "$BUILD_PROPS")')
print(tree.find('.//{*}FileVersion').text)
")
echo "Version: $VERSION"

# ── Find Windows SDK tools ─────────────────────────────────────────────────────

find_sdk_tool() {
    local name="$1"
    # Only search directories that exist — find exits 1 on missing dirs and
    # pipefail would abort the script if any candidate path is absent.
    local candidates=(
        "/c/Program Files (x86)/Windows Kits/10/bin"
        "/c/Program Files/Windows Kits/10/bin"
        "/c/Program Files (x86)/Windows Kits/10/App Certification Kit"
        "/c/Program Files/Windows Kits/10/App Certification Kit"
        "/c/Program Files (x86)/Microsoft Visual Studio"
        "/c/Program Files/Microsoft Visual Studio"
    )
    for dir in "${candidates[@]}"; do
        if [[ -d "$dir" ]]; then
            find "$dir" -name "$name" 2>/dev/null
        fi
    done | sort -V | tail -1
}

MAKEAPPX=$(find_sdk_tool "makeappx.exe")
if [[ -z "$MAKEAPPX" ]]; then
    echo "ERROR: makeappx.exe not found."
    echo "Install the Windows 10/11 SDK: https://developer.microsoft.com/windows/downloads/windows-sdk/"
    exit 1
fi
echo "makeappx: $MAKEAPPX"

SIGNTOOL=$(find_sdk_tool "signtool.exe")

# ── Publish ────────────────────────────────────────────────────────────────────

echo ""
echo "Publishing WPF app..."
rm -rf "$PUBLISH_DIR"

dotnet publish "$WPF_PROJ" \
    --configuration Release \
    --runtime win-x64 \
    --self-contained \
    --output "$PUBLISH_DIR" \
    -p:PublishSingleFile=false \
    -p:GenerateDocumentationFile=false

# ── Assemble layout ────────────────────────────────────────────────────────────

echo ""
echo "Assembling package layout..."
rm -rf "$LAYOUT_DIR"
mkdir -p "$LAYOUT_DIR"

# Copy published app files
cp -r "$PUBLISH_DIR"/. "$LAYOUT_DIR/"

# Substitute version into AppxManifest.xml
sed "s/__VERSION__/$VERSION/g" "$MANIFEST_SRC" > "$LAYOUT_DIR/AppxManifest.xml"

# Assets — copy real ones from installer/Assets/ or generate placeholders
mkdir -p "$LAYOUT_DIR/Assets"

declare -A ASSET_SIZES=(
    ["Square44x44Logo.png"]="44 44"
    ["Square150x150Logo.png"]="150 150"
    ["Wide310x150Logo.png"]="310 150"
    ["StoreLogo.png"]="50 50"
)

for asset in "${!ASSET_SIZES[@]}"; do
    src="$ASSETS_SRC/$asset"
    dest="$LAYOUT_DIR/Assets/$asset"
    if [[ -f "$src" ]]; then
        cp "$src" "$dest"
    else
        echo "WARNING: $asset not found in installer/Assets/ — generating placeholder."
        read -r w h <<< "${ASSET_SIZES[$asset]}"
        python3 - "$dest" "$w" "$h" <<'PYEOF'
import sys, struct, zlib

path, w, h = sys.argv[1], int(sys.argv[2]), int(sys.argv[3])

def png_chunk(tag, data):
    crc = zlib.crc32(tag + data) & 0xFFFFFFFF
    return struct.pack('>I', len(data)) + tag + data + struct.pack('>I', crc)

# Steel-blue fill (0x2E7DB2)
r, g, b = 0x2E, 0x7D, 0xB2
row = b'\x00' + bytes([r, g, b] * w)      # filter byte 0 (None) + RGB pixels
raw = row * h
idat = zlib.compress(raw, 9)

ihdr = struct.pack('>IIBBBBB', w, h, 8, 2, 0, 0, 0)  # 8-bit RGB
png = (b'\x89PNG\r\n\x1a\n'
       + png_chunk(b'IHDR', ihdr)
       + png_chunk(b'IDAT', idat)
       + png_chunk(b'IEND', b''))

with open(path, 'wb') as f:
    f.write(png)
PYEOF
    fi
done

# ── Pack ───────────────────────────────────────────────────────────────────────

MSIX_NAME="X12Viewer_${VERSION}.msix"
MSIX_PATH="$OUT_DIR/$MSIX_NAME"
rm -f "$MSIX_PATH"

echo ""
echo "Packing MSIX..."
MSYS_NO_PATHCONV=1 "$MAKEAPPX" pack /d "$(cygpath -w "$LAYOUT_DIR")" /p "$(cygpath -w "$MSIX_PATH")" /nv /o

# ── Sign (optional) ────────────────────────────────────────────────────────────

if [[ -n "$THUMBPRINT" || -n "$PFX_PATH" ]]; then
    if [[ -z "$SIGNTOOL" ]]; then
        echo "ERROR: signtool.exe not found in Windows SDK."
        exit 1
    fi

    echo ""
    echo "Signing MSIX..."
    SIGN_ARGS=(sign /fd SHA256 /td SHA256 /tr http://timestamp.digicert.com)

    if [[ -n "$THUMBPRINT" ]]; then
        SIGN_ARGS+=(/sha1 "$THUMBPRINT")
    else
        SIGN_ARGS+=(/f "$(cygpath -w "$PFX_PATH")")
        if [[ -n "$PFX_PASS" ]]; then
            SIGN_ARGS+=(/p "$PFX_PASS")
        fi
    fi

    SIGN_ARGS+=("$(cygpath -w "$MSIX_PATH")")
    MSYS_NO_PATHCONV=1 "$SIGNTOOL" "${SIGN_ARGS[@]}"
    echo "Signed."
else
    echo ""
    echo "WARNING: MSIX is unsigned. To sideload it:"
    echo "  1. Run ./installer/new-dev-cert.sh (elevated) to create and trust a dev cert."
    echo "  2. Re-run: ./installer/build-msix.sh --thumbprint <thumbprint>"
    echo "  3. Install (PowerShell): Add-AppxPackage ./installer/out/$MSIX_NAME"
fi

echo ""
echo "Done: $MSIX_PATH"
