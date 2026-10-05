# Hoja de ruta de desarrollo de Power!

[English](ROADMAP.md) · [简体中文](ROADMAP.zh-CN.md) · [Français](ROADMAP.fr.md) · [Русский](ROADMAP.ru.md) · [日本語](ROADMAP.ja.md) · [한국어](ROADMAP.ko.md) · [Deutsch](ROADMAP.de.md) · **Español** · [Italiano](ROADMAP.it.md) · [Português](ROADMAP.pt-BR.md)

El objetivo es la plataforma completa de grupos motopropulsores de Power!: física moderna en C#,
un estudio 3D de Unity y operación directa por agentes. Un laboratorio sintético que pasa
establece un resultado numérico acotado; el motor, la transmisión, los controles, la
calibración del vehículo y la aceptación de escritorio necesitan cada uno su propia evidencia.

## Hitos

| Hito | Base disponible | Trabajo que aún falta |
|---|---|---|
| Núcleo gestionado | Física sin dependencias y de doble destino, topología, unidades, tiempo entero, repetición y transacciones atómicas | Validación integrada de larga duración del grupo motopropulsor |
| Interfaz de agente | Herramientas MCP definidas por esquema, diagnósticos estructurados, revisiones, ramas, cancelación e informes compactos | Flujos de modelado y control para el alcance restante del grupo motopropulsor completo |
| Estudio de Unity | Importación de modelo compartida, reproducción de laboratorio, componentes esquemáticos 3D y pruebas preparadas | Evidencia real de Editor/Play/Player/IL2CPP y empaquetado de escritorio |
| Banco de modelado | Asset portátil v25, lectores v1-v24 y definiciones compartidas de JSON/CLI/MCP | Edición de grafos, guardado y gráficas de canales seleccionables |
| Física del motor | Masa y energía de gas independientes, trabajo de biela-manivela, válvulas temporizadas, combustión prescrita, dosificación de combustible gaseoso y líquido, raíles flexibles finitos, evaporación de película y accionamiento físico de la aguja | Bomba de raíl y repostaje, comportamiento magnético, electrónico y de pulverización refinado, acoplamiento de volumen líquido finito, control de encendido, admisión y escape detallados, pérdidas mecánicas, termoquímica y calibración medida |
| Transmisión | Embragues acoplados, engranajes y planetarios, convertidor y bloqueo mapeados, hidráulica, y caminos DCT de siete marchas adelante y marcha atrás y Ravigneaux de cuatro adelante y marcha atrás, con giro de satélite e inercia orbital resueltos | Flexibilidad, pérdidas y reparto de carga del engrane, actuación DCT, control completo de presión y cambio de la AT y encaminamiento medido, mapas medidos, comportamiento de válvulas, juntas y cavitación, y dinámica de convertidor más rica |
| Controles e integración eléctrica | PI de presión muestreado, control de cierre de aguja y entrega DCT escalonada confirmada por sensor, voltaje y ciclo de trabajo acotados, propiedad de los actuadores, circuito equivalente de batería y accesorios | Ciclos coordinados de ECU/TCU, sensores y actuadores, solicitudes de par, fallos, BMS y comportamiento térmico y eléctrico medido |
| Evidencia de vehículo y publicación | Muestras de investigación con límites y procedencia completos | Dos grupos motopropulsores medidos completos, presupuestos de incertidumbre, estabilidad, aceptación de escritorio y distribución |

Los puntos de control numéricos actuales, los fixtures auténticos de assets y los registros
de verificación por plataforma están en [VALIDATION.es.md](VALIDATION.es.md). Consulta
[DEVELOPMENT_STATUS.es.md](DEVELOPMENT_STATUS.es.md) para el estado de la implementación y
[ARCHITECTURE.es.md](ARCHITECTURE.es.md) para los invariantes. La evidencia de CI publicada se aplica a
su revisión registrada; los cambios locales nuevos necesitan una aceptación de plataforma aparte.

## Siguiente trabajo gestionado

Continúa el [contrato de inyección líquida](LIQUID_FUEL_INJECTION.es.md) con bomba
de raíl y repostaje, y con actuación magnética y electrónica refinada. Están implementados la masa
de fuente flexible y finita y la energía de presión, el movimiento real de la aguja, la realimentación de dosis muestreada,
la reposición de la película y la evaporación. Consulta [el contrato de la aguja](NEEDLE_ACTUATION.es.md) y la [predicción de cierre acotada](CLOSURE_PREDICTION.es.md). El
receptor sigue exportando trabajo de presión de desplazamiento bajo el límite declarado de volumen
líquido despreciable; la pulverización y el desplazamiento resueltos deben sustituirlo por una
geometría verificada y un acoplamiento de cantidad de movimiento y de trabajo. Mantén separados la entrega, la disponibilidad de vapor y
la reacción prescrita, y conserva la evidencia analítica, de conservación y de convergencia.

Después amplía el encendido y el control, la dinámica de admisión y escape, y las pérdidas mecánicas
del motor. El quemado de Wiebe actual es prescrito y no establece combustión
predictiva, picado, emisiones ni calibración OEM. El
[contrato de dosificación gaseosa](FUEL_METERING.es.md) sigue siendo un camino independiente y admitido.

Parte del [grafo DCT de siete marchas adelante y marcha atrás](DUAL_CLUTCH_TRANSMISSION.es.md) con
actuación detallada de sincronizador, garras y embrague. Parte del [grafo Ravigneaux](RAVIGNEAUX_TRANSMISSION.es.md)
con [propiedades de satélite y comportamiento de engrane medidos](RESOLVED_PLANETS.es.md), actuación completa
[por pistón alimentada por bomba](AT_HYDRAULIC_ACTUATION.es.md) y control AT usando los
primitivos acoplados de convertidor, engranajes, embragues e hidráulica. Parte del [control DCT muestreado](DCT_CONTROL.es.md) hacia cambios con mezcla de par y coordinación acotada de ECU/TCU,
incluidas solicitudes de par, sensores y actuadores, y fallos recuperables.
Amplía los modelos de pérdida constante de bomba, de batería y de válvula o acumulador cuando haya datos
medidos de propiedades y control; los valores de investigación suministrados siguen sin verificar.

## Estudio y aceptación medida

Asigna a `POWER_UNITY_EDITOR` el Editor fijado y ejecuta `unity-test`. Obtén evidencia real
de importación, Play y renderizado, y después evidencia de Player/IL2CPP. La selección general de canales,
la edición de grafos y el guardado siguen siendo funciones aparte de Studio. La CLI, el MCP y
Unity deben seguir consumiendo la misma semántica de modelo.

EA211 DJS + DQ200 y PSA EC5 + AT8 conservan los límites completos del grupo motopropulsor, la
aplicabilidad al vehículo y los manifiestos de evidencia. Las mediciones OEM que faltan no se sustituyen por
valores por defecto silenciosos. La terminación funcional, la corrección numérica y la credibilidad del vehículo
medido exigen aceptaciones separadas.

El [archivo Zig](NATIVE_ZIG.es.md) conserva los hashes originales y la procedencia Git en
`legacy/native/migration-manifest.json`, incluida la revisión C original `c342d4c`.
Sigue separado de la aplicación activa de C#/Unity. La migración nativa no
completa la migración de funciones gestionadas ni la aceptación del grupo motopropulsor. Introduce
paralelismo adicional, resolución dispersa o Burst cuando las mediciones lo justifiquen y los contratos
del núcleo sigan estables.

## Realimentación de AT hidráulica

`at_controller` acepta una marcha solicitada entera en [-1,4]; cero es punto muerto. Controla cinco pares de válvulas de llenado/vaciado y el bloqueo opcional del convertidor. El orden es entrada del portasatélites, solar pequeño, solar grande, freno del portasatélites, freno del solar grande y bloqueo.

`controlled-hydraulic-ravigneaux` y `controlled-fired-hydraulic-ravigneaux` usan canal 900 e ID 1400. Conservan 99 y 122 estados declarados dentro del límite sin cambios de 128. v25 conserva rutas, ganancias y relojes y lee v1-v24.

Son controles de investigación y los parámetros siguen `unverified`. Coordinación de par ECU, sensores/válvulas detallados, fallos completos del vehículo y calibración OEM siguen pendientes. Las pruebas managed y Standard no acreditan Unity Editor/Play/Player/IL2CPP real.

[AT_CONTROL.es.md](AT_CONTROL.es.md)
