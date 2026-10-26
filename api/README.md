# PetJourney

API ASP.NET Core 8 para gerenciamento de tutores, pets, clínicas, lembretes e eventos clínicos. Usa autenticação JWT, Oracle com Entity Framework Core e MongoDB. Inclui demonstração local com SQLite, Swagger e testes automatizados.

## Equipe

| Integrante | RM |
|---|---|
| Felipe Wiclif Leal da Silva | 563901 |
| Deryk de Souza Queiroz | 563412 |
| Lucas Gonçalves Viana | 563254 |
| Vinicius Paschoeto da Silva | 563089 |

## Executar a demonstração

Requisitos: SDK .NET 8 e acesso ao NuGet. Na raiz do projeto:

```powershell
dotnet restore PetJourney.sln --locked-mode
dotnet run --project src/PetJourneyTutorApi -- --environment Development --urls http://localhost:5187 --Database:Provider Sqlite --Demo:Enabled true
```

Abra [Swagger](http://localhost:5187/swagger). Para entrar, execute `POST /auth/login` e informe uma conta de demonstração:

| Perfil | Usuário |
|---|---|
| Administrador | admin.sistema@petjourney.com |
| Clínica | admin.petfeliz@petjourney.com |
| Veterinário | dr.joao@petjourney.com |
| Tutor | carlos@petjourney.com |

Senha de demonstração: `123456`. Copie o token para **Authorize**. Essas contas só são criadas no modo Demo, em Development com SQLite.

## Estrutura

```text
src/            Domain, Application, Infrastructure e API
tests/          Testes unitários e de integração
docs/           Arquitetura, DER e configuração
scripts/        Verificação e scripts Oracle
postman/        Coleção de requisições
observability/  Configuração do OpenTelemetry
tools/          Utilitário para hashes de senha
../evidencias/  Prints e resultados de QA
```

## Testes

```powershell
./scripts/verify.ps1
```

Para incluir MongoDB real, configure `PETJOURNEY_TEST_MONGO` com a conexão de uma instância de testes. Para Oracle, use `PETJOURNEY_TEST_ORACLE` com um schema de testes já migrado. Sem essas variáveis, os respectivos testes são ignorados.

## Documentação

- [Guia técnico e configuração dos bancos](docs/GUIA_TECNICO.md)
- [Arquitetura](docs/ARQUITETURA.md) · [DER](docs/DER.png)
- [Coleção Postman](postman/PetJourney-Sprint4.postman_collection.json)
- [Evidências de QA](../evidencias/README.md)

Oracle real ainda depende de execução no ambiente de testes.

