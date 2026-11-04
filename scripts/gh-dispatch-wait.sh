#!/usr/bin/env bash
set -euo pipefail

usage() {
  echo "usage: $0 <repo> <workflow-file>" >&2
  exit 2
}

[[ $# -eq 2 ]] || usage

repo="$1"
workflow="$2"
org="${ORG:-fiap-vcosta}"
full="${org}/${repo}"
poll_seconds="${POLL_SECONDS:-20}"

before="$(gh run list --repo "$full" --workflow "$workflow" --limit 1 --json databaseId --jq '.[0].databaseId // empty')"

gh workflow run "$workflow" --repo "$full"

run_id=""
# ~10 min para o run aparecer na API
attempts=$(( 600 / poll_seconds ))
for _ in $(seq 1 "$attempts"); do
  candidate="$(gh run list --repo "$full" --workflow "$workflow" --limit 1 --json databaseId --jq '.[0].databaseId // empty')"
  if [[ -n "$candidate" && "$candidate" != "$before" ]]; then
    run_id="$candidate"
    break
  fi
  sleep "$poll_seconds"
done

if [[ -z "$run_id" ]]; then
  echo "não apareceu run novo para ${full} / ${workflow}" >&2
  exit 1
fi

echo "watching ${full} ${workflow} run ${run_id} (interval ${poll_seconds}s)"
gh run watch "$run_id" --repo "$full" --exit-status --interval "$poll_seconds"
