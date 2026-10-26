# Guia técnico

## Autenticação

Faça POST em `/auth/login` com `username` e `password`. Use o token retornado no botão **Authorize** do Swagger ou no cabeçalho `Authorization: Bearer TOKEN`.

Os perfis são ADMIN_SISTEMA, ADMIN_CLINICA, VETERINARIO e TUTOR. Cada operação respeita o escopo do usuário. As contas são configuradas em `Accounts`; gere hashes com `dotnet run --project tools/PasswordHash`.

## MongoDB

Configure variáveis de ambiente antes de iniciar a API:

```powershell
$env:Mongo__ConnectionString = 'mongodb://127.0.0.1:27017'
$env:Mongo__Database = 'petjourney'
$env:Mongo__Required = 'true'
```

Para subir somente um Mongo local com Docker: `docker compose up -d mongo`. O compose é de desenvolvimento e limita a publicação da porta ao loopback. Em ambientes compartilhados, configure autenticação e transporte conforme o ambiente; não versione a conexão com credenciais.

`GET /api/pets/{petId}/eventos` lista eventos; `POST` cria um evento com `type` e `description`; `GET /api/pets/{petId}/eventos/{eventId}` consulta o recurso criado. O autor e a data são definidos no servidor. Administrador de sistema, administrador da clínica e veterinário podem criar eventos apenas de pets acessíveis; tutores podem ler os próprios pets. A resposta 201 aponta para um recurso consultável.

A criação do índice é assíncrona e repetível. O construtor não acessa a rede. Configuração ausente ou indisponibilidade operacional retorna 503 nos eventos autorizados. Pets fora do escopo retornam 404 antes de consultar Mongo. Em produção, Mongo é obrigatório para readiness por padrão; em Development/Testing sem conexão ele é opcional. `Mongo__Required` permite explicitar a política. Uma conexão configurada sempre participa do readiness. `Mongo__TimeoutSeconds` aceita de 1 a 30 segundos, com limite aplicado pelo adapter.

## Consultas e paginação

As listagens principais preservam o JSON em array. Recebem `page` (a partir de 1), `pageSize` (1–100), `sortBy` e `descending`. Valores inválidos retornam 400. Páginas distantes retornam array vazio sem estouro de inteiros. Count e Skip/Take são executados no banco relacional; Mongo usa count/skip/limit no servidor. A ordenação inclui identificador como desempate.

| Rota | Filtros | sortBy |
|---|---|---|
| `/api/pets` | name, tutorId, clinicId, speciesId, breedId, sex (M/F) | id, name, birthDate |
| `/api/lembretes` | petId, type, status, from, to | id, date, type, status |
| `/api/tutores` | name, email, plan | id, name, email |
| `/api/clinicas` | name, status | id, name |
| `/api/especies` | name | id, name |
| `/api/racas` | name, speciesId | id, name |
| `/api/pets/{petId}/eventos` | type | id, date, type |

`from` e `to` são limites inclusivos de data/hora ISO 8601. Filtros textuais respeitam a comparação/collation do banco. Não há expressão arbitrária de ordenação ou SQL fornecida pelo cliente.

As respostas incluem `X-Total-Count`, `X-Page`, `X-Page-Size` e o cabeçalho `Link` com self/first/last e next/prev quando aplicável, preservando os filtros. Recursos individuais e itens de coleções incluem `_links`, com self e relações pertinentes. Links de alteração/exclusão refletem o papel autorizado; as permissões são verificadas novamente no endpoint. Clientes devem tolerar o campo adicional `_links`.

Exemplos:

```text
GET /api/pets?name=Rex&sortBy=name&page=1&pageSize=10
GET /api/lembretes?status=PENDENTE&sortBy=date&descending=true
GET /api/pets/1/eventos?type=Consulta&pageSize=10
```

## Oracle

A conexão é lida de `ConnectionStrings__OracleConnection`. Configure `Jwt__SigningKey` (mínimo de 32 bytes), contas com hashes e `ExternalServices__ClinicalApiHealthUrl` no ambiente. Não coloque segredos no Git.

As migrações estão no projeto Infrastructure. Em um **schema de testes dedicado e vazio**, o operador pode executar:

```powershell
dotnet tool restore
dotnet ef migrations script --idempotent --project src/PetJourney.Infrastructure --startup-project src/PetJourneyTutorApi --output scripts/oracle-gerado.sql
dotnet ef database update --project src/PetJourney.Infrastructure --startup-project src/PetJourneyTutorApi
```

Leia `scripts/preflight-oracle.sql` antes de adicionar restrições em um schema que já contém dados. Os caminhos de criação de schema novo e extensão de banco compartilhado são distintos; não execute os dois sobre o mesmo banco. O teste real não aplica migrações automaticamente nem apaga tabelas:

```powershell
# Configure PETJOURNEY_TEST_ORACLE por um mecanismo seguro para um schema de testes já migrado.
dotnet test tests/PetJourney.IntegrationTests --filter FullyQualifiedName~OracleRealTests --logger trx
```

O teste verifica conexão, ausência de migrações pendentes e leitura das tabelas mapeadas. A execução em Oracle real ainda não foi realizada.

## Observabilidade

`/health/live` verifica o processo; `/health/ready` verifica dependências obrigatórias; `/health` exige ADMIN_SISTEMA e detalha as verificações. Configure a URL de saúde da API clínica; sem ela, readiness fica indisponível. Serilog grava console JSON e arquivos diários. OpenTelemetry exporta traces/métricas para console quando `Telemetry__Console=true`, ou OTLP quando `OTEL_EXPORTER_OTLP_ENDPOINT` está configurado. Instruções do coletor estão em `observability/README.md`.
