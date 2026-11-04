# e2e

Compose do sistema, testes de ponta a ponta e collections HTTP. Este repositório não contém código de serviço. Org [fiap-vcosta](https://github.com/fiap-vcosta).

Contrato entre os serviços:

- [Eventos](docs/eventos.md)
- [Saga](docs/saga.md)
- [HTTP](docs/contratos-http.md)

## Janela GCP (`demo-up` / `demo-down`)

Workflows manuais que disparam, via `gh`, os `tf-apply` / `tf-destroy` / `deploy` dos outros repos e esperam o resultado.

| Workflow | Jobs (UI) |
|----------|-----------|
| [`demo-up`](.github/workflows/demo-up.yml) | matrix `infra / *` (∥) → matrix `tf-apply / *` → matrix `deploy / *` |
| [`demo-down`](.github/workflows/demo-down.yml) | `destroy / infra-k8s` → matrix `destroy / <serviço>` → matrix `destroy / infra-db` ∥ `infra-events` |

Serviços na matrix hoje: **`administrativo-service`**. Acrescentar no `matrix.repo` dos dois YAMLs quando o repo tiver `tf-apply` / `tf-destroy` / `deploy`.

O `gh run watch` usa intervalo de **20s** (`POLL_SECONDS`) para não poluir o log.

### Secret

| Nome | Onde | Papel |
|------|------|--------|
| `DEMO_ORCHESTRATOR_PAT` | org (ou só neste repo) | PAT com permissão de disparar Actions nos repos `fiap-vcosta/*` (`workflow` / Actions read-write) |

O `GITHUB_TOKEN` do `e2e` não consegue dar `workflow_dispatch` em outro repositório.
