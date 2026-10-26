# Massa de dados e execução

A API incluída gera a fixture apenas quando cria um banco SQLite novo em Development com Demo:Enabled=true. O script de início cria um caminho temporário exclusivo; não reutilize uma base com registros desconhecidos.

Carlos e Ana são tutores sintéticos de clínicas diferentes. No preparo, a coleção autentica cada conta, consulta /auth/me e sua lista de pets e registra os IDs retornados. A presença de exatamente um pet por tutor na fixture é verificada para evitar testes de isolamento sobre uma lista vazia.

O pet QA-Aurora e seu lembrete pertencem ao tutor autenticado e são criados pela coleção. Edição é confirmada com GET. O lembrete é excluído antes do pet; GET após cada exclusão deve retornar 404. O último passo verifica que o pet original continua presente. O mês 11/2026 e a data 10/11/2026 são fixos e não dependem do relógio da máquina.

O relatório não grava tokens, cabeçalhos Authorization nem senha. As contas da fixture não são pessoas reais. Credenciais externas nunca devem ser salvas na coleção ou no Git.

Para uma execução manual, seguir a ordem funcional indicada pelo caso e registrar os valores efetivamente retornados. Não marcar Passed sem comparar o valor observado com a saída esperada. Casos de módulos ausentes permanecem bloqueados para execução. A existência do plano atende à preparação, não comprova a execução.
