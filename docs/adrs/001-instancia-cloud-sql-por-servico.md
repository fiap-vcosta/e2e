# ADR 001: Uma instância Cloud SQL por serviço PostgreSQL

**Data:** 12 de novembro de 2026  
**Status:** Aceito  
**Autores:** Victor Costa

## 1. Contexto e Problema

Na Fase 04 a instância PostgreSQL era compartilhada (`infra-db`, `tech-challenge-pg`), com um database/usuário por serviço SQL e isolamento lógico (`REVOKE CONNECT` / role NOLOGIN). O enunciado pede banco próprio por microsserviço; isolamento lógico não basta para a narrativa da entrega.

Serviços SQL: `administrativo-service`, `estoque-service`, `ordem-servico-service`. Firestore já é database nomeado por serviço (`orcamento`, `autenticacao`).

O problema a ser resolvido é: **Como provisionar PostgreSQL na GCP com banco próprio por serviço, custo baixo na janela de demo e dono claro no Git?**

## 2. Decisão

- **Uma instância Cloud SQL por serviço SQL**, no Terraform do próprio repo (`{repo}-pg`, ex. `administrativo-service-pg`).
- **Tier** `db-f1-micro`, PostgreSQL 16, IP privado via VPC do `infra-bootstrap`, sem HA, backup desligado, `deletion_protection = false`.
- **Database + usuário + secret** no mesmo stack do serviço; Auth Proxy no GKE; `Maximum Pool Size=4`.
- **Grants** mínimos de schema/`CONNECT` para o usuário da app (Data API / `grant.sql`) — sem isolamento cross-database.
- **Rede:** `terraform_remote_state` do bootstrap (a VPC não mora no serviço).
- **`infra-db`:** sai do `demo-up` / `demo-down` e é arquivado; a instância sobe/desce com o `tf-apply` / `tf-destroy` de cada serviço SQL.

## 3. Justificativa

* Alinha “infraestrutura e banco” no repositório do microsserviço.
* `db-f1-micro` basta com um serviço por instância e pool baixo; o `db-g1-small` era necessário só no modelo compartilhado.
* Grants mínimos evitam falha do EF migrate no `public` do PostgreSQL 16.

## 4. Alternativas Consideradas

* **Manter instância compartilhada no `infra-db`:** menor custo horário; rejeitado para alinhar ao PDF.
* **N stacks ainda no `infra-db`:** centraliza SQL; rejeitado — o dono natural é o repo do serviço.
* **Sem grants / Data API:** risco de migrate falhar no schema `public`.

## 5. Consequências

### Positivas
* Demo e READMEs mostram banco próprio por serviço.
* Destroy do serviço apaga a própria instância.

### Negativas / Riscos
* **~3× compute SQL** na janela da demo.
  * *Mitigação:* `demo-down` obrigatório; tier `f1-micro`.
* **Nome da instância reservado** alguns dias após destroy.
  * *Mitigação:* aguardar ou alterar `db_instance_name`.
