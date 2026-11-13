# Arquitetura do sistema

Visão dos componentes da oficina na GCP e no compose local. Contratos: [HTTP](contratos-http.md), [eventos](eventos.md). Sequências e estados da Saga: [saga.md](saga.md). Domínio: [dominio/](dominio/). ADRs: [adrs/](adrs/).

## Componentes (GCP)

```mermaid
flowchart TB
  subgraph entrada["Entrada pública"]
    Client["Cliente / Staff / Requestly"]
    Apex["API Gateway + HTTPS LB<br/>vcosta-fiap.online"]
    GKEGW["GKE Gateway<br/>listener https"]
  end

  subgraph gke["GKE Autopilot — namespace tech-challenge"]
    Adm["administrativo-service<br/>/administrativo"]
    Auth["autenticacao-service<br/>/autenticacao"]
    Est["estoque-service<br/>/estoque"]
    OS["ordem-servico-service<br/>/ordem-servico"]
    Orc["orcamento-service<br/>/orcamento"]
  end

  subgraph dados["Persistência"]
    PGAdm["Cloud SQL<br/>administrativo-service-pg"]
    PGEst["Cloud SQL<br/>estoque-service-pg"]
    PGOS["Cloud SQL<br/>ordem-servico-service-pg"]
    FS["Firestore nomeado<br/>autenticacao + orcamento"]
  end

  subgraph msg["Pub/Sub"]
    Topics["Tópicos de domínio<br/>+ notificacoes"]
  end

  subgraph ext["Externo"]
    MP["Mercado Pago Orders"]
  end

  Client --> Apex
  Apex --> GKEGW
  GKEGW --> Adm & Auth & Est & OS & Orc

  Adm --> PGAdm
  Est --> PGEst
  OS --> PGOS
  Auth --> FS
  Orc --> FS

  OS <--> Topics
  Est <--> Topics
  Orc <--> Topics

  Orc --> MP
  MP -.->|webhook x-signature| Orc

  Auth -.->|X-Service-Key /system| Adm
  OS -.->|X-Service-Key /system| Adm & Est
```

Rotas `/system/{servico}/…` não passam pelo Gateway público: só DNS interno do cluster (ou nome do container no compose).

## Componentes (compose local)

```mermaid
flowchart LR
  subgraph host["localhost"]
    R["Requestly / BDD"]
    A8080["administrativo :8080"]
    A8081["autenticacao :8081"]
    A8082["estoque :8082"]
    A8083["ordem-servico :8083"]
    A8084["orcamento :8084"]
    WM["WireMock :8089<br/>fake Mercado Pago"]
    PG["Postgres :5432"]
    FSe["Firestore emulator :8085"]
    PSe["Pub/Sub emulator :8086"]
  end

  R --> A8080 & A8081 & A8082 & A8083 & A8084
  A8080 & A8082 & A8083 --> PG
  A8081 & A8084 --> FSe
  A8082 & A8083 & A8084 <--> PSe
  A8084 --> WM
```

## Saga

Coreografia entre `ordem-servico-service`, `orcamento-service` e `estoque-service` (sem orquestrador). Diagrama de estados, caminho feliz e compensações estão em [saga.md](saga.md).
