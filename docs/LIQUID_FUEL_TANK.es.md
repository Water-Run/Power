# Tanque finito de combustible líquido

[English](LIQUID_FUEL_TANK.md) · [简体中文](LIQUID_FUEL_TANK.zh-CN.md) · [Français](LIQUID_FUEL_TANK.fr.md) · [Русский](LIQUID_FUEL_TANK.ru.md) · [日本語](LIQUID_FUEL_TANK.ja.md) · [한국어](LIQUID_FUEL_TANK.ko.md) · [Deutsch](LIQUID_FUEL_TANK.de.md) · **Español** · [Italiano](LIQUID_FUEL_TANK.it.md) · [Português](LIQUID_FUEL_TANK.pt-BR.md)

## Contrato

`liquid_fuel_tank` guarda masa líquida finita y energía térmica con densidad, referencia térmica de película y poder calorífico del inyector asociado. La alimentación lo elige con `tank_component` y omite `supply_temperature`. Cada tanque pertenece a una alimentación compatible.

El caudal positivo se limita por el inventario restante durante el intervalo aceptado. El mismo desplazamiento efectivo lleno fija reacción del eje y transferencia de presión, conservando trabajo eje/fluido. Girar adelante vacío no entrega líquido ni trabajo fluido; el retorno con signo mezcla energía térmica actual del raíl en el tanque.

Energías térmica y química del tanque forman el almacenamiento completo. La transferencia interna no agrega masa ni suministro químico externos. La presión de entrada prescrita mantiene su frontera de trabajo de presión. Admisión/escape gaseoso aún pueden llevar energía química.

`finite-tank-liquid-cylinder` y `finite-tank-needle-cylinder` usan tanque ID 1513 y alimentación ID 1511. Leer `mass`, `temperature`, `internal_energy`, `chemical_energy` y `tank_state`; 0 significa líquido y 1 vacío. La temperatura seca informa la referencia inicial declarada.

## Evidencia y límites

v27 conserva tanque y selección y lee v1-v26. Cada tanque añade 4 estados dentro de las mismas cotas. Se verifican intercambio húmedo independiente, presión/energía de eje agotadas analíticas, mezcla de retorno, balances completos, rollback, ramas y pasos sin asignación.

Capacidad geométrica, ventilación/espacio gaseoso/oleaje, cavitación, llenado/eficiencia/regulación medidos y spray resuelto siguen abiertos. Parámetros `unverified`; Unity Editor/Play/Player/IL2CPP real y calibración OEM siguen sin verificar.

[VALIDATION.es.md](VALIDATION.es.md)
