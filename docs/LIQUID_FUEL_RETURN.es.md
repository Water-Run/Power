# Retorno de alivio de combustible trazado

[English](LIQUID_FUEL_RETURN.md) · [简体中文](LIQUID_FUEL_RETURN.zh-CN.md) · [Français](LIQUID_FUEL_RETURN.fr.md) · [Русский](LIQUID_FUEL_RETURN.ru.md) · [日本語](LIQUID_FUEL_RETURN.ja.md) · [한국어](LIQUID_FUEL_RETURN.ko.md) · [Deutsch](LIQUID_FUEL_RETURN.de.md) · **Español** · [Italiano](LIQUID_FUEL_RETURN.it.md) · [Português](LIQUID_FUEL_RETURN.pt-BR.md)

## Contrato

`liquid_rail_return` une una alimentación con un `hydraulic_relief` unidireccional exclusivo. La válvula conecta el raíl a la misma presión de entrada prescrita de la bomba. Registrar toda ruta fluida; puertos incompatibles, propiedad duplicada y rutas no seguidas se rechazan.

`fluid_heat_fraction` elige explícitamente la fracción [0,1] de pérdida transportada por combustible retornado. El resto sigue la ruta térmica declarada. Mezcla simultánea raíl/tanque conserva masa, química, trabajo de presión y calor. El retorno a fuente externa saca masa/energía por la frontera.

Leer `total_fuel_delivered`, `reservoir_enthalpy`, `fuel_energy_in`, `fluid_heat` y `mass_flow` en retorno ID 1515. Alimentación ID 1511 informa transferencia bruta de bomba. Circulación bruta puede superar inventario inicial; inventario actual es inicial menos bombeo más retorno.

## Evidencia y límites

`recirculating-liquid-cylinder` y `recirculating-needle-cylinder` conservan combustible finito, inyección real, evaporación y aguja opcional. v30 guarda enlaces/fracción y lee v1-v29. Cada retorno añade 8 estados en los mismos límites.

Pasan decaimiento/trabajo independiente, refinamiento mecánico/presión/térmico simultáneo, fracciones, rutas, fronteras externas, replay, rollback y asignaciones. Ebullición o intervalo sin resolver falla todo el lote.

Tanque rígido mezclado, líquido incompresible y gas ideal. Oleaje/forma hidrostática, equilibrio de fases, cavitación, mapas medidos de bomba/válvula, calibración OEM y Unity Editor/Play/Player/IL2CPP real siguen abiertos. Parámetros `unverified`.

[VALIDATION.es.md](VALIDATION.es.md)

## Geometría del tanque y espacio gaseoso finito

`liquid_fuel_tank.parameters.headspace` declara `capacity` en `m3` o `l` y `gas_node`. El gas omite `storage`: volumen `capacity - liquid_mass / density`, con un propietario y volumen positivo. Bomba y retorno usan presión prescrita nula: el gas finito determina la presión de entrada.

[Geometría del tanque y espacio gaseoso finito](TANK_HEADSPACE.es.md)

## Equilibrio de fases del combustible y límites de suministro

Rieles, tanques finitos y entradas exponen `saturation_pressure` y `cavitation_margin` en Pa. El margen es presión absoluta local menos saturación a la temperatura líquida. Un margen negativo con líquido rechaza todo el lote. Esto limita el suministro monofásico; transferencia de vapor del tanque, burbujas y flujo cavitante de bomba siguen abiertos.

La comparación solo admite redondeo binary64: `64 * epsilon * max(p, p_sat)`, con `epsilon = 2^-52`. El margen informado no se recorta. Es una comprobación de saturación global; pérdidas locales y NPSH de bomba requieren un modelo de flujo resuelto. Las comprobaciones independientes también exigen entropía total no decreciente en el flash adiabático y el intercambio con la pared finita.

[Equilibrio de fases del combustible y límites de suministro](FUEL_PHASE_EQUILIBRIUM.es.md)
