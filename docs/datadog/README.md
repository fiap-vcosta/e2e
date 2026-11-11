# Datadog — observabilidade da Saga (demo)

Artefatos para a janela de demo na org Datadog (`DD_SITE` = `us5.datadoghq.com`). Complementam o dashboard legado da API monolítica em `ordem-servico-service/docs/datadog/` (monitores `service:api` devem ficar **muted** após a migração para os 5 serviços).

## Arquivos

| Arquivo | Uso |
|---------|-----|
| [`dashboard-saga-demo.json`](dashboard-saga-demo.json) | Dashboard ordered: APM p95/erros/hits nos 5 serviços, transições de OS, compensação, nacks, DLQ Pub/Sub, Cloud SQL |
| [`monitor-dlq-pubsub.json`](monitor-dlq-pubsub.json) | Alert se `dead_letter_message_count` > 0 nas 13 subscriptions com política DLQ |
| [`monitor-pubsub-nack.json`](monitor-pubsub-nack.json) | Log alert: `Failed to process Pub/Sub message` (`env:prod`) |
| [`monitor-orcamento-persistencia.json`](monitor-orcamento-persistencia.json) | Log alert: `Falha ao gravar orçamento` em `orcamento-service` |

## Importar (site `us5`)

1. Abra [https://us5.datadoghq.com](https://us5.datadoghq.com).
2. **Dashboards** → **New Dashboard** → **Import dashboard JSON** → cole [`dashboard-saga-demo.json`](dashboard-saga-demo.json).
3. **Monitors** → **New Monitor** (ou API/import) a partir de cada `monitor-*.json`.
4. **Não** versionar e-mail de notificação no Git: configure `@…` / canais na UI da conta estudante.

## Integração GCP

Widgets de **Pub/Sub DLQ** e **Cloud SQL** exigem a integração **Google Cloud Platform** no Datadog, com o projeto `vcosta-fiap-tech-challenge` conectado (roles de leitura de métricas).

- Cloud SQL: `database_id:vcosta-fiap-tech-challenge:tech-challenge-pg`
- DLQ: monitor usa `gcp.pubsub.subscription.dead_letter_message_count` nas subscriptions de consumo (tópico destino `${subscription}-dlq`)

Limite a coleta a tags/métricas necessárias — sidecars Datadog + nós GKE contam no plano estudante. HPA dos serviços com `maxReplicas=2` para não estourar hosts fora do teste de carga.

## Após `demo-down`

Mute (ou pause) os monitores desta pasta quando a janela cair — métricas GCP/APM somem e alertas ficam ruidosos. Mute também os monitores antigos `service:api` do monólito se ainda existirem na org.

## APM / service map

Com sidecars + propagação de trace no Pub/Sub, o service map deve mostrar o fluxo OS → Orçamento → Estoque (e demais bordas HTTP).

- APM services: filtre `env:prod` e os nomes `ordem-servico-service`, `estoque-service`, `orcamento-service`, `administrativo-service`, `autenticacao-service`
- Service map: [APM → Service Map](https://us5.datadoghq.com/apm/map) (site `us5`)
- Logs ↔ traces: logs JSON com `dd.trace_id` → **View Trace**
