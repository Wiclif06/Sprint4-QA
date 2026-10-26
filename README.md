# PetJourney — QA Sprint 4

Plano de testes e automação de APIs do PetJourney.

- [Azure Boards](https://dev.azure.com/RM563254/PetJourney/_workitems) · [49 casos de teste](azure-boards/casos_publicados.csv)
- [Vídeo da automação](https://github.com/Wiclif06/Sprint4-QA/raw/refs/heads/develop/evidencias/execucao-qa.mp4)
- [Resultados](evidencias/README.md) · [Requisitos e pendências](documentacao/MATRIZ_REQUISITOS_QA.md)

## Executar

Requisitos: .NET SDK 8, Node.js 18 ou superior e npm. Use uma base local de QA.

```powershell
npm ci
./scripts/iniciar_api_qa.ps1
```

Em outro terminal, na raiz do projeto:

```powershell
$env:QA_PASSWORD='123456'
npm test
npm run test:load
npm run test:stress
npm run verify
```

A senha pertence à demonstração local. O script inicia a API em `http://127.0.0.1:5188` com uma nova base SQLite. Encerre com Ctrl+C.

Para usar o Postman ou gravar a apresentação, consulte o [guia de execução](documentacao/EXECUCAO.md) e o [roteiro do vídeo](documentacao/roteiro_video.md).

## Conteúdo

| Pasta | Conteúdo |
|---|---|
| `testes-manuais/` | Plano com objetivos, entradas, passos e saídas esperadas |
| `azure-boards/` | Casos publicados e vínculos com o backlog |
| `postman/` | Coleção e ambiente local |
| `scripts/` | Automação, carga e estresse limitado |
| `evidencias/` | Resultados, prints e vídeo |
| `api/` | API .NET e testes |
| `documentacao/` | Rastreabilidade, execução e conferência da entrega |

## Integrantes

- Felipe Wiclif Leal da Silva — RM563901
- Deryk de Souza Queiroz — RM563412
- Lucas Gonçalves Viana — RM563254
- Vinicius Paschoeto da Silva — RM563089
