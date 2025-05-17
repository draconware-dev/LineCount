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

if [ -z "$SOURCE" ]; then
    if [ "$DISTRO" = "alpine" ]; then
        SOURCE="https://github.com/draconware-dev/LineCount/releases/download/__VERSION__/linecount-__VERSION__-linux-alpine-amd64.tar.xz"
    else
        SOURCE="https://github.com/draconware-dev/LineCount/releases/download/__VERSION__/linecount-__VERSION__-linux-amd64.tar.xz"
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

hasInstalledWebClient=0

installWebClient()
{
    case "$DISTRO" in
        "ubuntu" | "debian")
                echo "Temporarily installing wget..."
                apt-get update
                apt-get install -y wget
            ;;
        "centos" | "rhel" | "fedora")
                echo "Temporarily installing wget..."
                yum install -y wget
                dnf install -y wget
            ;;
        "arch" | "manjaro")
                echo "Temporarily installing wget..."
                pacman -Syu --noconfirm wget 
            ;;
        "alpine")
                echo "Temporarily installing wget..."
                apk add --no-cache wget
            ;;
        *)
                echo "Failed to install wget. Please install wget or curl manually as they constitute a requirement for the installation process."
                exit 2
            ;;
    esac

    hasInstalledWebClient=1
}

uninstallWebClient()
{
    case "$DISTRO" in
        "ubuntu" | "debian")
                echo "Uninstalling wget..."
                apt-get remove --purge -y wget
                apt-get autoremove -y
            ;;
        "centos" | "rhel" | "fedora")
                echo "Uninstalling wget..."
                yum remove -y wget
                dnf remove -y wget
            ;;
        "arch" | "manjaro")
                echo "Uninstalling wget..."
                pacman -Rns --noconfirm wget
            ;;
        "alpine")
                echo "Uninstalling wget..."
                apk del wget
            ;;
        *)
            ;;
    esac
}

fileName=$(basename "$SOURCE")

downloadProgram

if [ $? -ne 0 ]; then
    installWebClient
    downloadProgram
fi

if [ $hasInstalledWebClient -ne 0 ]; then
    uninstallWebClient
fi

mkdir -p "$INSTALLATION_PATH"
mkdir -p .linecount
tar -xf $fileName -C ".linecount"
cp .linecount/linecount "$INSTALLATION_PATH/linecount"
chmod +x "$INSTALLATION_PATH/linecount"

rm -rf .linecount
rm -f "$fileName"

echo "Installation complete."