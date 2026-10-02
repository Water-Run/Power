# Simulación acoplada de embrague

[English](CLUTCH_NETWORK.md) · [简体中文](CLUTCH_NETWORK.zh-CN.md) · [Français](CLUTCH_NETWORK.fr.md) · [Русский](CLUTCH_NETWORK.ru.md) · [日本語](CLUTCH_NETWORK.ja.md) · [한국어](CLUTCH_NETWORK.ko.md) · [Deutsch](CLUTCH_NETWORK.de.md) · **Español** · [Italiano](CLUTCH_NETWORK.it.md) · [Português](CLUTCH_NETWORK.pt-BR.md)

El componente `clutch` gestionado conecta dos nodos rotacionales, o un rotor a masa. Participa en la resolución electromecánica/de cilindro existente y encamina el calor de fricción generado a un nodo térmico o al libro externo de calor. JSON, la CLI, el MCP, el asset v10 y Studio usan las mismas definiciones. Esto implementa un elemento de acoplamiento de la transmisión; la topología DCT/AT completa, la dinámica de bomba/pistón y la coordinación ECU/TCU siguen siendo trabajo aparte. La [red hidráulica](HYDRAULIC_NETWORK.es.md) acciona ahora una variante de embrague operada por presión. El [convertidor mapeado](CONVERTER_NETWORK.es.md) comparte ahora esta resolución y usa un embrague paralelo aparte para el bloqueo.

El [contrato físico del embrague en seco](CLUTCH_PHYSICS.es.md) define la ley de Coulomb y una referencia exacta e independiente de dos inercias bajo cargas constantes. El solver de grafo de abajo extiende esa ley a redes acopladas. No congela el par del motor ni la corriente del motor eléctrico en una entrada unidireccional hacia el embrague.

## Definición y canales

```json
{
  "id": 16,
  "kind": "clutch",
  "node_a": 1,
  "node_b": 4,
  "heat_node": 5,
  "input_channel": 104,
  "initial_input": { "value": 0, "unit": "fraction" },
  "parameters": {
    "static_capacity": { "value": 100, "unit": "nm" },
    "sliding_capacity": { "value": 30, "unit": "nm" },
    "ratio": 1
  }
}
```

`node_a` es un nodo rotacional. `node_b` es un nodo rotacional distinto, o se omite o vale cero para un freno a masa. `ratio` es finita y distinta de cero; un freno a masa exige uno. La capacidad estática es al menos la capacidad deslizante, y ambas son pares finitos y no negativos en el puerto A. `initial_input` es una fracción de acoplamiento explícita en `[0,1]`, que escala ambas capacidades. Un canal de entrada opcional cambia el acoplamiento en límites exactos de tick externo. Un `heat_node` omitido o cero envía el calor al libro externo de rechazo; un sumidero indicado debe ser un nodo térmico. La temperatura no altera estas capacidades.

La fábrica de Core es `ComponentDefinition.Clutch(id, a, b, staticCapacity, slidingCapacity, channel, engagement, ratio, heat)`. Su descriptor `Friction` contiene las dos cantidades de par explícitas. Los modelos compilados copian sus parámetros, ordenan por ID estable y añaden la etiqueta de huella 8 solo cuando hay embragues. Cada fase de embrague cuenta contra el límite existente de 64 estados. Los modelos anteriores conservan sus huellas y hashes de repetición. El nombre de fidelidad combinado es `hybrid_clutch_powertrain`, y la calibración sigue siendo `unverified`.

| Campo | Unidad | Significado |
|---|---|---|
| `slip_speed` | rad/s | `omega_A - ratio*omega_B` actual |
| `clutch_mode` | StateCode | Fase del último intervalo aceptado: 0 desacoplado, 1 bloqueado, 2 deslizamiento positivo, 3 deslizamiento negativo |
| `torque` | Nm | Reacción media en A durante el último tick externo completo |
| `heat_flow` | W | Potencia de fricción media generada durante ese tick |
| `friction_heat` | J | Calor generado acumulado, sea cual sea el destino |

El par, la potencia y el calor iniciales son cero; la fase inicial se infiere del acoplamiento y de la velocidad relativa, antes de resolver una reacción de carga. Una fase describe el intervalo resuelto, así que una llegada exactamente en su extremo puede seguir mostrando la fase de aproximación hasta la resolución siguiente. Una entrada de frontera cambia el estado de entrada de inmediato y no reescribe el historial de salida del intervalo precedente. Esto también se aplica a los eventos de entrada al final de una llamada `Step`. Las medias de par y de potencia incluyen cada intervalo interno aceptado.

## Integración acoplada y eventos

Para `g = omega_A - r*omega_B`, los pares de puerto son `tau_A = tau`, `tau_B = -r*tau`. La potencia mecánica retirada es `-tau*g`; esta convención de signo funciona con cualquiera de los dos signos de `r`. El desacoplamiento fija par cero. El deslizamiento usa la capacidad cinética que se opone al deslizamiento. Un embrague bloqueado impone velocidad relativa nula en el punto medio, con una reacción acotada por la capacidad estática. Esto da trabajo bloqueado ideal nulo sin insertar un amortiguador artificial ni un resorte de penalización rígido.

Cada intervalo interno usa las ecuaciones electromecánicas de punto medio implícito existentes y la resolución conservativa de trabajo por presión del cilindro. Las respuestas de fuerza del embrague se obtienen de los mismos factores lineales acoplados. Una resolución de Gauss–Seidel proyectada determina las reacciones estáticas acotadas mientras se recalcula el par del cilindro para las fuerzas actuales. Las restricciones estáticas saturadas se liberan cuando el movimiento exigido supera la tolerancia de velocidad. El conjunto activo se reconsidera si otra restricción cambia una dirección de partida. Se permiten bucles de embrague redundantes; sus reacciones individuales pueden no ser únicas. El orden estable de componentes elige una asignación determinista, mientras que las pruebas comprueban el movimiento resultante, los límites de capacidad, el momento total y la energía.

Si un intervalo de deslizamiento invierte su velocidad relativa, una bisección acotada localiza la frontera observada de deslizamiento nulo y repite el intervalo desde una copia completa del estado. El intervalo siguiente o se adhiere o parte con la reacción cinética opuesta. La dinámica, los factores térmicos y las respuestas de fuerza del cilindro se recalculan para cada duración candidata; todos los factores mutables pertenecen a la simulación individual. El modelo compilado permanece inmutable.

La tolerancia de la restricción es `2e-13 + 32*epsilon*(abs(omega_A)+abs(r*omega_B))` rad/s, con `epsilon = 2.2204460492503131e-16`. La captura acepta raíces dentro de dieciséis veces esa tolerancia. No proyecta fuera un deslizamiento finito ni descarta energía cinética finita. Un trabajo de fricción negativo minúsculo, dentro del doble de la tolerancia par-por-velocidad del intervalo, se fija en cero; un trabajo negativo mayor falla. Las comprobaciones de conservación incluyen este efecto de redondeo. Un residuo dentro de la tolerancia de raíz no puede crear un segundo evento espurio.

La resolución permite como máximo 32 intervalos internos por tick exterior, 56 iteraciones de raíz, 256 iteraciones de restricción por conjunto activo y `2*clutch_count+2` intentos de conjunto activo. Factores no finitos, restricciones no convergidas, eventos no resueltos, presupuestos agotados o los límites existentes de cilindro/gas devuelven `NumericalFailure`. La cancelación se comprueba durante el trabajo acotado de restricción y de raíz. Reduce el tick externo e inspecciona las escalas de inercia y de relación, las restricciones redundantes y los programas de capacidad; no interpretes una llamada fallida como un acoplamiento completado de forma parcial.

El calor generado se integra como `-duration*tau*g_mid` y después se añade a la resolución térmica o al libro externo de calor. Todo el trabajo de fuentes de los intervalos aceptados, el transporte de gas, el historial químico, el intercambio de pared y el rechazo térmico entran en la contabilidad de energía existente. La fase del embrague, las salidas medias, el calor acumulado y la suma de calor compensada se copian y se hashean con el estado físico. Una llamada de varios ticks fallida o cancelada restaura el estado inicial completo, incluidas las entradas programadas, la fase y el calor. Las bifurcaciones comparten solo datos del modelo compilado. El tiempo externo sigue siendo un recuento entero acotado de nanosegundos; las duraciones de eventos internos no introducen ticks fraccionarios visibles desde fuera.

La prueba no lineal de despegue usa la demanda de par media del intervalo. No localiza el instante exacto de tiempo continuo en el que una carga estática cambiante supera por primera vez la capacidad. Del mismo modo, el encuadre de eventos concierne a la trayectoria discreta de punto medio; un tick grande puede perder oscilaciones físicas rápidas cuyos extremos ocultan una inversión. Refina el tiempo alrededor de las transiciones y compara las salidas. La dinámica electromecánica suave conserva la precisión de punto medio, el acoplamiento térmico y de pared sigue siendo de primer orden, y no se afirma un segundo orden universal para todas las trayectorias con conmutación.

## Laboratorio y evidencia

El [laboratorio de embrague encendido](../assets/labs/fired-clutch.power.json) conecta el cilindro premmezclado a una carga inercial aparte y a un nodo térmico del embrague. Seis eventos en ticks exactos aplican acoplamiento parcial y total, par de carga, liberación y reacoplamiento. Los parámetros son sintéticos. A lo largo de 0.6 segundos, el informe actual de Linux registra:

| Cantidad | Resultado |
|---|---|
| Velocidad final de motor/carga | 68.58488546 rad/s |
| Trabajo neto de fuentes externas, incluida la carga y la contrapresión del cilindro | -96.74607609 J |
| Calor de embrague generado | 191.55570747 J |
| Temperatura final del nodo térmico del embrague | 300.95777854 K |
| Calor de combustible liberado | 1,630.91064291 J |
| Deslizamiento final | 2.84e-14 rad/s, fase bloqueada |
| Residuo final de energía | 1.79e-10 J |
| Huella del modelo / hash de estado final | `197be44884deee90` / `28bf5335d8e35cde` |

Los 67 límites del informe coinciden con tamaños de lote alternos, reproducción portátil y repetición real del servidor MCP hijo. El informe es `artifacts/reports/fired-clutch.json`. Pide `get_example_model` con `name: "fired-clutch"`, o ejecuta:

```sh
dotnet src/Power.Cli/bin/Release/net10.0/Power.Cli.dll assets/labs/fired-clutch.power.json --output artifacts/reports/fired-clutch.json
```

Las comprobaciones de Core comparan acoplamiento, frenado e inversión frente a `ClutchPair`, incluidas relaciones con signo y ambos destinos del calor. Un motor RL bloqueado coincide con un modelo de inercia combinada de forma analítica; un cilindro de gas reactivo bloqueado coincide igualmente con su modelo independiente de inercia equivalente, incluida la presión y el consumo de combustible. Un oscilador de resorte y freno coincide con el movimiento sinusoidal analítico a trozos a través de tres inversiones y un cuarto punto de giro que captura, y el refinamiento reduce el error en más de 3.7x por cada reducción a la mitad. Los bucles de tres embragues ejercitan restricciones redundantes y acoplamiento simultáneo. Las pruebas cubren también la reversión completa tras un prefijo correcto de calentamiento y captura, la cancelación, la repetición programada exacta, la propiedad inmutable, la independencia de ramas y el funcionamiento sin asignaciones, incluidos eventos internos de inversión repetidos.

El asset v10 recorre en ambos sentidos las capacidades explícitas y todos los canales. Se rechazan recuentos mal formados, registros ausentes, duplicados o de tipo incorrecto, unidades incorrectas, límites inválidos y degradaciones falsificadas. Un fixture auténtico v6 de cilindro encendido conserva su resumen, su huella y la repetición actualizada. Las pruebas estrictas de JSON y de agente distinguen una ejecución correcta de unos KPIs aprobados. Consulta [VALIDATION.es.md](VALIDATION.es.md).

La compilación exporta `FiredClutch.powerasset`. Studio prepara dos discos esquemáticos de embrague, colores de fase y una salida de fase con nombre, junto a controles de acoplamiento y canales de calor. Las pruebas de importación y del ciclo de vida de Play están preparadas. La evidencia real de Unity Editor, renderizado, Play Mode e IL2CPP sigue pendiente; las comprobaciones de ensamblados Standard alojados en .NET no la sustituyen.

## Acoplamiento permanente de engranajes

Las [restricciones ideales de engranaje y planetario](GEAR_NETWORK.es.md) proyectan ahora el punto medio libre y las respuestas de fuerza de embrague y de cilindro en el mismo espacio de restricciones permanentes. El laboratorio planetario encendido combina un freno de corona y un embrague sol/corona con un planetario ideal y una reducción final, y repite una subida y una bajada de marcha. Un embrague cuya velocidad relativa ya está restringida de forma permanente se rechaza como reacción independiente indefinida. El resto de contratos de estado, capacidad, térmica y eventos del embrague no cambian.
