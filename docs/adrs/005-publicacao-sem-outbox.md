# ADR 005: Publicação de eventos sem transactional outbox

**Data:** 12 de novembro de 2026  
**Status:** Aceito  
**Autores:** Victor Costa

## 1. Contexto e Problema

Após commit no banco do serviço, o publisher envia a mensagem ao Pub/Sub. Se o processo cair entre o commit e o publish, o evento pode não sair. Transactional outbox (gravar evento na mesma transação e publicar depois) é o padrão de mercado para fechar essa janela.

O problema a ser resolvido é: **Outbox agora, ou publicação direta com risco documentado?**

## 2. Decisão

- **Publicação direta** após o commit bem-sucedido (sem tabela outbox / relay).
- Consumidores **idempotentes** (chave de negócio + registro de evento processado).
- Tópicos de **dead-letter** por subscription no Terraform do consumidor.
- Risco aceito na demo acadêmica; outbox fica como melhoria opcional (não bloqueia entrega).

## 3. Justificativa

* Menos peças para implementar e operar em cinco serviços na janela curta.
* Idempotência + DLQ + reprocessamento manual cobrem a maior parte dos cenários de demo e BDD.
* O enunciado prioriza Saga e observabilidade; outbox não é requisito explícito.

## 4. Alternativas Consideradas

* **Transactional outbox + worker de publicação:** corretude mais forte; adiado por tempo/custo de implementação.
* **Dual write com inbox só no consumidor:** já parcialmente coberto pela idempotência; não resolve perda no publish.

## 5. Consequências

### Positivas
* Código de publicação simples (publisher no `building-blocks` + chamada pós-commit).
* Menos schema e menos jobs por serviço.

### Negativas / Riscos
* **Evento perdido** se a falha ocorrer após commit e antes do publish.
  * *Mitigação:* idempotência na reentrega; DLQ; evidência em logs/traces; item opcional de outbox no backlog.
* Operador pode precisar republicar/reprocessar em incidente raro.
  * *Mitigação:* documentado; monitores de DLQ na janela.
