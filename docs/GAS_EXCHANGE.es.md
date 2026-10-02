# Primitivas de intercambio de gas

[English](GAS_EXCHANGE.md) · [简体中文](GAS_EXCHANGE.zh-CN.md) · [Français](GAS_EXCHANGE.fr.md) · [Русский](GAS_EXCHANGE.ru.md) · [日本語](GAS_EXCHANGE.ja.md) · [한국어](GAS_EXCHANGE.ko.md) · [Deutsch](GAS_EXCHANGE.de.md) · **Español** · [Italiano](GAS_EXCHANGE.it.md) · [Português](GAS_EXCHANGE.pt-BR.md)

Este documento registra el primer tramo del incremento de intercambio de gas descrito en
[las notas de reanudación del motor](NEXT_ENGINE_STEP.es.md): la física de flujo y de volumen de control, validada
por sí misma, antes de conectarla al grafo del modelo compilado.

Las primitivas viven en `src/Power.Core/GasExchange.cs` y las cubre
`tests/Power.Tests/GasChecks.cs`. Un [punto de control posterior de la red de gas en Core](GAS_NETWORK.es.md)
conecta ahora nodos de gas finitos, restricciones, enlaces térmicos y libros de conservación al
modelo compilado. La integración del 2026-09-22 añade JSON, CLI/MCP y el asset portátil v3,
y conserva los lectores v1/v2 para sus conjuntos de modelos originales. Las vistas esquemáticas de Unity y
las pruebas están preparadas; la verificación real del Editor sigue pendiente. El cilindro adiabático cerrado sigue siendo una
referencia analítica sin cambios, todavía sin intercambio de masa a través de su cámara acoplada al cigüeñal.

## Qué está implementado

| Tipo | Responsabilidad |
|---|---|
| `IdealGas` | Gas calóricamente perfecto de una composición fija: `R`, `gamma`, `cv`, `cp`, la relación de presiones crítica y los dos coeficientes de flujo másico de tobera precalculados. |
| `GasVolumeState` | Un volumen finito rastreado por **masa y energía interna como estados independientes**, con densidad, temperatura, presión y entalpía específica derivadas. |
| `Orifice` | Flujo compresible ideal a través de una restricción, con un coeficiente de descarga y una fracción de apertura adimensional en `[0,1]`, con signo en ambos sentidos, bloqueado y subcrítico. |

`GasVolumeState` sustituye de forma deliberada la derivación por ángulo y entropía inicial del cilindro cerrado.
Como la masa y la energía interna se transportan de forma independiente, el mismo estado puede absorber masa
transportada, entalpía transportada y calor de pared sin suponer una historia isentrópica.

## Ecuaciones

La presión estática usa `p = (gamma - 1) U / V`, exacta para un gas calóricamente perfecto, y evita
un viaje de ida y vuelta aparte por la temperatura. La temperatura es `T = U / (m cv)`.

El flujo másico sigue las relaciones estándar de una tobera isentrópica. Con `A` el área efectiva
(área geométrica × coeficiente de descarga × apertura), estado estático aguas arriba `p_u, T_u` y relación
de presiones `pr = p_d / p_u`:

- bloqueado, `pr <= (2/(gamma+1))^(gamma/(gamma-1))`:
  `mdot = A (p_u / sqrt(T_u)) sqrt(gamma/R) (2/(gamma+1))^((gamma+1)/(2(gamma-1)))`
- subcrítico: `mdot = A (p_u / sqrt(T_u)) sqrt(2 gamma / (R (gamma-1)) (pr^(2/gamma) - pr^((gamma+1)/gamma)))`

La corriente transporta la entalpía aguas arriba, `hdot = mdot cp T_u`, de modo que el sentido del flujo decide
qué temperatura de extremo se transporta. Un depósito se indica como un par ordinario `(p, T)`, así que
no hace falta un volumen ficticio para una frontera fija.

Referencia de las dos ramas: [bloqueo del flujo másico de la NASA](https://www.grc.nasa.gov/www/k-12/BGP/mflchk.html).
Es una referencia de las relaciones, no una validación de esta implementación.

## Notas numéricas

La función de flujo subcrítico se evalúa como `pr^(2/gamma) * (1 - pr^((gamma-1)/gamma))`, con el
segundo factor calculado mediante `expm1`. La diferencia de manual entre dos potencias casi iguales se cancela
de forma catastrófica cuando `pr` se acerca a uno: en `pr = 1 - 1e-12` conserva unos cuatro dígitos, mientras que
la forma con `expm1` es exacta hasta la precisión de la relación almacenada. `Numeric.Expm1` y `Numeric.Log1p`
se comparten ahora con la física del cilindro cerrado, en lugar de duplicarse.

Dos límites son propios del modelo y no de la implementación, y un solver que lo adopte
debe tratar ambos:

- La rama subcrítica tiene una **derivada infinita en la relación de presiones unidad**. Un paso de Newton
  no debe atravesar ese punto en línea recta; hay que acotarlo o amortiguarlo.
- Una relación próxima a uno no puede representarse de forma útil en binary64. En `pr = 1 - 1e-15` solo sobrevive
  alrededor de un dígito del desplazamiento, se escriba como se escriba la función.

El estancamiento aguas arriba y las condiciones estáticas se tratan como iguales. Es la aproximación habitual
cuasiestacionaria de volumen de control y **no** es válida para un flujo de cámara a alto Mach.

## Evidencia

`tests/Power.Tests/GasChecks.cs` añade seis comprobaciones, cada una escrita contra una forma cerrada independiente
y no contra una salida registrada de este código:

1. **Propiedades y continuidad del bloqueo** — `cv`, `cp` y la relación crítica frente a sus
   definiciones para `gamma` en `{1.1, 1.3, 1.4, 5/3}`; la rama subcrítica alcanza el
   coeficiente de bloqueo exactamente en la relación crítica; decaimiento monótono de la función de flujo hasta cero, comprobado
   contra la forma ingenua allí donde esa forma es fiable y contra el desarrollo de primer orden
   allí donde no lo es.
2. **Flujo de tobera** — 54 combinaciones de presión aguas arriba, temperatura aguas arriba y relación de presiones
   frente a las relaciones de la NASA escritas por completo, incluido que el flujo bloqueado es independiente de la
   presión aguas abajo y que la entalpía se transporta a la temperatura aguas arriba.
3. **Contratos** — antisimetría exacta al intercambiar los extremos, flujo nulo con un orificio cerrado y
   con presiones iguales, linealidad en la fracción de apertura, y rechazo de estados no finitos o
   no positivos, aperturas fuera de `[0,1]` y parámetros de gas u orificio no válidos.
4. **Vaciado adiabático de un recipiente** — integración RK4 de un recipiente de 2 L desde 20 bar y 900 K frente a la
   solución isentrópica analítica `rho(t) = (rho0^-k + k C t / V)^(-1/k)`, `k = (gamma-1)/2`, ajustada a
   1e-9 relativo en densidad y temperatura y 1e-8 en presión, con una comprobación de refinamiento.
5. **Llenado desde un depósito** — carga de un recipiente de 0.5 L desde un depósito a 6 bar y 320 K: la identidad exacta
   `dU = cp T_supply dm` mientras el flujo es unidireccional, y el límite del recipiente evacuado
   `T -> gamma T_supply`, comprobado desde dos presiones iniciales distintas.
6. **Red cerrada de dos volúmenes** — 2 s de intercambio entre un volumen caliente de 1.5 L y un volumen frío de 0.4 L:
   masa total conservada a 1e-14 relativo y energía interna total a 1e-12 relativo, presiones
   que se igualan, y equilibrio confirmado como mecánico y no como la temperatura de mezcla completa.

## Qué sigue abierto

El [punto de control de la red de gas en Core](GAS_NETWORK.es.md) cubre ahora nodos de volumen fijo,
depósitos, restricciones, enlaces térmicos, libros de masa y de energía, canales de salida y
el avance transaccional acotado. JSON, los assets portátiles, el descubrimiento de capacidades y los ejemplos
de repetición están integrados. La [extensión de cilindro móvil](MOVING_CYLINDER.es.md) acopla ahora el intercambio de gas y el
trabajo del cigüeñal. La [temporización opcional por ángulo de cigüeñal](VALVE_TIMING.es.md) controla las restricciones, y
la [combustión premmezclada](PREMIXED_COMBUSTION.es.md) añade combustible, aire y productos y la
contabilidad de energía química. La evidencia del Editor de Unity es una
entrega aparte. El método implícito por pares propuesto no se ha adoptado: el método
explícito actual, su limitador de equilibrio y sus límites de precisión están documentados allí.
Los volúmenes conectados exigen constantes de gas idénticas y el mismo gamma; la termoquímica detallada de especies sigue abierta.
