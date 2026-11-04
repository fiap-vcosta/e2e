# e2e

Compose do sistema, testes de ponta a ponta e collections HTTP. Este repositório não contém código de serviço. Org [fiap-vcosta](https://github.com/fiap-vcosta).

Contrato entre os serviços:

- [Eventos](docs/eventos.md)
- [Saga](docs/saga.md)
- [HTTP](docs/contratos-http.md)

## Janela GCP (`demo-up` / `demo-down`)

Workflows manuais que disparam, via `gh`, os `tf-apply` / `tf-destroy` / `deploy` dos outros repos e esperam o resultado.

| Workflow | Ordem |
|----------|--------|
| [`demo-up`](.github/workflows/demo-up.yml) | `infra-db` ∥ `infra-k8s` ∥ `infra-events` → `tf-apply` dos serviços → `deploy` dos serviços |
| [`demo-down`](.github/workflows/demo-down.yml) | `infra-k8s` destroy → `tf-destroy` dos serviços → `infra-db` ∥ `infra-events` destroy |

Serviços na lista hoje: **`administrativo-service`**. Acrescentar os demais em `SERVICE_REPOS` nos dois YAMLs quando o repo tiver `tf-apply` / `tf-destroy` / `deploy`.

### Secret

| Nome | Onde | Papel |
|------|------|--------|
| `DEMO_ORCHESTRATOR_PAT` | org (ou só neste repo) | PAT com permissão de disparar Actions nos repos `fiap-vcosta/*` (`workflow` / Actions read-write) |

O `GITHUB_TOKEN` do `e2e` não consegue dar `workflow_dispatch` em outro repositório.
