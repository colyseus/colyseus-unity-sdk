#!/bin/bash
set -e

NATIVEWEBSOCKET_BRANCH="upm-2.0"
NATIVEWEBSOCKET_REPO="https://github.com/endel/NativeWebSocket.git"
NATIVEWEBSOCKET_DIR="Assets/Colyseus/Runtime/WebSocket"

TMPDIR=$(mktemp -d)
trap "rm -rf $TMPDIR" EXIT

git clone --branch "$NATIVEWEBSOCKET_BRANCH" --depth 1 "$NATIVEWEBSOCKET_REPO" "$TMPDIR"

rm -rf "$NATIVEWEBSOCKET_DIR"
cp -r "$TMPDIR/WebSocket" "$NATIVEWEBSOCKET_DIR"
