#!/bin/sh

set -e

if ! command -v go > /dev/null; then
	echo "Error: 'go' not found"
	exit 1
fi
if ! command -v bun > /dev/null; then
	echo "Error: 'bun' not found"
	exit 1
fi

pixelgrove_rm_ifdef() {
	if [ -n "$1" ]; then
		rm -f "$1" > /dev/null 2>&1 || true
	fi
}

pixelgrove_kill_ifdef() {
	if [ -n "$1" ]; then
		kill "$1" > /dev/null 2>&1 || true
	fi
}

pixelgrove_dev_cleanup() {
	pixelgrove_rm_ifdef $PIXELGROVE_PID_TMPFILE
	pixelgrove_kill_ifdef $PIXELGROVE_SERVER_PID
	pixelgrove_kill_ifdef $PIXELGROVE_WEBAPP_PID
	pixelgrove_kill_ifdef $PIXELGROVE_SERVER_TAIL_PID
	pixelgrove_kill_ifdef $PIXELGROVE_WEBAPP_TAIL_PID
}

trap pixelgrove_dev_cleanup EXIT
trap pixelgrove_dev_cleanup INT
trap pixelgrove_dev_cleanup TERM

PIXELGROVE_REPO_DIR="$(cd "$(dirname "$0")/.." && pwd -P)"
pushd "$PIXELGROVE_REPO_DIR" > /dev/null || {
	echo "Error: Could not change to repository directory '$PIXELGROVE_REPO_DIR'"
	exit 1
}

echo "Setting up Pixel Grove dev environment"

PIXELGROVE_SERVER_LOG="$PIXELGROVE_REPO_DIR/server_out.log"
PIXELGROVE_WEBAPP_LOG="$PIXELGROVE_REPO_DIR/webapp_out.log"

#
# server
#
go build -o ./bin/server ./cmd/server
PIXELGROVE_SERVER_PID=$(
	./bin/server > "$PIXELGROVE_SERVER_LOG" 2>&1 &
	echo "$!"
)
echo "Go server launched (PID: $PIXELGROVE_SERVER_PID) with output redirected to '$PIXELGROVE_SERVER_LOG'"

#
# webapp
#
pushd webapp > /dev/null || {
	echo "Error: Could not change to webapp directory"
	exit 1
}
PIXELGROVE_WEBAPP_PID=$(
	bun dev > "$PIXELGROVE_WEBAPP_LOG" 2>&1 &
	echo "$!"
)
echo "Bun dev server launched (PID: $PIXELGROVE_WEBAPP_PID) with output redirected to '$PIXELGROVE_WEBAPP_LOG'"
popd > /dev/null || {
	echo "Error: Could not change from webapp directory"
	exit 1
}

popd > /dev/null || {
	echo "Error: Could not change from repository directory"
	exit 1
}

echo
echo "Pixel Grove dev environment is running with main server at http://localhost:$(grep "^PIXELGROVE_PORT=" .env | cut -d"=" -f2-)/"
echo

PIXELGROVE_PID_TMPFILE=$(mktemp)
(tail -f "$PIXELGROVE_SERVER_LOG" & echo $! > "$PIXELGROVE_PID_TMPFILE") | sed 's/^/server: /' &
PIXELGROVE_SERVER_TAIL_PID=$(cat "$PIXELGROVE_PID_TMPFILE")
(tail -f "$PIXELGROVE_WEBAPP_LOG" & echo $! > "$PIXELGROVE_PID_TMPFILE") | sed 's/^/webapp: /' &
PIXELGROVE_WEBAPP_TAIL_PID=$(cat "$PIXELGROVE_PID_TMPFILE")
pixelgrove_rm_ifdef $PIXELGROVE_PID_TMPFILE

wait
