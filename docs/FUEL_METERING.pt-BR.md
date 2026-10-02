# Trilho de combustível gasoso finito e dosagem por ciclo

[English](FUEL_METERING.md) · [简体中文](FUEL_METERING.zh-CN.md) · [Français](FUEL_METERING.fr.md) · [Русский](FUEL_METERING.ru.md) · [日本語](FUEL_METERING.ja.md) · [한국어](FUEL_METERING.ko.md) · [Deutsch](FUEL_METERING.de.md) · [Español](FUEL_METERING.es.md) · [Italiano](FUEL_METERING.it.md) · **Português**

`gas_fuel_injector` transfere combustível de um trilho de gás finito e rastreado para uma câmara
de gás compatível. Uma janela de virabrequim para a frente trava uma massa de combustível solicitada por ciclo;
pressão, temperatura, área do bocal e o inventário disponível do trilho determinam a entrega
real. O controlador restringe a porta perto da cota. Não acrescenta combustível
diretamente ao estado receptor nem supõe que uma dose solicitada tenha sido entregue.

Isto estende o modelo atual de mistura gasosa de propriedades constantes. Admissão de ar,
dosagem de combustível, mistura e reação prescrita agora podem ser modeladas. Não
implementa pulverização de gasolina líquida, evaporação, dinâmica de agulha ou elétrica, física de
trilho ou tanque líquidos, propriedades detalhadas de espécies nem comportamento calibrado de injeção ou de ECU OEM.
Continuam sendo trabalho necessário rumo ao objetivo do motor completo.

## Escoamento, constituintes e energia

A fonte e o receptor são nós de gás premisturado finitos e distintos. Os dois compartilham R, gamma,
poder calorífico e razão estequiométrica. A fonte pode ser combustível gasoso puro ou uma
mistura rastreada que contém combustível. O escoamento segue a lei existente de [orifício de gás](GAS_NETWORK.pt-BR.md),
dependente da pressão, bloqueado e subcrítico, com área e coeficiente de descarga
explícitos. O escoamento por pressão inversa fica fechado: o gás do receptor não reenche o trilho.

A cota diz respeito à **massa de combustível**, não à massa total da mistura da fonte. Cada avanço de gás
limita a taxa de combustível por `remaining_cycle_fuel / advance_duration`. O mesmo fator de escoamento
escala a massa total e a entalpia térmica de montante; as frações de constituinte usam o estado
real de montante. Os estágios de Heun e o histórico de entrega aceito usam as mesmas transferências.
Assim, o combustível recebido, o esvaziamento do trilho, a energia térmica e o inventário
químico permanecem consistentes mesmo quando a pressão do trilho cai ou a composição da fonte muda.

A transferência interna de combustível não entra em `fuel_energy_in` externo nem na entalpia
de reservatório. A energia química armazenada da fonte acompanha o combustível e só vira calor
do gás quando o componente de queima separado o consome. Química incompatível ou
parâmetros calóricos do gás são rejeitados na compilação. O limite de dose muda o escoamento
admitido, em vez de corrigir massa ou energia depois da integração.

## Contrato de ciclo e de comando

| Dado | Significado |
|---|---|
| Fonte A / receptor B | Volumes de gás finitos, rastreados e distintos |
| `area`, `discharge_coefficient` | m2/mm2 positivos e coeficiente em (0,1] |
| `crank_node` | Referência rotacional explícita de temporização; uma câmara de virabrequim móvel usa o próprio virabrequim |
| `cycle_angle` | 360 ou 720 graus, com unidades de ângulo explícitas |
| `start_angle`, `duration_angle` | Início da janela e duração positiva não maior que um ciclo |
| `maximum_dose` | Limite positivo de combustível por ciclo, em kg |
| Entrada `fuel_dose_per_cycle` | kg solicitados em [0,maximum_dose] |

A janela retangular ideal só abre durante o movimento para a frente. O primeiro avanço aceito
de janela aberta amostra a dose solicitada. Escritas durante esse ciclo observado
valem para a janela seguinte; o canal do ciclo solicitado continua mostrando o alvo
travado. Zero desativa esse ciclo. Se a pressão ou o combustível disponível não bastar, a entrega
real permanece abaixo do alvo. Um passo bem-sucedido não implica uma dose completa.

Os ordinais de ciclo são inteiros com sinal e limitados, reconstruídos a partir de ângulos de virabrequim
representáveis. Voltar a um ciclo já observado não pode zerar a cota; inverter
o movimento fecha a janela. O percurso por intervalo mecânico limita-se a
`min(0.25 rad,duration/8)`. As extremidades da janela usam a aproximação existente de tick fixo e divisão
simétrica, então a temporização perto de descontinuidades exige refinamento do passo de tempo.
Não se afirma um instante exato e contínuo de comutação.

As saídas incluem a abertura atual da janela de dosagem, a taxa média de combustível entregue no último tick,
a dose solicitada travada, a dose entregue no ciclo observado e o combustível entregue
acumulado. Pressão, temperatura e combustível restante do trilho finito são canais de gás comuns.
Todos os históricos de cota, ordinal e entrega, inclusive a compensação, são copiados e hasheados com
o estado especulativo. Cancelamento, falha tardia e ramificações preservam o estado completo.

O asset v17 preserva os registros de bocal e de temporização. JSON, CLI e MCP compartilham o mesmo
modelo; o esquema e o compilador verificam unidades, limites, extremidades finitas compatíveis e
a posse da temporização. Os destinos do Core permanecem sem dependências, em net10.0/netstandard2.1.

## Experimento e evidência

`metered-fired-cylinder` substitui a admissão de combustível premisturado por uma admissão só de ar e um
trilho gasoso finito. Um injetor ideal fornece solicitações explícitas de 8/12/4 mg por
uma janela de virabrequim, antes da queima de Wiebe prescrita. As mudanças de dose são amostradas na
janela observada seguinte. O experimento de seis décimos de segundo entrega 28 mg, queima cerca de
27.930 mg e libera cerca de 1228.918 J; o combustível não queimado e o perdido na fronteira permanecem na
conta de constituintes. Todos os parâmetros são sintéticos e não verificados.

As verificações cobrem entrega exata limitada pela cota, esgotamento do trilho finito, pressão inversa,
trava de comando no meio da janela, reversão sem reemitir a cota, uma EDO independente de massa e entalpia
de dois vasos com refinamento suave, queima dosada analítica, transações completas,
capacidade de estado, unidades e química, e avanço sem alocação. Relatórios, asset portátil e MCP coincidem
em cada limite. Os canais separados de entrega e de queima distinguem um comando
aceito do combustível e do calor reais. Veja [VALIDATION.pt-BR.md](VALIDATION.pt-BR.md) para os limites
medidos e a evidência de runtime e de desempenho. As vistas do Studio e os testes de Edit/Play estão preparados
em fonte C# 9; a aceitação real do Editor do Unity e do Player continua pendente.
