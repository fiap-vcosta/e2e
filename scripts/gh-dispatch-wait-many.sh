#!/usr/bin/env bash
set -euo pipefail

usage() {
  echo "usage: $0 <workflow-file> <repo> [repo...]" >&2
  exit 2
}

[[ $# -ge 2 ]] || usage

workflow="$1"
shift
script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

pids=()
repos=()
for repo in "$@"; do
  repos+=("$repo")
  "$script_dir/gh-dispatch-wait.sh" "$repo" "$workflow" &
  pids+=("$!")
done

status=0
for i in "${!pids[@]}"; do
  if ! wait "${pids[$i]}"; then
    echo "falhou: ${repos[$i]} / ${workflow}" >&2
    status=1
  fi
done

exit "$status"
