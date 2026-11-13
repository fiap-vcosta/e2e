# ADR 006: CI automático × deploy / demo manuais (custo)

**Data:** 12 de novembro de 2026  
**Status:** Aceito  
**Autores:** Victor Costa

## 1. Contexto e Problema

GKE Autopilot, Cloud SQL, load balancers e sidecars Datadog custam por hora enquanto ligados. Automatizar `terraform apply` e `deploy` em todo merge na `main` manteria a nuvem cara no dia a dia. O enunciado acadêmico valoriza pipeline com testes; isso não exige gatilho contínuo na infraestrutura cara.

O problema a ser resolvido é: **O que é automático no CI/CD, e o que permanece manual de propósito?**

## 2. Decisão

| Fluxo | Gatilho | Automático? |
|-------|---------|-------------|
| Lint, testes (gate ≥80%), SonarCloud, `terraform fmt`/`validate` (+ plan em PR quando couber) | PR / push | **Sim** |
| Build + push da imagem no Artifact Registry | Merge em `main` do serviço | **Sim** |
| `tf-apply` / `tf-destroy` (infra e serviços) | `workflow_dispatch` | **Manual** |
| `deploy` no GKE | `workflow_dispatch` | **Manual** |
| Janela completa (`demo-up` / `demo-down`) | `workflow_dispatch` neste `e2e` | **Manual** (orquestra os workflows acima) |

Reusable workflows: repo [`actions`](https://github.com/fiap-vcosta/actions). Camada persistente (`infra-bootstrap`) **só** apply local por owner (sem `tf-apply` de CI).

## 3. Justificativa

* Separar **qualidade contínua** (barata) de **nuvem cara** (sob demanda).
* `demo-up` / `demo-down` tornam a janela repetível sem deixar cluster/SQL ligados overnight.
* Imagens já no Artifact Registry permitem compose/BDD no CI do `e2e` sem provisionar GKE.

## 4. Alternativas Consideradas

* **CD automático em todo merge:** máxima “automação”; rejeitado — sangramento de custo fora da demo.
* **Tudo manual inclusive build de imagem:** pior DX e risco de drift; rejeitado — `build-push` permanece no merge.

## 5. Consequências

### Positivas
* Avaliador vê CI verde + imagens publicadas; o gatilho de nuvem é consciente.
* Processo de demo documentado no README deste repo.

### Negativas / Riscos
* Interpretar “deploy manual” como ausência de CI/CD.
  * *Mitigação:* esta ADR + seção explícita no README; vídeo mostra CI antes do `deploy`.
* Esquecer `demo-down`.
  * *Mitigação:* checklist operacional; destroy obrigatório ao fim da janela.
