# Bombas de combustible con relajación y dosificación congelada

[English](RELAXING_FUEL_BOUNDARIES.md) · [简体中文](RELAXING_FUEL_BOUNDARIES.zh-CN.md) · [Français](RELAXING_FUEL_BOUNDARIES.fr.md) · [Русский](RELAXING_FUEL_BOUNDARIES.ru.md) · [日本語](RELAXING_FUEL_BOUNDARIES.ja.md) · [한국어](RELAXING_FUEL_BOUNDARIES.ko.md) · [Deutsch](RELAXING_FUEL_BOUNDARIES.de.md) · **Español** · [Italiano](RELAXING_FUEL_BOUNDARIES.it.md) · [Português](RELAXING_FUEL_BOUNDARIES.pt-BR.md)

## Modelo físico

Un `compressible_fuel_chamber` puede controlar el volumen de un depósito `relaxing_fuel`. El trabajo reversible del eje mantiene fija la fracción de vapor; la relajación química es una fuente separada que conserva energía. Un `compressible_fuel_nozzle` conecta dos depósitos relajantes con `phase_mode` en `frozen`, transportando masa total, vapor y entalpía de remanso. Una aguja física con bobina, muelle y tope puede dosificar la apertura.

[NASA: isentropic ideal-gas relations](https://www.grc.nasa.gov/www/k-12/airplane/isentrop.html) · [Lund: two-phase relaxation hierarchy](https://www.sintef.no/globalassets/project/co2-dynamics/publications/lund_two-phase_relaxation_hierarchy.pdf)

## Acoplamiento y cálculo numérico

La relación de entropía a fracción fija determina presión y temperatura tras cambiar el volumen. El trabajo incluye compresión fría del líquido y funciones incrementales estables para movimientos pequeños. El mismo trabajo discreto genera el par del eje. Cada intervalo global aplica una vez la fuente de cada nodo alrededor del transporte conservativo por conducto y tobera congelada. CFL y 0.02 del menor inventario de puerto limitan los intervalos, con máximo de 4096. Trabajo y fuente se separan. El transporte acoplado sigue siendo de primer orden y requiere refinamiento temporal.

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

## Uso, evidencia y límites

Ejecute `relaxing-fuel-chamber` o `relaxing-pump-needle`. Observe volumen/trabajo, fracciones real y de equilibrio, entropía de fase, `total_vapor_delivered`, inventarios de puertos y residuo global de vapor. Se comparan adiabáticas puras, cuadratura independiente de presión a fracción fija, trabajo infinitesimal, ciclo reversible y ODE independiente de eje con vapor ideal. Refinamiento acoplado, tiempo de fuente en depósitos compartidos, balances completos, cancelación, reversión, ramas, asignaciones, reproducción portátil y MCP real se verifican aparte. Los valores figuran en la evidencia de validación.

Los parámetros siguen `unverified`. Ambos puertos deben tener el mismo dominio y material; los relajantes rechazan tránsito de equilibrio. La relajación finita en depósitos no resuelve conversión química ni residencia dentro de una tobera sin volumen. Faltan deslizamiento de fases, tensión negativa del líquido, barrera de nucleación, pulverización, gas portador y receptor con combustión. Los depósitos móviles están mezclados con velocidad global nula; las celdas del conducto son rígidas con un propietario. Use vapor ideal diluido lejos de la zona crítica, límites declarados de presión positiva y temperatura, entalpía latente positiva y volúmenes de coexistencia separados. La fracción inicial es una entrada de investigación explícita. El acuerdo numérico no acredita calibración medida ni aceptación real de Unity.

Resolver residencia finita en la tobera con evidencia del material, después añadir pulverización resuelta y receptores con combustión compatibles. Continuar encendido, admisión/escape, pérdidas mecánicas, actuación de transmisión y coordinación ECU/TCU. Calibración medida y aceptación real de Unity siguen separadas.

[VALIDATION.es.md](VALIDATION.es.md)
