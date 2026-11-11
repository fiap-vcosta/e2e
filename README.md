# e2e

Compose do sistema, testes de ponta a ponta e collections HTTP. Este repositório não contém código de serviço. Org [fiap-vcosta](https://github.com/fiap-vcosta).

Contrato entre os serviços:

- [Eventos](docs/eventos.md)
- [Saga](docs/saga.md)
- [HTTP](docs/contratos-http.md)

## Compose local (imagens do Artifact Registry)

Sobe os cinco serviços com **imagens já publicadas** no Artifact Registry, mais Postgres (3 databases), emuladores Firestore/Pub/Sub e WireMock (fake do Mercado Pago Orders — P14).

| Host | Serviço |
|------|---------|
| `:8080` | administrativo-service |
| `:8081` | autenticacao-service |
| `:8082` | estoque-service |
| `:8083` | ordem-servico-service |
| `:8084` | orcamento-service |
| `:8085` | Firestore emulator |
| `:8086` | Pub/Sub emulator |
| `:8089` | WireMock (Mercado Pago) |
| `:5432` | Postgres |

### Pré-requisitos

1. Imagens `:latest` (ou a tag em `IMAGE_TAG`) dos cinco serviços no AR.
2. ADC / login GCP com permissão de **pull** no Artifact Registry.
3. Docker Compose v2.

```bash
./scripts/setup-local-jwt.sh
gcloud auth login   # se ainda não
gcloud auth configure-docker "${GCP_REGION:-us-central1}-docker.pkg.dev"
./scripts/compose.sh pull
./scripts/compose.sh up -d
```

`./scripts/compose.sh` é um wrapper de `docker compose` que exporta `JWT_SIGNING_KEY_PEM` / `JWT_PUBLIC_KEY_PEM` a partir de `.local/jwt/` (não versionado).

### Health

```bash
curl -fsS http://localhost:8080/administrativo/health
curl -fsS http://localhost:8081/autenticacao/health
curl -fsS http://localhost:8082/estoque/health
curl -fsS http://localhost:8083/ordem-servico/health
curl -fsS http://localhost:8084/orcamento/health
```

Login staff (seed idempotente): `POST /autenticacao/login` com `admin` / `admin`.

### Mercado Pago no compose

`orcamento-service` usa `MercadoPago__BaseUrl=http://wiremock:8080`. Stubs em [`wiremock/mappings/`](wiremock/mappings/). Webhook público do MP **não** chega ao localhost: o fluxo e2e/BDD POSTa em `/orcamento/webhook/mercado-pago` com `x-signature` (secret em `MercadoPago__WebhookSecret`).

### Parar

```bash
./scripts/compose.sh down
# volume do Postgres: ./scripts/compose.sh down -v
```

BDD Reqnroll e Requestly do sistema entram em PRs seguintes desta seção.

## Janela GCP (`demo-up` / `demo-down`)

Workflows manuais que disparam, via `gh`, os `tf-apply` / `tf-destroy` / `deploy` dos outros repos e esperam o resultado.

| Workflow | Caixas na UI (reusable + matrix) |
|----------|----------------------------------|
| [`demo-up`](.github/workflows/demo-up.yml) | **infra** (`infra-db` ∥ `infra-k8s` ∥ `infra-events`) → **tf-servicos** → **deploy** |
| [`demo-down`](.github/workflows/demo-down.yml) | **k8s** (`infra-k8s`) → **tf-servicos** → **infra** (`infra-db` ∥ `infra-events`) |

Fases em `demo-phase-*.yml`. Serviços hoje: **`administrativo-service`**, **`autenticacao-service`**, **`estoque-service`**, **`orcamento-service`**, **`ordem-servico-service`**.

O `gh run watch` usa intervalo de **20s** (`POLL_SECONDS`).

### Secret

| Nome | Onde | Papel |
|------|------|--------|
| `DEMO_ORCHESTRATOR_PAT` | org (ou só neste repo) | PAT com permissão de disparar Actions nos repos `fiap-vcosta/*` (`workflow` / Actions read-write) |

O `GITHUB_TOKEN` do `e2e` não consegue dar `workflow_dispatch` em outro repositório.
