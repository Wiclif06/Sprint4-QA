# Requisitos — QA Sprint 4

Fonte: Challenge 2026, páginas 46–47. Conferência em 26/10/2026 com os arquivos e os critérios de aceitação do Azure.

| Requisito | Evidência | Situação |
|---|---|---|
| Plano manual no Azure Boards | 49 casos, IDs 510–558; 48 na Sprint 4 e CT029 complementar | Publicado |
| Objetivos — 20% | Objetivo e requisito vinculados em cada caso | Documentado |
| Entradas controladas — 20% | Massa, pré-condições, datas e fronteiras | Documentado |
| Saídas esperadas — 20% | Resultados específicos conforme critérios dos PBIs | Documentado |
| Procedimentos — 20% | Passos numerados em cada caso | Documentado |
| Coerência com release e tarefas | Rastreabilidade de 34 PBIs; franquia fora das entregas 3/4 | Conferido |
| Pelo menos 4 casos automatizados — 20% | 19 cenários, 33 requisições e 53 asserções aprovadas | Executado |
| Integração, desempenho e estresse de API | Regressão Newman, carga no health check e rampa autenticada de pets | Executado em ambiente local |
| Vídeo: preparação, configuração, execução e resultados | Vídeo funcional incluído; roteiro para nova gravação com carga e estresse | Atualizar a apresentação |
| Repositório público e branch develop | README com Azure e vídeo; arquivos na develop | Publicado |
| Professor administrador do projeto | Douglas Andrade, conta confirmada pelo grupo, no grupo Project Administrators | Confirmado |
| Professor com licença Basic na organização | Conta do professor aparece como Stakeholder | Pendente |

## Correções do plano

- **CT022/CT040/CT042:** contrato, pontuação, faixas, exemplos e desempate do SmartCare conforme PBIs 453, 459 e 462.
- **CT035:** cancelamento somente com mais de 24 horas; fronteiras de 23h59min59s, 24h e 24h00min01s, conforme PBI 434.
- **CT044:** idade pela regra TRUNC/MONTHS_BETWEEN; NULL e futuro retornam NULL e registram os códigos previstos, conforme PBI 470.
- **CT045:** soma dos pesos, subtotais, filtros e tratamento de nulos, conforme PBI 473.
- **CT029:** valores do backlog preservados. O PBI 402 está explicitamente fora das entregas 3 e 4; o caso permanece complementar, sem ser requisito de conclusão desta sprint.

## Pendências de entrega

1. Um administrador da organização deve alterar Douglas Andrade de **Stakeholder** para **Basic**, em Organization settings > Users. A conta conectada de Felipe não disponibiliza essa alteração.
2. Gravar a apresentação atualizada seguindo `roteiro_video.md`, conferir o acesso ao link e enviar a entrega no portal FIAP.

## Alcance da verificação

O PDF exige plano manual e demonstração automatizada. Os casos de outros módulos estão planejados; sua execução depende dos respectivos ambientes. A aprovação dos testes locais não certifica Oracle, Java, Mobile, Web, SmartCare ou produção. O caso Oracle real continua sem execução.
