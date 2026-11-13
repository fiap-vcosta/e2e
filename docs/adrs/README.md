# ADRs do sistema

Decisões transversais aos microsserviços. ADRs de implementação local (ex. RS256 no `autenticacao-service`, Mercado Pago no `orcamento-service`, Gateway no `infra-k8s`) ficam no repo dono.

| ADR | Tema | Status |
|-----|------|--------|
| [001](001-instancia-cloud-sql-por-servico.md) | Uma instância Cloud SQL por serviço PostgreSQL | Aceito |
| [002](002-firestore-databases-nomeados.md) | Firestore com database nomeado por serviço NoSQL | Aceito |
| [003](003-divisao-microsservicos.md) | Divisão em cinco microsserviços | Aceito |
| [004](004-saga-coreografada.md) | Saga coreografada (sem orquestrador) | Aceito |
| [005](005-publicacao-sem-outbox.md) | Publicação de eventos sem transactional outbox | Aceito |
| [006](006-deploy-demo-manuais-custo.md) | CI automático × `demo-up` / `deploy` / `tf-*` manuais (custo) | Aceito |
