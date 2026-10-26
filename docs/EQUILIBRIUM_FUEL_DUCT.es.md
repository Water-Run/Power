# Conductos de combustible compresible en equilibrio

[English](EQUILIBRIUM_FUEL_DUCT.md) · [简体中文](EQUILIBRIUM_FUEL_DUCT.zh-CN.md) · [Français](EQUILIBRIUM_FUEL_DUCT.fr.md) · [Русский](EQUILIBRIUM_FUEL_DUCT.ru.md) · [日本語](EQUILIBRIUM_FUEL_DUCT.ja.md) · [한국어](EQUILIBRIUM_FUEL_DUCT.ko.md) · [Deutsch](EQUILIBRIUM_FUEL_DUCT.de.md) · **Español** · [Italiano](EQUILIBRIUM_FUEL_DUCT.it.md) · [Português](EQUILIBRIUM_FUEL_DUCT.pt-BR.md)

## Modelo y material

Un `equilibrium_fuel_duct` transporta masa, cantidad de movimiento y energía total por celdas explícitas `compressible_fuel`. El líquido es compresible y el vapor ideal; ambas fases comparten presión, temperatura y velocidad. El equilibrio local permite condensación y vaporización súbita. Dos depósitos finitos estacionarios y una pared térmica finita cierran los balances.

Una `compressible_fuel_chamber` puede cambiar por trabajo del eje el volumen de cualquiera de los depósitos mezclados. La velocidad global permanece en cero y las celdas internas siguen rígidas. El impulso estacionario de puertos/carcasa no realiza trabajo; el trabajo del pistón es un intercambio mecánico separado. [Cámaras de combustible compresible accionadas por eje](COMPRESSIBLE_FUEL_CHAMBER.es.md)

El líquido tiene expansión térmica nula y calor específico constante. `K` es su módulo volumétrico a presión cero; el valor real es `K+p`. Energía de compresión fría, potenciales químicos, entropía y velocidad del sonido en equilibrio proceden del mismo potencial. `a` es el desplazamiento energético líquido existente, `p0(T)` la curva de saturación que desprecia el volumen líquido, `x` la fracción másica de vapor y `Cv_eq` la derivada de energía interna de la mezcla a volumen específico fijo. `Tr`, `Pref` y `Lu_ref` son temperatura, presión y energía interna latente de referencia. Se usan unidades SI.

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

## Transporte y fronteras

Declare `liquid_bulk_modulus`, omita `compliance` y especifique `initial_velocity` en cada nodo. Declare 1–24 `cells` internas distintas y ordenadas; cada una pertenece a un conducto. Su longitud es volumen dividido por `area`. Los depósitos tienen velocidad cero y no pueden ser celdas internas. Todos los puertos comparten material. `linear_resistance` y `quadratic_resistance` representan la resistencia total, distribuida por volumen; `fluid_heat_fraction` reparte calor de fricción entre fluido y pared. No hay entrada directa de apertura.

```text
U = [rho, rho velocity, rho (u + velocity^2/2)]
F = [rho velocity, rho velocity^2+p, (rho (u+velocity^2/2)+p) velocity]
F_Rusanov = (F_left+F_right)/2 - a_max (U_right-U_left)/2
a_max = max_ports(abs(velocity)) + sqrt(max(K/rho0, (1+R/cv) R Tmax))
```

El flujo conservativo de Rusanov avanza todas las caras desde el mismo estado de subpaso. La cota de velocidad suma la máxima velocidad absoluta y la cota acústica del material. El límite CFL es 0.2 veces el volumen mínimo de puerto dividido por área y velocidad; cada intervalo admite hasta 4096 subpasos. Transporte y decaimiento local exacto por resistencia se separan a primer orden. La difusión numérica contribuye al cambio total de entropía, aparte del calor de fricción informado. Las fronteras estacionarias absorben impulso sin trabajo mecánico; la energía cinética entrante se termaliza en el inventario finito. Estados inválidos o una disminución de entropía superior al redondeo rechazan todo el lote. Reduzca el paso y revise límites materiales y resolución.

## Uso y evidencia

Ejecute `equilibrium-fuel-condensing-duct` o `equilibrium-fuel-flashing-duct`. Cada celda expone densidad, velocidad, fracción de fase, velocidad del sonido, temperatura y energía. Masa/energía en puertos, impulso de pared e inventarios completos son observables. Las pruebas independientes cubren trabajo de compresión, límite de vapor ideal, igualdad de potenciales químicos, relaciones de Clapeyron y Gibbs, sonido por inversión de entropía, inversión masa-energía, conservación y fricción con pared finita. Los errores acústicos medios de celda disminuyen con 8, 16 y 24 celdas. Reproducción exacta, reversión, cancelación, ramas, pasos sin asignaciones, assets portables y MCP real se verifican por separado; los resultados detallados están en la evidencia de validación.

[VALIDATION.es.md](VALIDATION.es.md)

## Validez y próximos pasos

Los parámetros siguen `unverified`. Respete los rangos declarados de temperatura y presión, entalpía latente positiva, volúmenes de coexistencia separados y vapor diluido. Es un conducto horizontal rígido de sección constante y equilibrio homogéneo instantáneo. Omite nucleación a velocidad finita, deslizamiento, tensión superficial, gravedad, elasticidad y comportamiento crítico real. La difusión de primer orden puede ensanchar frentes; conservación y reproducción no demuestran precisión espacial ni validez de hardware medido. La aceptación real de Unity está pendiente.

Añadir fronteras compatibles de válvula direccional y aguja/tobera compresibles, con entalpía, energía cinética e hipótesis de fase explícitas. Después abordar relajación/deslizamiento, encendido, admisión/escape, pérdidas mecánicas y coordinación ECU/TCU. Calibración medida y aceptación real de Unity siguen separadas.

[NIST: thermodynamic derivatives](https://pages.nist.gov/teqp-docs/en/latest/derivs/derivs.html) · [Lund: two-phase relaxation hierarchy](https://www.sintef.no/globalassets/project/co2-dynamics/publications/lund_two-phase_relaxation_hierarchy.pdf) · [MFEM: Rusanov flux](https://docs.mfem.org/html/classmfem_1_1RusanovFlux.html) · [Modelica: conservation balances](https://doc.modelica.org/Modelica%203.2.3/Resources/helpWSM/Modelica/Modelica.Fluid.UsersGuide.ComponentDefinition.BalanceEquations.html)
