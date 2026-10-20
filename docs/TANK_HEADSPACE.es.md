# Geometría del tanque y espacio gaseoso finito

[English](TANK_HEADSPACE.md) · [简体中文](TANK_HEADSPACE.zh-CN.md) · [Français](TANK_HEADSPACE.fr.md) · [Русский](TANK_HEADSPACE.ru.md) · [日本語](TANK_HEADSPACE.ja.md) · [한국어](TANK_HEADSPACE.ko.md) · [Deutsch](TANK_HEADSPACE.de.md) · **Español** · [Italiano](TANK_HEADSPACE.it.md) · [Português](TANK_HEADSPACE.pt-BR.md)

## Contrato

`liquid_fuel_tank.parameters.headspace` declara `capacity` en `m3` o `l` y `gas_node`. El gas omite `storage`: volumen `capacity - liquid_mass / density`, con un propietario y volumen positivo. Bomba y retorno usan presión prescrita nula: el gas finito determina la presión de entrada.

El solver acoplado intercambia trabajo de presión entre eje, riel y gas sin fuente externa. Orificios gaseosos y enlaces térmicos dan venteo/calor explícitos. Leer `pressure`, `fill_fraction`, `hydraulic_work` acumulado con signo y masa, energía, volumen del gas. Dosis, líquido, evaporación y combustión permanecen separados.

## Evidencia y límites

`vented-tank-liquid-cylinder` y `vented-tank-needle-cylinder` usan tanque 1513, gas 1520 y entrada de venteo 960. Asset v31 guarda geometría y lee v1-v30. Pasan trabajo/derivadas analíticos, convergencia ODE independiente, balances, replay portable/MCP, rollback y pasos sin asignación.

Tanque rígido mezclado, líquido incompresible y gas ideal. Oleaje/forma hidrostática, termodinámica de mezclas reales, cavitación, mapas medidos de bomba/válvula, calibración OEM y Unity Editor/Play/Player/IL2CPP real siguen abiertos. Parámetros `unverified`.

[NASA](https://www.grc.nasa.gov/www/k-12/airplane/isentrop.html) · [Cantera](https://www.cantera.org/stable/reference/reactors/interactions.html) · [VALIDATION.md](VALIDATION.es.md)

## Equilibrio del tanque con volumen líquido finito

Activar `liquid_fuel_tank.parameters.headspace.phase_equilibrium` con `true` para resolver evaporación y condensación del tanque. La película asociada aporta propiedades `phase_equilibrium`; el gas seguido comparte R, cv, poder calorífico y estequiometría. Declarar temperatura inicial común y presión parcial saturada para tanque húmedo; un tanque seco comienza como máximo en saturación. La densidad total es menor que la líquida.

`evaporated_fuel_mass`, `condensed_fuel_mass` y `mass_flow` con signo cuentan transferencia neta por paso físico completo. Leer juntos `vapor_partial_pressure`, `saturation_pressure` corregida, inventario líquido y volumen gaseoso. La bomba toma líquido separado; transporte bifásico, arrastre de burbujas y NPSH local siguen abiertos.

[Equilibrio del tanque con volumen líquido finito](TANK_PHASE_EQUILIBRIUM.es.md)
