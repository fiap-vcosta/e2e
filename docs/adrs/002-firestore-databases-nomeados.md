# ADR 002: Firestore com database nomeado por serviço NoSQL

**Data:** 12 de novembro de 2026  
**Status:** Aceito  
**Autores:** Victor Costa

## 1. Contexto e Problema

Dois serviços não usam PostgreSQL: `orcamento-service` (orçamento e cobrança Pix) e `autenticacao-service` (usuários staff e emissão de JWT). O projeto GCP tem um Firestore; o database `(default)` seria compartilhado se ambos gravassem nele.

O problema a ser resolvido é: **Como isolar dados NoSQL por microsserviço sem misturar coleções no mesmo database?**

## 2. Decisão

- **Um database Firestore nomeado por serviço:** `orcamento` e `autenticacao` (nenhum usa `(default)`).
- Provisionamento no Terraform do próprio repo (junto de subscriptions/DLQ quando existirem).
- Emulador local e compose do `e2e` apontam para esses nomes.
- IAM: SA de runtime com `datastore.user` no projeto (bootstrap); escopo de dados = database do serviço.

## 3. Justificativa

* Espelha a narrativa de “banco próprio por serviço” já adotada no SQL ([ADR 001](001-instancia-cloud-sql-por-servico.md)).
* Evita colisão de coleções (`usuarios` vs `orcamentos`) e facilita destroy/recreate na janela de demo.
* Custo do Firestore na demo permanece baixo; databases nomeados não mudam o modelo de cobrança de forma relevante neste escopo.

## 4. Alternativas Consideradas

* **Só o database `(default)` com prefixo de coleção:** mais simples de provisionar; rejeitado — isolamento frágil e narrativa fraca na entrega.
* **Um projeto GCP por serviço:** isolamento máximo; inviável para conta/demo acadêmica.

## 5. Consequências

### Positivas
* READMEs e diagramas mostram database nomeado por serviço NoSQL.
* Destroy do serviço pode apagar o database sem afetar o outro.

### Negativas / Riscos
* Dois databases para operar e monitorar.
  * *Mitigação:* nomes fixos no Terraform; compose/e2e documentados.
