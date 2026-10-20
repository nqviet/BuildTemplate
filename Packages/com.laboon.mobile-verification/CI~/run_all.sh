#!/bin/sh
# Runs the Laboon mobile kit CI checks.
#   Packages/com.laboon.mobile-verification/CI~/run_all.sh [--icons scripts/mobile_icons.json] [--project-root .]
# Native probes run only on macOS with Xcode; elsewhere they are reported as skipped.
set -eu

CI_DIR=$(cd "$(dirname "$0")" && pwd)
PACKAGES_DIR=${LABOON_PACKAGES_DIR:-$(cd "$CI_DIR/../.." && pwd)}
PROJECT_ROOT=$(pwd)
ICONS=""
PYTHON=${PYTHON:-python3}

while [ $# -gt 0 ]; do
    case "$1" in
        --icons) ICONS="$2"; shift 2 ;;
        --project-root) PROJECT_ROOT="$2"; shift 2 ;;
        *) echo "Unknown option: $1" >&2; exit 2 ;;
    esac
done

echo "== Verification package tests"
"$PYTHON" -m unittest discover -s "$CI_DIR/tests" -p 'test_*.py'

IOS_CI="$PACKAGES_DIR/com.laboon.mobile-ios/CI~"
if [ -d "$IOS_CI/tests" ]; then
    echo "== iOS release verifier tests"
    "$PYTHON" -m unittest discover -s "$IOS_CI/tests" -p 'test_*.py'
fi

if [ -n "$ICONS" ]; then
    echo "== App icon guard"
    "$PYTHON" "$CI_DIR/verify_mobile_icons.py" --manifest "$ICONS" --project-root "$PROJECT_ROOT"
fi

echo "== Laboon mobile kit checks passed"
