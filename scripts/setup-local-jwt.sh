#!/usr/bin/env bash
set -euo pipefail

root="$(cd "$(dirname "$0")/.." && pwd)"
dir="$root/.local/jwt"
mkdir -p "$dir"

if [[ ! -f "$dir/private.pem" ]]; then
  openssl genrsa -out "$dir/private.pem" 2048
  openssl rsa -in "$dir/private.pem" -pubout -out "$dir/public.pem"
  chmod 600 "$dir/private.pem"
  echo "Par RSA de teste criado em $dir (não versionado)."
else
  echo "Par RSA já existe em $dir — mantido."
  if [[ ! -f "$dir/public.pem" ]]; then
    openssl rsa -in "$dir/private.pem" -pubout -out "$dir/public.pem"
  fi
fi

env_file="$root/.env"
if [[ ! -f "$env_file" ]]; then
  GCP_REGION="${GCP_REGION:-us-central1}"
  GCP_PROJECT_ID="${GCP_PROJECT_ID:-vcosta-fiap-tech-challenge}"
  GCP_AR_REPOSITORY="${GCP_AR_REPOSITORY:-tech-challenge}"
  IMAGE_TAG="${IMAGE_TAG:-latest}"

  cat >"$env_file" <<EOF
ASPNETCORE_ENVIRONMENT=Development

GCP_REGION=${GCP_REGION}
GCP_PROJECT_ID=${GCP_PROJECT_ID}
GCP_AR_REPOSITORY=${GCP_AR_REPOSITORY}
IMAGE_TAG=${IMAGE_TAG}

POSTGRES_USER=postgres
POSTGRES_PASSWORD=postgres
POSTGRES_DB=postgres

PUBSUB_PROJECT_ID=local-e2e
FIRESTORE_PROJECT_ID=local-e2e

SERVICE_AUTH_KEY=local-e2e-service-auth-key

JWT_ISSUER=autenticacao-service
JWT_AUDIENCE_FUNCIONARIO=funcionario
JWT_AUDIENCE_CLIENTE=cliente
JWT_KEY_ID=autenticacao-2026

MercadoPago__AccessToken=TEST-e2e-token
MercadoPago__WebhookSecret=e2e-webhook-secret
MercadoPago__ExpirationTime=P1D

DD_API_KEY=
DD_SITE=datadoghq.com
EOF
  echo "Criado $env_file"
else
  echo "$env_file já existe — mantido."
fi

echo "Use ./scripts/compose.sh up -d (exporta JWT_*.pem a partir de .local/jwt)."
echo "Antes: gcloud auth configure-docker \${GCP_REGION:-us-central1}-docker.pkg.dev"
