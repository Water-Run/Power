# Rede de gás compilada

[English](GAS_NETWORK.md) · [简体中文](GAS_NETWORK.zh-CN.md) · [Français](GAS_NETWORK.fr.md) · [Русский](GAS_NETWORK.ru.md) · [日本語](GAS_NETWORK.ja.md) · [한국어](GAS_NETWORK.ko.md) · [Deutsch](GAS_NETWORK.de.md) · [Español](GAS_NETWORK.es.md) · [Italiano](GAS_NETWORK.it.md) · **Português**

Redes de gás finitas agora rodam por `CompiledModel` e `Simulation`. O checkpoint
cobre câmaras de volume fixo, reservatórios de pressão/temperatura fixas, orifícios
controlados e elos térmicos de parede. A [extensão de cilindro móvel](MOVING_CYLINDER.pt-BR.md) agora liga a troca de gás ao volume dependente do virabrequim e ao trabalho de pressão; o solver de volume fixo descrito abaixo conserva o comportamento original.

## API C# e unidades

```csharp
var model = CompiledModel.Compile(new ModelDefinition
{
    StepNanoseconds = 100_000,
    Nodes = [NodeDefinition.GasVolume(1, 0.002, 2e6, 900)],
    Components = [ComponentDefinition.GasReservoir(10, 1, 2e-5, 1e5, 300,
        dischargeCoefficient: 0.9, channel: 100, opening: 0)]
});
var simulation = model.CreateSimulation();
simulation.SubmitInputs([new Scalar(100, 0.5)]);
var status = simulation.Step(100_000_000);
var values = new Scalar[model.OutputCount];
var snapshot = simulation.ReadSnapshot(values);
```

`GasVolume` recebe volume em m³, pressão em Pa, temperatura em K e, opcionalmente,
R em J/(kg K) e gamma. Um nó de gás guarda o volume em `Storage`, a temperatura em `Initial`,
a pressão em `Position` e a composição em `Gas`. Litros, bar e milímetros quadrados são
aceitos por quantidades explícitas e normalizados antes da impressão digital.

`GasOrifice` une dois IDs de nó de gás. `GasReservoir` une um nó a uma fronteira fixa;
`NodeB == 0` identifica esse reservatório. `GasHeatLink` une um nó de gás e um nó térmico
com condutância em W/K. Nós de gás conectados devem compartilhar exatamente o mesmo R e gamma.
A abertura é uma fração adimensional em [0,1], validada para entradas iniciais, diretas e programadas.
Um ID de canal de entrada zero deixa a abertura inicial fixa. Redes só de gás não precisam
de rotor fictício. Os limites continuam 32 nós, 64 componentes e 64 estados escalares; cada volume
de gás consome dois estados.

Cada nó de gás expõe pressão, temperatura, massa e energia interna. As restrições
expõem o fluxo de massa com sinal de A para B; os elos térmicos expõem o fluxo de calor com sinal do gás para a parede.
A entalpia de reservatório é positiva para dentro. O resíduo de energia é
`source_work + reservoir_enthalpy - heat_rejected - stored_energy_change`.
O resíduo de massa é `sum(mass - initial_mass) - cumulative_reservoir_mass`.
Resíduos de ponto flutuante são avaliados contra escalas físicas, não contra zero exato.

## Método numérico e fronteira

O solver de gás usa subpassos explícitos com um preditor/corretor de Heun. A taxa relativa máxima
inicial de massa/energia do tick escolhe uma contagem uniforme de subpassos, mirando 2% de variação
por subpasso. Mais de 4096 subpassos, estados não físicos, valores não finitos ou uma variação corrigida
de massa/energia acima de 25% rejeitam o lote inteiro. Reduza `StepNanoseconds` e
recompile, ou inspecione a área de escoamento, o volume, a condutância e as condições iniciais.

A lei do bocal tem derivada singular em pressões iguais. Cada avaliação limita a
energia transferida à quantidade de pressão igual do par conectado, escalando juntos a massa e a
entalpia de montante. Para volumes finitos essa energia é
`abs(pA-pB) / ((gammaA-1)/VA + (gammaB-1)/VB)`; um reservatório fixo omite o termo B.
Isso evita oscilações de cruzamento de pressão em pares isolados e preserva os
livros pareados. O limitador muda a integração perto do equilíbrio; a precisão de segunda ordem só
é afirmada para o caso de refinamento de escoamento bloqueado, suave e não limitado, nos testes.

A temperatura da parede permanece no valor inicial durante os subpassos de gás. O calor de parede acumulado
entra em seguida na solução térmica existente. Esse acoplamento é de primeira ordem no tick externo;
estabilidade de passo grande ou conservação, sozinhas, não estabelecem precisão. O teste de parede
compara temperaturas em tempo finito com a solução analítica de duas capacidades. Este método
não é o solver implícito por pares proposto antes e não valida essa proposta.

Massa, energia, somas de reservatório e correções do livro compensado pertencem ao estado da simulação
e entram na cópia, no rollback, nas ramificações e nos hashes. O avanço bem-sucedido e
instantâneos no buffer do chamador não alocam memória gerenciada. Um lote programado que falha restaura
todos os ticks e entradas anteriores, inclusive a falha depois de ticks já bem-sucedidos. Atualizações
de entrada e eventos programados terminais também rejeitam observáveis de gás não finitos.

Modelos com nós de gás acrescentam a etiqueta 4 de impressão digital do solver. Impressões digitais e hashes de estado
de modelos lineares e de cilindro anteriores conservam a construção anterior. Os parâmetros de amostra
permanecem `unverified`.

## Integração JSON, de agente e portátil — 2026-09-22

O [laboratório de rede de gás](../assets/labs/gas-network.power.json) é o exemplo compartilhado
de JSON, CLI, MCP e replay portátil. Contém duas câmaras de gás, uma restrição
interna controlada, uma restrição de reservatório controlada e um elo térmico de parede. Os eventos
incluem ticks entre limites de relatório e de apresentação; cada limite de relatório é comparado
com o replay decodificado do asset. Os parâmetros permanecem sintéticos e `unverified`.

`power.model.v1` acrescenta estas definições explícitas:

| Definição | Campos JSON e unidades |
|---|---|
| Nó de gás | `domain: "gas"`; `storage`: m3 ou l; `initial`: k; `position`: pa ou bar; `gas`: gas_constant em j_kg_k e gamma > 1 |
| Orifício de gás | `kind: "gas_orifice"`; node_a/node_b; `initial_input`: fração em [0, 1]; parâmetros: area em m2 ou mm2 e discharge_coefficient |
| Orifício de reservatório | Orifício de gás com node_b ausente ou zero; também exige reservoir_pressure em pa ou bar e reservoir_temperature em k |
| Elo de parede do gás | `kind: "gas_heat_link"`; node_a é gás, node_b é térmico; parâmetros: conductance em w_k |

Um `input_channel` ausente ou zero mantém fixa a abertura inicial explícita. Parâmetros de reservatório
são proibidos numa restrição de dois volumes. A composição só é exigida em
nós de gás. Os novos campos de verificação são `mass_flow`, `heat_flow`, `reservoir_enthalpy` e
`mass_residual`; os campos existentes de estado do gás e de energia continuam disponíveis.

`CompiledModel.ValidateInput` verifica restrições estáticas de canal e de valor sem alterar
o estado. A validação do experimento e a criação do asset portátil usam isso para todas as aberturas
programadas, inclusive eventos posteriores. A submissão e o avanço em runtime ainda fazem verificações
adicionais de observáveis dependentes do estado e conservam o rollback completo.

`power.asset.v3` e versões posteriores conservam composição do gás, área, coeficiente de descarga e pressão
de reservatório com registros de extensão indexados e limitados. Condutância de parede, temperatura de reservatório,
aberturas iniciais e IDs de canal de entrada usam os campos base do componente. Os leitores v1/v2
continuam aceitos para os conjuntos de modelos originais e rejeitam definições de gás. Fixtures
autênticos anteriores à mudança verificam a compatibilidade retroativa. Veja [o formato de asset](ASSET_FORMAT.pt-BR.md).

As capacidades MCP versão 0.8.0 anunciam o domínio de gás, os componentes, a fidelidade, os limites
de abertura e os limites delimitados do solver. `get_example_model` aceita `gas-network`. A compilação
exporta `GasNetwork.powerasset`; o Studio acrescenta vasos esquemáticos, marcadores de reservatório e
caminhos de restrição e de calor, com as entradas e os canais de saída existentes. Os novos testes de importação e
de Play Mode exigem uma execução real do Editor do Unity e não são cobertos por evidência .NET.

## Validação e o trabalho de motor que resta

Os nove grupos de modelo compilado e os seis grupos de primitivos de gás continuam rodando nos
dois destinos do Core. Os testes portáteis cobrem ainda modelos mistos de cilindro, gás e térmico,
quantidades fora do SI, composição não padrão, corrupção de extensão, registros
ausentes ou duplicados, compatibilidade v1/v2, limites programados, cancelamento e rollback do cursor de eventos.
A equivalência JSON/Core e o replay real do MCP cobrem a fronteira de integração.
Veja a [validação](VALIDATION.pt-BR.md) para os resultados da verificação em série.

Os assemblies Standard rodam em .NET 10 nestas verificações; isso não é evidência de Editor do Unity nem de
IL2CPP. As equações do solver só de volume fixo, os limites de integração e a construção da impressão digital
permanecem inalterados para modelos sem restrições temporizadas ou rastreamento premisturado. Modelos com câmaras móveis
ou restrições temporizadas usam o acoplamento dividido, com versão própria, documentado em
[MOVING_CYLINDER.pt-BR.md](MOVING_CYLINDER.pt-BR.md) e [VALVE_TIMING.pt-BR.md](VALVE_TIMING.pt-BR.md).

A [combustão premisturada](PREMIXED_COMBUSTION.pt-BR.md) opcional agora transporta combustível, ar fresco
e produtos com propriedades de gás constantes. Termoquímica detalhada de espécies, amostras
de veículos calibradas e os marcos completos de motor, transmissão e controle continuam em aberto.
