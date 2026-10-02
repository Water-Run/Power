# Física gestionada del embrague en seco

[English](CLUTCH_PHYSICS.md) · [简体中文](CLUTCH_PHYSICS.zh-CN.md) · [Français](CLUTCH_PHYSICS.fr.md) · [Русский](CLUTCH_PHYSICS.ru.md) · [日本語](CLUTCH_PHYSICS.ja.md) · [한국어](CLUTCH_PHYSICS.ko.md) · [Deutsch](CLUTCH_PHYSICS.de.md) · **Español** · [Italiano](CLUTCH_PHYSICS.it.md) · [Português](CLUTCH_PHYSICS.pt-BR.md)

`Power.Core` ofrece una ley de fricción `DryClutch` inmutable y un integrador de referencia `ClutchPair` para dos inercias bajo pares externos y acoplamiento constantes. Ambos compilan para `net10.0` y `netstandard2.1` sin dependencias de terceros.

Estos primitivos dan una referencia independiente del [componente de grafo de embrague](CLUTCH_NETWORK.es.md) ya integrado. El grafo acopla ejes, motores y cilindros, admite varios embragues y encaminamiento térmico, y conserva la reversión del lote completo a través de eventos internos. JSON, CLI/MCP, el asset v8 y Studio consumen esa definición de grafo. El par aislado documentado aquí sigue siendo una referencia de carga constante; no avanza por sí mismo una red compilada.

## Contrato de fricción

Todas las capacidades y reacciones se expresan en el puerto A. La relación con signo `r` usa la misma convención de potencia que el componente de eje existente:

```text
g       = omega_A - r * omega_B                  [rad/s]
tau_B   = -r * tau_A                            [Nm]
P_heat  = -tau_A * g                            [W]
C_s     = engagement * static_capacity          [Nm]
C_k     = engagement * sliding_capacity         [Nm]
```

La fracción de acoplamiento está en `[0,1]`. La capacidad estática es no negativa y no menor que la capacidad deslizante. Ambas pueden ser cero. Una capacidad estática efectiva nula desacopla el embrague. No se infieren presión de apriete, coeficiente de fricción, geometría de discos, pérdida por temperatura, desgaste, arrastre ni retardo del actuador.

Con deslizamiento distinto de cero, `tau_A = -sign(g) * C_k`. Con deslizamiento exactamente nulo, el sistema integrador debe aportar el par necesario para mantener nula la aceleración relativa. Si su magnitud es como máximo `C_s`, el embrague se bloquea en esa reacción y no produce calor de fricción. En caso contrario empieza a deslizar en la dirección de la carga desequilibrada, usando `C_k`. La igualdad en el límite estático permanece bloqueada. La ley no tiene banda muerta de velocidad y no convierte en silencio una velocidad relativa pequeña en una restricción de adherencia.

Esta distinción idealizada entre fricción cinética y una reacción estática restringida sigue la mecánica descrita por las referencias principales: [Modelica Clutch](https://doc.modelica.org/om/Modelica.Mechanics.Rotational.Components.Clutch.html) y [MathWorks Fundamental Friction Clutch](https://www.mathworks.com/help/sdl/ref/fundamentalfrictionclutch.html). La implementación de Power! está escrita de forma independiente y usa capacidades de par explícitas; no reproduce ninguna de las dos implementaciones ni reclama sus modelos constitutivos más amplios.

`ClutchMode` distingue `Disengaged`, `Locked`, `SlippingPositive` y `SlippingNegative`. Un modo a velocidad nula puede ser un estado de deslizamiento de partida cuando la carga externa supera la capacidad estática. `HeatFlowWatts` es instantáneo; su valor en esa partida a velocidad nula es cero aunque el calor posterior sea positivo.

## Par exacto de carga constante

Para dos inercias positivas `J_A`, `J_B` y pares externos constantes `T_A`, `T_B`:

```text
J_A * domega_A/dt = T_A + tau_A
J_B * domega_B/dt = T_B - r * tau_A
D                 = 1/J_A + r*r/J_B
b                 = T_A/J_A - r*T_B/J_B
required_tau_A    = -b/D
dg/dt             = b + D*tau_A
```

Cada fase de deslizamiento tiene aceleración constante. Si su velocidad relativa llega a cero dentro del intervalo pedido, el solver avanza exactamente hasta `t_zero = -g / (dg/dt)` y evalúa la reacción estática. Después integra el resto, ya bloqueado o deslizando en la dirección opuesta. El forzamiento constante admite como máximo una llegada así, de modo que la resolución requiere como máximo dos fases, sin bucle de convergencia ni subdivisión temporal. Un evento exactamente en el extremo del intervalo devuelve su modo de reacción por la derecha.

La trayectoria bloqueada obedece `omega_A = r*omega_B` con

```text
domega_B/dt = (r*T_A + T_B) / (r*r*J_A + J_B).
```

En una llegada calculada, una proyección que preserva el momento elimina el residuo de redondeo del evento en binary64. Usa pesos inerciales acotados en lugar de formar grandes sumas ponderadas por inercia. Una vez bloqueado, la restricción de velocidad se construye de forma explícita. Es una corrección de redondeo en un evento resuelto, no un acoplamiento instantáneo inelástico de un deslizamiento finito. Los avances angulares integran cada fase de aceleración constante. El trabajo externo es `T_A*delta_theta_A + T_B*delta_theta_B`; el calor de fricción es la integral de `-tau_A*g`. El resultado incluye el impulso de par con signo en A y el cambio de energía cinética comprobable por separado. El residuo de energía es `external_work - heat - delta_kinetic`.

El par admite cualquiera de los dos signos de un `r` finito y distinto de cero. Su momento generalizado `r*J_A*omega_A + J_B*omega_B` cambia solo a través de `r*T_A + T_B`. La conservación del momento angular ordinario se aplica cuando `r = 1`; una relación representa un transformador mecánico ideal cuyo soporte puede reaccionar par. Para un freno a masa, construye `ClutchPair.Brake(J, friction)`. El puerto B tiene entonces velocidad y par externo fijos en cero, y `r = 1`. No se usa el infinito como centinela de inercia.

## API y comportamiento ante fallos

```csharp
var pair = new ClutchPair(0.2, 0.8, new DryClutch(20, 10));
var status = pair.Advance(new ClutchPairState(100, 0),
    externalTorqueANewtonMeters: 0,
    externalTorqueBNewtonMeters: 0,
    engagement: 1,
    durationSeconds: 4,
    out var step);
```

El ejemplo alcanza 20 rad/s en ambos puertos tras 1.6 s y genera 800 J de calor. Sus avances angulares a lo largo de cuatro segundos son 144 rad y 64 rad. Todos los números son sintéticos, sin afirmación de calibración del vehículo.

Las dos clases son inmutables. `ClutchPairState` y `ClutchPairStep` son tipos valor. `Advance` ni muta el estado del llamador ni asigna memoria. Llamadores independientes pueden compartir el mismo par. No hay historial de fase retenido ni reloj global de simulación; las velocidades aportadas y las nuevas cargas constantes determinan el intervalo siguiente.

| Estado | Significado y recuperación |
|---|---|
| `Ok` | Hay un resultado local finito y completo; evalúa por separado la conservación y la idoneidad del modelo |
| `InvalidDuration` | Aporta un intervalo finito y estrictamente positivo, en segundos |
| `InvalidEngagement` | Aporta una fracción finita en `[0,1]` |
| `InvalidState` | Aporta velocidades finitas; un freno a masa exige velocidad B igual a cero |
| `InvalidTorque` | Aporta pares externos finitos; un freno a masa exige par B igual a cero |
| `NumericalFailure` | El movimiento derivado, el instante del evento o la energía supera el rango binary64 admitido; revisa unidades/escalas y acorta o reformula el intervalo |

Ante cualquier rechazo la salida es `default`; no hay estado publicado de forma parcial. Los parámetros inmutables inválidos lanzan `ArgumentException` o sus subclases en la construcción. `DryClutch.Evaluate` rechaza igualmente entradas inválidas o un calor instantáneo que desborda. Un instante de evento que se subdesborda a cero falla, en lugar de descartar en silencio energía cinética relativa finita. Los resultados físicos siguen sujetos al redondeo de coma flotante; las entradas finitas por sí solas no garantizan cantidades derivadas representables.

`ZeroSlipTimeSeconds` es la primera llegada acoplada a velocidad relativa nula, o cero cuando el intervalo empieza ahí. Es nulo cuando no hay tal llegada, incluido el movimiento desacoplado. No implica adherencia: una carga externa grande puede provocar una inversión inmediata. `SlippingDurationSeconds` incluye las fases de deslizamiento de partida; el movimiento desacoplado queda excluido. `EndReaction` es instantáneo en el estado final, mientras que el calor, el trabajo, el impulso y los avances angulares se integran sobre el intervalo completo.

El parámetro local en segundos no sustituye el reloj de nanosegundos fijo y acotado de `Simulation`. La integración del grafo conserva límites exactos de tick y de evento externos, hashes de estado, independencia de las bifurcaciones, cancelación y reversión completa de varios ticks.

## Evidencia y límites

[ClutchChecks.cs](../tests/Power.Tests/ClutchChecks.cs) ejecuta los mismos diez grupos contra ambos ensamblados de destino de Core:

- Tiempo de sincronización de dos inercias en forma cerrada, velocidad, avances angulares, impulso, momento y energía cinética perdida, con acoplamiento total y parcial.
- Reparto exacto de carga estática, umbral de despegue inclusivo, una capacidad cinética menor, deslizamiento distinto de cero sin banda muerta y un enclavamiento estático de capacidad cinética nula.
- Inversión dentro de un intervalo y en su extremo, además de frenado a masa, retención y partida bajo carga excesiva.
- Relaciones positivas y negativas, momento generalizado y cambios de energía calculados de forma independiente. Dos mil combinaciones deterministas barren inercia, relación, velocidad, carga externa, capacidad y duración.
- Invariancia de partición a través de eventos híbridos bajo forzamiento constante. El muestreo en el punto medio de cargas sinusoidales cambiantes converge frente a integrales analíticas independientes de velocidad, ángulo y calor. Esto demuestra el comportamiento de segundo orden de ese ejemplo de muestreo de carga; el grafo tiene sus propias comprobaciones de convergencia acoplada e híbrida.
- Entradas inválidas, desbordamiento, un evento irresoluble, salida por defecto ante fallo, evaluaciones repetidas independientes y asignación cero a lo largo de 10,000 intervalos correctos.

El par devuelve el calor como energía generada; el componente de grafo lo encamina a un nodo térmico o al libro externo. Ninguna de las dos API implementa topología DCT/AT, selección de marcha, un convertidor de par, actuadores hidráulicos, coordinación ECU/TCU, identificación del material del embrague ni calibración medida. Esos límites siguen en la [hoja de ruta](ROADMAP.es.md).
