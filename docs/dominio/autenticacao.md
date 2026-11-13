# Domínio — `autenticacao-service`

Firestore database nomeado `autenticacao` ([ADR 002](../adrs/002-firestore-databases-nomeados.md)). Emissor RS256; demais serviços validam via JWKS público.

## Linguagem onipresente

* **Usuário staff:** login + senha (hash) + tipo (`Admin`, e tipos legados se existirem no seed).
* **JWT staff:** claims de identidade de funcionário; obtido em `POST /autenticacao/login`.
* **JWT cliente:** emitido após consultar o cliente no administrativo (`/system/administrativo/…`) pelo **documento**; claim `documento`; expiração 1800s.
* **JWKS:** chaves públicas em `/autenticacao/.well-known/jwks.json`.
* **Service auth (malha):** header `X-Service-Key` compartilhado — **não** é JWT de serviço (escopo atual).

Não publica eventos Pub/Sub. Não persiste JWT de cliente.

## Modelo de dados (coleções)

| Coleção | Doc id | Campos |
|---------|--------|--------|
| `usuarios` | `login` (lower) | `id`, `login`, `senhaHash`, `tipoUsuario` |

Seed de demo: usuário `admin` / `admin`.
