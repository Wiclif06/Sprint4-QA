# Revisão e limites verificados

## Correções do pacote de QA

- Atualização da referência para a API .NET Sprint 4 revisada.
- IDs obtidos das respostas; fixture isolada e verificação de listas não vazias.
- Lembrete criado no pet da própria execução, em vez de gravar sobre o pet fixo da demonstração.
- Confirmação por GET após criação, edição e exclusão; limpeza na ordem das dependências.
- Isolamento validado em detalhe, lembretes e timeline de outro tutor, com HTTP 404 nos três acessos.
- Diagnóstico detalhado de health usa ADMIN_SISTEMA, conforme autorização da API; readiness retorna 503 quando a clínica está ausente.
- Rastreabilidade substituída por IDs reais: 49 casos ligados aos 34 PBIs.
- Relatório público sanitizado, dependência Newman fixada e scripts propagando falhas.

## Resultados

A API incluída também foi compilada novamente: zero avisos e zero erros. Seus testes tiveram 151 aprovações e um caso Oracle ignorado; o MongoDB foi executado localmente. A restauração com consulta ao NuGet oficial concluiu sem avisos. Relatórios: `evidencias/dotnet-build.log`, `dotnet-restore.log` e `dotnet-testes.json`.

Regressão gravada em 2026-10-26T19:10:14.404Z: 33 requisições, 53 asserções, zero falhas. Fonte: `evidencias/newman-resultados.json`.

Ensaio de carga: 330 chamadas GET /health/live; concorrências 1, 5 e 20; sem erros. Os critérios foram p95 menor que 1 segundo e nenhum erro, definidos no script antes da execução. Este ensaio curto de liveness não determina capacidade máxima, estresse até saturação, desempenho de banco nem SLA de produção.

O vídeo é formado por capturas periódicas da tela durante a execução real, mantendo seus intervalos. Não há áudio nem resultados simulados. O JSON e os scripts permitem conferir os resultados sem depender só do vídeo.

## Limites

Execução em Windows, API .NET Development, SQLite demo novo. Oracle real, integração clínica, Java, Mobile, Web, SmartCare e nuvem não foram executados nesta revisão. Testes manuais humanos não foram declarados como feitos. O PDF pede plano manual e demonstração automatizada; eventual exigência adicional do professor deve ser confirmada.

O alerta do pacote anterior sobre leitura cruzada não se reproduziu na versão revisada: listagem, detalhe, lembretes e timeline foram protegidos nos casos executados. Isso não equivale a auditoria completa de segurança.
