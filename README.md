# e2e

Compose do sistema, testes de ponta a ponta e collections HTTP. Este repositório não contém código de serviço. Org [fiap-vcosta](https://github.com/fiap-vcosta).

Contrato entre os serviços:

- [Eventos](docs/eventos.md)
- [Saga](docs/saga.md)
- [HTTP](docs/contratos-http.md)

## Janela GCP (`demo-up` / `demo-down`)

Workflows manuais que disparam, via `gh`, os `tf-apply` / `tf-destroy` / `deploy` dos outros repos e esperam o resultado.

| Workflow | Caixas na UI (reusable + matrix) |
|----------|----------------------------------|
| [`demo-up`](.github/workflows/demo-up.yml) | **infra** (`infra-db` ∥ `infra-k8s` ∥ `infra-events`) → **tf-servicos** → **deploy** |
| [`demo-down`](.github/workflows/demo-down.yml) | **k8s** (`infra-k8s`) → **tf-servicos** → **infra** (`infra-db` ∥ `infra-events`) |

Fases em `demo-phase-*.yml`. Serviços hoje: **`administrativo-service`**, **`autenticacao-service`**, **`estoque-service`**, **`ordem-servico-service`** — acrescente no `matrix.repo` das phases de serviço (checklist §14).

O `gh run watch` usa intervalo de **20s** (`POLL_SECONDS`).

### Secret

| Nome | Onde | Papel |
|------|------|--------|
| `DEMO_ORCHESTRATOR_PAT` | org (ou só neste repo) | PAT com permissão de disparar Actions nos repos `fiap-vcosta/*` (`workflow` / Actions read-write) |

O `GITHUB_TOKEN` do `e2e` não consegue dar `workflow_dispatch` em outro repositório.
