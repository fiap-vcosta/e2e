# Saga da ordem de serviço

Coreografia entre `ordem-servico-service`, `orcamento-service` e `estoque-service`. Não há orquestrador. Os nomes, tópicos e payloads estão em [eventos.md](eventos.md).

## Enum e listagem

A declaração abaixo é a ordem do enum. A listagem ordena pelo valor do enum, do maior para o menor, e exclui `Finalizada`, `AguardandoPagamento`, `Descartada` e `Entregue`. Com isso `AguardandoOrcamento` aparece logo acima de `EmDiagnostico`. Dentro do mesmo status, a mais antiga (`recebidaEm`) vem primeiro.

Prioridade da listagem, da mais alta para a mais baixa: `EmExecucao`, `LiberadaParaExecucao`, `AguardandoPeca`, `ChecandoEstoque`, `AguardandoAprovacao`, `AguardandoOrcamento`, `EmDiagnostico`, `Recebida`.

Declaração do enum:

1. `Recebida`
2. `EmDiagnostico`
3. `AguardandoOrcamento`
4. `AguardandoAprovacao`
5. `ChecandoEstoque`
6. `AguardandoPeca`
7. `LiberadaParaExecucao`
8. `EmExecucao`
9. `Finalizada`
10. `AguardandoPagamento`
11. `Descartada`
12. `Entregue`

## Estados

```mermaid
stateDiagram-v2
    [*] --> Recebida
    Recebida --> EmDiagnostico: enviar para diagnostico
    Recebida --> Descartada: descartar
    EmDiagnostico --> AguardandoOrcamento: finalizar diagnostico
    EmDiagnostico --> Descartada: descartar
    AguardandoOrcamento --> EmDiagnostico: OrcamentoFalhou
    AguardandoOrcamento --> AguardandoAprovacao: OrcamentoGerado com sugeridos
    AguardandoOrcamento --> ChecandoEstoque: OrcamentoGerado somente aprovados
    AguardandoOrcamento --> Entregue: OrcamentoGerado sem pendencia
    AguardandoOrcamento --> Descartada: descartar
    AguardandoAprovacao --> EmDiagnostico: rejeicao
    AguardandoAprovacao --> ChecandoEstoque: aprovacao
    AguardandoAprovacao --> Descartada: descartar
    ChecandoEstoque --> LiberadaParaExecucao: EstoqueReservado
    ChecandoEstoque --> AguardandoPeca: EstoqueEmFalta
    ChecandoEstoque --> Descartada: descartar
    AguardandoPeca --> LiberadaParaExecucao: EstoqueReservado
    AguardandoPeca --> Descartada: descartar
    LiberadaParaExecucao --> EmExecucao: execucao parcial
    LiberadaParaExecucao --> Finalizada: execucao completa
    LiberadaParaExecucao --> Descartada: descartar
    EmExecucao --> Finalizada: execucao completa
    Finalizada --> AguardandoPagamento: CobrancaGerada
    AguardandoPagamento --> Entregue: PagamentoConfirmado
```

`AguardandoPagamento` permanece nesse status em `PagamentoRecusado` e numa nova `CobrancaGerada`. `EmExecucao` permanece em `EmExecucao` enquanto a confirmação de execução não concluir todos os serviços.

Finalizar o diagnóstico não escolhe mais o destino sozinho. A OS vai para `AguardandoOrcamento` e publica `DiagnosticoFinalizado`. O destino sai quando `OrcamentoGerado` chega:

- há serviço `Sugerido` → `AguardandoAprovacao`
- não há sugerido e há serviço `Aprovado` → `ChecandoEstoque` e `OrdemServicoAprovada`
- não há serviço sugerido nem aprovado → `Entregue`

Rejeição total, ou parcial que ainda deixe sugerido, volta a `EmDiagnostico` e marca os sugeridos restantes como `Rejeitado`. Uma nova finalização incrementa `versaoDiagnostico` e passa de novo pelo orçamento.

Aprovação total, aprovação parcial sem sugeridos restantes, ou o caminho sem sugeridos e com aprovados, grava `aprovadaEm`, coloca a OS em `ChecandoEstoque` e publica `OrdemServicoAprovada`. A OS não compara saldo. `EstoqueReservado` libera a execução. `EstoqueEmFalta` coloca em `AguardandoPeca`. Uma entrada posterior no estoque pode publicar `EstoqueReservado` para essa mesma aprovação e tirar a OS de `AguardandoPeca`. Isso é espera de peça, não compensação.

Descarte é permitido em `Recebida`, `EmDiagnostico`, `AguardandoOrcamento`, `AguardandoAprovacao`, `ChecandoEstoque`, `AguardandoPeca` e `LiberadaParaExecucao`. Não é permitido em `EmExecucao`, `Finalizada`, `AguardandoPagamento` nem `Entregue`.

A execução concluída coloca a OS em `Finalizada` e publica `OrdemServicoFinalizada`. `CobrancaGerada` leva a `AguardandoPagamento`. `PagamentoConfirmado` leva a `Entregue`. `PagamentoRecusado` não muda o status.

## Caminho feliz

```mermaid
sequenceDiagram
    actor Staff
    actor Cliente
    participant OS as OrdemServico
    participant Adm as Administrativo
    participant Est as Estoque
    participant Orc as Orcamento
    participant MP as MercadoPago

    Staff->>OS: cria a OS
    OS->>Adm: veiculo, cliente e catalogo
    OS->>Est: item
    OS->>OS: Recebida, depois EmDiagnostico
    Staff->>OS: finaliza o diagnostico
    OS->>OS: AguardandoOrcamento
    OS->>Orc: DiagnosticoFinalizado
    Orc->>OS: OrcamentoGerado
    Orc-->>Orc: OrcamentoDisponivel
    OS->>OS: AguardandoAprovacao
    Cliente->>OS: aprova com JWT e token
    OS->>OS: ChecandoEstoque
    OS->>Est: OrdemServicoAprovada
    Est->>OS: EstoqueReservado
    OS->>OS: LiberadaParaExecucao
    Staff->>OS: confirma a execucao
    OS->>OS: Finalizada
    OS->>Est: OrdemServicoFinalizada
    OS->>Orc: OrdemServicoFinalizada
    Est->>Est: baixa o saldo reservado
    Orc->>MP: cria a order Pix
    Orc->>OS: CobrancaGerada
    Orc-->>Orc: CobrancaPixDisponivel
    OS->>OS: AguardandoPagamento
    MP->>Orc: webhook
    Orc->>MP: consulta a order
    Orc->>OS: PagamentoConfirmado
    OS->>OS: Entregue
```

A seta tracejada é publicação no tópico `notificacoes`, sem consumidor.

## Compensação: orçamento falha

```mermaid
sequenceDiagram
    participant OS as OrdemServico
    participant Orc as Orcamento

    OS->>OS: AguardandoOrcamento
    OS->>Orc: DiagnosticoFinalizado
    Orc->>OS: OrcamentoFalhou
    OS->>OS: EmDiagnostico
```

O staff ajusta o diagnóstico e finaliza de novo. A nova publicação usa outro `versaoDiagnostico`.

## Compensação: pagamento recusado ou expirado

```mermaid
sequenceDiagram
    participant OS as OrdemServico
    participant Orc as Orcamento
    participant MP as MercadoPago

    OS->>OS: AguardandoPagamento
    MP->>Orc: webhook
    Orc->>MP: consulta a order
    Orc->>MP: cancela a order
    Orc->>OS: PagamentoRecusado
    OS->>OS: permanece AguardandoPagamento
    Orc->>MP: cria outra order Pix
    Orc->>OS: CobrancaGerada
    Orc-->>Orc: CobrancaPixDisponivel
```

A segunda cobrança tem outro `cobrancaId`. A OS só sai de `AguardandoPagamento` com `PagamentoConfirmado`.

## Compensação: descarte

```mermaid
sequenceDiagram
    actor Staff
    participant OS as OrdemServico
    participant Orc as Orcamento
    participant Est as Estoque

    Staff->>OS: descarta
    OS->>OS: Descartada
    OS->>Orc: OrdemServicoDescartada
    OS->>Est: OrdemServicoDescartada
    Orc->>Orc: cancela o orçamento se existir
    Est->>Est: libera a reserva se existir
```

O descarte vale até `LiberadaParaExecucao`. Quem não tiver orçamento ou reserva só reconhece a mensagem.

## Espera de peça

```mermaid
sequenceDiagram
    participant OS as OrdemServico
    participant Est as Estoque
    actor Staff

    OS->>Est: OrdemServicoAprovada
    Est->>OS: EstoqueEmFalta
    OS->>OS: AguardandoPeca
    Staff->>Est: registra entrada
    Est->>OS: EstoqueReservado
    OS->>OS: LiberadaParaExecucao
```

A fila no estoque atende primeiro a aprovação mais antiga. Enquanto não couber, a OS permanece em `AguardandoPeca`.
