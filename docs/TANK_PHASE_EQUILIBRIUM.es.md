# Equilibrio del tanque con volumen líquido finito

[English](TANK_PHASE_EQUILIBRIUM.md) · [简体中文](TANK_PHASE_EQUILIBRIUM.zh-CN.md) · [Français](TANK_PHASE_EQUILIBRIUM.fr.md) · [Русский](TANK_PHASE_EQUILIBRIUM.ru.md) · [日本語](TANK_PHASE_EQUILIBRIUM.ja.md) · [한국어](TANK_PHASE_EQUILIBRIUM.ko.md) · [Deutsch](TANK_PHASE_EQUILIBRIUM.de.md) · **Español** · [Italiano](TANK_PHASE_EQUILIBRIUM.it.md) · [Português](TANK_PHASE_EQUILIBRIUM.pt-BR.md)

## Contrato

Activar `liquid_fuel_tank.parameters.headspace.phase_equilibrium` con `true` para resolver evaporación y condensación del tanque. La película asociada aporta propiedades `phase_equilibrium`; el gas seguido comparte R, cv, poder calorífico y estequiometría. Declarar temperatura inicial común y presión parcial saturada para tanque húmedo; un tanque seco comienza como máximo en saturación. La densidad total es menor que la líquida.

`evaporated_fuel_mass`, `condensed_fuel_mass` y `mass_flow` con signo cuentan transferencia neta por paso físico completo. Leer juntos `vapor_partial_pressure`, `saturation_pressure` corregida, inventario líquido y volumen gaseoso. La bomba toma líquido separado; transporte bifásico, arrastre de burbujas y NPSH local siguen abiertos.

## Referencia termodinámica

El recipiente rígido contiene líquido incompresible y gas ideal de pseudoespecie/portador. El líquido desplaza volumen gaseoso. Su potencial químico incluye `p/rho`, por lo que se corrige la referencia `p_0(T)` de desplazamiento despreciable. `reference_pressure` mantiene ese significado y no se reinterpreta como saturación real del fluido puro de densidad finita.

[Equilibrio de fases del combustible y límites de suministro](FUEL_PHASE_EQUILIBRIUM.es.md)

```text
M_f = m_l + m_v
V_g = V_tank - m_l / rho_l
p = (M_c + m_v) R T / V_g
p_v = m_v R T / V_g
ln(p_v / p_0(T)) = p / (rho_l R T)
U = (M_c + m_v) cv T + m_l (cl T + a)
```

Un flash acotado a U,V constantes conserva combustible y energía térmica. Orificios explícitos transportan vapor, portador y energía química; enlaces térmicos intercambian con nodos finitos. Bomba/retorno intercambian trabajo de desplazamiento con el gas y luego el equilibrio redistribuye energía. `hydraulic_work` cuenta desplazamiento de bomba, excluyendo trabajo interno de fase.

## Evidencia y límites

`equilibrium-tank-supply` prueba inversión de bomba, venteo con combustible y pared finita caliente. `equilibrium-tank-fired-cylinder` conecta esa fuente a un motor encendido. Se verifican máximo independiente de entropía, potenciales, derivadas calóricas, refinamiento ODE, balances, replay, rollback y asignaciones. Asset v37 conserva el indicador y lee v1-v36.

Casos acoplados de bomba, calor y venteo demuestran primer orden. Ambas fases fuente permanecen en el intervalo declarado; la densidad total queda bajo la líquida con margen binary64. Densidad/propiedades constantes, equilibrio instantáneo e interfaz plana son aproximaciones de investigación. Mezclas reales, tensión superficial, oleaje, cinética finita, calibración medida y Unity Editor/Play/Player/IL2CPP real siguen sin verificar.

[Cantera: constant-volume species](https://cantera.org/stable/cxx/da/d94/PDSS__ConstVol_8cpp_source.html) · [IUPAC: Poynting correction](https://goldbook.iupac.org/terms/view/15347) · [VALIDATION.es.md](VALIDATION.es.md)
