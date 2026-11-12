# e2e

Compose do sistema, testes de ponta a ponta e collections HTTP. Este repositório não contém código de serviço. Org [fiap-vcosta](https://github.com/fiap-vcosta).

Contrato entre os serviços:

- [Arquitetura](docs/arquitetura.md)
- [Eventos](docs/eventos.md)
- [Saga](docs/saga.md)
- [HTTP](docs/contratos-http.md)
- [Requestly do sistema](docs/requestly/)

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

### BDD Reqnroll (caminho feliz)

Com o compose no ar:

```bash
dotnet test tests/Bdd/E2e.Bdd.csproj -c Release
```

Os cenários sobem catálogo/estoque via HTTP e exercitam:
- caminho feliz até `Entregue` (webhook aprovado no WireMock);
- pagamento recusado (stub GET `rejected` + nova cobrança automática);
- descarte após `LiberadaParaExecucao`.

### CI

Workflow [`ci.yml`](.github/workflows/ci.yml) (PR / push `main` / manual): WIF → pull AR → `compose up` → `dotnet test` → Job Summary + artifact `bdd-results` → `compose down -v`.

### Requestly

[`docs/requestly/`](docs/requestly/): exploratória (`sistema.requestly.json`) e suites e2e (`sistema-e2e-tests.requestly.json`, Collection Runner). Environments **Docker** / **GCP-Gateway**.

## Stress HPA (`ordem-servico-service`)

Na janela GCP, com o cluster no ar, o script [`scripts/stress-hpa.sh`](scripts/stress-hpa.sh) autentica no `/autenticacao/login` e gera carga em `GET /ordem-servico` para o HPA do `ordem-servico-service` (teto `maxReplicas: 5` no manifesto desse serviço).

```bash
# Gateway (mesmo host)
./scripts/stress-hpa.sh https://vcosta-fiap.online 120 25

# Compose local (só valida login + listagem; sem HPA)
AUTH_BASE_URL=http://localhost:8081 OS_BASE_URL=http://localhost:8083 \
  ./scripts/stress-hpa.sh unused 30 5
```

Observar no cluster: `watch -n 2 kubectl get hpa,pods -n tech-challenge`. Não roda no CI.

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
