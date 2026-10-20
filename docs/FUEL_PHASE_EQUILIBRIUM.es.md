# Equilibrio de fases del combustible y límites de suministro

[English](FUEL_PHASE_EQUILIBRIUM.md) · [简体中文](FUEL_PHASE_EQUILIBRIUM.zh-CN.md) · [Français](FUEL_PHASE_EQUILIBRIUM.fr.md) · [Русский](FUEL_PHASE_EQUILIBRIUM.ru.md) · [日本語](FUEL_PHASE_EQUILIBRIUM.ja.md) · [한국어](FUEL_PHASE_EQUILIBRIUM.ko.md) · [Deutsch](FUEL_PHASE_EQUILIBRIUM.de.md) · **Español** · [Italiano](FUEL_PHASE_EQUILIBRIUM.it.md) · [Português](FUEL_PHASE_EQUILIBRIUM.pt-BR.md)

## Contrato

La opción `fuel_film.parameters.phase_equilibrium` declara `reference_pressure` en `pa` o `bar`, y `minimum_temperature` / `maximum_temperature` en `k`. `saturation_temperature` es la temperatura de referencia; `latent_internal_energy` es la diferencia de energía interna vapor-líquido allí. Cada receptor en equilibrio tiene una película. La instantánea inicial se conserva; los intervalos aceptados equilibran gas y líquido e intercambian calor con la pared finita.

`mass_flow` es la transferencia neta con signo del paso físico completo. `evaporated_fuel_mass` y `condensed_fuel_mass` acumulan sus masas positiva y negativa sin duplicar proyecciones que se cancelan. `vapor_partial_pressure` muestra la presión parcial de combustible. Los balances térmico, másico y químico incluyen ambas fases.

## Referencia termodinámica

El modelo usa una pseudoespecie ideal, la constante gaseosa común del receptor, capacidades constantes y desplazamiento líquido despreciable. La igualdad de potenciales químicos da la saturación siguiente. A volumen fijo, combustible total y energía interna determinan temperatura y masa de vapor. El inventario químico se transfiere internamente; la reacción solo consume vapor.

```text
a = (cv - cl) T_ref - L_u,ref
u_l(T) = cl T + a
u_v(T) = cv T
L_u(T) = (cv - cl) T - a
ln(p_sat(T) / p_ref) = (cv + R - cl) / R * ln(T / T_ref)
                        + a / R * (1/T - 1/T_ref)
m_v(T) = min(M_f, p_sat(T) V / (R T))
U_eq(T) = (M_c + m_v) cv T + (M_f - m_v) u_l(T)
```

## Límites de suministro y numéricos

Una solución escalar acotada usa la energía de equilibrio monótona. El calor de pared usa Euler implícito. Transporte y trabajo del cilindro móvil tienen evidencia de refinamiento de primer orden. La coexistencia líquida queda en el intervalo declarado. Tras evaporarse completamente dentro de él, el gas puede sobrecalentarse sin extrapolar la curva.

El suministro predeterminado rechaza líquido bajo saturación. La opción de tanque finito resuelve vapor fuente con potenciales corregidos por presión. Riel y aspiración aún llevan líquido separado; burbujas y flujo cavitante requieren más física.

La comparación solo admite redondeo binary64: `64 * epsilon * max(p, p_sat)`, con `epsilon = 2^-52`. El margen informado no se recorta. Es una comprobación de saturación global; pérdidas locales y NPSH de bomba requieren un modelo de flujo resuelto. Las comprobaciones independientes también exigen entropía total no decreciente en el flash adiabático y el intercambio con la pared finita.

## Evidencia

`equilibrium-fuel-cell` muestra condensación, evaporación por venteo y nueva condensación. `equilibrium-injected-cylinder` añade equilibrio reversible del receptor y márgenes al motor con tanque ventilado. Se comprueban máximo de entropía independiente, límites analíticos, refinamiento ODE simultáneo, replay completo, rollback y asignaciones. Asset v31 conserva propiedades y lee v1-v30.

Parámetros `unverified`. No se aportan propiedades medidas de gasolina, cinética multicomponente o de velocidad finita, desplazamiento líquido/pulverización resueltos ni calibración OEM. La aceptación real Unity Editor/Play/Player/IL2CPP sigue pendiente.

[Cantera: species thermodynamics](https://www.cantera.org/stable/reference/thermo/species-thermo.html) · [Cantera: equilibrium](https://www.cantera.org/stable/python/thermo.html) · [VALIDATION.es.md](VALIDATION.es.md)

## Equilibrio del tanque con volumen líquido finito

Activar `liquid_fuel_tank.parameters.headspace.phase_equilibrium` con `true` para resolver evaporación y condensación del tanque. La película asociada aporta propiedades `phase_equilibrium`; el gas seguido comparte R, cv, poder calorífico y estequiometría. Declarar temperatura inicial común y presión parcial saturada para tanque húmedo; un tanque seco comienza como máximo en saturación. La densidad total es menor que la líquida.

`evaporated_fuel_mass`, `condensed_fuel_mass` y `mass_flow` con signo cuentan transferencia neta por paso físico completo. Leer juntos `vapor_partial_pressure`, `saturation_pressure` corregida, inventario líquido y volumen gaseoso. La bomba toma líquido separado; transporte bifásico, arrastre de burbujas y NPSH local siguen abiertos.

[Equilibrio del tanque con volumen líquido finito](TANK_PHASE_EQUILIBRIUM.es.md)
