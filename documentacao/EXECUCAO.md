# Execução dos testes

## Preparar o ambiente

1. Instale o SDK .NET 8 e Node.js 18 ou superior com npm.
2. Na raiz do projeto, execute `npm ci`.
3. Execute `./scripts/iniciar_api_qa.ps1` no PowerShell e aguarde a API iniciar em `http://127.0.0.1:5188`.
4. Mantenha esse terminal aberto. O script cria uma base SQLite nova na pasta temporária para cada execução.

A fixture contém os tutores `carlos@petjourney.com` e `ana@petjourney.com`, seus pets e a conta `admin.sistema@petjourney.com`. A senha local é `123456`.

## Newman

Em outro terminal, na raiz, defina `$env:QA_PASSWORD='123456'` e execute:

| Comando | O que verifica | Relatório |
|---|---|---|
| `npm test` | 19 cenários funcionais em 33 requisições e 53 asserções | `evidencias/newman-resultados.json` e `relatorio.html` |
| `npm run test:load` | 330 chamadas ao health check, concorrências 1, 5 e 20 | `evidencias/desempenho.json` |
| `npm run test:stress` | Consulta autenticada de pets, rampa 1/10/50/100 clientes e recuperação com 1 cliente | `evidencias/estresse.json` |
| `npm run verify` | Consistência dos arquivos e resultados salvos | Resumo no terminal |

Os executores retornam erro quando os critérios falham. A regressão cria, altera, consulta e remove somente seus próprios registros. IDs e tokens são obtidos durante a execução. Os relatórios públicos omitem senhas e tokens.

A carga e o estresse usam p95 abaixo de 1 segundo e zero erros como critérios locais. O estresse limita cada etapa a 3 segundos de emissão ou ao teto de requisições, com até 5 segundos por resposta. Confere também se os dados dos pets permanecem corretos. Os limites não representam SLA nem capacidade máxima de produção.

## Postman

1. Importe a coleção e o ambiente em `postman/`.
2. Selecione **PetJourney QA local isolado**.
3. Preencha `password` com a senha da demonstração e `allowWriteTests` com `true`.
4. Confirme `baseUrl=http://127.0.0.1:5188` e execute a coleção completa, na ordem, pelo Collection Runner.
5. Confira as asserções e os códigos HTTP. O CT018 espera HTTP 503 porque a API clínica não está configurada nesse ambiente.

Os casos de isolamento esperam HTTP 404 para dados de outro tutor. Esses códigos esperados não são falhas do teste. Exporte o ambiente com senhas e tokens em branco.

## Escopo

Ambiente Development com SQLite. Oracle, integração clínica disponível, Java, Mobile, Web, SmartCare e nuvem dependem de seus próprios ambientes. A execução automática não representa execução humana do plano manual.

`npm test` e os ensaios sobrescrevem seus relatórios em `evidencias/`. O vídeo e os relatórios devem identificar a execução correspondente.
