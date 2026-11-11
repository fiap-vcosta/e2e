# language: pt
Funcionalidade: Compensações da Saga
  Como sistema de oficina
  Quero cobrir pagamento recusado e descarte
  Para validar as compensações no compose local

  Contexto:
    Dado que o compose e2e está saudável
    E que faço login staff com "admin" e "admin"
    E que preparo catálogo, estoque e veículo para a saga

  Cenário: Pagamento recusado regenera cobrança e mantém AguardandoPagamento
    Quando crio a ordem de serviço com o veículo e o serviço
    E finalizo o diagnóstico
    Então a ordem de serviço fica com status "AguardandoAprovacao"
    Quando aprovo a ordem de serviço como staff
    Então a ordem de serviço fica com status "LiberadaParaExecucao"
    Quando confirmo a execução dos serviços
    Então a ordem de serviço fica com status "AguardandoPagamento"
    Quando envio o webhook de pagamento recusado do Mercado Pago
    Então a ordem de serviço fica com status "AguardandoPagamento"
    E uma nova cobrança Pix foi criada no WireMock

  Cenário: Descarte após reserva libera a OS
    Quando crio a ordem de serviço com o veículo e o serviço
    E finalizo o diagnóstico
    Então a ordem de serviço fica com status "AguardandoAprovacao"
    Quando aprovo a ordem de serviço como staff
    Então a ordem de serviço fica com status "LiberadaParaExecucao"
    Quando descarto a ordem de serviço
    Então a ordem de serviço fica com status "Descartada"
