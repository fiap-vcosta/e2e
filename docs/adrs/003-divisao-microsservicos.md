# ADR 003: Divisão em cinco microsserviços

**Data:** 12 de novembro de 2026  
**Status:** Aceito  
**Autores:** Victor Costa

## 1. Contexto e Problema

O monólito anterior concentrou cadastro, estoque, ordem de serviço, autenticação e (parcialmente) orçamento. O enunciado da fase atual pede microsserviços com comunicação assíncrona e bancos próprios. Manter três serviços “grandes” ou cinco mais focados muda o custo de repo/CI/TF e a clareza de domínio.

O problema a ser resolvido é: **Quais bounded contexts viram repositórios de serviço, e o que fica compartilhado?**

## 2. Decisão

Cinco serviços .NET 8, cada um com repo, CI, imagem e deploy próprios:

| Serviço | Domínio | Persistência |
|---------|---------|--------------|
| `administrativo-service` | Cliente, Veículo, catálogo de Serviço | Cloud SQL própria |
| `autenticacao-service` | Login staff, JWT cliente, JWKS | Firestore `autenticacao` |
| `estoque-service` | ItemEstoque, reserva, fila de pendências | Cloud SQL própria |
| `ordem-servico-service` | Ciclo de vida da OS, diagnóstico, execução | Cloud SQL própria |
| `orcamento-service` | Orçamento, cobrança Pix (Mercado Pago) | Firestore `orcamento` |

Código transversal (envelope de eventos, Pub/Sub worker, JWT/JWKS validation, Problem Details, `X-Service-Key`) vive no pacote NuGet `building-blocks`. Contratos de sistema e Saga neste repo `e2e`. Infra compartilhada: `infra-bootstrap`, `infra-k8s`, `infra-events`.

## 3. Justificativa

* Cada serviço tem um dono de dados e de ciclo de vida (TF + destroy) alinhado ao PDF.
* Autenticação e administrativo separados evitam misturar emissão de JWT com catálogo e com a máquina de estados da OS.
* Estoque e orçamento reagem a eventos da OS sem compartilhar banco — Saga coreografada ([ADR 004](004-saga-coreografada.md)).

## 4. Alternativas Consideradas

* **Três serviços** (OS+estoque+orçamento juntos / admin+auth juntos): menos overhead de CI/TF; rejeitado — boundários e bancos ficam ambíguos na narrativa.
* **Monólito modular:** rejeitado pelo enunciado de microsserviços.

## 5. Consequências

### Positivas
* Deploy e smoke por prefixo HTTP (`/administrativo`, `/autenticacao`, …).
* Falha ou destroy de um serviço não apaga o banco dos outros.

### Negativas / Riscos
* Overhead fixo de cinco pipelines e cinco imagens.
  * *Mitigação:* reusable workflows no repo `actions`; template no `administrativo-service`.
* Dados de cliente/veículo/item na OS são **snapshot** via REST `/system/…`, não join cross-database.
