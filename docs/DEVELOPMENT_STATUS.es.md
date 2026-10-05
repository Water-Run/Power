# Estado de desarrollo

[English](DEVELOPMENT_STATUS.md) · [简体中文](DEVELOPMENT_STATUS.zh-CN.md) · [Français](DEVELOPMENT_STATUS.fr.md) · [Русский](DEVELOPMENT_STATUS.ru.md) · [日本語](DEVELOPMENT_STATUS.ja.md) · [한국어](DEVELOPMENT_STATUS.ko.md) · [Deutsch](DEVELOPMENT_STATUS.de.md) · **Español** · [Italiano](DEVELOPMENT_STATUS.it.md) · [Português](DEVELOPMENT_STATUS.pt-BR.md)

Power! tiene un núcleo de simulación gestionado, documentos de modelo y assets portátiles compartidos,
una CLI sin interfaz, un servicio de agente MCP y un estudio de Unity preparado. Los laboratorios
sintéticos ejercitan el comportamiento del motor, la transmisión, la hidráulica y la parte eléctrica.
Siguen inconclusos los grupos motopropulsores completos, el control coordinado de ECU/TCU, la calibración medida y una
aplicación de escritorio de Unity aceptada.

## Implementación actual

| Área | Implementado | Aceptación restante |
|---|---|---|
| Núcleo | Unidades explícitas e IDs estables; compilación inmutable; tiempo entero acotado; libros observables; repetición, cancelación, bifurcaciones independientes y reversión del lote entero | Evidencia de larga duración y del grupo motopropulsor completo |
| Motor | Cilindros cerrados y abiertos, trabajo de presión de biela-manivela, flujo de gas bidireccional, válvulas temporizadas por cigüeñal, calor de pared y combustión premmezclada prescrita | Admisión y escape detallados, encendido, pérdidas mecánicas, termoquímica más rica y comportamiento medido del motor |
| Combustible | Combustible, aire y productos rastreados, raíles gaseosos finitos con dosificación por ciclo, raíles líquidos flexibles y finitos que alimentan películas, evaporación pagada por la pared y reacción solo de vapor | Bomba de raíl y repostaje, comportamiento magnético, electrónico y de pulverización no lineal, equilibrio de fases dependiente de la presión y propiedades de combustible medidas |
| Transmisión | Embragues estáticos y deslizantes, actuación por contacto, engranajes y planetarios con signo, convertidor y bloqueo mapeados, y caminos DCT de siete marchas adelante y marcha atrás y Ravigneaux de cuatro adelante y marcha atrás, con giro de satélite e inercia orbital resueltos | Flexibilidad, pérdidas y reparto de carga del engrane, actuación DCT, control completo de presión y cambio de la AT y encaminamiento medido, cambios coordinados, pérdidas medidas y comportamiento de convertidor más rico |
| Hidráulica | Volúmenes flexibles, restricciones, bombas con fugas y arrastre explícitos, alivio, pistones dinámicos, correderas dosificadas y acumuladores de gas de energía finita | Mapas medidos de válvulas, acumuladores y bombas, fricción de juntas, cavitación e hidráulica completa de transmisión |
| Eléctrica | Motores RL, solenoides recíprocos de inductancia variable, batería de carga finita, polarización por resistencia y RC, conversión de ciclo de trabajo promediada y accesorios | Comportamiento químico y térmico medido, BMS, control de corriente e integración completa de la alimentación |
| Controles | PI de presión muestreado, realimentación y predicción de cierre de la aguja, y control DCT escalonado confirmado por sensor, con propiedad de los actuadores, relojes enteros y memoria transaccional | Coordinación de par de ECU/TCU, sensores, actuadores y tratamiento de fallos |
| Documentos y assets | 36 laboratorios JSON/CLI, 35 ejemplos MCP, asset v25 y lectores v1-v24 | Edición/guardado y colecciones de modelos calibrados |
| Agentes | Doce herramientas MCP definidas por esquema; evidencia compacta, comprobaciones de revisión y diagnósticos accionables | Flujos completos para el alcance físico y de control restante |
| Unity | Importación de modelos, reproducción en ticks exactos, componentes 3D esquemáticos, controles, reinicio y pruebas de ciclo de vida preparadas | Aceptación real de Editor/Play, gráficas seleccionables, edición y guardado de grafos, y Player/IL2CPP |
| Archivo nativo | Prototipos de investigación en Zig 0.15.2, ABI conservado y procedencia original de las fuentes | Referencia histórica; la migración gestionada sigue separada de la funcionalidad completa |

El núcleo y Assets tienen como destino tanto `net10.0` como `netstandard2.1`; el núcleo no tiene dependencias de Unity,
de transporte, de proveedor de modelos ni de terceros. Los scripts de Unity Assets usan
C# 9. Unity carga los ensamblados Standard compilados por el SDK externo; no
compila fuente de .NET 10 ni de C# 14.

## Evidencia y límites

El [grafo de actuación hidráulica de la AT](AT_HYDRAULIC_ACTUATION.es.md) suministra los cinco
elementos de rango y el bloqueo opcional del convertidor desde una bomba compartida accionada por eje.
Los caminos explícitos de llenado y drenaje, el movimiento finito del pistón, los resortes de retorno y la capacidad
derivada de la pastilla conservan el trabajo de desplazamiento hidráulico y el comportamiento real de captura y liberación.
Las válvulas prescritas no son un control AT confirmado por sensor ni una aceptación medida del cuerpo
de válvulas. El comportamiento completo del grupo motopropulsor y la aceptación medida siguen
inconclusos.


La `dotnet run --file tools/Build.cs -- verify` en serie exigida pasa en local en
Windows x64. Cubre los dos destinos de ensamblado alojados en .NET 10, un proceso hijo MCP
real, todos los informes de laboratorio, el archivo Zig y el ABI de C#. Los recuentos actuales,
las rutas de registro, las comprobaciones de esquema, los resultados numéricos y la procedencia de CI conservada están en
[VALIDATION.es.md](VALIDATION.es.md). Las pruebas de ensamblados Standard en .NET 10 no establecen
compatibilidad con el runtime de Unity.

El [incremento de película líquida](FUEL_FILM.es.md) tiene ahora comprobaciones analíticas de calentamiento, saturación y secado,
referencias ODE simultáneas independientes, conservación de masa, química y térmica,
repetición portátil y transacciones de sesión completas. El orden simétrico de las películas da
un refinamiento suave de segundo orden a las películas que comparten pared. El acoplamiento con otras fuentes de calor
de pared conserva el límite explícito de pared de primer orden existente. La repetición exacta
es independiente de la precisión del paso temporal, de los KPI aprobados y de la física calibrada.

El [grafo de investigación Ravigneaux](RAVIGNEAUX_TRANSMISSION.es.md) añade restricciones de piñón
simple y doble, cinco caminos de fricción, cuatro rangos adelante y marcha atrás.
Referencias independientes de masa libre, inercia reflejada y captura con freno comprueban
las reacciones de puerto y el calor. Los experimentos de par compartido y de convertidor encendido conservan
la repetición completa y los límites de investigación explícitos. El camino reducido omite el giro de los satélites; la hidráulica detallada,
el control AT y la topología y calibración OEM siguen inconclusos. La [opción resuelta](RESOLVED_PLANETS.es.md)
añade cuatro engranes reales, dos rotores de giro absoluto e inercia orbital explícita;
referencias independientes de masa de seis rotores, de momento angular y de captura conservan esas
energías. El comportamiento detallado de dientes, lubricación y reparto de carga aún necesita evidencia.

El [controlador DCT muestreado](DCT_CONTROL.es.md) posee ahora los comandos de tracción y de selector,
preselecciona caminos descargados, espera la sincronización y el bloqueo físicos, y realiza
una liberación y un acoplamiento exclusivos y escalonados. Son observables las solicitudes de marcha enteras, el aborto a
punto muerto, los fallos de dirección, de tiempo de espera y de pérdida persistente de bloqueo, y la recuperación por una solicitud nueva.
La marcha confirmada puede ser temporalmente cero durante un deslizamiento transitorio incluso después de una
confirmación previa. Las marchas largas controladas usan coordenadas compensadas transaccionales,
con las tolerancias de fase estrictas sin cambios. El límite explícito de estado es 128; los límites de nodos y
de componentes siguen en 32/64, lo que permite la composición encendido/controlador de 70 estados.
Siguen pendientes los cambios con mezcla de par completa, los actuadores y los fallos exhaustivos de ECU/TCU.

El [incremento de doble embrague](DUAL_CLUTCH_TRANSMISSION.es.md) ensambla ahora siete
caminos adelante, un piñón loco de marcha atrás en el camino par, tres ramas de salida y de transmisión final, y
selectores de fricción explícitos. Referencias independientes de inercia con signo y reflejada, y de impulso
y calor de preselección, verifican los caminos de potencia. Los experimentos de par y encendidos se repiten
a través de todas las capas ordinarias de grafo, asset y agente. Una reserva de bloqueo lineal normalizado
y acotado resuelve la entrega seis/siete que antes fallaba, y conserva las trayectorias
existentes como regresiones. La selección y la entrega prescritas no son una TCU completa ni
el comportamiento detallado de garras, anillo de bloqueo y actuador; los parámetros y las muestras OEM siguen sin verificar.

El [incremento de compensación de cierre](CLOSURE_PREDICTION.es.md) repite un futuro acotado
de la planta con entradas retenidas sin confirmar estado. Predice el caudal residual de la aguja
y planifica la retirada de voltaje en la rejilla de ticks físicos. El seguimiento aislado de la dosis
mejora, mientras el cierre y el rebote reales y los historiales de combustible y energía siguen siendo
mecanismos físicos sin cambios. Están verificados las predicciones de solo lectura, los límites enteros, el refinamiento
del horizonte, las cero asignaciones y las transacciones completas. La predicción
retiene los demás comandos y omite los eventos de entrada externos futuros; su modelo y su horizonte
finito son límites explícitos, no una calibración ni la aceptación completa de la ECU.

El [incremento de la aguja](NEEDLE_ACTUATION.es.md) acopla la energía de flujo magnético y la
fuerza recíproca a la masa real de la aguja, al resorte y la amortiguación, y a los topes elásticos. El muestreo
entero posee el voltaje de bobina a partir de la realimentación de la dosis entregada. El fluido sigue gobernado por
la elevación física a través del retardo de cierre y del rebote; no se recorta al objetivo.
Pasan las referencias independientes magnéticas, de RL y de movimiento, los libros de fuente, fase, electricidad y térmica,
la repetición portátil y por MCP, y las transacciones completas del controlador. El exceso de entrega y
el líquido restante en el límite del experimento siguen siendo observables; estos resultados no
establecen un seguimiento de dosis calibrado ni una electrónica o magnética completa del inyector.

El [incremento de inyección líquida](LIQUID_FUEL_INJECTION.es.md) parte ahora de una película
seca y toma de una fuente flexible y finita. Pasan la presión y el trabajo analíticos del raíl,
el refinamiento simultáneo independiente, los libros completos de fuente, película, química y térmica,
la repetición MCP real y la reversión especulativa del embrague. La energía de presión del raíl está
almacenada; el calor de la tobera y el trabajo de presión del receptor exportado siguen siendo distintos. La
reducción del receptor de volumen líquido despreciable es explícita y no se contabiliza como
volumen de gas adicional ni como trabajo de cigüeñal. Siguen pendientes el comportamiento magnético, electrónico y de pulverización refinado, la bomba y el repostaje, y el hardware medido.

`film-fired-cylinder` contiene al principio un inventario líquido declarado. Calienta
y evapora ese inventario antes de la reacción prescrita; no implementa un
inyector líquido. Los marcadores y las pruebas preparados de Studio consumen las mismas definiciones.
`POWER_UNITY_EDITOR` no está definido, así que Editor/Play real, el renderizado y Player/IL2CPP
siguen sin verificar.

Todos los parámetros de investigación siguen siendo `unverified`. EA211 DJS + DQ200 y PSA EC5 + AT8
conservan sus límites completos de grupo motopropulsor y sus manifiestos de evidencia en
[assets/samples](../assets/samples). Las mediciones OEM que faltan siguen faltando.
Se conservan las licencias y la procedencia histórica de las fuentes.

## Siguiente secuencia de desarrollo

1. Añade bomba de raíl y repostaje conservativos, y actuación magnética y electrónica medida, sobre la
   [entrega de líquido flexible y finita](LIQUID_FUEL_INJECTION.es.md). Sustituye el límite declarado
   de trabajo de desplazamiento exportado cuando se resuelvan el volumen líquido finito y la cantidad de movimiento
   de la pulverización. Mantén observables por separado el líquido entregado, el combustible evaporado y la reacción,
   y conserva referencias independientes.
2. Amplía el motor con control de encendido, dinámica de admisión y escape, pérdidas
   mecánicas y termoquímica más rica. Conserva el objetivo del motor completo.
3. Amplía los caminos de investigación DCT verificados con actuación detallada, propiedades y pérdidas de satélite medidas e hidráulica
   y control AT completos, y después construye la coordinación de cambio y de par de ECU/TCU a partir de
   los primitivos verificados de engranaje, embrague, convertidor e hidráulica. Añade estado de
   controlador acotado, comportamiento de sensores y actuadores, y recuperación de fallos.
4. Ejecuta `unity-test` con el Editor fijado y después obtén evidencia de Player/IL2CPP.
   Completa la selección de canales, la edición de grafos y el guardado como funciones distintas.
5. Obtén mapas medidos, datos OEM y presupuestos de incertidumbre para los dos grupos
   motopropulsores objetivo antes de declarar muestras calibradas o preparación para la publicación.

## Realimentación de AT hidráulica

`at_controller` acepta una marcha solicitada entera en [-1,4]; cero es punto muerto. Controla cinco pares de válvulas de llenado/vaciado y el bloqueo opcional del convertidor. El orden es entrada del portasatélites, solar pequeño, solar grande, freno del portasatélites, freno del solar grande y bloqueo.

`controlled-hydraulic-ravigneaux` y `controlled-fired-hydraulic-ravigneaux` usan canal 900 e ID 1400. Conservan 99 y 122 estados declarados dentro del límite sin cambios de 128. v25 conserva rutas, ganancias y relojes y lee v1-v24.

Son controles de investigación y los parámetros siguen `unverified`. Coordinación de par ECU, sensores/válvulas detallados, fallos completos del vehículo y calibración OEM siguen pendientes. Las pruebas managed y Standard no acreditan Unity Editor/Play/Player/IL2CPP real.

[AT_CONTROL.es.md](AT_CONTROL.es.md)
