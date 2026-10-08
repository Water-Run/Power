# Raíl de combustible líquido alimentado por bomba

[English](PUMP_FED_FUEL.md) · [简体中文](PUMP_FED_FUEL.zh-CN.md) · [Français](PUMP_FED_FUEL.fr.md) · [Русский](PUMP_FED_FUEL.ru.md) · [日本語](PUMP_FED_FUEL.ja.md) · [한국어](PUMP_FED_FUEL.ko.md) · [Deutsch](PUMP_FED_FUEL.de.md) · **Español** · [Italiano](PUMP_FED_FUEL.it.md) · [Português](PUMP_FED_FUEL.pt-BR.md)

## Contrato

`liquid_rail_feed` asocia un inyector líquido con una bomba de desplazamiento existente y una frontera explícita de materia/calor. El nodo de salida hidráulica debe coincidir con la compliancia y presión absoluta inicial del raíl. Bomba e inyector poseen ese nodo; otras rutas fluidas no contabilizadas se rechazan.

La energía de presión se almacena una sola vez en el nodo hidráulico. Caudal y reacción del eje siguen la resolución acoplada conservadora. El combustible entrante aporta energía térmica y química; el almacenamiento térmico del raíl mezcla su temperatura. El flujo inverso con signo devuelve combustible a la temperatura actual del raíl. Descarga, calentamiento de pared, vapor y combustión prescrita siguen separados.

`pump-fed-liquid-cylinder` y `pump-fed-needle-cylinder` conservan inyección física y movimiento opcional de aguja. Los KPI de presión usan una cota declarada de bomba con unidades explícitas. Leer `total_fuel_delivered`, `reservoir_enthalpy` y `fuel_energy_in` en ID 1511; la bomba ID 1510 muestra trabajo real eje-fluido.

## Evidencia y límites

v29 conserva enlaces y temperatura de fuente y lee v1-v28. Intercambio analítico eje/presión, refinamiento ODE simultáneo independiente, mezcla térmica, balances masa/combustible/energía/volumen, retorno y rollback completo tienen verificaciones separadas.

Tanque rígido mezclado, líquido incompresible y gas ideal. Oleaje/forma hidrostática, equilibrio de fases, cavitación, mapas medidos de bomba/válvula, calibración OEM y Unity Editor/Play/Player/IL2CPP real siguen abiertos. Parámetros `unverified`.

## Tanque finito de combustible líquido

`liquid_fuel_tank` guarda masa líquida finita y energía térmica con densidad, referencia térmica de película y poder calorífico del inyector asociado. La alimentación lo elige con `tank_component` y omite `supply_temperature`. Cada tanque pertenece a una alimentación compatible.

Energías térmica y química del tanque forman el almacenamiento completo. La transferencia interna no agrega masa ni suministro químico externos. La presión de entrada prescrita mantiene su frontera de trabajo de presión. Admisión/escape gaseoso aún pueden llevar energía química.

Tanque rígido mezclado, líquido incompresible y gas ideal. Oleaje/forma hidrostática, equilibrio de fases, cavitación, mapas medidos de bomba/válvula, calibración OEM y Unity Editor/Play/Player/IL2CPP real siguen abiertos. Parámetros `unverified`.

[LIQUID_FUEL_TANK.es.md](LIQUID_FUEL_TANK.es.md)

## Retorno de alivio de combustible trazado

`liquid_rail_return` une una alimentación con un `hydraulic_relief` unidireccional exclusivo. La válvula conecta el raíl a la misma presión de entrada prescrita de la bomba. Registrar toda ruta fluida; puertos incompatibles, propiedad duplicada y rutas no seguidas se rechazan.

`fluid_heat_fraction` elige explícitamente la fracción [0,1] de pérdida transportada por combustible retornado. El resto sigue la ruta térmica declarada. Mezcla simultánea raíl/tanque conserva masa, química, trabajo de presión y calor. El retorno a fuente externa saca masa/energía por la frontera.

[LIQUID_FUEL_RETURN.es.md](LIQUID_FUEL_RETURN.es.md)

## Geometría del tanque y espacio gaseoso finito

`liquid_fuel_tank.parameters.headspace` declara `capacity` en `m3` o `l` y `gas_node`. El gas omite `storage`: volumen `capacity - liquid_mass / density`, con un propietario y volumen positivo. Bomba y retorno usan presión prescrita nula: el gas finito determina la presión de entrada.

El solver acoplado intercambia trabajo de presión entre eje, riel y gas sin fuente externa. Orificios gaseosos y enlaces térmicos dan venteo/calor explícitos. Leer `pressure`, `fill_fraction`, `hydraulic_work` acumulado con signo y masa, energía, volumen del gas. Dosis, líquido, evaporación y combustión permanecen separados.

[Geometría del tanque y espacio gaseoso finito](TANK_HEADSPACE.es.md)
