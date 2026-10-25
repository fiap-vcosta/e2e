# Catálogo de eventos

Contrato de mensageria entre `ordem-servico-service`, `orcamento-service` e `estoque-service`. `administrativo-service` e `autenticacao-service` não publicam nem consomem estes eventos.

A máquina de estados que reage a eles está em [saga.md](saga.md). O HTTP está em [contratos-http.md](contratos-http.md).

## Envelope

Toda mensagem, inclusive as do tópico `notificacoes`, usa o mesmo corpo. `version` começa em 1. Um campo novo incompatível sobe `version`.

```json
{
  "eventId": "3f1c2a0e-7b4d-4e2a-9c11-0a6b8d2e4f10",
  "type": "DiagnosticoFinalizado",
  "version": 1,
  "occurredAt": "2026-10-25T15:04:05Z",
  "data": {}
}
```

| Campo | Tipo | Papel |
|-------|------|--------|
| `eventId` | UUID | Identidade desta publicação |
| `type` | string | Nome do evento, igual à coluna Evento |
| `version` | int | Versão do formato de `data` |
| `occurredAt` | date-time UTC | Instante da publicação |
| `data` | object | Payload descrito em cada evento |

A publicação ocorre depois do commit do publicador. A entrega é at-least-once. Não há ordem entre tópicos.

## Idempotência

O consumidor persiste `eventId` já aplicado. Uma reentrega do mesmo `eventId` é reconhecida e não muda estado.

Cada evento tem também uma chave de negócio. Um fato novo da mesma ordem de serviço (outra finalização de diagnóstico, outra cobrança) gera outro `eventId` e outra chave. Se a chave de negócio já foi aplicada, o consumidor reconhece a mensagem e não aplica de novo, mesmo com `eventId` diferente.

Se a ordem de serviço não está num status que aceita a transição, o consumidor reconhece a mensagem e não muda estado.

`OrdemServicoDescartada` pode chegar antes ou depois de `DiagnosticoFinalizado` ou `OrdemServicoAprovada`, porque são tópicos diferentes. Quem consome o descarte guarda a ordem descartada. Um fato posterior dessa ordem não reserva estoque nem cria orçamento. Se o fato chegou primeiro, o descarte cancela o que existir. Descarte sem orçamento e sem reserva não tem efeito colateral.

## Tópicos

Um tópico por evento de domínio. O id do tópico é o nome em kebab-case. As duas notificações compartilham o tópico `notificacoes` e se distinguem por `type`.

| Evento | Tópico | Publicador | Consumidores | Chave de negócio |
|--------|--------|------------|--------------|------------------|
| `DiagnosticoFinalizado` | `diagnostico-finalizado` | ordem-servico | orcamento | `ordemServicoId` + `versaoDiagnostico` |
| `OrcamentoGerado` | `orcamento-gerado` | orcamento | ordem-servico | `ordemServicoId` + `versaoDiagnostico` |
| `OrcamentoFalhou` | `orcamento-falhou` | orcamento | ordem-servico | `ordemServicoId` + `versaoDiagnostico` |
| `OrdemServicoAprovada` | `ordem-servico-aprovada` | ordem-servico | estoque | `ordemServicoId` + `aprovadaEm` |
| `EstoqueReservado` | `estoque-reservado` | estoque | ordem-servico | `ordemServicoId` + `aprovadaEm` |
| `EstoqueEmFalta` | `estoque-em-falta` | estoque | ordem-servico | `ordemServicoId` + `aprovadaEm` |
| `OrdemServicoFinalizada` | `ordem-servico-finalizada` | ordem-servico | estoque, orcamento | `ordemServicoId` |
| `CobrancaGerada` | `cobranca-gerada` | orcamento | ordem-servico | `ordemServicoId` + `cobrancaId` |
| `PagamentoConfirmado` | `pagamento-confirmado` | orcamento | ordem-servico | `ordemServicoId` + `cobrancaId` |
| `PagamentoRecusado` | `pagamento-recusado` | orcamento | ordem-servico | `ordemServicoId` + `cobrancaId` |
| `OrdemServicoDescartada` | `ordem-servico-descartada` | ordem-servico | orcamento, estoque | `ordemServicoId` |
| `OrcamentoDisponivel` | `notificacoes` | orcamento | nenhum | `ordemServicoId` + `versaoDiagnostico` |
| `CobrancaPixDisponivel` | `notificacoes` | orcamento | nenhum | `ordemServicoId` + `cobrancaId` |

`versaoDiagnostico` é um inteiro da ordem de serviço. A primeira finalização de diagnóstico vale 1. Cada finalização seguinte, depois de a OS voltar a `EmDiagnostico`, incrementa o contador. `aprovadaEm` é o instante UTC em que a OS entrou em `ChecandoEstoque`, serializado em ISO-8601. `orcamentoId` e `cobrancaId` são strings atribuídas pelo `orcamento-service`. `cobrancaId` é o id da order no Mercado Pago. Ids de ordem, serviço e item de estoque permanecem inteiros.

## Payloads (`version` 1)

Valores monetários e quantidades são números decimais. Status vão com o nome do enum (`Sugerido`, `Aprovado`, `Rejeitado`, `Concluido`, `EstoqueEmFalta`, `EstoqueTravado`).

### `DiagnosticoFinalizado`

A OS publica ao entrar em `AguardandoOrcamento`. O orçamento gera ou recalcula a partir desta lista. O total cobrado é a soma de `valorCobrado` dos serviços cujo `status` não é `Rejeitado`. Itens de estoque não entram neste payload: a reserva só acontece depois da aprovação.

```json
{
  "ordemServicoId": 10,
  "versaoDiagnostico": 1,
  "servicos": [
    { "id": 3, "nome": "Troca de óleo", "valorCobrado": 150.0, "status": "Sugerido" }
  ]
}
```

Efeito: o orçamento persiste ou atualiza o orçamento dessa versão e publica `OrcamentoGerado`, ou publica `OrcamentoFalhou`.

### `OrcamentoGerado`

Cobre a primeira geração e o recálculo. Toda finalização produz este evento ou `OrcamentoFalhou`, inclusive quando não há serviço sugerido.

```json
{
  "ordemServicoId": 10,
  "versaoDiagnostico": 1,
  "orcamentoId": "orc-10-1"
}
```

Efeito, só a partir de `AguardandoOrcamento`: a OS aplica o roteamento do diagnóstico sobre os status que ela já tem. Há sugeridos → `AguardandoAprovacao`. Não há sugeridos e há aprovados → `ChecandoEstoque` e `OrdemServicoAprovada`. Não há o que executar → `Entregue`.

### `OrcamentoFalhou`

```json
{
  "ordemServicoId": 10,
  "versaoDiagnostico": 1,
  "motivo": "falha ao gravar o orçamento"
}
```

`motivo` é texto livre. Efeito, só a partir de `AguardandoOrcamento`: a OS volta a `EmDiagnostico`.

### `OrdemServicoAprovada`

A OS publica ao entrar em `ChecandoEstoque` (aprovação humana ou roteamento sem sugeridos e com aprovados). `itens` reúne os itens dos serviços `Aprovado`. A prioridade da fila de pendência no estoque é `aprovadaEm` crescente.

```json
{
  "ordemServicoId": 10,
  "aprovadaEm": "2026-10-25T15:10:00Z",
  "itens": [
    { "itemEstoqueId": 7, "quantidade": 2 }
  ]
}
```

Efeito: o estoque tenta reservar. Se couber, trava o saldo e publica `EstoqueReservado`. Se não couber, não trava, guarda a pendência e publica `EstoqueEmFalta` uma vez para esta chave. Numa entrada de itens, reavalia as pendências por `aprovadaEm` e publica `EstoqueReservado` para as que passarem a caber. Pendência que continua sem saldo não gera outro `EstoqueEmFalta`.

### `EstoqueReservado`

O saldo já foi travado no estoque. A OS marca cada item como `EstoqueTravado`.

```json
{
  "ordemServicoId": 10,
  "aprovadaEm": "2026-10-25T15:10:00Z",
  "itens": [
    { "itemEstoqueId": 7, "quantidade": 2, "status": "EstoqueTravado" }
  ]
}
```

Efeito, a partir de `ChecandoEstoque` ou `AguardandoPeca`: a OS vai para `LiberadaParaExecucao`.

### `EstoqueEmFalta`

Nada foi travado. Cada item vem como `EstoqueDisponivel` ou `EstoqueEmFalta`. Falta de peça não é falha da Saga.

```json
{
  "ordemServicoId": 10,
  "aprovadaEm": "2026-10-25T15:10:00Z",
  "itens": [
    { "itemEstoqueId": 7, "quantidade": 2, "status": "EstoqueEmFalta" }
  ]
}
```

Efeito, a partir de `ChecandoEstoque` ou `AguardandoPeca`: a OS fica em `AguardandoPeca`.

### `OrdemServicoFinalizada`

A OS publica só na transição para `Finalizada` (todos os serviços `Concluido` ou `Rejeitado`). Execução parcial, que deixa a OS em `EmExecucao`, não publica. Os itens da OS já estão `Utilizado` nesse momento. O estoque baixa saldo e saldo reservado da reserva que guardou. O orçamento abre a cobrança Pix.

```json
{ "ordemServicoId": 10 }
```

### `CobrancaGerada`

Uma nova tentativa de pagamento usa outro `cobrancaId`. O QR e o ticket vão em `CobrancaPixDisponivel`, não aqui.

```json
{
  "ordemServicoId": 10,
  "cobrancaId": "ord_mp_1"
}
```

Efeito: de `Finalizada`, a OS vai para `AguardandoPagamento`. Se já está em `AguardandoPagamento`, permanece.

### `PagamentoConfirmado`

```json
{
  "ordemServicoId": 10,
  "cobrancaId": "ord_mp_1"
}
```

Efeito, a partir de `AguardandoPagamento`: a OS vai para `Entregue`.

### `PagamentoRecusado`

Vale para recusa e para expiração. O orçamento cancela a order no Mercado Pago antes de publicar. A OS não ganha endpoint de confirmação manual.

```json
{
  "ordemServicoId": 10,
  "cobrancaId": "ord_mp_1",
  "motivo": "recusado"
}
```

`motivo` é `recusado` ou `expirado`. Efeito, a partir de `AguardandoPagamento`: a OS permanece em `AguardandoPagamento` até uma nova `CobrancaGerada`.

### `OrdemServicoDescartada`

A OS publica em todo descarte, também quando ainda não há orçamento nem reserva.

```json
{ "ordemServicoId": 10 }
```

Efeito: o orçamento cancela o orçamento se existir. O estoque libera a reserva se existir e tira a ordem da fila de pendências. Os dois reconhecem a mensagem quando não há o que desfazer.

## Notificações

O tópico `notificacoes` não tem consumidor de aplicação. A evidência é a própria mensagem no tópico.

`OrcamentoDisponivel` sai junto de cada `OrcamentoGerado`. `servicos` traz só os não rejeitados.

```json
{
  "ordemServicoId": 10,
  "versaoDiagnostico": 1,
  "orcamentoId": "orc-10-1",
  "valorTotal": 150.0,
  "servicos": [
    { "nome": "Troca de óleo", "valorCobrado": 150.0 }
  ]
}
```

`CobrancaPixDisponivel` sai junto de cada `CobrancaGerada`.

```json
{
  "ordemServicoId": 10,
  "cobrancaId": "ord_mp_1",
  "valorTotal": 150.0,
  "qrCode": "00020126...",
  "ticket": "https://www.mercadopago.com/sandbox/ticket/1"
}
```

## Fora deste catálogo

Permanecem dentro de um único serviço, sem tópico: abertura da OS, envio para diagnóstico, rejeição que devolve a OS a `EmDiagnostico`, e a reavaliação interna da fila quando um item entra no estoque. O webhook do Mercado Pago é HTTP (`POST /orcamento/webhook/mercado-pago`), não é mensagem deste catálogo.
