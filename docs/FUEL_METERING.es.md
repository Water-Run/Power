# Raíl de combustible gaseoso finito y dosificación por ciclo

[English](FUEL_METERING.md) · [简体中文](FUEL_METERING.zh-CN.md) · [Français](FUEL_METERING.fr.md) · [Русский](FUEL_METERING.ru.md) · [日本語](FUEL_METERING.ja.md) · [한국어](FUEL_METERING.ko.md) · [Deutsch](FUEL_METERING.de.md) · **Español** · [Italiano](FUEL_METERING.it.md) · [Português](FUEL_METERING.pt-BR.md)

`gas_fuel_injector` transfiere combustible desde un raíl de gas finito y rastreado hasta una cámara
de gas compatible. Una ventana de cigüeñal hacia adelante enclava una masa de combustible solicitada por ciclo;
la presión, la temperatura, el área de tobera y el inventario disponible del raíl determinan la entrega
real. El controlador estrangula el puerto cerca de su cuota. No añade combustible
directamente al estado receptor ni supone que una dosis solicitada se haya entregado.

Esto extiende el modelo actual de mezcla gaseosa de propiedades constantes. La admisión de aire,
la dosificación de combustible, la mezcla y la reacción prescrita pueden modelarse ahora por separado. No
implementa pulverización de gasolina líquida, evaporación, dinámica de aguja o eléctrica, física de
raíl o depósito líquidos, propiedades detalladas de especies ni un comportamiento de inyección o de ECU OEM calibrado.
Siguen siendo trabajo necesario hacia el objetivo del motor completo.

## Flujo, constituyentes y energía

La fuente y el receptor son nodos de gas premmezclado finitos y distintos. Ambos comparten R, gamma,
poder calorífico y relación estequiométrica. La fuente puede ser combustible gaseoso puro o una
mezcla rastreada que contenga combustible. El flujo sigue la ley existente de [orificio de gas](GAS_NETWORK.es.md),
dependiente de la presión, bloqueada y subcrítica, con área y coeficiente de
descarga explícitos. El flujo por presión inversa está cerrado: el gas del receptor no rellena el raíl.

La cuota se refiere a la **masa de combustible**, no a la masa total de la mezcla de la fuente. Cada avance de gas
limita la tasa de combustible por `remaining_cycle_fuel / advance_duration`. El mismo factor de flujo
escala la masa total y la entalpía térmica aguas arriba; las fracciones de constituyente usan el estado
aguas arriba real. Las etapas de Heun y el historial de entrega aceptado usan las mismas transferencias.
En consecuencia, el combustible receptor, el vaciado del raíl, la energía térmica y el inventario
químico siguen siendo coherentes aunque caiga la presión del raíl o cambie la composición de la fuente.

La transferencia interna de combustible no entra en `fuel_energy_in` externo ni en la entalpía
del depósito. La energía química almacenada de la fuente viaja con el combustible y solo se convierte en calor
del gas cuando el componente de quemado aparte lo consume. La química incompatible o los
parámetros calóricos del gas se rechazan en la compilación. Limitar la dosis cambia el flujo
admitido, en lugar de corregir la masa o la energía después de integrar.

## Contrato de ciclo y de comando

| Dato | Significado |
|---|---|
| Fuente A / receptor B | Volúmenes de gas finitos, rastreados y distintos |
| `area`, `discharge_coefficient` | m2/mm2 positivos y coeficiente en (0,1] |
| `crank_node` | Referencia rotacional de temporización explícita; una cámara de cigüeñal móvil usa su propio cigüeñal |
| `cycle_angle` | 360 o 720 grados, con unidades de ángulo explícitas |
| `start_angle`, `duration_angle` | Inicio de la ventana y duración positiva no mayor que un ciclo |
| `maximum_dose` | Límite positivo de combustible por ciclo, en kg |
| Entrada `fuel_dose_per_cycle` | kg solicitados en [0,maximum_dose] |

La ventana rectangular ideal solo se abre durante el movimiento hacia adelante. El primer avance aceptado
con la ventana abierta muestrea la dosis solicitada. Las escrituras durante ese ciclo observado
se aplican a la ventana siguiente; el canal del ciclo solicitado sigue mostrando el
objetivo enclavado. Cero desactiva ese ciclo. Si la presión o el combustible disponibles no bastan, la entrega
real permanece por debajo del objetivo. Un paso con éxito no implica una dosis completa.

Los ordinales de ciclo son enteros con signo y acotados, reconstruidos a partir de ángulos de cigüeñal representables. Volver a un ciclo observado anterior no puede restablecer su cuota; invertir
el movimiento cierra la ventana. El recorrido por intervalo mecánico se limita a
`min(0.25 rad,duration/8)`. Los extremos de la ventana usan la aproximación existente de tick fijo y reparto
simétrico, así que la temporización cerca de las discontinuidades exige refinar el paso de tiempo.
No se afirma un instante exacto de conmutación continua.

Las salidas incluyen la apertura actual de la ventana de dosificación, la tasa media de combustible entregado en el último tick,
la dosis solicitada enclavada, la dosis entregada en el ciclo observado y el combustible entregado
acumulado. La presión, la temperatura y el combustible restante del raíl finito son canales de gas ordinarios.
Todos los historiales de cuota, ordinal y entrega, incluida la compensación, se copian y se hashean con
el estado especulativo. La cancelación, el fallo tardío y las bifurcaciones conservan el estado completo.

El asset v17 conserva los registros de tobera y de temporización. JSON, CLI y MCP comparten el mismo
modelo; el esquema y el compilador comprueban unidades, límites, extremos finitos y compatibles y
la propiedad de la temporización. Los destinos de Core siguen sin dependencias en net10.0/netstandard2.1.

## Experimento y evidencia

`metered-fired-cylinder` sustituye la admisión de combustible premmezclado por una admisión solo de aire y un
raíl gaseoso finito. Un inyector ideal suministra solicitudes explícitas de 8/12/4 mg a través de
una ventana de cigüeñal antes del quemado de Wiebe prescrito. Los cambios de dosis se muestrean en la
ventana observada siguiente. El experimento de seis décimas de segundo entrega 28 mg, quema unos
27.930 mg y libera unos 1228.918 J; el combustible no quemado y el perdido en la frontera permanecen en
la cuenta de constituyentes. Todos los parámetros son sintéticos y no verificados.

Las comprobaciones cubren la entrega exacta limitada por la cuota, el agotamiento del raíl finito, la presión inversa,
el enclavamiento de un comando a mitad de ventana, la inversión sin reemitir la cuota, una EDO independiente de masa y entalpía
de dos recipientes con refinamiento suave, el quemado dosificado analítico, las transacciones completas,
la capacidad de estado, las unidades y la química, y el avance sin asignaciones. Los informes, el asset portátil y MCP coinciden
en cada límite. Los canales separados de entrega y de quemado distinguen un comando
aceptado del combustible y del calor reales. Consulta [VALIDATION.es.md](VALIDATION.es.md) para los límites
medidos y la evidencia de runtime y de rendimiento. Las vistas de Studio y las pruebas de Edit/Play están preparadas
en fuente C# 9; la aceptación real del Editor de Unity y del Player sigue pendiente.
