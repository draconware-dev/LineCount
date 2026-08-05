#!/bin/sh

# $INSTALLATION_PATH
# SOURCE

set -e

if [ "$(id -u)" -ne 0 ]; then
    echo "Installation requires root privileges. Please execute this script as root (sudo)."
    exit 1
fi

INSTALLATION_PATH="${INSTALLATION_PATH%/}"

if [ -z "$INSTALLATION_PATH" ]; then
    INSTALLATION_PATH="/bin"
fi

determineDistro()
{
    . /etc/os-release
    DISTRO=$ID
}

determineDistro

case "$(uname -m)" in
    "x86_64" | "amd64")
        ARCH="amd64"
        ;;
    "aarch64" | "arm64")
        ARCH="arm64"
        ;;
    "i386" | "i686")
        ARCH="x86"
        ;;
    *)
        echo "Unsupported architecture."
        exit 1
        ;;
esac

if [ -z "$SOURCE" ]; then
    if [ "$DISTRO" = "alpine" ]; then
        SOURCE="https://github.com/draconware-dev/LoC/releases/download/__VERSION__/loc-__VERSION__-linux-alpine-$ARCH.tar.xz"
    else
        SOURCE="https://github.com/draconware-dev/LoC/releases/download/__VERSION__/loc-__VERSION__-linux-$ARCH.tar.xz"
    fi
fi

downloadProgram()
{
    if command -v wget >/dev/null 2>&1; then
        echo "Downloading $fileName..."
        wget -q --show-progress -O $fileName "$SOURCE"
        return 0
    else
        if command -v curl >/dev/null 2>&1; then
            curl -o $fileName "$SOURCE"
            return 0
        fi
    fi

    return 1
}

fileName=$(basename "$SOURCE")

downloadProgram

if [ $? -ne 0 ]; then
    echo "Either wget or curl must be installed for installation to proceed."
    exit 1
fi

mkdir -p "$INSTALLATION_PATH"
mkdir -p .loc
tar -xf $fileName -C ".loc"
cp .loc/loc "$INSTALLATION_PATH/loc"
chmod +x "$INSTALLATION_PATH/loc"

rm -rf .loc
rm -f "$fileName"

echo "Installation complete."
