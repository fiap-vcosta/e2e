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

before="$(gh run list --repo "$full" --workflow "$workflow" --limit 1 --json databaseId --jq '.[0].databaseId // empty')"

gh workflow run "$workflow" --repo "$full"

run_id=""
for _ in $(seq 1 60); do
  candidate="$(gh run list --repo "$full" --workflow "$workflow" --limit 1 --json databaseId --jq '.[0].databaseId // empty')"
  if [[ -n "$candidate" && "$candidate" != "$before" ]]; then
    run_id="$candidate"
    break
  fi
  sleep 2
done

if [[ -z "$run_id" ]]; then
  echo "não apareceu run novo para ${full} / ${workflow}" >&2
  exit 1
fi

echo "watching ${full} ${workflow} run ${run_id}"
gh run watch "$run_id" --repo "$full" --exit-status
