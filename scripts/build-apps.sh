#!/usr/bin/env bash
# ==============================================================================
# build-apps.sh — Master Build & Installation Script for Antigravity Suite
# ==============================================================================
set -euo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
INSTALL_DIR="${HOME}/.local/bin"
DIST_WIN="${REPO_ROOT}/dist/windows"
APPS=("agyswitch" "agyproj" "agygit" "agydocker" "agyterm" "agyx")

mkdir -p "${INSTALL_DIR}"

usage() {
    cat << EOF
⚡ Antigravity Go Suite Build Script

Usage:
  ./scripts/build-apps.sh [target] [options]

Targets:
  all (default)       Build & install all 6 applications
  test                Run unit tests across all applications
  windows             Cross-compile all applications for Windows (.exe)
  <app_name>          Build a specific app:
                      agyswitch (or switch), agyproj (or proj),
                      agygit (or git), agydocker (or docker),
                      agyterm (or term), agyx (or proxy)

Options:
  --skip-tests        Skip running tests before building
  --output <dir>      Custom output directory (default: ~/.local/bin)

Examples:
  ./scripts/build-apps.sh
  ./scripts/build-apps.sh git
  ./scripts/build-apps.sh docker
  ./scripts/build-apps.sh windows
EOF
}

TARGET="${1:-all}"
SKIP_TESTS=false
CUSTOM_OUT=""

while [[ $# -gt 0 ]]; do
    case "$1" in
        --skip-tests)
            SKIP_TESTS=true
            shift
            ;;
        --output)
            CUSTOM_OUT="$2"
            shift 2
            ;;
        -h|--help|help)
            usage
            exit 0
            ;;
        *)
            TARGET="$1"
            shift
            ;;
    esac
done

if [[ -n "${CUSTOM_OUT}" ]]; then
    INSTALL_DIR="${CUSTOM_OUT}"
    mkdir -p "${INSTALL_DIR}"
fi

# Normalize alias names
case "${TARGET}" in
    switch) TARGET="agyswitch" ;;
    proj|project) TARGET="agyproj" ;;
    git) TARGET="agygit" ;;
    docker) TARGET="agydocker" ;;
    term) TARGET="agyterm" ;;
    proxy) TARGET="agyx" ;;
esac

build_app() {
    local app="$1"
    local app_dir="${REPO_ROOT}/apps/${app}"
    
    if [[ ! -d "${app_dir}" ]]; then
        echo "❌ App directory not found: ${app_dir}"
        return 1
    fi

    echo "📦 [${app}] Processing..."
    if [[ "${SKIP_TESTS}" != "true" ]]; then
        (cd "${app_dir}" && go test ./...)
    fi
    
    (cd "${app_dir}" && go build -o "${INSTALL_DIR}/${app}" .)
    echo "✔ Installed: ${INSTALL_DIR}/${app}"
}

build_windows() {
    mkdir -p "${DIST_WIN}"
    for app in "${APPS[@]}"; do
        local app_dir="${REPO_ROOT}/apps/${app}"
        echo "🪟 Cross-compiling ${app} -> ${DIST_WIN}/${app}.exe"
        (cd "${app_dir}" && GOOS=windows GOARCH=amd64 go build -o "${DIST_WIN}/${app}.exe" .)
    done
    echo "✔ All Windows .exe binaries ready in ${DIST_WIN}"
}

if [[ "${TARGET}" == "test" ]]; then
    for app in "${APPS[@]}"; do
        echo "🧪 Testing ${app}..."
        (cd "${REPO_ROOT}/apps/${app}" && go test -v ./...)
    done
    echo "✔ All test suites passed!"
    exit 0
elif [[ "${TARGET}" == "windows" ]]; then
    build_windows
    exit 0
elif [[ "${TARGET}" == "all" ]]; then
    echo "🚀 Building and installing all 6 Antigravity Suite applications..."
    for app in "${APPS[@]}"; do
        build_app "${app}"
    done
    echo "🎉 All applications installed successfully in ${INSTALL_DIR}!"
    exit 0
else
    build_app "${TARGET}"
    exit 0
fi
