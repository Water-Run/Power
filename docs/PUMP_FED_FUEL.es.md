# Raíl de combustible líquido alimentado por bomba

[English](PUMP_FED_FUEL.md) · [简体中文](PUMP_FED_FUEL.zh-CN.md) · [Français](PUMP_FED_FUEL.fr.md) · [Русский](PUMP_FED_FUEL.ru.md) · [日本語](PUMP_FED_FUEL.ja.md) · [한국어](PUMP_FED_FUEL.ko.md) · [Deutsch](PUMP_FED_FUEL.de.md) · **Español** · [Italiano](PUMP_FED_FUEL.it.md) · [Português](PUMP_FED_FUEL.pt-BR.md)

## Contrato

`liquid_rail_feed` asocia un inyector líquido con una bomba de desplazamiento existente y una frontera explícita de materia/calor. El nodo de salida hidráulica debe coincidir con la compliancia y presión absoluta inicial del raíl. Bomba e inyector poseen ese nodo; otras rutas fluidas no contabilizadas se rechazan.

La energía de presión se almacena una sola vez en el nodo hidráulico. Caudal y reacción del eje siguen la resolución acoplada conservadora. El combustible entrante aporta energía térmica y química; el almacenamiento térmico del raíl mezcla su temperatura. El flujo inverso con signo devuelve combustible a la temperatura actual del raíl. Descarga, calentamiento de pared, vapor y combustión prescrita siguen separados.

`pump-fed-liquid-cylinder` y `pump-fed-needle-cylinder` conservan inyección física y movimiento opcional de aguja. Los KPI de presión usan una cota declarada de bomba con unidades explícitas. Leer `total_fuel_delivered`, `reservoir_enthalpy` y `fuel_energy_in` en ID 1511; la bomba ID 1510 muestra trabajo real eje-fluido.

## Evidencia y límites

v37 conserva enlaces y temperatura de fuente y lee v1-v36. Intercambio analítico eje/presión, refinamiento ODE simultáneo independiente, mezcla térmica, balances masa/combustible/energía/volumen, retorno y rollback completo tienen verificaciones separadas.

Tanque rígido mezclado, líquido incompresible y gas ideal. Oleaje/forma hidrostática, termodinámica de mezclas reales, cavitación, mapas medidos de bomba/válvula, calibración OEM y Unity Editor/Play/Player/IL2CPP real siguen abiertos. Parámetros `unverified`.

## Tanque finito de combustible líquido

`liquid_fuel_tank` guarda masa líquida finita y energía térmica con densidad, referencia térmica de película y poder calorífico del inyector asociado. La alimentación lo elige con `tank_component` y omite `supply_temperature`. Cada tanque pertenece a una alimentación compatible.

Energías térmica y química del tanque forman el almacenamiento completo. La transferencia interna no agrega masa ni suministro químico externos. La presión de entrada prescrita mantiene su frontera de trabajo de presión. Admisión/escape gaseoso aún pueden llevar energía química.

Tanque rígido mezclado, líquido incompresible y gas ideal. Oleaje/forma hidrostática, termodinámica de mezclas reales, cavitación, mapas medidos de bomba/válvula, calibración OEM y Unity Editor/Play/Player/IL2CPP real siguen abiertos. Parámetros `unverified`.

[LIQUID_FUEL_TANK.es.md](LIQUID_FUEL_TANK.es.md)

## Retorno de alivio de combustible trazado

`liquid_rail_return` une una alimentación con un `hydraulic_relief` unidireccional exclusivo. La válvula conecta el raíl a la misma presión de entrada prescrita de la bomba. Registrar toda ruta fluida; puertos incompatibles, propiedad duplicada y rutas no seguidas se rechazan.

`fluid_heat_fraction` elige explícitamente la fracción [0,1] de pérdida transportada por combustible retornado. El resto sigue la ruta térmica declarada. Mezcla simultánea raíl/tanque conserva masa, química, trabajo de presión y calor. El retorno a fuente externa saca masa/energía por la frontera.

[LIQUID_FUEL_RETURN.es.md](LIQUID_FUEL_RETURN.es.md)

## Geometría del tanque y espacio gaseoso finito

`liquid_fuel_tank.parameters.headspace` declara `capacity` en `m3` o `l` y `gas_node`. El gas omite `storage`: volumen `capacity - liquid_mass / density`, con un propietario y volumen positivo. Bomba y retorno usan presión prescrita nula: el gas finito determina la presión de entrada.

El solver acoplado intercambia trabajo de presión entre eje, riel y gas sin fuente externa. Orificios gaseosos y enlaces térmicos dan venteo/calor explícitos. Leer `pressure`, `fill_fraction`, `hydraulic_work` acumulado con signo y masa, energía, volumen del gas. Dosis, líquido, evaporación y combustión permanecen separados.

[Geometría del tanque y espacio gaseoso finito](TANK_HEADSPACE.es.md)

## Equilibrio de fases del combustible y límites de suministro

El suministro predeterminado rechaza líquido bajo saturación. La opción de tanque finito resuelve vapor fuente con potenciales corregidos por presión. Riel y aspiración aún llevan líquido separado; burbujas y flujo cavitante requieren más física.

La comparación solo admite redondeo binary64: `64 * epsilon * max(p, p_sat)`, con `epsilon = 2^-52`. El margen informado no se recorta. Es una comprobación de saturación global; pérdidas locales y NPSH de bomba requieren un modelo de flujo resuelto. Las comprobaciones independientes también exigen entropía total no decreciente en el flash adiabático y el intercambio con la pared finita.

[Equilibrio de fases del combustible y límites de suministro](FUEL_PHASE_EQUILIBRIUM.es.md)

## Equilibrio del tanque con volumen líquido finito

Activar `liquid_fuel_tank.parameters.headspace.phase_equilibrium` con `true` para resolver evaporación y condensación del tanque. La película asociada aporta propiedades `phase_equilibrium`; el gas seguido comparte R, cv, poder calorífico y estequiometría. Declarar temperatura inicial común y presión parcial saturada para tanque húmedo; un tanque seco comienza como máximo en saturación. La densidad total es menor que la líquida.

`evaporated_fuel_mass`, `condensed_fuel_mass` y `mass_flow` con signo cuentan transferencia neta por paso físico completo. Leer juntos `vapor_partial_pressure`, `saturation_pressure` corregida, inventario líquido y volumen gaseoso. La bomba toma líquido separado; transporte bifásico, arrastre de burbujas y NPSH local siguen abiertos.

[Equilibrio del tanque con volumen líquido finito](TANK_PHASE_EQUILIBRIUM.es.md)

El transporte se combina ahora con `fuel_chamber` por eje y `fuel_check_valve` ideal. Inyector, inercia/flujo crítico y mezclas reales medidas siguen separados. Los parámetros son `unverified`. [Bomba de combustible con cambio de fase impulsada por eje](FUEL_CHAMBER_PUMP.es.md)
