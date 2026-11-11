# language: pt
Funcionalidade: Caminho feliz da ordem de serviço
  Como sistema de oficina
  Quero percorrer a Saga até Entregue
  Para validar a coreografia local no compose

  Cenário: OS completa até Entregue com pagamento via WireMock
    Dado que o compose e2e está saudável
    E que faço login staff com "admin" e "admin"
    E que preparo catálogo, estoque e veículo para a saga
    Quando crio a ordem de serviço com o veículo e o serviço
    E finalizo o diagnóstico
    Então a ordem de serviço fica com status "AguardandoAprovacao"
    Quando aprovo a ordem de serviço como staff
    Então a ordem de serviço fica com status "LiberadaParaExecucao"
    Quando confirmo a execução dos serviços
    Então a ordem de serviço fica com status "AguardandoPagamento"
    Quando envio o webhook de pagamento aprovado do Mercado Pago
    Então a ordem de serviço fica com status "Entregue"
