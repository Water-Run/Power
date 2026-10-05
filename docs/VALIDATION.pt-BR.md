# Registro de validação

[English](VALIDATION.md) · [简体中文](VALIDATION.zh-CN.md) · [Français](VALIDATION.fr.md) · [Русский](VALIDATION.ru.md) · [日本語](VALIDATION.ja.md) · [한국어](VALIDATION.ko.md) · [Deutsch](VALIDATION.de.md) · [Español](VALIDATION.es.md) · [Italiano](VALIDATION.it.md) · **Português**

## 2026-10-05: Rail de combustível líquido alimentado por bomba

A verificação serial exigida passa localmente em Windows x64/.NET 10.0.12. Os testes Standard são executados em .NET 10; Unity Editor/Play/Player/IL2CPP real segue não verificado.

`dotnet run --file tools/Build.cs -- verify`

| Identifier | Value |
|---|---|
| managed_checks | 378/378 |
| standard_checks_on_dotnet | 291/291 |
| actual_mcp_groups | 41/41 |
| laboratories | 38 |
| zig_tests | 16/16 |
| native_model_cs_groups | 6 |
| original_baseline_values | 176 |
| original_baseline_maximum_error | 0 |
| asset_version | power.asset.v26 |
| v25_fixture_sha256 | df400d7e72a2375c6b6185e7206162f2dd8f74ecd52c1862b0bedca6c03ba6dc |
| verification_log_sha256 | 2a2c3aa19317c8251c970a4b98a169b402d1855838e7fb79831d273593bfdf29 |

| Identifier | pump-fed-liquid-cylinder | pump-fed-needle-cylinder |
|---|---|---|
| duration_s | 0.6 | 0.6 |
| boundaries | 65 | 65 |
| states | 39 | 48 |
| step_ns | 50000 | 10000 |
| fingerprint | 22cbbe4a983098f4 | 782b15c21ad211c2 |
| final_state_hash | 4004b74f7f1d8c22 | e322d7a4bd72088c |
| source_sha256 | 1bc97e3f985567f93fd4ace4006307ab571d2e10850e1bd75a1537ed6f0954bb | 3a801026e05e9e6cb67d298145ac20aea9948ab28e2c4552f8dee0d4f1eb4cce |
| report_sha256 | 9869c20db74d4bb1dd22f5b3a924402001ca29451acee7fcc72b9188f25bf437 | 16ac066d9f9e268b98d874aba37f75efeb6b1fa1034b6b06562899f20f67fbbb |
| max_sampled_energy_j | 4.160256139584817e-09 | 2.0303104975027964e-08 |
| max_sampled_mass_kg | 9.215718466126788e-19 | 2.439454888092385e-18 |
| max_sampled_fuel_kg | 1.0486267887008238e-18 | 2.825701912040346e-18 |
| max_sampled_hydraulic_volume_m3 | 1.523666688322677e-21 | 3.763671787116276e-21 |

- `artifacts/reports/rail-feed-integrated-2026-10-05.log`
- `artifacts/reports/rail-feed-evidence-2026-10-05.json`
- `artifacts/reports/rail-feed-schema-audit-2026-10-05.json`

Seis grupos físicos/transacionais, dois de ativos, três integrados e dois cenários MCP reais cobrem a alimentação. Energia térmica/química entrante se acumula com a energia da fronteira gasosa; pressão é armazenada uma vez. O KPI usa o limite analítico da bomba de 920541.8 Pa em vez da pressão inicial.

A fonte é uma fronteira externa explícita, não um tanque finito modelado. Esgotamento, eficiência/regulação da bomba, perdas de linhas, cavitação, propriedades dependentes da pressão e spray de volume finito seguem abertos. Parâmetros `unverified`; isso não comprova calibração OEM ou aceitação real Unity Editor/Play/Player/IL2CPP.

O checkpoint AT anterior também passa CI Windows, Linux e macOS na revisão registrada. Essa execução não verifica estas mudanças de alimentação.

`0d5a98583723b39e5a0b7723d06e988ebea1a425` · [CI 37259428892](https://github.com/Water-Run/Power/actions/runs/37259428892)

[PUMP_FED_FUEL.pt-BR.md](PUMP_FED_FUEL.pt-BR.md)

## 2026-10-05: Realimentação AT hidráulica e controle de pressão

A verificação serial exigida passa em Windows x64/.NET 10.0.12. A compilação Release não tem avisos ou erros. A aceitação real Unity e novas verificações Linux/macOS continuam não confirmadas.

`dotnet run --file tools/Build.cs -- verify`

| Identifier | Value |
|---|---|
| managed_checks | 367/367 |
| standard_checks_on_dotnet | 283/283 |
| actual_mcp_groups | 39/39 |
| laboratories | 36 |
| zig_tests | 16/16 |
| native_model_cs_groups | 6 |
| original_baseline_values | 176 |
| original_baseline_maximum_error | 0 |
| asset_version | power.asset.v25 |
| v24_fixture_sha256 | 6d6dc0f3b17ae1dfdcda59fc45ca7db63d260fb954c8c51567f9b00bab783ecc |

| Identifier | controlled-hydraulic-ravigneaux | controlled-fired-hydraulic-ravigneaux |
|---|---|---|
| duration_s | 4.5 | 2.2 |
| boundaries | 451 | 221 |
| states | 99 | 122 |
| confirmed_range | 1 | 4 |
| fault | 0 | 0 |
| lockup_state | 0 | 2 |
| max_sampled_energy_j | 7.059115887386724e-7 | 2.7647047318168916e-7 |
| max_sampled_hydraulic_volume_m3 | 1.7499700690273845e-18 | 2.7681036716270535e-18 |
| fingerprint | ceb522c56530be00 | 9fa63e406ccae528 |
| final_state_hash | 7ff1919e1f504740 | 958185fe86764b63 |
| source_sha256 | 9cac20ed7a79a2b9dd30f630adf5c6b3ce1f5dbbe4fa837eba3f0465cf67855b | cd4bd02357168532793462903670f9ec59e0c47edcd24986adc71c27bb4e88af |

- `artifacts/reports/at-control-final-2026-10-05.log`
- `artifacts/reports/at-control-evidence-2026-10-05.json`
- `artifacts/reports/at-control-schema-audit-2026-10-05.json`
- `artifacts/reports/at-controller-probe-2026-10-05.log`

8 grupos físicos/transacionais cobrem todas as marchas à frente/ré, pressão/contato/travamento reais, propriedade das válvulas, relógios e limites PI. Falhas incluem perda de alimentação, dreno bloqueado, tempo de aplicação/liberação, intertravamento de direção e perda do travamento confirmado. 2 grupos de assets preservam rotas tipadas e rejeitam downgrades reassinados. 3 grupos integrados e 2 cenários MCP reais correspondem em cada escalar e hash de estado.

Partida em ré e recuperação seguem Applying em 500 ms e são confirmadas antes de 800 ms dentro do tempo declarado. Os testes seguem travamento real, não atraso fixo. Drenagem segura não remove bloqueio físico. O grafo motor/planetas/atuadores/controle preserva 122 estados no limite inalterado de 128.

Os parâmetros continuam `unverified`. Coordenação de torque ECU, sensores/válvulas detalhados, falhas completas do veículo, calibração OEM e aceitação real Editor/Play/Player/IL2CPP continuam pendentes.

[AT_CONTROL.pt-BR.md](AT_CONTROL.pt-BR.md)


## 2026-10-02: alimentação hidráulica AT compartilhada e acionamento dinâmico por pistão

O serial exigido `dotnet run --file tools/Build.cs -- verify` passa localmente
em Windows x64/.NET 10.0.12: **354/354** verificações gerenciadas, **273/273**
verificações de assembly Standard hospedadas no .NET 10, **37/37** grupos MCP
reais, **16/16** testes Zig e seis verificações C# de modelo nativo. A compilação
Release informa zero avisos/erros. Todos os **34** laboratórios passam e todos os
**176** valores de baseline originais correspondem exatamente. As auditorias de
C/C++/Lua permanecem vazias. A aceitação real do Unity e a nova aceitação em
Linux/macOS permanecem não verificadas.

Arquivos de evidência:

- `artifacts/reports/at-actuation-closed-2026-10-02.log`: execução serial completa.
- `artifacts/reports/at-actuation-evidence-2026-10-02.json`: resumos de log/relatório/fonte,
  estado de pressão/curso/contato, trabalho da bomba e limites completos de inventário/energia.
- `artifacts/reports/at-actuation-schema-audit-2026-10-02.json`: todos os 34 documentos
  validam com jsonschema 4.25.1.
- `artifacts/reports/at-finer-10000.json` e `at-finer-5000.json`: referências em
  combustão acopladas mais finas, com os documentos de autoria retidos de forma explícita.

Seis grupos físicos/transacionais verificam a redução imutável do grafo ordinário,
a capacidade real de pressão/curso/contato, o refinamento RK4 simultâneo e independente
de bomba-pressão-movimento, o inventário varrido completo, o trabalho da bomba e o
calor encaminhado, seis ramos de alimentação comum, erros explícitos de
unidades/curso/referência/canal, cancelamento, rollback tardio, ramificações
independentes e avanço sem alocação. A verificação térmica de um único atuador
deduz de forma independente o calor de arrasto do eixo a partir do trabalho da
fonte, da variação cinética do eixo e do trabalho da bomba. As áreas dianteira/traseira
permanecem explícitas; os exemplos usam 0.001/0 m2 e, portanto, retêm o volume
varrido dianteiro no inventário.

Três grupos de integração e dois cenários MCP reais novos preservam cada escalar
e cada hash de estado em **201** limites do trem de torque de cinco ramos e **87**
limites em combustão de seis ramos. As impressões digitais do auxiliar e do JSON
coincidem. Os canais de engate anteriores estão ausentes; só as entradas reais de
enchimento/drenagem comandam a pressão e o movimento. A capacidade inicial nula das
pastilhas permanece zero apesar de um comando de aplicação. As travas físicas são
confirmadas nos quatro caminhos à frente de subida/descida. A trava em combustão
também usa o movimento real do pistão. Tanto a energia de pressão quanto a energia
de pistão/retorno/pastilha/batente permanecem na transação completa e no balanço
global de energia.

A comparação mais grossa do estado final em 40/20/10 us não foi monótona: os erros
normalizados em relação a 10 us foram `1.74505e-5` em 40 us e `2.19913e-5` em 20 us.
Esse critério reprovado não é tratado como resultado de convergência aprovado. Uma
comparação mais fina **20/10/5-us** entra numa faixa de erro decrescente: em relação
a 5 us, o máximo normalizado sobre oito saídas de motor/veículo/pressão/curso/trabalho/calor
cai de **2.01463e-5** em 20 us para **6.51409e-6** em 10 us. Todas as execuções de
referência cumprem os KPIs físicos e o replay exato no próprio runtime. Nenhuma ordem
global de convergência nem precisão de OEM é inferida a partir de entregas híbridas prescritas.

| Grandeza final | Trem de cinco ramos (2 s) | Trem em combustão de seis ramos (0.8 s) |
|---|---:|---:|
| Velocidade do motor/fonte | 102.2242615437 rad/s | 36.2608726849 rad/s |
| Velocidade do veículo | 8.5186884620 rad/s | 13.5978272568 rad/s |
| Pressão de linha | 1.1963842937 MPa | 1.0666023745 MPa |
| Trabalho da bomba | 117.4226709117 J | 37.6491650676 J |
| Estados reportados | 83 | 106 |
| Resíduo máximo de energia amostrado | `1.64680e-7 J` | `1.29307e-8 J` |
| Resíduo máximo de inventário varrido | `1.18076e-18 m3` | `2.94767e-19 m3` |
| Resíduo máximo de fase de engrenagem | `5.68434e-14 rad` | `1.42109e-14 rad` |
| Impressão digital | `f61d582874bd086b` | `4e83efb34de63922` |
| Hash final | `9307fb49117dad0a` | `70e00a107ef1ca6d` |

Os valores SHA-256 das fontes são
`119008206e0d1a5372ec399665d30669ac41bbf2f0fbc0c27c56bf3bc90c8f02`
(trem de torque) e
`9659b282454202857b16ee2dbc0226f30b79549fedab88b4c79f92c6c6e66801`
(em combustão). A planta densa em combustão cabe no limite existente de 128 estados,
com 106 estados, sem remover estado de motor, conversor, pressão ou atuador. Os
registros v24 existentes bastam; nenhum formato de asset novo é introduzido. A suíte
MCP real de 37 cenários permanece limitada a 300 s, incluindo estas plantas acopladas maiores.

Parâmetros e mapas permanecem `unverified`. As programações de válvula são prescritas;
a realimentação AT completa, a coordenação de torque da ECU, o comportamento medido do
corpo de válvulas, os modelos de vedação/cavitação/aeração/temperatura e a calibração
de OEM permanecem em aberto. Importações e reprodução preparadas no Studio não
estabelecem aceitação real de Editor/Play/Player/IL2CPP. O objetivo completo permanece
inacabado. Veja [AT_HYDRAULIC_ACTUATION.md](AT_HYDRAULIC_ACTUATION.pt-BR.md).

## 2026-10-02: giro absoluto dos planetas, inércia orbital e quatro engrenamentos físicos

O serial exigido `dotnet run --file tools/Build.cs -- verify` passa localmente
em Windows x64/.NET 10.0.12: **345/345** verificações gerenciadas, **267/267**
verificações de assembly Standard hospedadas no .NET 10, **35/35** grupos MCP
reais, **16/16** testes Zig e seis verificações C# de modelo nativo. A compilação
Release informa zero avisos/erros. Todos os **32** laboratórios passam e todos os
**176** valores de baseline históricos correspondem exatamente. As auditorias de
C/C++ e Lua permanecem vazias. A aceitação real do Unity e a nova aceitação em
Linux/macOS permanecem não verificadas.

Arquivos de evidência:

- `artifacts/reports/resolved-planets-endpoint-2026-10-02.log`: execução serial completa.
- `artifacts/reports/resolved-planets-evidence-2026-10-02.json`: resumos de log/relatório/fonte,
  geometria, energias de giro/órbita, resíduos e diagnósticos numéricos.
- `artifacts/reports/resolved-planets-schema-audit-2026-10-02.json`: 32 documentos
  válidos e oito casos malformados de engrenamento do porta-satélites rejeitados pelo jsonschema 4.25.1.
- `artifacts/reports/resolved-planet-failure-2026-10-02.json` e
  `resolved-planet-refinement-failure-2026-10-02.json`: traços anteriores à correção, preservados.

Oito grupos físicos/transacionais verificam razões com sinal relativas ao porta-satélites,
matrizes de massa independentes no espaço de aceleração, geometria rígida de passo,
agregação de massa/giro por planeta e inércia orbital, as quatro inércias refletidas
à frente e em ré, momento angular, potência de reação de engrenamento somada nula,
impulso/calor de captura do porta-satélites, sobremarcha carregada de 20 segundos,
erros de unidade/empacotamento/referência, cancelamento, rollback tardio, ramificações
e avanço/releitura sem alocação. Dois grupos portáteis retêm razões com sinal,
porta-satélites e armazenamento de giro completos, rejeitam registros malformados e
rebaixamentos v23 reassinados pelo resumo, e reproduzem o grafo reduzido v23 autêntico.
Seu SHA-256 é
`f2bd390e8ecf013e5843ed3352ccf2a2828133b541fc61d7927995f8ac1dc2e2`;
a impressão digital `63d28eb32bc4cfb2` e o replay atualizado em todos os limites permanecem intactos.

A execução inicial do trem de torque parou após **1.4033 s**. O resíduo de velocidade
sol grande/planeta externo era `1.2406076e-11 rad/s`, perto do limite inalterado
`1.2406654e-11 rad/s`; o erro de fase era zero e o resíduo de energia
`5.26143e-10 J`. Só o refinamento de Schur relativo adiou a parada para **1.4494 s**.
A solução livre de ponto médio agora impõe `G v_next=0` usando
`v_next=2 v_mid-v_old`, em vez de refletir repetidamente o arredondamento precedente.
Em estados antigos exatamente compatíveis, isso é a restrição ordinária de ponto médio nulo.
Todas as mudanças usam multiplicadores reais de resposta de força, que se acumulam nas
reações médias. Três refinamentos relativos limitados melhoram respostas de força pequenas;
o rascunho é de propriedade da simulação ou local ao construtor, e os fatores compilados
permanecem imutáveis. Os grafos existentes conservam o comportamento precedente de
projeção/replay. Nenhuma tolerância de inércia ou de velocidade/fase foi reduzida ou
aumentada para aprovar o caso.

Três grupos de integração e dois cenários MCP reais novos coincidem em cada escalar
e hash de estado em **201** limites do trem de torque e **87** limites em combustão/conversor.
As impressões digitais do auxiliar e do JSON plano coincidem. Giro absoluto interno/externo,
armazenamento orbital do porta-satélites, todas as reações de engrenamento e os balanços
térmicos completos de atrito/conversor permanecem observáveis. Revisões do agente,
cancelamento, reparo de razão inválida e ramificações independentes em neutro permanecem cobertos.

| Grandeza final | Trem de torque (2 s) | Trem em combustão (0.8 s) |
|---|---:|---:|
| Velocidade do motor/fonte | 117.3118882900 rad/s | 40.9899766814 rad/s |
| Velocidade do veículo | 9.7759906908 rad/s | 15.3712412555 rad/s |
| Velocidade absoluta do planeta interno | -469.2475531599 rad/s | -204.9498834069 rad/s |
| Velocidade absoluta do planeta externo | 156.4158510533 rad/s | 122.9699300441 rad/s |
| Energia de giro dos planetas | 23.3037873338 J | 12.2863030022 J |
| Energia orbital dos planetas | Quase zero com o porta-satélites retido | 15.4891426738 J |
| Estados reportados | 27 | 41 |
| Resíduo máximo de energia amostrado | `4.01224e-9 J` | `3.00179e-9 J` |
| Resíduo máximo de fase de engrenagem | `1.13687e-13 rad` | `2.84217e-14 rad` |
| Resíduo máximo de velocidade de engrenagem | `3.97904e-13 rad/s` | `2.27374e-13 rad/s` |
| Impressão digital | `d3010be4b33fdbaf` | `faf9384667ece88d` |
| Hash final | `8e1da00bda34941b` | `e606196f7b345c99` |

O trem em combustão queima **38.0629762199 mg** e libera **1674.7709536768 J**.
Em relação a uma referência de estado final de 12.5 us, o erro máximo normalizado
entre velocidades de motor/veículo/planetas e saídas de trabalho/calor cai de `1.13450e-6`
em 50 us para `1.04173e-6` em 25 us. Isso é refinamento limitado através de entregas
híbridas prescritas; nenhuma ordem global de convergência é reivindicada.

Os três pares síncronos declarados usam raio da coroa **0.1 m**, massas por planeta
interno/externo **0.3/1 kg** e inércias de giro **0.000015/0.0005 kg m2**.
A estrutura do porta-satélites **0.03 kg m2** recebe inércia orbital explícita
**0.0184375 kg m2**. Os valores SHA-256 das fontes são
`c45d90282097303da44c9405eb6945637b578db84617f0b749efc58ed879f0a5`
(torque) e
`e71d9ea4755826b1dcd916fd0157827b2e845dd48506e32f6291674d56f315f3`
(em combustão). Geometria, massas e mapas permanecem `unverified`. Engrenamentos
síncronos rígidos não estabelecem compartilhamento de carga de fabricação, conformidade
dos dentes, lubrificação/perdas, hidráulica/controle AT completos, identidade de OEM
nem comportamento calibrado de veículo. Os casos de Studio preparados incluem as três
portas de engrenamento do porta-satélites; não estabelecem resultados reais de
Editor/Play/Player/IL2CPP. Veja [RESOLVED_PLANETS.md](RESOLVED_PLANETS.pt-BR.md).

## 2026-10-02: caminhos compostos Ravigneaux e composição do conversor em combustão

O serial exigido `dotnet run --file tools/Build.cs -- verify` passa localmente
em Windows x64 com .NET 10.0.12: **332/332** verificações gerenciadas, **257/257**
verificações de assembly Standard hospedadas no .NET 10, **33/33** grupos MCP
reais, **16/16** testes Zig e seis verificações C# de modelo nativo. A compilação
Release informa zero avisos/erros. Todos os **176** valores de baseline originais
correspondem exatamente; os inventários de fontes C/C++ e Lua permanecem vazios.
Todos os **30** laboratórios passam. A aceitação real do Unity e a nova aceitação
em Linux/macOS permanecem não verificadas.

Arquivos de evidência:

- `artifacts/reports/ravigneaux-confirmed-2026-10-02.log`: execução serial completa.
- `artifacts/reports/ravigneaux-evidence-2026-10-02.json`: resumos de log/fonte/relatório,
  limites de estado, balanços numéricos e limitações declaradas.
- `artifacts/reports/ravigneaux-schema-audit-2026-10-02.json`: todos os 30 documentos
  passam no jsonschema 4.25.1; oito casos de topologia malformada são rejeitados.
- `artifacts/reports/ravigneaux-phase-failure-2026-10-02.json`: diagnóstico isolado
  antes da compensação de coordenadas.

Seis grupos físicos/transacionais verificam a matriz de massa livre 2x2 reduzida
de forma independente, as quatro inércias refletidas à frente e em ré, as reações
dos membros e a potência de reação de engrenagem somada nula, o impulso/calor de
captura do freio do porta-satélites, a sobremarcha carregada longa, unidades/geometria/portas
malformadas, cancelamento, rollback tardio, ramificações independentes e
avanço/releitura sem alocação. Dois grupos portáteis verificam registros completos
do porta-satélites, contagens tipadas, referências duplicadas e a rejeição de
rebaixamento v22 forjado. O SHA-256 do fixture DCT controlado v22 autêntico é
`db90df5fa9ca90067341abdcaf8c3a4a38316794a4b3c8ecc7c89a852375c24e`;
a impressão digital `72122eae163df98e` e o replay atualizado exato permanecem intactos.

A sobremarcha carregada longa parou inicialmente após o último limite confirmado de
**2.9668 s**. O resíduo de fase do pinhão duplo era `-8.27754e-10 rad`, perto do
limite `8.27906e-10 rad`, enquanto o resíduo de velocidade era `-1.77991e-12 rad/s`
contra `5.21235e-11 rad/s`. Os modelos compostos agora usam acumulação transacional
de coordenadas compensadas. A mesma verificação analítica de carga de **20 segundos**
passa sem aumentar tolerâncias de fase/velocidade nem projetar posições. O estado de
correção é copiado, entra no hash e sofre rollback com cada intervalo; os modelos
anteriores sem composto retêm o comportamento existente de integração/hash.

Três grupos de integração e dois cenários MCP reais retêm cada saída e hash de estado
em **201** limites do trem de torque e **87** limites em combustão/conversor.
Os cinco elementos de atrito executam fisicamente as entregas à frente prescritas de
subida/descida; a aceitação do comando não é tratada como trava concluída. O
armazenamento térmico corresponde à soma de todo o calor de embreagem/conversor
encaminhado. Verificações de revisão, cancelamento, rejeição limitada de entrada e
ramificações independentes em neutro permanecem cobertos.

| Grandeza final | Trem de torque (2 s) | Trem conversor em combustão (0.8 s) |
|---|---:|---:|
| Velocidade de entrada/motor | 119.6764605858 rad/s | 42.4589002302 rad/s |
| Velocidade do veículo | 9.9730383821 rad/s | 15.9220875863 rad/s |
| Estados reportados | 21 | 35 |
| Calor de atrito nos cinco elementos de faixa | 546.0473656082 J | 117.6775595348 J |
| Resíduo máximo de energia amostrado | `1.87947e-9 J` | `1.96445e-9 J` |
| Resíduo máximo de fase de engrenagem | `3.19744e-14 rad` | `1.06581e-14 rad` |
| Diferença máxima do balanço térmico | `1.52568e-10 J` | `2.41471e-9 J` |
| Impressão digital | `63d28eb32bc4cfb2` | `7c207499d9050fc6` |
| Hash final | `4eb3c05eeef034c6` | `0cbed5442969c06f` |

O trem em combustão queima **37.7116554560 mg**, libera **1659.3128400630 J**,
dissipa **66.4214778376 J** no conversor e **125.9622150447 J** na trava. Em relação
à referência de estado final de 12.5 us, o erro máximo normalizado entre velocidade
de motor/veículo e três saídas de trabalho/calor cai de `9.92882e-7` em 50 us para
`7.19762e-7` em 25 us. Isso é refinamento limitado através de eventos híbridos
prescritos, não uma ordem global de convergência reivindicada.

Os valores SHA-256 das fontes são
`8063388dbd341fd16b05ab971f6c2cc23d87ebfee3681da9d1ffcf95fffa52a0`
(trem de torque) e
`55587bff2a2e5f33c6e876ed0d539615cfee1daf938d72dc4946c39a36a74d44`
(trem em combustão). Parâmetros e mapas de pesquisa permanecem `unverified`. Giro
interno dos planetas, perdas detalhadas de engrenagem, hidráulica/controle AT,
coordenação da ECU, topologia exata de OEM e amostras medidas permanecem em aberto.
Os testes de importação/reprodução do Unity estão preparados como casos individuais
de recurso, incluindo erros anteriores de contagem de argumentos já reparados; nenhum
resultado de Editor/Play ou IL2CPP é inferido da execução gerenciada.
Veja [RAVIGNEAUX_TRANSMISSION.md](RAVIGNEAUX_TRANSMISSION.pt-BR.md).

## 2026-10-01: sincronização DCT amostrada, entrega escalonada e controle em combustão combinado

O serial exigido `dotnet run --file tools/Build.cs -- verify` passa localmente
em Windows x64 com SDK 10.0.401/runtime 10.0.12: **321/321** verificações gerenciadas,
**249/249** verificações de assembly Standard hospedadas no .NET 10, **31/31** grupos
MCP reais, **16/16** Zig e **6/6** verificações de ABI do C#. A compilação Release
informa zero avisos/erros. Todos os **176** valores de baseline históricos correspondem
exatamente e a auditoria de C/C++/Lua está vazia. A aceitação real do Unity e a nova
aceitação em Linux/macOS não estão verificadas.

Arquivos de evidência:

- `artifacts/reports/tcu-final-2026-10-01.log` — execução serial completa.
- `artifacts/reports/tcu-evidence-2026-10-01.json` — escopo, resumos de log/fonte,
  marcha/falha/fase reais, limites de estado e resíduos de fase e energia.
- `artifacts/reports/tcu-schema-audit-2026-10-01.json` — todos os **28** laboratórios
  passam no jsonschema 4.25.1; **10** casos de controlador malformados são rejeitados.
- `artifacts/reports/controlled-dual-clutch.json` e
  `artifacts/reports/controlled-fired-dual-clutch.json` — relatórios completos.

Oito grupos físicos/de controle verificam os sete caminhos confirmados, a pré-seleção
sem carga, a entrega escalonada e exclusiva de tração, a ré com sinal, o bloqueio de
sentido de movimento, o tempo esgotado de sincronização, a perda persistente da trava
confirmada, a recuperação em neutro/novo alvo, as verificações integrais de entrada
estática/imediata/programada, a posse de dez canais, a amostragem inteira, rotas
imutáveis, cancelamento/rollback tardio, ramificações e alocação zero. Uma execução
controlada de 20 segundos verifica a preservação estrita da fase de marcha e a energia.
Os desfechos do controlador permanecem falhas observáveis; não são tratados em silêncio
como troca bem-sucedida nem como falha numérica.

A sincronização carregada longa expôs primeiro o arredondamento acumulado de coordenadas
em 3.7688 s. O resíduo de velocidade de marcha estava dentro do limite, enquanto o erro
de fase normalizado `-3.2883917811e-10` excedia marginalmente o limite existente
`3.2882809435e-10`. Os modelos controlados novos agora acumulam coordenadas de velocidade
de ponto médio com correção compensada transacional. As tolerâncias estritas não foram
aumentadas e nenhuma posição de estado foi projetada numa razão escolhida. Os modelos
anteriores conservam o comportamento precedente de integração/hash; a compensação nova
é copiada, entra no hash e sofre rollback em cada intervalo real e especulativo.

O limite explícito de estados reportados é **128**. Os limites de nós/componentes
permanecem **32/64**; o comportamento serial de compilação/teste não muda. Um modelo
de 32 rotores/64 estados RL compila e avança exatamente em 128 estados reportados.
Modelos com mais gás rastreado, filme, injeção e controlador rejeitam a capacidade.
A composição completa em combustão/DCT/controlador agora cabe em 70 estados, em vez
de omitir estado de motor/controle para caber no limite precedente. Isso não estabelece
desempenho esparso/Burst nem do Unity.

Dois grupos portáteis verificam rotas v22, períodos, rampas, tolerâncias, posse e
histórico de falha/replay, contagens tipadas limitadas, referências/unidades erradas,
registros ausentes e rebaixamentos v21 forjados. O SHA-256 do fixture de grafo v21
autêntico é
`706618f7d32a5ef4b8a18ca801d2c3ba0b293eac03b584d88d584e9290a38437`;
a impressão digital `7466a75b99fbfd78` e o replay atualizado no mesmo runtime permanecem intactos.
Fixtures anteriores e impressões digitais físicas são evidência de regressão retida.

Três grupos de integração e dois cenários MCP reais verificam estado amostrado,
pedido inteiro, orientação acionável de canal pertencente, revisões, lotes
cancelados/reprovados e ramificações independentes de comando de marcha. Todos os
**421** limites do trem controlado e **83** limites em combustão controlada de
relatório/portátil/MCP coincidem exatamente.

| Grandeza final | Trem controlado (4.2 s) | Trem em combustão controlado (0.8 s) |
|---|---:|---:|
| Marcha pedida / confirmada | 1 / 1 | 3 / 3 |
| Fase da troca / falha | Driving / None | Driving / None |
| Velocidade do motor | 197.0169047878 rad/s | 51.7225290420 rad/s |
| Velocidade do rotor do veículo | 12.6455009492 rad/s | 7.6456066581 rad/s |
| Estados reportados | 64 | 70 |
| Resíduo máximo de energia amostrado | `1.18562e-8 J` | `1.19940e-9 J` |
| Erro máximo de fase de marcha reportado | `7.16227e-14 rad` | `7.10543e-15 rad` |
| Impressão digital | `72122eae163df98e` | `1d2b1a0c1eabf259` |
| Hash final | `ef2843acbb273e6d` | `b50dea693d6af82a` |

A redução final com carga e a pré-seleção sem carga seguinte são observadas por
confirmação física em 4.2 s. Em 4.0 s a confirmação anterior foi perturbada
temporariamente por deslizamento real de seletor/tração, de modo que a marcha real
reportou corretamente zero em vez de presumir a conclusão. A combinação em combustão
queima **37.2949777641 mg** e libera **1640.9790216183 J**.

Os valores SHA-256 das fontes são
`f15629d3fc8d8915f5884f77603458053c83e7835cf34d434fa607d85442bc87`
(controlado) e
`c3a6d491fd614d526a99831d7dfdc4285bca3c10a57ac811959023cfb9e40d5b`
(em combustão controlado). Todos os parâmetros permanecem `unverified`. Este
controlador usa de propósito entrega com interrupção de torque; a mistura completa
de torque da ECU, sensores/atuadores, mecanismos de garras/anel de bloqueio, falhas
abrangentes, AT, powertrains-alvo medidos e o Unity real permanecem em aberto.
Veja [DCT_CONTROL.md](DCT_CONTROL.pt-BR.md).

## 2026-10-01: caminhos de potência de embreagem dupla com sete marchas à frente e ré

O serial exigido `dotnet run --file tools/Build.cs -- verify` passa localmente
em Windows x64 com SDK 10.0.401/runtime 10.0.12: **308/308** verificações gerenciadas,
**239/239** verificações de assembly Standard hospedadas no .NET 10, **29/29** grupos
MCP reais, **16/16** Zig e **6/6** verificações de ABI do C#. A compilação Release
tem zero avisos/erros. Todos os **176** valores de baseline históricos correspondem
exatamente; a auditoria de C/C++/Lua está vazia. A aceitação real do Unity e a nova
aceitação em Linux/macOS permanecem não verificadas.

Arquivos de evidência:

- `artifacts/reports/dct-final-2026-10-01.log` — verificação serial completa.
- `artifacts/reports/dct-evidence-2026-10-01.json` — escopo, resumos, grafo/replay,
  grandezas finais e refinamento medido.
- `artifacts/reports/dct-schema-audit-2026-10-01.json` — todos os **26** laboratórios
  passam no contrato estrutural jsonschema 4.25.1 existente.
- `artifacts/reports/dual-clutch-transmission.json` e
  `artifacts/reports/fired-dual-clutch.json` — relatórios de experimento completos.

Seis grupos físicos verificam um grafo ordinário com quatorze rotores internos,
doze engrenagens permanentes e dez embreagens de atrito, vínculos estáveis
pertencentes ao chamador, sete caminhos à frente e ré com sinal, três ramos de
transmissão final e parâmetros imutáveis. Referências independentes de inércia
refletida/torque constante cobrem cada caminho selecionado com e sem pré-seleção
do caminho inativo. Verificações independentes de projeção de captura em duas
coordenadas conferem impulso de sincronização, velocidades finais e calor.
Ramificações completas, cancelamento, rollback tardio, contratos de
capacidade/unidade/ID/seleção e alocação gerenciada zero para avanço/releitura
bem-sucedidos passam.

O cenário completo de torque expôs uma falha de entrega de sexta para sétima logo
após a liberação da embreagem antiga. Travas refletidas de engrenagem correlacionadas
esgotaram o orçamento de projeção escalar. Uma reserva linear de Schur normalizada
e pré-alocada agora resolve travas independentes depois que esse orçamento se esgota,
com os mesmos limites estáticos, liberação limitada do conjunto ativo e verificações
de resíduo e calor passivo. Casos singulares/não lineares retêm os limites existentes.
Aceleração independente direta de sexta/sétima e a entrega que antes falhava são
evidência de regressão; a física mais antiga e as trajetórias de assets autênticos
permanecem verificadas. Não se usou aumento do limite de iterações nem aceitação de
resíduo reprovado.

Três grupos de integração verificam identidade de impressão digital de assembly/JSON,
largada, pré-seleção, todas as entregas à frente de subida/descida, encaminhamento
térmico, diagnósticos estruturados, revisões/cancelamento e ramificações independentes
de seletor. Os dois laboratórios fazem replay por assets portáteis e por um servidor
filho MCP real. Todos os **281** limites do laboratório de torque e **83** limites
do laboratório em combustão de relatório/portátil/MCP coincidem exatamente.

O cenário de torque atinge cada razão efetiva declarada após a entrega:

| Marcha à frente | Razão verificada de velocidade motor/veículo |
|---|---:|
| 1 | 15.58 |
| 2 | 9.43 |
| 3 | 6.765 |
| 4 | 5.166 |
| 5 | 3.774 |
| 6 | 3.182 |
| 7 | 2.664 |

São reduções de pesquisa declaradas, não medidas de OEM. O sinal de ré e a inércia
refletida pré-selecionada têm evidência independente de carga constante; o cenário
de estrada não reivindica engate de ré com o veículo em movimento.

| Grandeza final | DCT de torque (2.8 s) | DCT em combustão (0.8 s) |
|---|---:|---:|
| Velocidade do motor | 161.3549080416 rad/s | 53.8218438613 rad/s |
| Velocidade do rotor do veículo | 10.3565409526 rad/s | 7.9559266609 rad/s |
| Calor total de embreagem/sincronização | 870.3601867183 J | 224.4917038405 J |
| Nó térmico | 300.8703601867 K | 351.7212768995 K |
| Resíduo máximo de energia amostrado | `5.22732e-9 J` | `1.23919e-9 J` |
| Contagem de estados reportados | 55 | 61 |
| Impressão digital do modelo | `7466a75b99fbfd78` | `b7216a2ed8c88dc3` |
| Hash final | `c8932376afe516c6` | `35b434aca827c3a6` |

O exemplo em combustão queima **39.1255750233 mg** e libera **1721.5253010267 J**.
Seu grafo completo de sete marchas à frente/ré comanda a entrega programada de 1 para
2 para 3 dentro do orçamento de estado limitado atual. Em relação a uma referência de
12.5 microssegundos, as diferenças finais escaladas máximas de velocidades de
motor/veículo, trabalho da fonte e calores de embreagem selecionados são
**4.9008455434e-6** em 50 microssegundos e **4.3731765238e-6** em 25 microssegundos.
O erro diminui de forma modesta; isso sozinho não estabelece ordem uniforme de eventos
híbridos nem convergência assintótica completa. Referências independentes de
engrenagem/embreagem e a conservação permanecem evidência separada.

Os valores SHA-256 das fontes são
`f5db9f0a9f3a0585eff261d71285e4a390c099ec1a3bec27d1b9194beb311294`
(torque) e
`0dfc3ed92930b649c8312043625789a4a4394811fed9e0c88a615a18a6b07473`
(em combustão). Tipos de componente, unidades, formato de asset e leitores anteriores
existentes são retidos. Todos os parâmetros permanecem `unverified`; este arranjo de
trem de pesquisa não é uma DQ200 calibrada. Seletores de atrito não completam o
acionamento de garras/anel de bloqueio, e programações prescritas não implementam a
coordenação completa de torque de TCU/ECU. AT completa, perdas/acionamento medidos,
powertrains-alvo completos e o Unity real permanecem em aberto.
Veja [DUAL_CLUTCH_TRANSMISSION.md](DUAL_CLUTCH_TRANSMISSION.pt-BR.md).

## 2026-10-01: replay de fechamento limitado e compensação de corte em ticks físicos

O serial `dotnet run --file tools/Build.cs -- verify` passa localmente em Windows
x64 com SDK 10.0.401/runtime 10.0.12: **299/299** verificações gerenciadas, **233/233**
verificações de assembly Standard hospedadas no .NET 10, **27/27** grupos MCP reais,
**16/16** Zig e **6/6** verificações de ABI do C#. A compilação Release tem zero
avisos/erros. Todos os **176** valores numéricos históricos correspondem exatamente;
a auditoria de fontes C/C++/Lua permanece vazia. O Unity real e a CI nova de
Linux/macOS não estão verificados.

Arquivos de evidência:

- `artifacts/reports/closure-final-2026-10-01.log` — verificação serial completa.
- `artifacts/reports/closure-evidence-2026-10-01.json` — escopo legível por máquina,
  resumos de fonte/log, impressões digitais de replay e evidência de rastreamento e horizonte.
- `artifacts/reports/closure-schema-audit-2026-10-01.json` — todos os **24** documentos
  de laboratório passam no jsonschema 4.25.1; **6** casos de horizonte malformados são rejeitados.
- `artifacts/reports/closure-compensated-cylinder.json` — experimento em combustão.

Cinco grupos do Core verificam o replay manual independente e exato de tensão zero,
valores/hash/tempo confirmados inalterados, entrega real melhorada, memória de corte,
refinamento de horizonte, alinhamento/orçamento/imutabilidade, cancelamento de leitura
e de lote, falha tardia, ramificações independentes, alocação gerenciada zero para
previsões e avanço preditivo ativo, e históricos especulativos completos de embreagem.
Cada candidato usa estado pré-alocado separado e as equações normais da planta; nenhum
combustível previsto é somado a um balanço real. Previsões desativadas retêm as
impressões digitais e os hashes anteriores. Cancelamento ou uma previsão inválida
rejeita o lote real completo.

Em 1 ms o benchmark isolado prevê cerca de **2.9894117019 mg** de combustível adicional
sob remoção imediata de tensão, e seu replay manual de fechamento separado concorda
até a tolerância de comparação declarada `1e-15 kg`. A previsão de entrada retida é
distinta de eventos futuros reais ou do comportamento medido do dispositivo.

O rastreamento de horizonte finito retém o ricochete tardio no assento:

| Horizonte de previsão | Combustível realmente entregue para um pedido de 8 mg |
|---|---:|
| 8 ms | 8.2537740284 mg |
| 12 ms | 8.0606880453 mg |
| 20 ms | 8.0101785078 mg |
| 30 ms | 8.0101785078 mg |

A realimentação liga/desliga precedente entrega **9.8939438959 mg**. Com previsão de
20 ms e corte em tick físico, o erro relativo cai de **23.6743%** para **0.12723%**,
cerca de **186 vezes** menor neste benchmark sintético. A decisão de 20/30 ms concorda,
enquanto 8 ms trunca uma cauda material. Esta é evidência de rastreamento limitado
baseado em modelo, não precisão calibrada de injetor. O passo de tempo elétrico/de
contato e o refinamento de controlador/horizonte permanecem controles de aceitação separados.

Dois grupos portáteis verificam o replay v21 de horizonte/corte, a recodificação exata,
horizontes de driver malformados e a rejeição de rebaixamento v20 forjado. O fixture
v20 autêntico de SHA-256
`4d87e996d92a50508cfcffac610551b84220f2b3055f5b104c8ec7ec64ca9a2e`
retém a impressão digital `3fa813ff44b95a79` e o replay atualizado no mesmo runtime.
Fixtures mais antigos e trajetórias de modelo ideal/liga-desliga permanecem evidência de regressão.

Três grupos de integração mais um servidor filho MCP real verificam horizontes do
modelo, observáveis de previsão, tensão pertencente, erros acionáveis, revisões,
cancelamento, ramificações independentes e balanços completos de fonte/fase/energia.
Todos os **65** limites em combustão de relatório/portátil/MCP coincidem. No limite de 0.6 s:

| Grandeza | Valor |
|---|---:|
| Líquido entregue e evaporado | 28.0021908833 mg |
| Pressão do trilho | 725.327490978 kPa |
| Filme líquido restante | 0 mg |
| Vapor queimado | 27.9775490263 mg |
| Calor de reação | 1231.0121571575 J |
| Dose do ciclo pedida / entregue mais recente | 12 mg / 12.0346025230 mg |
| Última previsão de fechamento selecionada | 2.7431038678 mg |
| Comprimento da previsão | 2000 ticks físicos |
| Trava de corte / ticks de corte pendentes | 1 / 0 |
| Resíduo absoluto máximo de energia amostrado | `1.51078e-8 J` |
| Resíduo absoluto máximo de massa amostrado | `4.06576e-18 kg` |
| Resíduo absoluto máximo de combustível amostrado | `8.97855e-20 kg` |

A impressão digital é `ddefea6d870e7e75`; o hash final é `b8edcd36b58805b6`; o
SHA-256 da fonte do modelo é `106bde4e86ccb10cd214d1372cff300c09f277c88ee8811d2d9a144d72ca8326`.
O comando vivo do ciclo seguinte permanece 4 mg; a previsão e a entrega medida são
reportadas separadamente desse comando.

As previsões mantêm os outros comandos de atuador, ignoram eventos futuros de entrada
externa, respeitam um horizonte inteiro finito e exigem um intervalo local monótono de
corte. Essas hipóteses e os parâmetros físicos não verificados limitam esta evidência.
ECU/TCU completas, reposição do trilho, refinamento magnético/eletrônico/de fluido,
Unity real e aceitação de powertrain calibrado permanecem em aberto. Veja
[CLOSURE_PREDICTION.md](CLOSURE_PREDICTION.pt-BR.md).

## 2026-10-01: agulha eletromagnética e realimentação de entrega amostrada

O comando serial exigido `dotnet run --file tools/Build.cs -- verify` passa
localmente em Windows x64 com SDK 10.0.401/runtime 10.0.12: **289/289** verificações
gerenciadas, **226/226** verificações de assembly Standard no .NET 10, **26/26** grupos
MCP reais, **16/16** Zig e **6/6** verificações de ABI do C#. A compilação Release tem
zero avisos/erros. Todos os **176** valores numéricos históricos correspondem exatamente;
a auditoria de fontes encontra zero arquivos C/C++/Lua. Nenhuma CI nova de Linux/macOS
nem aceitação real de Unity Editor/Play/Player/IL2CPP é reivindicada.

Arquivos de evidência:

- `artifacts/reports/needle-final-2026-10-01.log` — execução serial completa.
- `artifacts/reports/needle-evidence-2026-10-01.json` — escopo legível por máquina,
  resumos de fonte/log, impressões digitais de laboratório, grandezas finais e refinamento.
- `artifacts/reports/needle-schema-audit-2026-10-01.json` — todos os **23** laboratórios
  passam no jsonschema 4.25.1; **16** casos magnéticos/batente/agulha/driver malformados são rejeitados.
- `artifacts/reports/needle-actuated-cylinder.json` — experimento/replay completos.

Nove grupos físicos verificam a identidade de energia discreta magnética, o trabalho
de alimentação com sinal e o calor de cobre não negativo, o jacobiano analítico de
força, a corrente RL analítica estacionária, a dinâmica simultânea magnética/mola,
as articulações conservativas dos batentes de curso, a entrega real da agulha fora
de cota/janela, a tensão pertencente, o desassentamento e o fechamento atrasados,
o excesso de dose, as transações completas e os contratos imutáveis/dimensionais.
O avanço ativo e os snapshots alocam **zero bytes gerenciados**. Os testes
especulativos de captura de embreagem preservam fluxo, fonte/fase, força média,
calor/trabalho compensados e o histórico de controle amostrado/retido através do
lote exato e de um lote posterior reprovado.

Uma ODE independente de cinco estados integra posição/velocidade da agulha, fluxo
magnético, calor de cobre e trabalho elétrico. Referências RK4 em **20,000/40,000**
passos concordam dentro da tolerância escalada `1e-10`. Ao longo de 5 ms, o
refinamento físico suave é:

| Tick | Erro escalado máximo | Erro anterior / erro atual |
|---|---:|---:|
| 25 microssegundos | `8.4976116406e-4` | — |
| 12.5 microssegundos | `2.1110507406e-4` | 4.02530 |
| 6.25 microssegundos | `5.2689855150e-5` | 4.00656 |
| 3.125 microssegundos | `1.3167017353e-5` | 4.00165 |

Isto é refinamento eletromagnético/mecânico suave de segunda ordem. Contato,
comutação de janela/driver e o acoplamento explícito de parede existente mantêm
limites de precisão separados; o replay exato não prova ordem uniforme nem controle calibrado.

O pedido isolado de 8 mg entrega **9.8939438959 mg** após o fechamento elétrico/mecânico
passivo e o ricochete no assento, um excesso de **1.8939438959 mg**. Uma abertura zero
em 20 ms é seguida por cerca de **0.0001200614 mg** de entrega adicional de ricochete
antes de assentar. O modelo retém esse escoamento em vez de cortar a massa no alvo ou
igualar um comando de tensão zero a uma válvula fechada. São dinâmicas de pesquisa, não
rastreamento de dose aceito nem comportamento medido de injetor.

Dois grupos portáteis verificam registros v20 completos de magnético, batente, agulha
e driver, replay de todos os limites no mesmo runtime, recodificação exata, contagens
tipadas limitadas, unidades/referências erradas, tabelas ausentes/duplicadas e a
rejeição de rebaixamento v19 forjado. O SHA-256 do fixture v19 autêntico é
`4ff5f6180006d53be5ffb6ef0663cc6de4af45f35f39dc4d52fd7e00e8cdd8c5`;
a impressão digital `4f74c6da6d89ab08` e o replay atualizado no mesmo runtime permanecem intactos.
O caminho legado de cota ideal, os fixtures mais antigos e as impressões digitais
físicas existentes permanecem evidência de regressão.

Três grupos de integração mais um servidor filho MCP real verificam dimensões/referências
estritas, orientação de comando de tensão pertencente, revisões, cancelamento,
ramificações independentes e balanços magnético/fonte/fase/térmico. Todos os **65**
limites de relatório/portátil/MCP coincidem. O laboratório em combustão de 0.6 s termina com:

| Grandeza | Valor |
|---|---:|
| Líquido entregue | 40.3903592511 mg |
| Pressão do trilho | 692.292375330 kPa |
| Combustível evaporado | 34.5106221450 mg |
| Filme líquido restante | 5.8797371061 mg |
| Vapor queimado | 31.2333851260 mg |
| Calor de reação | 1374.2689455424 J |
| Trabalho de alimentação elétrica | 0.168028754073 J |
| Calor de cobre | 0.167510468341 J |
| Energia magnética | `6.30199e-16 J` |
| Comando de bobina retido | 0 V |
| Curso real da agulha | 0.4842244877 micrômetros |
| Velocidade real da agulha | -0.1290065607 m/s |
| Pedido retido mais recente / dose realmente entregue | 4 mg / 5.8930695115 mg |
| Resíduo absoluto máximo de energia amostrado | `1.57642e-8 J` |
| Resíduo absoluto máximo de massa amostrado | `3.30682e-18 kg` |
| Resíduo absoluto máximo de combustível amostrado | `9.48677e-20 kg` |

O limite final ainda tem uma agulha em movimento, quase assentada, e um filme
incompletamente evaporado. O experimento novo verifica, portanto, o inventário
restante finito e o balanço completo de combustível, em vez de herdar a condição de
filme seco do laboratório de injetor ideal. KPIs numéricos aprovados não estabelecem
a dose comandada exata. A impressão digital do modelo é `3fa813ff44b95a79`; o hash
final é `de68045d420b9ffa`; o SHA-256 da fonte é
`d8c7472a3335a523821209cb183cf34687e454f326d9ac1946c618b7ca73050c`.

Todos os parâmetros permanecem `unverified`. Indutância linear não saturada, R
constante, agulha equilibrada em pressão, batentes elásticos e acionamento ideal de
tensão são reduções declaradas. Mapas magnéticos/térmicos não lineares, acionamento
de comutação/flyback/bateria, forças axiais de fluido, spray/deslocamento,
bomba/reposição do trilho, ECU/TCU completas e powertrains medidos permanecem em
aberto. As vistas preparadas no Unity de bobina/batente/controlador e a escala de
curso da agulha exigem verificação real de Editor/Play.
Veja [NEEDLE_ACTUATION.md](NEEDLE_ACTUATION.pt-BR.md).

## 2026-10-01: trilho líquido flexível e injeção por ciclo

O comando serial exigido `dotnet run --file tools/Build.cs -- verify` passa
localmente em Windows x64 com SDK 10.0.401/runtime 10.0.12: **275/275** verificações
gerenciadas, **215/215** verificações de assembly Standard hospedadas no .NET 10,
**25/25** grupos MCP reais, **16/16** testes Zig e **6/6** verificações de ABI do C#.
A compilação Release informa zero avisos/erros. Todos os **176** valores de baseline
históricos correspondem exatamente; a auditoria de fontes encontra zero arquivos
C/C++/Lua. Isso não estabelece CI nova de Linux/macOS nem aceitação real de Unity
Editor/Play/Player/IL2CPP.

Arquivos de evidência:

- `artifacts/reports/liquid-injector-final-2026-10-01.log` — execução serial completa.
- `artifacts/reports/liquid-injector-evidence-2026-10-01.json` — escopo legível por
  máquina, resumo do log, impressões digitais atuais de laboratório/fonte, grandezas
  finais e refinamento independente.
- `artifacts/reports/liquid-injector-schema-audit-2026-10-01.json` — todos os **22**
  laboratórios passam no jsonschema 4.25.1; **16** casos de injetor líquido malformados são rejeitados.
- `artifacts/reports/liquid-injected-cylinder.json` — experimento e replay completo.

Oito grupos físicos verificam o decaimento analítico da altura de pressão, o
inventário finito da fonte flexível, o trabalho exato do trilho, o calor passivo do
bico, os balanços calórico/químico/de pressão, a retenção de cota, o fechamento por
pressão reversa e a reversão sem reemissão de cota. O esgotamento atinge a pressão
prescrita do receptor sem inventar combustível. Ramificações independentes, entradas
rejeitadas/canceladas, falha tardia após injeção aceita e captura especulativa de
embreagem preservam todo o histórico de fonte/filme/cota/calor. A injeção ativa
quente e os snapshots alocam **zero bytes gerenciados**. Propriedades explícitas,
viabilidade do volume da fonte, unidades, posse de filme/virabrequim, capacidade de
estado limitada e compilação imutável são exercitadas.

Uma ODE simultânea independente de oito estados integra líquido entregue, massa do
filme, temperatura da parede, massa/energia interna do gás receptor e três históricos
de pressão/calor. Execuções RK4 independentes em **20,000/40,000** passos concordam
dentro da tolerância escalada declarada `1e-10`. Ao longo de 0.2 s numa janela suave
à frente, ticks físicos de 20/10/5/2.5 ms dão erros escalados máximos:

| Tick | Erro escalado máximo | Erro anterior / erro atual |
|---|---:|---:|
| 20 ms | `2.9306688569e-7` | — |
| 10 ms | `7.3266958993e-8` | 3.999987 |
| 5 ms | `1.8316757833e-8` | 3.999996 |
| 2.5 ms | `4.5791930144e-9` | 3.999997 |

Isto estabelece acoplamento suave de injeção/evaporação de segunda ordem sob a
redução declarada. Eventos de janela em tick fixo, esgotamento e outras fontes
explícitas de parede retêm seus limites de precisão separados; nenhuma ordem uniforme
de powertrain em combustão é reivindicada.

Dois grupos portáteis verificam registros v19 de fonte/bico/cronometragem,
recodificação exata, replay em todos os limites, contagens tipadas limitadas,
unidades/posse erradas, registros duplicados/ausentes e rebaixamentos v18 forjados.
O fixture de filme v18 autêntico retém SHA-256
`c0e59c6f6527b2b59ee1094cd6627c0829ecbd4ab0eb2e6ec06394ccfe15da97`,
impressão digital `cb103bce098f4e82` e replay atualizado no mesmo runtime. Fixtures
anteriores e a física sem filme permanecem evidência de regressão. A revisão também
atualiza os deslocamentos atuais de cabeçalho nos testes de registro malformado,
retendo os tamanhos de tabela da versão antiga.

Três grupos de integração mais um servidor filho MCP real verificam a fonte finita
compartilhada, a deposição líquida, a reação apenas do vapor, os balanços de
fonte/filme/parede, documentos estritos e revisões/cancelamento/ramificações de
sessão. Todos os **65** limites de relatório/portátil/MCP coincidem. O laboratório
de 0.6 s começa com filme seco e **500 mg** de líquido a **800 kPa**, e termina com:

| Grandeza | Valor |
|---|---:|
| Líquido entregue e evaporado | 28 mg |
| Líquido restante na fonte | 472 mg |
| Pressão do trilho | 725.333333333 kPa |
| Líquido restante no filme | 0 mg |
| Trabalho de pressão do trilho liberado | 0.028472888889 J |
| Trabalho de pressão do receptor exportado | 0.003236038241 J |
| Calor do bico | 0.025236850648 J |
| Calor do filme retirado da parede | 14 J |
| Temperatura da parede do filme | 498.602523685 K |
| Vapor queimado | 27.9759472237 mg |
| Calor de reação | 1230.9416778442 J |
| Resíduo absoluto máximo de energia amostrado | `5.52370e-9 J` |
| Resíduo absoluto máximo de massa amostrado | `1.08420e-18 kg` |
| Resíduo absoluto máximo de combustível amostrado | `7.45389e-20 kg` |

O ciclo final observado retém um pedido/entrega de **12 mg**, enquanto o comando
vivo é **4 mg** para uma janela futura. Aceitação da dose, entrega e reação
permanecem distintas. A impressão digital do modelo é `4f74c6da6d89ab08`; o hash
final é `ddf5b1c4b0678451`; o SHA-256 da fonte é
`85b9cba983e31258d9341943dab3c844a18c05c5320dda3d9d610a3935a14d09`.

A energia de pressão do trilho é energia interna armazenada; o calor do bico entra
na parede finita. O receptor de volume líquido desprezível exporta explicitamente
o trabalho de pressão de deslocamento, em vez de creditar volume de gás ou trabalho
de virabrequim ocultos. Sua referência de complacência a pressão zero e as
propriedades constantes são reduções de pesquisa declaradas. Todos os parâmetros
permanecem `unverified`. Bomba/reposição, mapas de contrapressão/propriedades,
dinâmica de agulha/elétrica/spray, acoplamento de volume líquido finito,
ignição/ECU, transmissão/controle completos e powertrains de veículo calibrados
permanecem em aberto. As vistas de trilho/bico no Unity e os testes de ciclo de
vida estão preparados, mas exigem o Editor fixado.
Veja [LIQUID_FUEL_INJECTION.md](LIQUID_FUEL_INJECTION.pt-BR.md).

## 2026-10-01: filme de combustível líquido finito e transporte simétrico

O comando serial exigido `dotnet run --file tools/Build.cs -- verify` passa
em Windows x64 com SDK 10.0.401/runtime 10.0.12: **262/262** verificações gerenciadas,
**205/205** verificações de assembly Standard hospedadas no .NET 10, **24/24** grupos
MCP reais, **16/16** testes Zig e **6/6** verificações de ABI do C#. Todos os **176**
valores de baseline históricos retidos correspondem exatamente. A auditoria de fontes
encontra zero arquivos C/C++/Lua. A compilação Release informa zero avisos/erros.
Esta é evidência local da árvore de trabalho, sem CI nova de Linux/macOS nem aceitação
real de Unity Editor/Play/Player/IL2CPP.

Arquivos de evidência:

- `artifacts/reports/fuel-film-final-2026-10-01.log` — execução serial completa.
- `artifacts/reports/fuel-film-evidence-2026-10-01.json` — escopo legível por máquina,
  resumo do log, impressões digitais de laboratório, grandezas de fase e erros de refinamento.
- `artifacts/reports/fuel-film-schema-audit-2026-10-01.json` — todos os **21** documentos
  de laboratório passam no jsonschema 4.25.1; **12** casos de filme malformados são rejeitados.
- `artifacts/reports/film-fired-cylinder.json` — experimento e replay completo.

Dez grupos físicos verificam aquecimento analítico de banho finito, referência de
fase com sinal, saturação/secagem, disponibilidade limitada de calor, resfriamento,
condutância zero, reação real apenas do vapor e balanços independentes de
constituinte/químico/térmico. Ramificações completas e lotes cancelados ou rejeitados
tarde preservam todos os históricos; o avanço quente e as leituras de snapshot
alocam **zero bytes gerenciados**. Portas/unidades erradas, propriedades de fase
inválidas, estouro de capacidade de estado e compilação imutável são exercitados.

A revisão corrigiu o segundo meio passo de filme/gás para gás/filme e inverteu a
varredura de filme da parede compartilhada. Isso torna filme/gás/mecânica/gás/filme
simétrico, preservando o caminho de solver existente para modelos sem filme.
Referências RK4 simultâneas independentes em 20,000 e 40,000 passos concordam dentro
da tolerância escalada declarada `1e-10`. Um estudo suave de dois segundos com filme
saturado usa ticks físicos de 40/20/10/5 ms e o erro escalado máximo entre massa
líquido/gás, temperatura da parede, energia interna do gás e constituintes transportados:

| Acoplamento | Erro em 40 ms | Erro em 5 ms | Razões de sucessivas metades |
|---|---:|---:|---|
| Dois filmes compartilhando uma parede finita | `3.43324e-8` | `5.36263e-10` | 4.0000, 4.0002, 4.0012 |
| Vapor do filme saindo por uma porta de gás bloqueada | `3.17195e-5` | `4.89033e-7` | 4.0495, 4.0029, 4.0014 |
| Filme mais troca de calor gás-parede | `5.30200e-4` | `6.61364e-5` | 2.0024, 2.0012, 2.0006 |

Os casos de filme compartilhado e de transporte de vapor mostram refinamento suave
de segunda ordem. Outras fontes de calor de parede retêm a temperatura explícita do
intervalo externo e o limite de acoplamento de primeira ordem. Estas verificações não
estabelecem segunda ordem uniforme através de secagem, eventos de válvula/reação ou
um powertrain completo em combustão.

Dois grupos de asset verificam grandezas de fase v18, codificação determinística,
replay em todos os limites, registros tipados limitados, unidades/contagens
malformadas, registros duplicados/ausentes e a rejeição de rebaixamento v17 forjado.
O fixture de dosagem v17 autêntico de SHA-256
`d51dc0968bc3d154b2c3b227f74b7210215d4ee00c5a47e8dd16dc1d71cd5da1`
retém a impressão digital `099db1021c8df1fe` e o replay atualizado no mesmo runtime.
Fixtures anteriores e impressões digitais de modelos sem filme permanecem evidência de regressão.

Três grupos de integração e o servidor filho MCP real verificam o mesmo inventário
finito, a energia de fase, a reação apenas do vapor, documentos estritos e o
comportamento de revisão/ramificação/cancelamento. Todos os **63** limites
JSON/relatório/portátil/MCP coincidem. O laboratório de 0.6 segundo começa com
**40 mg** de molhamento explícito e termina com:

| Grandeza | Valor |
|---|---:|
| Líquido restante | 0 mg |
| Combustível evaporado | 40 mg |
| Calor retirado da parede finita | 20 J |
| Temperatura da parede do filme | 498 K |
| Vapor queimado | 31.1117599551 mg |
| Calor de reação | 1368.9174380261 J |
| Resíduo final de energia | `-1.58434e-9 J` |
| Resíduo absoluto máximo de energia amostrado | `2.15960e-9 J` |
| Resíduo absoluto máximo de massa amostrado | `1.49078e-18 kg` |
| Resíduo absoluto máximo de combustível amostrado | `2.09641e-19 kg` |

A impressão digital do modelo é `cb103bce098f4e82`; o hash final é `495582b10f40832c`.
O SHA-256 da fonte é
`8c933a5889d3b8f22f37738e307989d2c0fa5c7dfe9018ffe50a21f19e2aa416`.
Todos os parâmetros permanecem `unverified`. O molhamento inicial não é injeção
líquida; propriedades de fase dependentes da pressão, reposição, ignição/ECU,
transmissão/controles completos e powertrains medidos permanecem em aberto.
Marcadores de filme preparados no Unity e testes de ciclo de vida precisam do Editor
fixado. Veja [FUEL_FILM.md](FUEL_FILM.pt-BR.md).

## 2026-09-30: trilho de combustível finito e dosagem por ciclo

O checkpoint publicado `dc7ec2d` do acumulador de gás passa na
[CI de Windows/Linux/macOS](https://github.com/Water-Run/Power/actions/runs/36710249585).
Essa evidência cobre a fonte precedente, não o injetor novo nem o Unity real.

O comando serial exigido `dotnet run --file tools/Build.cs -- verify` passa
localmente em Windows x64 com SDK 10.0.401/runtime 10.0.12: **247/247** verificações
gerenciadas, **193/193** verificações de assembly Standard hospedadas no .NET 10,
**23/23** grupos MCP reais, **16/16** Zig e **6/6** verificações de ABI do C#. Todos
os **176** valores numéricos históricos correspondem exatamente; a auditoria de fontes
encontra zero arquivos C/C++/Lua. A compilação Release tem zero avisos/erros. Log:
`artifacts/reports/fuel-injector-final-2026-09-30.log`.

Oito grupos físicos verificam cronometragem à frente/com envolvimento, trilho finito
e cota exata, pedidos retidos, fechamento por pressão reversa e esgotamento, reversão
sem reemissão de cota, uma ODE independente de massa/entalpia de dois vasos com
refinamento suave, queima premisturada dosada analítica, transações completas,
dimensões/compatibilidade/capacidade e geometria imutável. O avanço quente e os
snapshots alocam **zero bytes gerenciados**. Falha tardia de torque/volume,
cancelamento, rejeição de entrada, lote exato e ramificações preservam todos os
históricos de entrega/ordinal. Descontinuidades de cronometragem retêm requisitos de
refinamento em tick fixo; nenhuma reivindicação de tempo de comutação contínuo é feita.

Dois grupos portáteis verificam bico/cronometragem v17, registros tipados limitados,
unidades erradas, registros ausentes/duplicados e rejeição de rebaixamento forjado.
O fixture autêntico do acumulador de gás v16 retém SHA-256
`72f0ee5b3180b763de091674565b2b8f2cbd61f80a3e4ab46e9694ad153a79c7`, impressão digital
`739f2baba8c669a0` e replay atualizado no mesmo runtime em todos os limites. Fixtures
anteriores e impressões digitais físicas permanecem inalterados. Três grupos de
integração verificam o esgotamento real do trilho, combustível entregue/reagido/de
limite, contratos estritos e sessões.

Todos os **20** documentos de laboratório passam na validação estrutural de esquema;
**12** casos de injetor malformados são rejeitados pelo jsonschema 4.25.1. Valores
de ciclo, portas finitas compatíveis, máximos de dose e posse do virabrequim
permanecem verificações adicionais do compilador. Relatório:
`artifacts/reports/fuel-injector-schema-audit.json`. As auditorias de esquema
existentes também passam em todos os 20 documentos. Um receptor de reservatório
inválido agora devolve um diagnóstico de conexão antes da validação da fração de
reservatório. R/gamma/LHV/estequiometria diferentes são rejeitados, para que
transferências internas não possam inventar inventário químico.

O `metered-fired-cylinder` substitui a admissão de combustível premisturado por ar
puro mais um trilho gasoso finito. As doses pedidas são 8/12/4 mg; elas se retêm na
janela à frente seguinte. Em 0.6 s o ciclo observado mais recente ainda guarda 12 mg,
enquanto o comando vivo é 4 mg para uma janela futura. Execução e aceitação da dose
permanecem distintas da entrega real. Todos os **65** limites de relatório/portátil/MCP
coincidem exatamente.

| Grandeza final | Valor |
|---|---:|
| Combustível entregue | 28 mg |
| Combustível queimado | 27.9299615615 mg |
| Calor de reação liberado | 1228.918308706072 J |
| Combustível restante na câmara | 0.0055539661 mg |
| Saldo líquido de combustível no limite | -0.0644844724 mg |

Combustível queimado, restante e perdido no limite dão conta do combustível entregue.
A transferência interna do trilho não acrescenta entrada externa de energia de
combustível nem fonte ilimitada. Entalpia térmica e energia química do trilho usam
o mesmo fluxo limitado de constituinte.

| Erro absoluto máximo ao longo do experimento | Valor | Limite afirmado |
|---|---:|---:|
| Esgotamento do trilho contra entrega | 3.67e-18 kg | 1e-16 kg |
| Entregue contra combustível reagido/restante/de limite | 6.78e-21 kg | 1e-14 kg |
| Energia do modelo inteiro | 1.52e-9 J | 1e-6 J |
| Massa total | 4.07e-18 kg | 1e-14 kg |
| Constituinte de combustível | 3.67e-18 kg | 1e-14 kg |
| Constituinte de ar fresco | 1.20e-18 kg | 1e-14 kg |

Impressão digital `099db1021c8df1fe`; hash final de Windows/runtime `329e1109392b37f3`.
Relatórios: `artifacts/reports/metered-fired-cylinder.json` e
`fuel-injector-evidence-summary.json`.

Três execuções serial da CLI, cada uma com duas trajetórias de 0.6 s (24,000 ticks
aceitos), replay completo e 65 limites, levam **0.502 / 0.364 / 0.370 s**, mediana
**0.370 s** em carga ordinária de desktop. Os traços coincidem exatamente; o avanço
quente sem alocação é verificado separadamente contra os dois assemblies. Este é
custo local observado, não um ganho de velocidade nem uma garantia portátil de vazão.

O agente 0.20.0 e o MCP real verificam a descoberta de dose em kg, exportação/replay
completos, rejeição de cota inválida e ramificações independentes de controlador/combustível.
Vistas de injetor/trilho/cronometragem no Unity e testes de Edit/Play estão preparados
em fonte C# 9. Unity Editor/Play/Mono/IL2CPP/Player reais e evidência nova de três
plataformas para este incremento permanecem separados. Esta é dosagem gasosa ideal
com propriedades de gás constantes comuns; spray/evaporação líquidos, hardware de
agulha/trilho/tanque, injeção de gasolina calibrada, ignição/ECU e o powertrain
completo permanecem inacabados. Os parâmetros não estão verificados.

## 2026-09-30: pistão de gás e acumulador de energia finita

O desenvolvimento foi retomado a pedido do proprietário. O checkpoint precedente
`cebc978` do carretel passou na
[CI de Windows, Linux e macOS](https://github.com/Water-Run/Power/actions/runs/36695753045);
cada job da matriz concluiu a verificação serial exigida. Log baixado:
`artifacts/reports/spool-three-platform-2026-09-30.log`. Essa evidência cobre a
fonte publicada do carretel, não o incremento novo de pistão de gás nem o Unity real.

O novo comando serial exigido passa em Windows x64 local:
`dotnet run --file tools/Build.cs -- verify`. Resultados: **234/234** verificações
gerenciadas, **183/183** verificações de assembly Standard hospedadas no .NET 10,
**22/22** grupos MCP reais, **16/16** Zig e **6/6** verificações de ABI do C#. Todos
os **176** valores históricos correspondem exatamente; a auditoria de fontes encontra
zero arquivos C/C++/Lua. A compilação Release tem zero avisos/erros. Log:
`artifacts/reports/gas-accumulator-final-2026-09-30.log`.

Oito grupos físicos cobrem trabalho adiabático analítico e seu jacobiano, curso
pequeno, câmaras com sinal/opostas, RK4 independente de massa/energia e refinamento
suave de segunda ordem, movimento comum de gás/fluido, uma referência RK4 separada
de parede finita com refinamento acoplado à parede de primeira ordem, entrada de gás
em volume móvel e entalpia de reservatório, transações completas, geometria/unidades
e avanço sem alocação. O balanço de entrada mediu cerca de `1.14e-17 kg` de resíduo
acumulado de ponto flutuante após atualizações repetidas de massa; seu limite
`1e-16 kg` reflete esse acúmulo. Fechar a porta preserva exatamente a massa aceita.
Nenhuma correção de massa ou energia força uma aprovação.

Dois grupos portáteis retêm geometria v16 completa, direção com sinal, contagens
tipadas limitadas, unidades malformadas, registros ausentes/duplicados e rejeição de
rebaixamento. O fixture de carretel v15 autêntico corresponde byte a byte ao pacote
do checkpoint publicado: SHA-256
`67b976a27ca0bd7343ca6b024c5bc736e14f48326945da841785ade43b61f14b`,
impressão digital `28aa0965d248e280`. Todos os limites de evento originais/atualizados
coincidem dentro do runtime em execução. Fixtures anteriores e impressões digitais
físicas permanecem intactos.

Todos os **19** documentos de laboratório passam na validação de esquema. **12**
documentos de pistão de gás malformados são rejeitados separadamente pelo jsonschema
4.25.1. Volume nominal positivo e posse única de geometria permanecem verificações
adicionais do compilador. Os relatórios estão em `artifacts/reports`, incluindo
`gas-piston-schema-audit.json`; as auditorias existentes de pistão, carretel,
bateria/duty e controlador de pressão também passam em todos os 19 documentos.

O modelo de seis segundos `gas-accumulator-pump` acrescenta uma câmara de gás de
50 ml e um separador de 50 g à bomba elétrica, ao desvio mecânico de carretel e à
embreagem de pressão. O gás começa a 200 kPa absolutos/300 K, com uma referência
declarada de 100 kPa e penetração complacente de assentamento de 0.1 mm. Durante o
pulso de 3-4 s, a tensão do motor é 6 V e os dois caminhos de enchimento/drenagem
estão explicitamente abertos. A energia interna do gás cai **0.6196209894 J**; o
trabalho à pressão de referência é **-0.2094578629 J**. Depois da energia
cinética/de batente do separador e do amortecimento, a entrega líquida ao líquido é
**0.4092090634 J**, com **2.094578629 ml** de volume varrido devolvido. O enchimento
retoma e a embreagem fica travada com deslizamento zero no limite final. São
resultados sintéticos, não calibração de OEM.

| Erro absoluto máximo em todos os 306 limites | Valor | Limite afirmado |
|---|---:|---:|
| Conta separada de bomba/fluido/movimento/gás/referência/calor | 6.09e-13 J | 1e-8 J |
| Energia do modelo inteiro | 1.83e-8 J | 1e-6 J |
| Conta de volume de referência do líquido | 5.19e-19 m3 | 1e-16 m3 |
| Invariante adiabático normalizado do gás fechado | 6.83e-13 J | 1e-8 J |

Cada um dos **306** limites JSON/portátil/MCP tem hashes e valores idênticos.
Impressão digital `739f2baba8c669a0`; hash final de Windows/runtime `074917dc8e343131`.
Relatórios: `artifacts/reports/gas-accumulator-pump.json` e
`gas-accumulator-evidence-summary.json`. A conta de gás/fluido subtrai explicitamente
o trabalho de referência e o potencial inicial de batente; a energia interna absoluta
do gás sozinha não é rotulada como energia de fluido entregue.

### Otimização medida da câmara fechada

Modelos novos de pistão de gás fechados e não misturados, sem transporte de gás nem
elos de calor, retêm a validação de estado e pulam a integração de taxa zero. Três
execuções serial completas da CLI por estágio incluem duas trajetórias de seis
segundos (600,000 ticks aceitos) e todos os 306 limites. Os tempos de baseline foram
**2.727 / 2.603 / 2.496 s**; os tempos otimizados foram **2.382 / 2.362 / 2.357 s**.
O custo mediano diminui cerca de **9.3%** neste desktop. Os três traços de
antes/depois coincidem em cada observável e hash de estado exatamente. A alocação
estacionária de Step/ReadSnapshot permanece em **zero bytes gerenciados**. Esta é
medição local, não uma garantia portátil de vazão. Portas, elos de parede e transporte
de constituintes retêm seu caminho ordinário de integração e testes independentes.

O agente 0.19.0/MCP verifica descoberta termodinâmica, exportação/replay completos,
revisões e ramificações independentes de gás/fluido. Vistas do Studio e testes de
Edit/Play estão preparados em fonte C# 9. Unity Editor/Play/Mono/IL2CPP/Player reais
e a verificação nova de três plataformas deste incremento permanecem separados. O
motor completo, a transmissão, ECU/TCU, amostras de veículo calibradas e a aplicação
de desktop aceita permanecem inacabados. Todos os limites de amostra, o licenciamento
e a proveniência são preservados.

## 2026-09-30: checkpoint do regulador mecânico de carretel

O comando serial exigido passa em Windows x64, SDK 10.0.401/runtime 10.0.12:
`dotnet run --file tools/Build.cs -- verify`. O resultado é **221/221** verificações
gerenciadas, **173/173** verificações de assembly Standard hospedadas no .NET 10,
**21/21** grupos MCP de servidor filho real, **16/16** Zig e **6/6** verificações de
ABI do C#. Todos os **176** valores numéricos históricos correspondem exatamente. A
compilação Release tem zero avisos/erros; a auditoria de fontes encontra zero arquivos
C/C++/Lua. Log: `artifacts/reports/spool-final-2026-09-30.log`.

O [contrato do carretel](HYDRAULIC_SPOOL.pt-BR.md) acrescenta um ressalto de dosagem
equilibrado em pressão, desprezando explicitamente a força axial de jato. A posição
real do pistão e as pressões das duas portas de fluido participam da solução de Newton
compartilhada com derivadas analíticas. Sete grupos físicos verificam curso com sinal,
escoamento bidirecional passivo e derivadas, pressão estacionária independente, um
transitório RK4 separado de três estados com refinamento suave de segunda ordem, erro
decrescente através da abertura, equalização de porta finita, transações e geometria
imutável. A alocação quente de Step/ReadSnapshot permanece zero contra os dois alvos
do Core. Falha tardia, cancelamento e ramificações independentes retêm todo o estado
de pressão/movimento/calor. Nenhuma ordem não suave uniforme é reivindicada.

Dois grupos portáteis cobrem geometria v15 completa e contagens/tipos/unidades
malformados forjados, registros ausentes/duplicados e rejeição de rebaixamento. O
SHA-256 do fixture de pistão v14 autêntico é
`72e0605d00972a58e2f5358aa8cbd44417e81cf1c452a20ab9661eac0919bae0`; sua impressão
digital, referências físicas e replay atualizado exato no mesmo runtime retêm cada
limite de evento. Os fixtures anteriores estão intactos. Três grupos de integração
verificam documentos estritos, revisões/cancelamento/ramificações de sessão e um
balanço separado de fluido/movimento. Unidades erradas de posição de dosagem retêm o
diagnóstico de unidade, em vez de serem capturadas como erros de faixa do construtor.

Todos os **18** documentos de laboratório passam na validação estrutural de esquema.
**12** casos de carretel malformados são rejeitados de forma independente pelo
jsonschema 4.25.1. Relatório: `artifacts/reports/spool-schema-audit.json`. Curso com
sinal não nulo, posições do ressalto dentro do curso do pistão e posse tipada
permanecem verificações adicionais do compilador.

O experimento `spool-regulated-pump` executa **3 s** com ticks de **20,000 ns** e
**156** limites JSON/portátil/MCP coincidentes. Seu atuador de pressão móvel, mola/amortecimento
de retorno e desvio regulam a linha sem um controlador de válvula amostrado. Os
2 N*m de acionamento/freio são cargas de pesquisa explícitas. O critério herdado de
captura em alta pressão de dois segundos falhou na pressão regulada mais baixa; o
experimento agora executa tempo suficiente para observar a captura real, retendo as
asserções de deslizamento zero e modo travado. Nenhum estado de pressão, energia ou
deslizamento é corrigido para passar.

| Grandeza final | Valor |
|---|---:|
| Pressão de linha | 233956.17402528782 Pa |
| Deslocamento do carretel | 0.00016975695474013045 m |
| Abertura de dosagem | 0.08487847737006522 |
| Calor de restrição do carretel | 2.917614235503143 J |
| Calor de amortecimento de retorno | 0.0006249365922203377 J |
| Trabalho hidráulico da bomba | 3.3299555096992406 J |
| Calor da embreagem | 260.2402810714738 J |
| Modo/deslizamento final da embreagem | Locked / 0 rad/s |

Em todos os limites, o balanço separado hidráulico/movimento/mola/pastilha/calor tem
erro máximo **9.77e-15 J** (limite afirmado 1e-8 J), energia global **1.99e-8 J**
(limite 1e-6 J) e inventário de volume de referência **1.35e-20 m3** (limite 1e-16 m3).
Impressão digital `28aa0965d248e280`, hash final de Windows/runtime `180744d025212ef7`.
Relatórios: `artifacts/reports/spool-regulated-pump.json` e
`artifacts/reports/spool-evidence-summary.json`.

Três execuções serial da CLI, cada uma incluindo duas trajetórias completas (300,000
ticks aceitos), todas as verificações de replay e 156 limites de saída, levaram
**1.183 / 1.230 / 1.153 s**; mediana **1.183 s** em carga ordinária de desktop. Este
é um custo de checkpoint observado, não uma garantia de vazão entre plataformas nem
evidência de ganho de velocidade. Os buffers de matriz, de inclinação do ressalto
e de rollback são limitados e de propriedade da simulação; o avanço/releitura
estacionários permanecem sem alocação.

O agente 0.18.0 anuncia hidráulica regulada mecanicamente, unidades de geometria e
a omissão da força de jato. As verificações MCP reais cobrem exportação/replay
completos e a rejeição de uma tentativa de escrita na saída de abertura. Vistas de
válvula/atuador no Unity e testes de Edit/Play estão preparados em fonte C# 9.
`POWER_UNITY_EDITOR` não está definido; Editor/Play/Mono/IL2CPP/Player reais e a
verificação nova de Linux/macOS permanecem pendentes. O proprietário encerra o
desenvolvimento de hoje neste checkpoint numérico; Power! completo e calibração não
são reivindicados. A entrega de fonte/pacote preserva licenças e limites de amostra.

## 2026-09-30: pistão dinâmico e embreagem acionada por contato

O comando serial exigido passou em Windows x64, SDK 10.0.401/runtime 10.0.12:

```sh
dotnet run --file tools/Build.cs -- verify
```

- **209/209** verificações gerenciadas, **164/164** verificações de assembly Standard
  hospedadas no .NET 10 e **20/20** grupos MCP de servidor filho real.
- **16/16** Zig e **6/6** verificações de ABI do C#; todos os **176** valores
  históricos correspondem exatamente. A auditoria de fontes encontra zero arquivos
  C/C++/Lua. Compilação Release: zero avisos/erros.
- Log: `artifacts/reports/piston-final-2026-09-30.log`.
- Todos os **17** laboratórios passam na validação de JSON Schema. Auditorias
  separadas rejeitam cada uma **12** casos malformados de pistão, bateria/duty e
  controle de tensão com jsonschema 4.25.1. Os relatórios são `piston-schema-audit.json`,
  `battery-schema-audit.json` e `pressure-controller-schema-audit.json` em `artifacts/reports`.

[Pistões hidráulicos](HYDRAULIC_PISTON.pt-BR.md) acrescentam massa explícita,
deslocamento, volume varrido da câmara, mola/amortecimento, folga das pastilhas e
extremos de curso complacentes. Embreagens de contato derivam a capacidade da força
das pastilhas. Oito grupos de verificação física cobrem trabalho de articulação e
derivadas analíticas, históricos compactos separados de amortecimento/decaimento
analítico, oscilação acoplada mola/fluido com refinamento suave de segunda ordem,
trabalho e volume de reservatório finito/traseiro, refinamento RK4 por partes do
contato, enchimento/captura/liberação livres, contratos tipados e transações
completas. Nenhuma ordem uniforme é reivindicada através de eventos de contato.
Falha numérica tardia, cancelamento, ramificações e lote exato preservam o estado
inteiro. O avanço quente do núcleo e as leituras de snapshot alocam **zero bytes gerenciados**.

O laboratório sintético `piston-actuated-clutch` avança **15 s** em ticks de
**20,000 ns** com controle de duty amostrado de **5 ms**. Todos os **761** limites
de relatório/portátil/MCP têm hashes e valores observáveis idênticos neste runtime.
A pressão durante o enchimento livre não produz força nas pastilhas. A drenagem
programada libera a embreagem; o enchimento posterior a captura. No limite final:

| Grandeza | Valor |
|---|---:|
| Deslocamento do pistão | 0.0021772797986273195 m |
| Força das pastilhas | 177.27979862731945 N |
| Capacidades estática/deslizante | 22.69181422429689 / 11.345907112148446 N*m |
| Pressão da câmara dianteira | 199053.02928772685 Pa |
| Energia armazenada de pastilha/batente | 0.01571406350067147 J |
| Calor acumulado de amortecimento de retorno | 0.0025718766359138913 J |

Impressão digital `46f746398142c258`; hash final de Windows/runtime `3a7b8eee248785d3`.
Relatório: `artifacts/reports/piston-actuated-clutch.json`. A fonte usa explicitamente
amortecimento sintético de 300 N*s/m para manter a alimentação suficiente durante o
transitório de contato; o solver retém a rejeição de pressão negativa em vez de
limitar esse estado.

### Balanços de energia independentes e evidência de tolerância

Cada limite compara de forma independente o trabalho da bomba com fluido complacente,
energia cinética do cursor, energia de mola de retorno e de pastilha mais calor de
restrição/amortecimento; a perda química/RC da bateria com energia cinética/indutiva
do motor, trabalho da bomba e calor elétrico; e o trabalho rotacional externo com
energia dos rotores e calor da embreagem.

| Conta | Erro absoluto máximo ao longo do experimento | Limite afirmado |
|---|---:|---:|
| Hidráulica, movimento e contato | 1.56e-13 J | 1e-9 J |
| Alimentação elétrica e motor | 2.48e-8 J | 1e-7 J |
| Nó térmico compartilhado contra históricos diretos de calor | 2.87e-7 J | 5e-7 J |
| Rotores acionados e embreagem | 1.16e-6 J | 2e-6 J |
| Modelo inteiro | 1.42e-6 J | 5e-6 J |

A asserção hidráulica inicial de 1e-7 J inferia um calor minúsculo de mola subtraindo
o calor grande da embreagem da temperatura arredondada do nó compartilhado. Ela
falhou, e esse calor inferido até diminuía perto do movimento estacionário. Um canal
direto de amortecimento compensado agora retém a dissipação física de forma
independente; o balanço hidráulico mais apertado acima passa. A acumulação térmica e
rotacional explica o resíduo global restante. O limiar global herdado de 1e-6 J era
insuficiente para esta execução de 750,000 ticks; 5e-6 J é um limite numérico
explícito de execução longa, suplementado pelas verificações analíticas mais
apertadas, de volume de fluido e de energia separada. Nenhum estado de energia é
corrigido para forçar uma aprovação. Auditoria suplementar:
`artifacts/reports/piston-evidence-summary.json`.

### Escopo de desempenho

Três execuções serial da CLI em carga ordinária de desktop incluem cada uma **duas**
trajetórias completas (1.5 milhão de ticks aceitos), verificações de replay e 761
limites de saída. As medianas decorridas foram **4.007 s** antes das derivadas
analíticas/históricos de amortecimento, **4.100 s** com a derivada analítica e
históricos completos indexados por componente, e **4.234 s** com históricos
compactos. As três últimas execuções foram 4.234, 4.088 e 4.928 s. Estas medições
não estabelecem um ganho de velocidade nem uma garantia portátil de vazão. O
jacobiano analítico remove a perturbação de comprimento físico e as avaliações
repetidas de contato; os históricos compactos alocam e copiam só os espaços reais
de mola. O espaço de trabalho denso e os fatores LU permanecem limitados, em cache
e de propriedade da simulação. O avanço estacionário bem-sucedido e as leituras de
snapshot retêm a asserção de alocação zero.

O asset v14 armazena limite de pistão/traseiro, parâmetros de curso/pastilha e
geometria de atrito referenciada. Contagens, cobertura, unidades/tipos malformados,
registros duplicados/ausentes e rebaixamentos forjados são rejeitados. O fixture de
bateria v13 autêntico retém SHA-256
`67e7bde4dfcc2b8b41f49cadae8f89ff5fc5a1668059e084b7b5d9da02c7c44a`, referências
físicas e replay atualizado exato no mesmo runtime. Fixtures mais antigos permanecem
intactos. O agente 0.17.0/MCP exercita descoberta, validação, cada limite exportado,
força de contato, conflitos de revisão e ramificações independentes.

Vistas de cursor/contato no Unity e testes de Edit/Play estão preparados em fonte
C# 9. O host das verificações Standard é .NET 10; ele não exercita o Unity Editor.
`POWER_UNITY_EDITOR` não está definido. Evidência de Editor/Play/Mono/IL2CPP/Player
e de Linux/macOS nova permanece pendente. Os parâmetros são entradas de pesquisa não
verificadas; o motor completo, a transmissão, ECU/TCU e as amostras de veículo
calibradas permanecem inacabados.

## 2026-09-30: alimentação por bateria finita e regulação por duty cycle

O comando serial exigido passou em Windows x64, SDK 10.0.401/runtime 10.0.12:

```sh
dotnet run --file tools/Build.cs -- verify
```

- **196/196** verificações gerenciadas, **154/154** verificações de assembly Standard
  hospedadas no .NET 10 e **19/19** grupos MCP de servidor filho real.
- **16/16** Zig e **6/6** verificações de ABI do C#; todos os **176** valores
  históricos correspondem exatamente. A auditoria de fontes encontra zero arquivos
  C/C++/Lua. Compilação Release: zero avisos/erros.
- Log: `artifacts/reports/battery-final-2026-09-30.log`.
- Todos os **16** laboratórios passam no JSON Schema; **12** casos malformados de
  bateria/duty são rejeitados pelo jsonschema 4.25.1 num cache isolado ignorado.
  Relatório suplementar: `artifacts/reports/battery-schema-audit.json`.

Nós de bateria de capacidade finita acrescentam OCV/SOC afim e um ramo RC de
polarização. Motores de bateria usam um transformador de duty cycle médio
bidirecional; cargas resistivas comutadas de acessório compartilham a resistência
do barramento. Energia química/RC e indutiva do motor, calor de bateria/cobre/carga
e transferências mecânicas/hidráulicas compartilham o balanço de conservação. O
trabalho do motor de bateria é interno, não trabalho de fonte externa duplicado.
Verificações numéricas iniciais expuseram energia indutiva ausente do motor de
bateria; o balanço agora a inclui.

A evidência cobre decaimento analítico de polarização sem carga e resposta RC de
carga resistiva, inventário de carga, integração RK4 independente de quatro estados
motor/bateria e refinamento suave de segunda ordem. Duty com sinal, carga
regenerativa, equivalência de enrolamento em paralelo, respostas engrenadas
dependentes do duty, acoplamento conjunto embreagem/bomba, rollback completo de
esgotamento tardio, cancelamento, rejeição de entrada, ramificações independentes,
replay de lote e alocações zero passam contra os dois assemblies. Fatores de runtime
e respostas mecânicas permanecem de propriedade da simulação e se atualizam para
duties/aberturas de carga alterados.

A bomba regulada por bateria usa ticks físicos de 100 µs e um regulador de duty de
5 ms. Pulsos de acessório causam queda de barramento medida na simulação. Todos os
**761** limites de relatório/portátil/MCP coincidem. Em 15 s o SOC é **0.6269678451**,
a carga restante **31.3483922575 C**, a tensão terminal **12.6056975839 V**, a
polarização **0.0207555849 V** e a corrente de descarga **0.2052072574 A**. A
pressão termina em **200828.2935823 Pa** para um alvo de 200000 Pa. O último erro
amostrado é **-825.4441034 Pa**; integral/duty retido são **0.0642978516 / 0.0629221114**.
O resíduo final de energia é cerca de `2.13e-7 J`. Impressão digital `40d4fcbab9cad8f8`,
hash final de Windows/runtime `803d9adef384cd35`. Relatório:
`artifacts/reports/battery-regulated-pump.json`. A capacidade de carga de 50 C é
explicitamente um inventário sintético pequeno de verificação, não uma medida de
bateria de OEM. Cada limite verifica a energia elétrica isolada e a ausência de
trabalho de fonte duplicado.

O asset v13 retém parâmetros de OCV/resistência/capacitância/calor da bateria e
período/ganhos/limites/integral inicial do controle de duty. Tipos reassinados
malformados, contagens, unidades, extensões ausentes/duplicadas e rebaixamentos
forjados são rejeitados. O fixture autêntico da bomba regulada v12 retém seu resumo,
impressão digital, referências físicas e replay atualizado no mesmo runtime.
Fixtures anteriores e impressões digitais de modelos sem controle permanecem inalterados.
O agente 0.16.0/MCP verifica descoberta de bateria, replay completo de
experimento/exportação, posse do duty, escritas inválidas de acessório, revisões e
ramificações independentes de bateria/controle.

Vistas de bateria/elétrica/duty no Unity e testes de ciclo de vida de importação/Play
estão preparados. Evidência real de Editor/Play/Mono/IL2CPP/Player e verificações
novas de Linux/macOS permanecem pendentes. Parâmetros afins constantes de bateria,
conversor ideal e programações prescritas de acessório/válvula não estabelecem
BMS/química/envelhecimento, controle PWM/corrente, contatores/falhas, mecânica de
atuadores, ECU/TCU completa, DCT/AT completa, o comportamento restante do motor nem
calibração de veículo. Todos os parâmetros permanecem `unverified`; o objetivo
completo do Power! permanece em aberto.

## 2026-09-30: checkpoint de controle de pressão amostrado

O comando serial exigido passou em Windows x64 com SDK 10.0.401/runtime 10.0.12:

```sh
dotnet run --file tools/Build.cs -- verify
```

- **185/185** verificações gerenciadas, **146/146** verificações de assembly Standard
  hospedadas no .NET 10 e **18/18** grupos de integração MCP de servidor filho real.
- **16/16** Zig e **6/6** verificações de ABI do C#; todos os **176** valores
  históricos correspondem exatamente. A auditoria de fontes encontra zero arquivos
  C/C++/Lua. Compilação Release: zero avisos/erros.
- Log: `artifacts/reports/pressure-control-final-2026-09-30.log`.
- Todos os **15** laboratórios passam no JSON Schema do modelo; **12** casos de
  controlador malformados são rejeitados estruturalmente. A auditoria suplementar
  usa jsonschema 4.25.1 num cache isolado ignorado. Relatório:
  `artifacts/reports/pressure-controller-schema-audit.json`.

`pressure_controller` lê um nó hidráulico e possui uma entrada de tensão de motor CC.
As amostras ocorrem no tempo zero e em múltiplos inteiros de um período alinhado a
ticks; a tensão é retida entre amostras. A primeira amostra retém a integral inicial
fornecida. A integração condicional impede incrementos mais adentro da saturação de
tensão. Quatro estados do controlador e a entrada retida participam de rollback
completo, cancelamento, ramificações, hashes e avanço sem alocação. Sobreposições
externas de tensão são rejeitadas com erros acionáveis `controlled_input`. Veja
[o contrato](HYDRAULIC_PUMP.pt-BR.md#sampled-pressure-regulation).

A evidência numérica independente inclui uma planta amostrada PI/RK4 de
motor/eixo/pressão programada separadamente. Reduzir pela metade os ticks físicos
num período de controle fixo de 10 ms mostra convergência suave de segunda ordem
para essa referência amostrada; isto não é uma reivindicação de convergência de
segunda ordem para um controlador em tempo contínuo. Testes exatos de pressão
constante verificam amostra/retenção, ordenação dos extremos de evento e fase do
relógio da ramificação. Saturação e desenrolamento, unidades/períodos/posse
malformados, capacidade de estado, rollback de falha aritmética tardia, cancelamento,
memória independente e verificações de alocação executam contra os dois assemblies
alvo do Core. Um alvo inalcançável executa e faz replay com sucesso, mas reprova os
KPIs de rastreamento enquanto o comando permanece saturado e a integral é retida.

O asset v12 carrega o registro completo de controle de 80 bytes, com alvo, período
inteiro, ganhos, limites de tensão e integral inicial. Cobertura tipada, contagens
limitadas, unidades/registros malformados, extensões duplicadas/ausentes e
rebaixamentos forjados são rejeitados. O fixture autêntico da bomba em combustão v11
foi capturado antes da mudança do escritor; seu SHA-256 original e a impressão
digital permanecem fixos. Replay atualizado no mesmo runtime e referências físicas
passam, junto com todos os fixtures anteriores e os modelos sem controle inalterados.

O experimento `pressure-regulated-pump` usa ticks físicos de 100 µs, amostras de
controle de 5 ms e setpoints de 300/350/200 kPa com perturbações programadas de
enchimento/drenagem da embreagem. Todos os **757** limites de relatório, portátil e
MCP coincidem. Em 15 s, a pressão de linha é **200550.7972096 Pa** para o alvo de
200000 Pa. A última pressão amostrada é **200544.9882311 Pa**, erro **-544.9882311 Pa**,
integral **0.8124749429 V** e tensão retida **0.8015751783 V**. A corrente do motor
é **0.5964870315 A** e a velocidade do eixo da bomba **2.0526093297 rad/s**.
Impressão digital do modelo `67e8edb13dc42f42`; hash final de estado de
Windows/runtime `44342c02cd41f3c6`. Relatório:
`artifacts/reports/pressure-regulated-pump.json`. Os testes também isolam o trabalho
elétrico dos limites mecânicos de acionado/carga em cada amostra de relatório e
verificam resíduos de volume/energia.

O agente 0.15.0 expõe o contrato de controle, ganhos dimensionais, erros de entrada
pertencente e o exemplo. Testes MCP reais exercitam replay completo de
experimento/exportação, escritas de tensão bloqueadas, atualizações de setpoint,
revisões, ramificações do controlador e independência do pai. O Unity agora prepara
vistas de regulador/sensor/comando e testes de reinício/replay de importação/Play.
Nenhuma evidência real de Editor/Play/Mono/IL2CPP ou Player foi obtida; a verificação
nova de Linux/macOS também permanece pendente. Programações prescritas de válvula e
o sensor/fonte de tensão ideais não implementam ECU/TCU completa, bateria/PWM,
dinâmica de sensor, mecânica de atuadores, DCT/AT completa, o comportamento restante
do motor nem calibração medida. Todos os parâmetros de pesquisa e de amostra
permanecem `unverified`. O objetivo completo do Power! permanece em aberto.

## 2026-09-30: checkpoint retomado de perdas da bomba e alimentação elétrica

O proprietário retomou o desenvolvimento. As mudanças de fonte foram verificadas
localmente em Windows x64 com .NET SDK 10.0.401 e runtime 10.0.12 (o roll-forward
`latestPatch` configurado). O commit base é `d266095`; estas mudanças estavam sem
commit no momento da verificação.

```sh
dotnet run --file tools/Build.cs -- verify
```

- **174/174** verificações gerenciadas, **138/138** verificações de assembly Standard
  hospedadas no .NET 10 e **17/17** grupos MCP contra um servidor filho real.
- **16/16** verificações Zig e **6/6** verificações de ABI do C#; todos os **176**
  valores históricos correspondem exatamente. A auditoria de fontes retém zero
  arquivos C/C++/Lua. Compilação Release: zero avisos/erros.
- Log: `artifacts/reports/pump-assembly-final-2026-09-30.log`. Só o reparo anterior
  da baseline passou 165/165, 132/132 e 15/15 em `resume-baseline-2026-09-30.log`.

A [execução de CI 35809732259](https://github.com/Water-Run/Power/actions/runs/35809732259)
precedente passou no Linux e reprovou quatro verificações de asset em Windows/macOS.
Cada uma falhou só num hash de estado final fixo no código, vindo da execução
original do fixture no Linux, depois que a reprodução original e a atualizada
concordaram. Os resumos dos arquivos de fixture e as impressões digitais do modelo
permanecem exatos. Os testes agora preservam a igualdade de hash no mesmo runtime e
usam referências físicas dos checkpoints documentados, com tolerâncias explícitas
que refletem a precisão publicada. Os hashes históricos permanecem registrados na
proveniência dos fixtures. Esta execução no Windows repara localmente a falha
observada; não estabelece um resultado novo de CI em Linux/macOS.

`HydraulicPumpAssembly` compõe a bomba ideal, o vazamento de pressão da saída para a
entrada e o atrito viscoso do eixo aterrado. A evidência independente cobre todos os
regimes com sinal e as identidades de potência passiva, movimento analítico amortecido
de eixo/pressão, refinamento suave de segunda ordem, inventário de entrada finita,
trabalho de reservatório, uma integração RK4 independente da ODE motor RL/eixo/pressão,
equilíbrio elétrico analítico, rollback completo de falha tardia, cancelamento,
ramificações, imutabilidade e avanço sem alocação. Os dois assemblies alvo executam
as mesmas verificações. O experimento em combustão de perda zero reproduz os
observáveis físicos compartilhados do laboratório original dentro das tolerâncias declaradas.

O laboratório `fired-pump-losses` tem **89** limites coincidentes de relatório/portátil/MCP.
Seu modelo tem 60 estados contados. Em 0.8 s, o trabalho da bomba é **52.6573534421 J**,
o calor de vazamento **8.6081420133 J**, o calor combinado de perda da bomba
**41.0517855370 J**, e o nó térmico da bomba atinge **300.4105178554 K**. A
velocidade final do virabrequim é **68.6812975063 rad/s** e a pressão de linha
**1.0581382360 MPa**. O resíduo de energia é cerca de `-6.13e-10 J`. Impressão
digital `524661ea3d721bbc`. Relatório: `artifacts/reports/fired-pump-losses.json`.

O laboratório `electric-pump` tem **106** limites coincidentes ao longo de 2 s. Seu
motor RL, eixo separado da bomba, vazamento, arrasto, alívio e linha flexível
comandam enchimento/drenagem/recaptura programados da embreagem de pressão. O
trabalho hidráulico externo é **zero**. A velocidade final do eixo da bomba é
**67.0570291504 rad/s**, a corrente **5.3148298325 A**, a pressão de linha
**0.5606191525 MPa** e o deslizamento da embreagem abaixo de `1e-8 rad/s`. O trabalho
da bomba é **3.6117807126 J**; o calor de vazamento é **0.3597803209 J**. O resíduo
de energia é cerca de `5.85e-10 J`. Impressão digital `d8f8fedfdce59003`. Relatório:
`artifacts/reports/electric-pump.json`. As verificações isolam o trabalho elétrico
dos limites separados de eixo acionado/carga e verificam que válvulas seladas
impedem o acionamento por pressão mesmo enquanto a bomba elétrica opera.

O agente 0.14.0 anuncia a composição, as unidades, a semântica de potência e os dois
exemplos. O esquema JSON inalterado e o asset v11 carregam componentes ordinários;
nenhum formato novo nem tipo de componente foi introduzido. Quatorze laboratórios
exportam e executam na ferramenta de compilação serial, incluindo o relatório de CLI
da bomba em combustão que antes era só de exportação.

Testes de replay, reinício e limpeza de importação/Play no Unity estão preparados
para os dois assets novos. `POWER_UNITY_EDITOR` não está definido e o Editor fixado
não foi encontrado no diretório padrão de instalação. Nenhuma evidência real de
Editor/Play/Mono/IL2CPP ou Player de desktop foi obtida. A verificação nova de
Linux/macOS também permanece pendente. Valores constantes de perda, comandos
prescritos e todos os parâmetros de amostra permanecem `unverified`; mapas medidos,
dinâmica de bateria/controle/regulador/pistão, DCT/AT e ECU/TCU completas, o
comportamento restante do motor, amostras de veículo calibradas e a aceitação de
lançamento permanecem inacabados.

## 2026-09-22, checkpoint de encerramento: bomba acionada pelo eixo e alívio de pressão

Acrescentadas bombas de deslocamento reversíveis ideais, entradas explícitas
finitas/de reservatório e alívio de pressão unidirecional de condutância finita.
Velocidades da bomba e pressões hidráulicas entram na solução de Newton de
cilindro/conversor; as capacidades da embreagem de pressão são atualizadas dentro
da iteração de restrição. Transferência aceita de eixo/fluido, trabalho de
reservatório, volume de referência e perdas térmicas compartilham estado
transacional completo. JSON/esquema, asset v11, agente 0.13.0, descoberta MCP e o
laboratório da bomba em combustão usam as mesmas definições.

A verificação serial exigida concluiu com sucesso:

```sh
.cache/dotnet/dotnet run --file tools/Build.cs -p:UseSharedCompilation=false -- verify
```

- **165/165** verificações gerenciadas, **132/132** verificações de assembly Standard
  hospedadas no .NET 10 e **15/15** grupos MCP contra um processo real de servidor filho.
- **16/16** testes Zig e **6/6** testes de ABI do Python; todos os **176** valores
  numéricos históricos retidos correspondem exatamente. A auditoria de fontes retém
  zero arquivos C/C++/Lua.
- Compilação Release: zero avisos e erros. Log:
  `artifacts/reports/pump-integration-verify.log`.

A evidência independente inclui identidades ideais de potência nos dois sentidos,
oscilação analítica de eixo/complacência, inventário de entrada fechada, motorização
reversa, reações de bomba engrenada, decaimento exato de alívio no ponto médio, um
equilíbrio regulado de carga constante e uma solução analítica de realimentação de
pressão de embreagem deslizante. Refinamentos do oscilador suave e da realimentação
da embreagem aproximam segunda ordem. Captura, independência de ramo, cancelamento,
falha numérica tardia, nova tentativa, rejeição de pressão negativa e avanço sem
alocação também passam. O teste inicial de alocação expôs alocação na formatação do
próprio status; a formatação agora se limita a falhas, e o laço quente medido aloca
zero bytes.

Os testes de asset retêm pressão/topologia de entrada e ajuste de alívio, rejeitam
extensões malformadas e duplicadas, dimensões/contagens inválidas e rebaixamentos
forjados. O fixture hidráulico em combustão v10 autêntico retém a impressão digital
`01b69cb3abe52211` e o hash final `46a01d103e6159d3` após a atualização. Resumos e
trajetórias de fixtures anteriores também passam.

O laboratório da bomba em combustão tem **89** limites coincidentes de relatório,
portátil e MCP ao longo de 0.8 s em ticks de 50,000 ns. Seus 56 estados contados
incluem uma linha de alimentação de 4e-12 m³/Pa, uma bomba ideal de 1e-6 m³/rad
acionada pelo virabrequim e um ajuste de alívio de 1e6 Pa com condutância
1e-9 m³/(s·Pa). A energia hidráulica inicial é explicitamente 3 J. O trabalho da
bomba é **53.9425016232 J**, o trabalho hidráulico externo **0 J** e o calor de
alívio **45.0264051429 J**. A pressão final de linha é **1.0697262404 MPa**, a
velocidade de virabrequim/turbina **69.7555356890 rad/s**, a velocidade da carga
**6.6433843513 rad/s** e a temperatura do nó térmico da transmissão **301.5306383749 K**.
O resíduo de energia total é `1.0671e-9 J`; o resíduo de volume de referência é
`3.0493e-20 m³`. Impressão digital `d0bd8f29a706fd89`, hash final `572150ab5d66a2f6`.
Relatório: `artifacts/reports/fired-pump.json`.

Todos os doze documentos de laboratório passam no JSON Schema; dez casos malformados
de bomba/alívio são rejeitados. Auditoria: `artifacts/reports/pump-schema-audit.json`.
Vistas de porta de bomba no Studio e testes de ciclo de vida de importação/Play estão
preparados. `POWER_UNITY_EDITOR` não está definido; Editor, Play Mode e IL2CPP
permanecem não verificados. Estas verificações gerenciadas não são evidência de Unity.

O desenvolvimento está pausado aqui a pedido do proprietário. Perdas/controle da
bomba, carretel do regulador e dinâmica do pistão atuador, DCT/AT completa, ECU/TCU,
comportamento de motor mais rico, amostras de veículo calibradas e aceitação de
desktop permanecem inacabados. Veja [o contrato da bomba](HYDRAULIC_PUMP.pt-BR.md) e
o [status de desenvolvimento](DEVELOPMENT_STATUS.pt-BR.md).

## 2026-09-22: rede hidráulica e transmissão acionada por pressão

Acrescentados nós hidráulicos flexíveis, restrições lineares e turbulentas
regularizadas, pressões explícitas de reservatório e embreagens acionadas por
pressão. Pressão hidráulica e históricos de volume/trabalho/calor participam de
tentativas internas de captura e de transações do lote inteiro. JSON/esquema, asset
v10, capacidades do agente 0.12.0 e o laboratório hidráulico em combustão
compartilham estas definições. Veja [as equações e os limites](HYDRAULIC_NETWORK.pt-BR.md).

Verificado localmente no Linux com:

```sh
.cache/dotnet/dotnet run --file tools/Build.cs -p:UseSharedCompilation=false -- verify
```

- **154/154** verificações gerenciadas e **123/123** verificações de assembly Standard
  de Core/Assets no .NET 10.
- **14/14** grupos MCP contra um processo real de servidor filho.
- **16/16** Zig e **6/6** verificações de ABI do Python; todos os **176** valores
  históricos correspondem exatamente.
- A compilação Release tem zero avisos/erros; a auditoria de fontes passa.
- Log: `artifacts/reports/hydraulic-integration-verify.log`.

A evidência física nova inclui verificações de escoamento/passividade/faixa com
sinal, carga RC analítica, equalização fechada, identidades exatas de trabalho de
reservatório e térmicas, integração RK4 independente de escoamento não linear,
refinamento de segunda ordem de pressão e de impulso de embreagem comandado por
pressão, pré-carga e captura/liberação. Falha após histórico hidráulico/de embreagem
aceito, cancelamento, entrada inválida, ramificações e lote preservam a transação
completa. Captura e leituras de snapshot alocam zero bytes após o aquecimento. Um
passo de drenagem grande demais rejeita pressão manométrica negativa sem mudar o estado.

O replay portátil expôs um campo de pressão de reservatório omitido durante a
implementação. O registro de restrição v10 agora o carrega explicitamente, e os
testes de ida e volta comparam os descritores físicos completos e cada limite de
replay. Registros reassinados malformados, extensões duplicadas/ausentes, dimensões
erradas, portas de atuador inválidas e rebaixamentos de nó só hidráulico são
rejeitados. O fixture v9 autêntico de SHA-256
`96a78326ae2f2e8dd4a434fb81fbe88f4a19156cbf13cf069a7bd4e798e93c9f` retém a impressão
digital `839d03901973668d` e o estado final `834a679376b7a6fd` quando atualizado.
Fixtures mais antigos permanecem.

O laboratório hidráulico em combustão tem **89** limites coincidentes de
relatório/portátil/MCP ao longo de 0.8 s em ticks de 50,000 ns. Impressão digital
`01b69cb3abe52211`, hash final `46a01d103e6159d3`. A velocidade final de
virabrequim/turbina é 70.94321138 rad/s; a velocidade da carga é 6.75649632 rad/s.
Os reservatórios fornecem 8 J, as restrições dissipam 7 J e a energia hidráulica
armazenada aumenta 1 J. O calor do conversor é 48.80297187 J; o calor da trava
32.89304173 J; o calor da embreagem/freio de troca 119.31915873 J e 60.16098886 J.
O nó de calor compartilhado atinge 301.34088081 K. O resíduo de energia total é
`1.0896e-9 J`; o resíduo de volume de referência é `-1.0804e-18 m³`. O trabalho
líquido de fonte externa é -65.07102675 J, incluindo alimentação hidráulica, carga e
trabalho de contrapressão do cilindro. Não é uma medida direta só do trabalho da carga.

Todos os onze documentos de laboratório validam contra o esquema; dez contratos
hidráulicos malformados são rejeitados estruturalmente. O compilador acrescenta
verificações de dimensão/topologia/faixa. Auditoria:
`artifacts/reports/hydraulic-schema-audit.json`. Vistas hidráulicas do Studio e
testes de importação/Play estão preparados, mas `POWER_UNITY_EDITOR` não está
definido; Editor real, renderização, Play Mode e Player/IL2CPP não estão verificados.
Bombas/reguladores, dinâmica de pistão e acumulador, DCT/AT completa, motor/controles
e amostras calibradas permanecem em aberto.

As descrições anteriores de trabalho de fonte, abaixo, agora identificam explicitamente
o trabalho externo líquido: esse balanço inclui a contrapressão do cilindro, então
sua magnitude não deve ser rotulada como trabalho de saída só da carga. Esta é uma
correção da descrição da evidência, não uma mudança de física.

## 2026-09-22: conversor de torque acoplado e laboratório de trava/troca em combustão

Acrescentados quatro mapas explícitos de conversor com sinal, validação de
interpolação passiva, reações de estator estacionário, calor do fluido e uma solução
conjunta conversor/cilindro integrada a restrições de engrenagem e eventos de
embreagem. JSON/esquema, v9 portátil, capacidades do agente 0.11.0 e o exemplo
`fired-converter` compartilham este contrato. Vistas do Studio e testes de
Editor/Play estão preparados. Veja [CONVERTER_NETWORK.md](CONVERTER_NETWORK.pt-BR.md).

O comando serial completo:

```sh
.cache/dotnet/dotnet run --file tools/Build.cs -p:UseSharedCompilation=false -- verify
```

Evidência local no Linux:

- **143/143** verificações gerenciadas; **114/114** verificações de assembly Standard
  de Core/Assets no .NET 10.
- **13/13** grupos MCP contra um servidor filho real.
- **16/16** Zig e **6/6** testes de ABI do Python; todos os **176** números históricos
  correspondem exatamente.
- Compilação Release: zero avisos/erros; a auditoria de fontes passa.
- Log: `artifacts/reports/converter-integration-verify.log`.

As verificações novas cobrem mapas com sinal e continuidade do membro de referência,
balanço de estator/energia, violações de passividade interior, unidades estritas,
posse imutável de pontos, falha por estouro, acoplamento fluido analítico e estol,
refinamento de segunda ordem, ré/coast/contrarrotação, portas compartilhadas,
encaminhamento de perda térmica/externa, reflexão de engrenagem e trava em paralelo.
Verificações de falha, cancelamento e ramo preservam o estado completo; a captura
interna aloca zero bytes após o aquecimento. As verificações portáteis rejeitam
contagens ruins reassinadas, índices/unidades errados, mapas duplicados e
rebaixamentos. O fixture v8 autêntico retém SHA-256
`6872f857bc521ed114d6eea5837cbb380a01f374ddfe7e829b30afa2c44fe0aa`, impressão digital
`6703f00c995e6b62` e estado final `b328de221532fbae` quando lido ou atualizado.
Fixtures mais antigos permanecem inalterados.

O experimento em combustão do conversor de 0.8 s usa ticks de 50,000 ns e reproduz
exatamente todos os **87** limites através do relatório, do asset portátil e do MCP.
Impressão digital `839d03901973668d`, hash final `834a679376b7a6fd`; velocidade de
bomba/turbina 73.37747546 rad/s e velocidade da carga 6.98833100 rad/s. O calor do
fluido é 24.27663069 J, o calor da trava 22.84709072 J, o calor da embreagem de
troca 157.18199410 J e o calor do freio 83.42288714 J. O nó térmico 5 termina em
301.43864301 K, com resíduo de energia total `3.2969e-11 J`. O trabalho líquido de
fonte externa é -63.19344680 J, incluindo carga e trabalho de contrapressão do
cilindro, enquanto o combustível rastreado libera 2049.02269691 J. São saídas numéricas sintéticas.

Um estudo de cinco passos em 50,000/25,000/12,500/6,250/3,125 ns verifica distância
normalizada combinada decrescente na velocidade final do virabrequim, no calor do
fluido e no calor da trava em relação à execução mais fina, mais diferenças absolutas
abaixo de 0.0002 rad/s ou J, respectivamente. Diferenças individuais de calor não são
monótonas perto de eventos de embreagem; nenhuma ordem uniforme de convergência
acoplada é reivindicada. A execução mais fina dá 73.37753852 rad/s, 24.27656820 J e
22.84711838 J. Testes analíticos suaves separados retêm um fator de refinamento
maior que 3.9.

Unity Editor, Play Mode, renderização e IL2CPP reais permanecem não verificados:
`POWER_UNITY_EDITOR` não está definido. Comportamento completo do motor, topologia
DCT/AT, hidráulica, controles e amostras de veículo calibradas permanecem em aberto.
Mapas quase estacionários não estabelecem dinâmica de fluido nem desempenho medido do conversor.

## 2026-09-22: engrenagens ideais acopladas e transmissão planetária em combustão

Engrenagens ideais e restrições planetárias de três portas agora compartilham a
solução eletromecânica, de cilindro e de embreagem. A projeção direta de restrição
preserva o movimento compatível e a fase relativa inicial; reações médias por porta
são observáveis e transacionais. JSON/esquema, asset v8, CLI/MCP e Studio usam a
mesma topologia. Veja [o contrato de engrenagem e os limites numéricos](GEAR_NETWORK.pt-BR.md).

O comando serial completo passou em Linux x64 com .NET SDK 10.0.400/runtime 10.0.11
em cache e Zig 0.15.2:

```sh
.cache/dotnet/dotnet run --file tools/Build.cs -p:UseSharedCompilation=false -- verify
```

- **131/131** verificações gerenciadas de Core/aplicação.
- **105/105** verificações de assembly Standard de Core/Assets hospedadas no .NET 10.
- **12/12** grupos MCP de servidor filho real.
- **16/16** grupos Zig, **6/6** testes de ABI do Python e **176** valores de baseline
  históricos correspondentes exatamente. A auditoria de fontes não encontra arquivos
  de implementação C/C++ ou Lua.
- A compilação Release informa zero avisos/erros. Log: `artifacts/reports/gear-integration-verify.log`.

Nove grupos de grafo comparam razões positivas/negativas e movimento/reações
planetários livres com referências exatas independentes, inércia refletida de vários
estágios e ordenação por ID estável, equivalência de motor RL/térmico e de cilindro
reagente, e redução analítica/captura direta de troca e calor. Um oscilador
restrito demonstra convergência de segunda ordem e conservação de energia. Rollback
completo após um prefixo de troca aceito, cancelamento, independência de ramo,
replay em lote exato e alocação zero são verificados; a cobertura de alocação inclui
captura interna com fatores variáveis. Diagnósticos de posto, velocidade inicial,
porta, razão e parâmetro não suportado são explícitos.

Dois grupos de asset cobrem topologia de três portas, cada limite de reprodução,
contagens malformadas, registros ausentes/duplicados/de tipo errado, porta-satélites
inválidos e tentativas de engrenagem rebaixadas. Um fixture genuíno de embreagem em
combustão v7 conserva a impressão digital `197be44884deee90` e o hash final
`28bf5335d8e35cde` após a atualização. Fixtures mais antigos e modelos sem engrenagem
permanecem inalterados. Dois grupos de integração gerenciados acrescentam contratos
estritos de JSON/agente, comportamento de revisão/cancelamento/ramo e a distinção
entre execução bem-sucedida e KPIs aprovados.

O laboratório planetário novo em combustão tem **84 limites coincidentes** entre
lotes alternados, reprodução portátil e MCP. Seu experimento de subida/descida de
0.8 segundo registra **-56.83157714 J** de trabalho líquido de fonte externa, gerando
**254.52399968 J** na embreagem sol/coroa e **156.31560557 J** no freio da coroa. O
nó térmico termina em **302.05419803 K**; as velocidades de virabrequim/carga são
**76.81548837 / 7.31576080 rad/s**, com a coroa retida. O resíduo final de energia é
**2.51020538e-10 J**. A impressão digital é `6703f00c995e6b62`; o hash final é
`b328de221532fbae`. Fonte/relatório são `assets/labs/fired-planetary.power.json` e
`artifacts/reports/fired-planetary.json`. Os parâmetros permanecem sintéticos e
`unverified`. Desativar a programação de troca remove o calor da embreagem sol/coroa
e muda o movimento da carga.

O Studio tem vistas esquemáticas de planetário de três portas e de transmissão final,
com testes preparados de importação, replay de troca, reinício e limpeza.
`POWER_UNITY_EDITOR` não está definido: nenhuma evidência de Editor/Play/IL2CPP é
reivindicada. Topologia DCT/AT completa, conversor, hidráulica, ECU/TCU, o
comportamento restante do motor, calibração medida de veículo e aceitação de
lançamento permanecem em aberto.

## 2026-09-22: referências independentes de engrenagem ideal e planetário

Acrescentados os primitivos imutáveis `IdealGearPair` e `SimplePlanetaryGear` para
torques externos constantes. Os resultados expõem velocidades dos membros,
deslocamentos, torques de reação, trabalho, variação de energia cinética e resíduo.
As velocidades iniciais devem satisfazer a restrição; nenhuma sincronização de
deslizamento finito é inferida. Veja [equações, sinais e limites](IDEAL_GEARS.pt-BR.md).

O comando serial completo passou em Linux x64 com SDK 10.0.400/runtime 10.0.11 em cache:

```sh
.cache/dotnet/dotnet run --file tools/Build.cs -p:UseSharedCompilation=false -- verify
```

- **118/118** verificações gerenciadas de Core/aplicação e **94/94** verificações de
  assembly Standard de Core/Assets hospedadas no .NET 10; oito grupos novos executam
  contra cada alvo do Core.
- **11/11** grupos MCP de servidor filho real, **16/16** grupos Zig e **6/6** testes
  de ABI do Python. Todos os **176** valores de baseline históricos correspondem
  exatamente; a auditoria de fontes não encontra arquivos de implementação C/C++ ou Lua.
- A compilação Release tem zero avisos/erros. Log:
  `artifacts/reports/ideal-gear-reference-verify.log`.

As verificações novas cobrem razões de engrenagem positivas/negativas, inércia
refletida, balanço de impulso por membro, potência de reação nula, dinâmica
independente de força de restrição planetária, três condições de membro retido e
tração direta sol/coroa. Cargas de retenção e de trava são explícitas. Resultados
de carga constante coincidem com intervalos particionados, incluindo reversão de
velocidade. A amostragem de ponto médio de cargas senoidais converge contra integrais
independentes a cerca de quatro vezes de redução de erro por metade de intervalo,
para os dois primitivos.

A varredura determinística inclui **2,500 casos por referência**. **10,000 avaliações
de cada primitivo** não alocam memória gerenciada; chamadores concorrentes
independentes compartilham só parâmetros imutáveis. Valores inválidos, velocidades
iniciais incompatíveis, condicionamento do construtor, estouro aritmético e erros
finitos de cancelamento de força rejeitam sem resultado parcial. Uma regressão de
razão alta preserva uma reação pequena, fisicamente exigida, em vez de perdê-la por
subtração de torques quase iguais.

A semântica do solver de grafo e do asset v7 não muda; as verificações existentes de
laboratório, portátil e replay MCP permanecem aprovadas. Estes primitivos ainda não
são componentes de transmissão acoplados, ferramentas de agente, simulações de troca
nem modelos calibrados. A verificação real de Unity Editor/Play/IL2CPP permanece
pendente, assim como o restante do motor, DCT/AT, hidráulica, controles e os
objetivos completos de veículo calibrado.

## 2026-09-22: embreagens acopladas e integração da carga do motor em combustão

Reações estáticas/cinéticas da embreagem agora compartilham a solução
eletromecânica/de cilindro, com eventos internos limitados de captura/reversão e
encaminhamento de calor de atrito. Fase, saídas médias e calor compensado são
transacionais e entram no hash. JSON/esquema, CLI/MCP, asset v7 e Studio consomem o
mesmo componente; os leitores v1–v6 permanecem suportados. Veja
[as equações e os limites numéricos explícitos](CLUTCH_NETWORK.pt-BR.md).

O comando serial completo passou em Linux x64 com .NET SDK 10.0.400/runtime 10.0.11
em cache e Zig 0.15.2:

```sh
.cache/dotnet/dotnet run --file tools/Build.cs -p:UseSharedCompilation=false -- verify
```

- **110/110** verificações gerenciadas de Core/aplicação.
- **86/86** verificações de assembly Standard de Core/Assets hospedadas no .NET 10.
- **11/11** grupos de integração MCP de processo filho real.
- **16/16** grupos Zig e **6/6** testes de ABI do Python; todos os **176** valores
  históricos correspondem exatamente. A auditoria de fontes não encontra arquivos
  de implementação C/C++ ou Lua.
- A compilação Release informa zero avisos e erros. O log local é
  `artifacts/reports/clutch-integration-verify.log`.

A evidência física nova compara o grafo com o `ClutchPair` exato de carga constante
através de engate interno e reversão, razões positivas/negativas e os dois destinos
térmicos. Trajetórias de motor/corrente travados e de pressão/combustível de cilindro
reagente coincidem com modelos separados de inércia combinada analiticamente. Um
oscilador mola/freio coincide com movimento senoidal por partes através de três
reversões e captura final no quarto ponto de retorno; reduzir o tick pela metade
reduz o erro em mais de 3.7x. Laços de três embreagens exercitam restrições
redundantes e engate simultâneo com movimento e energia conservados. A liberação
estática exige saturação; o resíduo da resolução de raiz não pode criar uma segunda
reversão espúria.

O rollback completo de vários ticks é verificado após um prefixo aceito de
aquecimento/captura e uma sobrecarga numérica posterior. Replay programado,
cancelamento, independência de ramificação, posse imutável e alocação zero são
retidos. As verificações de alocação incluem eventos internos repetidos de reversão,
exercitando cópias de candidatos e fatores variáveis. Estes testes sustentam o
escopo documentado do solver, não precisão híbrida arbitrária de tick grande.

O laboratório novo `fired-clutch` tem 67 limites de relatório exatamente coincidentes
entre lotes alternados, reprodução portátil e MCP. Seu relatório de 0.6 segundo
registra 96.74607609 J de trabalho externo líquido exportado, incluindo a carga e a
contrapressão do cilindro, 191.55570747 J de calor de embreagem, velocidade final de
motor/carga 68.58488546 rad/s e resíduo final de energia de 1.79e-10 J. Sua impressão
digital é `197be44884deee90` e o hash de estado final `28bf5335d8e35cde`. O nó
térmico da embreagem atinge 300.95777854 K. A fonte e o resultado são
`assets/labs/fired-clutch.power.json` e `artifacts/reports/fired-clutch.json`; os
parâmetros permanecem sintéticos e `unverified`.

A ida e volta do asset v7 conserva capacidades e canais, rejeita extensões
malformadas/ausentes/duplicadas e rebaixamentos inválidos, e preserva o resumo, a
impressão digital e o replay atualizado de um fixture autêntico de cilindro em
combustão v6. Hashes de modelos anteriores permanecem inalterados. Verificações
estritas de JSON/agente retêm erros acionáveis, atomicidade de entrada/revisão e a
distinção entre execução bem-sucedida, KPIs aprovados e calibração medida.

Placas de embreagem do Studio, saídas de fase nomeadas e testes de importação/ciclo
de vida estão preparados. `POWER_UNITY_EDITOR` ainda não está definido: Editor,
renderização, Play Mode e IL2CPP permanecem não verificados. Topologia DCT/AT,
conjuntos planetários, conversor de torque, hidráulica, controles, comportamento
completo do motor e amostras de veículo calibradas permanecem inacabados.

## 2026-09-22: lei de embreagem a seco gerenciada e referência de carga constante

Acrescentados `DryClutch`, uma lei imutável de capacidade de torque estático/cinético,
e `ClutchPair`, uma referência exata de duas inércias ou freio ao solo sob carga
constante. Um evento de deslizamento zero é resolvido dentro do intervalo, seguido
de movimento restrito ou reversão. Os resultados expõem movimento, avanços angulares,
modo de reação, impulso, calor, trabalho externo e variação de energia. Veja
[as equações, a API e o limite de implementação](CLUTCH_PHYSICS.pt-BR.md).

O comando serial passou em Linux x64 com .NET SDK 10.0.400/runtime 10.0.11 em cache
e Zig 0.15.2:

```sh
.cache/dotnet/dotnet run --file tools/Build.cs -p:UseSharedCompilation=false -- verify
```

- **99/99** verificações gerenciadas de Core/aplicação e **77/77** verificações de
  assembly Standard hospedadas no .NET 10, incluindo os mesmos dez grupos novos de
  embreagem nos dois alvos.
- **10/10** grupos de integração MCP de processo filho real.
- **16/16** grupos Zig e **6/6** testes de ABI do Python. Todos os **176** valores
  numéricos históricos correspondem exatamente. A auditoria de fontes encontra zero
  arquivos de implementação C/C++ ou Lua.
- A compilação Release informa zero avisos e erros. A saída completa é retida
  localmente em `artifacts/reports/clutch-kernel-verify.log`.

A evidência física cobre engate analítico, partilha exata de carga estática,
descolamento, reversão, eventos de extremo, engate parcial, um freio ao solo e
razões de engrenagem com sinal. Os testes verificam de forma independente momento,
trabalho externo integrado e energias cinéticas absolutas, em vez de comparar só os
contadores de energia da implementação. Um par com inércias 0.2 e 0.8 kg m2,
velocidades iniciais 100 e 0 rad/s e capacidade deslizante 10 Nm sincroniza a
20 rad/s após 1.6 s, gerando 800 J de calor.

Soluções de carga constante concordam entre partições de intervalo que cortam
eventos híbridos. Cargas senoidais congeladas no ponto médio convergem contra
integrais independentes de velocidade, ângulo e calor em mais de 3.8x por metade,
com o erro máximo mais fino abaixo de 2e-5 nas saídas SI testadas. Uma varredura
determinística de 2,000 casos verifica conservação e avaliação repetida. Ela encontrou
e corrigiu contagem excessiva de um ulp da duração de deslizamento durante uma
reversão. Dados inválidos e falhas de aritmética/resolução de evento não publicam
resultado parcial. O caminho de medição isolado e aquecido registra alocação zero
para 10,000 intervalos.

Este é um primitivo físico autônomo do Core, **ainda não um componente de grafo compilado**.
Acoplamento de eixo/motor/cilindro, restrições de várias embreagens, encaminhamento
térmico, estado híbrido transacional, representação JSON/asset/MCP e integração ao
Studio permanecem pendentes. Impressões digitais de grafo existentes, sete
experimentos de laboratório e a semântica do asset v6 permanecem inalterados. A
evidência de combustão anterior deles é retida abaixo.

`POWER_UNITY_EDITOR` permanece indefinido. Estes testes de assembly Standard não
estabelecem comportamento real de importação Unity, Play Mode ou IL2CPP. Todos os
parâmetros de veículo permanecem `unverified`; o primitivo novo não completa uma
transmissão, controles nem um powertrain calibrado.

## 2026-09-22: combustão premisturada, reagentes transportados e trabalho da carga em combustão

Acrescentados rastreamento opcional de combustível/ar fresco/produtos nos nós de gás
e frações explícitas de reservatório, mais um componente `premixed_combustion`
referenciado ao virabrequim. Um hazard de Wiebe prescrito consome os reagentes
limitantes, armazena histórico irreversível do virabrequim e converte energia química
em energia térmica. A prévia de calor participa do trabalho conservativo do
virabrequim. Veja [o modelo, as equações e as limitações](PREMIXED_COMBUSTION.pt-BR.md).

A verificação serial completa passou em Linux x64 com .NET SDK 10.0.400/runtime
10.0.11 em cache e Zig 0.15.2:

```sh
.cache/dotnet/dotnet run --file tools/Build.cs -p:UseSharedCompilation=false -- verify
```

- **89/89** verificações gerenciadas de Core/aplicação.
- **67/67** verificações de assembly Standard de Core/Assets hospedadas no .NET 10.
- **10/10** grupos de integração MCP de processo filho real.
- **16/16** grupos Zig e **6/6** testes de ABI do Python, incluindo os dois hosts nativos.
- Todos os **176** valores nativos históricos correspondem exatamente; zero fontes
  C/C++ ou Lua encontradas. A compilação Release informa zero avisos e erros.

A evidência física nova inclui:

- Exposição analítica de Wiebe através de ciclos explícitos, fases negativas e
  envolvimento de ciclo. Combustível de vaso fechado, consumo de ar fresco, calor e
  temperatura coincidem com a solução de forma fechada do reagente limitante para
  cargas pobre, rica, sem combustível e sem ar. Massa e energia térmica-mais-química
  total são verificadas independentemente do contador de calor.
- A conservação de constituintes em rede fechada e o enchimento/descarga de
  reservatório transportam a composição de montante e a entalpia química em qualquer
  sentido de escoamento. Um vaso de massa constante e temperatura constante com
  entrada/saída bloqueadas equilibradas coincide com a substituição exponencial da
  mistura dentro de 2e-5 em fração de massa, mesmo com mais de uma renovação de vaso
  por tick externo. Isto exercita o limite de escoamento de saída quando as taxas
  líquidas de massa e de energia térmica sozinhas não fornecem um passo útil de traçador.
- Uma referência RK4 de cilindro reagente escrita de forma independente integra
  movimento do virabrequim, massa, energia térmica, combustível e ar fresco com
  descarga bloqueada. Suas soluções de 1 e 0.5 microssegundo diferem em menos de 1e-9
  normalizado. Ticks do Core de 100, 50 e 12.5 microssegundos reduzem o erro
  normalizado máximo em mais de 2.8x e depois 8x, com o mais fino abaixo de 1e-4.
  Esta é evidência de segunda ordem sem parede; o acoplamento de parede permanece de primeira ordem.
- Vários cilindros reagentes em virabrequins compartilhados ou acoplados por eixo
  conservam energia total e constituintes, incluindo misturas isoladas com poderes
  caloríficos e razões estequiométricas diferentes. Parar, inverter e retraçar não
  pode repetir a liberação de calor; desativar uma queima pula a exposição à frente
  sem recuperação posterior. O maior ângulo de virabrequim visitado é observável como
  `burn_frontier_angle`.
- Mudanças de torque programadas que falham após reação parcial fazem rollback do
  estado de constituintes, do histórico de ângulo e dos balanços compensados.
  Cancelamento, independência de lote do chamador, isolamento de ramificação,
  rejeição/recuperação de queima sub-resolvida e alocações zero de avanço/snapshot
  passam contra os dois assemblies. Composição estrita, posse, unidades e o orçamento
  estendido de 64 estados são verificados.

O [laboratório de cilindro em combustão](../assets/labs/fired-cylinder.power.json)
passa nos KPIs com impressão digital `a10f880d74494677` e **63** limites de relatório
coincidentes. JSON/CLI, MCP e o asset v6 decodificado concordam em cada canal em cada
limite, incluindo dois eventos de carga entre tempos de relatório. A execução de
0.6 segundo registra **-369.98 J** de trabalho líquido de fonte externa, consome
**3.265e-5 kg** de combustível na reação e libera **1436.67 J**. A energia líquida
final de combustível no limite é **1785.07 J**, com combustível também restante na
câmara; estes números transitórios não são uma reivindicação de eficiência estacionária
nem de economia de combustível. Desativar a combustão remove a liberação de calor e
produz velocidade de virabrequim substancialmente menor sob a mesma carga.

O resíduo final de energia é aproximadamente **-2.11e-9 J**, o resíduo de massa total
**-1.25e-18 kg**, o resíduo de combustível **2.03e-20 kg** e o resíduo de ar fresco
**1.41e-18 kg**. A pressão amostrada atinge o pico em cerca de **2.08 MPa** e a
temperatura em **1761 K**. São saídas de modelo sintético; a amostragem de relatório
de 10 ms não estabelece o pico contínuo de pressão/temperatura.

O asset v6 retém leitores v1–v5. Testes novos preservam definições de
modelo/mistura/queima e o replay, rejeitam semântica de extensão errada/ausente/duplicada
e contagens malformadas, e verificam o resumo, a impressão digital e o replay
atualizado de um fixture v5 autêntico anterior à mudança. A
[proveniência dos fixtures](../tests/Power.Tests/Fixtures/README.md) registra seu
checkpoint de fonte sem commit, sem reivindicar um commit publicado. Impressões
digitais não reagentes existentes permanecem inalteradas. Os testes de agente cobrem
validação estruturada, entrada inválida e cancelamento sem mudança de revisão,
escritas obsoletas, independência de ramo, saídas filtradas de combustível/calor,
recuperação com tick menor e a distinção entre execução bem-sucedida e KPIs reprovados.

O esquema Draft 2020-12 e todos os **sete** laboratórios passam no `jsonschema` do
Python. Oito formas malformadas de composição/queima são rejeitadas, incluindo
frações ausentes, constituintes desconhecidos, unidades erradas, frações inválidas,
parâmetros de queima ausentes e frações de reservatório deslocadas/nulas. Verificações
do compilador impõem separadamente somas de fração e compatibilidade da mistura
conectada. A evidência local está em `artifacts/reports/combustion-verify.log` e
`artifacts/reports/fired-cylinder.json`.

Os testes de importação e Play Mode do Unity agora incluem um marcador de liberação
de calor, reinício e replay completo do exemplo em combustão. **Eles não executaram
no Editor**: `POWER_UNITY_EDITOR` não está definido. Nenhuma reivindicação de
renderização, Mono/IL2CPP ou Player é inferida de testes de assembly Standard.
R/gamma constantes, queima prescrita e a política de fronteira à frente são limites
explícitos; dosagem de combustível, controle de ignição, química preditiva,
admissão/escape detalhados, perdas mecânicas, transmissões, controles e amostras de
veículo calibradas permanecem em aberto.

## 2026-09-22: sincronismo de válvulas por ângulo de virabrequim e motoreagem em velocidade variável

Acrescentado `valve_timing` opcional nas restrições de gás, com ciclos explícitos de
360/720 graus, ângulos de abertura/duração, entrada de pico e saída de abertura
efetiva. Os perfis seguem o ângulo real do virabrequim através de aceleração, parada,
reversão e envolvimento de fase. Lobos sub-resolvidos rejeitam o lote completo. Veja
[as equações, os limites e o escopo](VALVE_TIMING.pt-BR.md).

O comando serial completo passou em Linux x64 com .NET SDK 10.0.400/runtime 10.0.11
em cache e Zig 0.15.2:

```sh
.cache/dotnet/dotnet run --file tools/Build.cs -p:UseSharedCompilation=false -- verify
```

- **76/76** verificações gerenciadas de Core/aplicação.
- **57/57** verificações contra assemblies .NET Standard 2.1 de Core/Assets hospedados no .NET 10.
- **9/9** grupos de integração MCP de processo filho real.
- **16/16** grupos Zig e **6/6** testes de ABI do Python, com os dois hosts nativos executados.
- Todos os **176** valores nativos históricos correspondem exatamente; a auditoria de
  fontes encontra zero arquivos C/C++ ou Lua. A compilação Release informa zero avisos e erros.

Evidência física adicional:

- Descarga adiabática bloqueada de forma fechada com exposição de válvula seno ao
  quadrado integrada de forma independente, a +40 e -40 rad/s, testa o acoplamento
  de sincronismo a escoamento através do envolvimento de ciclo. Refinar ticks de 1 ms
  para 0.5 ms reduz o erro relativo de massa em mais de 2.8x; 0.125 ms o reduz em
  mais de 8x adicionais, para abaixo de 1e-7. Balanços de energia/massa são verificados separadamente.
- Uma referência RK4 independente de cilindro móvel inclui trabalho de pressão do
  virabrequim, escoamento bloqueado e um lobo estreito cronometrado. A trajetória
  cruza os dois limites do lobo. Reduzir pela metade o passo de referência de 1 para
  0.5 microssegundo muda os resultados normalizados em menos de 1e-10. Ticks do Core
  de 200, 100 e 25 microssegundos reduzem o erro em mais de 2.8x e depois 8x, com o
  erro mais fino abaixo de 1e-6. Evidência de segunda ordem sem parede não muda o
  acoplamento de parede de primeira ordem documentado.
- Verificações cinemáticas exatas de torque constante cobrem abertura durante
  desaceleração/reversão; válvulas estacionárias e desativadas retêm o comportamento
  documentado. Um tick que abrange um lobo estreito inteiro com extremos fechados
  deve falhar e sofrer rollback. Reduzir o tick resolve seu escoamento.
- Cancelamento, programações reprovadas, independência de lote do chamador, isolamento
  de ramificação, parâmetros de sincronismo malformados e avanço/snapshots sem
  alocação passam contra os dois assemblies.

O [laboratório cronometrado pelo virabrequim](../assets/labs/crank-timed-cylinder.power.json)
passa em todos os KPIs com impressão digital `38f0437eac4def69` e **63** limites de
relatório coincidentes. JSON/CLI, MCP e o asset v5 decodificado concordam em cada
limite, incluindo dois eventos de torque entre tempos de relatório. O resíduo final
de energia é aproximadamente **8.53e-10 J**, e o resíduo de massa é **-2.87e-18 kg**.
A velocidade amostrada do virabrequim vai de **53.25 a 63.34 rad/s**, enquanto a
abertura é verificada de forma independente contra o ângulo do virabrequim. São
verificações numéricas de parâmetros sintéticos, não calibração.

O asset v5 retém leitores v1–v4. Os testes rejeitam contagens malformadas, registros
de sincronismo duplicados/errados e semântica de sincronismo removida, e verificam
um fixture v4 autêntico anterior à mudança com seu resumo original, impressão digital
e replay atualizado. A proveniência dos fixtures está registrada
[nas notas dos fixtures](../tests/Power.Tests/Fixtures/README.md). Impressões digitais
de modelos anteriores permanecem inalteradas. Os testes MCP também preservam
estado/revisão diante de entrada de pico inválida, e as verificações de aplicação
distinguem execução bem-sucedida de KPIs reprovados e demonstram recuperação de uma
falha de runtime de lobo estreito recriando com um tick menor.

O esquema Draft 2020-12 e todos os **seis** documentos de laboratório passam no
`jsonschema` do Python. Seis formas de sincronismo malformadas são rejeitadas,
incluindo campos ausentes, campos extras de perfil, unidades erradas, um posicionamento
inválido de componente e sincronismo nulo. Testes do compilador cobrem separadamente
restrições de ciclo/faixa e de topologia.

A evidência é execução local no Linux, registrada em
`artifacts/reports/valve-timing-verify.log` e
`artifacts/reports/crank-timed-cylinder.json`. Testes novos de importação/Play no
Unity verificam marcadores cronometrados, reinício e replay, mas **não executaram no
Editor**: `POWER_UNITY_EDITOR` não está definido. Nenhum resultado de Editor,
renderização, Mono/IL2CPP ou Player de desktop é inferido das verificações
gerenciadas. Combustão, comportamento completo do motor, transmissões, controles e
amostras de veículo calibradas permanecem em aberto; todos os parâmetros de pesquisa
permanecem `unverified`.

## 2026-09-22: troca de gás do cilindro móvel e trabalho conservativo no virabrequim

Acrescentado `gas_cylinder`, um componente de geometria que liga um virabrequim
rotacional e uma câmara de gás com massa/energia interna independentes. O volume
inicial é derivado da posição do virabrequim e da geometria; volume/posse ambíguos
são rejeitados. Troca de gás, trabalho do virabrequim e transferência de parede
correm por rollback de lote inteiro, ramificações e cancelamento. As mesmas
definições são aceitas por JSON/CLI/MCP e pelo asset portátil v4, com leitores
v1/v2/v3 retidos. Veja [as equações e o contrato](MOVING_CYLINDER.pt-BR.md).

A verificação serial completa passou em Linux x64 com .NET SDK 10.0.400/runtime
10.0.11 em cache e Zig 0.15.2:

```sh
.cache/dotnet/dotnet run --file tools/Build.cs -p:UseSharedCompilation=false -- verify
```

- **68/68** verificações gerenciadas de Core/aplicação.
- **51/51** verificações contra assemblies .NET Standard 2.1 de Core/Assets hospedados no .NET 10.
- **8/8** grupos de integração MCP de processo filho real.
- **16/16** grupos Zig e **6/6** testes de ABI do Python, com os dois hosts nativos executados.
- Todos os **176** valores de baseline nativos originais correspondem exatamente; o
  inventário de fontes contém zero arquivos C/C++ e Lua. A compilação Release informa
  zero avisos e erros.

As verificações de física novas cobrem:

- Concordância de válvula fechada com o benchmark do cilindro fechado durante rotação
  à frente/ré, pontos mortos e ticks minúsculos; massa constante e energia conservada.
- Movimento de escoamento bloqueado aberto comparado com uma integração RK4 escrita
  de forma independente das ODEs de massa, energia e virabrequim. Suas equações de
  geometria e escoamento não chamam os auxiliares do Core sob teste. Reduzir pela
  metade o passo de referência de 1 para 0.5 microssegundo muda os resultados
  normalizados em menos de 1e-10. Reduzir o tick do Core de 200 para 100 microssegundos
  reduz o erro de escoamento suave em mais de 3x; 25 microssegundos o reduzem em mais
  de 10x adicionais e permanecem abaixo de 1e-6 relativo.
- O refinamento acoplado à parede é avaliado separadamente como primeira ordem: os
  mesmos refinamentos reduzem o erro em mais de 1.7x e 3x, respectivamente, com o
  erro mais fino abaixo de 1e-5 relativo.
- Virabrequins compartilhados/acoplados, cilindros fechados e abertos mistos, elos de
  gás, calor de parede e balanços completos de energia/massa. Mudanças de torque
  programadas que falham restauram todos os ticks e entradas anteriores; cancelamento,
  isolamento de ramificação e alocações zero de avanço/snapshot passam.

O laboratório novo de motoreagem tem impressão digital `dd62971021fa06e6` e **28**
limites de replay, todos idênticos entre relatórios de experimento JSON, assets
decodificados e exportação MCP real. Ele admite e expulsa gás de forma demonstrável
enquanto a câmara se move. O resíduo final de energia é `2.9882230023758893e-10 J`;
o resíduo de massa é `1.463672932855431e-18 kg`. São observações numéricas de
conservação para parâmetros sintéticos, não calibração. Os quatro relatórios de
laboratório precedentes retêm suas impressões digitais e passam no replay/KPIs.

A cobertura portátil inclui registros mistos de cilindro antigo/novo, ida e volta
exata da geometria, extensões de tipo errado/duplicadas/ausentes, contagens inválidas
e rejeição de registros de cilindro móvel sob versões mais antigas. O fixture v3
salvo de volume fixo retém a impressão digital `eeb18a7f1dc76175` e o replay após a
recodificação v4. A proveniência de fonte/resumo dos fixtures está registrada em
[Fixtures](../tests/Power.Tests/Fixtures/README.md).

O esquema Draft 2020-12 e todos os cinco laboratórios passam no `jsonschema` do
Python; seis documentos malformados de cilindro móvel são rejeitados. Os testes de
agente cobrem erros de geometria, posse da câmara, falha não linear limitada sem
mudança de revisão/estado e recuperação. Os logs são
`artifacts/reports/moving-cylinder-verify.log` e
`artifacts/reports/moving-cylinder-schema.log`; o experimento é
`artifacts/reports/moving-cylinder.json`.

Testes de pistão móvel/importação/Play no Unity estão preparados, mas não executados:
`POWER_UNITY_EDITOR` não está definido. Unity Editor/Play/renderização, Mono/IL2CPP,
empacotamento de Player e a execução deste incremento em Windows/macOS permanecem não
verificados. Aberturas de restrição programadas no tempo não implementam sincronismo
de válvulas por ângulo de virabrequim. Combustão, comportamento completo do ciclo do
motor, transmissões, controles e amostras de veículo calibradas permanecem em aberto;
o objetivo completo do Power! não está completo.


## 2026-09-22: integração JSON, asset e agente da rede de gás finita

O checkpoint do Core em `69bc1c4` foi verificado antes das mudanças: **53/53
gerenciadas, 41/41 de assembly Standard e 6/6 grupos MCP**. As equações existentes
do solver, a construção da impressão digital e os limites físicos não mudam neste incremento.

A verificação serial completa passou então em Linux x64 usando o .NET SDK 10.0.400
fixado em cache e Zig 0.15.2:

```sh
.cache/dotnet/dotnet run --file tools/Build.cs -p:UseSharedCompilation=false -- verify
```

- **60/60** verificações de Core/aplicação no .NET 10.
- **45/45** verificações contra os assemblies .NET Standard 2.1 de Core/Assets, hospedados no .NET 10.
- **7/7** grupos de integração MCP contra um servidor filho real.
- **16/16** grupos Zig nativos e **6/6** testes de ABI do Python; os dois hosts nativos executaram.
- Todos os **176** valores de baseline nativos originais correspondem exatamente; zero arquivos C/C++ e Lua.
- Compilação Release: **zero avisos e zero erros**.

Os quatro relatórios de laboratório passam em KPIs e replay: eletrotérmico (11 limites),
rede térmica (11), cilindro fechado (21) e rede de gás (14). O documento de gás
compila para a mesma impressão digital de uma definição do Core montada de forma
independente: `eeb18a7f1dc76175`. Relatórios JSON, reprodução v3 decodificada e
exportação MCP real concordam em cada limite de relatório de gás, incluindo eventos
de válvula entre limites de amostragem. As verificações de resíduo de massa e energia
usam limites absolutos de 1e-14 kg e 1e-6 J, respectivamente; o teste também
reconstrói a troca de energia do reservatório a partir dos estados de câmara e parede.

As verificações portáteis incluem topologia mista de gás/cilindro/térmica, composição
de gás não padrão, unidades não SI, posse, limites de programação, cancelamento e
falha no meio do lote com rollback do cursor de eventos. Arquivos reprocessados no
hash de forma correta, mas inválidos, cobrem contagens, registros de extensão
ausentes/duplicados/de tipo errado, rejeição de gás de versão antiga e impressões
digitais obsoletas. Fixtures autênticos v1 e de cilindro v2 retêm a impressão digital
original e o comportamento de replay após a recodificação v3; o commit de fonte e o
hash do fixture v2 estão registrados em
[Fixtures](../tests/Power.Tests/Fixtures/README.md).

As verificações de agente cobrem descoberta, limites de abertura inicial/programada,
atomicidade de entrada inválida, conflitos de revisão, cancelamento, independência
de ramo e a distinção entre uma chamada bem-sucedida e um KPI reprovado.
Separadamente, o `jsonschema` do Python validou o esquema Draft 2020-12 publicado,
todos os quatro documentos de laboratório e uma variante de abertura fixa, e rejeitou
doze casos de documento de gás malformados.

Logs: `artifacts/reports/gas-integration-baseline.log`,
`artifacts/reports/gas-integration-verify.log` e
`artifacts/reports/gas-integration-schema.log`. Relatórios e assets Unity gerados
permanecem artefatos de compilação reproduzíveis, não fixtures de fonte.

`POWER_UNITY_EDITOR` não está definido. Vistas esquemáticas de gás, testes de
importação e um teste de ciclo de vida/replay em Play Mode estão preparados, mas
**não executados no Unity**. Editor/Play/renderização, Mono/IL2CPP, empacotamento de
Player e a execução deste incremento em Windows/macOS permanecem não verificados.
Física completa do ciclo do motor, transmissões, controles e calibração de veículo
permanecem em aberto; os parâmetros de amostra permanecem `unverified`.

## 2026-09-19: cadeia de ferramentas Python retirada; verificação nativa movida para C#

O repositório não contém mais Python. `tools/Install Zig.py`, `tools/VerifyNative.py`,
`legacy/native/tools/model_lab.py` e seus testes foram portados para C# e incorporados
à ferramenta de compilação de arquivo único `tools/Build.cs` (aplicativos baseados em
arquivo do .NET 10 permitem um arquivo-fonte). O host ctypes tornou-se P/Invoke. Os
atributos externos de consumidor de ABI não mudam. O instalador Zig usa o extrator ZIP
embutido no Windows e delega plataformas `tar.xz` ao `tar` do sistema. A CI e a
documentação foram atualizadas na mesma mudança.

Verificação serial local em Windows x64 (`install-zig` + `native-verify`):

- Auditoria de fontes: 0 arquivos C/C++, 0 arquivos Lua, 35 fontes Zig, 38 entradas do manifesto de migração.
- `verify` completo (Windows x64, local, serial): 53/53 verificações gerenciadas, 41/41 verificações de assembly .NET Standard,
  6/6 grupos de integração MCP, 16/16 testes Zig nativos. `power_host` e `power_model_host` executam.
- 6/6 testes de ABI do C# portados passam, incluindo a limpeza de 70 espaços de compilação reprovados, a rejeição de 15 mutações de documento
  e o código de saída 2 de KPI reprovado.
- Comparação de baseline: **176/176** valores de baseline C originais correspondem exatamente (erro absoluto máximo 0.0).

## 2026-09-14: checkpoint Core da rede de gás compilada

Recuperado o trabalho WSL até `4a81716` e sua integração Core inacabada. O checkpoint
agora compila redes só de gás e mistas de gás/térmica, valida faixas de composição e
abertura, e inclui estado de massa/energia interna e balanços de reservatório em
snapshots, hashes, ramificações e rollback de lote inteiro. Um limitador de estágio
conservativo impede que um par isolado em equalização oscile através do equilíbrio.
O cilindro fechado inalterado e os modelos lineares retêm as impressões digitais e o
comportamento de replay anteriores.

Verificação serial usando o .NET SDK 10.0.400 fixado em cache em Linux x64:

- **53/53** verificações de Core/aplicação no .NET 10.
- **41/41** verificações contra os assemblies .NET Standard 2.1 de Core/Assets no .NET 10.
- **6/6** grupos de integração MCP contra um servidor filho real.
- Os relatórios existentes de replay dos experimentos eletrotérmico, térmico e de cilindro passam.
- Grupos Zig nativos e seis testes de ABI do Python passam; todos os **176** valores
  de baseline originais correspondem exatamente. Auditoria de fontes: zero arquivos C/C++ e zero arquivos Lua.

A evidência específica de gás inclui física de bico bloqueado/subcrítico, descarga
analítica de vaso e refinamento, entalpia de enchimento de reservatório, escoamento
reverso, conservação de massa e energia em rede fechada, troca analítica de parede
em tempo finito, isolamento de válvula fechada, rejeição de faixa de entrada/programação,
estouro observável, falha/recuperação no meio do lote, equivalência de ramo e de lote,
compilação imutável e alocações zero de avanço/snapshot. A rejeição de assets v1/v2
é testada para impedir o descarte de campos de gás não suportados.

Veja [GAS_NETWORK.md](GAS_NETWORK.pt-BR.md) para o método numérico e o escopo restante.
Este checkpoint tem evidência local no Linux; o status atual de CI em Windows/macOS
deve ser lido no workflow do seu commit. Unity Editor/Play/IL2CPP e comportamento de
veículo calibrado permanecem não verificados. O registro autônomo anterior, abaixo,
descreve o commit precedente.

## 2026-09-14: física de troca de gás (autônoma)

Acrescentada a primeira fatia do incremento de troca de gás como **só física**:
`IdealGas`, `GasVolumeState` e `Orifice` em `src/Power.Core/GasExchange.cs`. Volumes
finitos agora carregam massa e energia interna como estados independentes, e o
orifício implementa as relações padrão de bico isentrópico nos dois sentidos, com
um coeficiente de descarga e uma fração adimensional de abertura. `Numeric.Expm1` e
`Numeric.Log1p` saíram de `CylinderPhysics` e são compartilhados; as implementações
não mudam, e cada hash de estado de cilindro, impressão digital de modelo e limite
de replay existentes ainda coincidem.

**Nenhum tipo de nó, tipo de componente, canal, campo de esquema ou versão de asset mudou.**
Um documento de modelo ainda não pode conter um volume de gás finito, e as superfícies
de CLI, MCP e Unity não foram tocadas. O método de separação proposto com escoamento
de Euler retrógrado permanece não validado e não adotado. Veja [troca de gás](GAS_EXCHANGE.pt-BR.md)
para as equações, os limites numéricos e a lista completa de contratos que não entraram.

Seis verificações analíticas novas em `tests/Power.Tests/GasChecks.cs`, cada uma
escrita contra uma forma fechada independente e não contra uma saída registrada:
continuidade de bloqueio e monotonicidade da função de escoamento para gamma em
{1.1, 1.3, 1.4, 5/3}; 54 casos de bico contra as relações de vazão mássica
compressível da NASA; contratos do orifício incluindo antissimetria exata de
escoamento reverso, isolamento de válvula fechada e estados rejeitados; descarga
adiabática de vaso contra a solução isentrópica analítica a 1e-9 relativo; a
identidade de enchimento de reservatório dU = cp*T_supply*dm com o limite de vaso
evacuado T -> gamma*T_supply; e conservação fechada de dois volumes a 1e-14 relativo
em massa e 1e-12 em energia, com equalização de pressão.

O `dotnet run --file tools/Build.cs -p:UseSharedCompilation=false -- verify` serial
completo passou em Linux x64 com o .NET SDK 10.0.400 fixado em cache e Zig 0.15.2:
**44/44 verificações gerenciadas** (38 antes desta mudança), **26/26 verificações
contra os assemblies .NET Standard 2.1 voltados ao Unity**, **6/6 grupos de processo
MCP**, três relatórios de experimento com 11, 11 e 21 limites de replay, **16/16
grupos Zig nativos** e os testes de ABI externa do Python. Os dois hosts Zig
executaram contra a biblioteca compartilhada real, a auditoria de fontes reportou
`c_source_files: 0` e `lua_files: 0`, e todos os **176 valores de baseline
corresponderam exatamente ao binário C original** (erro absoluto máximo 0.0). O log
é `artifacts/reports/gas-exchange-verify.log`.

Windows e macOS não foram exercitados para esta mudança, e a validação de Unity
Editor, Play Mode, renderização e IL2CPP permanece pendente como antes. Os parâmetros
de amostra permanecem `unverified`.

## 2026-09-11: limpeza do empacotamento Lua

Removidos o lançador LuaInstaller restante e seu README de empacotamento obsoleto.
O lançador dependia da ponte `power_native` nunca implementada e não tinha chamadores
ativos de compilação ou runtime. Caminhos originais e hashes SHA-256 são preservados
no [manifesto de migração](../legacy/native/migration-manifest.json) e coincidem com
os arquivos na revisão de fonte registrada. Documentos históricos de projeto retêm
sua proveniência; suas propostas Lua estão retiradas.

A auditoria de fontes agora rejeita código-fonte, bytecode e pacotes Lua além de
código-fonte e cabeçalhos C/C++, e reporta `lua_files: 0`. Sondas temporárias não
rastreadas `.lua`, `.luau`, `.luac`, `.rockspec`, `.rock` e `.LUA` em maiúsculas
produziram cada uma um status de saída reprovado e um erro estruturado identificando
o arquivo; a árvore limpa passou depois.

O `dotnet run --file tools/Build.cs -- verify` serial completo passou em Linux x64:
**38/38 gerenciadas, 26/26 de assembly .NET Standard, 6/6 MCP, 16/16 Zig e 6/6
verificações de ABI do Python**. Os dois hosts nativos executaram, e todos os 176
valores de baseline corresponderam exatamente. O log é
`artifacts/reports/lua-removal-verify.log`. Comportamento do núcleo, evidência de
amostra e arquivos de licença não mudam. O Unity Editor não foi exercitado.

## 2026-09-11: migração nativa para Zig

O proprietário retomou a migração de linguagem nativa em 2026-09-10. Todos os **26
arquivos de implementação/teste/host C e 12 cabeçalhos** foram substituídos por Zig.
O [manifesto de migração](../legacy/native/migration-manifest.json) registra a revisão
Git original, os caminhos de arquivo e os hashes SHA-256. Nenhum código-fonte ou
cabeçalho C/C++ permanece no inventário de fontes do repositório; o comando de
verificação da raiz impõe essa restrição. Arquivos de licença/exceção e evidência
de amostra são preservados.

O `dotnet run --file tools/Build.cs -- verify` serial completo passou em Linux x64
usando o .NET SDK 10.0.400 fixado em cache e Zig 0.15.2: **38/38 verificações
gerenciadas, 26/26 verificações contra os assemblies .NET Standard 2.1 voltados ao
Unity, 6/6 grupos de processo MCP, 16/16 grupos Zig nativos e 6/6 testes de ABI
externa do Python**. A suíte nativa também passou em todos os 16 grupos em Debug
com verificações de segurança ativadas. Os dois hosts Zig executaram contra a
biblioteca compartilhada real. A biblioteca exporta só `pwr_get_api` e não tem
símbolos ELF não resolvidos. Todos os **176 valores em 11 instantes de amostra
eletrotérmica corresponderam exatamente** ao binário C original neste host, com a
mesma impressão digital de modelo e os mesmos contratos de canal. A comparação de
fixture entre cadeias de ferramentas ainda usa tolerâncias explícitas, e o replay
do mesmo binário deve coincidir exatamente. Os relatórios estão em
`artifacts/reports/zig-migration-verify.log`, `native-verification.json` e
`native-electrothermal.json`.

A compilação cruzada ReleaseSafe de biblioteca/host também passou para **x86_64 Windows**
e **aarch64 macOS**. Compilação cruzada não é evidência de execução nesses sistemas.
Os logs locais são `artifacts/reports/zig-cross-windows.log` e `zig-cross-macos.log`.

O GitHub Actions concluiu em seguida a verificação real com sucesso em **Linux x64,
Windows x64 e macOS arm64**, cada um passando em todas as **38/38 gerenciadas, 26/26
de assembly .NET Standard, 6/6 MCP, 16/16 Zig e 6/6 verificações de ABI do Python**.
Os dois hosts de biblioteca compartilhada executaram em cada plataforma. Todos os
176 valores de baseline nativos corresponderam exatamente nos três runners, e seus
inventários de fontes continham zero fontes ou cabeçalhos C/C++. Evidência:
[execução 34549950147](https://github.com/Water-Run/Power/actions/runs/34549950147),
commit de código [`dd3de10`](https://github.com/Water-Run/Power/commit/dd3de10294949c8b2ffb49afc02a9c70b5808c00).
O checkout do Windows fixa os arquivos Zig em LF; a verificação no macOS usa os stubs
Darwin incluídos no Zig para evitar a incompatibilidade do Apple SDK mais novo
descrita nas [notas de compilação nativa](NATIVE_ZIG.pt-BR.md#build-and-maintenance).
Os logs completos dos jobs são retidos localmente em
`artifacts/reports/zig-ci-34549950147-{linux,windows,macos}.log`, com metadados da
execução em `zig-ci-34549950147.json`. As correções posteriores de documentação e
comentários não mudam código executável.

A suíte nativa cobre ainda o módulo de powertrain automático antes não compilado,
incluindo replay, contabilidade de energia, brownout e rollback de transação, mais
o tratamento concorrente de tempo de vida do SDK. Três pontos de entrada históricos
de teste devolviam sucesso em silêncio quando as verificações falhavam; a porta
corrige a propagação e separa os cenários de combustão estacionária, limitador e
contrapressão do motor. Veja [as notas de migração](NATIVE_ZIG.pt-BR.md) para as
equações preservadas e o arranjo experimental corrigido. O resultado CTest original
sozinho era insuficiente por causa dessas falhas ocultas.

Esta migração não completa os objetivos gerenciados de motor/transmissão nem
estabelece calibração de veículo. Unity Editor, Play Mode, renderização, Mono e
IL2CPP não foram exercitados. Toda a calibração de amostra permanece `unverified`.


## 2026-09-08: incremento do cilindro fechado

O serial `tools/Build.cs verify` passou em Linux x64 com SDK 10.0.400 e runtime 10.0.11: **38/38 verificações gerenciadas, 26/26 verificações contra os assemblies .NET Standard 2.1 reais e 6/6 grupos de integração de processo MCP**. A compilação Release informou zero avisos e erros. O log é `artifacts/reports/cylinder-verify.log`; os três documentos JSON de laboratório também passaram no JSON Schema publicado usando o validador local `jsonschema`.

As verificações novas cobrem geometria e derivadas analíticas de biela-manivela, identidades de estado de gás ideal, execuções de conservação de dois segundos com e sem contrapressão, convergência de segunda ordem sob refinamento de passo, rotação reversa, passos minúsculos e pontos mortos, virabrequins compartilhados/acoplados com componentes elétricos e térmicos, rollback de falha não linear, cancelamento, ramificações, unidades, extensões de cilindro malformadas e alocações gerenciadas zero em avanço/snapshots de regime estacionário. Um fixture v1 preservado decodifica, retém a impressão digital linear original e reproduz de forma idêntica após a exportação v2.

O GitHub Actions repetiu a mesma verificação com sucesso em **Windows, macOS e Linux**, com verificações 38/38, 26/26 e 6/6 e zero avisos/erros em cada plataforma. Evidência: [execução 34176008291](https://github.com/Water-Run/Power/actions/runs/34176008291), commit de código [`6209df2`](https://github.com/Water-Run/Power/commit/6209df22876f9928ddc04a1a32845cd7efe087da). Logs completos dos jobs e metadados de status são retidos localmente como `artifacts/reports/github-actions-34176008291.log` e `.json`. A atualização posterior de documentação não muda código executável.

O experimento sintético de cilindro passou nos KPIs finais e reproduziu exatamente em 21 limites através de JSON, reprodução de asset e um servidor filho MCP real. Resultados no Linux: impressão digital `c64b61efdb827680`, velocidade final `153.00340249454544 rad/s`, pressão `118835.36885412445 Pa`, temperatura `315.16234058802814 K`, resíduo final de energia `8.7464e-10 J` e resíduo absoluto máximo amostrado `8.9570e-10 J`. Relatório completo: `artifacts/reports/sealed-cylinder.json`. Estes valores estabelecem evidência numérica para este benchmark de gás ideal fechado, não calibração de motor.

O importador Unity e os testes Play agora incluem o asset de cilindro e o movimento esquemático do pistão, mas **não foram executados**. A evidência de Unity Editor, Mono, renderização e IL2CPP permanece pendente. A pilha ativa permanece C#/Unity; a direção futura de reescrita em Zig não introduz runtime nativo neste incremento.

## Checkpoint de pausa: 2026-09-08

O proprietário pediu o encerramento e uma pausa de desenvolvimento após o incremento do cilindro. A fonte executável permanece no commit de código verificado `6209df2`; os commits posteriores atualizam só a documentação. A extensão prospectiva de troca de gás não foi aplicada, compilada nem publicada. Suas [notas de retomada](NEXT_ENGINE_STEP.pt-BR.md) distinguem trabalho proposto de capacidades implementadas. Nenhuma compilação adicional foi necessária para este checkpoint só de documentação. Retome o desenvolvimento só após uma instrução explícita do proprietário.

## Baseline histórica: 2026-09-07


Ambiente: 2026-09-07, Linux x64, .NET SDK 10.0.400, runtime .NET 10.0.11. O resultado executado é a saída de `tools/Build.cs verify` e os relatórios gerados.

Baseline gerenciada nessa data: 30/30 verificações de núcleo, asset e agente, 19/19 verificações de assembly da biblioteca padrão, e 5/5 grupos de integração de processo MCP passaram. A compilação Release informou 0 avisos e 0 erros. O processo MCP ao vivo descobriu 12 ferramentas e verificou esquemas de entrada e saída. Respostas de sucesso e de erro foram verificadas quanto a campos de saída exigidos e a um resultado de texto compatível. O log bruto é `artifacts/reports/managed-verification.log`.

## Evidências registradas então

- Core e Assets foram compilados para `net10.0` e `netstandard2.1`.
- Verificações analíticas cobriram torque constante, a resposta RL e o equilíbrio térmico. Reduzir o passo pela metade verificou convergência mecânica de segunda ordem e convergência térmica de primeira ordem.
- Razões positivas e negativas foram verificadas para momento generalizado, calor de amortecimento e conservação. A frenagem regenerativa foi verificada quanto a corrente negativa e a uma diminuição do trabalho da fonte.
- Rejeição de entrada, estouro num tick posterior, pré-cancelamento e verificações de capacidade de buffer confirmaram todos que o estado e os dados do chamador não são modificados parcialmente.
- Posse da descrição do modelo, instâncias independentes em paralelo, ramificações de estado completo e avanço por tick contra avanço em lote foram verificados quanto a concordância.
- Um contador de alocação de thread .NET mediu 0 alocações gerenciadas para o caminho quente do núcleo de entrada, passo e snapshot combinados. Essa contagem exclui compilação, relatórios e a UI do Unity.
- Codificação e decodificação de asset conservaram fonte, modelo e eventos. Um resumo danificado, contagens forjadas, uma versão de formato ruim, uma impressão digital de modelo ruim e bytes extras foram todos rejeitados.
- Entradas programadas cobriram o tempo zero, o fim de um lote e eventos dentro de um lote de apresentação. Uma falha numérica posterior fez rollback do lote inteiro. A reprodução de asset mediu 0 alocações gerenciadas quando cada tick carregava uma mudança de entrada. O cancelamento conservou o cursor de eventos.
- Relatórios JSON e assets importados compararam hashes de estado e valores de saída em cada limite de relatório, incluindo eventos que não caem num limite de apresentação de 20 ms.
- As mesmas verificações de física carregaram as DLLs .NET Standard 2.1 reais copiadas para o Unity e verificaram o framework alvo. O host ainda era .NET 10, então isto não mostra que Mono ou IL2CPP passaram.
- Verificações de agente cobriram diagnósticos estruturados de campo, snapshots filtrados, o limite de sessão, conflitos concorrentes de revisão, cancelamento, isolamento de ramificação pai e filho, ciclo de vida e relatórios compactos.
- O cliente MCP oficial iniciou um processo filho real de servidor e concluiu a descoberta de 12 ferramentas, esquemas de entrada e saída, recuperação de erro, operações de sessão, um experimento completo e exportação de asset. O resumo do arquivo foi verificado após a decodificação Base64, e a reprodução importada foi comparada com o estado final do experimento MCP.

O experimento eletrotérmico padrão executa por 10 segundos, cai para 4 V em 5 segundos e volta a 24 V em 6 segundos. Dois tamanhos de lote concordam bit a bit em 11 limites. Valores finais típicos são cerca de motor `29.74182442 rad/s`, carga `9.91394147 rad/s` e temperatura do motor `302.4760663 K`. O limiar de resíduo de energia é `1e-5 J`. Hashes de replay são comparados só para o mesmo binário, runtime e arquitetura. Números entre runtimes usam uma tolerância.

O experimento de troca de calor usa os nós 42/77, sem entrada externa e com passo de 7 ms, e executa por 7 segundos. Dois tamanhos de lote concordam em 11 limites. A temperatura final difere da solução discreta de Euler retrógrado em menos de `1e-9 K`, da solução analítica contínua em menos de `0.004 K`, e o erro total de energia é menor que `1e-7 J`. Os dois relatórios são `artifacts/reports/electrothermal.json` e `thermal-network.json`.

## Reprodução

```sh
dotnet run --file tools/Build.cs -- verify
```

Estas verificações são programas de aceitação de console que executam asserções em Release. Elas não dependem de testes `Debug.Assert` vazios. Elas não precisam de Unity, Python nem da biblioteca C original. O host de verificação nativa e o instalador Zig foram movidos para a mesma ferramenta de compilação .NET em 2026-09-19 (P/Invoke do C#). O projeto MCP usa o pacote NuGet oficial, e `packages.lock.json` fixa a resolução.

O GitHub Actions concluiu a mesma aceitação gerenciada em Windows, macOS e Linux: 30/30, 19/19 e 5/5 em cada plataforma. A evidência é o commit de código [`aea6136`](https://github.com/Water-Run/Power/commit/aea6136bbdcbeaea91d63836d947637e7eac730e) e a [execução 34087686661](https://github.com/Water-Run/Power/actions/runs/34087686661). `artifacts/reports/github-actions-34087686661.log` e `.json` estão armazenados localmente e contêm a saída dos jobs e o status final. Uma aceitação local adicional foi executada a partir de uma cópia limpa da fonte, sem cache e sem assemblies gerados. Seu log é `github-clean-checkout.log`.

Os links de evidência do repositório e da CI são públicos. O desenvolvimento foi retomado em 2026-09-08; os registros anteriores abaixo identificam as próprias baselines verificadas.

A atualização de publicação GPL acrescentou avisos de licença sem mudar o conteúdo executável da fonte; uma comparação com o commit precedente confirmou que todas as 90 edições de fonte/compilação eram só avisos. Uma verificação serial nova passou em 30/30 verificações gerenciadas, 19/19 verificações de assembly voltadas ao Unity e 5/5 grupos de integração MCP, com zero avisos ou erros de compilação. Seu log é `artifacts/reports/license-verification.log`. Isto não acrescenta evidência de validação de Unity Editor ou Player.

## Evidências ainda não obtidas

Este ambiente não tem Unity Editor instalado. Importação do Editor, testes de Edit Mode e Play Mode, verificações de renderização de cena e uma compilação IL2CPP não foram executados. O projeto, as cenas, os testes e a entrada de automação estão no lugar:

```sh
dotnet run --file tools/Build.cs -- unity-test
```

Defina `POWER_UNITY_EDITOR` primeiro. Logs do Unity e resultados XML vão para `artifacts/unity`. Os testes Play precisam de uma máquina que possa executar o editor gráfico e de uma licença Unity válida. Testes escritos e ainda não executados cobrem importação URP e de assembly para os dois assets de modelo, concordância de reprodução, início e parada repetidos sem sobras, o experimento de referência de 10 segundos, a troca para uma topologia só térmica durante a execução, listas dinâmicas de nós e entradas, e o agendamento de ticks de 7 ms. Controles, tema, tamanhos de janela e apresentação de desktop ainda precisam de uma pessoa para olhá-los, e as compilações de Player para as três plataformas de desktop ainda estão em aberto.

A entrada de publicação é `Power.Studio.Editor.ProjectSetup.BuildPlayer`, usando o alvo de desktop selecionado e IL2CPP. Ela precisa do módulo de compilação da plataforma Unity correspondente. Ainda não há um pacote de Player compilado ou testado.

Cada parâmetro atual é um parâmetro de experimento sintético. Um motor e uma transmissão completos, calibração de veículo, emissões, acústica, um orçamento de tempo real e execuções longas ainda precisam da própria implementação e evidência. Estas verificações não estabelecem esse trabalho.
