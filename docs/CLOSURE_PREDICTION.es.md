# Predicción acotada del cierre de aguja y corte en la rejilla de ticks

[English](CLOSURE_PREDICTION.md) · [简体中文](CLOSURE_PREDICTION.zh-CN.md) · [Français](CLOSURE_PREDICTION.fr.md) · [Русский](CLOSURE_PREDICTION.ru.md) · [日本語](CLOSURE_PREDICTION.ja.md) · [한국어](CLOSURE_PREDICTION.ko.md) · [Deutsch](CLOSURE_PREDICTION.de.md) · **Español** · [Italiano](CLOSURE_PREDICTION.it.md) · [Português](CLOSURE_PREDICTION.pt-BR.md)

El [accionamiento físico de la aguja](NEEDLE_ACTUATION.es.md) puede compensar el combustible entregado
después de que termine su comando de voltaje. El `closure_prediction_ns` opcional activa una repetición
preasignada de la planta completa. La elevación real de la aguja, el decaimiento de la corriente, el trabajo de presión,
el rebote en el asiento y el inventario de combustible siguen siendo físicos; la predicción cambia la temporización
del comando, en lugar de recortar la masa entregada real.

## Contrato de predicción

En una muestra que vence, el predictor copia el estado actual completo. Repite los
ticks físicos configurados con su bobina a voltaje cero, o con un periodo acotado
de voltaje de accionamiento antes del corte. Todos los demás comandos de actuador se mantienen. Los controladores
muestreados no se llaman de forma recursiva ni cambian comandos dentro de este pronóstico, y los eventos
de entrada externos futuros no se anticipan. El intercambio de gas, el comportamiento del cigüeñal y del cilindro,
la combustión, el suministro hidráulico y eléctrico, los contactos y los intervalos de embrague aceptados
continúan por las ecuaciones normales de la planta.

La masa adicional pronosticada es el aumento de la entrega total de ese inyector.
El estado auxiliar no se confirma en la simulación real. Cada candidato empieza
desde el mismo estado fuente completo; el tiempo real, la memoria del controlador y los historiales
físicos permanecen intactos. El espacio de trabajo del solver que pertenece a la simulación se prepara de nuevo
para el intervalo real. `PredictNeedleClosure(driver_id, out estimate)` expone una
predicción de solo lectura a voltaje cero para los clientes de Core, con cancelación y estado.

El horizonte es un múltiplo entero de ticks físicos, cubre al menos dos periodos de
muestreo del controlador y se limita a **4096 ticks físicos**. Cero conserva el comportamiento previo
del controlador todo o nada. La predicción no puede dar la vuelta al reloj entero acotado. Los pronósticos
fallidos o cancelados rechazan el lote real entero; un pronóstico parcial no se
trata en silencio como una estimación válida.

## Decisión de cierre programada

El controlador compara la entrega del ciclo actual más el combustible de cierre pronosticado con la
solicitud enclavada. Si el cierre a voltaje cero ya alcanza el objetivo, corta ahora.
Si no, también pronostica mantener el accionamiento hasta la muestra siguiente. Si esos dos
candidatos encierran el objetivo, una bisección entera acotada encuentra candidatos de corte
en ticks físicos vecinos y elige la masa final proyectada más próxima.

El plazo elegido es una cuenta atrás de ticks físicos. Puede retirar el voltaje
antes de la muestra siguiente del controlador. La decisión de corte se enclava para el ciclo
observado, y así se evita reabrir de forma repetida por diferencias de predicción minúsculas. Un ciclo observado
nuevo restablece ese enclavamiento. El apagado por ventana o por inversión puede cancelar un plazo pendiente.
El combustible real sigue gobernado por la aguja en movimiento durante todo el cierre y el rebote.

El intervalo local de candidatos debe ser monótono dentro de la tolerancia numérica
declarada. Un intervalo violado devuelve fallo numérico con el estado del modelo y de la sesión
sin cambios; inspecciona el voltaje, la mecánica, el muestreo y las hipótesis de predicción, en lugar
de aceptar un corte no válido. Cada candidato se limita a 4096 ticks y la
bisección entera tiene como máximo doce consultas interiores más los pronósticos de los extremos.

Esto es temporización todo o nada basada en el modelo, no combustión predictiva, control de ECU
calibrado, gestión robusta de fallos ni un mapa de inyector medido. Mantener los demás
comandos y omitir los eventos externos futuros son hipótesis explícitas del pronóstico.
Los cambios de la carga, la presión o la acción del controlador futuras pueden cambiar la entrega real.

## Horizonte y precisión física

Una predicción finita debe incluir el combustible relevante de cierre y de rebote. En el actuador de
investigación aislado, una predicción de 8 ms trunca una cola tardía no despreciable; una predicción de 20/30 ms
da la misma decisión en la rejilla de ticks. El estudio de horizonte se conserva como
evidencia, en lugar de tratar un pronóstico corto arbitrario como un cierre completo.

El paso de tiempo físico, el periodo de muestreo del controlador y el horizonte del pronóstico son controles
de precisión aparte. Un horizonte más largo no repara una integración eléctrica o de contacto gruesa
ni un modelo constitutivo inexacto. La igualdad entre pronóstico y realidad bajo
el mismo modelo de entradas mantenidas verifica la implementación, no una calibración OEM. Las comprobaciones analíticas,
de EDO independiente, de conservación y de eventos de la planta subyacente siguen aplicando.

## Observables y transacciones

Las salidas del controlador con predicción activada incluyen:

- `predicted_fuel_mass`: combustible adicional del candidato de cierre elegido, kg.
- `prediction_ticks`: la cuenta configurada de repetición física.
- `driver_state`: si el corte se ha enclavado para el ciclo observado.
- `closing_delay_ticks`: ticks físicos restantes antes de la retirada programada de voltaje.

El voltaje mantenido y el objetivo y la entrega de la última muestra siguen disponibles. La cantidad
predicha incluye cualquier retardo de accionamiento planificado, mientras que la consulta pública de solo lectura de Core
predice siempre el cierre inmediato a voltaje cero. Estas cantidades no son transferencias reales
de combustible y no entran en los libros de masa, químico ni de energía.

Cinco entradas de estado informadas adicionales por controlador conservan la masa y la cuenta de la predicción,
el enclavamiento y el ciclo del corte, y la cuenta atrás, cuando la predicción está activada. El estado de repetición
aparte se asigna una vez por simulación. Las lecturas, el avance activo con éxito y
las instantáneas no asignan memoria administrada tras el calentamiento. La cancelación, las revisiones,
las bifurcaciones independientes, la captura especulativa del embrague y el fallo numérico tardío conservan
todos los historiales de predicción, de control y físicos. La predicción desactivada conserva las
huellas y los hashes anteriores.

## Definiciones compartidas y evidencia

JSON acepta `needle_driver.parameters.closure_prediction_ns` opcional. Core usa
`NeedleDriverDefinition.ClosurePredictionNanoseconds`. Las capacidades declaran límites,
hipótesis de mantenimiento y observables; `closure-compensated-cylinder` es el ejemplo
compartido. Las solicitudes de origen en `kg` siguen siendo escribibles y el voltaje de la bobina sigue perteneciendo al controlador.
Las solicitudes con éxito son distintas del seguimiento real de la dosis y de los KPI que pasan.

El asset v21 conserva la tabla de cuentas existente y amplía cada registro de controlador de
32 a 40 bytes con un uint64 de horizonte. Los lectores anteriores toman por defecto la predicción desactivada;
un fixture auténtico v20 conserva su huella y la repetición en el mismo runtime. La predicción
activada añade la etiqueta 25 de huella y el horizonte configurado. Se comprueban las cuentas acotadas,
las unidades, el horizonte y la alineación, la propiedad del controlador y el rechazo de degradación.

La solicitud aislada de 8 mg, el laboratorio encendido completo, el estudio de horizonte, los modelos inmutables,
el pronóstico de solo lectura y el cierre manual independiente, la repetición completa, las cero asignaciones
y los lotes cancelados o fallidos se verifican en [VALIDATION.es.md](VALIDATION.es.md).
El motor, la transmisión y el control completos, los mapas físicos y de accionamiento medidos, el repostaje del raíl,
Unity real y la aceptación de un vehículo calibrado siguen sin terminar.
