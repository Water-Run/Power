# Bombas de combustível com relaxação e dosagem congelada

[English](RELAXING_FUEL_BOUNDARIES.md) · [简体中文](RELAXING_FUEL_BOUNDARIES.zh-CN.md) · [Français](RELAXING_FUEL_BOUNDARIES.fr.md) · [Русский](RELAXING_FUEL_BOUNDARIES.ru.md) · [日本語](RELAXING_FUEL_BOUNDARIES.ja.md) · [한국어](RELAXING_FUEL_BOUNDARIES.ko.md) · [Deutsch](RELAXING_FUEL_BOUNDARIES.de.md) · [Español](RELAXING_FUEL_BOUNDARIES.es.md) · [Italiano](RELAXING_FUEL_BOUNDARIES.it.md) · **Português**

## Modelo físico

Um `compressible_fuel_chamber` pode controlar o volume de um reservatório `relaxing_fuel`. O trabalho reversível do eixo mantém a fração de vapor fixa; a relaxação química é uma fonte separada que conserva energia. Um `compressible_fuel_nozzle` conecta dois reservatórios relaxantes com `phase_mode` em `frozen`, transportando massa total, vapor e entalpia de estagnação. Uma agulha física com bobina, mola e batente pode dosar a abertura.

[NASA: isentropic ideal-gas relations](https://www.grc.nasa.gov/www/k-12/airplane/isentrop.html) · [Lund: two-phase relaxation hierarchy](https://www.sintef.no/globalassets/project/co2-dynamics/publications/lund_two-phase_relaxation_hierarchy.pdf)

## Acoplamento e método numérico

A relação de entropia com fração fixa determina pressão e temperatura após variar o volume. O trabalho inclui compressão fria do líquido e funções incrementais estáveis para pequenos movimentos. O mesmo trabalho discreto gera o torque do eixo. Cada intervalo global aplica uma vez a fonte de cada nó ao redor do transporte conservativo em duto e bocal congelado. CFL e 0.02 do menor inventário de porta limitam os intervalos, no máximo 4096. Trabalho e fonte ficam separados. O transporte acoplado continua de primeira ordem e exige refinamento temporal.

```text
x = vapor_mass / total_mass
cp_x = (1-x) c_liquid + x (c_vapor+R)
cv_x = (1-x) c_liquid + x c_vapor
alpha = x R/cp_x
T/T0 = (p/p0)^alpha                         (fixed quality and entropy)
v(p) = (1-x)/(rho0 (1+p/K)) + x R T/p
work_on_fluid = mass [cv_x T0 expm1(alpha ln(p/p0)) + (1-x) delta_u_cold]
fluid_torque = -work_on_fluid/delta_angle   (nonzero angular increment)
delta_vapor_mass_volume_work = 0
delta_vapor_mass_nozzle = transferred_total_mass * source_vapor_quality
donor_delta_energy = -transferred_total_mass * stagnation_enthalpy
receiver_delta_energy = -donor_delta_energy - wall_fraction * jet_kinetic_energy
dx/dt_phase_source = (x_equilibrium(rho,u)-x)/tau
```

## Uso, evidências e limites

Execute `relaxing-fuel-chamber` ou `relaxing-pump-needle`. Observe volume/trabalho, frações real e de equilíbrio, entropia de fase, `total_vapor_delivered`, inventários das portas e resíduo global de vapor. As verificações comparam adiabáticas puras, quadratura independente da pressão com fração fixa, trabalho infinitesimal, ciclo reversível e ODE independente do eixo com vapor ideal. Refinamento acoplado, tempo de fonte compartilhado, balanços completos, cancelamento, reversão, ramificações, alocações, reprodução portátil e MCP real são verificados separadamente. Os números estão nas evidências de validação.

Os parâmetros continuam `unverified`. Ambas as portas exigem mesmo domínio e material; portas relaxantes rejeitam trânsito de equilíbrio. Relaxação finita nos reservatórios não resolve química ou residência dentro de um bocal sem volume. Não há deslizamento de fases, tensão líquida negativa, barreira de nucleação, spray, gás portador ou receptor com combustão. Reservatórios móveis são misturados com velocidade média nula; células internas continuam rígidas com um proprietário. Use vapor ideal diluído longe do crítico, limites declarados de pressão positiva e temperatura, entalpia latente positiva e volumes de coexistência separados. A fração inicial é entrada explícita de pesquisa. Concordância numérica não comprova calibração medida nem aceitação real do Unity.

Resolver residência finita no bocal com evidências do material, depois adicionar spray resolvido e receptores com combustão compatíveis. Continuar ignição, admissão/escape, perdas mecânicas, atuação da transmissão e coordenação ECU/TCU. Calibração medida e aceitação real do Unity seguem separadas.

[VALIDATION.pt-BR.md](VALIDATION.pt-BR.md)
