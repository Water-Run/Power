# Referencias de engranaje ideal y planetario

[English](IDEAL_GEARS.md) · [简体中文](IDEAL_GEARS.zh-CN.md) · [Français](IDEAL_GEARS.fr.md) · [Русский](IDEAL_GEARS.ru.md) · [日本語](IDEAL_GEARS.ja.md) · [한국어](IDEAL_GEARS.ko.md) · [Deutsch](IDEAL_GEARS.de.md) · **Español** · [Italiano](IDEAL_GEARS.it.md) · [Português](IDEAL_GEARS.pt-BR.md)

`Power.Core` ofrece dos referencias de carga constante inmutables y sin asignaciones: `IdealGearPair` y `SimplePlanetaryGear`. Devuelven velocidades de los miembros, avances angulares, pares de reacción, trabajo externo, cambio de energía cinética y un residuo de energía. Aportan evidencia analítica independiente para el solver de transmisión acoplado. El [solver de engranajes acoplado](GEAR_NETWORK.es.md), aparte, expone ahora componentes permanentes de engranaje y planetario a través de JSON, assets portátiles y CLI/MCP, incluidos experimentos de cambio gobernados por embrague. Las clases de referencia siguen siendo soluciones analíticas locales puras.

## Alcance físico y signos

Un engranaje ideal no tiene inercia de engrane, flexibilidad, juego ni pérdidas; todas las inercias aportadas son inercias de rotor acopladas. Las dos inercias de un par, o los tres miembros de un planetario, deben ser positivas y finitas. No se infieren una masa fija ni nodos sin masa a partir de una inercia nula. La abstracción sigue el alcance de [IdealGear](https://doc.modelica.org/Modelica%204.0.0/Resources/helpWSM/Modelica/Modelica.Mechanics.Rotational.Components.IdealGear.html) e [IdealPlanetary](https://doc.modelica.org/Modelica%204.0.0/Resources/helpWSM/Modelica/Modelica.Mechanics.Rotational.Components.IdealPlanetary.html) de la Modelica Standard Library. La implementación de Power! está escrita de forma independiente; no se incluye ni se llama a ninguna implementación de terceros.

Para un par, la relación con signo `r` define:

```text
omega_A = r omega_B
reaction_A = lambda
reaction_B = -r lambda
```

Las relaciones positivas dan la misma dirección de puerto; las negativas la invierten. Las reacciones son pares **sobre los rotores acoplados**, no los pares que los rotores aplican al engranaje. Realizan trabajo neto cero para un movimiento compatible. La carcasa de un par de engranajes puede llevar una reacción; el momento angular ordinario de los dos rotores, por sí solo, no se conserva en general. El momento generalizado `r J_A omega_A + J_B omega_B` cambia con el par externo generalizado `r T_A + T_B`.

Para el planetario simple, sol, corona y portasatélites comparten un eje positivo. La relación de dientes `k = N_ring / N_sun` debe ser mayor que uno. La relación cinemática y los dos grados de libertad independientes concuerdan con las [ecuaciones de engranaje planetario de MathWorks](https://www.mathworks.com/help/sdl/ref/planetarygear.html).

```text
omega_S + k omega_R = (1+k) omega_C
reaction_S = lambda
reaction_R = k lambda
reaction_C = -(1+k) lambda
```

Estas reacciones suman cero y realizan trabajo neto cero. El modelo acepta una relación continua, sin inferir números de dientes, módulo, resistencia del diente ni geometría fabricable. La inercia de giro y orbital de los satélites, las pérdidas, los cojinetes, la lubricación y el comportamiento térmico quedan fuera de esta referencia.

## Solución independiente en coordenadas reducidas

El movimiento del par usa el puerto B como coordenada independiente:

```text
J_equivalent_B = r^2 J_A + J_B
alpha_B = (r T_A + T_B) / J_equivalent_B
alpha_A = r alpha_B
```

El planetario elimina el movimiento del portasatélites antes de formar su matriz de masa de energía cinética. Con `a = 1/(1+k)` y `b = k/(1+k)`:

```text
omega_C = a omega_S + b omega_R

M = [ J_S + a^2 J_C,    ab J_C        ]
    [ ab J_C,           J_R + b^2 J_C ]

M [alpha_S, alpha_R]^T = [T_S + a T_C, T_R + b T_C]^T
alpha_C = a alpha_S + b alpha_R
```

La implementación escala esta matriz de dos por dos y desarrolla su determinante en términos positivos para evitar restar productos casi iguales. Para cargas constantes, la aceleración es constante, así que la velocidad y el desplazamiento siguen una integración temporal lineal/cuadrática exacta hasta el redondeo de coma flotante. Los pares de reacción se recuperan después de las ecuaciones de los miembros. Las pruebas usan un multiplicador independiente de restricción de aceleración para el planetario libre; no reutilizan la matriz reducida como solución esperada.

## Contrato de estado, unidades y fallos

Los nombres públicos de propiedad llevan unidades SI: kg m2, rad/s, rad, N m, J y segundos. Las relaciones son adimensionales. `Advance` toma una duración local positiva y finita y devuelve `GearStepStatus`. Esta duración local de referencia no sustituye el reloj entero acotado de `Simulation`. Las clases no guardan estado evolutivo. Las entradas son registros por valor; la salida es el valor por defecto en cada rechazo, y las instancias pueden compartirlas llamadores independientes.

Las velocidades iniciales ya deben satisfacer la relación. La compatibilidad usa una prueba de redondeo relativo con épsilon de binary64 `2.2204460492503131e-16`, sin banda muerta absoluta de baja velocidad. Para un par, el límite es `64 epsilon (|omega_A| + |r omega_B|)`. El planetario incluye además las magnitudes de sus dos términos de velocidad ponderados, de modo que la cancelación se trata respecto de las operaciones que formaron la velocidad del portasatélites. Los términos se escalan antes de sumarlos para evitar desbordar la tolerancia.

Tras la validación, la velocidad dependiente y el avance angular se reconstruyen a partir de coordenadas independientes. Esto elimina el residuo de redondeo aceptado; no es un cálculo de acoplamiento con deslizamiento finito ni de sincronización. Las velocidades incompatibles devuelven `IncompatibleState`. Usa un modelo explícito de embrague o de impacto para un desajuste real de velocidad, en lugar de descartar su energía. La fase absoluta del engranaje no está especificada: solo se informan avances angulares.

Los parámetros de construcción inválidos lanzan excepciones de argumento accionables. Se rechazan las combinaciones de parámetros no finitas o mal condicionadas; el determinante planetario escalado debe superar `64 epsilon`. El rechazo del intervalo distingue duración inválida, estado inválido, estado incompatible, par inválido y fallo numérico. El desbordamiento aritmético devuelve `NumericalFailure`; las entradas finitas por sí solas no garantizan cantidades derivadas representables. Una comprobación de equilibrio de fuerzas en tiempo de ejecución rechaza también una cancelación que deje reacciones de los miembros finitas pero incoherentes: cada residuo de fuerza está acotado por `512 epsilon` veces la suma de las magnitudes de los pares inerciales, aplicados y de reacción. El extremo aceptado comprueba además el equilibrio de impulsos de cada miembro, usando `512 epsilon` veces las magnitudes del momento antiguo y nuevo y de los impulsos aplicado y de reacción. Esto último detecta una cancelación excesiva en la reconstrucción de la velocidad dependiente. Estas comprobaciones acotan residuos, no el error de la solución para parámetros arbitrariamente mal condicionados. Las pruebas incluyen un fallo finito de cancelación y un par de relación alta cuya reacción pequeña debe seguir siendo observable. El residuo es `external_work - kinetic_energy_change`; no se fabrica calor de fricción.

## Estados de transmisión y evidencia

Las pruebas aportan de forma explícita pares de retención o de bloqueo para establecer estos límites ideales:

| Condición impuesta | Relación de velocidades resultante |
|---|---|
| Corona retenida | `omega_C = omega_S / (1+k)` |
| Sol retenido | `omega_C = k omega_R / (1+k)` |
| Portasatélites retenido | `omega_S = -k omega_R` |
| Sol bloqueado con la corona | Las tres velocidades de los miembros son iguales |

El freno aportado realiza trabajo cero cuando su miembro está retenido; un bloqueo sol/corona recibe pares opuestos con trabajo combinado cero. Estas comprobaciones establecen estados estáticos de transmisión. Esta referencia no implementa ningún cambio, acoplamiento de embrague, circuito hidráulico ni TCU, y unos pares externos arbitrarios no retienen un miembro de forma automática.

Los mismos ocho grupos de pruebas se ejecutan contra `net10.0` y `netstandard2.1`:

- Relaciones con signo e inercia reflejada; potencia de reacción y equilibrio de impulsos por miembro.
- Movimiento planetario libre frente a una solución independiente de multiplicador de fuerzas.
- Tres casos de miembro retenido y directa, con cargas explícitas de retención y de bloqueo.
- Invariancia de partición bajo carga constante e inversión de velocidad a través de cero.
- Carga sinusoidal frente a integrales independientes para ambas referencias; reducir el intervalo a la mitad da una reducción aproximadamente cuádruple del error de velocidad y de ángulo.
- Valores inválidos de inercia, relación, estado y carga, velocidades incompatibles, mal condicionamiento y desbordamiento.
- 2,500 casos deterministas para cada referencia, que comprueban trabajo, momento y reproducibilidad.
- 10,000 evaluaciones repetidas de cada primitivo con asignación gestionada cero, más uso inmutable compartido por llamadores concurrentes independientes.

Consulta la [validación](VALIDATION.es.md) para el resultado completo de la verificación en serie. Las pruebas de ensamblados Standard se ejecutan en .NET 10 y no aportan evidencia de Unity Editor, Play Mode ni IL2CPP.

## Integración acoplada

Las restricciones permanentes participan ahora en la resolución electromecánica, de cilindro y de embrague, con espacios de trabajo de simulación independientes, reversión completa y canales estables de reacción y de error. JSON y el esquema, el asset v8 con lectores anteriores, el descubrimiento MCP y la repetición usan la misma topología. El experimento planetario encendido realiza subidas de reducción a directa y una bajada. Consulta [el contrato acoplado](GEAR_NETWORK.es.md) para las ecuaciones y la evidencia. La topología DCT/AT completa, el convertidor, la hidráulica, los controles, el comportamiento completo del motor y la calibración medida del vehículo siguen formando parte del objetivo completo de Power!.
