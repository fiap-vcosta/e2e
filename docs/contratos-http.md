# Contratos HTTP

Paths públicos usam o nome do serviço sem o sufixo `-service` e sem o prefixo `/api`: `/ordem-servico`, `/estoque`, `/orcamento`, `/administrativo`, `/autenticacao`. Cada serviço declara esse prefixo nas rotas. O API Gateway repassa o path inteiro.

Rotas `/system/{servico}/...` existem só na rede do cluster (DNS do Kubernetes ou nome do container no compose). Não entram no API Gateway nem no HTTPRoute público. Exigem o header `X-Service-Key`. Chave ausente ou diferente responde 401.

O webhook é público e anônimo. A proteção é a validação de `x-signature`.

Corpos públicos que este mapa não redefine continuam os que o serviço já expõe hoje, no path novo. Os corpos internos abaixo são o contrato de leitura para snapshot e para o JWT de cliente.

## Rotas internas

Leitura síncrona na criação da OS e ao adicionar serviço. A OS copia o retorno para o snapshot local e não consulta de novo esses dados no restante da Saga.

### `GET /system/administrativo/clientes/por-documento/{documento}`

Usado pelo `autenticacao-service`. `documento` é CPF ou CNPJ. O administrativo normaliza antes de buscar. Documento inválido responde 400 com `{ "errors": ["..."] }`. Cliente existente responde 200 sem corpo. Ausente responde 404.

### `GET /system/administrativo/clientes/{id}`

```json
{
  "id": 1,
  "nome": "Ada",
  "email": "ada@oficina.dev",
  "tipoDocumento": "Cpf",
  "documento": "86421537090"
}
```

`tipoDocumento` é `Cpf` ou `Cnpj`. A OS grava estes campos no snapshot. O `documento` fica no snapshot para o ownership da aprovação: o claim `documento` do JWT cliente tem de ser o do cliente da OS localizada pelo token opaco.

### `GET /system/administrativo/veiculos/{id}`

```json
{
  "id": 4,
  "idCliente": 1,
  "placa": "ABC1D23",
  "marca": "Fiat",
  "modelo": "Uno"
}
```

A OS usa `idCliente` para buscar o cliente e copia `placa`, `marca` e `modelo` para o snapshot.

### `GET /system/administrativo/servicos/{id}`

Catálogo de mão de obra.

```json
{
  "id": 2,
  "codigo": "OLEO",
  "nome": "Troca de óleo",
  "precoPadrao": 120.0
}
```

A OS copia `id`, `codigo` e `nome`. O `valorCobrado` da OS é o informado na criação ou na adição, e pode diferir de `precoPadrao`.

### `GET /system/estoque/itens/{id}`

```json
{
  "id": 7,
  "codigo": "FILTRO",
  "nome": "Filtro de óleo",
  "unidadeMedida": "Unidade"
}
```

`unidadeMedida` usa o nome do enum: `Unidade`, `Jogo`, `Par`, `Litro`, `Kg`, `mL`, `Frasco`. A resposta não traz saldo, saldo reservado nem preço. A OS copia `id`, `codigo`, `nome` e `unidadeMedida`. A reserva acontece depois, por `OrdemServicoAprovada`.

Ausente em qualquer GET por id responde 404.

## Rotas públicas

JWT staff protege o CRUD de administrativo, estoque e as rotas de staff da ordem de serviço. JWT cliente é RS256, validado pelo JWKS, com claim `documento`.

### `administrativo-service`

| Método | Path |
|--------|------|
| `POST` | `/administrativo/clientes` |
| `GET` | `/administrativo/clientes` |
| `GET` | `/administrativo/clientes/{id}` |
| `GET` | `/administrativo/clientes/{id}/veiculos` |
| `PUT` | `/administrativo/clientes/{id}` |
| `DELETE` | `/administrativo/clientes/{id}` |
| `POST` | `/administrativo/veiculos` |
| `GET` | `/administrativo/veiculos` |
| `GET` | `/administrativo/veiculos/{id}` |
| `PUT` | `/administrativo/veiculos/{id}` |
| `DELETE` | `/administrativo/veiculos/{id}` |
| `POST` | `/administrativo/servicos` |
| `GET` | `/administrativo/servicos` |
| `GET` | `/administrativo/servicos/{id}` |
| `PUT` | `/administrativo/servicos/{id}` |
| `DELETE` | `/administrativo/servicos/{id}` |

### `estoque-service`

O recurso público é `itens`, o mesmo substantivo da rota interna.

| Método | Path |
|--------|------|
| `POST` | `/estoque/itens` |
| `GET` | `/estoque/itens` |
| `GET` | `/estoque/itens/{id}` |
| `PUT` | `/estoque/itens/{id}` |
| `DELETE` | `/estoque/itens/{id}` |
| `POST` | `/estoque/itens/{id}/registrar-entrada` |

### `ordem-servico-service`

Staff:

| Método | Path |
|--------|------|
| `POST` | `/ordem-servico` |
| `GET` | `/ordem-servico` |
| `GET` | `/ordem-servico/{id}` |
| `GET` | `/ordem-servico/tempo-medio-execucao` |
| `POST` | `/ordem-servico/{id}/adicionar-servico` |
| `POST` | `/ordem-servico/{id}/finalizar-diagnostico` |
| `POST` | `/ordem-servico/{id}/aprovar` |
| `POST` | `/ordem-servico/{id}/aprovar-parcialmente` |
| `POST` | `/ordem-servico/{id}/rejeitar` |
| `POST` | `/ordem-servico/{id}/confirmar-execucao` |
| `POST` | `/ordem-servico/{id}/descartar` |

Cliente. JWT cliente e token opaco na query. Os dois acionam os mesmos casos de uso de aprovar e rejeitar do staff. O token não aparece nas respostas de criação nem de consulta. O path do cliente não leva `{id}`; o do staff leva `{id}` inteiro.

| Método | Path |
|--------|------|
| `POST` | `/ordem-servico/aprovar?token=` |
| `POST` | `/ordem-servico/rejeitar?token=` |

A OS chega a `Entregue` ao consumir `PagamentoConfirmado`. Não há rota de staff para confirmar pagamento.

### `autenticacao-service`

| Método | Path | Acesso |
|--------|------|--------|
| `POST` | `/autenticacao/login` | anônimo; corpo `{ "login", "senha" }` |
| `POST` | `/autenticacao/clientes/token` | anônimo; corpo `{ "documento" }` |
| `GET` | `/autenticacao/.well-known/jwks.json` | anônimo |

`POST /autenticacao/clientes/token` consulta `GET /system/administrativo/clientes/por-documento/{documento}` e emite o JWT cliente com claim `documento` e expiração de 1800 segundos. Login staff emite o JWT staff.

### `orcamento-service`

| Método | Path | Acesso |
|--------|------|--------|
| `POST` | `/orcamento/webhook/mercado-pago` | anônimo; validar `x-signature` |

O webhook responde 200 e, em seguida, valida `x-signature` e consulta a order. Assinatura inválida não publica evento de pagamento. `PagamentoConfirmado` ou `PagamentoRecusado` saem depois dessa consulta.
