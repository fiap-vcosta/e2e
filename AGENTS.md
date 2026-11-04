# Tech Challenge — Guia para agentes (`e2e`)

Compose do sistema, testes de ponta a ponta (Reqnroll), collections HTTP e diagramas de arquitetura. Org [fiap-vcosta](https://github.com/fiap-vcosta).

## Antes de mudar código

1. Perguntar antes de mudar o contrato dos cenários de ponta a ponta
2. Não colocar código-fonte de microsserviço nem Terraform de GCP aqui
3. **Git:** nunca commit/push direto em `main` — branch → PR → merge (ver [`.cursor/rules/git-workflow.mdc`](.cursor/rules/git-workflow.mdc))

## Layout alvo

Compose do sistema inteiro, BDD com Reqnroll, collections HTTP e diagramas de arquitetura.

| Peça | Papel |
|------|--------|
| `.github/workflows/demo-up.yml` / `demo-down.yml` | Orquestradores: jobs `uses:` → phases (caixa grande na UI) |
| `.github/workflows/demo-phase-*.yml` | Matrix por repo (caixas pequenas); dispara `tf-apply` / `tf-destroy` / `deploy` via PAT |
| `scripts/gh-dispatch-wait.sh` | Dispara um workflow e espera o run (`POLL_SECONDS=20`) |

## Fora de escopo

Código de domínio dos serviços e root modules Terraform da GCP (só orquestra callers já existentes).

## Comandos

```bash
# Na UI do repo e2e: Actions → demo-up / demo-down → Run workflow
```

Compose / BDD / collections ainda não existem — não inventar comandos `dotnet` até chegarem.
