# Raíl de combustible líquido alimentado por bomba

[English](PUMP_FED_FUEL.md) · [简体中文](PUMP_FED_FUEL.zh-CN.md) · [Français](PUMP_FED_FUEL.fr.md) · [Русский](PUMP_FED_FUEL.ru.md) · [日本語](PUMP_FED_FUEL.ja.md) · [한국어](PUMP_FED_FUEL.ko.md) · [Deutsch](PUMP_FED_FUEL.de.md) · **Español** · [Italiano](PUMP_FED_FUEL.it.md) · [Português](PUMP_FED_FUEL.pt-BR.md)

## Contrato

`liquid_rail_feed` asocia un inyector líquido con una bomba de desplazamiento existente y una frontera explícita de materia/calor. El nodo de salida hidráulica debe coincidir con la compliancia y presión absoluta inicial del raíl. Bomba e inyector poseen ese nodo; otras rutas fluidas no contabilizadas se rechazan.

La energía de presión se almacena una sola vez en el nodo hidráulico. Caudal y reacción del eje siguen la resolución acoplada conservadora. El combustible entrante aporta energía térmica y química; el almacenamiento térmico del raíl mezcla su temperatura. El flujo inverso con signo devuelve combustible a la temperatura actual del raíl. Descarga, calentamiento de pared, vapor y combustión prescrita siguen separados.

`pump-fed-liquid-cylinder` y `pump-fed-needle-cylinder` conservan inyección física y movimiento opcional de aguja. Los KPI de presión usan una cota declarada de bomba con unidades explícitas. Leer `total_fuel_delivered`, `reservoir_enthalpy` y `fuel_energy_in` en ID 1511; la bomba ID 1510 muestra trabajo real eje-fluido.

## Evidencia y límites

v26 conserva enlaces y temperatura de fuente y lee v1-v25. Intercambio analítico eje/presión, refinamiento ODE simultáneo independiente, mezcla térmica, balances masa/combustible/energía/volumen, retorno y rollback completo tienen verificaciones separadas.

La fuente es una frontera externa explícita, no un tanque finito modelado. Agotamiento, eficiencia/regulación de bomba, pérdidas de línea, cavitación, propiedades dependientes de presión y spray de volumen finito siguen abiertos. Parámetros `unverified`; no se acredita calibración OEM ni aceptación real Unity Editor/Play/Player/IL2CPP.
