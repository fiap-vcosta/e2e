# Requestly — sistema

Collection agregada dos cinco prefixos públicos. Collections detalhadas por serviço ficam nos respectivos repositórios.

| Arquivo | Uso |
|---------|-----|
| [`sistema.requestly.json`](sistema.requestly.json) | Collection + environments embutidos |
| [`environments/docker.requestly.json`](environments/docker.requestly.json) | Só environment **Docker** |
| [`environments/gcp-gateway.requestly.json`](environments/gcp-gateway.requestly.json) | Só environment **GCP-Gateway** |

## Environments

| Nome | URLs | Quando |
|------|------|--------|
| **Docker** | `localhost:8080`–`8084` (uma variável por serviço) | `./scripts/compose.sh up -d` |
| **GCP-Gateway** | `https://vcosta-fiap.online` (todas iguais) | Janela `demo-up` |

Variáveis: `administrativoUrl`, `autenticacaoUrl`, `estoqueUrl`, `ordemServicoUrl`, `orcamentoUrl`, `token`, `tokenCliente`, `documentoCliente`, `serviceAuthKey`, ids da saga, `tokenAprovacao`, `dataId` / `xRequestId` / `xSignature` (webhook).

## Importar

1. [Requestly API Client](https://requestly.com/) → **Import → Requestly**
2. Importe `sistema.requestly.json` (ou só o environment desejado)
3. Selecione **Docker** ou **GCP-Gateway**
4. Ordem sugerida: `00-health` → `01-auth` → `02-administrativo` → `03-estoque` → `04-ordem-servico`

Credenciais seed: `admin` / `admin`. O token opaco de aprovação do cliente **não** vem na API; no compose consulte o Postgres da OS e preencha `tokenAprovacao` antes de `05-cliente-aprovacao`.

Arquitetura: [../arquitetura.md](../arquitetura.md). Saga: [../saga.md](../saga.md).
