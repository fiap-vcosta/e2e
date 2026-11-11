#!/bin/bash
set -euo pipefail

psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" --dbname "$POSTGRES_DB" <<-EOSQL
  SELECT 'CREATE DATABASE administrativo'
  WHERE NOT EXISTS (SELECT FROM pg_database WHERE datname = 'administrativo')\gexec
  SELECT 'CREATE DATABASE estoque'
  WHERE NOT EXISTS (SELECT FROM pg_database WHERE datname = 'estoque')\gexec
  SELECT 'CREATE DATABASE ordem_servico'
  WHERE NOT EXISTS (SELECT FROM pg_database WHERE datname = 'ordem_servico')\gexec
EOSQL
