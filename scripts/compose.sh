#!/usr/bin/env bash
set -euo pipefail

root="$(cd "$(dirname "$0")/.." && pwd)"
cd "$root"

if [[ ! -f .env ]]; then
  echo "Falta .env — rode ./scripts/setup-local-jwt.sh primeiro." >&2
  exit 1
fi

if [[ ! -f .local/jwt/private.pem || ! -f .local/jwt/public.pem ]]; then
  echo "Faltam chaves em .local/jwt — rode ./scripts/setup-local-jwt.sh primeiro." >&2
  exit 1
fi

# PEM com newlines reais (ImportFromPem / JwtIssuer). Não coloque o corpo no .env.
export JWT_SIGNING_KEY_PEM
export JWT_PUBLIC_KEY_PEM
JWT_SIGNING_KEY_PEM="$(cat .local/jwt/private.pem)"
JWT_PUBLIC_KEY_PEM="$(cat .local/jwt/public.pem)"

exec docker compose --env-file .env "$@"
