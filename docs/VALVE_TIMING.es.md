# Distribución por ángulo de cigüeñal

[English](VALVE_TIMING.md) · [简体中文](VALVE_TIMING.zh-CN.md) · [Français](VALVE_TIMING.fr.md) · [Русский](VALVE_TIMING.ru.md) · [日本語](VALVE_TIMING.ja.md) · [한국어](VALVE_TIMING.ko.md) · [Deutsch](VALVE_TIMING.de.md) · **Español** · [Italiano](VALVE_TIMING.it.md) · [Português](VALVE_TIMING.pt-BR.md)

Un `valve_timing` opcional en un `gas_orifice` multiplica su apertura por una envolvente periódica
del ángulo de cigüeñal. Admite recipientes de gas fijos y cilindros móviles a través de las
mismas definiciones de Core, JSON, CLI, MCP y de asset portátil. La envolvente sigue la posición real
del cigüeñal durante la aceleración, la parada y la inversión. Representa el área de flujo
efectiva; no modela el contacto de leva, la alzada física de la válvula, las fuerzas del resorte ni la fricción.

## Contrato y fase

```json
"valve_timing": {
  "crank_node": 1,
  "cycle_angle": { "value": 720, "unit": "deg" },
  "open_angle": { "value": 520, "unit": "deg" },
  "duration_angle": { "value": 200, "unit": "deg" }
}
```

`crank_node` debe identificar un nodo rotacional. Los tres ángulos exigen unidades explícitas `deg`
o `rad`. El ángulo de ciclo es exactamente 360 o 720 grados; la duración está entre 1e-6
radianes y el ángulo de ciclo. El ángulo de apertura es finito y se normaliza módulo el ciclo.
Se admiten ángulos negativos y un lóbulo que cruza el límite del ciclo. Varias válvulas
pueden referenciar un mismo cigüeñal, incluidos lóbulos solapados.

La fase es relativa al ángulo del cigüeñal referenciado, incluida su posición inicial.
La fase geométrica de un cilindro **no** se suma de forma automática: quien escribe el modelo debe elegir el
ángulo de apertura de válvula adecuado para cada cilindro. Un ciclo de 720 grados distingue
revoluciones sucesivas del cigüeñal. No hay una fase implícita de cuatro tiempos inferida de
la posición del pistón, de la velocidad ni del tiempo transcurrido.

Para un ciclo `C`, un ángulo de apertura `a`, una duración `D`, una apertura máxima `u` y un ángulo de cigüeñal `theta`:

```text
s = modulo(theta - a, C)       // en [0, C)
opening = u sin²(pi s / D)     // 0 < s < D
opening = 0                   // en caso contrario, incluidos ambos límites
A_effective = A_orifice * opening
```

Este perfil y su primera derivada son continuos en los límites del lóbulo. La rotación
inversa lo recorre de nuevo; un cigüeñal parado mantiene su apertura actual y puede seguir fluyendo.
La apertura no elige el sentido del flujo: sigue aplicando la ley de orificio bidireccional
existente, gobernada por la presión. Su coeficiente de descarga permanece como un multiplicador aparte.

Para una restricción temporizada, `initial_input` y su canal de entrada opcional especifican la **apertura
máxima**, una fracción en [0, 1]. La cantidad del canal es `peak_opening`; cero desactiva
el lóbulo. La cantidad de salida `effective_opening` usa `Field.Opening` (campo KPI de JSON
`opening`) e informa la fracción real. `mass_flow` se evalúa con esa fracción.
Las restricciones sin temporización conservan su cantidad de entrada y su semántica existentes. Los cambios de pico
programados e interactivos conservan la validación atómica de entradas y las comprobaciones de revisión.

## Integración y recuperación

Los modelos temporizados usan la integración simétrica de medio paso de gas, paso completo de trabajo de cigüeñal y medio paso de gas,
incluido un recipiente fijo accionado por un cigüeñal independiente. El primer medio
usa el ángulo de cigüeñal inicial, y el segundo el ángulo resultante. El solver de gas
resuelve su propia dinámica de masa y energía dentro de cada medio paso. No localiza de forma continua
los flancos de válvula ni adapta el tick mecánico exterior.

Para cada lóbulo activo, el tick exterior debe cumplir:

```text
limit = min(0.25 rad, D / 8)
max(abs(theta_next - theta_old), dt * max(abs(omega_old), abs(omega_next))) <= limit
8 * binary64_epsilon * max(abs(theta_old), abs(theta_next)) <= limit
```

El límite por la velocidad en los extremos cubre también una inversión cuyo cambio neto de ángulo es pequeño.
El límite de precisión evita que un ángulo desenrollado pierda la resolución que necesita
su lóbulo. Un tick mal resuelto falla aunque ambos extremos estén cerrados; no puede
saltarse en silencio una apertura estrecha entera. Un pico desactivado no exige resolución del lóbulo.
Son protecciones numéricas, no una tolerancia de error ni una garantía para una dinámica arbitraria.

Un fallo devuelve `NumericalFailure` / `numerical_failure` y no confirma ninguna parte del
lote del llamador, incluidos los ticks anteriores y las entradas programadas. Reduce `step_ns` y vuelve a crear
el modelo o la sesión; asegúrate de que los eventos sigan alineados con el tick nuevo. Para ángulos iniciales muy grandes,
elige un ángulo equivalente coherente con la fase de cada componente conectado.
También aplican los límites existentes de cilindro y de gas. No se añade un estado de leva mutable oculto;
la posición del cigüeñal y las entradas de pico ya participan en las instantáneas, los hashes y las bifurcaciones.

Los casos de referencia suaves y sin pared muestran convergencia de segundo orden. Las temperaturas de pared
permanecen fijas durante el tick exterior, así que los modelos acoplados a la pared siguen siendo de primer orden. El
limitado de flujo existente cerca del equilibrio puede reducir el orden local. La conservación y la repetición no
demuestran por sí solas la precisión temporal.

## Evidencia y compatibilidad

Las comprobaciones incluyen valores analíticos de la envolvente, ciclos explícitos, fase módulo el ciclo, aceleración,
inversión, un cigüeñal estacionario, picos desactivados, cruces de un lóbulo entero con resolución insuficiente, cancelación,
reversión del lote completo, bifurcaciones independientes y avance e instantáneas sin asignaciones.

Una prueba de vaciado de recipiente fijo integra de forma independiente la exposición del seno al cuadrado y usa
la solución cerrada de descarga adiabática bloqueada. Los casos directo e inverso
convergen al refinar el tick. Una prueba aparte de cilindro móvil integra masa, energía
interna, movimiento del cigüeñal y una restricción dependiente del ángulo con EDO RK4 escritas
de forma independiente, cruzando ambos límites del lóbulo. El refinamiento de la referencia establece su propia precisión
antes de comparar los resultados de Core. Consulta [la validación](VALIDATION.es.md) para los umbrales.

Solo los modelos temporizados añaden la etiqueta 6 de huella, los identificadores de componente y de cigüeñal de destino y los
parámetros de perfil normalizados. Los modelos sin temporización conservan sus huellas y su avance anteriores. El asset v5
añade registros de temporización acotados y conserva los lectores v1–v4; fixtures auténticos anteriores comprueban
las huellas y la repetición actualizada. Consulta la [disposición del asset](ASSET_FORMAT.es.md).

El [laboratorio de cilindro temporizado por cigüeñal](../assets/labs/crank-timed-cylinder.power.json)
arrastra una cámara sintética a través de ciclos repetidos de 720 grados con perfiles de admisión y de
escape. Dos cambios de par programados varían la velocidad del cigüeñal; la propia distribución no tiene
programa temporal. La repetición de JSON/CLI, MCP y del asset coincide en los 63 límites de informe.
La compilación exporta `CrankTimedCylinder.powerasset`; Studio anima marcadores esquemáticos
desde los canales de apertura efectiva. La ejecución de Editor, Play e IL2CPP sigue pendiente.

El [ejemplo de reactor de combustión interna de Cantera](https://cantera.org/stable/examples/python/reactors/ic_engine.html)
es una referencia conceptual del control de puertos por ángulo de cigüeñal. Sus hipótesis de velocidad fija,
su ley de válvula y sus parámetros de ejemplo no se adoptan como calibración ni como verificación del
solver de Power!. Esta implementación usa el acoplamiento conservativo de cigüeñal del proyecto
y la ley de tobera bidireccional. La [combustión premmezclada](PREMIXED_COMBUSTION.es.md) aparte
añade ahora la contabilidad de combustible y de energía química. Todos los parámetros de muestra siguen siendo `unverified`;
el comportamiento completo del motor y la calibración medida del vehículo siguen abiertos.
