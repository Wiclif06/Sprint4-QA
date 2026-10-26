# PetJourney — QA Sprint 4

Plano de testes e automação do Challenge 2026, conforme a seção de QA do enunciado, páginas 46–47.

- [Plano no Azure Boards](https://dev.azure.com/RM563254/PetJourney/_workitems/edit/510): 49 casos ligados aos 34 requisitos existentes. Todos os links estão em [casos_publicados.csv](azure-boards/casos_publicados.csv).
- [Entrega na branch develop](https://github.com/Wiclif06/Sprint4-QA/tree/develop).
- [Vídeo da execução real](https://github.com/Wiclif06/Sprint4-QA/raw/refs/heads/develop/evidencias/execucao-qa.mp4).
- [Resultados da automação](evidencias/newman-resultados.json): **33 requisições, 53 asserções, nenhuma falha**, em 2026-10-26T19:10:14.404Z.

## Conteúdo

| Pasta | Conteúdo |
|---|---|
| `testes-manuais` | Objetivos, pré-condições, entradas controladas, passos e saídas esperadas |
| `azure-boards` | IDs publicados, exportação de referência e atualização dos casos |
| `postman` | Coleção e ambiente sem senha nem tokens preenchidos |
| `scripts` | Execução Newman, relatório sanitizado e ensaio de carga |
| `evidencias` | Vídeo, relatório HTML, resultados JSON e carga local |
| `api` | Código .NET revisado usado como alvo de QA, com testes e dependências fixadas |
| `documentacao` | Matriz de requisitos, rastreabilidade e limites da validação |

## Reproduzir a automação

Pré-requisitos: .NET SDK 8 e Node.js 18 ou superior. A API deve usar exclusivamente uma base descartável de QA.

1. Na raiz, execute `npm ci`.
2. No PowerShell, execute `./scripts/iniciar_api_qa.ps1`. O script cria uma base SQLite nova na pasta temporária, inicia a API em `http://127.0.0.1:5188` e informa onde a base foi criada.
3. Em outro terminal, defina `$env:QA_PASSWORD='123456'`. Essa é a senha pública da fixture de demonstração, disponível apenas em modo Development; não é uma credencial de produção.
4. Execute `npm test`. O processo retorna erro se ocorrer falha; os relatórios são atualizados em `evidencias/` sem salvar tokens ou a senha.
5. Execute `npm run test:load` para o ensaio local de carga e `npm run verify` para conferir a estrutura do pacote.
6. Encerre a API com Ctrl+C. A base temporária é mantida para inspeção; uma nova execução do script usa outra base.

No Postman, importe os dois JSONs de `postman/`, informe a senha local e defina `allowWriteTests=true` somente no ambiente QA descartável. Execute a coleção completa, na ordem. Os IDs são obtidos nas respostas; não precisam ser digitados.

## Escopo comprovado

A regressão valida 19 cenários funcionais .NET: autenticação, isolamento entre tutores, CRUD de pets e lembretes, timeline, validação de entradas e health checks. A API clínica permanece propositalmente ausente no CT018; o HTTP 503 é o resultado esperado desse teste. SQLite é usado como banco local de QA. Não houve validação Oracle nem de produção nesta execução.

O vídeo contém capturas periódicas reais do painel alimentado pelo Newman, com os tempos preservados, sem áudio. Mostra preparação, configuração, execução e resultados; o painel não é a interface do Postman. O enunciado de QA não exige narração, YouTube nem gravação sem cortes.

O plano manual está publicado como tarefas no Azure Boards. O PDF exige o plano na plataforma, sem impor Azure Test Plans ou o tipo Test Case. Não foi atribuída execução humana a testes executados por automação. Fluxos dos demais módulos e regras comerciais ausentes estão identificados como dependências.

## Integrantes

- Felipe Wiclif Leal da Silva — RM563901
- Deryk de Souza Queiroz — RM563412
- Lucas Gonçalves Viana — RM563254
- Vinicius Paschoeto da Silva — RM563089

Consulte [ENTREGA.md](ENTREGA.md) para os links e as pendências externas restantes.
