# Datadog — observabilidade da Saga (demo)

Artefatos para a janela de demo na org Datadog (`DD_SITE` = `us5.datadoghq.com`). Complementam o dashboard legado da API monolítica em `ordem-servico-service/docs/datadog/` (monitores `service:api` devem ficar **muted** após a migração para os 5 serviços).

## Arquivos

| Arquivo | Uso |
|---------|-----|
| [`dashboard-saga-demo.json`](dashboard-saga-demo.json) | Dashboard geral da Saga (APM, Pub/Sub, Cloud SQL, Firestore, negócio) |

Monitores ficam nos **repos de cada serviço** (dono do consume/DLQ/nack, do publish e do domínio). Exemplos: `ordem-servico-service`, `estoque-service` e `orcamento-service` em `docs/datadog/monitor-*.json`.

## Importar (site `us5`)

1. Abra [https://us5.datadoghq.com](https://us5.datadoghq.com).
2. Importe os `monitor-*.json` de **cada serviço** (e só depois o dashboard da Saga, se quiser ver o `manage_status` preenchido).
3. **Dashboards** → **New Dashboard** → **Import dashboard JSON** → cole [`dashboard-saga-demo.json`](dashboard-saga-demo.json).
4. **Não** versionar e-mail de notificação no Git: configure `@…` / canais na UI da conta estudante.

## Integração GCP

Widgets de **Pub/Sub DLQ** e **Cloud SQL** exigem a integração **Google Cloud Platform** no Datadog, com o projeto `vcosta-fiap-tech-challenge` conectado (roles de leitura de métricas).

- Cloud SQL: `database_id:vcosta-fiap-tech-challenge:tech-challenge-pg`
- DLQ: monitores por serviço consumidor usam `gcp.pubsub.subscription.dead_letter_message_count` (tópico destino `${subscription}-dlq`)

Limite a coleta a tags/métricas necessárias — sidecars Datadog + nós GKE contam no plano estudante. HPA dos serviços com `maxReplicas=2` para não estourar hosts fora do teste de carga.

## Após `demo-down`

Mute (ou pause) os monitores dos serviços quando a janela cair — métricas GCP/APM somem e alertas ficam ruidosos. Mute também os monitores antigos `service:api` do monólito se ainda existirem na org.

## APM / service map

Com sidecars + propagação de trace no Pub/Sub, o service map deve mostrar o fluxo OS → Orçamento → Estoque (e demais bordas HTTP).

- APM services: filtre `env:prod` e os nomes `ordem-servico-service`, `estoque-service`, `orcamento-service`, `administrativo-service`, `autenticacao-service`
- Service map: [APM → Service Map](https://us5.datadoghq.com/apm/map) (site `us5`)
- Logs ↔ traces: logs JSON com `dd.trace_id` → **View Trace**

## Grupos do dashboard

Cores por tipo (só grupos com conteúdo): **Monitores** (purple), **APIs / golden metrics** (vivid_blue), **Aplicação** (blue), **Eventos de entrada** (orange), **Eventos de saída** (green), **Dead-letter** (pink), **Banco** (gray — Cloud SQL ou Firestore), **Insights de negócio** (yellow).
Monitores: `name:([ordem-servico]* OR … OR [saga]*)`. Inclui Cloud SQL e Firestore (`orcamento`, `autenticacao`).

