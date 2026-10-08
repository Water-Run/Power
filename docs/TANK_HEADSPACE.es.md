# Geometría del tanque y espacio gaseoso finito

[English](TANK_HEADSPACE.md) · [简体中文](TANK_HEADSPACE.zh-CN.md) · [Français](TANK_HEADSPACE.fr.md) · [Русский](TANK_HEADSPACE.ru.md) · [日本語](TANK_HEADSPACE.ja.md) · [한국어](TANK_HEADSPACE.ko.md) · [Deutsch](TANK_HEADSPACE.de.md) · **Español** · [Italiano](TANK_HEADSPACE.it.md) · [Português](TANK_HEADSPACE.pt-BR.md)

## Contrato

`liquid_fuel_tank.parameters.headspace` declara `capacity` en `m3` o `l` y `gas_node`. El gas omite `storage`: volumen `capacity - liquid_mass / density`, con un propietario y volumen positivo. Bomba y retorno usan presión prescrita nula: el gas finito determina la presión de entrada.

El solver acoplado intercambia trabajo de presión entre eje, riel y gas sin fuente externa. Orificios gaseosos y enlaces térmicos dan venteo/calor explícitos. Leer `pressure`, `fill_fraction`, `hydraulic_work` acumulado con signo y masa, energía, volumen del gas. Dosis, líquido, evaporación y combustión permanecen separados.

## Evidencia y límites

`vented-tank-liquid-cylinder` y `vented-tank-needle-cylinder` usan tanque 1513, gas 1520 y entrada de venteo 960. Asset v29 guarda geometría y lee v1-v28. Pasan trabajo/derivadas analíticos, convergencia ODE independiente, balances, replay portable/MCP, rollback y pasos sin asignación.

Tanque rígido mezclado, líquido incompresible y gas ideal. Oleaje/forma hidrostática, equilibrio de fases, cavitación, mapas medidos de bomba/válvula, calibración OEM y Unity Editor/Play/Player/IL2CPP real siguen abiertos. Parámetros `unverified`.

[NASA](https://www.grc.nasa.gov/www/k-12/airplane/isentrop.html) · [Cantera](https://www.cantera.org/stable/reference/reactors/interactions.html) · [VALIDATION.md](VALIDATION.es.md)
