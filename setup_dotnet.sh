#!/bin/bash
set -e
DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
BASE_URL="https://mirrors.nxtgen.com/ubuntu-mirror/ubuntu/pool/main/d/dotnet8"
DEB_DIR="$DIR/dotnet_debs"
TARGET_DIR="$DIR/dotnet"

mkdir -p "$DEB_DIR"
mkdir -p "$TARGET_DIR"

PACKAGES=(
    "dotnet-host-8.0_8.0.31-0ubuntu1~24.04.1_amd64.deb"
    "dotnet-hostfxr-8.0_8.0.31-0ubuntu1~24.04.1_amd64.deb"
    "dotnet-runtime-8.0_8.0.31-0ubuntu1~24.04.1_amd64.deb"
    "aspnetcore-runtime-8.0_8.0.31-0ubuntu1~24.04.1_amd64.deb"
    "dotnet-targeting-pack-8.0_8.0.31-0ubuntu1~24.04.1_amd64.deb"
    "aspnetcore-targeting-pack-8.0_8.0.31-0ubuntu1~24.04.1_amd64.deb"
    "dotnet-apphost-pack-8.0_8.0.31-0ubuntu1~24.04.1_amd64.deb"
    "netstandard-targeting-pack-2.1-8.0_8.0.131-0ubuntu1~24.04.1_amd64.deb"
    "dotnet-templates-8.0_8.0.131-0ubuntu1~24.04.1_amd64.deb"
    "dotnet-sdk-8.0_8.0.131-0ubuntu1~24.04.1_amd64.deb"
)

for pkg in "${PACKAGES[@]}"; do
    if [ ! -f "$DEB_DIR/$pkg" ]; then
        echo "Downloading $pkg..."
        curl -k -sSL "$BASE_URL/$pkg" -o "$DEB_DIR/$pkg"
    fi
    echo "Extracting $pkg..."
    dpkg -x "$DEB_DIR/$pkg" "$TARGET_DIR"
done

echo "Dotnet packages extracted successfully."
