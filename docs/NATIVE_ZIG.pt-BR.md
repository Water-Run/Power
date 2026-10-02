# Fronteira Zig nativa

[English](NATIVE_ZIG.md) · [简体中文](NATIVE_ZIG.zh-CN.md) · [Français](NATIVE_ZIG.fr.md) · [Русский](NATIVE_ZIG.ru.md) · [日本語](NATIVE_ZIG.ja.md) · [한국어](NATIVE_ZIG.ko.md) · [Deutsch](NATIVE_ZIG.de.md) · [Español](NATIVE_ZIG.es.md) · [Italiano](NATIVE_ZIG.it.md) · **Português**

O proprietário retomou a migração de linguagem nativa em 2026-09-10. Os protótipos nativos em `legacy/native` foram migrados para Zig, incluindo seus testes e hosts. Eles continuam um runtime de pesquisa separado: `Power.Core` e `Power.Assets` mantêm seus contratos gerenciados sem dependências e os alvos duplos, e o Unity continua carregando os assemblies gerenciados.

## Interoperabilidade

A biblioteca compartilhada nativa conserva o ponto de entrada versionado `pwr_get_api`, campos escalares de largura fixa, tamanhos de estrutura, tempo inteiro em nanossegundos, handles de geração, buffers de instantâneo pertencentes ao chamador e a tabela de funções. Declarações Zig `extern struct` e convenções de chamada `.c` expressam o ABI binário existente; elas não exigem código-fonte nem cabeçalhos C neste repositório. O executor de experimento P/Invoke em C# na ferramenta de build .NET continua um consumidor desta fronteira. Os protótipos nativos de motor, escape e transmissão continuam APIs Zig internas, em vez de serem acrescentados em silêncio ao esquema de modelo gerenciado ou às capacidades nativas públicas.

A migração precisa conservar as equações, os limites de modelo, os IDs estáveis, os diagnósticos, o rollback de lote, os balanços de energia e de massa e os cenários de regressão. A fidelidade dos protótipos nativos e a calibração de amostra `unverified` não mudam. A verificação nativa é separada da evidência real de Unity Editor, Play Mode e IL2CPP e da conclusão do objetivo de powertrain completo.

## Procedência

A implementação C original pode ser recuperada do commit Git `c342d4c05c9d0b14cae0ce1a85e3f2100dfd7bb3`. Os caminhos e os hashes dos arquivos-fonte estão registrados em `legacy/native/migration-manifest.json`. A porta Zig conserva os avisos originais de direitos autorais e a GPL-3.0-or-later com a exceção de vinculação Unity. Documentos históricos de projeto e de pesquisa conservam suas citações originais; as descrições da era C não descrevem a compilação nova.

O launcher LuaInstaller não usado e o README do seu empacotamento foram retirados em 2026-09-11. Seus caminhos e hashes originais estão no mesmo manifesto e se referem à mesma revisão de fonte. O launcher dependia de uma ponte `power_native` não implementada e nunca fez parte de uma compilação funcional. As operações atuais de CLI e de modelo usam os hosts C#/JSON e Zig existentes, mais o host de ABI P/Invoke da ferramenta de build C#. Propostas Lua em documentos históricos são registros de procedência, não dependências nem requisitos de implementação atuais.

<a id="build-and-maintenance"></a>
## Compilação e manutenção

O compilador está fixado em Zig 0.15.2 em `.zig-version`. O comando `install-zig` da ferramenta de build (em `tools/Build.cs`) usa os [metadados oficiais de download do Zig](https://ziglang.org/download/index.json), com os hashes commitados dos arquivos de distribuição de cada plataforma. A compilação não precisa de tradutor C, cabeçalhos, CMake nem compilação de código-fonte C. O Linux não precisa de libc; o macOS usa o `libSystem` fornecido pelo sistema. A porta foi traduzida uma vez no início e depois dividida em módulos Zig mantidos, com layouts binários compartilhados. Memória, funções matemáticas e operações atômicas usam Zig e as APIs do sistema da plataforma. As verificações de segurança continuam habilitadas em builds ReleaseSafe.

As fontes Zig usam checkouts LF em todas as plataformas. No macOS, a ferramenta de verificação desativa a descoberta do SDK Apple apenas nos subprocessos de build Zig, definindo `DEVELOPER_DIR=/dev/null`. Isso seleciona os stubs do linker Darwin incluídos no Zig e evita a [incompatibilidade do Zig 0.15.2 com o Xcode 26.4 e SDKs mais novos](https://github.com/ghostty-org/ghostty/issues/11991), cujo stub `libSystem` usa alvos arm64e. A seleção de Xcode do sistema não muda; esses alvos nativos não precisam de frameworks Apple nem de cabeçalhos de SDK.

`dotnet run --file tools/Build.cs -- verify` executa a verificação gerenciada e, em seguida, a verificação nativa em série. `native-verify` executa só a parte nativa. A implementação de `native-verify` (em `tools/Build.cs`) rejeita código-fonte e cabeçalhos C/C++, além de código-fonte, bytecode e pacotes Lua. Ela confere a versão fixada do compilador e a formatação, compila a biblioteca e os dois hosts Zig, executa as suítes Zig e a suíte do host de ABI P/Invoke em C#, e compara o experimento eletrotérmico com a fixture de baseline C original. No Linux, também confere que só `pwr_get_api` é exportado publicamente e que a biblioteca não tem símbolos externos não resolvidos.

A comparação de baseline conserva impressões digitais do modelo, unidades, mapeamentos de canais, 11 instantes de amostra e valores físicos. Valores entre cadeias de ferramentas usam tolerâncias absolutas/relativas explícitas; hashes de replay precisam coincidir dentro do mesmo binário. Os relatórios em `artifacts/reports` distinguem execução, resultados de KPI/replay e calibração não verificada. Caches de compilador ignorados e checkouts privados de referência de terceiros não são fonte do repositório.

## Correções dos testes do acervo

Três pontos de entrada antigos de teste retornavam zero mesmo quando a macro `CHECK` falhava. A porta propaga essas falhas. A suíte do motor parava antes numa falha oculta porque a amostra final de carga de 22 Nm podia estar no corte de combustível do limitador de rotação. Esse cenário é conservado como um teste explícito de limitador; a combustão contínua usa uma carga de teste sintética de 32 Nm. A comparação de contrapressão agora parte de um estado de funcionamento compartilhado e confere o incremento analítico de torque de bombeamento antes de comparar a velocidade, evitando a confusão entre partida e estol. Nenhuma equação de motor nem valor de calibração de produção foi alterado. A cobertura de powertrain automático agora também exercita replay, energia do diferencial, brownout e rollback; a compilação CMake antiga omitia esse módulo inteiro.
