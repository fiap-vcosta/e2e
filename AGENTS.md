# Tech Challenge — Guia para agentes (`e2e`)

Compose do sistema, testes de ponta a ponta (Reqnroll), collections HTTP e diagramas de arquitetura. Org [fiap-vcosta](https://github.com/fiap-vcosta).

## Antes de mudar código

1. Perguntar antes de mudar o contrato dos cenários de ponta a ponta
2. Não colocar código-fonte de microsserviço nem Terraform de GCP aqui
3. **Git:** nunca commit/push direto em `main` — branch → PR → merge (ver [`.cursor/rules/git-workflow.mdc`](.cursor/rules/git-workflow.mdc))

## Layout

| Peça | Papel |
|------|--------|
| `docker-compose.yml` | Sistema: 5 serviços (imagens AR) + Postgres + Firestore/Pub/Sub emulators + WireMock |
| `scripts/compose.sh` | Wrapper que exporta PEMs JWT e chama `docker compose` |
| `scripts/setup-local-jwt.sh` | Gera `.local/jwt` + `.env` |
| `scripts/init-pubsub-emulator.sh` | Tópicos/subscriptions alinhados aos serviços |
| `wiremock/mappings/` | Fake Mercado Pago Orders (`MercadoPago__BaseUrl`) |
| `tests/Bdd/` | Reqnroll (caminho feliz + compensações contra o compose) |
| `e2e.sln` | Solution do BDD |
| `.github/workflows/demo-up.yml` / `demo-down.yml` | Orquestradores GCP |
| `.github/workflows/demo-phase-*.yml` | Matrix por repo; dispara `tf-apply` / `tf-destroy` / `deploy` via PAT |
| `scripts/gh-dispatch-wait.sh` | Dispara um workflow e espera o run (`POLL_SECONDS=20`) |

## Fora de escopo

Código de domínio dos serviços e root modules Terraform da GCP (só orquestra callers já existentes).

## Comandos

```bash
./scripts/setup-local-jwt.sh
gcloud auth configure-docker us-central1-docker.pkg.dev
./scripts/compose.sh pull
./scripts/compose.sh up -d
# health: curl localhost:8080–8084 …/health
dotnet test tests/Bdd/E2e.Bdd.csproj -c Release

# Na UI do repo e2e: Actions → demo-up / demo-down → Run workflow
```

Requestly do sistema / CI BDD: partes seguintes da §13.
