# Relaxação de fase do combustível com taxa finita

[English](RELAXING_FUEL.md) · [简体中文](RELAXING_FUEL.zh-CN.md) · [Français](RELAXING_FUEL.fr.md) · [Русский](RELAXING_FUEL.ru.md) · [日本語](RELAXING_FUEL.ja.md) · [한국어](RELAXING_FUEL.ko.md) · [Deutsch](RELAXING_FUEL.de.md) · [Español](RELAXING_FUEL.es.md) · [Italiano](RELAXING_FUEL.it.md) · **Português**

## Modelo físico

Um nó `relaxing_fuel` tem fração mássica de vapor independente. Líquido e vapor compartilham pressão, temperatura e velocidade, mas o equilíbrio químico exige um tempo finito declarado. Um `relaxing_fuel_duct` transporta massa, quantidade de movimento, energia total e massa de vapor em células explícitas. A residência depende da geometria e da velocidade real.

O fechamento mantém o potencial frio do líquido compressível e o vapor ideal do modelo de equilíbrio. A fração inicial é explícita e não é projetada ao equilíbrio. Com massa, volume e energia interna fixos, a fração converge exponencialmente ao alvo com `phase_relaxation_time_ns` constante. O calor latente permanece no balanço interno. A afinidade química é o potencial do líquido menos o do vapor; seu produto pela transferência de fase, dividido pela temperatura, gera entropia não negativa. É uma lei de pesquisa, não uma correlação de nucleação medida.

[Lund: A hierarchy of relaxation models for two-phase flow](https://www.sintef.no/globalassets/project/co2-dynamics/publications/lund_two-phase_relaxation_hierarchy.pdf)

## Transporte e método numérico

Fluxos conservativos de Rusanov transportam quatro campos. Cada intervalo CFL global aplica meia fonte local, transporte e meia fonte. Reservatórios compartilhados recebem cada fonte uma vez, independentemente do número de dutos. A fonte é exata para estados isolados de volume fixo; o transporte acoplado mantém erro de primeira ordem. CFL usa o limite acústico de fração congelada. Entropias de fase e duto são verificadas separadamente. Estados fora dos limites revertem o lote inteiro.

```text
x = vapor_mass / total_mass; v = 1/rho
v_liquid(p) = 1 / (rho0 (1 + p/K)); v_vapor(T,p) = R T/p
v = (1-x) v_liquid + x v_vapor
u = (1-x) [c_liquid T + a + u_cold(p)] + x c_vapor T
u_cold(p) = K/rho0 [ln(1+p/K) - (p/K)/(1+p/K)]

dx/dt = (x_equilibrium(rho,u) - x)/tau
x(t+h) = x_equilibrium + (x(t)-x_equilibrium) exp(-h/tau)
d(total_mass)/dt = d(total_internal_energy)/dt = d(volume)/dt = 0  (local source)
affinity = mu_liquid - mu_vapor
(ds/dx) at fixed u,v = affinity/T
ds/dt = affinity (x_equilibrium-x)/(T tau) >= 0

cp_frozen = (1-x) c_liquid + x (c_vapor+R)
alpha = x R/cp_frozen
c_frozen^2 = v^2 / [(1-x) v_liquid/(K+p) + (1-alpha) x R T/p^2]
U = (rho, rho velocity, rho total_specific_energy, rho x)
F = (rho velocity, rho velocity^2+p,
     velocity (rho total_specific_energy+p), rho x velocity)
```

## Uso, evidências e limites

Execute `relaxing-fuel-evaporation` ou `relaxing-fuel-duct`. Observe frações real e de equilíbrio, afinidade, entropia de fase, balanços de vapor nas portas e resíduo global. Históricos de evaporação e condensação separam por sinal a transferência líquida da fonte em cada passo aceito; não contam trocas microscópicas brutas. As verificações cobrem solução exponencial independente, derivadas de entropia, acústica congelada, limite rígido de equilíbrio, autorrefinamento temporal e refinamento espacial acústico linear independente. Transações, reprodução portátil e MCP real são verificados separadamente; os números ficam nas evidências de validação.

Os parâmetros continuam `unverified`. Ambas as portas exigem mesmo domínio e material; portas relaxantes rejeitam trânsito de equilíbrio. Relaxação finita nos reservatórios não resolve química ou residência dentro de um bocal sem volume. Não há deslizamento de fases, tensão líquida negativa, barreira de nucleação, spray, gás portador ou receptor com combustão. Reservatórios móveis são misturados com velocidade média nula; células internas continuam rígidas com um proprietário. Use vapor ideal diluído longe do crítico, limites declarados de pressão positiva e temperatura, entalpia latente positiva e volumes de coexistência separados. A fração inicial é entrada explícita de pesquisa. Concordância numérica não comprova calibração medida nem aceitação real do Unity.

Resolver residência finita no bocal com evidências do material, depois adicionar spray resolvido e receptores com combustão compatíveis. Continuar ignição, admissão/escape, perdas mecânicas, atuação da transmissão e coordenação ECU/TCU. Calibração medida e aceitação real do Unity seguem separadas.

[VALIDATION.pt-BR.md](VALIDATION.pt-BR.md)

Um `compressible_fuel_chamber` pode controlar o volume de um reservatório `relaxing_fuel`. O trabalho reversível do eixo mantém a fração de vapor fixa; a relaxação química é uma fonte separada que conserva energia. Um `compressible_fuel_nozzle` conecta dois reservatórios relaxantes com `phase_mode` em `frozen`, transportando massa total, vapor e entalpia de estagnação. Uma agulha física com bobina, mola e batente pode dosar a abertura. [Bombas de combustível com relaxação e dosagem congelada](RELAXING_FUEL_BOUNDARIES.pt-BR.md)
