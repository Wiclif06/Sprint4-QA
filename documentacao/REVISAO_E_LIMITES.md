# Verificação técnica

Conferência de 26/10/2026.

| Verificação | Resultado |
|---|---|
| Instalação pelo lockfile npm | Concluída |
| Compilação .NET Release | 0 erros e 0 avisos |
| Testes .NET | 151 testes distintos aprovados; 1 Oracle real ignorado |
| Regressão Newman | 33 requisições, 53 asserções, 0 falhas |
| Carga do health check | 330 requisições, concorrências 1/5/20, 0 erros |
| Estresse limitado de GET /api/pets | 3.200 consultas autenticadas; rampa 1/10/50/100 e recuperação com 1; 0 erros |
| p95 no pico / recuperação | 737,82 ms / 20,59 ms |
| Plano manual | 49 casos com objetivos, entradas, passos e saídas; 48 na Sprint 4 e 1 complementar |

A nova regressão usou uma base SQLite descartável em Development. O relatório funcional incluído permanece associado à gravação original; a repetição confirmou os mesmos 33 pedidos e 53 asserções aprovadas. Os testes MongoDB foram executados separadamente contra uma instância real local. Oracle não foi configurado.

O ensaio de estresse confere resposta HTTP e conteúdo dos pets, com p95 abaixo de 1 segundo e zero erros. A emissão é limitada por tempo e quantidade. Não houve saturação nos limites configurados; não se determinou capacidade máxima nem SLA de produção.

As dependências transitivas do Newman emitiram avisos de descontinuação na instalação. A instalação e a execução concluíram. Esses avisos não foram tratados como resultado de auditoria de vulnerabilidades.

## Limites

A verificação cobre a API .NET local e os artefatos de QA. Não inclui execução Oracle, integração clínica disponível, Java, Mobile, Web, SmartCare ou nuvem. O CT018 valida a indisponibilidade esperada da API clínica, com HTTP 503. Os casos manuais não foram marcados como executados por uma pessoa.

Os critérios do Azure corrigiram as saídas de SmartCare, cancelamento, idade e pesos. A franquia foi mantida como caso complementar porque o requisito original está fora das entregas 3 e 4. Consulte a matriz de requisitos para as pendências de licença e apresentação.
