# Estado de desenvolvimento

[English](DEVELOPMENT_STATUS.md) · [简体中文](DEVELOPMENT_STATUS.zh-CN.md) · [Français](DEVELOPMENT_STATUS.fr.md) · [Русский](DEVELOPMENT_STATUS.ru.md) · [日本語](DEVELOPMENT_STATUS.ja.md) · [한국어](DEVELOPMENT_STATUS.ko.md) · [Deutsch](DEVELOPMENT_STATUS.de.md) · [Español](DEVELOPMENT_STATUS.es.md) · [Italiano](DEVELOPMENT_STATUS.it.md) · **Português**

O Power! tem um núcleo de simulação gerenciado, documentos de modelo compartilhados e assets portáteis, uma CLI sem interface, um serviço de agente MCP e um estúdio Unity preparado. Laboratórios sintéticos exercitam comportamentos de motor, transmissão, hidráulica e elétrica. Powertrains completos, controle coordenado de ECU/TCU, calibração medida e um aplicativo Unity de desktop aceito continuam inacabados.

## Implementação atual

| Área | Implementado | Aceitação restante |
|---|---|---|
| Núcleo | Unidades explícitas e IDs estáveis; compilação imutável; tempo inteiro limitado; balanços observáveis; replay, cancelamento, ramificações independentes e rollback do lote inteiro | Evidência de longa duração e de powertrain completo |
| Motor | Cilindros fechados/abertos, trabalho de pressão da biela-manivela, escoamento de gás bidirecional, válvulas cronometradas pelo virabrequim, calor de parede e combustão premisturada prescrita | Admissão/escape detalhados, ignição, perdas mecânicas, termoquímica mais rica e comportamento medido do motor |
| Combustível | Dosagem por ciclo, trilhos flexíveis com bomba, tanques finitos, retornos conservativos de alívio, evaporação de filme, agulhas físicas e previsão limitada de fechamento; [Geometria do tanque e espaço gasoso finito](TANK_HEADSPACE.pt-BR.md); [Equilíbrio de fases do combustível e limites de suprimento](FUEL_PHASE_EQUILIBRIUM.pt-BR.md); [Equilíbrio do tanque com volume líquido finito](TANK_PHASE_EQUILIBRIUM.pt-BR.md); [Bomba de combustível com mudança de fase acionada por eixo](FUEL_CHAMBER_PUMP.pt-BR.md); [Descarga de combustível com título congelado e receptores finitos](FUEL_DISCHARGE.pt-BR.md); [Dutos de combustível compressível em equilíbrio](EQUILIBRIUM_FUEL_DUCT.pt-BR.md); [Câmaras de combustível compressível acionadas por eixo](COMPRESSIBLE_FUEL_CHAMBER.pt-BR.md) | Adicionar fronteiras compatíveis de válvula direcional e agulha/bocal compressíveis, com entalpia, energia cinética e hipóteses de fase explícitas. Depois avançar relaxação/deslizamento, ignição, admissão/escape, perdas mecânicas e coordenação ECU/TCU. Calibração medida e aceitação real no Unity permanecem separadas. |
| Transmissão | Embreagens estática/deslizante, acionamento por contato, engrenagens/planetárias com sinal, conversor/trava mapeados e caminhos DCT de sete marchas à frente/ré e Ravigneaux de quatro faixas à frente/ré com giro/inércia orbital dos planetas resolvidos | Flexibilidade/perdas de engrenamento e divisão de carga, acionamento DCT, controle completo de pressão/troca da AT e roteamento medido, trocas coordenadas, perdas medidas e comportamento mais rico do conversor |
| Hidráulica | Volumes flexíveis, restrições, bombas com vazamento/arrasto explícitos, alívio, pistões dinâmicos, carretéis dosados e acumuladores de gás de energia finita | Mapas medidos de válvula/acumulador/bomba, atrito de vedação, cavitação e hidráulica completa da transmissão |
| Elétrica | Motores RL, solenoides recíprocos de indutância variável, bateria de carga finita, polarização por resistência/RC, conversão de duty médio e acessórios | Comportamento químico/térmico medido, BMS, controle de corrente e integração completa da alimentação |
| Controles | PI de pressão amostrado, dose/fechamento da agulha, transição DCT escalonada e trocas AT por realimentação de pressão com confirmação física da trava | Coordenação de torque ECU/TCU, sensores, atuadores e tratamento de falhas |
| Documentos e assets | 60 laboratórios JSON/CLI, 59 exemplos MCP, asset v37 e leitores v1-v36 | Edição/salvamento e coleções de modelos calibrados |
| Agentes | Doze ferramentas MCP definidas por esquema; evidência compacta, verificações de revisão e diagnósticos acionáveis | Fluxos de trabalho completos para o escopo físico/de controle restante |
| Unity | Importação de modelo, reprodução em ticks exatos, componentes 3D esquemáticos, controles, reinício e testes de ciclo de vida preparados | Aceitação real de Editor/Play, gráficos selecionáveis, edição/gravação de grafo e Player/IL2CPP |
| Acervo nativo | Protótipos de pesquisa Zig 0.15.2, ABI preservado e procedência original das fontes | Referência histórica; a migração gerenciada continua separada da funcionalidade completa |

Core e Assets visam tanto `net10.0` quanto `netstandard2.1`; o Core não tem dependência de Unity, transporte, provedor de modelos nem de terceiros. Os scripts de Assets do Unity usam C# 9. O Unity carrega os assemblies Standard compilados pelo SDK externo; ele não compila código-fonte .NET 10/C# 14.

`LiquidColumn` é uma referência C# independente para tubo horizontal rígido com seção, velocidade uniforme, densidade líquida e temperatura constantes. As pressões absolutas são prescritas por intervalo. Resistências linear e quadrática são entradas de pesquisa explícitas. Não é componente do grafo e não altera rede ou bocal. [Referência de quantidade de movimento na entrada líquida](LIQUID_COLUMN.pt-BR.md)

Um `fuel_inertial_pipe` conecta dois nós finitos de combustível e uma parede térmica finita. A linha horizontal rígida possui massa líquida, energia calórica e cinética próprias além dos vasos. Densidade constante e temperatura variável perfeitamente misturada. Portas e linha ficam líquidas; conexões quase estáticas mantêm seu modelo bifásico separado. [Quantidade de movimento da linha finita de combustível líquido](FUEL_INERTIAL_PIPE.pt-BR.md)

Um `equilibrium_fuel_duct` transporta massa, quantidade de movimento e energia total por células explícitas `compressible_fuel`. O líquido é compressível e o vapor ideal; as fases compartilham pressão, temperatura e velocidade. O equilíbrio local permite condensação e vaporização súbita. Dois reservatórios finitos estacionários e uma parede térmica finita fecham os balanços. [Dutos de combustível compressível em equilíbrio](EQUILIBRIUM_FUEL_DUCT.pt-BR.md)

Uma `compressible_fuel_chamber` conecta um eixo a um reservatório finito `compressible_fuel`. O movimento muda o volume; o mesmo trabalho de pressão muda energia do fluido e torque. O reservatório pode alimentar ou receber um `equilibrium_fuel_duct`, com vaporização súbita e condensação em equilíbrio. [Câmaras de combustível compressível acionadas por eixo](COMPRESSIBLE_FUEL_CHAMBER.pt-BR.md)

## Evidência e limites

O comando serial exigido `dotnet run --file tools/Build.cs -- verify` cobre ambos os alvos de assembly, um processo MCP real, todos os laboratórios, o arquivo Zig e a ABI C#. Contagens, resultados e caminhos dos logs estão em [VALIDATION.md](VALIDATION.md). Verificações Standard no .NET 10 não estabelecem aceitação do runtime Unity.

O suprimento inclui [tanques finitos](LIQUID_FUEL_TANK.pt-BR.md) e [retornos de alívio rastreados](LIQUID_FUEL_RETURN.pt-BR.md), com balanços de massa, energia calórica/química e trabalho de pressão. O [controlador AT hidráulico](AT_CONTROL.pt-BR.md) regula pressão dos atuadores e confirma marcha/trava físicas. Referências independentes e transações completas sustentam esses modelos de pesquisa; coordenação ECU/TCU completa e comportamento medido do hardware seguem pendentes.

`POWER_UNITY_EDITOR` não está definido no ambiente atual. Importação Studio, reprodução e testes preparados ainda precisam de evidência real de Editor/Play, renderização e Player/IL2CPP.

Todos os parâmetros permanecem `unverified`. EA211 DJS + DQ200 e PSA EC5 + AT8 mantêm limites completos e manifestos de evidência em [assets/samples](../assets/samples). Medições OEM ausentes continuam ausentes. Licenças e procedência histórica do código são preservadas.

CLI `list-labs`, descoberta de exemplos MCP e verificação serial compartilham [um catálogo de laboratórios](../assets/labs/catalog.json). A verificação exige a cobertura de cada fonte de laboratório.

A bomba do domínio de combustível agora alimenta agulha física e descarga de título congelado em um receptor em combustão com volume líquido finito. Os inventários completos incluem mudança de fase, trabalho de deslocamento, energia cinética e impulso na parede imóvel. [Bomba de combustível com mudança de fase acionada por eixo](FUEL_CHAMBER_PUMP.pt-BR.md)

## Próxima sequência de desenvolvimento

1. Adicionar fronteiras compatíveis de válvula direcional e agulha/bocal compressíveis, com entalpia, energia cinética e hipóteses de fase explícitas. Depois avançar relaxação/deslizamento, ignição, admissão/escape, perdas mecânicas e coordenação ECU/TCU. Calibração medida e aceitação real no Unity permanecem separadas.
2. Estenda o motor com controle de ignição, dinâmica de admissão/escape, perdas mecânicas e termoquímica mais rica. Conserve o objetivo completo do motor.
3. Estender acionamento DCT e hidráulica planetária/AT medida, depois coordenar solicitações de torque e trocas ECU/TCU com os controladores DCT e AT amostrados existentes. Acrescentar sensores/atuadores medidos e falhas recuperáveis.
4. Execute `unity-test` com o Editor fixado e depois obtenha evidência de Player/IL2CPP. Complete a seleção de canais, a edição de grafo e a gravação como recursos distintos.
5. Obtenha mapas medidos, dados OEM e orçamentos de incerteza para os dois powertrains alvo antes de declarar amostras calibradas ou prontidão de lançamento.

[Descarga de combustível com título congelado e receptores finitos](FUEL_DISCHARGE.pt-BR.md)
