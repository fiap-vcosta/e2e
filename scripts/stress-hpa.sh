#!/usr/bin/env bash
# Gera carga HTTP na listagem de OS (JWT staff) para exercitar o HPA do ordem-servico-service.
# Login no autenticacao-service; listagem no ordem-servico-service.
# Pré-requisito: algumas OS no banco melhoram o custo por request.
#
# Uso (mesmo host — GCP / Gateway):
#   ./scripts/stress-hpa.sh <base_url> [duração_segundos] [concorrência]
#   ./scripts/stress-hpa.sh https://vcosta-fiap.online 120 25
#
# Compose local (portas distintas — e2e README):
#   AUTH_BASE_URL=http://localhost:8081 OS_BASE_URL=http://localhost:8083 \
#     ./scripts/stress-hpa.sh unused 60 10
#
# Em outro terminal (cluster da janela):
#   watch -n 2 kubectl get hpa,pods -n tech-challenge
set -euo pipefail

BASE_URL="${1:-${BASE_URL:-https://vcosta-fiap.online}}"
DURATION="${2:-120}"
CONCURRENCY="${3:-25}"
AUTH_BASE_URL="${AUTH_BASE_URL:-${BASE_URL}}"
OS_BASE_URL="${OS_BASE_URL:-${BASE_URL}}"
LOGIN="${LOGIN:-admin}"
SENHA="${SENHA:-${PASSWORD:-admin}}"
NAMESPACE="${NAMESPACE:-tech-challenge}"

echo "Login em ${AUTH_BASE_URL}/autenticacao/login ..."
TOKEN="$(curl -fsS -X POST "${AUTH_BASE_URL}/autenticacao/login" \
  -H 'Content-Type: application/json' \
  -d "{\"login\":\"${LOGIN}\",\"senha\":\"${SENHA}\"}" \
  | sed -n 's/.*"token"[[:space:]]*:[[:space:]]*"\([^"]*\)".*/\1/p')"

if [[ -z "${TOKEN}" ]]; then
  echo "Erro: não foi possível obter o token JWT (login ${LOGIN})." >&2
  exit 1
fi

URL="${OS_BASE_URL}/ordem-servico"
echo "Stress: url=${URL} duration=${DURATION}s concurrency=${CONCURRENCY}"
echo "Em outro terminal: watch -n 2 kubectl get hpa,pods -n ${NAMESPACE}"

if command -v hey >/dev/null 2>&1; then
  hey -z "${DURATION}s" -c "${CONCURRENCY}" -H "Authorization: Bearer ${TOKEN}" "${URL}"
  exit 0
fi

echo "hey não encontrado — usando loop curl."
end=$((SECONDS + DURATION))
while (( SECONDS < end )); do
  for ((i = 0; i < CONCURRENCY; i++)); do
    curl -s -o /dev/null -H "Authorization: Bearer ${TOKEN}" "${URL}" &
  done
  wait
done
echo "Stress finalizado."
