# Alimentación hidráulica accionada por eje

[English](HYDRAULIC_PUMP.md) · [简体中文](HYDRAULIC_PUMP.zh-CN.md) · [Français](HYDRAULIC_PUMP.fr.md) · [Русский](HYDRAULIC_PUMP.ru.md) · [日本語](HYDRAULIC_PUMP.ja.md) · [한국어](HYDRAULIC_PUMP.ko.md) · [Deutsch](HYDRAULIC_PUMP.de.md) · **Español** · [Italiano](HYDRAULIC_PUMP.it.md) · [Português](HYDRAULIC_PUMP.pt-BR.md)

El grafo gestionado admite una bomba ideal reversible de desplazamiento y un alivio de presión unidireccional cuasiestacionario. El [laboratorio de bomba encendida](../assets/labs/fired-pump.power.json) conecta el cigüeñal a una línea de alimentación flexible, válvulas de cambio y embragues accionados por presión. Sus parámetros son sintéticos y `unverified`.

## Ecuaciones y potencia

El desplazamiento `D > 0` está en m³/rad. Una velocidad positiva del eje entrega volumen de referencia de la admisión a la salida:

```
Q = D * omega
shaft reaction = -D * (p_out - p_in)
shaft-to-fluid power = Q * (p_out - p_in) = -shaft reaction * omega
```

Se permiten el caudal inverso y el funcionamiento como motor hidráulico. No se infieren válvula de retención, fugas, fricción ni mapa de rendimiento. La inercia pertenece al nodo de eje explícito. Una admisión finita pierde exactamente el volumen entregado a la salida. Una admisión de depósito aporta `p_in * Q` al trabajo hidráulico externo; el trabajo de eje a fluido es una transferencia interna y no se añade al trabajo global de fuentes.

El alivio usa una característica explícita lineal de exceso de presión:

```
Q_A_to_B = G * max(p_A - p_B - p_crack, 0)
heat = Q_A_to_B * (p_A - p_B)
```

`G >= 0` tiene unidades m³/(s·Pa), y `p_crack >= 0` es una presión diferencial. Por debajo del umbral sella exactamente. Un caudal finito exige sobrepresión; la presión no se fija nunca al tarado. Es una aproximación constitutiva, no la mecánica de una corredera ni una curva ajustada de área de apertura de válvula. Toda su caída de presión genera calor, incluida la parte de la presión de apertura.

Las ecuaciones de la bomba ideal siguen el límite de pérdidas nulas de la [descripción de bomba de desplazamiento fijo de MathWorks](https://www.mathworks.com/help/hydro/ref/fixeddisplacementpumpil.html). El comportamiento de umbral es coherente con la [descripción de la válvula de alivio de presión](https://www.mathworks.com/help/hydro/ref/pressurereliefvalveil.html); la ley lineal de exceso de presión de Power! es una elección de modelado explícita y más simple. Esas referencias aportan ecuaciones y alcance, no mediciones de parámetros OEM ni código fuente.

## Contratos compartidos

`hydraulic_pump` exige un `node_a` rotacional, una salida hidráulica `node_b` y `parameters.inlet_node` (cero selecciona el depósito). La admisión debe ser distinta de la salida. Los parámetros incluyen un `displacement` positivo en `m3_rad`, más una `reservoir_pressure` explícita en `pa` o `bar` solo cuando la admisión es cero. No tiene entrada, sumidero de calor ni `node_c` planetario.

`hydraulic_relief` usa un `node_a` hidráulico, un `node_b` hidráulico opcional (cero u omitido selecciona un depósito), un `heat_node` térmico opcional, y los parámetros `coefficient`, `cracking_pressure` y `reservoir_pressure` solo del depósito. No tiene entrada de apertura. Los programas de válvula siguen usando restricciones controladas aparte.

Las salidas de la bomba son el `volume_flow` medio del último tick, la reacción de eje `torque`, la `hydraulic_power` con signo y el `hydraulic_work` acumulado con signo. Los historiales iniciales son cero; los cambios de entrada dejan sin cambios las medias aceptadas. El `hydraulic_work` global sigue siendo el trabajo externo del depósito. Las salidas del alivio reutilizan el caudal de la restricción, la potencia térmica media y el calor de fluido acumulado. Todas las entradas, los historiales y los términos de compensación participan en las bifurcaciones, los hashes, la cancelación y la reversión del lote completo. El asset v11 conserva las definiciones nuevas y todos los lectores v1–v10. El agente 0.13.0 anuncia `shaft_driven_hydraulics` y `fired-pump`.

## Evidencia numérica y límites

La velocidad de la bomba, las presiones de cámara, las velocidades de puerto del convertidor y el trabajo del cilindro comparten un sistema de Newton, usando respuestas mecánicas proyectadas por los engranajes. Las capacidades del embrague de presión se actualizan dentro de la iteración acotada de restricciones. Las transferencias de fluido aceptadas actualizan ambos puertos y el libro de volumen de referencia. Se conserva el camino hidráulico independiente existente para los modelos sin bombas, y así se preservan los hashes de repetición anteriores.

La resolución de Newton conjunta permite 24 iteraciones y 12 bisecciones de búsqueda lineal. La tolerancia del residuo de presión hidráulica es `2e-7 Pa + 64*epsilon*(|p_mid|+|p_old|)`; las tolerancias mecánicas y del embrague conservan sus contratos existentes. Los estados no finitos, las presiones manométricas aceptadas negativas o los presupuestos del solver agotados rechazan la llamada completa. Reduce `step_ns` e inspecciona las escalas de presión, flexibilidad, desplazamiento, inercia y embrague antes de reintentar. No hay fijación de cavitación.

Las pruebas cubren la oscilación analítica de eje y flexibilidad y el refinamiento de segundo orden, la conservación con admisión cerrada, el funcionamiento inverso como motor, las reacciones de una bomba engranada, el decaimiento analítico del alivio, una carga de eje estacionaria regulada y una solución analítica independiente de embrague deslizante dependiente de la presión. Se comprueban la captura, las ramas, la cancelación, el fallo tardío, el reintento y el avance sin asignaciones. La repetición portátil y del MCP compara los 89 límites de la bomba encendida; se rechazan los contratos mal formados y las degradaciones de versión.

En el experimento encendido de 0.8 s, el eje entrega 53.94250162 J al fluido, el trabajo hidráulico externo es cero y el alivio disipa 45.02640514 J. La energía hidráulica inicial es explícitamente 3 J. La presión final de línea es 1.06972624 MPa, la velocidad de cigüeñal/turbina 69.75553569 rad/s y la velocidad de carga 6.64338435 rad/s. El residuo de energía total es de unos `1.07e-9 J`; el residuo de volumen de referencia es `3.05e-20 m³`. La huella es `d0bd8f29a706fd89` y el hash final `572150ab5d66a2f6`.

Siguen abiertos los mapas de pérdidas medidos, el control del desplazamiento, la dinámica de batería y de control de voltaje, el recorrido y la inercia de corredera y de pistón, los acumuladores de gas, la cavitación, las propiedades dependientes de la temperatura y la coordinación ECU/TCU. Este punto no establece una DCT/AT completa, un rendimiento de vehículo calibrado ni la aceptación de Unity Editor o del Player.

## Fugas explícitas, fricción de eje y alimentación eléctrica

`HydraulicPumpAssembly` aporta una reducción reutilizable de bomba de coeficiente constante. Acepta el desplazamiento D en m³/rad, la conductancia de fugas G en m³/(s·Pa) y la fricción viscosa de eje B en N·m·s/rad. D debe ser positivo; G y B deben ser no negativos y finitos. No se infieren un rendimiento nominal ni una propiedad del aceite.

Para la presión diferencial `dp = p_out - p_in`:

```text
net inlet-to-outlet flow = D*omega - G*dp
shaft reaction          = -D*dp - B*omega
leakage heat power      = G*dp^2
shaft friction heat     = B*omega^2
absorbed shaft power    = net fluid power + leakage heat + shaft friction heat
```

Los signos admiten el bombeo y el funcionamiento como motor hidráulico en cualquiera de los dos sentidos, así como las fugas a través de una bomba parada. Las fugas siguen siendo un camino pasivo de salida a admisión incluso cuando superan el caudal de desplazamiento. La reducción de fugas de conductancia constante sigue la descripción analítica de pérdidas de la [referencia de bomba de MathWorks](https://www.mathworks.com/help/hydro/ref/fixeddisplacementpumpil.html). El arrastre viscoso lineal es una elección constitutiva explícita de Power!; no es el modelo de fricción dependiente de la presión de esa referencia ni un mapa de rendimiento OEM.

`TryEvaluate` devuelve el caudal neto instantáneo, la reacción total de eje, la potencia con signo de eje y de fluido, y las dos potencias de pérdida no negativas. Rechaza presiones manométricas negativas, entradas no finitas y el desbordamiento, sin devolver una reacción parcial.

`CreateComponents` devuelve una lista inmutable con ID explícitos y distintos para una `hydraulic_pump` ideal, una `hydraulic_resistance` de apertura fija de la salida a la admisión, y un `shaft` de rigidez nula desde el eje de la bomba hasta masa. Indica un sumidero térmico o deja que las pérdidas entren en el rechazo externo de calor. El compilador del modelo comprueba puertos, dominios, unidades e ID globales. Los componentes ordinarios conservan la resolución acoplada de punto medio, las transacciones, los canales, el esquema JSON y el asset v11; no hay estado oculto del ensamblado ni un formato nuevo. Los canales de la bomba describen la rama ideal. Resta el caudal de fugas para obtener la entrega del ensamblado; incluye el arrastre del eje al interpretar la carga total del eje. No cuentes el trabajo de la bomba ideal a la vez como trabajo externo de fuentes y como transferencia interna.

`fired-pump-losses` conecta las fugas y el arrastre a la transmisión encendida existente. Un nodo térmico aparte de la bomba recibe ambas pérdidas. El límite de pérdidas nulas reproduce todos los observables compartidos de `fired-pump` dentro de la tolerancia física. G y B constantes son entradas de investigación y siguen siendo `unverified`.

`electric-pump` conecta un motor CC RL de 12 V a un eje de bomba aparte, con fuerza contraelectromotriz, inductancia, par y calor del cobre explícitos. Una línea de alimentación flexible, un alivio y válvulas programadas de llenado y de drenaje accionan un embrague entre un eje accionado y una carga. Los cambios de voltaje y los eventos de válvula usan ticks exactos. La bomba no tiene conexión al cigüeñal y el trabajo hidráulico externo es cero. El trabajo eléctrico se incluye en el trabajo global de fuentes; el eje accionado y el par de carga son fronteras de potencia externas aparte. El voltaje prescrito y las consignas de válvula no implementan una batería, una ECU/TCU ni un regulador en lazo cerrado.

El movimiento analítico amortiguado de eje y de presión, y una ODE de tres estados de motor RL, eje y presión integrada de forma independiente, comprueban el refinamiento suave de segundo orden. Se comprueban las admisiones cerradas, el trabajo del depósito, el funcionamiento con signo, las pérdidas pasivas, el encaminamiento del calor, las asignaciones cero, las ramas, la cancelación y la reversión por fallo tardío contra ambos ensamblados de Core. JSON, los assets portátiles y el MCP comparan los 89 límites del informe de pérdidas encendidas y los 106 del eléctrico. Las pruebas preparadas de importación y de Play de Unity exigen una ejecución aparte del Editor.

<a id="sampled-pressure-regulation"></a>
## Regulación de presión muestreada

`pressure_controller` lee un nodo de presión manométrica hidráulica y posee un canal existente de voltaje de motor CC. Es un controlador PI discreto con ganancia proporcional explícita en `v_pa`, ganancia integral en `v_pa_s`, límites de voltaje y voltaje integral inicial. La entrada de consigna tiene unidades de presión. Su `sample_period_ns` entero va de 1 ns a 1 s y debe ser un múltiplo exacto del tick del modelo. Las ganancias y las consignas de presión son no negativas; los límites de voltaje son finitos y estrictamente crecientes. No se infiere ningún ajuste.

```text
error = setpoint - sampled_pressure
I_candidate = I + Ki * sample_period_s * error
raw_voltage = Kp * error + I_candidate
if raw_voltage exceeds a limit and the integral increment pushes farther outside:
    retain the preceding integral state
command = clamp(Kp * error + accepted_integral, minimum_voltage, maximum_voltage)
```

La integración condicional es la estrategia de anti-windup por fijación descrita por la [referencia de control de MathWorks](https://www.mathworks.com/help/simulink/slref/anti-windup-control-using-a-pid-controller.html). La transición discreta precisa de Power! de arriba es su modelo declarado, no código de implementación copiado ni evidencia de un ajuste OEM. La saturación por sí sola no establece el seguimiento: un objetivo inalcanzable puede ejecutarse y repetirse con éxito mientras falla los KPIs.

Las muestras ocurren en el instante cero y en los múltiplos absolutos del periodo configurado. La primera muestra preserva la integral inicial aportada de forma explícita; las muestras posteriores usan el periodo. La consigna se mantiene entre muestras. Los eventos de tick exacto se aplican antes de una muestra en el mismo tick. Un evento en el extremo de una llamada actualiza la consigna antes de la instantánea; el muestreo en ese extremo ocurre solo cuando empieza el tick físico siguiente. Los intervalos internos de captura o de inversión del embrague no disparan actualizaciones extra del controlador.

El compilador comprueba que el objetivo es una entrada de voltaje de motor CC y tiene exactamente un propietario, que el sensor es hidráulico y que el voltaje inicial del motor está dentro de los límites. El canal de voltaje poseído permanece en la definición del componente, pero está ausente de la lista de entradas externas. Se rechazan las escrituras directas y las sustituciones programadas de voltaje; cambia en su lugar la entrada `pressure_setpoint` del controlador. Los demás canales de motor, de bomba y de válvula conservan su semántica existente. Varios lazos independientes pueden compartir un sensor de presión.

Los canales observables son `sampled_pressure`, `pressure_error`, `integral_voltage` y `command_voltage`. Los historiales de presión y de error empiezan en cero; la consigna inicial es el voltaje configurado del motor, y la integral inicial es explícita. Los historiales describen la última muestra, no un error de presión recalculado de forma continua. Los cuatro estados del controlador y la entrada mantenida del motor participan en los hashes, las bifurcaciones y la reversión completa del lote. El muestreo y el avance correcto no asignan memoria gestionada tras el calentamiento. Una aritmética PI no finita rechaza la llamada completa; inspecciona las escalas de ganancia, de consigna y de integral.

El controlador no añade energía almacenada física ni una frontera de potencia. Su consigna cambia la frontera de voltaje del motor existente, cuya corriente, trabajo y calor del cobre permanecen en la resolución acoplada y en el libro de conservación. Los modelos sin controladores conservan sus huellas y su avance. Los modelos controlados añaden la etiqueta de huella 13. El asset v12 conserva la definición completa del controlador; un fixture auténtico de bomba v11 conserva su resumen original, su huella y la repetición en el mismo runtime tras la actualización.

`pressure-regulated-pump` usa un controlador de 5 ms y ticks físicos de 100 µs, con perturbaciones programadas de llenado y de drenaje del embrague y objetivos de 300/350/200 kPa. Las ganancias, los límites del actuador y todos los demás parámetros siguen siendo `unverified`. Tiene 757 límites coincidentes de informe JSON, de asset y de MCP. Las pruebas comparan un controlador muestreado y una planta RK4 aparte, comprueban el refinamiento del tick físico a periodo de controlador fijo, las reglas exactas de reloj y de extremo, la recuperación de la saturación, los diagnósticos de unidades y de propiedad, la reversión del estado del controlador, las ramas, las asignaciones cero y el libro completo de trabajo eléctrico e hidráulico.

Esto aporta un lazo de realimentación de presión. La dinámica de batería y de PWM o del lazo de corriente, el filtrado, el retardo y la cuantización del sensor, la dinámica de válvula, corredera y pistón, la coordinación ECU/TCU, la DCT/AT completa, los fallos y la calibración medida siguen siendo trabajo aparte sin terminar.

<a id="finite-battery-supply-and-duty-regulation"></a>
## Alimentación finita de batería y regulación por ciclo de trabajo

Un nodo `battery` posee dos estados: la fracción de carga z y el voltaje de polarización v_p. Su almacenamiento es una capacidad de carga explícita Q en C o Ah (1 Ah = 3600 C), el estado inicial es el SOC en `fraction` y la posición es el voltaje inicial de polarización en V. Su registro de batería aporta la OCV en vacío y a plena carga, la resistencia serie R0, la resistencia de polarización Rp, la capacidad Cp y un sumidero térmico o el rechazo externo de calor. La OCV es afín en el SOC:

```text
E(z)       = V_empty + (V_full - V_empty)*z
z'         = -I_battery / Q
v_p'       = I_battery / Cp - v_p/(Rp*Cp)
V_bus      = E(z) - v_p - R0*I_battery
U_chemical = Q*(V_empty*z + (V_full - V_empty)*z^2/2)
U_polar    = Cp*v_p^2/2
heat power = R0*I_battery^2 + v_p^2/Rp
```

La corriente positiva descarga; la corriente negativa carga. La topología sigue la [descripción del circuito equivalente de batería](https://www.mathworks.com/help/simscape-battery/ref/batteryequivalentcircuit.html). La OCV afín y los parámetros constantes son reducciones explícitas de Power!, no tablas de temperatura o de envejecimiento, química medida, pérdida de capacidad ni un BMS. El SOC permanece en [0,1]. Superar el inventario de carga u obtener un voltaje de bus negativo rechaza el lote completo; no hay fijación silenciosa ni reserva inventada. Acorta el lote, detén la descarga o la carga, o aporta otras condiciones iniciales declaradas.

`battery_motor` conecta un eje rotacional a un bus de batería y conserva la resistencia del motor, la inductancia, el coeficiente de par y de fuerza contraelectromotriz, y la corriente inicial explícitos. Su entrada de ciclo de trabajo bidireccional promediado está en [-1,1]: el voltaje del motor es el ciclo de trabajo por el voltaje del bus, y la corriente del lado de la batería es el ciclo de trabajo por la corriente del motor. Esa transferencia de potencia es interna y no se añade a `source_work`. Tanto la energía inductiva del motor como la energía de polarización y química de la batería participan en la energía almacenada total. El calor del cobre, de la serie y de la polarización se encamina a sus sumideros explícitos. Es un convertidor promediado ideal, no conmutación PWM, pérdidas del convertidor, contactores ni un lazo de control de corriente.

`resistive_load` aporta una resistencia positiva explícita, una entrada de apertura opcional en [0,1] y un sumidero de calor. La apertura escala la conductancia; una apertura nula desconecta exactamente. Para la conductancia total de carga G y la corriente de bus del lado del motor I_m:

```text
V_bus     = (E(z) - v_p - R0*I_m)/(1 + R0*G)
I_battery = I_m + G*V_bus
load heat = opening*V_bus^2/R_load
```

La resistencia compartida de la batería acopla a todos los consumidores. La matriz de punto medio acoplada incluye la carga, la polarización, la corriente del motor y las respuestas mecánicas. Los factores que posee la simulación se actualizan cuando cambian los ciclos de trabajo, las aperturas de accesorios o las duraciones de los intervalos internos. Las respuestas de engranaje, de cilindro, de convertidor y de embrague usan estos mismos factores. Los modelos anteriores sin alimentación conservan su camino de solver y sus huellas precedentes. La energía química afín y la del RC son cuadráticas, así que las transferencias eléctricas de punto medio tienen comprobaciones de conservación independientes. La misma física admite la regeneración del motor.

`pressure_duty_controller` usa la transición existente de PI y de fijación sobre el reloj entero, con ganancias en `fraction_pa` y `fraction_pa_s`, límites explícitos de ciclo de trabajo dentro de [-1,1] y un ciclo de trabajo integral inicial. Posee un canal de ciclo de trabajo de `battery_motor`. Los agentes cambian `pressure_setpoint`; las sustituciones directas de ciclo de trabajo devuelven `controlled_input`. Lee `sampled_pressure`, `pressure_error`, `integral_duty` y `command_duty`. El ciclo de trabajo mantenido, la carga, la polarización y la memoria de control comparten instantáneas, bifurcaciones, cancelación y reversión completa. El muestreo y el avance correcto siguen sin asignaciones.

`battery-regulated-pump` combina la alimentación finita de batería, pulsos de carga de accesorios y un regulador de ciclo de trabajo de 5 ms con el laboratorio del embrague de presión. Su capacidad de 50 C es un inventario de prueba sintético pequeño, no una medición de batería de vehículo. A los 15 s el SOC baja de 0.8 a unos 0.627, mientras la presión termina en unos 200.828 kPa para un objetivo de 200 kPa. Los 761 límites de JSON, de asset y de MCP coinciden. Las pruebas comprueban por separado la relajación RC analítica, el inventario de la carga resistiva, la integración RK4 independiente del motor y del circuito, el refinamiento del tick físico, el ciclo de trabajo con signo y la regeneración, la equivalencia de devanados en paralelo, el acoplamiento de engranaje, embrague y bomba, la reversión por agotamiento tardío, las ramas, la cancelación y las asignaciones cero.

Siguen abiertos el BMS, la química y el envejecimiento de la batería, y la realimentación de temperatura, los fallos y los contactores, el control PWM y de corriente, la dinámica de sensores, la mecánica de los actuadores, la ECU/TCU completa y la calibración. Los parámetros de la batería y todas las entradas de laboratorio siguen siendo `unverified`.
