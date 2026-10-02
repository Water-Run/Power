# Accionamiento electromagnético de aguja y realimentación de dosis muestreada

[English](NEEDLE_ACTUATION.md) · [简体中文](NEEDLE_ACTUATION.zh-CN.md) · [Français](NEEDLE_ACTUATION.fr.md) · [Русский](NEEDLE_ACTUATION.ru.md) · [日本語](NEEDLE_ACTUATION.ja.md) · [한국어](NEEDLE_ACTUATION.ko.md) · [Deutsch](NEEDLE_ACTUATION.de.md) · **Español** · [Italiano](NEEDLE_ACTUATION.it.md) · [Português](NEEDLE_ACTUATION.pt-BR.md)

Un inyector líquido accionado lee la elevación de una aguja de traslación, en lugar de
cerrar una compuerta de masa ideal en la dosis solicitada. Un solenoide dependiente de la posición,
una masa de aguja explícita, un resorte de retorno con amortiguamiento y topes elásticos de carrera aportan el
movimiento. Un controlador muestreado posee el voltaje de la bobina y detiene su comando cuando se cierra la ventana
del ciclo o la entrega medida alcanza la solicitud enclavada.

El decaimiento de la corriente, el retardo mecánico de cierre y el rebote en el asiento pueden seguir entregando
después de ese comando. El combustible real permanece en el libro de fuente, película y gas; la dosis
solicitada es un objetivo de control, no un corte físico impuesto. Es un actuador de investigación
y una realimentación todo o nada sencilla. Los mapas magnéticos no lineales, la saturación,
las pérdidas por histéresis y corrientes de Foucault, la resistencia dependiente de la temperatura, la conmutación y el flyback,
la alimentación por batería, la fuerza axial del fluido y la inyección calibrada siguen abiertos.

## Energía magnética y mecánica recíprocas

`solenoid` usa una resistencia de devanado constante suministrada y una inductancia lineal:

```text
L(x) = L_reference + gradient (x - x_reference) > 0
lambda = L(x) i
W_magnetic = lambda^2 / (2 L(x))
d(lambda)/dt = V - R i
F_magnetic = gradient i^2 / 2
```

Una elevación positiva aumenta la inductancia y la fuerza magnética actúa en ese sentido.
Ambas polaridades de corriente atraen la armadura. La fuerza sigue la coenergía
magnética, como describen [la guía de fuerza de reluctancia de Modelica](https://doc.modelica.org/Modelica%204.0.0/Resources/helpWSM/Modelica/Modelica.Magnetic.FluxTubes.UsersGuide.ReluctanceForceCalculation.html)
y las [ecuaciones del solenoide de Simscape](https://www.mathworks.com/help/simscape-electrical/ref/solenoid.html).
Power! usa su propia ley constitutiva reducida y su propia integración; no se introduce ninguna dependencia
de Modelica ni de Simscape. La inductancia debe permanecer positiva en todas las
posiciones aceptadas y especulativas. El modelo no recorta la inductancia negativa
ni sustituye las mediciones magnéticas que faltan por un mapa calibrado.

El estado magnético es el enlace de flujo. Para un intervalo `h`, inductancias en los extremos
`L0,L1` y voltaje retenido, un gradiente discreto simétrico da:

```text
A = (1/L0 + 1/L1) / 4
lambda1 = ((1 - h R A) lambda0 + h V) / (1 + h R A)
i_bar = A (lambda0 + lambda1)
F_bar = gradient (lambda0^2 + lambda1^2) / (4 L0 L1)
W_supply = h V i_bar
Q_copper = h R i_bar^2
delta W_magnetic = W_supply - Q_copper - F_bar (x1 - x0)
```

El flujo se elimina de forma analítica para una posición de extremo propuesta. La fuerza restante
y su derivada analítica respecto de la posición se unen a la misma resolución mecánica no lineal
que los cilindros, los convertidores, los pistones hidráulicos y los embragues especulativos.
El movimiento aceptado confirma el flujo, el trabajo eléctrico, el calor de cobre y la fuerza media una sola vez.
El calor de cobre entra en el nodo térmico declarado o en el calor rechazado externo; la energía almacenada
magnética y la mecánica permanecen aparte.

Una integración simultánea e independiente de EDO verifica el refinamiento suave de segundo orden.
El límite RL estacionario también tiene una referencia analítica de corriente. El avance
conjugado en energía no establece por sí solo un movimiento preciso con un paso de tiempo grueso;
las constantes de tiempo eléctricas, el recorrido de la carrera y los eventos de contacto siguen necesitando resolución.

## Masa de la aguja, resorte y topes elásticos

La aguja es un nodo de traslación ordinario, con kg, m y m/s. Un `linear_spring` ordinario
aporta precarga y amortiguamiento, con un encaminamiento explícito del calor.
`travel_stop` añade energía unilateral reversible en los límites nominales de carrera:

```text
E_stop = K/2 [max(0,x_min-x)^2 + max(0,x-x_max)^2]
```

Su reacción discreta es el gradiente de energía negativo entre los extremos aceptados.
La penetración almacena energía, en lugar de fijar la posición. La derivada analítica
comparte la resolución mecánica; el recorrido de la carrera nominal por intervalo se limita a
un cuarto de la amplitud. Un deslizador tiene un solo propietario de tope, incluidos los topes que ya
posee un pistón hidráulico. Las coordenadas hidráulicas y de solenoide compartidas siguen
siendo posibles, y cada fuerza contribuye a la misma coordenada.

El rebote en el asiento es físico dentro de esta reducción elástica. Una apertura nula en una
instantánea no demuestra flujo nulo durante un intervalo posterior. El amortiguamiento de contacto,
la fricción de la junta, la restitución del impacto y el comportamiento medido de asiento y aguja siguen abiertos.

## Apertura física y entrega

El `parameters.needle` opcional de `liquid_fuel_injector` contiene un `needle_node`
de traslación más `closed_position` y `full_open_position` en m/mm. La apertura real
es la relación lineal acotada de la elevación:

```text
opening = clamp((x - x_closed)/(x_full_open - x_closed), 0, 1)
```

La [ley pasiva de raíl y tobera líquidos](LIQUID_FUEL_INJECTION.es.md) integra la carga de
presión con esa apertura efectiva. Conserva el inventario finito y los límites de energía
de presión, pero no limita la entrega física a la dosis solicitada ni borra el flujo
cuando la ventana del cigüeñal se cierra o se invierte. Una aguja abierta puede admitir combustible incluso con un
cigüeñal parado o una dosis solicitada nula. La ventana del cigüeñal sigue enclavando el historial del objetivo
para la realimentación; la entrega fuera de una ventana observada nueva sigue formando parte
de los historiales del último ciclo observado y de los totales.

Sin `needle`, se conserva el camino anterior del inyector ideal limitado por cuota, con
huellas de modelo y repetición sin cambios. Los modelos equipados con aguja declaran su
fidelidad distinta. En este incremento la aguja está equilibrada en presión; no se infiere ninguna fuerza axial
de presión o de chorro. El receptor existente, de volumen líquido despreciable,
exporta de forma explícita su trabajo de presión de desplazamiento.

## Controlador de reloj entero y propiedad de las entradas

`needle_driver` nombra un inyector accionado, su solenoide y el mismo cigüeñal de
temporización. Exige un `sample_period_ns` positivo explícito, alineado con los ticks
físicos y no mayor que un segundo, y un `drive_voltage` positivo en V. La
bobina que controla empieza a voltaje cero. En cada muestra que vence, el controlador registra la
dosis enclavada y la masa entregada real, y después mantiene el voltaje de accionamiento mientras la ventana
hacia adelante tenga entrega de objetivo restante; si no, mantiene voltaje cero.

El controlador posee el canal de voltaje del solenoide. Los agentes escriben la solicitud en `kg`
del inyector; las escrituras directas de voltaje devuelven `controlled_input`, identifican el canal de
comando escribible y conservan el estado y la revisión. Las escrituras iniciales y de evento no
avanzan el historial de control. La fase de muestreo sigue el tiempo entero de simulación. Este
controlador no implementa regulación de corriente pico/retención, compensación predictiva del cierre,
PWM o flyback, ni un comportamiento completo de ECU o TCU.

## Definiciones, canales y transacciones

| Componente | Parámetros y puertos |
|---|---|
| `solenoid` | Nodo de traslación A; entrada en V; resistencia en ohmios no negativa, inductancia de referencia positiva en H y gradiente en H/m (`h_m`), posición de referencia en m/mm, corriente inicial en A; sumidero térmico opcional |
| `travel_stop` | Nodo de traslación A; límites crecientes en m/mm y rigidez positiva en N/m |
| `needle_driver` | Nodo rotacional de temporización A; identificadores estables de inyector y solenoide, periodo de muestreo entero y nivel de accionamiento en V |

Las definiciones rechazan cantidades no relacionadas, unidades o dominios incorrectos, una inductancia
inicial no válida, una propiedad duplicada del tope o del voltaje, una aguja, bobina o cigüeñal que no coinciden y
periodos de muestreo no alineados. Los clientes de Core usan `SolenoidCoil`, `StrokeStop`,
`NeedleDrive`, `InjectorNeedleDefinition` y las leyes magnéticas y de contacto independientes.

Las salidas del solenoide exponen la `current` instantánea, la `force` media discreta del último tick,
la `internal_energy` magnética, el `copper_heat` acumulado y el `source_work` eléctrico.
Las salidas del tope exponen la energía elástica y la reacción instantánea. Las salidas del controlador
exponen el `command_voltage` retenido y la dosis solicitada y entregada de la última muestra. La `opening`
del inyector es la apertura real de posición, con la entrega media real del último tick.
Todos los identificadores y las unidades se descubren mediante la validación y la creación de la sesión.

Cuatro entradas de estado informadas por solenoide y tres por controlador se unen al presupuesto de
estado acotado. El flujo, la fuerza media, el calor y el trabajo compensados, el estado de control muestreado,
las entradas retenidas, la aguja, el tope y todos los historiales de fuente y de fase se copian, se hashean y se revierten con
la simulación completa. El avance activo con éxito y las instantáneas no asignan
memoria administrada. La cancelación, los lotes fallidos y las bifurcaciones independientes conservan juntos
los historiales eléctrico, mecánico, térmico y de controlador.

El asset v20 añade tablas tipadas de magnético, tope, aguja y controlador, y conserva
los lectores v1-v19. Se comprueban longitudes y cuentas acotadas, el resumen, las unidades, la propiedad distinta y
la protección contra degradaciones falsificadas. Un fixture auténtico de inyección líquida v19
conserva su huella y la repetición actualizada en el mismo runtime. Consulta
[ASSET_FORMAT.es.md](ASSET_FORMAT.es.md).

## Experimentos y aceptación

`needle-actuated-cylinder` conecta el actuador y la realimentación muestreada al
cilindro encendido de raíl finito y película. Su límite de 0.6 s puede conservar película líquida durante
el último transitorio de cierre y evaporación. Se verifica el inventario completo de fuente, película, gas y reacción,
en lugar de suponer una película seca o una entrega exacta del objetivo. JSON,
los assets portátiles y un servidor MCP hijo real comparten las mismas definiciones y la misma repetición.

El actuador aislado solicita 8 mg y observa un exceso de entrega a través del decaimiento de la corriente,
el movimiento de cierre y pequeños rebotes posteriores en el asiento. Esas cantidades son resultados de
investigación, no una temporización de inyector calibrada ni un controlador de seguimiento de dosis aceptado.
[VALIDATION.es.md](VALIDATION.es.md) registra la evidencia numérica y los límites.
Las vistas preparadas en Unity de la bobina, el tope, el controlador y la aguja a escala siguen exigiendo
una verificación real de Editor y Play. El grupo motopropulsor completo, el accionamiento medido, las fuerzas
magnéticas, electrónicas y de fluido refinadas, el repostaje del raíl y la ECU/TCU siguen sin terminar.
