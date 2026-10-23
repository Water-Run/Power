# Dutos de combustível compressível em equilíbrio

[English](EQUILIBRIUM_FUEL_DUCT.md) · [简体中文](EQUILIBRIUM_FUEL_DUCT.zh-CN.md) · [Français](EQUILIBRIUM_FUEL_DUCT.fr.md) · [Русский](EQUILIBRIUM_FUEL_DUCT.ru.md) · [日本語](EQUILIBRIUM_FUEL_DUCT.ja.md) · [한국어](EQUILIBRIUM_FUEL_DUCT.ko.md) · [Deutsch](EQUILIBRIUM_FUEL_DUCT.de.md) · [Español](EQUILIBRIUM_FUEL_DUCT.es.md) · [Italiano](EQUILIBRIUM_FUEL_DUCT.it.md) · **Português**

## Modelo e material

Um `equilibrium_fuel_duct` transporta massa, quantidade de movimento e energia total por células explícitas `compressible_fuel`. O líquido é compressível e o vapor ideal; as fases compartilham pressão, temperatura e velocidade. O equilíbrio local permite condensação e vaporização súbita. Dois reservatórios finitos estacionários e uma parede térmica finita fecham os balanços.

O líquido tem expansão térmica nula e calor específico constante. `K` é o módulo volumétrico na pressão zero; o valor real é `K+p`. Energia de compressão fria, potenciais químicos, entropia e velocidade do som em equilíbrio derivam do mesmo potencial. `a` é o deslocamento energético líquido existente, `p0(T)` a curva de saturação que despreza o volume líquido, `x` a fração mássica de vapor e `Cv_eq` a derivada da energia interna da mistura a volume específico fixo. `Tr`, `Pref` e `Lu_ref` são temperatura, pressão e energia interna latente de referência. Usam-se unidades SI.

```text
p_l = K (rho_l/rho0 - 1); y = p/K
v_l = 1/[rho0 (1+y)]
u_cold = (K/rho0) [ln(1+y) - y/(1+y)]
u_l = cl T + a + u_cold
h_l = cl T + a + (K/rho0) ln(1+y)
s_l = cl ln(T/Tr)
u_v = cv T; v_v = R T/p; h_v = (cv+R) T
s_v = (cv+R) ln(T/Tr) - R ln(p/Pref) + Lu_ref/Tr + R
R T ln[p_sat/p0(T)] = (K/rho0) ln(1+p_sat/K)
v = (1-x) v_l + x v_v; u = (1-x) u_l + x u_v
dp_sat/dT = (h_v-h_l)/[T(v_v-v_l)]
c_eq^2 = v^2 T (dp_sat/dT)^2 / Cv_eq
c_l^2 = K/rho0; c_v^2 = (1+R/cv) R T
```

## Transporte e fronteiras

Declare `liquid_bulk_modulus`, omita `compliance` e forneça `initial_velocity` explicitamente em cada nó. Declare 1–24 `cells` internas ordenadas e distintas, cada uma pertencente a um duto. O comprimento é o volume dividido por `area`. Reservatórios têm velocidade zero e não podem ser células internas. Todas as portas compartilham material. `linear_resistance` e `quadratic_resistance` representam a resistência total, distribuída por volume; `fluid_heat_fraction` divide o calor de atrito entre fluido e parede. Não existe entrada direta de abertura.

```text
U = [rho, rho velocity, rho (u + velocity^2/2)]
F = [rho velocity, rho velocity^2+p, (rho (u+velocity^2/2)+p) velocity]
F_Rusanov = (F_left+F_right)/2 - a_max (U_right-U_left)/2
a_max = max_ports(abs(velocity)) + sqrt(max(K/rho0, (1+R/cv) R Tmax))
```

O fluxo conservativo de Rusanov avança todas as faces a partir do mesmo estado de subpasso. O limite de velocidade soma a maior velocidade absoluta e o limite acústico do material. O limite CFL é 0.2 vezes o menor volume de porta dividido por área e velocidade; cada intervalo permite até 4096 subpassos. Transporte e decaimento local exato por arrasto são separados em primeira ordem. A difusão numérica contribui para a variação total de entropia, distinta do calor de atrito informado. Fronteiras estacionárias absorvem impulso sem trabalho mecânico; a energia cinética que entra se termaliza no inventário finito. Estados inválidos ou queda de entropia além do arredondamento rejeitam todo o lote. Reduza o passo e examine limites do material e resolução.

## Uso e evidências

Execute `equilibrium-fuel-condensing-duct` ou `equilibrium-fuel-flashing-duct`. Cada célula expõe densidade, velocidade, fração de fase, velocidade do som, temperatura e energia. Massa/energia nas portas, impulso da parede e inventários completos são observáveis. Verificações independentes abrangem trabalho de compressão, limite do vapor ideal, igualdade dos potenciais químicos, relações de Clapeyron e Gibbs, som por inversão da entropia, inversão massa-energia, conservação e atrito com parede finita. Os erros acústicos médios de célula diminuem com 8, 16 e 24 células. Reprodução exata, reversão, cancelamento, ramificações, passos sem alocação, assets portáveis e MCP real são verificados separadamente; detalhes constam nas evidências de validação.

[VALIDATION.pt-BR.md](VALIDATION.pt-BR.md)

## Validade e próximos passos

Os parâmetros continuam `unverified`. Respeite as faixas declaradas de temperatura e pressão, entalpia latente positiva, volumes de coexistência separados e vapor diluído. O duto é rígido, horizontal, de seção constante e equilíbrio homogêneo instantâneo. Não inclui nucleação com taxa finita, deslizamento, tensão superficial, gravidade, elasticidade ou comportamento crítico real. A difusão de primeira ordem pode alargar frentes; conservação e reprodução não demonstram precisão espacial nem validade de hardware medido. A aceitação real no Unity permanece pendente.

Conecte dutos compressíveis a câmaras acionadas por eixo e fronteiras de agulha/bocal compatíveis, com trabalho de pressão e hipóteses de fase explícitos. Depois avance relaxação/deslizamento, ignição, admissão/escape, perdas mecânicas e coordenação ECU/TCU. Calibração medida e aceitação real no Unity permanecem separadas.

[NIST: thermodynamic derivatives](https://pages.nist.gov/teqp-docs/en/latest/derivs/derivs.html) · [Lund: two-phase relaxation hierarchy](https://www.sintef.no/globalassets/project/co2-dynamics/publications/lund_two-phase_relaxation_hierarchy.pdf) · [MFEM: Rusanov flux](https://docs.mfem.org/html/classmfem_1_1RusanovFlux.html) · [Modelica: conservation balances](https://doc.modelica.org/Modelica%203.2.3/Resources/helpWSM/Modelica/Modelica.Fluid.UsersGuide.ComponentDefinition.BalanceEquations.html)
