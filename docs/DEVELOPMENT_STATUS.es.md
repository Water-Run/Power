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
| Combustible | Dosificación por ciclo, rieles flexibles con bomba, tanques finitos, retornos de alivio conservativos, evaporación de película, agujas físicas y predicción acotada de cierre; [Geometría del tanque y espacio gaseoso finito](TANK_HEADSPACE.es.md) | oleaje/forma hidrostática y llenado/regulación medidos, comportamiento magnético, electrónico y de pulverización no lineal, equilibrio de fases dependiente de la presión y propiedades de combustible medidas |
| Transmisión | Embragues estáticos y deslizantes, actuación por contacto, engranajes y planetarios con signo, convertidor y bloqueo mapeados, y caminos DCT de siete marchas adelante y marcha atrás y Ravigneaux de cuatro adelante y marcha atrás, con giro de satélite e inercia orbital resueltos | Flexibilidad, pérdidas y reparto de carga del engrane, actuación DCT, control completo de presión y cambio de la AT y encaminamiento medido, cambios coordinados, pérdidas medidas y comportamiento de convertidor más rico |
| Hidráulica | Volúmenes flexibles, restricciones, bombas con fugas y arrastre explícitos, alivio, pistones dinámicos, correderas dosificadas y acumuladores de gas de energía finita | Mapas medidos de válvulas, acumuladores y bombas, fricción de juntas, cavitación e hidráulica completa de transmisión |
| Eléctrica | Motores RL, solenoides recíprocos de inductancia variable, batería de carga finita, polarización por resistencia y RC, conversión de ciclo de trabajo promediada y accesorios | Comportamiento químico y térmico medido, BMS, control de corriente e integración completa de la alimentación |
| Controles | PI de presión muestreado, dosis/cierre de aguja, relevo DCT escalonado y cambios AT por realimentación de presión con confirmación física de bloqueo | Coordinación de par de ECU/TCU, sensores, actuadores y tratamiento de fallos |
| Documentos y assets | 44 laboratorios JSON/CLI, 43 ejemplos MCP, asset v29 y lectores v1-v28 | Edición/guardado y colecciones de modelos calibrados |
| Agentes | Doce herramientas MCP definidas por esquema; evidencia compacta, comprobaciones de revisión y diagnósticos accionables | Flujos completos para el alcance físico y de control restante |
| Unity | Importación de modelos, reproducción en ticks exactos, componentes 3D esquemáticos, controles, reinicio y pruebas de ciclo de vida preparadas | Aceptación real de Editor/Play, gráficas seleccionables, edición y guardado de grafos, y Player/IL2CPP |
| Archivo nativo | Prototipos de investigación en Zig 0.15.2, ABI conservado y procedencia original de las fuentes | Referencia histórica; la migración gestionada sigue separada de la funcionalidad completa |

El núcleo y Assets tienen como destino tanto `net10.0` como `netstandard2.1`; el núcleo no tiene dependencias de Unity,
de transporte, de proveedor de modelos ni de terceros. Los scripts de Unity Assets usan
C# 9. Unity carga los ensamblados Standard compilados por el SDK externo; no
compila fuente de .NET 10 ni de C# 14.

## Evidencia y límites

El comando serie requerido `dotnet run --file tools/Build.cs -- verify` cubre ambos destinos de ensamblado, un proceso MCP real, todos los laboratorios, el archivo Zig y la ABI C#. Los recuentos, resultados y rutas de registros están en [VALIDATION.md](VALIDATION.md). Las comprobaciones Standard en .NET 10 no establecen aceptación del runtime Unity.

El suministro incluye [tanques finitos](LIQUID_FUEL_TANK.es.md) y [retornos de alivio rastreados](LIQUID_FUEL_RETURN.es.md), con balances de masa, energía calórica/química y trabajo de presión. El [controlador AT hidráulico](AT_CONTROL.es.md) regula la presión de actuadores y confirma marcha/bloqueo físicos. Referencias independientes y transacciones completas respaldan estos modelos de investigación; la coordinación ECU/TCU completa y el comportamiento medido de hardware siguen pendientes.

`POWER_UNITY_EDITOR` no está definido en el entorno actual. La importación Studio, la reproducción y las pruebas preparadas aún necesitan evidencia real de Editor/Play, renderizado y Player/IL2CPP.

Todos los parámetros siguen `unverified`. EA211 DJS + DQ200 y PSA EC5 + AT8 conservan límites completos y manifiestos de evidencia en [assets/samples](../assets/samples). Las mediciones OEM ausentes siguen ausentes. Se conservan las licencias y la procedencia histórica del código.

CLI `list-labs`, el descubrimiento de ejemplos MCP y la verificación serie comparten [un catálogo de laboratorios](../assets/labs/catalog.json). La verificación comprueba que cubra cada fuente de laboratorio.

## Siguiente secuencia de desarrollo

1. Ampliar equilibrio de fases dependiente de la presión, cavitación, llenado/regulación medidos y actuación magnética/electrónica refinada. Sustituye el límite declarado
   de trabajo de desplazamiento exportado cuando se resuelvan el volumen líquido finito y la cantidad de movimiento
   de la pulverización. Mantén observables por separado el líquido entregado, el combustible evaporado y la reacción,
   y conserva referencias independientes.
2. Amplía el motor con control de encendido, dinámica de admisión y escape, pérdidas
   mecánicas y termoquímica más rica. Conserva el objetivo del motor completo.
3. Extender el accionamiento DCT y la hidráulica planetaria/AT medida, y coordinar solicitudes de par y cambios ECU/TCU con los controladores DCT y AT muestreados existentes. Añadir sensores/actuadores medidos y fallos recuperables.
4. Ejecuta `unity-test` con el Editor fijado y después obtén evidencia de Player/IL2CPP.
   Completa la selección de canales, la edición de grafos y el guardado como funciones distintas.
5. Obtén mapas medidos, datos OEM y presupuestos de incertidumbre para los dos grupos
   motopropulsores objetivo antes de declarar muestras calibradas o preparación para la publicación.
