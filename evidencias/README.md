# Evidências

- `execucao-qa.mp4`: capturas periódicas reais do painel Newman durante configuração, execução e conclusão; sem áudio.
- `newman-resultados.json`: resultados da execução gravada, com códigos HTTP, tempos, corpos não sensíveis e asserções. Tokens omitidos.
- `relatorio.html`: apresentação legível dos mesmos resultados; abrir no navegador.
- `desempenho.json`: amostras de 330 chamadas locais ao liveness, concorrências 1/5/20.
- `estresse.json`: 3.200 consultas autenticadas de pets, até 100 clientes simultâneos e recuperação; zero erros. Produzido após o vídeo funcional.
- `dotnet-testes.json`, `dotnet-build.log` e `dotnet-restore.log`: 151 testes .NET aprovados, um Oracle ignorado, build e restore sem avisos.
- `prints/`: capturas reais do Azure e do painel, com os casos publicados.

Os resultados comprovam o ambiente local descrito, sem confirmar Oracle, nuvem ou execução manual humana.
