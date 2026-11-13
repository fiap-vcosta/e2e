# ADR 004: Saga coreografada (sem orquestrador)

**Data:** 12 de novembro de 2026  
**Status:** Aceito  
**Autores:** Victor Costa

## 1. Contexto e Problema

O fluxo da ordem de serviço atravessa orçamento, estoque e pagamento. Um processo distribuído precisa coordenar sucesso e compensações (falha de orçamento, descarte, recusa/expiração de Pix) sem um banco único.

O problema a ser resolvido é: **Orquestrador central ou coreografia por eventos entre os serviços?**

## 2. Decisão

- **Saga coreografada:** cada serviço reage a eventos Pub/Sub e publica o próximo passo; **não** há orquestrador nem process manager dedicado.
- Contratos: [eventos.md](../eventos.md), estados e sequências: [saga.md](../saga.md).
- Compensações explícitas no domínio (ex.: `OrcamentoFalhou` → OS volta a `EmDiagnostico`; descarte → libera reserva / cancela orçamento; Pix recusado vs expirado com políticas distintas).

## 3. Justificativa

* Encaixa no desenho já escolhido de **1 tópico por evento** e workers pull nos serviços.
* Evita um quarto (ou sexto) componente “orquestrador” para manter, deployar e cobrar na demo.
* A OS permanece a autoridade do **status** visível; os outros serviços não “empurram” status arbitrário na borda HTTP.

## 4. Alternativas Consideradas

* **Orquestrador (ex. workflow engine / serviço Saga):** visibilidade central; custo de operação e ponto único de falha — rejeitado para o escopo acadêmico.
* **Só REST síncrono encadeado:** rejeitado — acoplamento temporal e timeouts na reserva/pagamento.

## 5. Consequências

### Positivas
* Serviços evoluem consumidores/publicadores de forma independente.
* Compensações ficam documentadas e testáveis (BDD no `e2e` + testes nos serviços).

### Negativas / Riscos
* Ordem eventual e reentregas exigem **idempotência** e DLQ.
  * *Mitigação:* chave de negócio por evento; `EventosProcessados` / equivalente; monitores Datadog de dead-letter.
* Diagnóstico de fluxo exige traces distribuídos e o diagrama da Saga.
  * *Mitigação:* propagação de trace no Pub/Sub (`building-blocks`) + dashboard da Saga neste repo.
