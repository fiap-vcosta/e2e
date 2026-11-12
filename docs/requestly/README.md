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
| **Docker** | `localhost:8080`–`8084` (uma variável por serviço) + WireMock `:8089` | `./scripts/compose.sh up -d` |
| **GCP-Gateway** | `https://vcosta-fiap.online` (todas iguais) | Janela `demo-up` |

## Suites e2e (pastas `docker/` e `gcp/`)

Importe `sistema-e2e-tests.requestly.json`. Escolha o **environment** certo e rode só a pasta correspondente (`⋯ → Run` na pasta `docker` ou `gcp`, ou numa suite filha).

### Poll de status (até 3 minutos)

Todo assert de status da OS faz **poll/retry**: se o status ainda não chegou (Pub/Sub), o passo se repete com ~5 s de intervalo até o status esperado ou **timeout de 3 minutos**. Não rode o GET “na mão” no meio — deixe o runner terminar o poll.

### Pasta `docker` (+ environment **Docker**)

| Suite | O que cobre |
|-------|-------------|
| `01-staff-ate-entregue` | Seed → OS → aprovar → executar → WireMock + webhook → `Entregue` |
| `02-rejeicao-depois-descartar` | Rejeitar → `EmDiagnostico` → descartar |
| `03-itens-em-falta-ate-liberada` | Saldo 0 → `AguardandoPeca` → entrada → `LiberadaParaExecucao` |
| `04-contratos-borda` | 401 sem JWT + JWKS |

### Pasta `gcp` (+ environment **GCP-Gateway**)

Sem passos WireMock (Mercado Pago real na nuvem).

| Suite | O que cobre |
|-------|-------------|
| `01-staff-ate-aguardando-pagamento` | Mesmo happy path até `AguardandoPagamento` (pare antes do Pix sandbox/webhook) |
| `02-rejeicao-depois-descartar` | Igual ao Docker |
| `03-itens-em-falta-ate-liberada` | Igual ao Docker |
| `04-contratos-borda` | Igual ao Docker |

Cada suite gera CPF/placa/códigos no login.

## Importar (exploratória)

1. [Requestly API Client](https://requestly.com/) → **Import → Requestly**
2. Importe `sistema.requestly.json` e/ou `sistema-e2e-tests.requestly.json`
3. Selecione **Docker** ou **GCP-Gateway**
4. Reimporte as suites se já tinha a versão antiga (estrutura mudou: pastas `docker` / `gcp`)

Arquitetura: [../arquitetura.md](../arquitetura.md). Saga: [../saga.md](../saga.md).
