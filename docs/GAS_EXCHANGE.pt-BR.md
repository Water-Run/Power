# Primitivos de troca de gás

[English](GAS_EXCHANGE.md) · [简体中文](GAS_EXCHANGE.zh-CN.md) · [Français](GAS_EXCHANGE.fr.md) · [Русский](GAS_EXCHANGE.ru.md) · [日本語](GAS_EXCHANGE.ja.md) · [한국어](GAS_EXCHANGE.ko.md) · [Deutsch](GAS_EXCHANGE.de.md) · [Español](GAS_EXCHANGE.es.md) · [Italiano](GAS_EXCHANGE.it.md) · **Português**

Este documento registra a primeira fatia do incremento de troca de gás descrito nas
[notas de retomada do motor](NEXT_ENGINE_STEP.pt-BR.md): a física de escoamento e de volume de controle, validada
por si, antes de qualquer parte ser ligada ao grafo do modelo compilado.

Os primitivos estão em `src/Power.Core/GasExchange.cs` e são cobertos por
`tests/Power.Tests/GasChecks.cs`. Um [checkpoint posterior da rede de gás no Core](GAS_NETWORK.pt-BR.md)
agora liga nós de gás finitos, restrições, elos térmicos e livros de conservação ao
modelo compilado. A integração de 2026-09-22 acrescenta JSON, CLI/MCP e o asset portátil v3,
mantendo leitores v1/v2 para os conjuntos de modelos originais. Vistas esquemáticas e
testes do Unity estão preparados; a verificação real no Editor continua pendente. O cilindro adiabático fechado permanece uma
referência analítica inalterada, ainda sem troca de massa pela câmara acoplada ao virabrequim.

## O que está implementado

| Tipo | Responsabilidade |
|---|---|
| `IdealGas` | Gás caloricamente perfeito de uma composição fixa: `R`, `gamma`, `cv`, `cp`, a razão crítica de pressão e os dois coeficientes pré-calculados de fluxo de massa do bocal. |
| `GasVolumeState` | Um volume finito rastreado por **massa e energia interna como estados independentes**, com densidade, temperatura, pressão e entalpia específica derivadas. |
| `Orifice` | Escoamento compressível ideal por uma restrição, com coeficiente de descarga e fração de abertura adimensional em `[0,1]`, com sinal nos dois sentidos, bloqueado e subcrítico. |

`GasVolumeState` substitui de propósito a derivação por ângulo e entropia inicial do cilindro fechado.
Como a massa e a energia interna são carregadas de forma independente, o mesmo estado pode absorver massa
transportada, entalpia transportada e calor de parede sem supor um histórico isentrópico.

## Equações

A pressão estática usa `p = (gamma - 1) U / V`, exata para um gás caloricamente perfeito e que evita
uma ida e volta separada pela temperatura. A temperatura é `T = U / (m cv)`.

O fluxo de massa segue as relações padrão de bocal isentrópico. Com `A` a área efetiva
(área geométrica x coeficiente de descarga x abertura), estado estático de montante `p_u, T_u` e razão
de pressão `pr = p_d / p_u`:

- bloqueado, `pr <= (2/(gamma+1))^(gamma/(gamma-1))`:
  `mdot = A (p_u / sqrt(T_u)) sqrt(gamma/R) (2/(gamma+1))^((gamma+1)/(2(gamma-1)))`
- subcrítico: `mdot = A (p_u / sqrt(T_u)) sqrt(2 gamma / (R (gamma-1)) (pr^(2/gamma) - pr^((gamma+1)/gamma)))`

A corrente transporta a entalpia de montante, `hdot = mdot cp T_u`, então o sentido do escoamento decide
qual temperatura de extremidade é transportada. Um reservatório é passado como um par `(p, T)` comum, então
não é preciso um volume fictício para uma fronteira fixa.

Referência para os dois ramos: [bloqueio de fluxo de massa da NASA](https://www.grc.nasa.gov/www/k-12/BGP/mflchk.html).
É uma referência para as relações, não uma validação desta implementação.

## Notas numéricas

A função de escoamento subcrítico é avaliada como `pr^(2/gamma) * (1 - pr^((gamma-1)/gamma))`, com o
segundo fator calculado por `expm1`. A diferença de manual entre duas potências quase iguais se cancela
de forma catastrófica quando `pr` se aproxima de um: em `pr = 1 - 1e-12` ela conserva cerca de quatro dígitos, enquanto
a forma com `expm1` é precisa até a precisão da razão armazenada. `Numeric.Expm1` e `Numeric.Log1p`
agora são compartilhados com a física do cilindro fechado, em vez de duplicados.

Dois limites são inerentes ao modelo, e não à implementação, e um solver que o adote
precisa tratar os dois:

- O ramo subcrítico tem **derivada infinita na razão de pressão unitária**. Um passo de Newton não
  deve atravessar esse ponto em linha reta; delimite-o ou amorteça-o.
- Uma razão perto de um não pode ser representada de forma útil em binary64. Em `pr = 1 - 1e-15` só cerca de um
  dígito do desvio sobrevive, não importa como a função seja escrita.

As condições de estagnação e estática de montante são tratadas como iguais. Essa é a aproximação quase estacionária
usual de volume de controle e **não** vale para escoamento de câmara em Mach alto.

## Evidência

`tests/Power.Tests/GasChecks.cs` acrescenta seis verificações, cada uma escrita contra uma forma fechada independente
e não contra uma saída registrada deste código:

1. **Propriedades e continuidade do bloqueio** — `cv`, `cp` e a razão crítica contra as
   definições para `gamma` em `{1.1, 1.3, 1.4, 5/3}`; o ramo subcrítico atingindo o
   coeficiente bloqueado exatamente na razão crítica; decaimento monótono da função de escoamento até zero, conferido
   contra a forma ingênua onde essa forma é confiável e contra a expansão de ordem dominante
   onde não é.
2. **Escoamento de bocal** — 54 combinações de pressão de montante, temperatura de montante e razão de pressão
   contra as relações da NASA escritas por extenso, inclusive que o escoamento bloqueado independe da
   pressão de jusante e que a entalpia é transportada na temperatura de montante.
3. **Contratos** — antissimetria exata ao trocar as extremidades, fluxo zero em orifício fechado e
   em pressões iguais, linearidade na fração de abertura e rejeição de estados não finitos ou
   não positivos, aberturas fora de `[0,1]` e parâmetros inválidos de gás ou de orifício.
4. **Descarga adiabática de vaso** — integração RK4 de um vaso de 2 L a partir de 20 bar e 900 K contra a
   solução isentrópica analítica `rho(t) = (rho0^-k + k C t / V)^(-1/k)`, `k = (gamma-1)/2`, coincidente até
   1e-9 relativo em densidade e temperatura e 1e-8 em pressão, com uma verificação de refinamento.
5. **Enchimento por reservatório** — carga de um vaso de 0.5 L a partir de um reservatório a 6 bar e 320 K: a identidade exata
   `dU = cp T_supply dm` enquanto o escoamento é unidirecional, e o limite de vaso evacuado
   `T -> gamma T_supply`, conferido a partir de duas pressões iniciais diferentes.
6. **Rede fechada de dois volumes** — 2 s de troca entre um volume quente de 1.5 L e um volume frio de 0.4 L:
   massa total conservada até 1e-14 relativo e energia interna total até 1e-12 relativo, pressões
   se igualando, e o equilíbrio confirmado como mecânico, e não como a temperatura da mistura completa.

## O que continua em aberto

O [checkpoint da rede de gás no Core](GAS_NETWORK.pt-BR.md) agora cobre nós de volume fixo,
reservatórios, restrições, elos térmicos, livros de massa e de energia, canais de saída e
avanço transacional limitado. JSON, assets portáteis, descoberta de capacidades e exemplos de replay
estão integrados. A [extensão de cilindro móvel](MOVING_CYLINDER.pt-BR.md) agora acopla troca de gás e trabalho
do virabrequim. O [comando opcional por ângulo de virabrequim](VALVE_TIMING.pt-BR.md) controla restrições, e a
[combustão premisturada](PREMIXED_COMBUSTION.pt-BR.md) acrescenta combustível/ar/produtos e a
contabilidade de energia química. A evidência do Editor do Unity é uma
entrega separada. O método implícito por pares proposto não foi adotado: o método
explícito atual, o limitador de equilíbrio e os limites de precisão estão documentados lá.
Volumes conectados exigem as mesmas constantes de gás e o mesmo gamma; a termoquímica detalhada de espécies continua em aberto.
