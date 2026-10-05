# Registro de validación

[English](VALIDATION.md) · [简体中文](VALIDATION.zh-CN.md) · [Français](VALIDATION.fr.md) · [Русский](VALIDATION.ru.md) · [日本語](VALIDATION.ja.md) · [한국어](VALIDATION.ko.md) · [Deutsch](VALIDATION.de.md) · **Español** · [Italiano](VALIDATION.it.md) · [Português](VALIDATION.pt-BR.md)

## 2026-10-05: Raíl de combustible líquido alimentado por bomba

La verificación serie requerida pasa localmente en Windows x64/.NET 10.0.12. Las pruebas Standard se ejecutan en .NET 10; Unity Editor/Play/Player/IL2CPP real sigue sin verificar.

`dotnet run --file tools/Build.cs -- verify`

| Identifier | Value |
|---|---|
| managed_checks | 378/378 |
| standard_checks_on_dotnet | 291/291 |
| actual_mcp_groups | 41/41 |
| laboratories | 38 |
| zig_tests | 16/16 |
| native_model_cs_groups | 6 |
| original_baseline_values | 176 |
| original_baseline_maximum_error | 0 |
| asset_version | power.asset.v26 |
| v25_fixture_sha256 | df400d7e72a2375c6b6185e7206162f2dd8f74ecd52c1862b0bedca6c03ba6dc |
| verification_log_sha256 | 2a2c3aa19317c8251c970a4b98a169b402d1855838e7fb79831d273593bfdf29 |

| Identifier | pump-fed-liquid-cylinder | pump-fed-needle-cylinder |
|---|---|---|
| duration_s | 0.6 | 0.6 |
| boundaries | 65 | 65 |
| states | 39 | 48 |
| step_ns | 50000 | 10000 |
| fingerprint | 22cbbe4a983098f4 | 782b15c21ad211c2 |
| final_state_hash | 4004b74f7f1d8c22 | e322d7a4bd72088c |
| source_sha256 | 1bc97e3f985567f93fd4ace4006307ab571d2e10850e1bd75a1537ed6f0954bb | 3a801026e05e9e6cb67d298145ac20aea9948ab28e2c4552f8dee0d4f1eb4cce |
| report_sha256 | 9869c20db74d4bb1dd22f5b3a924402001ca29451acee7fcc72b9188f25bf437 | 16ac066d9f9e268b98d874aba37f75efeb6b1fa1034b6b06562899f20f67fbbb |
| max_sampled_energy_j | 4.160256139584817e-09 | 2.0303104975027964e-08 |
| max_sampled_mass_kg | 9.215718466126788e-19 | 2.439454888092385e-18 |
| max_sampled_fuel_kg | 1.0486267887008238e-18 | 2.825701912040346e-18 |
| max_sampled_hydraulic_volume_m3 | 1.523666688322677e-21 | 3.763671787116276e-21 |

- `artifacts/reports/rail-feed-integrated-2026-10-05.log`
- `artifacts/reports/rail-feed-evidence-2026-10-05.json`
- `artifacts/reports/rail-feed-schema-audit-2026-10-05.json`

Seis grupos físicos/transaccionales, dos de activos, tres integrados y dos escenarios MCP reales cubren la alimentación. Energía térmica/química entrante se acumula con la energía de frontera gaseosa; la presión se almacena una vez. El KPI usa la cota analítica de bomba de 920541.8 Pa en lugar de la presión inicial.

La fuente es una frontera externa explícita, no un tanque finito modelado. Agotamiento, eficiencia/regulación de bomba, pérdidas de línea, cavitación, propiedades dependientes de presión y spray de volumen finito siguen abiertos. Parámetros `unverified`; no se acredita calibración OEM ni aceptación real Unity Editor/Play/Player/IL2CPP.

El checkpoint AT anterior también pasa CI Windows, Linux y macOS en la revisión registrada. Esa ejecución no verifica estos cambios de alimentación.

`0d5a98583723b39e5a0b7723d06e988ebea1a425` · [CI 37259428892](https://github.com/Water-Run/Power/actions/runs/37259428892)

[PUMP_FED_FUEL.es.md](PUMP_FED_FUEL.es.md)

## 2026-10-05: Realimentación AT hidráulica y control de presión

La verificación serie requerida pasa en Windows x64/.NET 10.0.12. La compilación Release no tiene advertencias ni errores. La aceptación real de Unity y nuevas verificaciones Linux/macOS sigue sin confirmar.

`dotnet run --file tools/Build.cs -- verify`

| Identifier | Value |
|---|---|
| managed_checks | 367/367 |
| standard_checks_on_dotnet | 283/283 |
| actual_mcp_groups | 39/39 |
| laboratories | 36 |
| zig_tests | 16/16 |
| native_model_cs_groups | 6 |
| original_baseline_values | 176 |
| original_baseline_maximum_error | 0 |
| asset_version | power.asset.v25 |
| v24_fixture_sha256 | 6d6dc0f3b17ae1dfdcda59fc45ca7db63d260fb954c8c51567f9b00bab783ecc |

| Identifier | controlled-hydraulic-ravigneaux | controlled-fired-hydraulic-ravigneaux |
|---|---|---|
| duration_s | 4.5 | 2.2 |
| boundaries | 451 | 221 |
| states | 99 | 122 |
| confirmed_range | 1 | 4 |
| fault | 0 | 0 |
| lockup_state | 0 | 2 |
| max_sampled_energy_j | 7.059115887386724e-7 | 2.7647047318168916e-7 |
| max_sampled_hydraulic_volume_m3 | 1.7499700690273845e-18 | 2.7681036716270535e-18 |
| fingerprint | ceb522c56530be00 | 9fa63e406ccae528 |
| final_state_hash | 7ff1919e1f504740 | 958185fe86764b63 |
| source_sha256 | 9cac20ed7a79a2b9dd30f630adf5c6b3ce1f5dbbe4fa837eba3f0465cf67855b | cd4bd02357168532793462903670f9ec59e0c47edcd24986adc71c27bb4e88af |

- `artifacts/reports/at-control-final-2026-10-05.log`
- `artifacts/reports/at-control-evidence-2026-10-05.json`
- `artifacts/reports/at-control-schema-audit-2026-10-05.json`
- `artifacts/reports/at-controller-probe-2026-10-05.log`

8 grupos físicos/transaccionales cubren todas las marchas adelante y atrás, presión/contacto/bloqueo reales, propiedad de válvulas, relojes y límites PI. Los fallos incluyen pérdida de alimentación, drenaje bloqueado, tiempo de aplicación/liberación, interbloqueo de dirección y pérdida de bloqueo confirmado. 2 grupos de assets conservan rutas tipadas y rechazan descensos firmados de nuevo. 3 grupos integrados y 2 escenarios MCP reales coinciden en cada escalar y hash de estado.

El arranque en reversa y la recuperación siguen Applying a 500 ms y se confirman antes de 800 ms dentro del tiempo declarado. Las pruebas siguen el bloqueo real y no una demora fija. El vaciado seguro no elimina un bloqueo físico. El grafo motor/planetas/actuadores/control conserva 122 estados dentro del límite inalterado de 128.

Los parámetros siguen `unverified`. Coordinación de par ECU, sensores/válvulas detallados, fallos completos del vehículo, calibración OEM y aceptación real Editor/Play/Player/IL2CPP siguen pendientes.

[AT_CONTROL.es.md](AT_CONTROL.es.md)


## 2026-10-02: alimentación hidráulica AT compartida y actuación dinámica por pistón

La verificación en serie exigida, `dotnet run --file tools/Build.cs -- verify`, pasa en local
en Windows x64/.NET 10.0.12: **354/354** comprobaciones administradas, **273/273**
comprobaciones de ensamblados Standard alojadas en .NET 10, **37/37** grupos MCP reales,
**16/16** pruebas Zig y seis comprobaciones C# de modelos nativos. La compilación Release informa
cero advertencias/errores. Los **34** laboratorios pasan y los **176**
valores de referencia originales coinciden exactamente. Las auditorías de C/C++/Lua siguen vacías. La aceptación real de Unity y
la nueva de Linux/macOS siguen sin verificar.

Archivos de evidencia:

- `artifacts/reports/at-actuation-closed-2026-10-02.log`: ejecución en serie completa.
- `artifacts/reports/at-actuation-evidence-2026-10-02.json`: resúmenes de registro/informe/fuente,
  estado de presión/recorrido/contacto, trabajo de bomba y cotas completas de inventario/energía.
- `artifacts/reports/at-actuation-schema-audit-2026-10-02.json`: los 34 documentos
  se validan con jsonschema 4.25.1.
- `artifacts/reports/at-finer-10000.json` y `at-finer-5000.json`: referencias encendidas
  acopladas más finas, con los documentos de autoría conservados de forma explícita.

Seis grupos físicos/transaccionales comprueban la reducción inmutable del grafo ordinario,
la capacidad real de presión/recorrido/contacto, el refinamiento RK4 simultáneo e independiente
de bomba-presión-movimiento, el inventario barrido completo, el trabajo de bomba y el calor
encaminado, seis ramas de alimentación común, errores explícitos de unidades/carrera/referencia/canal,
la cancelación, la reversión tardía, las bifurcaciones independientes y el avance sin asignaciones.
La comprobación térmica de un solo actuador deduce de forma independiente el calor de arrastre del eje a partir del
trabajo de la fuente, el cambio cinético del eje y el trabajo de la bomba. Las áreas delantera/trasera siguen explícitas;
los ejemplos usan 0.001/0 m2 y, por tanto, conservan el volumen barrido delantero en el inventario.

Tres grupos de integración y dos escenarios MCP reales nuevos conservan cada escalar
y cada hash de estado en **201** límites del tren de par de cinco ramas y **87** límites encendidos de seis
ramas. Las huellas del auxiliar y del JSON coinciden. Los canales de acoplamiento anteriores
están ausentes; solo las entradas reales de llenado/drenaje impulsan la presión y el movimiento. Una capacidad inicial
de pastilla nula sigue en cero a pesar de una orden de aplicación. Los bloqueos físicos se confirman
en los cuatro caminos adelante de subida/bajada. El bloqueo encendido también usa el movimiento real del
pistón. Tanto la energía de presión como la energía de pistón/retorno/pastilla/tope permanecen en el
libro de energía de la transacción completa y en el global.

La comparación más gruesa del estado final a 40/20/10 us no fue monótona: los errores normalizados
frente a 10 us fueron `1.74505e-5` a 40 us y `2.19913e-5` a 20 us. Ese criterio
fallido no se trata como un resultado de convergencia aprobado. Una comparación más fina **20/10/5-us**
entra en un rango de error decreciente: respecto a 5 us, el máximo normalizado
sobre ocho salidas de motor/vehículo/presión/recorrido/trabajo/calor baja
de **2.01463e-5** a 20 us a **6.51409e-6** a 10 us. Todas las ejecuciones de referencia cumplen
los KPI físicos y la repetición exacta en su propio runtime. No se infiere un orden global de convergencia ni una
exactitud OEM a partir de entregas híbridas prescritas.

| Magnitud final | Tren de cinco ramas (2 s) | Tren encendido de seis ramas (0.8 s) |
|---|---:|---:|
| Velocidad de motor/fuente | 102.2242615437 rad/s | 36.2608726849 rad/s |
| Velocidad del vehículo | 8.5186884620 rad/s | 13.5978272568 rad/s |
| Presión de línea | 1.1963842937 MPa | 1.0666023745 MPa |
| Trabajo de bomba | 117.4226709117 J | 37.6491650676 J |
| Estados informados | 83 | 106 |
| Residuo máximo de energía muestreado | `1.64680e-7 J` | `1.29307e-8 J` |
| Residuo máximo de inventario barrido | `1.18076e-18 m3` | `2.94767e-19 m3` |
| Residuo máximo de fase de engranaje | `5.68434e-14 rad` | `1.42109e-14 rad` |
| Huella | `f61d582874bd086b` | `4e83efb34de63922` |
| Hash final | `9307fb49117dad0a` | `70e00a107ef1ca6d` |

Los valores SHA-256 de las fuentes son
`119008206e0d1a5372ec399665d30669ac41bbf2f0fbc0c27c56bf3bc90c8f02`
(tren de par) y
`9659b282454202857b16ee2dbc0226f30b79549fedab88b4c79f92c6c6e66801`
(encendido). La planta encendida densa cabe en el límite existente de 128 estados con 106 estados,
sin eliminar el estado del motor, del convertidor, de la presión ni del actuador. Los registros v24
existentes bastan; no se introduce un formato de asset nuevo. La batería MCP real de 37 escenarios
sigue acotada a 300 s, incluidas estas plantas acopladas más grandes.

Los parámetros y los mapas siguen siendo `unverified`. Las programaciones de válvulas son prescritas;
la realimentación AT completa, la coordinación de par de la ECU, el comportamiento medido del cuerpo de válvulas,
los modelos de junta/cavitación/aireación/temperatura y la calibración OEM siguen abiertos.
Las importaciones y la reproducción preparadas en Studio no establecen la aceptación real de Editor/Play/Player/
IL2CPP. El objetivo completo sigue inacabado. Consulta
[AT_HYDRAULIC_ACTUATION.md](AT_HYDRAULIC_ACTUATION.es.md).

## 2026-10-02: giro absoluto de satélites, inercia orbital y cuatro engranes físicos

La verificación en serie exigida, `dotnet run --file tools/Build.cs -- verify`, pasa en local
en Windows x64/.NET 10.0.12: **345/345** comprobaciones administradas, **267/267**
comprobaciones de ensamblados Standard alojadas en .NET 10, **35/35** grupos MCP reales,
**16/16** pruebas Zig y seis comprobaciones C# de modelos nativos. La compilación Release informa
cero advertencias/errores. Los **32** laboratorios pasan y los **176**
valores de referencia históricos coinciden exactamente. Las auditorías de C/C++ y Lua siguen vacías. La aceptación real de Unity
y la nueva de Linux/macOS siguen sin verificar.

Archivos de evidencia:

- `artifacts/reports/resolved-planets-endpoint-2026-10-02.log`: ejecución en serie completa.
- `artifacts/reports/resolved-planets-evidence-2026-10-02.json`: resúmenes de registro/informe/fuente,
  geometría, energías de giro/órbita, residuos y diagnósticos numéricos.
- `artifacts/reports/resolved-planets-schema-audit-2026-10-02.json`: 32 documentos válidos
  y ocho casos malformados de engrane del portasatélites rechazados por jsonschema 4.25.1.
- `artifacts/reports/resolved-planet-failure-2026-10-02.json` y
  `resolved-planet-refinement-failure-2026-10-02.json`: trazas previas a la corrección, conservadas.

Ocho grupos físicos/transaccionales comprueban relaciones con signo relativas al portasatélites,
matrices de masa independientes en el espacio de aceleraciones, geometría rígida de paso, agregación
de masa/giro por satélite e inercia orbital, las cuatro inercias reflejadas adelante/atrás,
el momento angular, la potencia de reacción de engrane sumada nula, el impulso/calor de captura del
portasatélites, la sobremarcha cargada de 20 segundos, errores de unidades/empaquetado/referencia,
la cancelación, la reversión tardía, las bifurcaciones y el avance/relectura sin asignaciones.
Dos grupos portátiles conservan relaciones con signo, portasatélites y almacenamiento de giro completos,
rechazan registros malformados y rebajas v23 con el resumen resellado, y repiten el
grafo reducido v23 auténtico. Su SHA-256 es
`f2bd390e8ecf013e5843ed3352ccf2a2828133b541fc61d7927995f8ac1dc2e2`;
la huella `63d28eb32bc4cfb2` y la repetición actualizada en cada límite siguen intactas.

La ejecución inicial del tren de par se detuvo tras **1.4033 s**. El residuo de velocidad
sol grande/satélite exterior era `1.2406076e-11 rad/s`, cerca de la cota sin cambios
`1.2406654e-11 rad/s`; el error de fase era cero y el residuo de energía
`5.26143e-10 J`. El refinamiento de Schur relativo por sí solo retrasó la parada hasta **1.4494 s**.
La resolución libre del punto medio impone ahora `G v_next=0` mediante
`v_next=2 v_mid-v_old`, en lugar de reflejar una y otra vez el redondeo precedente.
En estados antiguos exactamente compatibles, esto es la restricción ordinaria de punto medio nulo.
Todos los cambios usan multiplicadores reales de respuesta a la fuerza, que se acumulan en las reacciones
medias. Tres refinamientos relativos acotados mejoran las respuestas pequeñas a la fuerza;
el espacio de trabajo pertenece a la simulación o es local al constructor, y los factores compilados siguen
inmutables. Los grafos existentes conservan su comportamiento precedente de proyección/repetición.
No se redujo ni se aumentó ninguna tolerancia de inercia o de velocidad/fase para aprobar el caso.

Tres grupos de integración y dos escenarios MCP reales nuevos coinciden en cada escalar
y cada hash de estado en **201** límites del tren de par y **87** límites encendidos/de convertidor.
Las huellas del auxiliar y del JSON plano coinciden. El giro absoluto interior/exterior, el almacenamiento
orbital del portasatélites, todas las reacciones de engrane y los libros térmicos completos de fricción/convertidor
siguen siendo observables. Las revisiones del agente, la cancelación, la reparación de relaciones inválidas
y las bifurcaciones independientes en punto muerto siguen cubiertas.

| Magnitud final | Tren de par (2 s) | Tren encendido (0.8 s) |
|---|---:|---:|
| Velocidad de motor/fuente | 117.3118882900 rad/s | 40.9899766814 rad/s |
| Velocidad del vehículo | 9.7759906908 rad/s | 15.3712412555 rad/s |
| Velocidad absoluta del satélite interior | -469.2475531599 rad/s | -204.9498834069 rad/s |
| Velocidad absoluta del satélite exterior | 156.4158510533 rad/s | 122.9699300441 rad/s |
| Energía de giro de los satélites | 23.3037873338 J | 12.2863030022 J |
| Energía orbital de los satélites | Casi cero, con el portasatélites retenido | 15.4891426738 J |
| Estados informados | 27 | 41 |
| Residuo máximo de energía muestreado | `4.01224e-9 J` | `3.00179e-9 J` |
| Residuo máximo de fase de engranaje | `1.13687e-13 rad` | `2.84217e-14 rad` |
| Residuo máximo de velocidad de engranaje | `3.97904e-13 rad/s` | `2.27374e-13 rad/s` |
| Huella | `d3010be4b33fdbaf` | `faf9384667ece88d` |
| Hash final | `8e1da00bda34941b` | `e606196f7b345c99` |

El tren encendido quema **38.0629762199 mg** y libera **1674.7709536768 J**.
Respecto a una referencia de estado final a 12.5 us, el error máximo normalizado entre
las velocidades de motor/vehículo/satélites y las salidas de trabajo/calor baja de `1.13450e-6`
a 50 us a `1.04173e-6` a 25 us. Esto es un refinamiento acotado a través de
entregas híbridas prescritas; no se afirma un orden global de convergencia.

Los tres pares síncronos declarados usan un radio de corona de **0.1 m**, masas por satélite interior/exterior
de **0.3/1 kg** e inercias de giro de **0.000015/0.0005 kg m2**.
La estructura del portasatélites, **0.03 kg m2**, recibe una inercia orbital explícita
de **0.0184375 kg m2**. Los valores SHA-256 de las fuentes son
`c45d90282097303da44c9405eb6945637b578db84617f0b749efc58ed879f0a5`
(par) y
`e71d9ea4755826b1dcd916fd0157827b2e845dd48506e32f6291674d56f315f3`
(encendido). La geometría, las masas y los mapas siguen siendo `unverified`. Los engranes síncronos rígidos
no establecen el reparto de carga de fabricación, la flexibilidad del diente, la lubricación/las pérdidas,
la hidráulica/el control AT completos, la identidad OEM ni un comportamiento de vehículo calibrado.
Los casos de Studio preparados incluyen los tres puertos de engrane del portasatélites; no establecen
resultados reales de Editor/Play/Player/IL2CPP. Consulta [RESOLVED_PLANETS.md](RESOLVED_PLANETS.es.md).

## 2026-10-02: caminos compuestos Ravigneaux y composición de convertidor encendido

La verificación en serie exigida, `dotnet run --file tools/Build.cs -- verify`, pasa en local
en Windows x64 con .NET 10.0.12: **332/332** comprobaciones administradas, **257/257**
comprobaciones de ensamblados Standard alojadas en .NET 10, **33/33** grupos MCP reales,
**16/16** pruebas Zig y seis comprobaciones C# de modelos nativos. La compilación Release informa
cero advertencias/errores. Los **176** valores de referencia originales coinciden exactamente;
los inventarios de fuentes C/C++ y Lua siguen vacíos. Los **30** laboratorios pasan.
La aceptación real de Unity y la nueva de Linux/macOS siguen sin verificar.

Archivos de evidencia:

- `artifacts/reports/ravigneaux-confirmed-2026-10-02.log`: ejecución en serie completa.
- `artifacts/reports/ravigneaux-evidence-2026-10-02.json`: resúmenes de registro/fuente/informe,
  cotas de estado, libros numéricos y limitaciones declaradas.
- `artifacts/reports/ravigneaux-schema-audit-2026-10-02.json`: los 30 documentos
  pasan jsonschema 4.25.1; ocho casos de topología malformada se rechazan.
- `artifacts/reports/ravigneaux-phase-failure-2026-10-02.json`: diagnóstico
  aislado antes de la compensación de coordenadas.

Seis grupos físicos/transaccionales comprueban la matriz de masa libre 2x2 reducida de forma independiente,
las cuatro inercias reflejadas adelante y atrás, las reacciones de los miembros y
la potencia de reacción de engranaje sumada nula, el impulso/calor de captura del freno del portasatélites, la sobremarcha
cargada larga, unidades/geometría/puertos malformados, la cancelación, la reversión tardía,
las bifurcaciones independientes y el avance/relectura sin asignaciones. Dos grupos portátiles
comprueban registros completos del portasatélites, recuentos tipados, referencias duplicadas y el rechazo
de una rebaja v22 falsificada. El SHA-256 del fixture auténtico de DCT controlada v22 es
`db90df5fa9ca90067341abdcaf8c3a4a38316794a4b3c8ecc7c89a852375c24e`;
la huella `72122eae163df98e` y la repetición actualizada exacta siguen intactas.

La sobremarcha cargada larga se detuvo al principio tras el último límite confirmado de **2.9668 s**.
El residuo de fase del doble piñón era `-8.27754e-10 rad`, cerca de la
cota `8.27906e-10 rad`, mientras que el residuo de velocidad era `-1.77991e-12 rad/s` frente a
`5.21235e-11 rad/s`. Los modelos compuestos usan ahora una acumulación transaccional de coordenadas compensadas.
La misma comprobación analítica de carga de **20 segundos** pasa sin aumentar
las tolerancias de fase/velocidad ni proyectar posiciones. El estado de corrección se copia, entra en el hash
y se revierte con cada intervalo; los modelos anteriores sin compuesto conservan su
comportamiento existente de integración/hash.

Tres grupos de integración y dos escenarios MCP reales conservan cada salida y
cada hash de estado en **201** límites del tren de par y **87** límites encendidos/de convertidor.
Los cinco elementos de fricción ejecutan físicamente las entregas adelante prescritas de subida/bajada;
aceptar la orden no se trata como un bloqueo completado. El almacenamiento térmico coincide con la
suma de todo el calor encaminado de embrague/convertidor. Las comprobaciones de revisión, la cancelación, el rechazo
acotado de entradas y las bifurcaciones independientes en punto muerto siguen cubiertas.

| Magnitud final | Tren de par (2 s) | Tren de convertidor encendido (0.8 s) |
|---|---:|---:|
| Velocidad de entrada/motor | 119.6764605858 rad/s | 42.4589002302 rad/s |
| Velocidad del vehículo | 9.9730383821 rad/s | 15.9220875863 rad/s |
| Estados informados | 21 | 35 |
| Calor de fricción en los cinco elementos de rango | 546.0473656082 J | 117.6775595348 J |
| Residuo máximo de energía muestreado | `1.87947e-9 J` | `1.96445e-9 J` |
| Residuo máximo de fase de engranaje | `3.19744e-14 rad` | `1.06581e-14 rad` |
| Diferencia máxima del libro térmico | `1.52568e-10 J` | `2.41471e-9 J` |
| Huella | `63d28eb32bc4cfb2` | `7c207499d9050fc6` |
| Hash final | `4eb3c05eeef034c6` | `0cbed5442969c06f` |

El tren encendido quema **37.7116554560 mg**, libera **1659.3128400630 J**,
disipa **66.4214778376 J** en el convertidor y **125.9622150447 J** en el
bloqueo. Respecto a la referencia de estado final a 12.5 us, el error máximo normalizado
entre la velocidad de motor/vehículo y tres salidas de trabajo/calor baja de
`9.92882e-7` a 50 us a `7.19762e-7` a 25 us. Esto es un refinamiento acotado
a través de eventos híbridos prescritos, no un orden global de convergencia afirmado.

Los valores SHA-256 de las fuentes son
`8063388dbd341fd16b05ab971f6c2cc23d87ebfee3681da9d1ffcf95fffa52a0`
(tren de par) y
`55587bff2a2e5f33c6e876ed0d539615cfee1daf938d72dc4946c39a36a74d44`
(tren encendido). Los parámetros y mapas de investigación siguen siendo `unverified`. El giro interno de los
satélites, las pérdidas detalladas de engranaje, la hidráulica/el control AT, la coordinación de la ECU, la topología OEM
exacta y las muestras medidas siguen abiertos. Las pruebas de importación/reproducción de Unity están
preparadas como casos individuales de recursos, incluidos errores anteriores de número de argumentos ya reparados;
no se infiere ningún resultado de Editor/Play o IL2CPP a partir de la ejecución administrada.
Consulta [RAVIGNEAUX_TRANSMISSION.md](RAVIGNEAUX_TRANSMISSION.es.md).

## 2026-10-01: sincronización DCT muestreada, entrega escalonada y control encendido combinado

La verificación en serie exigida, `dotnet run --file tools/Build.cs -- verify`, pasa en local
en Windows x64 con SDK 10.0.401/runtime 10.0.12: **321/321** comprobaciones administradas,
**249/249** comprobaciones de ensamblados Standard alojadas en .NET 10, **31/31** grupos MCP
reales, **16/16** Zig y **6/6** comprobaciones de ABI de C#. La compilación Release informa cero
advertencias/errores. Los **176** valores de referencia históricos coinciden exactamente y
la auditoría de C/C++/Lua está vacía. La aceptación real de Unity y la nueva de Linux/macOS están sin verificar.

Archivos de evidencia:

- `artifacts/reports/tcu-final-2026-10-01.log` — ejecución en serie completa.
- `artifacts/reports/tcu-evidence-2026-10-01.json` — alcance, resúmenes de registro/fuente,
  marcha/fallo/fase reales, cotas de estado, residuos de fase y de energía.
- `artifacts/reports/tcu-schema-audit-2026-10-01.json` — los **28** laboratorios
  pasan jsonschema 4.25.1; **10** casos de controlador malformados se rechazan.
- `artifacts/reports/controlled-dual-clutch.json` y
  `artifacts/reports/controlled-fired-dual-clutch.json` — informes completos.

Ocho grupos físicos/de control verifican los siete caminos confirmados, la preselección
sin carga, la entrega exclusiva escalonada de tracción, la marcha atrás con signo, el bloqueo por
sentido de movimiento, el tiempo de espera de sincronización, la pérdida persistente del bloqueo confirmado, la recuperación
a punto muerto/nuevo objetivo, las comprobaciones integrales de entrada estática/inmediata/programada, la propiedad de diez canales,
el muestreo entero, las rutas inmutables, la cancelación/reversión tardía, las bifurcaciones y cero
asignaciones. Una ejecución controlada de 20 segundos verifica la conservación estricta de la fase de engranaje
y la energía. Los resultados del controlador siguen siendo fallos observables; no se tratan en silencio
como un cambio logrado o como un fallo numérico.

La sincronización cargada larga expuso primero un redondeo acumulado de coordenadas a
3.7688 s. El residuo de velocidad de engranaje estaba dentro de su cota, mientras que el error de fase normalizado
`-3.2883917811e-10` superaba por poco la cota existente `3.2882809435e-10`.
Los modelos controlados nuevos acumulan ahora coordenadas de velocidad de punto medio con
corrección compensada transaccional. Las tolerancias estrictas no se aumentaron y
ninguna posición de estado se proyectó sobre una relación elegida. Los modelos anteriores conservan su
comportamiento precedente de integración/hash; la compensación nueva se copia, entra en el hash y se revierte
con cada intervalo real y especulativo.

La cota explícita de estados informados es **128**. Los límites de nodos/componentes siguen en
**32/64**; el comportamiento de compilación/prueba en serie no cambia. Un modelo de 32 rotores/64 estados RL
compila y avanza con exactamente 128 estados informados. Los modelos superiores de gas rastreado, película,
inyección y controlador rechazan la capacidad. La composición completa encendida/DCT/controlador
cabe ahora en 70 estados, en lugar de omitir el estado de motor/control para
caber en el límite precedente. Esto no establece un rendimiento disperso/Burst ni de Unity.

Dos grupos portátiles verifican rutas v22, periodos, rampas, tolerancias, propiedad e
historial de fallos/repetición, recuentos tipados acotados, referencias/unidades incorrectas, registros ausentes
y rebajas v21 falsificadas. El SHA-256 del fixture auténtico de grafo v21 es
`706618f7d32a5ef4b8a18ca801d2c3ba0b293eac03b584d88d584e9290a38437`;
la huella `7466a75b99fbfd78` y la repetición actualizada en el mismo runtime siguen intactas.
Los fixtures anteriores y las huellas físicas se conservan como evidencia de regresión.

Tres grupos de integración y dos escenarios MCP reales comprueban el estado muestreado,
la petición entera, la guía accionable del canal en propiedad, las revisiones, los lotes cancelados/fallidos
y las bifurcaciones independientes de orden de marcha. Los **421** límites del tren controlado y
los **83** límites encendidos controlados de informe/portátil/MCP coinciden exactamente.

| Magnitud final | Tren controlado (4.2 s) | Tren encendido controlado (0.8 s) |
|---|---:|---:|
| Marcha pedida / confirmada | 1 / 1 | 3 / 3 |
| Fase de cambio / fallo | Driving / None | Driving / None |
| Velocidad del motor | 197.0169047878 rad/s | 51.7225290420 rad/s |
| Velocidad del rotor del vehículo | 12.6455009492 rad/s | 7.6456066581 rad/s |
| Estados informados | 64 | 70 |
| Residuo máximo de energía muestreado | `1.18562e-8 J` | `1.19940e-9 J` |
| Error máximo de fase de engranaje informado | `7.16227e-14 rad` | `7.10543e-15 rad` |
| Huella | `72122eae163df98e` | `1d2b1a0c1eabf259` |
| Hash final | `ef2843acbb273e6d` | `b50dea693d6af82a` |

La reducción de marcha cargada final y la preselección sin carga posterior se observan
mediante confirmación física a 4.2 s. A 4.0 s la confirmación anterior quedó
perturbada de forma temporal por el deslizamiento real del selector/la tracción, así que la marcha real informó
correctamente cero en lugar de suponer que había terminado. La combinación encendida quema
**37.2949777641 mg** y libera **1640.9790216183 J**.

Los valores SHA-256 de las fuentes son
`f15629d3fc8d8915f5884f77603458053c83e7835cf34d434fa607d85442bc87`
(controlado) y
`c3a6d491fd614d526a99831d7dfdc4285bca3c10a57ac811959023cfb9e40d5b`
(encendido controlado). Todos los parámetros siguen siendo `unverified`. Este controlador usa de forma deliberada
una entrega con interrupción de par; la mezcla completa de par de la ECU, los sensores/actuadores,
los mecanismos de garras/anillo de bloqueo, los fallos exhaustivos, la AT, los grupos motopropulsores objetivo medidos
y Unity real siguen abiertos. Consulta [DCT_CONTROL.md](DCT_CONTROL.es.md).

## 2026-10-01: caminos de potencia de doble embrague con siete marchas adelante y marcha atrás

La verificación en serie exigida, `dotnet run --file tools/Build.cs -- verify`, pasa en local
en Windows x64 con SDK 10.0.401/runtime 10.0.12: **308/308** comprobaciones administradas,
**239/239** comprobaciones de ensamblados Standard alojadas en .NET 10, **29/29** grupos MCP reales,
**16/16** Zig y **6/6** comprobaciones de ABI de C#. La compilación Release tiene cero advertencias/errores.
Los **176** valores de referencia históricos coinciden exactamente; la auditoría de C/C++/Lua está vacía.
La aceptación real de Unity y la nueva de Linux/macOS siguen sin verificar.

Archivos de evidencia:

- `artifacts/reports/dct-final-2026-10-01.log` — verificación en serie completa.
- `artifacts/reports/dct-evidence-2026-10-01.json` — alcance, resúmenes, grafo/repetición,
  magnitudes finales y refinamiento medido.
- `artifacts/reports/dct-schema-audit-2026-10-01.json` — los **26** laboratorios
  pasan el contrato estructural existente de jsonschema 4.25.1.
- `artifacts/reports/dual-clutch-transmission.json` y
  `artifacts/reports/fired-dual-clutch.json` — informes de experimento completos.

Seis grupos físicos verifican un grafo ordinario con catorce rotores internos,
doce engranajes permanentes y diez embragues de fricción, enlaces estables en propiedad del llamador,
siete caminos adelante y de marcha atrás con signo, tres ramas de transmisión final y parámetros
inmutables. Las referencias independientes de inercia reflejada/par constante cubren cada
camino seleccionado con y sin preselección del camino inactivo. La proyección independiente de captura
en dos coordenadas comprueba el impulso de sincronización, las velocidades finales y el calor.
Pasan las bifurcaciones completas, la cancelación, la reversión tardía, los contratos de capacidad/unidades/ID/selección
y cero asignaciones administradas en el avance/relectura logrados.

El escenario completo de par expuso un fallo de la entrega de sexta a séptima justo
después de soltar el embrague antiguo. Los bloqueos correlacionados reflejados en el engranaje agotaron el presupuesto
de proyección escalar. Una reserva lineal de Schur normalizada y preasignada resuelve ahora
bloqueos independientes después de agotar ese presupuesto, con los mismos límites estáticos,
liberación acotada del conjunto activo y comprobaciones de residuo y de calor pasivo. Los casos singulares/no lineales
conservan sus límites existentes. La aceleración independiente directa de sexta/séptima
y la entrega que antes fallaba son evidencia de regresión; la física anterior y
las trayectorias auténticas de assets siguen verificadas. No se usó un aumento del límite de iteraciones ni
la aceptación de un residuo fallido.

Tres grupos de integración verifican la identidad de huella de ensamblado/JSON, el arranque,
la preselección, todas las entregas adelante de subida/bajada, el encaminamiento térmico, los diagnósticos estructurados,
las revisiones/la cancelación y las bifurcaciones independientes del selector. Ambos laboratorios se repiten
a través de assets portátiles y de un servidor MCP hijo real. Los **281** límites del laboratorio de par
y los **83** límites del laboratorio encendido de informe/portátil/MCP coinciden exactamente.

El escenario de par alcanza cada relación efectiva declarada tras la entrega:

| Marcha adelante | Relación verificada de velocidad motor/vehículo |
|---|---:|
| 1 | 15.58 |
| 2 | 9.43 |
| 3 | 6.765 |
| 4 | 5.166 |
| 5 | 3.774 |
| 6 | 3.182 |
| 7 | 2.664 |

Son reducciones de investigación declaradas, no mediciones OEM. El signo de la marcha atrás y
la inercia reflejada preseleccionada tienen evidencia independiente de carga constante; el escenario
de carretera no afirma el acoplamiento de la marcha atrás con el vehículo en movimiento.

| Magnitud final | DCT de par (2.8 s) | DCT encendido (0.8 s) |
|---|---:|---:|
| Velocidad del motor | 161.3549080416 rad/s | 53.8218438613 rad/s |
| Velocidad del rotor del vehículo | 10.3565409526 rad/s | 7.9559266609 rad/s |
| Calor total de embrague/sincronización | 870.3601867183 J | 224.4917038405 J |
| Nodo térmico | 300.8703601867 K | 351.7212768995 K |
| Residuo máximo de energía muestreado | `5.22732e-9 J` | `1.23919e-9 J` |
| Número de estados informados | 55 | 61 |
| Huella del modelo | `7466a75b99fbfd78` | `b7216a2ed8c88dc3` |
| Hash final | `c8932376afe516c6` | `35b434aca827c3a6` |

El ejemplo encendido quema **39.1255750233 mg** y libera **1721.5253010267 J**.
Su grafo completo de siete marchas adelante y marcha atrás conduce la entrega programada de 1 a 2 a 3 dentro
del presupuesto de estado acotado actual. Frente a una referencia de 12.5 microsegundos, las diferencias finales
escaladas máximas de las velocidades de motor/vehículo, el trabajo de la fuente y los calores del embrague
seleccionado son **4.9008455434e-6** a 50 microsegundos y **4.3731765238e-6** a
25 microsegundos. El error baja de forma modesta; esto por sí solo no establece un orden uniforme
de eventos híbridos ni una convergencia asintótica completa. Las referencias independientes de engranaje/embrague
y la conservación siguen siendo evidencia aparte.

Los valores SHA-256 de las fuentes son
`f5db9f0a9f3a0585eff261d71285e4a390c099ec1a3bec27d1b9194beb311294`
(par) y
`0dfc3ed92930b649c8312043625789a4a4394811fed9e0c88a615a18a6b07473`
(encendido). Se conservan los tipos de componente, las unidades, el formato de asset y los lectores anteriores.
Todos los parámetros siguen siendo `unverified`; esta disposición de tren de investigación no es
una DQ200 calibrada. Los selectores de fricción no completan la actuación de garras/anillo de bloqueo,
y las programaciones prescritas no implementan la coordinación completa de par de la TCU/ECU.
La AT completa, las pérdidas/la actuación medidas, los grupos motopropulsores objetivo completos y Unity real
siguen abiertos. Consulta [DUAL_CLUTCH_TRANSMISSION.md](DUAL_CLUTCH_TRANSMISSION.es.md).

## 2026-10-01: repetición de cierre acotada y compensación de corte en ticks físicos

La verificación en serie `dotnet run --file tools/Build.cs -- verify` pasa en local en Windows
x64 con SDK 10.0.401/runtime 10.0.12: **299/299** comprobaciones administradas, **233/233**
comprobaciones de ensamblados Standard alojadas en .NET 10, **27/27** grupos MCP reales,
**16/16** Zig y **6/6** comprobaciones de ABI de C#. La compilación Release tiene cero advertencias/errores.
Los **176** valores numéricos históricos coinciden exactamente; la auditoría de fuentes C/C++/Lua
sigue vacía. Unity real y la CI nueva de Linux/macOS están sin verificar.

Archivos de evidencia:

- `artifacts/reports/closure-final-2026-10-01.log` — verificación en serie completa.
- `artifacts/reports/closure-evidence-2026-10-01.json` — alcance legible por máquina,
  resúmenes de fuente/registro, huellas de repetición y evidencia de seguimiento y de horizonte.
- `artifacts/reports/closure-schema-audit-2026-10-01.json` — los **24** documentos de
  laboratorio pasan jsonschema 4.25.1; **6** casos de horizonte malformados se rechazan.
- `artifacts/reports/closure-compensated-cylinder.json` — experimento encendido.

Cinco grupos del núcleo verifican la repetición manual independiente exacta a voltaje cero, los valores
confirmados/el hash/el tiempo sin cambios, la entrega real mejorada, la memoria de corte, el refinamiento
del horizonte, la alineación/el presupuesto/la inmutabilidad, la cancelación de lectura y de lote, el fallo
tardío, las bifurcaciones independientes, cero asignaciones administradas en las previsiones y en el avance
predictivo activo, y los historiales especulativos completos de embrague. Cada candidato
usa un estado preasignado aparte y las ecuaciones normales de la planta; no se añade combustible previsto
a un libro real. Las predicciones desactivadas conservan las huellas y los
hashes anteriores. La cancelación o una predicción inválida rechaza el lote real completo.

A 1 ms, el banco aislado prevé alrededor de **2.9894117019 mg** de combustible adicional
bajo una retirada inmediata de voltaje, y su repetición manual de cierre, aparte, coincide
hasta la tolerancia de comparación declarada `1e-15 kg`. La previsión con la entrada mantenida es
distinta de los eventos futuros reales o del comportamiento medido del dispositivo.

El seguimiento de horizonte finito conserva el rebote tardío en el asiento:

| Horizonte de predicción | Combustible entregado real para una petición de 8 mg |
|---|---:|
| 8 ms | 8.2537740284 mg |
| 12 ms | 8.0606880453 mg |
| 20 ms | 8.0101785078 mg |
| 30 ms | 8.0101785078 mg |

La realimentación de encendido/apagado precedente entrega **9.8939438959 mg**. Con una previsión de 20 ms
y el corte en ticks físicos, el error relativo baja de **23.6743%** a **0.12723%**,
unas **186 veces** más pequeño en este banco sintético. La decisión de 20/30 ms
coincide, mientras que 8 ms trunca una cola material. Esto es evidencia de seguimiento acotado basado en el modelo,
no una exactitud de inyector calibrada. El paso de tiempo eléctrico/de contacto y
el refinamiento de controlador/horizonte siguen siendo controles de aceptación aparte.

Dos grupos portátiles verifican la repetición v21 de horizonte/corte, la recodificación exacta, horizontes
de accionamiento malformados y el rechazo de una rebaja v20 falsificada. El fixture auténtico v20, con SHA-256
`4d87e996d92a50508cfcffac610551b84220f2b3055f5b104c8ec7ec64ca9a2e`,
conserva la huella `3fa813ff44b95a79` y la repetición actualizada en el mismo runtime. Los fixtures
anteriores y las trayectorias de los modelos ideal y de encendido/apagado siguen siendo evidencia de regresión.

Tres grupos de integración más un servidor MCP hijo real verifican los horizontes del modelo,
los observables de predicción, el voltaje en propiedad, los errores accionables, las revisiones, la cancelación,
las bifurcaciones independientes y los libros completos de fuente/fase/energía. Los **65** límites encendidos
de informe/portátil/MCP coinciden. En el límite de 0.6 s:

| Magnitud | Valor |
|---|---:|
| Líquido entregado y evaporado | 28.0021908833 mg |
| Presión del raíl | 725.327490978 kPa |
| Película líquida restante | 0 mg |
| Vapor quemado | 27.9775490263 mg |
| Calor de reacción | 1231.0121571575 J |
| Última dosis de ciclo pedida / entregada | 12 mg / 12.0346025230 mg |
| Última predicción de cierre seleccionada | 2.7431038678 mg |
| Longitud de la predicción | 2000 ticks físicos |
| Retén de corte / ticks de corte pendientes | 1 / 0 |
| Residuo absoluto máximo de energía muestreado | `1.51078e-8 J` |
| Residuo absoluto máximo de masa muestreado | `4.06576e-18 kg` |
| Residuo absoluto máximo de combustible muestreado | `8.97855e-20 kg` |

La huella es `ddefea6d870e7e75`; el hash final es `b8edcd36b58805b6`; el SHA-256 de la fuente del modelo
es `106bde4e86ccb10cd214d1372cff300c09f277c88ee8811d2d9a144d72ca8326`.
La orden viva del ciclo siguiente sigue en 4 mg; la predicción y la entrega medida se
informan aparte de esa orden.

Las previsiones mantienen las demás órdenes de actuador, ignoran los eventos futuros de entrada externa,
respetan un horizonte entero finito y exigen un intervalo local monótono de corte.
Esas suposiciones y los parámetros físicos no verificados limitan esta evidencia.
La ECU/TCU completa, la reposición del raíl, el refinamiento magnético/electrónico/de fluido, Unity real y
la aceptación de un grupo motopropulsor calibrado siguen abiertos. Consulta
[CLOSURE_PREDICTION.md](CLOSURE_PREDICTION.es.md).

## 2026-10-01: aguja electromagnética y realimentación de entrega muestreada

La orden en serie exigida, `dotnet run --file tools/Build.cs -- verify`, pasa
en local en Windows x64 con SDK 10.0.401/runtime 10.0.12: **289/289** comprobaciones
administradas, **226/226** comprobaciones de ensamblados Standard en .NET 10, **26/26** grupos MCP
reales, **16/16** Zig y **6/6** comprobaciones de ABI de C#. La compilación Release tiene cero
advertencias/errores. Los **176** valores numéricos históricos coinciden exactamente; la auditoría de
fuentes encuentra cero archivos C/C++/Lua. No se afirma una CI nueva de Linux/macOS ni una aceptación real de Unity
Editor/Play/Player/IL2CPP.

Archivos de evidencia:

- `artifacts/reports/needle-final-2026-10-01.log` — ejecución en serie completa.
- `artifacts/reports/needle-evidence-2026-10-01.json` — alcance legible por máquina,
  resúmenes de fuente/registro, huellas de laboratorio, magnitudes finales y refinamiento.
- `artifacts/reports/needle-schema-audit-2026-10-01.json` — los **23** laboratorios
  pasan jsonschema 4.25.1; **16** casos magnéticos/de tope/de aguja/de accionamiento malformados se rechazan.
- `artifacts/reports/needle-actuated-cylinder.json` — experimento/repetición completos.

Nueve grupos físicos verifican la identidad discreta de energía magnética, el trabajo de alimentación con signo
y el calor de cobre no negativo, el jacobiano analítico de la fuerza, la corriente RL analítica
estacionaria, la dinámica simultánea magnética/de muelle, las bisagras conservativas de los topes de recorrido,
la entrega real de la aguja fuera de la cuota/la ventana, el voltaje en propiedad, el retraso al desasentar y el
cierre, el exceso de dosis, las transacciones completas y los contratos inmutables/dimensionales.
El avance activo y las instantáneas asignan **cero bytes administrados**. Las pruebas de captura especulativa de embrague
conservan el flujo, la fuente/fase, la fuerza media, el calor/trabajo compensados y el
historial de control muestreado/mantenido a través del lote exacto y de un lote posterior fallido.

Una ODE independiente de cinco estados integra la posición/velocidad de la aguja, el flujo magnético,
el calor de cobre y el trabajo eléctrico. Las referencias RK4 a **20,000/40,000** pasos coinciden
dentro de la tolerancia escalada `1e-10`. En 5 ms, el refinamiento físico suave es:

| Tick | Error escalado máximo | Error anterior / error actual |
|---|---:|---:|
| 25 microsegundos | `8.4976116406e-4` | — |
| 12.5 microsegundos | `2.1110507406e-4` | 4.02530 |
| 6.25 microsegundos | `5.2689855150e-5` | 4.00656 |
| 3.125 microsegundos | `1.3167017353e-5` | 4.00165 |

Esto es un refinamiento electromagnético/mecánico suave de segundo orden. El contacto,
la conmutación de ventana/accionamiento y el acoplamiento explícito de pared existente conservan límites de
exactitud aparte; la repetición exacta no demuestra un orden uniforme ni un control calibrado.

La petición aislada de 8 mg entrega **9.8939438959 mg** tras el cierre eléctrico/
mecánico pasivo y el rebote en el asiento, un exceso de **1.8939438959 mg**. Una apertura
nula a 20 ms va seguida de alrededor de **0.0001200614 mg** de entrega adicional por rebote
antes de asentarse. El modelo conserva ese caudal en lugar de recortar la masa
en el objetivo o de igualar una orden de voltaje cero con una válvula cerrada. Son
dinámicas de investigación, no un seguimiento de dosis aceptado ni un comportamiento medido del inyector.

Dos grupos portátiles comprueban los registros v20 completos de imán, tope, aguja y accionamiento,
la repetición de todos los límites en el mismo runtime, la recodificación exacta, los recuentos tipados acotados, las
unidades/referencias incorrectas, las tablas ausentes/duplicadas y el rechazo de una rebaja v19 falsificada.
El SHA-256 del fixture auténtico v19 es
`4ff5f6180006d53be5ffb6ef0663cc6de4af45f35f39dc4d52fd7e00e8cdd8c5`;
la huella `4f74c6da6d89ab08` y la repetición actualizada en el mismo runtime siguen intactas.
El camino heredado de cuota ideal, los fixtures anteriores y las huellas físicas existentes
siguen siendo evidencia de regresión.

Tres grupos de integración más un servidor MCP hijo real verifican dimensiones/referencias
estrictas, la guía de la orden de voltaje en propiedad, las revisiones, la cancelación, las bifurcaciones
independientes y los libros magnéticos/de fuente/fase/térmicos. Los **65** límites de informe/portátil/MCP
coinciden. El laboratorio encendido de 0.6 s termina con:

| Magnitud | Valor |
|---|---:|
| Líquido entregado | 40.3903592511 mg |
| Presión del raíl | 692.292375330 kPa |
| Combustible evaporado | 34.5106221450 mg |
| Película líquida restante | 5.8797371061 mg |
| Vapor quemado | 31.2333851260 mg |
| Calor de reacción | 1374.2689455424 J |
| Trabajo de la alimentación eléctrica | 0.168028754073 J |
| Calor de cobre | 0.167510468341 J |
| Energía magnética | `6.30199e-16 J` |
| Orden mantenida de la bobina | 0 V |
| Levantamiento real de la aguja | 0.4842244877 micrómetros |
| Velocidad real de la aguja | -0.1290065607 m/s |
| Última petición retenida / dosis entregada real | 4 mg / 5.8930695115 mg |
| Residuo absoluto máximo de energía muestreado | `1.57642e-8 J` |
| Residuo absoluto máximo de masa muestreado | `3.30682e-18 kg` |
| Residuo absoluto máximo de combustible muestreado | `9.48677e-20 kg` |

El límite final aún tiene una aguja en movimiento, casi asentada, y una película evaporada de forma
incompleta. El experimento nuevo comprueba, por tanto, el inventario restante finito
y el libro de combustible completo, en lugar de heredar la condición de película seca del laboratorio
de inyector ideal. Aprobar los KPI numéricos no establece la dosis ordenada exacta.
La huella del modelo es `3fa813ff44b95a79`; el hash final es `de68045d420b9ffa`; el SHA-256 de la fuente
es `d8c7472a3335a523821209cb183cf34687e454f326d9ac1946c618b7ca73050c`.

Todos los parámetros siguen siendo `unverified`. La inductancia lineal no saturada, R constante,
la aguja equilibrada en presión, los topes elásticos y el accionamiento ideal de voltaje son reducciones
declaradas. Los mapas magnéticos/térmicos no lineales, el accionamiento por conmutación/flyback/batería,
las fuerzas axiales del fluido, la pulverización/el desplazamiento, la bomba/reposición del raíl, la ECU/TCU completa y los
grupos motopropulsores medidos siguen abiertos. Las vistas preparadas de bobina/tope/controlador en Unity y la
escala de carrera de la aguja exigen una verificación real de Editor/Play. Consulta
[NEEDLE_ACTUATION.md](NEEDLE_ACTUATION.es.md).

## 2026-10-01: raíl líquido flexible e inyección por ciclo

La orden en serie exigida, `dotnet run --file tools/Build.cs -- verify`, pasa
en local en Windows x64 con SDK 10.0.401/runtime 10.0.12: **275/275** comprobaciones
administradas, **215/215** comprobaciones de ensamblados Standard alojadas en .NET 10, **25/25** grupos
MCP reales, **16/16** pruebas Zig y **6/6** comprobaciones de ABI de C#. La compilación Release informa
cero advertencias/errores. Los **176** valores de referencia históricos coinciden exactamente;
la auditoría de fuentes encuentra cero archivos C/C++/Lua. Esto no establece una CI nueva de Linux/macOS
ni una aceptación real de Unity Editor/Play/Player/IL2CPP.

Archivos de evidencia:

- `artifacts/reports/liquid-injector-final-2026-10-01.log` — ejecución en serie completa.
- `artifacts/reports/liquid-injector-evidence-2026-10-01.json` — alcance legible por
  máquina, resumen del registro, huellas actuales de laboratorio/fuente, magnitudes finales y
  refinamiento independiente.
- `artifacts/reports/liquid-injector-schema-audit-2026-10-01.json` — los **22**
  laboratorios pasan jsonschema 4.25.1; **16** casos de inyector líquido malformados se rechazan.
- `artifacts/reports/liquid-injected-cylinder.json` — experimento y repetición completa.

Ocho grupos físicos comprueban el decaimiento analítico de la altura de presión, el inventario finito de la fuente
flexible, el trabajo exacto del raíl, el calor pasivo de la tobera, los libros calórico/químico/de presión,
el retenido de la cuota, el cierre por presión inversa y la inversión sin reemitir la cuota.
La inanición alcanza la presión prescrita del receptor sin inventar combustible.
Las bifurcaciones independientes, las entradas rechazadas/canceladas, el fallo tardío tras una inyección aceptada
y la captura especulativa de embrague conservan cada historial de fuente/película/cuota/calor.
La inyección activa en caliente y las instantáneas asignan **cero bytes administrados**. Se ejercitan las
propiedades explícitas, la viabilidad del volumen de la fuente, las unidades, la propiedad de película/cigüeñal, la capacidad
de estado acotada y la compilación inmutable.

Una ODE simultánea independiente de ocho estados integra el líquido entregado, la masa de película,
la temperatura de la pared, la masa/energía interna del gas receptor y tres historiales de presión/calor.
Las ejecuciones RK4 independientes a **20,000/40,000** pasos coinciden dentro de la tolerancia escalada
declarada `1e-10`. En 0.2 s, dentro de una ventana suave hacia adelante, los ticks físicos
de 20/10/5/2.5 ms dan errores escalados máximos:

| Tick | Error escalado máximo | Error anterior / error actual |
|---|---:|---:|
| 20 ms | `2.9306688569e-7` | — |
| 10 ms | `7.3266958993e-8` | 3.999987 |
| 5 ms | `1.8316757833e-8` | 3.999996 |
| 2.5 ms | `4.5791930144e-9` | 3.999997 |

Esto establece un acoplamiento suave de inyección/evaporación de segundo orden bajo la
reducción declarada. Los eventos de ventana a tick fijo, la inanición y otras fuentes explícitas de pared
conservan sus límites de exactitud aparte; no se afirma un orden uniforme del grupo motopropulsor encendido.

Dos grupos portátiles verifican los registros v19 de fuente/tobera/temporización, la recodificación exacta,
la repetición de cada límite, los recuentos tipados acotados, las unidades/la propiedad incorrectas, los registros duplicados/
ausentes y las rebajas v18 falsificadas. El fixture auténtico de película v18 conserva
el SHA-256 `c0e59c6f6527b2b59ee1094cd6627c0829ecbd4ab0eb2e6ec06394ccfe15da97`,
la huella `cb103bce098f4e82` y la repetición actualizada en el mismo runtime. Los fixtures anteriores
y la física sin película siguen siendo evidencia de regresión. La revisión también actualiza los desplazamientos
actuales de cabecera en las pruebas de registros malformados, y conserva los tamaños de tabla de la versión antigua.

Tres grupos de integración más un servidor MCP hijo real comprueban la fuente finita compartida,
la deposición líquida, la reacción solo de vapor, los balances de fuente/película/pared, los documentos estrictos
y las revisiones/la cancelación/las bifurcaciones de sesión. Los **65** límites de informe/portátil/MCP
coinciden. El laboratorio de 0.6 s empieza con una película seca y **500 mg** de
líquido a **800 kPa**, y termina con:

| Magnitud | Valor |
|---|---:|
| Líquido entregado y evaporado | 28 mg |
| Líquido restante en la fuente | 472 mg |
| Presión del raíl | 725.333333333 kPa |
| Líquido restante en la película | 0 mg |
| Trabajo de presión del raíl liberado | 0.028472888889 J |
| Trabajo de presión del receptor exportado | 0.003236038241 J |
| Calor de la tobera | 0.025236850648 J |
| Calor de película tomado de la pared | 14 J |
| Temperatura de la pared de la película | 498.602523685 K |
| Vapor quemado | 27.9759472237 mg |
| Calor de reacción | 1230.9416778442 J |
| Residuo absoluto máximo de energía muestreado | `5.52370e-9 J` |
| Residuo absoluto máximo de masa muestreado | `1.08420e-18 kg` |
| Residuo absoluto máximo de combustible muestreado | `7.45389e-20 kg` |

El ciclo final observado conserva una petición/entrega de **12 mg**, mientras que la orden viva
es **4 mg** para una ventana futura. La aceptación de la dosis, la entrega y la reacción
siguen siendo distintas. La huella del modelo es `4f74c6da6d89ab08`; el hash final es
`ddf5b1c4b0678451`; el SHA-256 de la fuente es
`85b9cba983e31258d9341943dab3c844a18c05c5320dda3d9d610a3935a14d09`.

La energía de presión del raíl es energía interna almacenada; el calor de la tobera entra en la pared
finita. El receptor de volumen líquido despreciable exporta de forma explícita el trabajo de presión de desplazamiento,
en lugar de acreditar un volumen de gas oculto o trabajo de cigüeñal. Su referencia de flexibilidad
a presión cero y sus propiedades constantes son reducciones de investigación declaradas.
Todos los parámetros siguen siendo `unverified`. La bomba/reposición, los mapas de contrapresión/propiedades,
la dinámica de aguja/eléctrica/de pulverización, el acoplamiento de volumen líquido finito, el encendido/la ECU,
la transmisión/el control completos y los grupos motopropulsores de vehículo calibrados siguen abiertos.
Las vistas de raíl/tobera en Unity y las pruebas de ciclo de vida están preparadas, pero exigen el Editor
fijado. Consulta [LIQUID_FUEL_INJECTION.md](LIQUID_FUEL_INJECTION.es.md).

## 2026-10-01: película finita de combustible líquido y transporte simétrico

La orden en serie exigida, `dotnet run --file tools/Build.cs -- verify`, pasa
en Windows x64 con SDK 10.0.401/runtime 10.0.12: **262/262** comprobaciones administradas,
**205/205** comprobaciones de ensamblados Standard alojadas en .NET 10, **24/24** grupos MCP reales,
**16/16** pruebas Zig y **6/6** comprobaciones de ABI de C#. Los **176** valores de referencia
históricos conservados coinciden exactamente. La auditoría de fuentes encuentra cero archivos C/C++/Lua. La compilación Release
informa cero advertencias/errores. Esta es evidencia local del árbol de trabajo, sin
CI nueva de Linux/macOS ni aceptación real de Unity Editor/Play/Player/IL2CPP.

Archivos de evidencia:

- `artifacts/reports/fuel-film-final-2026-10-01.log` — ejecución en serie completa.
- `artifacts/reports/fuel-film-evidence-2026-10-01.json` — alcance legible por máquina,
  resumen del registro, huellas de laboratorio, magnitudes de fase y errores de refinamiento.
- `artifacts/reports/fuel-film-schema-audit-2026-10-01.json` — los **21** documentos de
  laboratorio pasan jsonschema 4.25.1; **12** casos de película malformados se rechazan.
- `artifacts/reports/film-fired-cylinder.json` — experimento y repetición completa.

Diez grupos físicos verifican el calentamiento analítico de un baño finito, la referencia de fase con signo,
la saturación/el secado, la disponibilidad limitada de calor, el enfriamiento, la conductancia nula, la reacción
real solo de vapor y los libros independientes de constituyentes/químico/térmico. Las bifurcaciones
completas y los lotes cancelados y rechazados tarde conservan todos los historiales; el avance en caliente
y las lecturas de instantánea asignan **cero bytes administrados**. Se ejercitan los puertos/unidades incorrectos, las propiedades
de fase inválidas, el desbordamiento de la capacidad de estado y la compilación inmutable.

La revisión corrigió el segundo medio paso, de película/gas a gas/película, e invirtió
el barrido de película de la pared compartida. Esto hace simétricos película/gas/mecánica/gas/película, y
conserva el camino de solver existente para los modelos sin película. Las referencias RK4 simultáneas
independientes a 20,000 y 40,000 pasos coinciden dentro de la tolerancia escalada declarada `1e-10`.
Un estudio de dos segundos de película saturada suave usa ticks físicos de 40/20/10/5 ms
y el error escalado máximo entre la masa de líquido/gas, la temperatura de la pared, la energía
interna del gas y los constituyentes transportados:

| Acoplamiento | Error a 40 ms | Error a 5 ms | Razones de reducción sucesiva |
|---|---:|---:|---|
| Dos películas que comparten una pared finita | `3.43324e-8` | `5.36263e-10` | 4.0000, 4.0002, 4.0012 |
| Vapor de película que sale por un puerto de gas bloqueado | `3.17195e-5` | `4.89033e-7` | 4.0495, 4.0029, 4.0014 |
| Película más intercambio de calor gas-pared | `5.30200e-4` | `6.61364e-5` | 2.0024, 2.0012, 2.0006 |

Los casos de película compartida y de transporte de vapor muestran un refinamiento suave de segundo orden. Otras
fuentes de calor de pared conservan la temperatura explícita del intervalo exterior y el límite de
acoplamiento de primer orden. Estas comprobaciones no establecen un segundo orden uniforme a través del secado,
los eventos de válvula/reacción o un grupo motopropulsor encendido completo.

Dos grupos de assets verifican las magnitudes de fase v18, la codificación determinista, la repetición de cada
límite, los registros tipados acotados, las unidades/recuentos malformados, los registros duplicados/ausentes
y el rechazo de una rebaja v17 falsificada. El fixture auténtico de dosificación v17, con SHA-256
`d51dc0968bc3d154b2c3b227f74b7210215d4ee00c5a47e8dd16dc1d71cd5da1`,
conserva la huella `099db1021c8df1fe` y la repetición actualizada en el mismo runtime. Los fixtures
anteriores y las huellas de los modelos sin película siguen siendo evidencia de regresión.

Tres grupos de integración y el servidor MCP hijo real verifican el mismo inventario
finito, la energía de fase, la reacción solo de vapor, los documentos estrictos y el comportamiento de revisión/bifurcación/
cancelación. Los **63** límites JSON/informe/portátil/MCP coinciden. El
laboratorio de 0.6 segundos empieza con **40 mg** de mojado explícito y termina con:

| Magnitud | Valor |
|---|---:|
| Líquido restante | 0 mg |
| Combustible evaporado | 40 mg |
| Calor tomado de la pared finita | 20 J |
| Temperatura de la pared de la película | 498 K |
| Vapor quemado | 31.1117599551 mg |
| Calor de reacción | 1368.9174380261 J |
| Residuo final de energía | `-1.58434e-9 J` |
| Residuo absoluto máximo de energía muestreado | `2.15960e-9 J` |
| Residuo absoluto máximo de masa muestreado | `1.49078e-18 kg` |
| Residuo absoluto máximo de combustible muestreado | `2.09641e-19 kg` |

La huella del modelo es `cb103bce098f4e82`; el hash final es `495582b10f40832c`.
El SHA-256 de la fuente es
`8c933a5889d3b8f22f37738e307989d2c0fa5c7dfe9018ffe50a21f19e2aa416`.
Todos los parámetros siguen siendo `unverified`. El mojado inicial no es inyección líquida;
las propiedades de fase dependientes de la presión, la reposición, el encendido/la ECU, la
transmisión/los controles completos y los grupos motopropulsores medidos siguen abiertos. Los marcadores de película preparados en Unity
y las pruebas de ciclo de vida necesitan el Editor fijado. Consulta [FUEL_FILM.md](FUEL_FILM.es.md).

## 2026-09-30: raíl de combustible finito y dosificación por ciclo

El punto de control publicado `dc7ec2d` del acumulador de gas pasa la
[CI de Windows/Linux/macOS](https://github.com/Water-Run/Power/actions/runs/36710249585).
Esa evidencia cubre la fuente precedente, no el inyector nuevo ni Unity real.

La orden en serie exigida, `dotnet run --file tools/Build.cs -- verify`, pasa
en local en Windows x64 con SDK 10.0.401/runtime 10.0.12: **247/247** comprobaciones administradas,
**193/193** comprobaciones de ensamblados Standard alojadas en .NET 10, **23/23** grupos MCP reales,
**16/16** Zig y **6/6** comprobaciones de ABI de C#. Los **176** valores numéricos históricos
coinciden exactamente; la auditoría de fuentes encuentra cero archivos C/C++/Lua. La compilación Release tiene cero
advertencias/errores. Registro: `artifacts/reports/fuel-injector-final-2026-09-30.log`.

Ocho grupos físicos verifican la temporización hacia adelante y con cierre de ciclo, el raíl finito y la cuota exacta,
las peticiones retenidas, el cierre por presión inversa y la inanición, la inversión sin reemitir la cuota,
una ODE independiente de masa/entalpía de dos recipientes con refinamiento suave, el quemado
premmezclado dosificado analítico, las transacciones completas, las dimensiones/la compatibilidad/la capacidad y
la geometría inmutable. El avance en caliente y las instantáneas asignan **cero bytes administrados**.
El fallo tardío de par/volumen, la cancelación, el rechazo de entradas, el lote exacto y las bifurcaciones
conservan todos los historiales de entrega/ordinal. Las discontinuidades de temporización conservan los requisitos de
refinamiento a tick fijo; no se afirma un tiempo de conmutación continuo.

Dos grupos portátiles verifican la tobera/temporización v17, los registros tipados acotados, las unidades incorrectas,
los registros ausentes/duplicados y el rechazo de una rebaja falsificada. El fixture auténtico del acumulador de gas v16 conserva el SHA-256
`72f0ee5b3180b763de091674565b2b8f2cbd61f80a3e4ab46e9694ad153a79c7`, la huella
`739f2baba8c669a0` y la repetición actualizada de cada límite en el mismo runtime. Los fixtures anteriores
y las huellas físicas siguen sin cambios. Tres grupos de integración verifican el agotamiento real
del raíl, el combustible entregado/reaccionado/de contorno, los contratos estrictos y las sesiones.

Los **20** documentos de laboratorio pasan la validación estructural del esquema; **12** casos de
inyector malformados son rechazados por jsonschema 4.25.1. Los valores de ciclo, los puertos finitos compatibles,
los máximos de dosis y la propiedad del cigüeñal siguen siendo comprobaciones adicionales del compilador. Informe:
`artifacts/reports/fuel-injector-schema-audit.json`. Las auditorías de esquema existentes también pasan
los 20 documentos. Un receptor de depósito inválido devuelve ahora un diagnóstico de conexión
antes de la validación de la fracción del depósito. Se rechazan R/gamma/LHV/estequiometría distintos,
de modo que las transferencias internas no pueden inventar inventario químico.

El `metered-fired-cylinder` sustituye la admisión de combustible premmezclado por aire puro más un
raíl gaseoso finito. Las dosis pedidas son 8/12/4 mg; se retienen en la siguiente ventana
hacia adelante. A 0.6 s, el último ciclo observado aún mantiene 12 mg, mientras que la orden viva
es 4 mg para una ventana futura. La ejecución y la aceptación de la dosis siguen siendo distintas de
la entrega real. Los **65** límites de informe/portátil/MCP coinciden exactamente.

| Magnitud final | Valor |
|---|---:|
| Combustible entregado | 28 mg |
| Combustible quemado | 27.9299615615 mg |
| Calor de reacción liberado | 1228.918308706072 J |
| Combustible restante en la cámara | 0.0055539661 mg |
| Combustible neto de contorno | -0.0644844724 mg |

El combustible quemado, el restante y el perdido por el contorno dan cuenta del combustible entregado. La transferencia interna del raíl
no añade una entrada externa de energía de combustible ni una fuente ilimitada. La entalpía térmica
del raíl y la energía química usan el mismo flujo limitado de constituyentes.

| Error absoluto máximo a lo largo del experimento | Valor | Límite afirmado |
|---|---:|---:|
| Agotamiento del raíl frente a la entrega | 3.67e-18 kg | 1e-16 kg |
| Entregado frente a combustible reaccionado/restante/de contorno | 6.78e-21 kg | 1e-14 kg |
| Energía del modelo completo | 1.52e-9 J | 1e-6 J |
| Masa total | 4.07e-18 kg | 1e-14 kg |
| Constituyente de combustible | 3.67e-18 kg | 1e-14 kg |
| Constituyente de aire fresco | 1.20e-18 kg | 1e-14 kg |

Huella `099db1021c8df1fe`; hash final de Windows/runtime `329e1109392b37f3`.
Informes: `artifacts/reports/metered-fired-cylinder.json` y
`fuel-injector-evidence-summary.json`.

Tres ejecuciones de CLI en serie, cada una con dos trayectorias de 0.6 s (24,000 ticks aceptados),
repetición completa y 65 límites, tardan **0.502 / 0.364 / 0.370 s**, mediana **0.370 s**,
con carga ordinaria de escritorio. Las trazas coinciden exactamente; el avance en caliente sin asignaciones se
comprueba aparte contra ambos ensamblados. Este es un coste local observado, no una
aceleración ni una garantía portable de rendimiento.

El agente 0.20.0 y el MCP real verifican el descubrimiento de la dosis en kg, la exportación/repetición completa, el rechazo
de una cuota inválida y las bifurcaciones independientes de controlador/combustible. Las vistas de inyector/raíl/temporización en Unity
y las pruebas Edit/Play están preparadas en fuente C# 9. Unity Editor/Play/Mono/IL2CPP/Player reales
y la evidencia nueva en tres plataformas de este incremento siguen
aparte. Esto es dosificación gaseosa ideal con propiedades de gas constantes comunes;
la pulverización/evaporación líquida, el hardware de aguja/raíl/depósito, la inyección de gasolina calibrada,
el encendido/la ECU y el grupo motopropulsor completo siguen inacabados. Los parámetros no están verificados.

## 2026-09-30: pistón de gas y acumulador de energía finita

El desarrollo se reanudó a petición del propietario. El punto de control anterior de la corredera `cebc978`
pasó la [CI de Windows, Linux y macOS](https://github.com/Water-Run/Power/actions/runs/36695753045);
cada trabajo de la matriz completó su verificación en serie exigida. Registro descargado:
`artifacts/reports/spool-three-platform-2026-09-30.log`. Esa evidencia cubre la
fuente publicada de la corredera, no el incremento nuevo del pistón de gas ni Unity real.

La orden en serie nueva exigida pasa en Windows x64 local:
`dotnet run --file tools/Build.cs -- verify`. Resultados: **234/234** comprobaciones administradas,
**183/183** comprobaciones de ensamblados Standard alojadas en .NET 10, **22/22** grupos MCP reales,
**16/16** Zig y **6/6** comprobaciones de ABI de C#. Los **176** valores históricos coinciden exactamente;
la auditoría de fuentes encuentra cero archivos C/C++/Lua. La compilación Release tiene cero advertencias/errores.
Registro: `artifacts/reports/gas-accumulator-final-2026-09-30.log`.

Ocho grupos físicos cubren el trabajo adiabático analítico y su jacobiano, el recorrido pequeño,
las cámaras con signo/opuestas, una RK4 independiente de masa/energía y el refinamiento suave de segundo
orden, el movimiento común de gas/fluido, una referencia RK4 aparte de pared finita con
refinamiento acoplado a la pared de primer orden, la entrada de gas de volumen móvil y la entalpía del depósito,
las transacciones completas, la geometría/las unidades y el avance sin asignaciones. El libro de entrada
midió alrededor de `1.14e-17 kg` de residuo acumulado de coma flotante tras actualizaciones repetidas
de masa; su cota `1e-16 kg` refleja esa acumulación. Cerrar el puerto
conserva la masa aceptada con exactitud. Ninguna corrección de masa o energía fuerza un aprobado.

Dos grupos portátiles conservan la geometría v16 completa, la dirección con signo, los recuentos tipados
acotados, las unidades malformadas, los registros ausentes/duplicados y el rechazo de la rebaja. El
fixture auténtico de corredera v15 coincide byte a byte con el paquete del punto de control publicado:
SHA-256 `67b976a27ca0bd7343ca6b024c5bc736e14f48326945da841785ade43b61f14b`,
huella `28aa0965d248e280`. Todos los límites de evento originales/actualizados coinciden dentro
del runtime en ejecución. Los fixtures anteriores y las huellas físicas siguen intactos.

Los **19** documentos de laboratorio pasan la validación del esquema. **12** documentos de pistón de gas
malformados son rechazados aparte por jsonschema 4.25.1. El volumen nominal positivo y
la propiedad única de la geometría siguen siendo comprobaciones adicionales del compilador. Los informes están bajo
`artifacts/reports`, incluido `gas-piston-schema-audit.json`; las auditorías existentes de pistón,
corredera, batería/ciclo de trabajo y controlador de presión también pasan los 19 documentos.

El modelo `gas-accumulator-pump` de seis segundos añade una cámara de gas de 50 ml y un separador de 50 g
a la bomba eléctrica, la derivación mecánica de corredera y el embrague de presión. El gas
empieza a 200 kPa absolutos/300 K, con una referencia declarada de 100 kPa y una penetración
de asiento flexible de 0.1 mm. Durante el pulso de 3-4 s, el voltaje del motor es 6 V y ambos
caminos de llenado/drenaje están abiertos de forma explícita. La energía interna del gas baja **0.6196209894 J**;
el trabajo a la presión de referencia es **-0.2094578629 J**. Tras la energía cinética/de tope del separador
y el amortiguamiento, la entrega neta al líquido es **0.4092090634 J**, con **2.094578629 ml**
de volumen barrido devuelto. El llenado se reanuda y el embrague queda bloqueado con deslizamiento nulo
en el límite final. Son resultados sintéticos, no una calibración OEM.

| Error absoluto máximo en los 306 límites | Valor | Límite afirmado |
|---|---:|---:|
| Cuenta separada de bomba/fluido/movimiento/gas/referencia/calor | 6.09e-13 J | 1e-8 J |
| Energía del modelo completo | 1.83e-8 J | 1e-6 J |
| Cuenta del volumen de referencia del líquido | 5.19e-19 m3 | 1e-16 m3 |
| Invariante adiabático normalizado del gas cerrado | 6.83e-13 J | 1e-8 J |

Cada uno de los **306** límites JSON/portátil/MCP tiene hashes y valores idénticos. Huella
`739f2baba8c669a0`; hash final de Windows/runtime `074917dc8e343131`. Informes:
`artifacts/reports/gas-accumulator-pump.json` y `gas-accumulator-evidence-summary.json`.
La cuenta de gas/fluido resta de forma explícita el trabajo de referencia y el potencial inicial del
tope; la energía interna absoluta del gas por sí sola no se etiqueta como energía de fluido entregada.

### Optimización medida de la cámara cerrada

Los modelos nuevos de pistón de gas cerrados y sin mezcla, sin transporte de gas ni enlaces de calor, conservan
la validación de estado y omiten la integración de tasa nula. Tres ejecuciones completas de CLI en serie por
etapa incluyen dos trayectorias de seis segundos (600,000 ticks aceptados) y los 306
límites. Los tiempos de referencia fueron **2.727 / 2.603 / 2.496 s**; los tiempos optimizados fueron
**2.382 / 2.362 / 2.357 s**. El coste mediano baja alrededor de un **9.3%** en este escritorio.
Las tres trazas de antes/después coinciden en cada observable y cada hash de estado con exactitud.
La asignación en régimen de Step/ReadSnapshot sigue en **cero bytes administrados**. Esta es una medición
local, no una garantía portable de rendimiento. Los puertos, los enlaces de pared y el transporte de constituyentes
conservan su camino de integración ordinario y sus pruebas independientes.

El agente 0.19.0/MCP verifica el descubrimiento termodinámico, la exportación/repetición completa, las revisiones
y las bifurcaciones independientes de gas/fluido. Las vistas de Studio y las pruebas Edit/Play están preparadas en
fuente C# 9. Unity Editor/Play/Mono/IL2CPP/Player reales y la verificación nueva en tres plataformas
de este incremento siguen aparte. El motor completo,
la transmisión, la ECU/TCU, las muestras de vehículo calibradas y la aplicación de escritorio aceptada
siguen inacabados. Se conservan todos los límites de las muestras, las licencias y la procedencia.

## 2026-09-30: punto de control del regulador mecánico de corredera

La orden en serie exigida pasa en Windows x64, SDK 10.0.401/runtime 10.0.12:
`dotnet run --file tools/Build.cs -- verify`. El resultado es **221/221** comprobaciones
administradas, **173/173** comprobaciones de ensamblados Standard alojadas en .NET 10, **21/21** grupos
reales de servidor MCP hijo, **16/16** Zig y **6/6** comprobaciones de ABI de C#. Los **176**
valores numéricos históricos coinciden exactamente. La compilación Release tiene cero advertencias/
errores; la auditoría de fuentes encuentra cero archivos C/C++/Lua. Registro:
`artifacts/reports/spool-final-2026-09-30.log`.

El [contrato de la corredera](HYDRAULIC_SPOOL.es.md) añade un escalón de dosificación equilibrado en presión,
que desprecia de forma explícita la fuerza axial del chorro. La posición real del pistón y las presiones de ambos puertos
de fluido participan en la resolución de Newton compartida con derivadas analíticas. Siete
grupos físicos verifican el recorrido con signo, el caudal bidireccional pasivo y las derivadas,
la presión estacionaria independiente, un transitorio RK4 aparte de tres estados con refinamiento suave
de segundo orden, el error decreciente a través de la apertura, la igualación de puertos finitos,
las transacciones y la geometría inmutable. La asignación en caliente de Step/ReadSnapshot sigue en cero
contra ambos destinos del núcleo. El fallo tardío, la cancelación y las bifurcaciones independientes conservan
todo el estado de presión/movimiento/calor. No se afirma un orden uniforme no suave.

Dos grupos portátiles cubren la geometría v15 completa y los recuentos/tipos/unidades malformados falsificados,
los registros ausentes/duplicados y el rechazo de la rebaja. El SHA-256 del fixture auténtico de pistón v14 es
`72e0605d00972a58e2f5358aa8cbd44417e81cf1c452a20ab9661eac0919bae0`; su huella,
las referencias físicas y la repetición actualizada exacta en el mismo runtime conservan cada límite de
evento. Los fixtures anteriores están intactos. Tres grupos de integración verifican documentos
estrictos, revisiones/cancelación/bifurcaciones de sesión y un libro aparte de fluido/movimiento.
Las unidades incorrectas de la posición de dosificación conservan el diagnóstico de unidades, en lugar de capturarse
como errores de rango del constructor.

Los **18** documentos de laboratorio pasan la validación estructural del esquema. **12** casos de
corredera malformados son rechazados de forma independiente por jsonschema 4.25.1. Informe:
`artifacts/reports/spool-schema-audit.json`. El recorrido con signo distinto de cero, las posiciones del escalón
dentro de la carrera del pistón y la propiedad tipada siguen siendo comprobaciones adicionales del compilador.

El experimento `spool-regulated-pump` dura **3 s**, con ticks de **20,000 ns** y
**156** límites JSON/portátil/MCP coincidentes. Su actuador de presión móvil, el muelle
de retorno/amortiguamiento y la derivación regulan la línea sin un controlador de válvula muestreado.
El accionamiento/freno de 2 N*m son cargas de investigación explícitas. El criterio heredado de captura
a alta presión en dos segundos falló a la presión regulada más baja; el
experimento ahora dura lo bastante para observar la captura real, y conserva las afirmaciones de deslizamiento nulo
y de modo bloqueado. No se corrige ningún estado de presión, energía o deslizamiento para aprobar.

| Magnitud final | Valor |
|---|---:|
| Presión de línea | 233956.17402528782 Pa |
| Desplazamiento de la corredera | 0.00016975695474013045 m |
| Apertura de dosificación | 0.08487847737006522 |
| Calor de restricción de la corredera | 2.917614235503143 J |
| Calor de amortiguamiento de retorno | 0.0006249365922203377 J |
| Trabajo hidráulico de la bomba | 3.3299555096992406 J |
| Calor del embrague | 260.2402810714738 J |
| Modo/deslizamiento final del embrague | Locked / 0 rad/s |

En todos los límites, el balance separado de hidráulica/movimiento/muelle/pastilla/calor tiene
un error máximo de **9.77e-15 J** (límite afirmado 1e-8 J), la energía global **1.99e-8 J**
(límite 1e-6 J) y el inventario de volumen de referencia **1.35e-20 m3** (límite 1e-16 m3).
Huella `28aa0965d248e280`, hash final de Windows/runtime `180744d025212ef7`.
Informes: `artifacts/reports/spool-regulated-pump.json` y
`artifacts/reports/spool-evidence-summary.json`.

Tres ejecuciones de CLI en serie, cada una con dos trayectorias completas (300,000 ticks
aceptados), todas las comprobaciones de repetición y 156 límites de salida, tardaron **1.183 / 1.230 / 1.153 s**;
mediana **1.183 s** con carga ordinaria de escritorio. Este es un coste observado del punto de control,
no una garantía de rendimiento entre plataformas ni evidencia de aceleración. Los búferes de matriz, pendiente del escalón
y reversión están acotados y pertenecen a la simulación; el avance/relectura en régimen
siguen sin asignaciones.

El agente 0.18.0 anuncia la hidráulica regulada mecánicamente, las unidades de geometría y la
omisión de la fuerza de chorro. Las comprobaciones MCP reales cubren la exportación/repetición completa y el rechazo de
un intento de escritura en la salida de apertura. Las vistas de válvula/actuador en Unity y las pruebas Edit/Play
están preparadas en fuente C# 9. `POWER_UNITY_EDITOR` no está definido; Editor/Play/Mono/IL2CPP/Player reales
y la verificación nueva de Linux/macOS siguen pendientes. El propietario cierra
el desarrollo de hoy en este punto de control numérico; no se afirma un Power! completo ni una calibración.
La entrega de fuentes y paquetes conserva las licencias y los límites de las muestras.

## 2026-09-30: pistón dinámico y embrague accionado por contacto

La orden en serie exigida pasó en Windows x64, SDK 10.0.401/runtime 10.0.12:

```sh
dotnet run --file tools/Build.cs -- verify
```

- **209/209** comprobaciones administradas, **164/164** comprobaciones de ensamblados Standard alojadas en .NET 10
  y **20/20** grupos MCP de servidor hijo real.
- **16/16** Zig y **6/6** comprobaciones de ABI de C#; los **176** valores históricos coinciden exactamente.
  La auditoría de fuentes encuentra cero archivos C/C++/Lua. Compilación Release: cero advertencias/errores.
- Registro: `artifacts/reports/piston-final-2026-09-30.log`.
- Los **17** laboratorios pasan la validación JSON Schema. Auditorías aparte rechazan cada una
  **12** casos malformados de pistón, batería/ciclo de trabajo y control de voltaje con jsonschema
  4.25.1. Los informes son `piston-schema-audit.json`, `battery-schema-audit.json` y
  `pressure-controller-schema-audit.json`, bajo `artifacts/reports`.

Los [pistones hidráulicos](HYDRAULIC_PISTON.es.md) añaden masa, desplazamiento, volumen
barrido de la cámara, muelle/amortiguamiento, holgura de pastilla y extremos de carrera flexibles explícitos. Los embragues de
contacto derivan la capacidad de la fuerza de la pastilla. Ocho grupos de comprobación física cubren el trabajo de bisagra
y las derivadas analíticas, historiales compactos aparte de amortiguamiento/decaimiento analítico,
la oscilación acoplada muelle/fluido con refinamiento suave de segundo orden, el trabajo y el volumen de un depósito finito/trasero,
el refinamiento RK4 a trozos del contacto, el llenado/la captura/la liberación libres,
los contratos tipados y las transacciones completas. No se afirma un orden uniforme a través de los eventos de
contacto. El fallo numérico tardío, la cancelación, las bifurcaciones y el lote exacto conservan el
estado completo. El avance en caliente del núcleo y las lecturas de instantánea asignan **cero bytes administrados**.

El laboratorio sintético `piston-actuated-clutch` avanza **15 s** con ticks de **20,000 ns**
y control de ciclo de trabajo muestreado a **5 ms**. Los **761** límites de informe/portátil/MCP
tienen hashes y valores observables idénticos en este runtime. La presión durante el llenado
libre no produce fuerza de pastilla. El drenaje programado libera el embrague; el llenado posterior
lo captura. En el límite final:

| Magnitud | Valor |
|---|---:|
| Desplazamiento del pistón | 0.0021772797986273195 m |
| Fuerza de la pastilla | 177.27979862731945 N |
| Capacidades estática/deslizante | 22.69181422429689 / 11.345907112148446 N*m |
| Presión de la cámara delantera | 199053.02928772685 Pa |
| Energía almacenada de pastilla/tope | 0.01571406350067147 J |
| Calor acumulado de amortiguamiento de retorno | 0.0025718766359138913 J |

Huella `46f746398142c258`; hash final de Windows/runtime `3a7b8eee248785d3`.
Informe: `artifacts/reports/piston-actuated-clutch.json`. La fuente usa de forma explícita
un amortiguamiento sintético de 300 N*s/m para mantener la alimentación suficiente durante el transitorio de contacto;
el solver conserva el rechazo de presión negativa en lugar de recortar ese estado.

### Cuentas de energía independientes y evidencia de tolerancia

Cada límite compara de forma independiente el trabajo de la bomba con el fluido flexible, la cinética del deslizador,
la energía del muelle de retorno y de la pastilla más el calor de restricción/amortiguamiento; la pérdida química/RC de la batería
con la energía cinética/inductiva del motor, el trabajo de la bomba y el calor eléctrico; y el trabajo
rotacional externo con la energía de los rotores y el calor del embrague.

| Cuenta | Error absoluto máximo a lo largo del experimento | Límite afirmado |
|---|---:|---:|
| Hidráulica, movimiento y contacto | 1.56e-13 J | 1e-9 J |
| Alimentación eléctrica y motor | 2.48e-8 J | 1e-7 J |
| Nodo térmico compartido frente a historiales directos de calor | 2.87e-7 J | 5e-7 J |
| Rotores accionados y embrague | 1.16e-6 J | 2e-6 J |
| Modelo completo | 1.42e-6 J | 5e-6 J |

La afirmación hidráulica inicial de 1e-7 J infería un calor de muelle minúsculo al restar
el gran calor del embrague de la temperatura redondeada del nodo compartido. Falló, y ese calor inferido
incluso disminuía cerca del movimiento estacionario. Un canal directo de amortiguamiento compensado
conserva ahora la disipación física de forma independiente; el balance hidráulico más estricto de arriba
pasa. La acumulación térmica y rotacional explica el residuo global restante.
El umbral global heredado de 1e-6 J era insuficiente para esta ejecución de 750,000 ticks;
5e-6 J es una cota numérica explícita de ejecución larga, complementada por las comprobaciones analíticas,
de volumen de fluido y de energía separada, más estrictas. No se corrige ningún estado de energía para forzar un aprobado.
Auditoría complementaria: `artifacts/reports/piston-evidence-summary.json`.

### Alcance del rendimiento

Tres ejecuciones de CLI en serie, con carga ordinaria de escritorio, incluyen cada una **dos** trayectorias
completas (1.5 millones de ticks aceptados), comprobaciones de repetición y 761 límites de salida.
Las medianas transcurridas fueron **4.007 s** antes de la derivada analítica y los historiales de amortiguamiento,
**4.100 s** con la derivada analítica y los historiales indexados por componente completo, y
**4.234 s** con historiales compactos. Las tres últimas ejecuciones fueron 4.234, 4.088 y 4.928 s.
Estas mediciones no establecen una aceleración ni una garantía portable de rendimiento.
El jacobiano analítico elimina la perturbación de longitud física y las evaluaciones repetidas de contacto;
los historiales compactos asignan y copian solo las ranuras reales de muelle. El espacio de trabajo
denso y los factores LU siguen acotados, en caché y pertenecientes a la simulación. El avance
en régimen logrado y las lecturas de instantánea conservan la afirmación de cero asignaciones.

El asset v14 almacena el contorno del pistón/trasero, los parámetros de carrera/pastilla y la geometría de fricción
referenciada. Se rechazan los recuentos, la cobertura, las unidades/tipos malformados, los registros duplicados/ausentes y
las rebajas falsificadas. El fixture auténtico de batería v13 conserva el SHA-256
`67e7bde4dfcc2b8b41f49cadae8f89ff5fc5a1668059e084b7b5d9da02c7c44a`, las referencias
físicas y la repetición actualizada exacta en el mismo runtime. Los fixtures anteriores siguen intactos.
El agente 0.17.0/MCP ejercita el descubrimiento, la validación, cada límite exportado, la fuerza de
contacto, los conflictos de revisión y las bifurcaciones independientes.

Las vistas de deslizador/contacto en Unity y las pruebas Edit/Play están preparadas en fuente C# 9. El
host de comprobación Standard es .NET 10; no ejercita el Editor de Unity. `POWER_UNITY_EDITOR`
no está definido. La evidencia de Editor/Play/Mono/IL2CPP/Player y la nueva de Linux/macOS siguen pendientes.
Los parámetros son entradas de investigación no verificadas; el motor completo, la transmisión,
la ECU/TCU y las muestras de vehículo calibradas siguen inacabados.

## 2026-09-30: alimentación finita de batería y regulación por ciclo de trabajo

La orden en serie exigida pasó en Windows x64, SDK 10.0.401/runtime 10.0.12:

```sh
dotnet run --file tools/Build.cs -- verify
```

- **196/196** comprobaciones administradas, **154/154** comprobaciones de ensamblados Standard alojadas en .NET 10
  y **19/19** grupos MCP de servidor hijo real.
- **16/16** Zig y **6/6** comprobaciones de ABI de C#; los **176** valores históricos coinciden exactamente.
  La auditoría de fuentes encuentra cero archivos C/C++/Lua. Compilación Release: cero advertencias/errores.
- Registro: `artifacts/reports/battery-final-2026-09-30.log`.
- Los **16** laboratorios pasan JSON Schema; **12** casos malformados de batería/ciclo de trabajo son
  rechazados por jsonschema 4.25.1 en una caché aislada e ignorada. Informe complementario:
  `artifacts/reports/battery-schema-audit.json`.

Los nodos de batería de capacidad finita añaden OCV/SOC afín y una rama RC de polarización.
Los motores de batería usan un transformador bidireccional de ciclo de trabajo promediado; las cargas resistivas
de accesorios conmutadas comparten la resistencia del bus. La energía química/RC y la inductiva del motor, el calor de batería/
cobre/carga y las transferencias mecánicas/hidráulicas comparten el libro de conservación.
El trabajo del motor de batería es interno, no un trabajo de fuente externa duplicado. Las comprobaciones numéricas
iniciales revelaron que faltaba la energía inductiva del motor de batería; el libro ahora la incluye.

La evidencia cubre el decaimiento analítico de polarización en vacío y la respuesta RC con carga resistiva,
el inventario de carga, la integración RK4 independiente de cuatro estados del motor/batería y el refinamiento suave
de segundo orden. El ciclo de trabajo con signo, la carga regenerativa, la equivalencia de devanados en paralelo,
las respuestas engranadas dependientes del ciclo de trabajo, el acoplamiento conjunto de embrague/bomba, la reversión completa del
agotamiento tardío, la cancelación, el rechazo de entradas, las bifurcaciones independientes, la repetición por lotes y
cero asignaciones pasan contra ambos ensamblados. Los factores de runtime y las respuestas
mecánicas siguen perteneciendo a la simulación y se actualizan cuando cambian los ciclos de trabajo o las aperturas de carga.

La bomba regulada por batería usa ticks físicos de 100 µs y un regulador de ciclo de trabajo de 5 ms.
Los pulsos de accesorios provocan una caída del bus medida en la simulación. Los **761** límites de informe/portátil/MCP
coinciden. A 15 s el SOC es **0.6269678451**, la carga restante **31.3483922575 C**,
el voltaje de terminal **12.6056975839 V**, la polarización **0.0207555849 V** y la corriente de descarga
**0.2052072574 A**. La presión termina en **200828.2935823 Pa** para un objetivo de 200000 Pa.
El último error muestreado es **-825.4441034 Pa**; la integral y el ciclo de trabajo mantenido son
**0.0642978516 / 0.0629221114**. El residuo final de energía es de alrededor de `2.13e-7 J`.
Huella `40d4fcbab9cad8f8`, hash final de Windows/runtime `803d9adef384cd35`.
Informe: `artifacts/reports/battery-regulated-pump.json`. La capacidad de carga de 50 C es
de forma explícita un inventario sintético pequeño de verificación, no una medición de batería OEM.
Cada límite comprueba la energía eléctrica aislada y la ausencia de trabajo de fuente duplicado.

El asset v13 conserva los parámetros de OCV/resistencia/capacidad/calor de la batería y el periodo/las ganancias/las cotas/la integral
inicial del control de ciclo de trabajo. Se rechazan los tipos, recuentos, unidades, extensiones ausentes/duplicadas
refirmados y malformados, y las rebajas falsificadas. El fixture auténtico de bomba regulada v12
conserva su resumen, su huella, las referencias físicas y la repetición actualizada en el mismo runtime.
Los fixtures anteriores y las huellas de los modelos sin control siguen sin cambios.
El agente 0.16.0/MCP verifica el descubrimiento de la batería, la repetición completa del experimento/la exportación, la propiedad
del ciclo de trabajo, las escrituras inválidas de accesorios, las revisiones y las bifurcaciones independientes de batería/control.

Las vistas de batería/eléctrica/ciclo de trabajo en Unity y las pruebas de ciclo de vida de importación/Play están preparadas.
La evidencia real de Editor/Play/Mono/IL2CPP/Player y las comprobaciones nuevas de Linux/macOS siguen pendientes.
Los parámetros afines constantes de la batería, el convertidor ideal y las programaciones prescritas de accesorios/válvulas
no establecen BMS/química/envejecimiento, control PWM/de corriente, contactores/fallos,
mecánica de actuadores, ECU/TCU completa, DCT/AT completa, el comportamiento restante del motor ni la
calibración del vehículo. Todos los parámetros siguen siendo `unverified`; el objetivo completo de Power! sigue abierto.

## 2026-09-30: punto de control de presión muestreada

La orden en serie exigida pasó en Windows x64 con SDK 10.0.401/runtime 10.0.12:

```sh
dotnet run --file tools/Build.cs -- verify
```

- **185/185** comprobaciones administradas, **146/146** comprobaciones de ensamblados Standard alojadas en .NET 10
  y **18/18** grupos de integración MCP de servidor hijo real.
- **16/16** Zig y **6/6** comprobaciones de ABI de C#; los **176** valores históricos coinciden exactamente.
  La auditoría de fuentes encuentra cero archivos C/C++/Lua. Compilación Release: cero advertencias/errores.
- Registro: `artifacts/reports/pressure-control-final-2026-09-30.log`.
- Los **15** laboratorios pasan el JSON Schema del modelo; **12** casos de controlador malformados
  se rechazan de forma estructural. La auditoría complementaria usa jsonschema 4.25.1 en
  una caché aislada e ignorada. Informe: `artifacts/reports/pressure-controller-schema-audit.json`.

`pressure_controller` lee un nodo hidráulico y posee una entrada de voltaje de motor CC.
Las muestras ocurren en el instante cero y en múltiplos enteros de un periodo alineado a ticks; el voltaje
se mantiene entre muestras. La primera muestra conserva la integral inicial suministrada.
La integración condicional evita incrementos que penetren más en la saturación de voltaje. Cuatro
estados del controlador y la entrada mantenida participan en la reversión completa, la cancelación,
las bifurcaciones, los hashes y el avance sin asignaciones. Las anulaciones externas de voltaje se rechazan
con errores accionables `controlled_input`. Consulta [el contrato](HYDRAULIC_PUMP.es.md#sampled-pressure-regulation).

La evidencia numérica independiente incluye una planta muestreada de motor/eje/presión con PI/RK4,
programada aparte. Reducir a la mitad los ticks físicos con un periodo de control fijo de 10 ms
muestra una convergencia suave de segundo orden hacia esa referencia muestreada; esto no afirma
una convergencia de segundo orden hacia un controlador en tiempo continuo. Las pruebas de presión constante
exacta verifican la retención de muestra, el orden de los extremos de evento y la fase de reloj de la bifurcación.
La saturación y el desenrollado, las unidades/periodos/propiedad malformados, la capacidad de estado, la reversión
por fallo aritmético tardío, la cancelación, la memoria independiente y las comprobaciones de asignación
se ejecutan contra ambos ensamblados destino del núcleo. Un objetivo inalcanzable se ejecuta y se repite
con éxito, pero falla los KPI de seguimiento mientras la orden permanece saturada y la integral se mantiene.

El asset v12 lleva el registro de control completo de 80 bytes, con objetivo, periodo entero,
ganancias, límites de voltaje e integral inicial. Se rechazan la cobertura tipada, los recuentos acotados, las
unidades/registros malformados, las extensiones duplicadas/ausentes y las rebajas falsificadas. El
fixture auténtico de bomba encendida v11 se capturó antes de que cambiara el escritor; su SHA-256
original y su huella siguen fijos. La repetición actualizada en el mismo runtime y las referencias
físicas pasan, junto con todos los fixtures anteriores y los modelos sin control sin cambios.

El experimento `pressure-regulated-pump` usa ticks físicos de 100 µs, muestras de control de 5 ms
y consignas de 300/350/200 kPa, con perturbaciones programadas de llenado/drenaje del embrague.
Los **757** límites de informe, portátil y MCP coinciden. A 15 s, la presión de línea es
**200550.7972096 Pa** para el objetivo de 200000 Pa. La última presión muestreada es
**200544.9882311 Pa**, el error **-544.9882311 Pa**, la integral **0.8124749429 V** y el voltaje
mantenido **0.8015751783 V**. La corriente del motor es **0.5964870315 A** y la velocidad del eje de la bomba
**2.0526093297 rad/s**. Huella del modelo `67e8edb13dc42f42`; hash final de estado de Windows/runtime
`44342c02cd41f3c6`. Informe: `artifacts/reports/pressure-regulated-pump.json`.
Las pruebas también aíslan el trabajo eléctrico de los límites mecánicos de accionamiento/carga en
cada muestra del informe y comprueban los residuos de volumen y energía.

El agente 0.15.0 expone el contrato de control, las ganancias dimensionales, los errores de entrada en propiedad y el
ejemplo. Las pruebas MCP reales ejercitan la repetición completa del experimento/la exportación, las escrituras de voltaje
bloqueadas, las actualizaciones de consigna, las revisiones, las bifurcaciones del controlador y la independencia del padre.
Unity prepara ahora vistas de regulador/sensor/orden y pruebas de reinicio/repetición de importación/Play.
No se obtuvo evidencia real de Editor/Play/Mono/IL2CPP ni de Player; la verificación nueva de Linux/macOS
también sigue pendiente. Las programaciones prescritas de válvulas y el sensor/la fuente de
voltaje ideales no implementan la ECU/TCU completa, la batería/PWM, la dinámica de sensores,
la mecánica de actuadores, la DCT/AT completa, el comportamiento restante del motor ni una calibración medida.
Todos los parámetros de investigación y de muestra siguen siendo `unverified`. El objetivo completo de Power! sigue abierto.

## 2026-09-30: punto de control reanudado de pérdidas de bomba y alimentación eléctrica

El propietario reanudó el desarrollo. Los cambios de fuente se verificaron en local en Windows x64
con el SDK de .NET 10.0.401 y el runtime 10.0.12 (el avance configurado `latestPatch`).
El commit base es `d266095`; estos cambios no estaban confirmados en el momento de la verificación.

```sh
dotnet run --file tools/Build.cs -- verify
```

- **174/174** comprobaciones administradas, **138/138** comprobaciones de ensamblados Standard alojadas en .NET 10
  y **17/17** grupos MCP contra un servidor hijo real.
- **16/16** comprobaciones Zig y **6/6** comprobaciones de ABI de C#; los **176** valores históricos coinciden
  exactamente. La auditoría de fuentes conserva cero archivos C/C++/Lua. Compilación Release: cero advertencias/errores.
- Registro: `artifacts/reports/pump-assembly-final-2026-09-30.log`. La reparación anterior de la referencia,
  por sí sola, pasó 165/165, 132/132 y 15/15 en `resume-baseline-2026-09-30.log`.

La [ejecución de CI 35809732259](https://github.com/Water-Run/Power/actions/runs/35809732259) precedente
pasó en Linux y falló cuatro comprobaciones de assets en Windows/macOS. Cada una falló solo en
un hash de estado final codificado de forma fija, procedente de la ejecución original del fixture en Linux, después de que la reproducción original y
la actualizada coincidieran. Los resúmenes de los archivos de fixture y las huellas de modelo siguen exactos.
Las pruebas conservan ahora la igualdad de hash en el mismo runtime y usan referencias físicas de los
puntos de control documentados, con tolerancias explícitas que reflejan su precisión publicada.
Los hashes históricos siguen registrados en la procedencia de los fixtures. Esta ejecución de Windows repara el
fallo observado en local; no establece un resultado nuevo de CI en Linux/macOS.

`HydraulicPumpAssembly` compone la bomba ideal, la fuga de presión de salida a entrada y
la fricción viscosa del eje referida a tierra. La evidencia independiente cubre todos los regímenes con signo y
las identidades de potencia pasiva, el movimiento analítico amortiguado de eje/presión, el refinamiento suave de segundo
orden, el inventario de entrada finito, el trabajo del depósito, una integración RK4 independiente
de la ODE del motor RL/eje/presión, el equilibrio eléctrico analítico, la reversión completa por fallo tardío,
la cancelación, las bifurcaciones, la inmutabilidad y el avance sin asignaciones. Ambos ensamblados destino
ejecutan las mismas comprobaciones. El experimento encendido sin pérdidas reproduce los observables físicos compartidos del laboratorio original
dentro de las tolerancias declaradas.

El laboratorio `fired-pump-losses` tiene **89** límites coincidentes de informe/portátil/MCP.
Su modelo tiene 60 estados contados. A 0.8 s, el trabajo de la bomba es **52.6573534421 J**, el calor de fuga
**8.6081420133 J**, el calor combinado de pérdidas de bomba **41.0517855370 J**, y el nodo
térmico de la bomba alcanza **300.4105178554 K**. La velocidad final del cigüeñal es **68.6812975063 rad/s**
y la presión de línea **1.0581382360 MPa**. El residuo de energía es de alrededor de `-6.13e-10 J`.
Huella `524661ea3d721bbc`. Informe: `artifacts/reports/fired-pump-losses.json`.

El laboratorio `electric-pump` tiene **106** límites coincidentes en 2 s. Su motor RL,
el eje de bomba aparte, la fuga, el arrastre, el alivio y la línea flexible accionan el llenado/drenaje/recaptura programados del embrague
de presión. El trabajo hidráulico externo es **cero**. La velocidad final del eje de la bomba
es **67.0570291504 rad/s**, la corriente **5.3148298325 A**, la presión de línea **0.5606191525 MPa**
y el deslizamiento del embrague está por debajo de `1e-8 rad/s`. El trabajo de la bomba es **3.6117807126 J**; el calor de fuga es
**0.3597803209 J**. El residuo de energía es de alrededor de `5.85e-10 J`. Huella `d8f8fedfdce59003`.
Informe: `artifacts/reports/electric-pump.json`. Las comprobaciones aíslan el trabajo eléctrico de los
límites separados de los ejes accionado y de carga, y verifican que las válvulas cerradas impiden la actuación por presión
incluso mientras la bomba eléctrica funciona.

El agente 0.14.0 anuncia la composición, las unidades, la semántica de potencia y ambos ejemplos.
El JSON Schema sin cambios y el asset v11 llevan componentes ordinarios; no se introdujo un formato nuevo ni
un tipo de componente nuevo. Catorce laboratorios se exportan y ejecutan en la herramienta de compilación en serie,
incluido el informe de CLI de la bomba encendida que antes solo se exportaba.

Las pruebas de repetición, reinicio y limpieza de importación/Play en Unity están preparadas para ambos assets nuevos.
`POWER_UNITY_EDITOR` no está definido y el Editor fijado no se encontró en el directorio de instalación
estándar. No se obtuvo evidencia real de Editor/Play/Mono/IL2CPP ni de un Player de escritorio.
La verificación nueva de Linux/macOS también sigue pendiente. Los valores de pérdida constantes,
las órdenes prescritas y todos los parámetros de muestra siguen siendo `unverified`; los mapas medidos,
la dinámica de batería/control/regulador/pistón, la DCT/AT y la ECU/TCU completas, el comportamiento restante del motor,
las muestras de vehículo calibradas y la aceptación de publicación siguen inacabados.

## 2026-09-22, punto de control de cierre: bomba accionada por eje y alivio de presión

Se añadieron bombas de desplazamiento reversibles ideales, entradas finitas/de depósito explícitas y
alivio de presión unidireccional de conductancia finita. Las velocidades de bomba y las presiones hidráulicas se unen
a la resolución de Newton del cilindro/convertidor; las capacidades de los embragues de presión se actualizan dentro de la
iteración de restricciones. La transferencia aceptada eje/fluido, el trabajo del depósito, el volumen de referencia
y las pérdidas térmicas comparten el estado transaccional completo. JSON/esquema, asset v11, agente
0.13.0, el descubrimiento MCP y el laboratorio de bomba encendida usan las mismas definiciones.

La verificación en serie exigida se completó con éxito:

```sh
.cache/dotnet/dotnet run --file tools/Build.cs -p:UseSharedCompilation=false -- verify
```

- **165/165** comprobaciones administradas, **132/132** comprobaciones de ensamblados Standard alojadas en .NET 10
  y **15/15** grupos MCP contra un proceso de servidor hijo real.
- **16/16** pruebas Zig y **6/6** pruebas de ABI de Python; los **176** valores numéricos históricos
  conservados coinciden exactamente. La auditoría de fuentes conserva cero archivos C/C++/Lua.
- Compilación Release: cero advertencias y errores. Registro:
  `artifacts/reports/pump-integration-verify.log`.

La evidencia independiente incluye identidades de potencia ideal en ambos sentidos, oscilación analítica
de eje/flexibilidad, inventario de entrada cerrada, arrastre inverso, reacciones de bomba engranada,
decaimiento exacto del alivio en el punto medio, un equilibrio regulado de carga constante y una
solución analítica de realimentación de presión del embrague deslizante. Los refinamientos del oscilador suave y de la realimentación del embrague
se acercan al segundo orden. También pasan la captura, la independencia de ramas, la cancelación,
el fallo numérico tardío, el reintento, el rechazo de presión negativa y el avance sin asignaciones.
La prueba inicial de asignación expuso asignación en su propio formato de estado;
el formato ahora se limita a los fallos, y el bucle en caliente medido asigna cero bytes.

Las pruebas de asset conservan la presión/topología de entrada y el ajuste del alivio, y rechazan extensiones malformadas y
duplicadas, dimensiones/recuentos inválidos y rebajas falsificadas. El fixture auténtico
v10 de hidráulica encendida conserva la huella `01b69cb3abe52211` y el hash final
`46a01d103e6159d3` tras la actualización. También pasan los resúmenes y las trayectorias de los fixtures anteriores.

El laboratorio de bomba encendida tiene **89** límites coincidentes de informe, portátil y MCP a lo largo de
0.8 s con ticks de 50,000 ns. Sus 56 estados contados incluyen una línea de alimentación de 4e-12 m³/Pa, una
bomba ideal de 1e-6 m³/rad accionada por cigüeñal y un ajuste de alivio de 1e6 Pa con conductancia
1e-9 m³/(s·Pa). La energía hidráulica inicial es de forma explícita 3 J. El trabajo de la bomba es
**53.9425016232 J**, el trabajo hidráulico externo **0 J** y el calor de alivio **45.0264051429 J**.
La presión final de línea es **1.0697262404 MPa**, la velocidad de cigüeñal/turbina **69.7555356890 rad/s**,
la velocidad de carga **6.6433843513 rad/s** y la temperatura del nodo térmico de la transmisión
**301.5306383749 K**. El residuo de energía total es `1.0671e-9 J`; el residuo de volumen de referencia
es `3.0493e-20 m³`. Huella `d0bd8f29a706fd89`, hash final `572150ab5d66a2f6`.
Informe: `artifacts/reports/fired-pump.json`.

Los doce documentos de laboratorio pasan JSON Schema; diez casos malformados de bomba/alivio se
rechazan. Auditoría: `artifacts/reports/pump-schema-audit.json`. Las vistas de puertos de bomba en Studio y
las pruebas de ciclo de vida de importación/Play están preparadas. `POWER_UNITY_EDITOR` no está definido; el Editor,
Play Mode e IL2CPP siguen sin verificar. Estas comprobaciones administradas no son evidencia de Unity.

El desarrollo se pausa aquí a petición del propietario. Las pérdidas y el control de la bomba, la corredera del
regulador y la dinámica del pistón actuador, la DCT/AT completa, la ECU/TCU, un comportamiento de motor más rico,
las muestras de vehículo calibradas y la aceptación de escritorio siguen inacabados. Consulta
[el contrato de la bomba](HYDRAULIC_PUMP.es.md) y el [estado de desarrollo](DEVELOPMENT_STATUS.es.md).

## 2026-09-22: red hidráulica y transmisión accionada por presión

Se añadieron nodos hidráulicos flexibles, restricciones lineales y turbulentas regularizadas,
presiones de depósito explícitas y embragues accionados por presión. La presión hidráulica y
los historiales de volumen/trabajo/calor participan en los ensayos internos de captura y en las transacciones
del lote completo. JSON/esquema, asset v10, capacidades del agente 0.12.0 y el laboratorio de hidráulica encendida
comparten estas definiciones. Consulta [las ecuaciones y los límites](HYDRAULIC_NETWORK.es.md).

Verificado en local en Linux con:

```sh
.cache/dotnet/dotnet run --file tools/Build.cs -p:UseSharedCompilation=false -- verify
```

- **154/154** comprobaciones administradas y **123/123** comprobaciones de ensamblados Standard de Core/Assets en .NET 10.
- **14/14** grupos MCP contra un proceso de servidor hijo real.
- **16/16** Zig y **6/6** comprobaciones de ABI de Python; los **176** valores históricos coinciden exactamente.
- La compilación Release tiene cero advertencias/errores; la auditoría de fuentes pasa.
- Registro: `artifacts/reports/hydraulic-integration-verify.log`.

La evidencia física nueva incluye comprobaciones de caudal con signo, pasividad y rango, carga RC analítica,
igualación cerrada, identidades exactas de trabajo del depósito y térmicas, integración RK4 independiente
de caudal no lineal, refinamiento de segundo orden de la presión y del impulso del embrague accionado por presión,
precarga y captura/liberación. El fallo tras un historial hidráulico/de embrague aceptado,
la cancelación, la entrada inválida, las bifurcaciones y el lote conservan la transacción completa.
La captura y las lecturas de instantánea asignan cero bytes tras el calentamiento. Un paso de drenaje excesivo
rechaza la presión manométrica negativa sin cambiar el estado.

La repetición portátil reveló, durante la implementación, un campo de presión de depósito omitido.
El registro de restricción v10 lo lleva ahora de forma explícita, y las pruebas de ida y vuelta comparan los
descriptores físicos completos y cada límite de repetición. Se rechazan los registros malformados refirmados,
las extensiones duplicadas/ausentes, las dimensiones incorrectas, los puertos de actuador inválidos y las rebajas de
nodos solo hidráulicos. El fixture auténtico v9, con SHA-256
`96a78326ae2f2e8dd4a434fb81fbe88f4a19156cbf13cf069a7bd4e798e93c9f`, conserva la huella
`839d03901973668d` y el estado final `834a679376b7a6fd` al actualizarse. Los fixtures anteriores permanecen.

El laboratorio de hidráulica encendida tiene **89** límites coincidentes de informe/portátil/MCP a lo largo de
0.8 s con ticks de 50,000 ns. Huella `01b69cb3abe52211`, hash final `46a01d103e6159d3`.
La velocidad final de cigüeñal/turbina es 70.94321138 rad/s; la velocidad de carga es 6.75649632 rad/s. Los depósitos
aportan 8 J, las restricciones disipan 7 J y la energía hidráulica almacenada aumenta en 1 J.
El calor del convertidor es 48.80297187 J; el calor de bloqueo, 32.89304173 J; el calor del embrague/freno de cambio,
119.31915873 J y 60.16098886 J. El nodo de calor compartido alcanza 301.34088081 K.
El residuo de energía total es `1.0896e-9 J`; el residuo de volumen de referencia es `-1.0804e-18 m³`.
El trabajo neto de fuente externa es -65.07102675 J, e incluye la alimentación hidráulica, la carga y el trabajo de
contrapresión del cilindro. No es una medición directa del trabajo solo de la carga.

Los once documentos de laboratorio se validan contra el esquema; diez contratos hidráulicos malformados
se rechazan de forma estructural. El compilador añade comprobaciones de dimensión/topología/rango.
Auditoría: `artifacts/reports/hydraulic-schema-audit.json`. Las vistas hidráulicas de Studio y
las pruebas de importación/Play están preparadas, pero `POWER_UNITY_EDITOR` no está definido; el Editor real,
el renderizado, Play Mode y Player/IL2CPP están sin verificar. Las bombas/reguladores, la dinámica de pistón y
acumulador, la DCT/AT completa, el motor/los controles y las muestras calibradas siguen abiertos.

Las descripciones anteriores del trabajo de fuente, más abajo, identifican ahora el trabajo externo neto de forma explícita:
ese libro incluye la contrapresión del cilindro, así que su magnitud no debe etiquetarse como
trabajo de salida solo de la carga. Esto es una corrección de la descripción de la evidencia, no un cambio de la física.

## 2026-09-22: convertidor de par acoplado y laboratorio encendido de bloqueo/cambio

Se añadieron cuatro mapas de convertidor explícitos con signo, la validación de interpolación pasiva,
las reacciones del estator estacionario, el calor del fluido y una resolución conjunta convertidor/cilindro integrada
con las restricciones de engranaje y los eventos de embrague. JSON/esquema, v9 portátil, capacidades del agente
0.11.0 y el ejemplo `fired-converter` comparten este contrato. Las vistas de Studio y las pruebas de Editor/Play
están preparadas. Consulta [CONVERTER_NETWORK.md](CONVERTER_NETWORK.es.md).

La orden en serie completa:

```sh
.cache/dotnet/dotnet run --file tools/Build.cs -p:UseSharedCompilation=false -- verify
```

Evidencia local en Linux:

- **143/143** comprobaciones administradas; **114/114** comprobaciones de ensamblados Standard de Core/Assets en .NET 10.
- **13/13** grupos MCP contra un servidor hijo real.
- **16/16** Zig y **6/6** pruebas de ABI de Python; los **176** números históricos coinciden exactamente.
- Compilación Release: cero advertencias/errores; la auditoría de fuentes pasa.
- Registro: `artifacts/reports/converter-integration-verify.log`.

Las comprobaciones nuevas cubren mapas con signo y continuidad del miembro de referencia, el balance de estator/energía,
las violaciones de pasividad en el interior, las unidades estrictas, la propiedad inmutable de los puntos, el fallo por desbordamiento,
el acoplamiento fluido y el calado analíticos, el refinamiento de segundo orden, la marcha atrás/la retención/la contrarrotación,
los puertos compartidos, el encaminamiento de pérdidas térmicas/externas, la reflexión de engranaje y el bloqueo en paralelo. El fallo,
la cancelación y las comprobaciones de rama conservan el estado completo; la captura interna asigna cero
bytes tras el calentamiento. Las comprobaciones portátiles rechazan recuentos incorrectos refirmados, índices/unidades erróneos,
mapas duplicados y rebajas. El fixture auténtico v8 conserva el SHA-256
`6872f857bc521ed114d6eea5837cbb380a01f374ddfe7e829b30afa2c44fe0aa`, la huella
`6703f00c995e6b62` y el estado final `b328de221532fbae` al leerse o actualizarse. Los fixtures anteriores
siguen sin cambios.

El experimento encendido de convertidor de 0.8 s usa ticks de 50,000 ns y repite con exactitud los **87**
límites a través del informe, el asset portátil y el MCP. Huella `839d03901973668d`,
hash final `834a679376b7a6fd`; velocidad de bomba/turbina 73.37747546 rad/s y velocidad de carga
6.98833100 rad/s. El calor del fluido es 24.27663069 J, el calor de bloqueo 22.84709072 J, el calor del embrague de cambio
157.18199410 J y el calor del freno 83.42288714 J. El nodo térmico 5 termina en 301.43864301 K,
con un residuo de energía total de `3.2969e-11 J`. El trabajo neto de fuente externa es -63.19344680 J, e incluye el trabajo de la carga y de la contrapresión del cilindro,
mientras que el combustible rastreado libera 2049.02269691 J. Son salidas numéricas sintéticas.

Un estudio de cinco pasos a 50,000/25,000/12,500/6,250/3,125 ns verifica que se reduce la distancia
normalizada combinada de la velocidad final del cigüeñal, el calor del fluido y el calor de bloqueo respecto a la
ejecución más fina, además de diferencias absolutas por debajo de 0.0002 rad/s o J, respectivamente. Las diferencias
individuales de calor no son monótonas cerca de los eventos de embrague; no se afirma un orden uniforme de convergencia
acoplada. La ejecución más fina da 73.37753852 rad/s, 24.27656820 J y 22.84711838 J.
Las pruebas analíticas suaves aparte conservan un factor de refinamiento mayor que 3.9.

Unity Editor, Play Mode, el renderizado e IL2CPP reales siguen sin verificar:
`POWER_UNITY_EDITOR` no está definido. El comportamiento completo del motor, la topología DCT/AT, la hidráulica,
los controles y las muestras de vehículo calibradas siguen abiertos. Los mapas cuasiestacionarios no establecen
la dinámica de fluidos ni el rendimiento medido del convertidor.

## 2026-09-22: engranajes ideales acoplados y transmisión planetaria encendida

Los engranajes ideales y las restricciones planetarias de tres puertos comparten ahora la resolución electromecánica,
de cilindro y de embrague. La proyección directa de restricciones conserva el movimiento compatible
y la fase relativa inicial; las reacciones medias por puerto son observables y transaccionales.
JSON/esquema, asset v8, CLI/MCP y Studio usan la misma topología. Consulta
[el contrato de engranajes y los límites numéricos](GEAR_NETWORK.es.md).

La orden en serie completa pasó en Linux x64 con el SDK de .NET en caché 10.0.400/runtime
10.0.11 y Zig 0.15.2:

```sh
.cache/dotnet/dotnet run --file tools/Build.cs -p:UseSharedCompilation=false -- verify
```

- **131/131** comprobaciones administradas de núcleo/aplicación.
- **105/105** comprobaciones de ensamblados Standard de Core/Assets alojadas en .NET 10.
- **12/12** grupos MCP de servidor hijo real.
- **16/16** grupos Zig, **6/6** pruebas de ABI de Python y **176** valores de referencia históricos que coinciden
  exactamente. La auditoría de fuentes no encuentra archivos de implementación C/C++ ni Lua.
- La compilación Release informa cero advertencias/errores. Registro: `artifacts/reports/gear-integration-verify.log`.

Nueve grupos de grafo comparan relaciones positivas/negativas y el movimiento/las reacciones planetarias libres
con referencias exactas independientes, la inercia reflejada de varias etapas y el orden por ID estable,
la equivalencia de motor CC RL/térmica y de cilindro reactivo, y la captura y el calor de la reducción analítica y del cambio
directo. Un oscilador restringido demuestra convergencia de segundo orden y
conservación de la energía. Se comprueban la reversión completa tras un prefijo de cambio aceptado, la cancelación,
la independencia de ramas, la repetición exacta por lotes y cero asignaciones; la cobertura de
asignación incluye la captura interna con factores variables. Los diagnósticos de rango, velocidad inicial, puerto,
relación y parámetro no admitido son explícitos.

Dos grupos de assets cubren la topología de tres puertos, cada límite de reproducción, los recuentos malformados,
los registros ausentes/duplicados/de tipo incorrecto, los portasatélites inválidos y los intentos de engranaje rebajados.
Un fixture auténtico v7 de embrague encendido conserva la huella `197be44884deee90` y el hash final
`28bf5335d8e35cde` tras actualizarse. Los fixtures anteriores y los modelos sin engranaje siguen sin cambios.
Dos grupos de integración administrados añaden contratos estrictos de JSON/agente, el comportamiento de revisión/cancelación/
rama y la distinción entre una ejecución lograda y unos KPI que pasan.

El laboratorio planetario encendido nuevo tiene **84 límites coincidentes** entre lotes
alternos, reproducción portátil y MCP. Su experimento de 0.8 segundos de subida/bajada registra
**-56.83157714 J** de trabajo neto de fuente externa, y genera **254.52399968 J** en el embrague sol/corona y
**156.31560557 J** en el freno de corona. El nodo térmico termina en **302.05419803 K**;
las velocidades de cigüeñal/carga son **76.81548837 / 7.31576080 rad/s**, con la corona retenida. El residuo
final de energía es **2.51020538e-10 J**. La huella es `6703f00c995e6b62`; el hash final
es `b328de221532fbae`. La fuente y el informe son `assets/labs/fired-planetary.power.json` y
`artifacts/reports/fired-planetary.json`. Los parámetros siguen siendo sintéticos y `unverified`.
Desactivar la programación de cambios elimina el calor del embrague sol/corona y cambia el movimiento de la carga.

Studio tiene vistas esquemáticas del planetario de tres puertos y de la transmisión final, con pruebas preparadas de importación,
repetición de cambio, reinicio y limpieza. `POWER_UNITY_EDITOR` no está definido: no se afirma evidencia de Editor/Play/
IL2CPP. La topología DCT/AT completa, el convertidor, la hidráulica, la ECU/TCU,
el comportamiento restante del motor, la calibración medida del vehículo y la aceptación de publicación siguen abiertos.

## 2026-09-22: referencias independientes de engranaje ideal y planetario

Se añadieron los primitivos inmutables `IdealGearPair` y `SimplePlanetaryGear` para pares externos
constantes. Los resultados exponen las velocidades de los miembros, los desplazamientos, los pares de reacción, el trabajo, el cambio de energía
cinética y el residuo. Las velocidades iniciales deben satisfacer la restricción; no se infiere una sincronización
con deslizamiento finito. Consulta [ecuaciones, signos y límites](IDEAL_GEARS.es.md).

La orden en serie completa pasó en Linux x64 con el SDK en caché 10.0.400/runtime 10.0.11:

```sh
.cache/dotnet/dotnet run --file tools/Build.cs -p:UseSharedCompilation=false -- verify
```

- **118/118** comprobaciones administradas de núcleo/aplicación y **94/94** comprobaciones de ensamblados Standard de Core/Assets
  alojadas en .NET 10; ocho grupos nuevos se ejecutan contra cada destino del núcleo.
- **11/11** grupos MCP de servidor hijo real, **16/16** grupos Zig y **6/6** pruebas de ABI de Python.
  Los **176** valores de referencia históricos coinciden exactamente; la auditoría de fuentes no encuentra
  archivos de implementación C/C++ ni Lua.
- La compilación Release tiene cero advertencias/errores. Registro:
  `artifacts/reports/ideal-gear-reference-verify.log`.

Las comprobaciones nuevas cubren relaciones de engranaje positivas/negativas, la inercia reflejada, el balance de impulso
por miembro, la potencia de reacción nula, la dinámica independiente de fuerza de restricción planetaria, tres
condiciones de miembro retenido y la tracción directa sol/corona. Las cargas de retención y de bloqueo son explícitas.
Los resultados de carga constante coinciden en intervalos partidos, incluida la inversión de velocidad. El muestreo
en el punto medio de cargas sinusoidales converge frente a integrales independientes, con una reducción del error
de cerca de cuatro veces por cada mitad de intervalo, para ambos primitivos.

El barrido determinista incluye **2,500 casos por referencia**. **10,000 evaluaciones de
cada primitivo** no asignan memoria administrada; los llamadores concurrentes independientes comparten solo
parámetros inmutables. Los valores inválidos, las velocidades iniciales incompatibles, el condicionamiento del constructor,
el desbordamiento aritmético y los errores finitos de cancelación de fuerzas se rechazan sin un resultado parcial.
Una regresión de relación alta conserva una reacción pequeña, exigida por la física, en lugar de perderla
al restar pares casi iguales.

El solver de grafo y la semántica del asset v7 no cambian; las comprobaciones existentes de laboratorio, portátil y
repetición MCP siguen pasando. Estos primitivos aún no son componentes de transmisión
acoplados, herramientas de agente, simulaciones de cambio ni modelos calibrados. La verificación real de Unity
Editor/Play/IL2CPP sigue pendiente, como el resto del motor, la DCT/AT,
la hidráulica, los controles y los objetivos completos de vehículo calibrado.

## 2026-09-22: embragues acoplados e integración de carga con motor encendido

Las reacciones estáticas/cinéticas del embrague comparten ahora la resolución electromecánica/de cilindro, con
eventos internos acotados de captura/inversión y encaminamiento del calor de fricción. La fase, las salidas medias
y el calor compensado son transaccionales y entran en el hash. JSON/esquema, CLI/MCP, asset v7 y
Studio consumen el mismo componente; los lectores v1–v6 siguen admitidos. Consulta
[las ecuaciones y los límites numéricos explícitos](CLUTCH_NETWORK.es.md).

La orden en serie completa pasó en Linux x64 con el SDK de .NET en caché 10.0.400/runtime
10.0.11 y Zig 0.15.2:

```sh
.cache/dotnet/dotnet run --file tools/Build.cs -p:UseSharedCompilation=false -- verify
```

- **110/110** comprobaciones administradas de núcleo/aplicación.
- **86/86** comprobaciones de ensamblados Standard de Core/Assets alojadas en .NET 10.
- **11/11** grupos de integración MCP de proceso hijo real.
- **16/16** grupos Zig y **6/6** pruebas de ABI de Python; los **176** valores históricos
  coinciden exactamente. La auditoría de fuentes no encuentra archivos de implementación C/C++ ni Lua.
- La compilación Release informa cero advertencias y errores. El registro local es
  `artifacts/reports/clutch-integration-verify.log`.

La evidencia física nueva compara el grafo con el `ClutchPair` exacto de carga constante
a través del acoplamiento interno y la inversión, relaciones positivas/negativas y ambos destinos
térmicos. Las trayectorias de motor/corriente bloqueados y de presión/combustible del cilindro reactivo
coinciden con modelos separados de inercia combinada de forma analítica. Un oscilador muelle/freno
coincide con el movimiento sinusoidal a trozos a través de tres inversiones y la captura final en el
cuarto punto de giro; reducir el tick a la mitad baja el error en más de 3.7 veces. Los lazos de tres embragues
ejercitan restricciones redundantes y el acoplamiento simultáneo, con movimiento
y energía conservados. La liberación estática exige saturación; el residuo de la resolución de raíces no puede crear
una segunda inversión espuria.

Se comprueba la reversión completa de varios ticks tras un prefijo aceptado de calentamiento/captura y una
sobrecarga numérica posterior. Se conservan la repetición programada, la cancelación, la independencia de la bifurcación, la propiedad
inmutable y cero asignaciones. Las comprobaciones de asignación incluyen eventos internos repetidos
de inversión, y ejercitan copias candidatas y factores variables. Estas pruebas respaldan
el alcance documentado del solver, no una exactitud híbrida arbitraria con ticks grandes.

El laboratorio nuevo `fired-clutch` tiene 67 límites de informe que coinciden con exactitud entre
lotes alternos, reproducción portátil y MCP. Su informe de 0.6 segundos registra 96.74607609 J
de trabajo externo neto exportado, incluida la carga y la contrapresión del cilindro, 191.55570747 J de calor de embrague, una velocidad final de motor/carga de
68.58488546 rad/s y un residuo final de energía de 1.79e-10 J. Su huella es
`197be44884deee90` y el hash de estado final `28bf5335d8e35cde`. El nodo térmico del embrague alcanza
300.95777854 K. La fuente y el resultado son `assets/labs/fired-clutch.power.json` y
`artifacts/reports/fired-clutch.json`; los parámetros siguen siendo sintéticos y `unverified`.

El asset v7 hace la ida y vuelta de capacidades y canales, rechaza extensiones malformadas/ausentes/duplicadas
y rebajas inválidas, y conserva el resumen, la huella y la repetición actualizada de un fixture auténtico v6 de cilindro encendido.
Los hashes de modelo anteriores siguen sin cambios.
Las comprobaciones estrictas de JSON/agente conservan los errores accionables, la atomicidad de entrada/revisión y la
distinción entre una ejecución lograda, unos KPI que pasan y una calibración medida.

Las placas de embrague de Studio, las salidas de fase con nombre y las pruebas de importación/ciclo de vida están preparadas.
`POWER_UNITY_EDITOR` sigue sin definir: el Editor, el renderizado, Play Mode e IL2CPP siguen
sin verificar. La topología DCT/AT, los conjuntos planetarios, el convertidor de par, la hidráulica, los controles,
el comportamiento completo del motor y las muestras de vehículo calibradas siguen inacabados.

## 2026-09-22: ley administrada de embrague en seco y referencia de carga constante

Se añadieron `DryClutch`, una ley inmutable de capacidad de par estático/cinético, y `ClutchPair`,
una referencia exacta de carga constante con dos inercias o freno a tierra. Un evento de deslizamiento nulo se
resuelve dentro del intervalo, seguido de movimiento restringido o inversión. Los resultados exponen
el movimiento, los avances angulares, el modo de reacción, el impulso, el calor, el trabajo externo y el cambio de energía.
Consulta [las ecuaciones, la API y la frontera de implementación](CLUTCH_PHYSICS.es.md).

La orden en serie pasó en Linux x64 con el SDK de .NET en caché 10.0.400/runtime 10.0.11
y Zig 0.15.2:

```sh
.cache/dotnet/dotnet run --file tools/Build.cs -p:UseSharedCompilation=false -- verify
```

- **99/99** comprobaciones administradas de núcleo/aplicación y **77/77** comprobaciones de ensamblados Standard
  alojadas en .NET 10, incluidos los mismos diez grupos nuevos de embrague en ambos destinos.
- **10/10** grupos de integración MCP de proceso hijo real.
- **16/16** grupos Zig y **6/6** pruebas de ABI de Python. Los **176** valores numéricos históricos
  coinciden exactamente. La auditoría de fuentes encuentra cero archivos de implementación C/C++ o Lua.
- La compilación Release informa cero advertencias y errores. La salida completa se conserva en local en
  `artifacts/reports/clutch-kernel-verify.log`.

La evidencia física cubre el acoplamiento analítico, el reparto exacto de la carga estática, el despegue,
la inversión, los eventos en los extremos, el acoplamiento parcial, un freno a tierra y relaciones de engranaje con signo.
Las pruebas comprueban de forma independiente el momento, el trabajo externo integrado y las energías cinéticas
absolutas, en lugar de comparar solo los contadores de energía de la implementación. Un par con
inercias 0.2 y 0.8 kg m2, velocidades iniciales 100 y 0 rad/s y capacidad deslizante 10 Nm
se sincroniza a 20 rad/s tras 1.6 s, y genera 800 J de calor.

Las soluciones de carga constante coinciden entre particiones de intervalo que cortan eventos híbridos.
Las cargas sinusoidales congeladas en el punto medio convergen frente a integrales independientes de velocidad, ángulo y calor
en más de 3.8 veces por cada mitad, con el error máximo más fino por debajo de 2e-5 en las
salidas SI probadas. Un barrido determinista de 2,000 casos comprueba la conservación y la evaluación
repetida. Encontró y corrigió un sobrecuento de un ulp de la duración de deslizamiento durante una
inversión. Los datos inválidos y los fallos de resolución aritmética o de eventos no publican un resultado parcial.
El camino de medición aislado y en caliente registra cero asignaciones en 10,000 intervalos.

Este es un primitivo físico autónomo del núcleo, **aún no un componente de grafo compilado**.
El acoplamiento eje/motor/cilindro, las restricciones de varios embragues, el encaminamiento térmico, el estado
híbrido transaccional, la representación JSON/asset/MCP y la integración en Studio siguen pendientes.
Las huellas de grafo existentes, siete experimentos de laboratorio y la semántica del asset v6 siguen
sin cambios. Su evidencia de combustión anterior se conserva más abajo.

`POWER_UNITY_EDITOR` sigue sin definir. Estas pruebas de ensamblados Standard no establecen el comportamiento real
de importación en Unity, Play Mode o IL2CPP. Todos los parámetros de vehículo siguen siendo `unverified`;
el primitivo nuevo no completa una transmisión, unos controles ni un grupo motopropulsor calibrado.

## 2026-09-22: combustión premmezclada, reactivos transportados y trabajo de carga encendido

Se añadió el seguimiento opcional de combustible/aire fresco/productos a los nodos de gas y fracciones de depósito
explícitas, más un componente `premixed_combustion` referido al cigüeñal. Un riesgo de Wiebe
prescrito consume los reactivos limitantes, almacena el historial irreversible del cigüeñal y convierte
la energía química en energía térmica. La previsión de calor participa en el trabajo conservativo del cigüeñal.
Consulta [el modelo, las ecuaciones y las limitaciones](PREMIXED_COMBUSTION.es.md).

La verificación en serie completa pasó en Linux x64 con el SDK de .NET en caché 10.0.400/runtime
10.0.11 y Zig 0.15.2:

```sh
.cache/dotnet/dotnet run --file tools/Build.cs -p:UseSharedCompilation=false -- verify
```

- **89/89** comprobaciones administradas de núcleo/aplicación.
- **67/67** comprobaciones de ensamblados Standard de Core/Assets alojadas en .NET 10.
- **10/10** grupos de integración MCP de proceso hijo real.
- **16/16** grupos Zig y **6/6** pruebas de ABI de Python, incluidos ambos hosts nativos.
- Los **176** valores nativos históricos coinciden exactamente; se encontraron cero fuentes C/C++ o Lua.
  La compilación Release informa cero advertencias y errores.

La evidencia física nueva incluye:

- Exposición analítica de Wiebe a través de ciclos explícitos, fases negativas y el cierre de ciclo.
  El combustible del recipiente cerrado, el consumo de aire fresco, el calor y la temperatura coinciden con la solución de forma cerrada
  del reactivo limitante para cargas pobres, ricas, sin combustible y sin aire. La masa y
  la energía total térmica más química se comprueban con independencia del contador de calor.
- La conservación de constituyentes de la red cerrada y el llenado/descarga del depósito transportan la
  composición aguas arriba y la entalpía química en cualquiera de los dos sentidos del flujo. Un recipiente de masa
  y temperatura constantes, con entrada y salida bloqueadas equilibradas, coincide con el reemplazo exponencial
  de la mezcla dentro de 2e-5 en fracción másica, incluso con más de una
  renovación del recipiente por tick exterior. Esto ejercita la cota del flujo saliente cuando las tasas netas de masa y
  de energía térmica por sí solas no dan un paso de tiempo útil para el trazador.
- Una referencia RK4 de cilindro reactivo, escrita de forma independiente, integra el movimiento del cigüeñal,
  la masa, la energía térmica, el combustible y el aire fresco con descarga bloqueada. Sus soluciones de 1 y 0.5 microsegundos
  difieren en menos de 1e-9 normalizado. Los ticks del núcleo de 100, 50 y 12.5 microsegundos
  reducen el error normalizado máximo en más de 2.8 veces y después en 8 veces, con el más fino por debajo de
  1e-4. Esta es evidencia de segundo orden sin pared; el acoplamiento de pared sigue siendo de primer orden.
- Varios cilindros reactivos en cigüeñales compartidos o acoplados por eje conservan la energía total
  y los constituyentes, incluidas mezclas aisladas con distintos poderes caloríficos y
  relaciones estequiométricas. Parar, invertir y volver a recorrer no puede repetir la liberación de calor;
  desactivar un quemado omite la exposición hacia adelante sin una recuperación posterior. El mayor ángulo de
  cigüeñal visitado es observable como `burn_frontier_angle`.
- Los cambios de par programados que fallan tras una reacción parcial revierten el estado de los constituyentes,
  el historial de ángulo y los libros compensados. Pasan la cancelación, la independencia de los lotes del llamador,
  el aislamiento de la bifurcación, el rechazo/la recuperación de un quemado poco resuelto y cero asignaciones de avance/instantánea
  contra ambos ensamblados. Se comprueban la composición estricta, la propiedad, las unidades y
  el presupuesto ampliado de 64 estados.

El [laboratorio de cilindro encendido](../assets/labs/fired-cylinder.power.json) pasa sus KPI
con la huella `a10f880d74494677` y **63** límites de informe coincidentes. JSON/CLI, MCP
y el asset v6 decodificado coinciden en cada canal y en cada límite, incluidos dos eventos de carga
entre instantes de informe. La ejecución de 0.6 segundos registra **-369.98 J** de trabajo neto de fuente externa,
consume **3.265e-5 kg** de combustible en la reacción y libera **1436.67 J**. La energía neta final de combustible
en el contorno es **1785.07 J**, y también queda combustible en la cámara; estos números transitorios
no son una afirmación de eficiencia estacionaria ni de consumo. Desactivar la combustión elimina
la liberación de calor y produce una velocidad de cigüeñal sustancialmente menor bajo la misma carga.

El residuo final de energía es aproximadamente **-2.11e-9 J**, el residuo de masa total **-1.25e-18 kg**,
el residuo de combustible **2.03e-20 kg** y el residuo de aire fresco **1.41e-18 kg**. La presión muestreada alcanza un pico
de alrededor de **2.08 MPa** y la temperatura de **1761 K**. Son salidas sintéticas del modelo;
el muestreo del informe a 10 ms no establece el pico continuo de presión/temperatura.

El asset v6 conserva los lectores v1–v5. Las pruebas nuevas preservan las definiciones de modelo/mezcla/quemado y
la repetición, rechazan la semántica de extensiones incorrectas/ausentes/duplicadas y los recuentos malformados, y
comprueban el resumen, la huella y la repetición actualizada de un fixture auténtico v5 anterior al cambio.
La [procedencia de los fixtures](../tests/Power.Tests/Fixtures/README.md) registra su punto de control
de fuente no confirmado, sin afirmar un commit publicado. Las huellas no reactivas existentes
siguen sin cambios. Las pruebas del agente cubren la validación estructurada, la entrada inválida y la cancelación
sin cambios de revisión, las escrituras obsoletas, la independencia de ramas, las salidas filtradas de combustible/calor,
la recuperación con un tick menor y la distinción entre una ejecución lograda y unos KPI fallidos.

El esquema Draft 2020-12 y los **siete** laboratorios pasan `jsonschema` de Python. Se rechazan ocho
formas malformadas de composición/quemado, incluidas fracciones ausentes, constituyentes
desconocidos, unidades incorrectas, fracciones inválidas, parámetros de quemado ausentes y fracciones de
depósito mal colocadas o nulas. Las comprobaciones del compilador imponen aparte las sumas de fracciones y la compatibilidad
de la mezcla conectada. La evidencia local está en `artifacts/reports/combustion-verify.log` y
`artifacts/reports/fired-cylinder.json`.

Las pruebas de importación en Unity y de Play Mode incluyen ahora un marcador de liberación de calor, el reinicio y la repetición completa
del ejemplo encendido. **No se han ejecutado en el Editor**: `POWER_UNITY_EDITOR` no está definido.
No se infiere ninguna afirmación de renderizado, Mono/IL2CPP o Player a partir de las pruebas de ensamblados Standard.
R/gamma constantes, el quemado prescrito y la política de frontera hacia adelante son límites explícitos;
la dosificación de combustible, el control de encendido, la química predictiva, la admisión/el escape detallados, las pérdidas mecánicas,
las transmisiones, los controles y las muestras de vehículo calibradas siguen abiertos.

## 2026-09-22: distribución por ángulo de cigüeñal y arrastre a velocidad cambiante

Se añadió `valve_timing` opcional en las restricciones de gas, con ciclos explícitos de 360/720 grados,
ángulos de apertura/duración, entrada de pico y salida de apertura efectiva. Los perfiles siguen el ángulo real
del cigüeñal a través de la aceleración, la parada, la inversión y el cierre de fase. Los lóbulos
poco resueltos rechazan el lote completo. Consulta [las ecuaciones, las cotas y el alcance](VALVE_TIMING.es.md).

La orden en serie completa pasó en Linux x64 con el SDK de .NET en caché 10.0.400/runtime
10.0.11 y Zig 0.15.2:

```sh
.cache/dotnet/dotnet run --file tools/Build.cs -p:UseSharedCompilation=false -- verify
```

- **76/76** comprobaciones administradas de núcleo/aplicación.
- **57/57** comprobaciones contra ensamblados Core/Assets de .NET Standard 2.1 alojados en .NET 10.
- **9/9** grupos de integración MCP de proceso hijo real.
- **16/16** grupos Zig y **6/6** pruebas de ABI de Python, con ambos hosts nativos ejecutados.
- Los **176** valores nativos históricos coinciden exactamente; la auditoría de fuentes encuentra cero archivos C/C++ o
  Lua. La compilación Release informa cero advertencias y errores.

Evidencia física adicional:

- La descarga adiabática bloqueada de forma cerrada, con exposición de válvula en seno al cuadrado
  integrada de forma independiente, a +40 y -40 rad/s, prueba el acoplamiento de la distribución al caudal a través del cierre de ciclo. Refinar
  los ticks de 1 ms a 0.5 ms reduce el error relativo de masa en más de 2.8 veces; 0.125 ms lo reduce
  en más de otras 8 veces, hasta por debajo de 1e-7. Los libros de energía/masa se comprueban aparte.
- Una referencia RK4 independiente de cilindro móvil incluye el trabajo de presión del cigüeñal, el caudal bloqueado
  y un lóbulo estrecho temporizado. La trayectoria cruza ambos límites del lóbulo. Reducir a la mitad los pasos de referencia
  de 1 a 0.5 microsegundos cambia los resultados normalizados en menos de 1e-10. Los ticks del núcleo
  de 200, 100 y 25 microsegundos reducen el error en más de 2.8 veces y después en 8 veces, con el error
  más fino por debajo de 1e-6. La evidencia de segundo orden sin pared no cambia el acoplamiento de
  pared de primer orden documentado.
- La cinemática exacta de par constante comprueba la apertura durante la desaceleración/inversión; las válvulas
  estacionarias y desactivadas conservan su comportamiento documentado. Un tick que abarca un lóbulo estrecho
  entero, con extremos cerrados, debe fallar y revertir. Reducir el tick resuelve su caudal.
- Pasan la cancelación, las programaciones fallidas, la independencia de los lotes del llamador, el aislamiento de la bifurcación, los parámetros
  de temporización malformados y el avance/las instantáneas sin asignaciones, contra ambos ensamblados.

El [laboratorio temporizado por cigüeñal](../assets/labs/crank-timed-cylinder.power.json) pasa todos
los KPI con la huella `38f0437eac4def69` y **63** límites de informe coincidentes. JSON/CLI,
MCP y el asset v5 decodificado coinciden en cada límite, incluidos dos eventos de par entre
instantes de informe. El residuo final de energía es aproximadamente **8.53e-10 J**, y el residuo de masa
es **-2.87e-18 kg**. La velocidad muestreada del cigüeñal va de **53.25 a 63.34 rad/s**, mientras que la apertura
se comprueba de forma independiente contra el ángulo del cigüeñal. Son comprobaciones numéricas de parámetros
sintéticos, no una calibración.

El asset v5 conserva los lectores v1–v4. Las pruebas rechazan recuentos malformados, registros de temporización duplicados/incorrectos
y la semántica de temporización eliminada, y verifican un fixture auténtico v4 anterior al cambio
con su resumen original, su huella y su repetición actualizada. La procedencia de los fixtures está registrada
[en las notas de los fixtures](../tests/Power.Tests/Fixtures/README.md). Las huellas de modelo anteriores
siguen sin cambios. Las pruebas MCP también conservan el estado/la revisión ante una entrada de pico inválida, y
las comprobaciones de aplicación distinguen una ejecución lograda de unos KPI fallidos y demuestran
la recuperación de un fallo de runtime por lóbulo estrecho al recrear con un tick menor.

El esquema Draft 2020-12 y los **seis** documentos de laboratorio pasan `jsonschema` de Python.
Se rechazan seis formas de temporización malformadas, incluidos campos ausentes, campos de perfil de más,
unidades incorrectas, una colocación inválida del componente y una temporización nula. Las pruebas del compilador cubren aparte
las restricciones de ciclo/rango y de topología.

La evidencia es una ejecución local en Linux, registrada en `artifacts/reports/valve-timing-verify.log`
y `artifacts/reports/crank-timed-cylinder.json`. Las pruebas nuevas de importación/Play en Unity comprueban marcadores
temporizados, el reinicio y la repetición, pero **no se han ejecutado en el Editor**: `POWER_UNITY_EDITOR` no está
definido. No se infiere ningún resultado de Editor, renderizado, Mono/IL2CPP o Player de escritorio a partir de las comprobaciones
administradas. La combustión, el comportamiento completo del motor, las transmisiones, los controles y las muestras de vehículo calibradas
siguen abiertos; todos los parámetros de investigación siguen siendo `unverified`.

## 2026-09-22: intercambio de gas del cilindro móvil y trabajo conservativo del cigüeñal

Se añadió `gas_cylinder`, un componente de geometría que conecta un cigüeñal rotacional y una cámara de
gas con masa y energía interna independientes. El volumen inicial se deriva de la posición del cigüeñal
y de la geometría; se rechaza un volumen o una propiedad ambiguos. El intercambio de gas, el trabajo del cigüeñal
y la transferencia de pared pasan por la reversión del lote completo, las bifurcaciones y la cancelación.
Las mismas definiciones las aceptan JSON/CLI/MCP y el asset portátil v4, y se conservan los lectores
v1/v2/v3. Consulta [las ecuaciones y el contrato](MOVING_CYLINDER.es.md).

La verificación en serie completa pasó en Linux x64 con el SDK de .NET en caché 10.0.400/runtime
10.0.11 y Zig 0.15.2:

```sh
.cache/dotnet/dotnet run --file tools/Build.cs -p:UseSharedCompilation=false -- verify
```

- **68/68** comprobaciones administradas de núcleo/aplicación.
- **51/51** comprobaciones contra ensamblados Core/Assets de .NET Standard 2.1 alojados en .NET 10.
- **8/8** grupos de integración MCP de proceso hijo real.
- **16/16** grupos Zig y **6/6** pruebas de ABI de Python, con ambos hosts nativos ejecutados.
- Los **176** valores de referencia nativos originales coinciden exactamente; el inventario de fuentes contiene
  cero archivos C/C++ y Lua. La compilación Release informa cero advertencias y errores.

Las comprobaciones físicas nuevas cubren:

- El acuerdo con la válvula cerrada frente al banco del cilindro cerrado durante la rotación adelante/atrás,
  los puntos muertos y los ticks minúsculos; masa constante y energía conservada.
- El movimiento con caudal bloqueado abierto, comparado con una integración RK4 escrita de forma independiente de
  las ODE de masa, energía y cigüeñal. Sus ecuaciones de geometría y de caudal no llaman a los auxiliares del núcleo
  bajo prueba. Reducir a la mitad el paso de referencia de 1 a 0.5 microsegundos cambia
  los resultados normalizados en menos de 1e-10. Reducir el tick del núcleo de 200 a 100 microsegundos
  baja el error de caudal suave en más de 3 veces; 25 microsegundos lo bajan en más de
  otras 10 veces y se mantiene por debajo de 1e-6 relativo.
- El refinamiento acoplado a la pared se valora aparte como de primer orden: los mismos refinamientos
  bajan el error en más de 1.7 veces y 3 veces, respectivamente, con el error más fino por debajo de 1e-5 relativo.
- Cigüeñales compartidos/acoplados, cilindros cerrados y abiertos mezclados, enlaces de gas, calor de pared y libros
  completos de energía/masa. Los cambios de par programados que fallan restauran todos los ticks anteriores y
  las entradas; pasan la cancelación, el aislamiento de la bifurcación y cero asignaciones de avance/instantánea.

El laboratorio de arrastre nuevo tiene la huella `dd62971021fa06e6` y **28** límites de
repetición, todos idénticos entre los informes JSON del experimento, los assets decodificados y la exportación MCP
real. Admite y expulsa gas de forma demostrable mientras la cámara se mueve. El residuo final de energía
es `2.9882230023758893e-10 J`; el residuo de masa es `1.463672932855431e-18 kg`.
Son observaciones numéricas de conservación para parámetros sintéticos, no una calibración.
Los cuatro informes de laboratorio precedentes conservan sus huellas y pasan su repetición y sus KPI.

La cobertura portátil incluye registros de cilindro antiguos/nuevos mezclados, la ida y vuelta exacta de la geometría,
extensiones de tipo incorrecto/duplicadas/ausentes, recuentos inválidos y el rechazo de registros de cilindro móvil
bajo versiones anteriores. El fixture guardado v3 de volumen fijo conserva la huella
`eeb18a7f1dc76175` y la repetición tras la recodificación v4. La procedencia de fuente/resumen del fixture está
registrada en [Fixtures](../tests/Power.Tests/Fixtures/README.md).

El esquema Draft 2020-12 y los cinco laboratorios pasan `jsonschema` de Python; se rechazan seis documentos
malformados de cilindro móvil. Las pruebas del agente cubren errores de geometría, la propiedad de la cámara,
el fallo no lineal acotado sin cambios de revisión/estado y la recuperación.
Los registros son `artifacts/reports/moving-cylinder-verify.log` y
`artifacts/reports/moving-cylinder-schema.log`; el experimento es
`artifacts/reports/moving-cylinder.json`.

Las pruebas de pistón móvil/importación/Play en Unity están preparadas, pero no se ejecutaron: `POWER_UNITY_EDITOR`
no está definido. El Editor/Play/renderizado de Unity, Mono/IL2CPP, el empaquetado del Player y la ejecución en
Windows/macOS de este incremento siguen sin verificar. Las aperturas de restricción programadas en el tiempo no
implementan la distribución por ángulo de cigüeñal. La combustión, el comportamiento completo del ciclo del motor, las transmisiones,
los controles y las muestras de vehículo calibradas siguen abiertos; el objetivo completo de Power! no
está completo.


## 2026-09-22: JSON de red de gas finita, asset e integración de agente

El punto de control del núcleo en `69bc1c4` se verificó antes de los cambios: **53/53 administradas,
41/41 de ensamblados Standard y 6/6 grupos MCP**. Las ecuaciones existentes del solver,
la construcción de la huella y los límites físicos no cambian en este incremento.

La verificación en serie completa pasó después en Linux x64 con el SDK de .NET fijado en caché
10.0.400 y Zig 0.15.2:

```sh
.cache/dotnet/dotnet run --file tools/Build.cs -p:UseSharedCompilation=false -- verify
```

- **60/60** comprobaciones de núcleo/aplicación en .NET 10.
- **45/45** comprobaciones contra los ensamblados Core/Assets de .NET Standard 2.1, alojados en .NET 10.
- **7/7** grupos de integración MCP contra un servidor hijo real.
- **16/16** grupos Zig nativos y **6/6** pruebas de ABI de Python; se ejecutaron ambos hosts nativos.
- Los **176** valores de referencia nativos originales coinciden exactamente; cero archivos C/C++ y Lua.
- Compilación Release: **cero advertencias y cero errores**.

Los cuatro informes de laboratorio pasan los KPI y la repetición: electrotérmico (11 límites),
red térmica (11), cilindro cerrado (21) y red de gas (14). El documento de gas
compila a la misma huella que una definición del núcleo ensamblada de forma independiente:
`eeb18a7f1dc76175`. Los informes JSON, la reproducción v3 decodificada y la exportación MCP real coinciden en
cada límite de informe de gas, incluidos los eventos de válvula entre límites de muestreo. Las comprobaciones
de residuo de masa y energía usan límites absolutos de 1e-14 kg y 1e-6 J, respectivamente;
la prueba también reconstruye el intercambio de energía del depósito a partir de los estados de la cámara y de la pared.

Las comprobaciones portátiles incluyen topología mixta de gas/cilindro/térmica, composición de gas no predeterminada,
unidades no SI, propiedad, cotas de programación, cancelación y fallo a mitad de lote
con reversión del cursor de eventos. Los archivos con el resumen recalculado de forma correcta, pero inválidos, cubren recuentos,
registros de extensión ausentes/duplicados/de tipo incorrecto, el rechazo de gas de versión antigua y huellas
obsoletas. Los fixtures auténticos v1 y de cilindro v2 conservan su huella original
y su comportamiento de repetición tras la recodificación v3; el commit de fuente y el hash del fixture v2 están
registrados en [Fixtures](../tests/Power.Tests/Fixtures/README.md).

Las comprobaciones del agente cubren la descubribilidad, las cotas de apertura inicial/programada, la atomicidad de la entrada
inválida, los conflictos de revisión, la cancelación, la independencia de ramas y la distinción
entre una llamada lograda y un KPI que falla. Aparte, `jsonschema` de Python validó
el esquema Draft 2020-12 publicado, los cuatro documentos de laboratorio y una variante de apertura fija,
y rechazó doce casos malformados de documento de gas.

Registros: `artifacts/reports/gas-integration-baseline.log`,
`artifacts/reports/gas-integration-verify.log` y
`artifacts/reports/gas-integration-schema.log`. Los informes y los assets de Unity generados
siguen siendo artefactos de compilación reproducibles, no fixtures de fuente.

`POWER_UNITY_EDITOR` no está definido. Las vistas esquemáticas de gas, las pruebas de importación y una prueba de ciclo de vida/repetición
en Play Mode están preparadas, pero **no se ejecutaron en Unity**. El Editor/Play/renderizado,
Mono/IL2CPP, el empaquetado del Player y la ejecución en Windows/macOS de este incremento siguen
sin verificar. La física completa del ciclo del motor, las transmisiones, los controles y la calibración del vehículo
siguen abiertos; los parámetros de muestra siguen siendo `unverified`.

## 2026-09-19: cadena de herramientas Python retirada; verificación nativa trasladada a C#

El repositorio ya no contiene Python. `tools/Install Zig.py`, `tools/VerifyNative.py`,
`legacy/native/tools/model_lab.py` y sus pruebas se portaron a C# y se incorporaron a
la herramienta de compilación de un solo archivo `tools/Build.cs` (las aplicaciones basadas en archivo de .NET 10 permiten un archivo fuente).
El host ctypes pasó a ser P/Invoke. Los atributos del consumidor externo del ABI no cambian. El instalador de Zig
usa el extractor ZIP integrado en Windows y delega las plataformas `tar.xz` en el
`tar` del sistema. La CI y la documentación se actualizaron en el mismo cambio.

Verificación en serie local en Windows x64 (`install-zig` + `native-verify`):

- Auditoría de fuentes: 0 archivos C/C++, 0 archivos Lua, 35 fuentes Zig, 38 entradas del manifiesto de migración.
- `verify` completo (Windows x64, local, en serie): 53/53 comprobaciones administradas, 41/41 comprobaciones de ensamblados .NET Standard,
  6/6 grupos de integración MCP, 16/16 pruebas Zig nativas. Se ejecutan `power_host` y `power_model_host`.
- Pasan 6/6 pruebas de ABI de C# portadas, incluida la limpieza de 70 ranuras de compilación fallidas, el rechazo de 15 mutaciones de documento
  y el código de salida 2 por KPI fallido.
- Comparación de referencia: **176/176** valores de referencia C originales coinciden exactamente (error absoluto máximo 0.0).

## 2026-09-14: punto de control del núcleo de red de gas compilada

Se recuperó el trabajo de WSL hasta `4a81716` y su integración inacabada del núcleo. El
punto de control compila ahora redes solo de gas y mixtas de gas/térmica, valida los rangos de composición
y de apertura, e incluye el estado de masa/energía interna y los libros del depósito en
instantáneas, hashes, bifurcaciones y la reversión del lote completo. Un limitador de etapa conservativo impide
que un par aislado en igualación oscile a través del equilibrio. El cilindro cerrado sin cambios
y los modelos lineales conservan sus huellas anteriores y su comportamiento de repetición.

Verificación en serie con el SDK de .NET 10.0.400 fijado en caché, en Linux x64:

- **53/53** comprobaciones de núcleo/aplicación en .NET 10.
- **41/41** comprobaciones contra los ensamblados Core/Assets de .NET Standard 2.1 en .NET 10.
- **6/6** grupos de integración MCP contra un servidor hijo real.
- Pasan los informes existentes de repetición de los experimentos electrotérmico, térmico y de cilindro.
- Pasan los grupos Zig nativos y seis pruebas de ABI de Python; los **176** valores de referencia originales
  coinciden exactamente. Auditoría de fuentes: cero archivos C/C++ y cero archivos Lua.

La evidencia específica de gas incluye la física de tobera bloqueada/subcrítica, la descarga analítica del recipiente
y el refinamiento, la entalpía de llenado del depósito, el flujo inverso, la conservación de masa
y energía de la red cerrada, el intercambio analítico de pared en tiempo finito, el aislamiento con válvula cerrada,
el rechazo de rango de entrada/programación, el desbordamiento observable, el fallo/la recuperación a mitad de lote, la equivalencia
de ramas y de lotes, la compilación inmutable y cero asignaciones de avance/instantánea.
Se prueba el rechazo de assets v1/v2 para impedir que se descarten campos de gas no admitidos.

Consulta [GAS_NETWORK.md](GAS_NETWORK.es.md) para el método numérico y el alcance restante.
Este punto de control tiene evidencia local en Linux; el estado actual de la CI de Windows/macOS debe leerse
en el flujo de trabajo de su commit. Unity Editor/Play/IL2CPP y el comportamiento de vehículo calibrado
siguen sin verificar. El registro autónomo anterior, más abajo, describe el commit precedente.

## 2026-09-14: física de intercambio de gas (autónoma)

Se añadió el primer corte del incremento de intercambio de gas como **solo física**: `IdealGas`,
`GasVolumeState` y `Orifice` en `src/Power.Core/GasExchange.cs`. Los volúmenes finitos llevan ahora
masa y energía interna como estados independientes, y el orificio implementa las
relaciones estándar de tobera isentrópica en ambos sentidos, con un coeficiente de descarga y
una fracción de apertura adimensional. `Numeric.Expm1` y `Numeric.Log1p` salieron de
`CylinderPhysics` y se comparten; las implementaciones no cambian, y cada hash de estado de cilindro existente,
cada huella de modelo y cada límite de repetición siguen coincidiendo.

**No cambió ningún tipo de nodo, tipo de componente, canal, campo de esquema ni versión de asset.** Un documento
de modelo sigue sin poder contener un volumen de gas finito, y las superficies de CLI, MCP y Unity no se
tocan. El método de división propuesto, con flujo de Euler hacia atrás, sigue sin validar y
sin adoptar. Consulta [intercambio de gas](GAS_EXCHANGE.es.md) para las ecuaciones, los límites numéricos y
la lista completa de contratos que no llegaron a integrarse.

Seis comprobaciones analíticas nuevas en `tests/Power.Tests/GasChecks.cs`, cada una escrita contra una
forma cerrada independiente y no contra una salida registrada: continuidad del bloqueo y monotonía de la función de flujo
para gamma en {1.1, 1.3, 1.4, 5/3}; 54 casos de tobera contra las relaciones de NASA de caudal másico
compresible; contratos del orificio, incluida la antisimetría exacta del flujo inverso, el aislamiento con válvula
cerrada y los estados rechazados; la descarga adiabática del recipiente contra la solución isentrópica
analítica hasta 1e-9 relativo; la identidad de llenado del depósito dU = cp*T_supply*dm con el
límite del recipiente evacuado T -> gamma*T_supply; y la conservación cerrada de dos volúmenes hasta 1e-14
relativo en masa y 1e-12 en energía, con igualación de presión.

La verificación en serie completa `dotnet run --file tools/Build.cs -p:UseSharedCompilation=false -- verify`
pasó en Linux x64 con el SDK de .NET 10.0.400 fijado en caché y Zig 0.15.2: **44/44 comprobaciones
administradas** (38 antes de este cambio), **26/26 comprobaciones contra los ensamblados .NET Standard 2.1
orientados a Unity**, **6/6 grupos de proceso MCP**, tres informes de experimento con 11, 11 y 21 límites de
repetición, **16/16 grupos Zig nativos** y las pruebas de ABI externo de Python. Ambos hosts Zig se ejecutaron
contra la biblioteca compartida real, la auditoría de fuentes informó `c_source_files: 0` y
`lua_files: 0`, y los **176 valores de referencia coincidieron exactamente con el binario C original**
(error absoluto máximo 0.0). El registro es `artifacts/reports/gas-exchange-verify.log`.

Windows y macOS no se ejercitaron para este cambio, y la validación de Unity Editor, Play Mode, renderizado
e IL2CPP sigue pendiente, como antes. Los parámetros de muestra siguen siendo `unverified`.

## 2026-09-11: limpieza del empaquetado Lua

Se eliminó el lanzador LuaInstaller restante y su README de empaquetado obsoleto.
El lanzador dependía del puente `power_native`, que nunca se implementó, y no tenía
llamadores activos de compilación ni de runtime. Las rutas originales y los hashes SHA-256 se
conservan en el [manifiesto de migración](../legacy/native/migration-manifest.json)
y coinciden con los archivos de la revisión de fuente registrada. Los documentos históricos de diseño
conservan su procedencia; sus propuestas Lua quedan retiradas.

La auditoría de fuentes rechaza ahora código fuente, bytecode y paquetes Lua, además de
código fuente y cabeceras C/C++, e informa `lua_files: 0`. Las sondas temporales no rastreadas
`.lua`, `.luau`, `.luac`, `.rockspec`, `.rock` y `.LUA` en mayúsculas produjeron cada una
un estado de salida fallido y un error estructurado que identifica el archivo;
el árbol limpio pasó después.

La verificación en serie completa `dotnet run --file tools/Build.cs -- verify` pasó en Linux x64:
**38/38 administradas, 26/26 de ensamblados .NET Standard, 6/6 MCP, 16/16 Zig y 6/6 comprobaciones de ABI
de Python**. Se ejecutaron ambos hosts nativos, y los 176 valores de referencia coincidieron exactamente.
El registro es `artifacts/reports/lua-removal-verify.log`. El comportamiento del núcleo, la evidencia de las muestras
y los archivos de licencia no cambian. El Editor de Unity no se ejercitó.

## 2026-09-11: migración nativa a Zig

El propietario reanudó la migración del lenguaje nativo el 2026-09-10. Los **26 archivos C
de implementación/prueba/host y 12 cabeceras** se sustituyeron por Zig. El
[manifiesto de migración](../legacy/native/migration-manifest.json) registra la revisión
Git original, las rutas de archivo y los hashes SHA-256. No quedan fuentes ni cabeceras C/C++
en el inventario de fuentes del repositorio; la orden de verificación de la raíz impone
esa restricción. Se conservan los archivos de licencia/excepción y la evidencia de las muestras.

La verificación en serie completa `dotnet run --file tools/Build.cs -- verify` pasó en Linux x64
con el SDK de .NET 10.0.400 fijado en caché y Zig 0.15.2: **38/38 comprobaciones
administradas, 26/26 comprobaciones contra los ensamblados .NET Standard 2.1 orientados a Unity,
6/6 grupos de proceso MCP, 16/16 grupos Zig nativos y 6/6 pruebas de ABI externo
de Python**. La batería nativa también pasó los 16 grupos en Debug, con las comprobaciones de seguridad
activadas. Ambos hosts Zig se ejecutaron contra la biblioteca compartida real. La biblioteca
exporta solo `pwr_get_api` y no tiene símbolos ELF sin resolver. Los **176 valores
en 11 instantes de muestra electrotérmicos coincidieron exactamente** con el binario C original en
este host, con la misma huella de modelo y los mismos contratos de canal. La
comparación de fixtures entre cadenas de herramientas sigue usando tolerancias explícitas, y
la repetición del mismo binario debe coincidir con exactitud. Los informes están en
`artifacts/reports/zig-migration-verify.log`, `native-verification.json` y
`native-electrothermal.json`.

La compilación cruzada ReleaseSafe de biblioteca/host también pasó para **Windows x86_64**
y **macOS aarch64**. La compilación cruzada no es evidencia de ejecución en esos
sistemas. Los registros locales son `artifacts/reports/zig-cross-windows.log` y
`zig-cross-macos.log`.

GitHub Actions completó después la verificación real con éxito en
**Linux x64, Windows x64 y macOS arm64**, y cada uno pasó las **38/38 administradas,
26/26 de ensamblados .NET Standard, 6/6 MCP, 16/16 Zig y 6/6 comprobaciones de ABI de Python**.
Ambos hosts de biblioteca compartida se ejecutaron en cada plataforma. Los 176 valores de referencia nativos
coincidieron exactamente en los tres ejecutores, y sus inventarios de fuentes contenían
cero fuentes o cabeceras C/C++. Evidencia:
[ejecución 34549950147](https://github.com/Water-Run/Power/actions/runs/34549950147),
commit de código [`dd3de10`](https://github.com/Water-Run/Power/commit/dd3de10294949c8b2ffb49afc02a9c70b5808c00).
La extracción de Windows fija los archivos Zig en LF; la verificación de macOS usa los stubs Darwin incluidos con Zig
para evitar la incompatibilidad con el SDK de Apple más reciente, descrita en las
[notas de compilación nativa](NATIVE_ZIG.es.md#build-and-maintenance). Los registros completos de los trabajos se
conservan en local bajo `artifacts/reports/zig-ci-34549950147-{linux,windows,macos}.log`,
con los metadatos de la ejecución en `zig-ci-34549950147.json`. Las correcciones posteriores de documentación y
comentarios no cambian ningún código ejecutable.

La batería nativa cubre además el módulo de grupo motopropulsor automático que antes no se compilaba,
incluida la repetición, la contabilidad de energía, la caída de tensión y la reversión de transacciones,
más el manejo concurrente del tiempo de vida del SDK. Tres puntos de entrada históricos de prueba
devolvían éxito en silencio cuando las comprobaciones fallaban; el puerto corrige la propagación y
separa los escenarios de combustión estacionaria, limitador y contrapresión del motor.
Consulta [las notas de migración](NATIVE_ZIG.es.md) para las ecuaciones conservadas y
el montaje experimental corregido. El resultado original de CTest por sí solo era insuficiente
a causa de esos fallos ocultos.

Esta migración no completa los objetivos administrados de motor/transmisión ni
establece la calibración del vehículo. No se ejercitaron Unity Editor, Play Mode, el renderizado, Mono ni
IL2CPP. Toda la calibración de las muestras sigue siendo `unverified`.


## 2026-09-08: incremento del cilindro cerrado

La verificación en serie `tools/Build.cs verify` pasó en Linux x64 con SDK 10.0.400 y runtime 10.0.11: **38/38 comprobaciones administradas, 26/26 comprobaciones contra los ensamblados .NET Standard 2.1 reales y 6/6 grupos de integración de proceso MCP**. La compilación Release informó cero advertencias y errores. El registro es `artifacts/reports/cylinder-verify.log`; los tres documentos JSON de laboratorio también pasaron el JSON Schema publicado con el validador local `jsonschema`.

Las comprobaciones nuevas cubren la geometría analítica de biela-manivela y sus derivadas, las identidades de estado del gas ideal, ejecuciones de conservación de dos segundos con y sin contrapresión, la convergencia de segundo orden bajo refinamiento del paso, la rotación inversa, los pasos minúsculos y los puntos muertos, los cigüeñales compartidos/acoplados con componentes eléctricos y térmicos, la reversión por fallo no lineal, la cancelación, las bifurcaciones, las unidades, las extensiones de cilindro malformadas y cero asignaciones administradas en el avance/las instantáneas en régimen. Un fixture v1 conservado se decodifica, retiene su huella lineal original y se repite de forma idéntica tras la exportación v2.

GitHub Actions repitió la misma verificación con éxito en **Windows, macOS y Linux**, con comprobaciones 38/38, 26/26 y 6/6 y cero advertencias/errores en cada plataforma. Evidencia: [ejecución 34176008291](https://github.com/Water-Run/Power/actions/runs/34176008291), commit de código [`6209df2`](https://github.com/Water-Run/Power/commit/6209df22876f9928ddc04a1a32845cd7efe087da). Los registros completos de los trabajos y los metadatos de estado se conservan en local como `artifacts/reports/github-actions-34176008291.log` y `.json`. La actualización posterior de documentación no cambia ningún código ejecutable.

El experimento sintético de cilindro pasó sus KPI finales y se repitió con exactitud en 21 límites a través de JSON, la reproducción del asset y un servidor MCP hijo real. Resultados de Linux: huella `c64b61efdb827680`, velocidad final `153.00340249454544 rad/s`, presión `118835.36885412445 Pa`, temperatura `315.16234058802814 K`, residuo final de energía `8.7464e-10 J` y residuo absoluto máximo muestreado `8.9570e-10 J`. Informe completo: `artifacts/reports/sealed-cylinder.json`. Estos valores establecen evidencia numérica de este banco de gas ideal cerrado, no una calibración de motor.

Las pruebas de importación y Play en Unity incluyen ahora el asset del cilindro y el movimiento esquemático del pistón, pero **no se ejecutaron**. La evidencia de Unity Editor, Mono, renderizado e IL2CPP sigue pendiente. La pila activa sigue siendo C#/Unity; la dirección futura de reescritura en Zig no introduce un runtime nativo en este incremento.

## Punto de control de pausa: 2026-09-08

El propietario pidió el cierre y una pausa del desarrollo tras el incremento del cilindro. La fuente ejecutable permanece en el commit de código verificado `6209df2`; los commits posteriores actualizan solo la documentación. La extensión prevista de intercambio de gas no se aplicó, ni se compiló, ni se publicó. Sus [notas de reanudación](NEXT_ENGINE_STEP.es.md) distinguen el trabajo propuesto de las capacidades implementadas. No hizo falta otra compilación para este punto de control solo de documentación. El desarrollo se reanuda solo después de una instrucción explícita del propietario.

## Referencia histórica: 2026-09-07


Entorno: 2026-09-07, Linux x64, .NET SDK 10.0.400, runtime .NET 10.0.11. El resultado ejecutado es la salida de `tools/Build.cs verify` y los informes generados.

Referencia administrada en esa fecha: pasaron 30/30 comprobaciones de núcleo, asset y agente, 19/19 comprobaciones de ensamblados de biblioteca estándar y 5/5 grupos de integración de proceso MCP. La compilación Release informó 0 advertencias y 0 errores. El proceso MCP en vivo descubrió 12 herramientas y comprobó los esquemas de entrada y de salida. Las respuestas de éxito y de error se comprobaron en cuanto a los campos de salida exigidos y un resultado de texto compatible. El registro en bruto es `artifacts/reports/managed-verification.log`.

## Evidencia registrada entonces

- Core y Assets se compilaron para `net10.0` y `netstandard2.1`.
- Las comprobaciones analíticas cubrieron el par constante, la respuesta RL y el equilibrio térmico. Reducir el paso a la mitad comprobó la convergencia mecánica de segundo orden y la convergencia térmica de primer orden.
- Se comprobaron las relaciones positivas y negativas en cuanto al momento generalizado, el calor de amortiguamiento y la conservación. El frenado regenerativo se comprobó en cuanto a la corriente negativa y una disminución del trabajo de la fuente.
- El rechazo de entradas, un desbordamiento en un tick posterior, la precancelación y las comprobaciones de capacidad del búfer confirmaron que el estado y los datos del llamador no se modifican de forma parcial.
- Se comprobó el acuerdo de la propiedad de la descripción del modelo, las instancias independientes en paralelo, las bifurcaciones de estado completo y el avance por tick frente al avance por lote.
- Un contador de asignación de hilos de .NET midió 0 asignaciones administradas en el camino caliente del núcleo de entrada, paso e instantánea combinados. Ese recuento excluye la compilación, los informes y la UI de Unity.
- La codificación y la decodificación del asset conservaron la fuente, el modelo y los eventos. Se rechazaron un resumen dañado, recuentos falsificados, una versión de formato incorrecta, una huella de modelo incorrecta y bytes de más.
- Las entradas programadas cubrieron el instante cero, el final de un lote y eventos dentro de un lote de presentación. Un fallo numérico posterior revirtió el lote entero. La reproducción del asset midió 0 asignaciones administradas cuando cada tick llevaba un cambio de entrada. La cancelación conservó el cursor de eventos.
- Los informes JSON y los assets importados compararon hashes de estado y valores de salida en cada límite de informe, incluidos los eventos que no caen en un límite de presentación de 20 ms.
- Las mismas comprobaciones físicas cargaron las DLL reales de .NET Standard 2.1 copiadas para Unity y comprobaron su marco de destino. El host seguía siendo .NET 10, así que esto no muestra que Mono o IL2CPP pasaran.
- Las comprobaciones del agente cubrieron diagnósticos de campo estructurados, instantáneas filtradas, el límite de sesiones, conflictos de revisión concurrentes, la cancelación, el aislamiento de la bifurcación padre e hija, el ciclo de vida y los informes compactos.
- El cliente MCP oficial arrancó un proceso hijo real de servidor y completó el descubrimiento de 12 herramientas, los esquemas de entrada y de salida, la recuperación de errores, las operaciones de sesión, un experimento completo y la exportación del asset. El resumen del archivo se comprobó tras la decodificación Base64, y la reproducción importada se comparó con el estado final del experimento MCP.

El experimento electrotérmico por defecto dura 10 segundos, baja a 4 V a los 5 segundos y vuelve a 24 V a los 6 segundos. Dos tamaños de lote coinciden bit a bit en 11 límites. Los valores finales típicos son alrededor de motor `29.74182442 rad/s`, carga `9.91394147 rad/s` y temperatura del motor `302.4760663 K`. El umbral del residuo de energía es `1e-5 J`. Los hashes de repetición se comparan solo para el mismo binario, runtime y arquitectura. Los números entre runtimes usan una tolerancia.

El experimento de intercambio de calor usa los nodos 42/77, sin entrada externa y con un paso de 7 ms, y dura 7 segundos. Dos tamaños de lote coinciden en 11 límites. La temperatura final difiere de la solución discreta de Euler hacia atrás en menos de `1e-9 K`, de la solución analítica continua en menos de `0.004 K`, y el error de energía total es menor que `1e-7 J`. Los dos informes son `artifacts/reports/electrothermal.json` y `thermal-network.json`.

## Reproducción

```sh
dotnet run --file tools/Build.cs -- verify
```

Estas comprobaciones son programas de aceptación de consola que ejecutan aserciones en Release. No dependen de pruebas `Debug.Assert` vacías. No necesitan Unity, Python ni la biblioteca C original. El host de verificación nativa y el instalador de Zig se trasladaron a la misma herramienta de compilación .NET el 2026-09-19 (P/Invoke de C#). El proyecto MCP usa el paquete NuGet oficial, y `packages.lock.json` fija la resolución.

GitHub Actions completó la misma aceptación administrada en Windows, macOS y Linux: 30/30, 19/19 y 5/5 en cada plataforma. La evidencia es el commit de código [`aea6136`](https://github.com/Water-Run/Power/commit/aea6136bbdcbeaea91d63836d947637e7eac730e) y la [ejecución 34087686661](https://github.com/Water-Run/Power/actions/runs/34087686661). `artifacts/reports/github-actions-34087686661.log` y `.json` se guardan en local y contienen la salida del trabajo y el estado final. Se ejecutó otra aceptación local desde una copia limpia de las fuentes, sin caché y sin ensamblados generados. Su registro es `github-clean-checkout.log`.

Los enlaces de evidencia del repositorio y de la CI son públicos. El desarrollo se reanudó el 2026-09-08; los registros anteriores, más abajo, identifican sus propias referencias verificadas.

La actualización de publicación GPL añadió avisos de licencia sin cambiar el contenido de la fuente ejecutable; una comparación con el commit precedente confirmó que las 90 ediciones de fuente/compilación eran solo avisos. Una verificación en serie nueva pasó 30/30 comprobaciones administradas, 19/19 comprobaciones de ensamblados orientados a Unity y 5/5 grupos de integración MCP, con cero advertencias o errores de compilación. Su registro es `artifacts/reports/license-verification.log`. Esto no añade evidencia de validación de Unity Editor ni de Player.

## Evidencia aún no obtenida

Este entorno no tiene instalado el Editor de Unity. No se ejecutaron las pruebas de importación del Editor, de Edit Mode y de Play Mode, las comprobaciones de renderizado de la escena ni una compilación IL2CPP. El proyecto, las escenas, las pruebas y la entrada de automatización están en su sitio:

```sh
dotnet run --file tools/Build.cs -- unity-test
```

Define primero `POWER_UNITY_EDITOR`. Los registros de Unity y los resultados XML van a `artifacts/unity`. Las pruebas de Play necesitan una máquina que pueda ejecutar el editor gráfico y una licencia válida de Unity. Las pruebas escritas y aún no ejecutadas cubren la importación de URP y de ensamblados para ambos assets de modelo, el acuerdo de la reproducción, el arranque y la parada repetidos sin restos, el experimento de referencia de 10 segundos, el cambio a una topología solo térmica en marcha, las listas dinámicas de nodos y de entradas, y la programación de ticks de 7 ms. Los controles, el tema, los tamaños de ventana y la presentación de escritorio aún necesitan que una persona los mire, y las builds de Player para las tres plataformas de escritorio siguen abiertas.

La entrada de publicación es `Power.Studio.Editor.ProjectSetup.BuildPlayer`, con el destino de escritorio seleccionado e IL2CPP. Necesita el módulo de compilación de la plataforma Unity correspondiente. Aún no hay un paquete de Player compilado ni probado.

Cada parámetro actual es un parámetro de experimento sintético. Un motor y una transmisión completos, la calibración del vehículo, las emisiones, la acústica, un presupuesto de tiempo real y las ejecuciones largas aún necesitan su propia implementación y su evidencia. Estas comprobaciones no establecen ese trabajo.
