#!/usr/bin/env bash
# Build the encrypted-tic-tac-toe Docker image.
# Usage: ./build.sh [image-name]
set -euo pipefail
cd "$(dirname "$0")"
IMAGE="${1:-${IMAGE:-encrypted-tic-tac-toe}}"
docker build -t "$IMAGE" .
echo "Built $IMAGE"
