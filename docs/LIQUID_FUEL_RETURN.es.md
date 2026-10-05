# Retorno de alivio de combustible trazado

[English](LIQUID_FUEL_RETURN.md) · [简体中文](LIQUID_FUEL_RETURN.zh-CN.md) · [Français](LIQUID_FUEL_RETURN.fr.md) · [Русский](LIQUID_FUEL_RETURN.ru.md) · [日本語](LIQUID_FUEL_RETURN.ja.md) · [한국어](LIQUID_FUEL_RETURN.ko.md) · [Deutsch](LIQUID_FUEL_RETURN.de.md) · **Español** · [Italiano](LIQUID_FUEL_RETURN.it.md) · [Português](LIQUID_FUEL_RETURN.pt-BR.md)

## Contrato

`liquid_rail_return` une una alimentación con un `hydraulic_relief` unidireccional exclusivo. La válvula conecta el raíl a la misma presión de entrada prescrita de la bomba. Registrar toda ruta fluida; puertos incompatibles, propiedad duplicada y rutas no seguidas se rechazan.

`fluid_heat_fraction` elige explícitamente la fracción [0,1] de pérdida transportada por combustible retornado. El resto sigue la ruta térmica declarada. Mezcla simultánea raíl/tanque conserva masa, química, trabajo de presión y calor. El retorno a fuente externa saca masa/energía por la frontera.

Leer `total_fuel_delivered`, `reservoir_enthalpy`, `fuel_energy_in`, `fluid_heat` y `mass_flow` en retorno ID 1515. Alimentación ID 1511 informa transferencia bruta de bomba. Circulación bruta puede superar inventario inicial; inventario actual es inicial menos bombeo más retorno.

## Evidencia y límites

`recirculating-liquid-cylinder` y `recirculating-needle-cylinder` conservan combustible finito, inyección real, evaporación y aguja opcional. v28 guarda enlaces/fracción y lee v1-v27. Cada retorno añade 8 estados en los mismos límites.

Pasan decaimiento/trabajo independiente, refinamiento mecánico/presión/térmico simultáneo, fracciones, rutas, fronteras externas, replay, rollback y asignaciones. Ebullición o intervalo sin resolver falla todo el lote. Geometría/ventilación, válvulas/bombas medidas, cavitación, spray, calibración OEM y Unity Editor/Play/Player/IL2CPP real siguen abiertos.

[VALIDATION.es.md](VALIDATION.es.md)
