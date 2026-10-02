# Combustão premisturada e contabilidade da energia do combustível

[English](PREMIXED_COMBUSTION.md) · [简体中文](PREMIXED_COMBUSTION.zh-CN.md) · [Français](PREMIXED_COMBUSTION.fr.md) · [Русский](PREMIXED_COMBUSTION.ru.md) · [日本語](PREMIXED_COMBUSTION.ja.md) · [한국어](PREMIXED_COMBUSTION.ko.md) · [Deutsch](PREMIXED_COMBUSTION.de.md) · [Español](PREMIXED_COMBUSTION.es.md) · [Italiano](PREMIXED_COMBUSTION.it.md) · **Português**

`premixed_combustion` acopla um perfil de queima de Wiebe prescrito a um virabrequim e a uma câmara
de gás finita. Combustível, ar fresco e produtos inertes são transportados pela rede de gás;
a reação consome o reagente limitante disponível e converte a energia química armazenada
em energia térmica do gás. O trabalho de pressão aciona o mesmo solver de virabrequim usado pelos cilindros
móveis. Core, JSON, CLI, MCP e o asset v6 compartilham estas definições.

É um modelo premisturado concentrado, de propriedades constantes. Cada constituinte numa rede
conectada compartilha um único R e gamma. As três classes de massa não representam espécies detalhadas,
capacidades térmicas variáveis, cinética de reação, propagação de chama, autoignição, detonação,
emissões, evaporação de combustível nem injeção. O exemplo com queima original usa uma admissão gasosa
já misturada. A [medição de combustível por ciclo](FUEL_METERING.pt-BR.md) admite um trilho gasoso finito
separado e admissão de ar; pulverização e evaporação líquidas ficam fora do modelo. Uma queima prescrita e testes de conservação aprovados não estabelecem desempenho
medido de motor nem completam o objetivo do powertrain completo.

## Composição e portas

Um nó de gás pode acrescentar `premixed` ao objeto `gas` existente:

```json
"gas": {
  "gas_constant": { "value": 287, "unit": "j_kg_k" },
  "gamma": 1.35,
  "premixed": {
    "lower_heating_value": { "value": 44000000, "unit": "j_kg" },
    "stoichiometric_air_fuel_ratio": 14.7,
    "initial_fractions": { "fuel": 0.04, "fresh_air": 0.96 }
  }
}
```

O poder calorífico e a razão mássica estequiométrica ar/combustível devem ser positivos e finitos.
As frações de combustível e de ar fresco devem ser não negativas, com soma no máximo um. O restante
são produtos inertes. O ar fresco representa o oxidante junto com o diluente; consumir
`r` kg de ar fresco com 1 kg de combustível cria `1+r` kg de produtos. O excesso de ar fresco ou de combustível
continua disponível; os produtos não podem reagir de novo.

Cada restrição de reservatório num nó premisturado deve especificar
`reservoir_fractions` explícitas, usando os mesmos dois campos. Frações são proibidas nos outros
componentes ou em restrições internas. Na entrada, a fronteira fornece essa composição;
na saída, remove a composição real do volume finito. Volumes de gás finitos conectados
devem compartilhar rastreamento, R, gamma, LHV e razão estequiométrica. Conexões incompatíveis ou
sem rastreamento são rejeitadas; inventários químicos não podem desaparecer numa porta.

O solver de gás transfere cada constituinte com o mesmo fluxo de massa com sinal e as mesmas frações de montante
do gás total. Evolui massas de constituintes não negativas e reconstrói a massa total
a partir da soma delas. Um passo premisturado também é limitado pelo fluxo total de saída, mesmo quando
as taxas totais de massa de entrada e de saída quase se cancelam. Nenhum inventário químico é criado
por equalização de pressão nem por refluxo de reservatório.

Um gás premisturado acrescenta três valores armazenados de constituinte ao orçamento de estado declarado. Um componente
de queima acrescenta uma fronteira angular irreversível; tudo permanece dentro do limite existente de 64 estados.
Livros compensados de fronteira e de reação participam do rollback, do hash e das ramificações.

## Lei de queima e histórico do virabrequim

O componente liga `node_a` (virabrequim) a `node_b` (gás premisturado). Uma câmara móvel deve
usar o virabrequim da própria geometria, e cada câmara admite no máximo um componente de queima. Um vaso
fixo pode usar um virabrequim independente para experimentos analíticos.

```json
{
  "id": 15,
  "kind": "premixed_combustion",
  "node_a": 1,
  "node_b": 2,
  "input_channel": 103,
  "initial_input": { "value": 1, "unit": "fraction" },
  "parameters": {
    "cycle_angle": { "value": 720, "unit": "deg" },
    "start_angle": { "value": 340, "unit": "deg" },
    "duration_angle": { "value": 80, "unit": "deg" },
    "shape_exponent": 3,
    "burn_coefficient": 6.9
  }
}
```

O ciclo é explicitamente 360 ou 720 graus. O ângulo de início é relativo ao virabrequim real,
não deslocado de forma implícita pela fase geométrica do cilindro. A duração está em [1e-6 rad, ângulo de ciclo];
o expoente de forma `n` está em [1,16] e o coeficiente `a` em (0,50]. O início é normalizado módulo
o ciclo. Todos os ângulos exigem unidades. Para o avanço para a frente `z` a partir do início da queima, recortado
em [0,1], o hazard integrado é `H(z) = a z^n`. Cada ciclo completo contribui `a`.

Sobre ângulos para a frente recém-percorridos:

```text
hazard = burn_multiplier * delta(H)
limiting_fuel = min(fuel_mass, fresh_air_mass / stoichiometric_air_fuel_ratio)
burned_fuel = limiting_fuel * (1 - exp(-hazard))
consumed_air = stoichiometric_air_fuel_ratio * burned_fuel
created_products = burned_fuel + consumed_air
released_heat = LHV * burned_fuel
```

Para uma carga fechada e multiplicador 1, a fração queimada é `1-exp(-a z^n)` da
quantidade inicial de combustível limitante. Ela **não** é forçada a um no limite da duração:
`exp(-a)` permanece sem queimar depois de uma janela de queima completa. Exposições pequenas usam `expm1` para
evitar cancelação. Carga fresca que entra durante uma janela ativa junta-se aos reagentes
bem misturados; não há uma fonte de calor oculta e ilimitada por ciclo.

O canal de entrada opcional é `burn_multiplier`, uma fração em [0,1] que escala o hazard.
Zero desativa a reação; não impede o combustível de entrar por uma admissão aberta. Esta entrada
não é um comando de injetor nem um controlador preditivo de ignição.

Cada componente guarda o maior ângulo de virabrequim atingido, inicializado no ângulo de
partida. A reação só ocorre além dessa fronteira. Parar, girar para trás ou
repercorrer ângulos já visitados não pode liberar calor de novo. O avanço para a frente desativado
ainda move a fronteira, então reativar não libera o calor perdido. Começar dentro
de uma janela de queima consome só a exposição para a frente que resta. Depois de uma reversão grande,
a queima permanece suprimida até o virabrequim superar o máximo anterior; ignição bidirecional
do motor e rearme comandado pelo controlador continuam como trabalho futuro de controle.

## Energia e acoplamento numérico

A energia interna do gás permanece térmica: `U = m cv T`. A energia química é, à parte,
`E_chemical = m_fuel LHV`. A entalpia total do reservatório inclui tanto `mdot cp T` quanto a
energia química transportada. A variação global da energia armazenada inclui o inventário químico,
então a combustão é uma conversão interna, não trabalho de fonte externo adicional:

```text
energy_residual = mechanical/electrical source work + reservoir total enthalpy
                  - rejected heat - change(total stored energy)
```

`net_fuel_energy_in` expõe à parte a parte química do livro de fronteira. É
entrada líquida, inclusive combustível não queimado que deixa o modelo; não é a entrega bruta de combustível nem
uma métrica de consumo de combustível em regime permanente. `fuel_residual` e `fresh_air_residual` comparam
inventário inicial, transferência líquida de fronteira, inventário atual e reação acumulada.
`mass_residual` continua cobrindo a massa total de gás. A conversão de constituintes preserva a massa.

Para uma câmara móvel, a prévia de calor depende do ângulo de virabrequim novo de ensaio e participa
da solução não linear do virabrequim. Com calor total `Q` durante o tick e
`r = (V_old/V_new)^(gamma-1)`:

```text
U_after = (U_before + Q/2) r + Q/2
adiabatic_work = (U_before + Q/2) (1-r)
crank_work = adiabatic_work - back_pressure * (V_new - V_old)
```

O torque discreto de pressão usa esse mesmo trabalho, então energia do gás, energia química e
trabalho do virabrequim concordam. O combustível só é consumido depois que a solução tem sucesso no estado candidato.
O transporte de gás ainda usa meios passos simétricos em torno do trabalho do virabrequim e da reação. A temperatura da parede
permanece fixa ao longo do tick externo; o acoplamento de parede é de primeira ordem.

Para uma queima ativada, o percurso angular e o percurso pela velocidade nas extremidades por tick devem permanecer dentro de
`min(0.25 rad, duration_angle/32)`, com uma proteção correspondente de resolução angular em binary64.
O calor liberado não deve exceder 25% da energia térmica anterior à queima num tick. São limites
sobre o trabalho admitido e a resolução, não garantias de precisão. Valem junto com os limites de
subpasso de gás e de iteração do cilindro. Reduza `step_ns` em `numerical_failure`, alinhe
os eventos programados ao tick novo e recrie o modelo ou a sessão. Chamadas que falham ou são canceladas
não confirmam estado, entrada, fronteira, livro químico nem cursor de reprodução.

## Saídas, compatibilidade e evidência

Nós premisturados acrescentam os campos de KPI `fuel_mass`, `fresh_air_mass`, `product_mass` e `chemical_energy`.
Um componente de queima acrescenta `fuel_burned` (kg) e `heat_released` (J) acumulados.
O campo de KPI `burn_frontier` expõe o maior ângulo de virabrequim visitado (quantidade de canal
`burn_frontier_angle`, rad), para que a queima suprimida depois de uma reversão possa ser inspecionada.
Canais globais acrescentam energia química, entrada líquida de energia de combustível, resíduo de combustível e resíduo
de ar fresco. As quantidades de canal devolvidas pela descoberta são autoritativas; por exemplo, a massa de combustível
do nó chama-se `unburned_fuel_mass`. As saídas comuns de energia interna do gás e de fluxo conservam
os significados térmico e de fluxo com sinal.

Modelos premisturados acrescentam a etiqueta 7 de impressão digital e parâmetros normalizados de reação e de composição.
Impressões digitais e avanço anteriores, sem reação, permanecem inalterados. O asset v6 acrescenta registros de composição,
de frações de reservatório e de queima; fixtures autênticos v1–v5 conservam a compatibilidade. As
fidelidades novas são `premixed_gas_transport` e `premixed_wiebe_combustion`.

Os testes cobrem consumo analítico de combustível e de ar e temperatura em vaso fechado, reagentes
limitantes, transferência de reservatório nos dois sentidos, conservação de constituintes em rede fechada,
convergência de uma EDO independente de virabrequim e gás em reação, queima parada, invertida ou desativada,
contratos malformados, rollback de lote, cancelamento, ramificações e alocação zero de avanço e de instantâneo.
O [laboratório de cilindro em combustão](../assets/labs/fired-cylinder.power.json)
aciona uma carga por fases repetidas de admissão, compressão, queima, expansão e escape e
reproduz de forma idêntica nos 63 limites de relatório de JSON, CLI, MCP e asset. A evidência numérica
e o escopo real de execução estão registrados em [VALIDATION.pt-BR.md](VALIDATION.pt-BR.md).

[As equações do reator de gás ideal do Cantera](https://www.cantera.org/stable/reference/reactors/ideal-gas-reactor.html)
fornecem o contexto de massa, espécies e energia do volume de controle. O
[exemplo de motor de ignição por centelha da Ansys](https://chemkin.docs.pyansys.com/version/stable/examples/advanced/SI_engine_optimization.html)
usa temporização explícita de queima e parâmetros de Wiebe. Essas referências motivam os contratos;
a química detalhada, os modelos de duas zonas e os parâmetros de exemplo não são copiados nem
apresentados como verificação deste solver de propriedades constantes. Não há dependência de runtime
de nenhum dos dois pacotes. Todos os parâmetros de amostra permanecem `unverified`.
