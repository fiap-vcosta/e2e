# Requestly — sistema

Collections dos cinco prefixos públicos. Detalhe por serviço continua nos respectivos repositórios.

| Arquivo | Uso |
|---------|-----|
| [`sistema.requestly.json`](sistema.requestly.json) | Exploratória (health, auth, CRUD, OS, webhook) |
| [`sistema-e2e-tests.requestly.json`](sistema-e2e-tests.requestly.json) | Suites e2e (Collection Runner) — seed + asserts |
| [`environments/docker.requestly.json`](environments/docker.requestly.json) | Só environment **Docker** |
| [`environments/gcp-gateway.requestly.json`](environments/gcp-gateway.requestly.json) | Só environment **GCP-Gateway** |

O legado `ordem-servico-service/docs/requestly/tech-challenge-e2e-tests.requestly.json` (paths `/api/…`) fica obsoleto; use as suites daqui.

## Environments

| Nome | URLs | Quando |
|------|------|--------|
| **Docker** | `localhost:8080`–`8084` (uma variável por serviço) | `./scripts/compose.sh up -d` |
| **GCP-Gateway** | `https://vcosta-fiap.online` (todas iguais) | Janela `demo-up` |

## Suites e2e

Importe `sistema-e2e-tests.requestly.json`, escolha **Docker**, e em cada pasta: **⋯ → Run**.

| Pasta | O que cobre |
|-------|-------------|
| `01-staff-ate-entregue` | Seed → OS → aprovar → executar → WireMock + webhook → `Entregue` (Docker; em GCP pare no passo 12) |
| `02-rejeicao-depois-descartar` | Rejeitar → `EmDiagnostico` → descartar |
| `03-itens-em-falta-ate-liberada` | Saldo 0 → `AguardandoPeca` → entrada → `LiberadaParaExecucao` |
| `04-contratos-borda` | 401 sem JWT + JWKS |

Cada suite gera CPF/placa/códigos no login. Após passos Pub/Sub o assert espera alguns segundos; se falhar, rode o GET de novo.

## Importar (exploratória)

1. [Requestly API Client](https://requestly.com/) → **Import → Requestly**
2. Importe `sistema.requestly.json` e/ou `sistema-e2e-tests.requestly.json`
3. Selecione **Docker** ou **GCP-Gateway**

Arquitetura: [../arquitetura.md](../arquitetura.md). Saga: [../saga.md](../saga.md).
