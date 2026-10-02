# Intercambio de gas del cilindro móvil

[English](MOVING_CYLINDER.md) · [简体中文](MOVING_CYLINDER.zh-CN.md) · [Français](MOVING_CYLINDER.fr.md) · [Русский](MOVING_CYLINDER.ru.md) · [日本語](MOVING_CYLINDER.ja.md) · [한국어](MOVING_CYLINDER.ko.md) · [Deutsch](MOVING_CYLINDER.de.md) · **Español** · [Italiano](MOVING_CYLINDER.it.md) · [Português](MOVING_CYLINDER.pt-BR.md)

Un `gas_cylinder` conecta un cigüeñal rotacional con una cámara de gas. A diferencia de la referencia
adiabática cerrada, esta cámara transporta masa y energía interna independientes, de modo que
las restricciones y los enlaces de pared pueden cambiar su estado mientras la presión acciona el cigüeñal.
El componente está disponible a través de Core, JSON, CLI, MCP y los assets portátiles. No
modela la inercia del pistón ni la química detallada. Los componentes aparte de
[combustión premmezclada](PREMIXED_COMBUSTION.es.md) y de [temporización por ángulo de cigüeñal](VALVE_TIMING.es.md)
aportan ahora la conversión de energía del combustible y controlan las restricciones conectadas.

## Contrato del modelo

```csharp
var definition = new ModelDefinition
{
    StepNanoseconds = 50_000,
    Nodes = [NodeDefinition.Rotor(1, 0.2, 60),
        NodeDefinition.CylinderGas(2, 100_000, 300),
        NodeDefinition.Thermal(3, 500, 350)],
    Components = [ComponentDefinition.GasCylinder(10, 1, 2, new()
    {
        Bore = new(86, Unit.Millimeter), Stroke = new(86, Unit.Millimeter),
        RodLength = new(143, Unit.Millimeter), Phase = new(0, Unit.Radian),
        CompressionRatio = 10, BackPressure = new(1, Unit.Bar)
    }), ComponentDefinition.GasReservoir(11, 2, 50e-6, 110_000, 300, 0.8, 100, 1),
        ComponentDefinition.GasHeatLink(12, 2, 3, 2.5)]
};
```

El nodo de gas aporta la presión absoluta inicial, la temperatura, R y gamma. Su cantidad `Storage`
es cero/`None`: exactamente un componente de cilindro posee el volumen. El compilador
deriva la masa y la energía iniciales a partir de la geometría en el ángulo inicial del cigüeñal, incluida
la fase. Rechaza un volumen especificado de forma independiente o dos propietarios de cilindro para una cámara.
Los nodos de gas fijos no conectados siguen exigiendo un volumen explícito positivo.

En JSON, usa `domain: "gas"` y omite `storage` para una cámara móvil. El componente `gas_cylinder`
exige `node_a` (rotacional), `node_b` (gas) y los parámetros `bore`, `stroke`,
`rod_length`, `phase`, `compression_ratio` y `back_pressure`. En este componente no se duplican la composición ni el estado
inicial del gas. El nodo de gas expone presión, temperatura,
masa y energía interna; el cilindro expone volumen, desplazamiento del pistón y par
de cigüeñal. Los puertos, las restricciones, los límites de apertura y los enlaces de pared usan el
[contrato de red de gas](GAS_NETWORK.es.md) existente.

Los modelos con cámaras móviles sin restricciones temporizadas ni seguimiento premmezclado informan la fidelidad `moving_cylinder_gas_exchange` y
añaden la etiqueta 5 de huella del solver. Los modelos lineales, de cilindro cerrado y solo de volumen fijo
existentes conservan sus huellas y su avance. La composición permanece fija, los nodos de gas conectados
deben coincidir y todos los parámetros siguen siendo `unverified`.

## Ecuaciones y acoplamiento conservativo

Para un gas calóricamente perfecto de composición fija:

```text
p = (gamma - 1) U / V(theta)
T = U / (m cv)
dm/dt = sum(inflow) - sum(outflow)
dU/dt = -p dV/dt + Q_wall + sum(mdot_in h_in) - sum(mdot_out h_out)
tau_gas = (p - p_back) dV/dtheta
```

El balance de masa y energía sigue la primera ley estándar de un sistema abierto; consulta
[las ecuaciones de volumen de control de Cantera](https://www.cantera.org/stable/reference/reactors/controlreactor.html).
Esa referencia respalda las ecuaciones, no el esquema de integración de Power! ni su validación.
El gas usa la ley de tobera compresible bidireccional existente, no la implementación
lineal de válvula unidireccional de Cantera. El trabajo de contrapresión es trabajo de fuente externo;
la entalpía del depósito y el intercambio con la pared conservan los signos de libro existentes.

La implementación usa un reparto simétrico de operadores para los modelos con cámaras móviles:

1. Avanza el intercambio de gas y el calor de pared medio tick con la geometría de cigüeñal inicial.
2. Resuelve la electromecánica acoplada y el trabajo de presión adiabático durante el tick completo con
   el solver de cigüeñal de gradiente discreto acotado.
3. Avanza el intercambio de gas y el calor de pared medio tick con la geometría de cigüeñal resultante.
4. Aplica el calor acumulado de gas a pared y las pérdidas electromecánicas a la resolución térmica.

Durante el paso 2, la masa es fija y `U_new = U_old (V_old/V_new)^(gamma-1)`. La presión media del gas
y el par salen de la diferencia dividida de este mismo cambio de energía.
El cigüeñal gana el trabajo del gas menos el trabajo de contrapresión; la cámara pierde exactamente el
trabajo de gas correspondiente, con la exactitud del coma flotante. `log1p`/`expm1` y la diferencia
dividida analítica del volumen evitan restar estados casi iguales en pasos pequeños y
en los puntos muertos. Varios cilindros pueden compartir un cigüeñal o actuar a través de ejes acoplados.

El reparto tiene convergencia de segundo orden para el caso probado de flujo bloqueado suave sin
transferencia de pared. Las temperaturas de pared permanecen fijas durante ambos medios pasos de gas, y después
sigue la resolución térmica existente: la precisión con acoplamiento de pared sigue siendo de primer orden. El limitador
de flujo cerca del equilibrio también puede cambiar el orden local. La conservación no establece la precisión.

## Límites, fallo y compatibilidad

El recorrido del cigüeñal se limita a 0.25 rad por tick; la resolución no lineal usa como máximo 16
iteraciones y 10 ensayos de búsqueda lineal. Cada medio paso de gas conserva su límite de 4096 subpasos,
el objetivo del 2 % de cambio relativo y el rechazo del cambio corregido del 25 %. Los estados, las salidas o el agotamiento
del solver de gas no válidos o no finitos rechazan el lote entero del llamador, incluidos todos los ticks
anteriores y las entradas programadas. Reduce `step_ns` e inspecciona el área de flujo, el estado del gas, la conductancia,
la velocidad del cigüeñal y la inercia antes de reintentar. La cancelación y las bifurcaciones conservan todo el estado de gas y de libro;
el avance con éxito y las instantáneas del búfer del llamador no asignan memoria administrada.

El asset v4 añade un registro de geometría indexado y acotado para cada cilindro de gas, y conserva todos
los lectores v1/v2/v3. No serializa espacios de trabajo del solver. Un fixture auténtico de volumen fijo v3
verifica que introducir la geometría móvil no cambia las huellas de gas anteriores
ni la repetición. Consulta el [formato de asset](ASSET_FORMAT.es.md) y la [procedencia de los fixtures](../tests/Power.Tests/Fixtures/README.md).

## Experimento y evidencia

El [laboratorio de cilindro móvil](../assets/labs/moving-cylinder.power.json) arrastra un
cilindro con dos restricciones de depósito y una pared térmica finita. Los ocho eventos de apertura
basados en el tiempo ejercitan el flujo hacia la cámara y desde ella, e incluyen ticks entre límites
de informe y de presentación. Es un experimento remolcado no calibrado; el programa
no es una ECU, un perfil de leva, un controlador de motor de cuatro tiempos ni un modelo de combustión.

Las pruebas comparan una cámara cerrada con la implementación existente del cilindro cerrado en
rotación directa e inversa y en los puntos muertos, y comparan una cámara abierta con una
integración RK4, escrita de forma independiente, de las EDO que gobiernan el sistema. Esta última escribe la geometría,
el flujo másico bloqueado y el trabajo de presión directamente desde las ecuaciones. Las comprobaciones de refinamiento de paso
separan la precisión de flujo suave y la del acoplamiento de pared. Otras comprobaciones cubren varios
cigüeñales acoplados o compartidos, cilindros cerrados y abiertos mezclados, conservación, propiedad mal formada,
normalización de unidades, fallo y recuperación atómicos, bifurcaciones, cancelación, cero asignaciones,
compatibilidad portátil y todos los límites de informe de JSON, MCP y del asset.

Unity incluye una vista de pistón móvil, conexiones de gas y pruebas de importación y de Play. La evidencia real
de Editor, renderizado, Play Mode e IL2CPP sigue pendiente. Consulta el
[registro de validación](VALIDATION.es.md) para las comprobaciones ejecutadas y la [hoja de ruta](ROADMAP.es.md)
para el trabajo pendiente de motor, transmisión, control y calibración.
