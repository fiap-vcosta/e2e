#!/bin/sh
set -eu

# Emulator HTTP API (mais estável que `gcloud pubsub` neste container).
HOST="${PUBSUB_EMULATOR_HOST:-pubsub:8085}"
PROJECT="${PUBSUB_PROJECT_ID:-local-e2e}"
BASE="http://${HOST}/v1/projects/${PROJECT}"

echo "Waiting for Pub/Sub emulator at ${HOST}..."
i=0
while [ "$i" -lt 60 ]; do
  if curl -fsS "${BASE}/topics" >/dev/null 2>&1; then
    break
  fi
  i=$((i + 1))
  sleep 1
done
curl -fsS "${BASE}/topics" >/dev/null

create_topic() {
  topic="$1"
  code="$(curl -s -o /dev/null -w '%{http_code}' -X PUT "${BASE}/topics/${topic}" || true)"
  case "$code" in
    200|409) ;;
    *)
      echo "failed to create topic ${topic}: HTTP ${code}" >&2
      exit 1
      ;;
  esac
}

create_subscription() {
  subscription="$1"
  topic="$2"
  code="$(curl -s -o /dev/null -w '%{http_code}' -X PUT "${BASE}/subscriptions/${subscription}" \
    -H 'Content-Type: application/json' \
    -d "{\"topic\":\"projects/${PROJECT}/topics/${topic}\"}" || true)"
  case "$code" in
    200|409) ;;
    *)
      echo "failed to create subscription ${subscription}: HTTP ${code}" >&2
      exit 1
      ;;
  esac
}

for topic in \
  diagnostico-finalizado \
  orcamento-gerado \
  orcamento-falhou \
  ordem-servico-aprovada \
  estoque-reservado \
  estoque-em-falta \
  ordem-servico-finalizada \
  cobranca-gerada \
  pagamento-confirmado \
  pagamento-recusado \
  ordem-servico-descartada \
  notificacoes
do
  create_topic "$topic"
done

create_subscription orcamento-diagnostico-finalizado diagnostico-finalizado
create_subscription orcamento-ordem-servico-finalizada ordem-servico-finalizada
create_subscription orcamento-ordem-servico-descartada ordem-servico-descartada

create_subscription estoque-ordem-servico-aprovada ordem-servico-aprovada
create_subscription estoque-ordem-servico-finalizada ordem-servico-finalizada
create_subscription estoque-ordem-servico-descartada ordem-servico-descartada

create_subscription ordem-servico-orcamento-gerado orcamento-gerado
create_subscription ordem-servico-orcamento-falhou orcamento-falhou
create_subscription ordem-servico-estoque-reservado estoque-reservado
create_subscription ordem-servico-estoque-em-falta estoque-em-falta
create_subscription ordem-servico-cobranca-gerada cobranca-gerada
create_subscription ordem-servico-pagamento-confirmado pagamento-confirmado
create_subscription ordem-servico-pagamento-recusado pagamento-recusado

echo "Pub/Sub emulator topics and subscriptions ready."
