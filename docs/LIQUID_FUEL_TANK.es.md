# Tanque finito de combustible líquido

[English](LIQUID_FUEL_TANK.md) · [简体中文](LIQUID_FUEL_TANK.zh-CN.md) · [Français](LIQUID_FUEL_TANK.fr.md) · [Русский](LIQUID_FUEL_TANK.ru.md) · [日本語](LIQUID_FUEL_TANK.ja.md) · [한국어](LIQUID_FUEL_TANK.ko.md) · [Deutsch](LIQUID_FUEL_TANK.de.md) · **Español** · [Italiano](LIQUID_FUEL_TANK.it.md) · [Português](LIQUID_FUEL_TANK.pt-BR.md)

## Contrato

`liquid_fuel_tank` guarda masa líquida finita y energía térmica con densidad, referencia térmica de película y poder calorífico del inyector asociado. La alimentación lo elige con `tank_component` y omite `supply_temperature`. Cada tanque pertenece a una alimentación compatible.

El caudal positivo se limita por el inventario restante durante el intervalo aceptado. El mismo desplazamiento efectivo lleno fija reacción del eje y transferencia de presión, conservando trabajo eje/fluido. Girar adelante vacío no entrega líquido ni trabajo fluido; el retorno con signo mezcla energía térmica actual del raíl en el tanque.

Energías térmica y química del tanque forman el almacenamiento completo. La transferencia interna no agrega masa ni suministro químico externos. La presión de entrada prescrita mantiene su frontera de trabajo de presión. Admisión/escape gaseoso aún pueden llevar energía química.

`finite-tank-liquid-cylinder` y `finite-tank-needle-cylinder` usan tanque ID 1513 y alimentación ID 1511. Leer `mass`, `temperature`, `internal_energy`, `chemical_energy` y `tank_state`; 0 significa líquido y 1 vacío. La temperatura seca informa la referencia inicial declarada.

## Geometría del tanque y espacio gaseoso finito

`liquid_fuel_tank.parameters.headspace` declara `capacity` en `m3` o `l` y `gas_node`. El gas omite `storage`: volumen `capacity - liquid_mass / density`, con un propietario y volumen positivo. Bomba y retorno usan presión prescrita nula: el gas finito determina la presión de entrada.

[Geometría del tanque y espacio gaseoso finito](TANK_HEADSPACE.es.md)

## Evidencia y límites

v33 conserva tanque y selección y lee v1-v32. Cada tanque añade 4 estados dentro de las mismas cotas. Se verifican intercambio húmedo independiente, presión/energía de eje agotadas analíticas, mezcla de retorno, balances completos, rollback, ramas y pasos sin asignación.

Tanque rígido mezclado, líquido incompresible y gas ideal. Oleaje/forma hidrostática, termodinámica de mezclas reales, cavitación, mapas medidos de bomba/válvula, calibración OEM y Unity Editor/Play/Player/IL2CPP real siguen abiertos. Parámetros `unverified`.

[VALIDATION.es.md](VALIDATION.es.md)

## Retorno de alivio de combustible trazado

`liquid_rail_return` une una alimentación con un `hydraulic_relief` unidireccional exclusivo. La válvula conecta el raíl a la misma presión de entrada prescrita de la bomba. Registrar toda ruta fluida; puertos incompatibles, propiedad duplicada y rutas no seguidas se rechazan.

`fluid_heat_fraction` elige explícitamente la fracción [0,1] de pérdida transportada por combustible retornado. El resto sigue la ruta térmica declarada. Mezcla simultánea raíl/tanque conserva masa, química, trabajo de presión y calor. El retorno a fuente externa saca masa/energía por la frontera.

[LIQUID_FUEL_RETURN.es.md](LIQUID_FUEL_RETURN.es.md)

## Equilibrio de fases del combustible y límites de suministro

El suministro predeterminado rechaza líquido bajo saturación. La opción de tanque finito resuelve vapor fuente con potenciales corregidos por presión. Riel y aspiración aún llevan líquido separado; burbujas y flujo cavitante requieren más física.

La comparación solo admite redondeo binary64: `64 * epsilon * max(p, p_sat)`, con `epsilon = 2^-52`. El margen informado no se recorta. Es una comprobación de saturación global; pérdidas locales y NPSH de bomba requieren un modelo de flujo resuelto. Las comprobaciones independientes también exigen entropía total no decreciente en el flash adiabático y el intercambio con la pared finita.

[Equilibrio de fases del combustible y límites de suministro](FUEL_PHASE_EQUILIBRIUM.es.md)

## Equilibrio del tanque con volumen líquido finito

Activar `liquid_fuel_tank.parameters.headspace.phase_equilibrium` con `true` para resolver evaporación y condensación del tanque. La película asociada aporta propiedades `phase_equilibrium`; el gas seguido comparte R, cv, poder calorífico y estequiometría. Declarar temperatura inicial común y presión parcial saturada para tanque húmedo; un tanque seco comienza como máximo en saturación. La densidad total es menor que la líquida.

`evaporated_fuel_mass`, `condensed_fuel_mass` y `mass_flow` con signo cuentan transferencia neta por paso físico completo. Leer juntos `vapor_partial_pressure`, `saturation_pressure` corregida, inventario líquido y volumen gaseoso. La bomba toma líquido separado; transporte bifásico, arrastre de burbujas y NPSH local siguen abiertos.

[Equilibrio del tanque con volumen líquido finito](TANK_PHASE_EQUILIBRIUM.es.md)
