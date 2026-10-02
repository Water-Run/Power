# Red de convertidor de par cuasiestacionario

[English](CONVERTER_NETWORK.md) · [简体中文](CONVERTER_NETWORK.zh-CN.md) · [Français](CONVERTER_NETWORK.fr.md) · [Русский](CONVERTER_NETWORK.ru.md) · [日本語](CONVERTER_NETWORK.ja.md) · [한국어](CONVERTER_NETWORK.ko.md) · [Deutsch](CONVERTER_NETWORK.de.md) · **Español** · [Italiano](CONVERTER_NETWORK.it.md) · [Português](CONVERTER_NETWORK.pt-BR.md)

`torque_converter` participa en la misma resolución de ejes, motor, cilindro, embrague y engranaje ideal. La bomba y la turbina son nodos rotacionales distintos con inercia explícita. El estator es masa estacionaria; su reacción es observable, pero no realiza trabajo. La pérdida de fluido alimenta un nodo térmico opcional o el libro externo de rechazo de calor. Un `clutch` paralelo aparte aporta el bloqueo. Todos los parámetros del laboratorio son sintéticos y `unverified`.

## Mapas y ecuaciones explícitos

Se exigen cuatro mapas: `pump_positive`, `pump_negative`, `turbine_positive` y `turbine_negative`. El miembro de mayor velocidad absoluta es el conductor de referencia; la bomba gana un empate exacto. El signo de ese miembro selecciona su mapa positivo o negativo. Es una convención matemática de miembro de referencia, también durante la contrarrotación. No infiere una característica de marcha atrás o de marcha de inercia que no esté disponible.

Para la velocidad del conductor `wD` y la del conducido `wF`, `s = wF/wD` está en `[-1,1]`. Cada mapa contiene de 2 a 32 puntos explícitos con `speed_ratio` adimensional, `torque_ratio` R y `capacity_coefficient` C en `nm_s2_rad2`. C multiplica la velocidad al cuadrado; no es un factor K inverso. R y C se interpolan de forma lineal y no se extrapolan nunca. Ambas velocidades nulas producen reacciones nulas y modo cero.

```text
Tdriver   = -C(s) * wD * abs(wD)
Tfollower =  R(s) * C(s) * wD * abs(wD)
Tstator   = -(Tpump + Tturbine)
Qdot      = C(s) * abs(wD)^3 * (1 - s*R(s))
```

La compilación exige nudos estrictamente crecientes que abarquen `[-1,1]`, C y R no negativos, y `s*R(s) <= 1` a lo largo de cada segmento. Comprobar solo los nudos no basta: R lineal en s hace cuadrática la eficiencia; también se comprueba cualquier máximo interior. Las pendientes deben ser finitas. En `s=1`, C debe ser cero y R uno, lo que da par de fluido nulo a igual velocidad y en el mismo sentido. En `s=-1`, los mapas bomba-positiva/turbina-negativa y bomba-negativa/turbina-positiva deben dar reacciones físicas coincidentes. Esto evita un salto cuando cambia el miembro de referencia durante la contrarrotación. La comparación de extremos admite solo una tolerancia de redondeo de `64*epsilon*(abs(a)+abs(b))`. Los arrays de mapas son propios e inmutables; los coeficientes normalizados entran en la huella del modelo.

Estas restricciones definen el modelo pasivo actual de mapas con signo de Power!. No pretenden cubrir curvas arbitrarias de convertidor medidas. Las convenciones generales de tracción y de marcha de inercia basadas en mapas están documentadas por [MathWorks Torque Converter](https://www.mathworks.com/help/sdl/ref/torqueconverter.html) y su [ejemplo de dos modos](https://de.mathworks.com/help/sdl/ug/torque-converter-two-mode.html). Los cuatro mapas con signo, la validación de la interpolación y el solver de abajo son diseño de Power!; no se copió código fuente ni un conjunto de parámetros medidos de esas referencias.

## Integración acoplada y límites

Las reacciones del convertidor usan las velocidades del punto medio del intervalo. Cuando hay cilindros, un sistema de Newton conjunto resuelve su trabajo discreto por ángulo de cigüeñal y las dos velocidades de puerto del convertidor. Cada respuesta a par unitario incluye el sistema electromecánico y la proyección de engranajes permanentes. Las iteraciones de restricción del embrague llaman a esta misma resolución no lineal; la subdivisión por captura e inversión regenera las respuestas del intervalo. No hay un par de convertidor retardado aplicado después de la integración del cilindro o del embrague.

La resolución no lineal tiene 24 iteraciones y 12 intentos de búsqueda lineal por bisección en cada iteración. La tolerancia del residuo de ángulo del cilindro es `2e-14 rad`. Un residuo de velocidad del convertidor usa `2e-12 + 64*epsilon*(abs(value)+abs(free_prediction)) rad/s`. Los jacobianos analíticos del mapa a trozos y las derivadas por diferencias finitas del trabajo del cilindro construyen el sistema conjunto. Siguen aplicándose los límites existentes de recorrido del cilindro de 0.25 rad, de resolución de válvula y de quemado, de iteración y evento del embrague, y de restricción de engranaje. Se admiten como máximo ocho convertidores dentro de los presupuestos existentes de 32 nodos, 64 componentes y 64 estados. Cada convertidor añade cuatro estados lógicos de historial observable: dos pares medios, la potencia térmica media y el calor acumulado. La suma de calor compensada también participa en la copia, el hash y la reversión.

El calor aceptado del intervalo es `-h*(Tp*wp_mid + Tt*wt_mid)`, de modo que el trabajo mecánico retirado es el calor registrado. Un calor negativo más allá de la tolerancia de redondeo de la velocidad resuelta rechaza el intervalo; solo se fija en cero un residuo negativo del tamaño del redondeo. Los pares y la potencia térmica se ponderan por duración a lo largo de los intervalos internos aceptados y después se dividen por el tick completo. Los ensayos especulativos de eventos no confirman nunca su calor ni sus reacciones. Todo el estado del convertidor, del embrague, del engranaje, del gas, del historial de quemado, de las entradas y del libro global se revierte en un lote fallido o cancelado. Las bifurcaciones poseen sus espacios de trabajo. Las pruebas ejercitan la captura sin asignaciones.

La integración de punto medio es de segundo orden para el movimiento suave de un convertidor aislado. El modelo encendido acoplado conserva el acoplamiento explícito de temperatura de pared y el despegue del embrague por media del intervalo, así que no afirma un segundo orden uniforme a través de todas las transiciones. Refina los ticks ante un fallo numérico o un estudio de precisión; inspecciona las pendientes de los mapas, las escalas de inercia y de velocidad y las restricciones del embrague antes de recrear una sesión. Un mapa válido no garantiza que cada paso de tiempo elegido sea resoluble.

## Contrato de modelo compartido y observable

JSON usa `node_a` obligatorio (bomba), `node_b` (turbina), cuatro arrays bajo `parameters` y un `heat_node` opcional. No acepta canal de entrada del convertidor, puerto de portasatélites ni valores por defecto implícitos de mapa. `ComponentDefinition.TorqueConverter` expone el mismo modelo de Core. Los errores de mapa llevan el ID del componente y un campo accionable como `converter.pump_positive` o `converter.counter_rotation`.

| Campo | Significado | Unidad |
|---|---|---|
| `torque` | Par medio de bomba del último tick completo | Nm |
| `torque_at_b` | Par medio de turbina del último tick completo | Nm |
| `torque_at_c` | Reacción media del estator estacionario del último tick completo | Nm |
| `heat_flow` | Potencia térmica media del fluido del último tick completo | W |
| `fluid_heat` | Calor de fluido aceptado acumulado | J |
| `speed_ratio` | Relación de velocidades con signo conducido/conductor actual; cero en parado | fraction |
| `converter_drive` | 0 parado, 1 bomba positiva, 2 bomba negativa, 3 turbina positiva, 4 turbina negativa | código de estado |

Los pares y la potencia medios empiezan en cero. Las actualizaciones de entrada de frontera no reescriben las salidas medias del tick anterior. `torque_at_c` nombra aquí la reacción del estator; este componente no tiene un tercer rotor. Los modelos que contienen convertidor anuncian `quasisteady_converter_powertrain` y añaden la etiqueta de huella 10. Las huellas y las trayectorias de los modelos sin convertidor no cambian. El asset portátil v10 conserva los cuatro mapas; los lectores v1–v9 y los fixtures auténticos permanecen.

## Laboratorio y evidencia

Pide el ejemplo MCP `fired-converter`, o ejecuta:

```sh
dotnet src/Power.Cli/bin/Release/net10.0/Power.Cli.dll assets/labs/fired-converter.power.json --output artifacts/reports/fired-converter.json
```

El laboratorio de 0.8 s empieza con una bomba a 600 rpm y una turbina a 300 rpm, que alimentan el sol de un planetario y una reducción final 3:1. El frenado de la corona selecciona la reducción; un embrague sol/corona selecciona la directa. Un bloqueo, una liberación y una recaptura programados de forma independiente ejercitan los caminos de fluido y de fricción. Son eventos prescritos, no un controlador de transmisión automática.

Con ticks de 50,000 ns, los 87 límites del informe se repiten exactamente a través de la CLI, los assets portátiles y el MCP. La huella del modelo es `839d03901973668d` y el hash de estado final es `834a679376b7a6fd`. La velocidad final de bomba/turbina es aproximadamente 73.37748 rad/s, la velocidad de carga 6.988331 rad/s, el calor de fluido 24.27663 J y el calor de bloqueo 22.84709 J. El embrague de cambio y el freno añaden 157.18199 J y 83.42289 J. El nodo térmico compartido llega a 301.438643 K; el residuo total de energía es de unos `3.30e-11 J`. Estos números describen un transitorio sintético.

Las pruebas independientes cubren la solución analítica de dos inercias `C(s)=k(1-s), R=1`, el decaimiento analítico en calado, el refinamiento de segundo orden, el encaminamiento térmico y al calor externo, el equilibrio del estator, los estados de marcha atrás, de marcha de inercia y de contrarrotación, puertos de convertidor compartidos, la reflexión de engranajes, el bloqueo, la reversión del lote completo, la cancelación, las bifurcaciones y la asignación cero. El refinamiento del modelo encendido comprueba una distancia combinada adimensional en velocidad final, calor de fluido y calor de bloqueo frente a una ejecución de 3,125 ns, usando cinco tamaños de tick. También acota las diferencias absolutas por debajo de 0.0002 rad/s o J, respectivamente. Los errores individuales de calor no tienen que disminuir en cada reducción a la mitad cerca de los eventos. Esta comprobación es aparte del orden analítico aislado. La propiedad y la validación de los mapas con signo, y los registros portátiles mal formados con el signo rehecho, tienen comprobaciones dedicadas.

La [red hidráulica](HYDRAULIC_NETWORK.es.md) aporta ahora capacidad de bloqueo y de cambio derivada de la presión. Siguen abiertos el momento angular del fluido, la dinámica de llenado y de presión del convertidor, la mecánica del estator giratorio o de rueda libre, las propiedades dependientes de la temperatura, la dinámica de bomba, regulador y pistón, la topología DCT/AT completa y la coordinación ECU/TCU. También siguen abiertos el comportamiento completo del motor, las mediciones OEM y la calibración del vehículo. Studio tiene puertos de fluido esquemáticos y pruebas preparadas; la evidencia real de Editor, Play Mode e IL2CPP sigue aparte y no está disponible en este entorno.
