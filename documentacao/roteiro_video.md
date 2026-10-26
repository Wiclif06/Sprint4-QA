# Roteiro do vídeo

Mostre a preparação, a configuração, a execução e os resultados dos testes, conforme as páginas 46–47 do enunciado.

1. **Apresentação:** identifique o grupo e o PetJourney. Abra a branch `develop` e os links de entrega.
2. **Preparação:** mostre a API local iniciada, o endereço e a base QA isolada. Importe a coleção e o ambiente no Postman. Oculte senhas e tokens.
3. **Configuração:** mostre pelo menos quatro cenários e suas asserções. Sugestão: login (CT001), acesso sem token (CT006), cadastro e consulta de pet (CT008), atualização (CT009) e exclusão (CT010).
4. **Execução:** rode a coleção inteira no Collection Runner ou execute `npm test` com Newman. Mantenha os resultados da ferramenta visíveis.
5. **Desempenho e estresse:** execute `npm run test:load` e `npm run test:stress`. Mostre as etapas, latências, erros e recuperação. Informe que o ambiente é local com SQLite.
6. **Resultados:** confira o total de requisições, asserções e falhas. Explique o HTTP 503 esperado no CT018 e o HTTP 404 nos casos de isolamento. Se ocorrer falha, investigue antes de concluir a gravação.
7. **Entrega:** publique o vídeo, confira se o link abre sem pedir autorização e atualize README.md e ENTREGA.md.

A coleção contém 19 cenários funcionais em 33 requisições, com 53 asserções. Os números exibidos devem corresponder à execução gravada.

O vídeo já incluído, `evidencias/execucao-qa.mp4`, registra a execução anterior em um painel alimentado pelo Newman, sem áudio. O novo ensaio de estresse não aparece nessa gravação. Para uma nova apresentação, mostre diretamente a ferramenta e os comandos acima.
