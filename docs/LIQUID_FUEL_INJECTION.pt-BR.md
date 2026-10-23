# Trilho líquido finito, injeção por ciclo e reposição do filme

[English](LIQUID_FUEL_INJECTION.md) · [简体中文](LIQUID_FUEL_INJECTION.zh-CN.md) · [Français](LIQUID_FUEL_INJECTION.fr.md) · [Русский](LIQUID_FUEL_INJECTION.ru.md) · [日本語](LIQUID_FUEL_INJECTION.ja.md) · [한국어](LIQUID_FUEL_INJECTION.ko.md) · [Deutsch](LIQUID_FUEL_INJECTION.de.md) · [Español](LIQUID_FUEL_INJECTION.es.md) · [Italiano](LIQUID_FUEL_INJECTION.it.md) · **Português**

`liquid_fuel_injector` entrega líquido de um trilho flexível finito para um
[filme de combustível](FUEL_FILM.pt-BR.md) separado. Uma janela de virabrequim para a frente trava uma massa
solicitada por ciclo. A pressão real do receptor, a geometria do bocal, o inventário
restante do trilho e a energia de pressão determinam a entrega. O filme então aquece e
evapora o líquido; a reação prescrita existente consome só vapor.

Isto liga entrega, mudança de fase e reação e mantém cada inventário
e cada transferência de energia observáveis. É um modelo de pesquisa de densidade e flexibilidade
constantes. A alimentação opcional por bomba usa uma fronteira externa explícita de matéria/calor. Capacidade geométrica, ventilação/espaço gasoso/oscilação, cavitação, enchimento/eficiência/regulação medidos e spray resolvido seguem abertos. Parâmetros `unverified`; Unity Editor/Play/Player/IL2CPP real e calibração OEM seguem não verificados.

```mermaid
flowchart LR
    RAIL[Trilho líquido finito e flexível] --> INJ[liquid_fuel_injector]
    INJ --> FILM[fuel_film]
    WALL[Calor finito da parede] --> FILM
    FILM --> VAP[Vapor no volume de gás]
    VAP --> BURN[Reação prescrita]
```

## Equações do trilho e do bocal

O trilho tem densidade líquida constante `rho`, flexibilidade positiva `C` em m3/Pa,
massa inicial `m0` e pressão absoluta inicial `P0`. O volume de referência à pressão
zero deve ser não negativo:

Sem alimentação por bomba, o rail segue estas equações e mantém a temperatura fornecida.

```text
V_reference = m0 / rho - C P0
m_rail = m0 - total_delivered_mass
P_rail = P0 - total_delivered_mass / (rho C)
E_pressure = C P_rail^2 / 2
```

Isto declara a referência de flexibilidade de forma explícita na pressão absoluta zero;
não infere pressão de apoio ambiente, um mapa de módulo de compressibilidade nem uma bomba de trilho.
O volume flexível finito faz parte do conjunto de parâmetros de pesquisa fornecido.
A energia de pressão pertence ao livro de energia armazenada, separada do inventário
calórico e químico. O líquido da fonte permanece na temperatura fornecida;
a energia calórica sai com o líquido entregue e, neste incremento, não há aquecimento do trilho
nem mapa de propriedades dependente da temperatura.

Na abertura para a frente, o bocal quase estacionário de um só sentido usa:

```text
mass_rate = Cd A sqrt(2 rho (P_rail - P_receiver))
```

O escoamento é zero quando a pressão do trilho não é maior que a pressão do receptor.
Densidade e pressão têm unidades explícitas. Esta relação de pressão e velocidade baseia-se
na redução de energia incompressível descrita pela
[dedução de Bernoulli da NASA](https://www1.grc.nasa.gov/beginners-guide-to-aeronautics/bernoullis-equation/).
`Cd` é um coeficiente positivo fornecido, não maior que um; não estabelece
comportamento medido de bocal nem resolve quantidade de movimento, movimento da agulha ou cavitação.

Para pressão de receptor fixa dentro de um subpasso de injeção, a carga de pressão tem uma
solução analítica. Seja `r0 = sqrt(P_rail - P_receiver)`:

```text
r1 = max(0, r0 - Cd A h / (C sqrt(2 rho)))
available_mass = rho C (r0^2 - r1^2)
```

A massa aceita é limitada por essa quantidade disponível, pela cota restante do ciclo e
pelo inventário restante da fonte. A lei resolve o esgotamento da carga sem admitir
carga negativa nem inventar combustível. A aceitação da dose solicitada é separada da
entrega real; pressão insuficiente pode deixar uma cota sem preencher.

## Energia sensível, química e de pressão

O filme receptor determina a referência calórica líquida compatível:
`u_supply = c_liquid T_supply + e_offset`. A temperatura deve ser positiva e
não maior que a temperatura de saturação declarada do filme. A massa injetada acrescenta
`delta_m * u_supply` à energia térmica do filme e transfere o mesmo inventário químico
internamente. Não entra nos livros externos de combustível e de entalpia nem reage
antes da evaporação.

Para o volume de líquido entregue `delta_V = delta_m / rho`, o trabalho aceito é:

```text
W_rail = (P_before - delta_V / (2 C)) delta_V
W_receiver = P_receiver delta_V
Q_nozzle = W_rail - W_receiver
```

`W_rail` é igual à diminuição exata da energia de pressão armazenada do trilho. O calor
não negativo do bocal entra na parede térmica finita do filme. O trabalho de pressão do trilho é interno
e não é contado de novo como trabalho de fonte externo.

O contrato existente do filme despreza o volume de deslocamento líquido na geometria
do gás. Por isso, este injetor exporta `W_receiver` por uma fronteira explícita
de trabalho de pressão do receptor. O trabalho de fonte global recebe `-W_receiver`; o volume
de gás e o trabalho do virabrequim não são aumentados em silêncio. É uma redução declarada de interface,
não evidência de deslocamento de gota resolvido nem de quantidade de movimento da pulverização.
Um acoplamento futuro de gás com volume líquido finito precisa substituir esta fronteira por geometria
e trabalho de pressão reais, num contrato verificado à parte.

Energia calórica, de pressão e química permanecem distintas. A necessidade de conservar o trabalho de pressão
junto com a energia interna segue a relação `h = u + p/rho` explicada na
[documentação de meios incompressíveis do Modelica](https://doc.modelica.org/Modelica%204.0.0/Resources/helpWSM/Modelica/Modelica.Media.Incompressible.html).
O livro completo de trilho, filme, gás e térmico equilibra o trabalho exportado, sem tratar
calor de fase, dissipação do bocal ou energia de pressão como calor de reação do combustível.

## Contrato de definição e de temporização

| Dado | Requisito |
|---|---|
| `node_a` | Receptor de gás rastreado que pertence ao filme de destino |
| `film_component` | Componente `fuel_film` existente nesse receptor |
| `crank_node` | Referência rotacional de temporização; um cilindro de virabrequim usa o próprio virabrequim |
| `cycle_angle`, `start_angle`, `duration_angle` | Ângulos explícitos; ciclo de 360/720 graus e duração positiva limitada |
| `maximum_dose`, `initial_input` | Máximo positivo e kg solicitados não negativos por ciclo |
| `initial_mass` | Inventário inicial positivo do trilho, em kg |
| `supply_temperature` | K do líquido em `(0,film_saturation]` |
| `liquid_density` | kg/m3 positivo, unidade JSON `kg_m3` |
| `initial_pressure` | Pa/bar absolutos positivos |
| `pressure_compliance` | m3/Pa positivo, unidade JSON `m3_pa` |
| `area`, `discharge_coefficient` | m2/mm2 positivo e coeficiente em `(0,1]` |

Todas as quantidades são exigidas. O injetor tem uma entrada de dose em `kg`, não tem `node_b` e
não tem sumidouro de calor independente; o calor do bocal entra na parede do filme de destino. Parâmetros
não relacionados, unidades, domínios ou posse de filme errados, alimentação superaquecida, volume
de referência impossível e capacidade de estado não suportada são rejeitados com diagnósticos de objeto e de campo.
Os clientes do Core usam `LiquidFuelMeter`, `LiquidFuelInjectorDefinition`
e a lei independente `CompliantLiquidRail`.

O [perfil de dose](FUEL_METERING.pt-BR.md) compartilhado trava um comando uma vez em cada janela
para a frente observada. Mudanças no meio da janela valem para um ciclo posterior. A reversão fecha o escoamento
e não pode reemitir uma cota já observada. O percurso por intervalo mecânico é
limitado por `min(0.25 rad,duration/8)` e os ordinais de ciclo permanecem representáveis.
As extremidades da janela usam amostragem de tick fixo e precisam de refinamento de evento à parte.

## Integração e transações

O intervalo usa meios passos de injeção / filme / gás / mecânica e reação / gás / filme /
injeção. As varreduras do injetor e do filme invertem a ordem na segunda metade.
O calor do bocal muda a parede finita do filme durante esses subpassos; a evaporação paga
o orçamento de calor a partir dessa parede. A integração simultânea independente de EDOs verifica
refinamento suave de segunda ordem para o trilho, o filme, o gás e as transferências de pressão e de calor.
Outras fontes de parede de gás e térmicas conservam o limite existente de precisão de parede explícita.
Eventos e esgotamento não herdam uma afirmação uniforme de segunda ordem.

Cada injetor acrescenta nove entradas ao orçamento limitado de estado reportado: as seis
entradas existentes de cota e de entrega e três históricos acumulados de pressão e de calor. A massa
e a pressão da fonte derivam da entrega total compensada. Toda a compensação, os ordinais de ciclo,
os alvos mantidos e os fluxos médios são copiados, hasheados e sofrem rollback com a simulação,
inclusive intervalos especulativos de embreagem. A entrega ativa já aquecida e os instantâneos
não alocam memória gerenciada. Cancelamento, falha tardia, escritas rejeitadas e
ramificações independentes preservam os históricos físicos e de controlador completos.

## Canais e assets portáteis

Descubra IDs e unidades pela validação ou pela criação da sessão. As saídas do injetor são:

- `mass` restante da fonte, `pressure` absoluta, `temperature` fornecida e `volume` líquido.
- `internal_energy` para a energia calórica da fonte mais a de pressão; `chemical_energy` à parte.
- `opening` da janela, `mass_flow` médio do último tick, `requested_fuel_dose` travada,
  `delivered_fuel_dose` e `total_fuel_delivered` acumulado.
- `source_work` para o trabalho de pressão do trilho liberado, `hydraulic_work` para o trabalho
  de pressão do receptor exportado e `fluid_heat` para a dissipação do bocal.

Estes campos de componente têm significados distintos do trabalho de fonte externo global.
Os canais globais de massa, de combustível e de energia química incluem a fonte líquida restante,
o filme e os inventários normais de gás e de reação.

O asset v19 escreve um registro tipado de trilho e temporização de 120 bytes por injetor líquido, mais
o registro de bocal existente de 36 bytes. O codificador e os leitores v1-v18 conservados verificam
contagens e comprimento limitados, digest, cobertura tipada completa, unidades, posse e rebaixamentos
forjados. Um fixture autêntico de filme v18 conserva a impressão digital e o replay
atualizado no mesmo runtime. Veja [ASSET_FORMAT.pt-BR.md](ASSET_FORMAT.pt-BR.md).

## Laboratório e aceitação

`liquid-injected-cylinder` começa com um filme seco e uma fonte pressurizada finita.
Admissão de ar separada, solicitações de dose por ciclo, disponibilidade de vapor limitada pela parede e
reação prescrita acionam o mesmo modelo de virabrequim e carga dos outros laboratórios. JSON,
CLI, assets portáteis e o servidor MCP real compartilham as definições e os limites
de replay. Todos os parâmetros permanecem `unverified`.

Trabalho do eixo, pressão e armazenamento térmico misturado têm verificações de conservação e ODE independentes; aceitação Unity real segue pendente. [VALIDATION.pt-BR.md](VALIDATION.pt-BR.md) Capacidade geométrica, ventilação/espaço gasoso/oscilação, cavitação, enchimento/eficiência/regulação medidos e spray resolvido seguem abertos. Parâmetros `unverified`; Unity Editor/Play/Player/IL2CPP real e calibração OEM seguem não verificados.

## Extensão da agulha física

A definição opcional de agulha liga a entrega ao levantamento de translação real.
Um [solenoide, batentes elásticos e um driver amostrado](NEEDLE_ACTUATION.pt-BR.md) agora fornecem esse
movimento. Neste modo, a dose solicitada é um alvo do controlador; não limita o escoamento físico
durante o atraso de fechamento, o ricochete ou a reversão. O caminho ideal limitado por cota permanece
separado e inalterado. Comportamento magnético, de driver e de pulverização refinado, e a calibração,
continuam em aberto.

## Rail de combustível líquido alimentado por bomba

`liquid_rail_feed` associa um injetor líquido a uma bomba volumétrica existente e a uma fronteira explícita de matéria/calor. O nó de saída hidráulica deve corresponder à complacência e pressão absoluta inicial do rail. Bomba e injetor possuem esse nó; outras rotas fluidas não contabilizadas são rejeitadas.

v27 preserva conexões e temperatura da fonte e lê v1-v26. Troca analítica eixo/pressão, refinamento ODE simultâneo independente, mistura térmica, balanços massa/combustível/energia/volume, retorno e rollback completo têm verificações separadas.

Capacidade geométrica, ventilação/espaço gasoso/oscilação, cavitação, enchimento/eficiência/regulação medidos e spray resolvido seguem abertos. Parâmetros `unverified`; Unity Editor/Play/Player/IL2CPP real e calibração OEM seguem não verificados.

[PUMP_FED_FUEL.pt-BR.md](PUMP_FED_FUEL.pt-BR.md)
