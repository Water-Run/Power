# Sincronización muestreada de doble embrague y entrega escalonada

[English](DCT_CONTROL.md) · [简体中文](DCT_CONTROL.zh-CN.md) · [Français](DCT_CONTROL.fr.md) · [Русский](DCT_CONTROL.ru.md) · [日本語](DCT_CONTROL.ja.md) · [한국어](DCT_CONTROL.ko.md) · [Deutsch](DCT_CONTROL.de.md) · **Español** · [Italiano](DCT_CONTROL.it.md) · [Português](DCT_CONTROL.pt-BR.md)

`dct_controller` posee los dos canales de embrague de tracción y los ocho de selector de un [grafo de investigación de siete marchas adelante y marcha atrás](DUAL_CLUTCH_TRANSMISSION.es.md). Su comando entero de marcha solicitada es distinta de la marcha real confirmada, de los caminos seleccionados, de la fase del cambio, del error de sincronización medido y del fallo del controlador.

Es una máquina de estados de investigación gobernada por sensores, con una interrupción explícita de par. No establece la mezcla completa de par TCU/ECU, el comportamiento detallado de garras, del anillo de bloqueo o del actuador del embrague, una estrategia de cambio calibrada ni la gestión completa de fallos del vehículo.

## Estados y confirmación física

| Fase | Política de comando mantenido y transición |
|---|---|
| Punto muerto | Los dos embragues de tracción y los selectores, liberados |
| Preparación | El camino opuesto objetivo está descargado y preseleccionado mientras la tracción precedente sigue acoplada |
| Liberación | El comando de la tracción precedente baja en rampa; la tracción objetivo sigue liberada |
| Sincronización | El selector objetivo sube en rampa hasta el comando pleno, con ambas tracciones liberadas; espera el deslizamiento medido y el bloqueo físico |
| Acoplamiento | La tracción objetivo sube en rampa; la otra tracción sigue liberada |
| Conducción | Tracción y selector objetivo confirmados; se permite la preselección del camino descargado adyacente |
| Fallo | Ambas tracciones y todos los selectores, liberados; el fallo se retiene hasta el punto muerto o una solicitud distinta |

El objetivo queda enclavado mientras hay una entrega en curso. Las solicitudes posteriores distintas del punto muerto se procesan después de esa entrega; el punto muerto aborta en una muestra debida. Los cambios del mismo camino de entrada liberan su tracción antes de cambiar los selectores. Los cambios del camino de entrada opuesto pueden preparar el objetivo descargado antes de liberar la tracción. Cada camino manda como máximo un selector, y no se usa solapamiento mandado de los embragues de tracción.

Las rampas de comando del selector usan la duración de acoplamiento configurada. Un selector está listo solo tras el comando pleno, un deslizamiento medido dentro de la tolerancia aportada y el modo físico `Locked`. La tracción se confirma solo tras el comando de acoplamiento pleno, un deslizamiento de tracción pequeño y el bloqueo físico. Aceptar el comando no anuncia una relación instantánea ni la terminación física de la marcha.

La preselección inactiva puede perturbar brevemente un camino confirmado. La marcha real de la instantánea es cero mientras la tracción o el camino seleccionado no están bloqueados físicamente. La pérdida persistente se cronometra por separado; el controlador no confunde una muestra transitoria con un fallo sostenido. Ese temporizador se reinicia con los cambios de fase y la recuperación.

## Marcha solicitada, dirección y fallos

La marcha solicitada es un entero en `[-1,7]`, con cero como punto muerto y -1 como marcha atrás. La validación estática, inmediata y programada de la entrada rechaza las fracciones. El comando de origen usa unidades explícitas `state_code`; ninguna fracción ordinaria de embrague se interpreta como un número de marcha.

Un tiempo de espera de sincronización devuelve un fallo observable en descarga. Una solicitud de marcha atrás contra un movimiento positivo del vehículo por encima del límite de velocidad aportado, o una solicitud adelante contra un movimiento negativo, se bloquea como fallo de cambio de dirección. La pérdida sostenida del bloqueo confirmado de tracción o de selector usa el mismo tiempo de espera aportado y un código de fallo distinto. Estos resultados del controlador son estados de política física, no fallos numéricos ni KPIs implícitos de un cambio correcto.

| Código de fallo | Significado |
|---:|---|
| 0 | Sin fallo |
| 1 | Tiempo de espera de sincronización o de acoplamiento |
| 2 | Solicitud de cambio de dirección bloqueada por el movimiento del vehículo |
| 3 | Pérdida persistente del bloqueo confirmado |

El punto muerto borra el fallo y libera el tren. Una solicitud válida distinta puede iniciar un intento nuevo; volver a enviar el mismo objetivo fallido no reinicia el tiempo de espera en cada muestra. Las decisiones de fallo de nivel superior, las comprobaciones de plausibilidad, los fallos de sensor y las funciones de seguridad del conductor y del vehículo siguen siendo trabajo aparte.

## Definición y propiedad

El controlador declara el nodo A del motor, el nodo del vehículo, los ID de los embragues de tracción impar y par, ocho selectores en el orden adelante 1-7/marcha atrás y su entrada de marcha solicitada. Los diez canales de actuador deben ser distintos, empezar liberados y tener un único propietario. El compilador verifica los tipos ordinarios de embrague, la topología de ejes, cubos y reducción final, la asignación impar/par, el camino del piñón loco de marcha atrás y las referencias estables. Las listas de selectores se copian a datos inmutables de definición y compilados.

La temporización explícita consiste en nanosegundos de muestra, de liberación, de acoplamiento y de tiempo de espera de sincronización. El muestreo se alinea con los ticks físicos; los demás tiempos son múltiplos positivos de la muestra y como máximo diez segundos. La tolerancia de sincronización y el límite de velocidad de dirección usan `rad_s` o `rpm`. No se aportan en silencio valores OEM, mapas de actuador ni curvas de pérdidas.

Los agentes escriben la marcha solicitada. Las escrituras directas de tracción o de selector devuelven `controlled_input` con el nombre y el canal de comando correctos, y dejan sin cambios la revisión y el estado. Las lecturas exponen la solicitud en vivo, la marcha confirmada, las selecciones impar/par mandadas, la fase, el deslizamiento del selector objetivo y el fallo. Estos códigos de estado y los canales físicos conservan sus semánticas distintas.

## Relojes enteros y transacciones completas

Las muestras corren sobre el tiempo de simulación entero acotado. Las escrituras de entrada no hacen avanzar la memoria de control. Las fracciones mantenidas se aplican al solver físico normal; la inercia, las reacciones de engranaje, la sincronización y el calor de tracción permanecen en los libros existentes. El estado del controlador contiene la marcha enclavada y la activa, las selecciones, la fase y el fallo, el reloj de fase, el error medido y el temporizador de bloqueo persistente. Las bifurcaciones, la cancelación, los lotes fallidos tardíos y los intervalos especulativos copian, hashean y revierten esa memoria y cada comando mantenido juntos. Los avances correctos y las instantáneas no asignan memoria gestionada.

Las ejecuciones largas de marcha controlada usan incrementos de coordenada compensados a partir de la velocidad de punto medio. Su compensación es transaccional y se hashea; las tolerancias estrictas de fase no cambian. Esto resuelve el redondeo acumulado que expone el escenario nuevo de sincronización larga bajo carga. Los caminos de modelo anteriores conservan la integración y el comportamiento de repetición precedentes.

El límite de estado informado es explícitamente **128**, con 32 nodos y 64 componentes sin cambios. Esto permite la composición completa de investigación de motor encendido, DCT y controlador, que supera el límite anterior de 64 estados. Se comprueban la compilación y el avance en el límite exacto, y los modelos físicos y de controlador por encima del límite; las compilaciones y las pruebas siguen siendo en serie.

## Experimentos portátiles y compartidos

El asset v22 añade un registro tipado de 104 bytes de ruta, temporización y tolerancia por cada controlador DCT. Conserva los lectores v1-v21, los ID precedentes estables y el recuento y la longitud acotados, el resumen, la propiedad tipada, las unidades y las comprobaciones de compilación física. Los modelos de controlador añaden la etiqueta de huella 26. Los campos de solicitado, real, selección, fase, error y fallo se añaden sin cambiar los ID precedentes. Un fixture auténtico de grafo v21 conserva su resumen y la repetición actualizada en el mismo runtime.

`controlled-dual-clutch` emite solicitudes de marcha a través de los siete caminos y de bajadas seleccionadas. Observa la entrega final y la preselección inactiva hasta la terminación física, en lugar de suponer un tiempo nominal. `controlled-fired-dual-clutch` combina la misma política muestreada con la combustión premmezclada de cilindro abierto. JSON, la CLI, la repetición portátil y un servidor MCP hijo real comparten las definiciones.

[VALIDATION.es.md](VALIDATION.es.md) registra la evidencia de estado y de enclavamientos, de fallo y de recuperación, de propiedad, de entrada entera, de ruta inmutable, de capacidad, de preservación de fase larga, de conservación y de repetición completa. Todos los parámetros siguen siendo `unverified`. La mezcla de par, el comportamiento completo de actuador, sensor y ECU, la AT completa, Unity real y los grupos motopropulsores objetivo calibrados siguen sin terminar.
