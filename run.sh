#!/usr/bin/env bash
# Run encrypted tic-tac-toe via Docker.
# Usage:
#   ./run.sh host [extra host args...]      # Player X, publishes TCP 10000
#   ./run.sh client [server] [extra args...] # Player O, default server: host.docker.internal
#   ./run.sh selftest [extra args...]       # crypto round-trip check, no network
set -euo pipefail
cd "$(dirname "$0")"
IMAGE="${IMAGE:-encrypted-tic-tac-toe}"

usage() {
  echo "Usage: $0 {host|client [server]|selftest} [args...]" >&2
  exit 1
}

cmd="${1:-}"
case "$cmd" in
  host)
    shift
    docker run --rm -it -p 10000:10000 --name tictactoe-host "$IMAGE" "$@"
    ;;
  client)
    shift
    server="host.docker.internal"
    if [ $# -gt 0 ]; then server="$1"; shift; fi
    docker run --rm -it -e ROLE=client --name tictactoe-client "$IMAGE" "$server" "$@"
    ;;
  selftest)
    shift
    docker run --rm "$IMAGE" --selftest "$@"
    ;;
  *)
    usage
    ;;
esac
