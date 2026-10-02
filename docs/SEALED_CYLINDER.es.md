# Fundamento del cilindro cerrado

[English](SEALED_CYLINDER.md) · [简体中文](SEALED_CYLINDER.zh-CN.md) · [Français](SEALED_CYLINDER.fr.md) · [Русский](SEALED_CYLINDER.ru.md) · [日本語](SEALED_CYLINDER.ja.md) · [한국어](SEALED_CYLINDER.ko.md) · [Deutsch](SEALED_CYLINDER.de.md) · **Español** · [Italiano](SEALED_CYLINDER.it.md) · [Português](SEALED_CYLINDER.pt-BR.md)

`sealed_cylinder` acopla una biela-manivela rígida a un nodo rotacional. El cilindro contiene una masa fija de gas ideal con relación de calores específicos constante y sin transferencia de calor con la pared. Es una referencia de compresión y expansión, no un motor encendido completo. La admisión, el escape, el combustible, la combustión, las fugas, la transferencia de calor con la pared, la inercia alternativa y los eventos de control siguen siendo trabajo de implementación aparte. Todos los parámetros actuales son sintéticos y `unverified`.

La presión y la temperatura iniciales se aplican en el ángulo inicial del rotor conectado más la fase del cilindro. Cambiar ese ángulo inicial cambia la masa atrapada, salvo que la presión y la temperatura se ajusten de forma coherente. El estado del gas se deriva de la posición del cigüeñal y de la entropía inicial inmutable; añade canales observables, pero ninguna variable de estado independiente. Esta reducción solo es válida para el componente adiabático cerrado.

## Geometría y estado del gas

Las longitudes se compilan a metros, las presiones a pascales y la fase a radianes. La entrada acepta `m`/`mm`, `pa`/`bar` y `rad`/`deg`. La temperatura está en kelvin; la constante específica del gas usa `j_kg_k`. La relación de compresión y gamma son adimensionales. El diámetro y la carrera deben ser positivos, la longitud de biela debe superar la mitad de la carrera, la relación de compresión y gamma deben ser mayores que uno, y la presión, la temperatura y la constante del gas iniciales deben ser positivas. La contrapresión puede ser cero.

Con radio de manivela `r = stroke/2`, longitud de biela `l`, área del pistón `A = π bore²/4` y ángulo `θ` medido desde el punto muerto superior:

```text
x(θ) = r (1 - cos θ) + l - sqrt(l² - r² sin² θ)
Vc   = A stroke / (compression_ratio - 1)
V(θ) = Vc + A x(θ)
```

La implementación usa una forma algebraicamente equivalente para evitar la cancelación cerca del punto muerto superior. Esta geometría sigue la [relación de volumen de la biela-manivela centrada de la Colorado State University](https://www.engr.colostate.edu/~allan/thermo/page2/page2.html).

Sean `V0`, `P0` y `T0` el estado inicial. Las relaciones reversibles del gas ideal son:

```text
m = P0 V0 / (R T0)
P = P0 (V0/V)^gamma
T = T0 (V0/V)^(gamma-1)
U = P V / (gamma-1)
τ = (P - Pback) dV/dθ
```

Las relaciones de presión/volumen y de temperatura siguen la [derivación de la compresión isentrópica de la NASA](https://www1.grc.nasa.gov/beginners-guide-to-aeronautics/isentrophic-compression/). Con una relación de compresión de 10 y gamma de 1.4, la compresión del punto muerto inferior al superior multiplica la presión por unos 25.119 y la temperatura por unos 2.512. Son relaciones idealizadas, no un rendimiento medido del motor.

## Integración y energía

La resolución electromecánica de punto medio existente aporta una solución base y una respuesta al par precalculada en cada cigüeñal de cilindro distinto. Una resolución no lineal reducida determina los incrementos angulares de esos cigüeñales. Los cilindros del mismo cigüeñal contribuyen a una sola suma de par; los cigüeñales acoplados se resuelven juntos. Ninguna llamada a un proveedor de modelos, objeto de Unity ni dependencia de terceros participa en un tick físico.

Cada cilindro usa un par discreto coherente con el trabajo:

```text
τ_discrete Δθ = -(U_next - U_old) - Pback (V_next - V_old)
```

Las diferencias divididas analíticas estables del volumen y las evaluaciones de logaritmo y exponencial de argumento pequeño tratan los incrementos pequeños y los pasos por el punto muerto. La salida instantánea `Torque` sigue siendo `(P-Pback) dV/dθ`; es distinta del par medio usado para integrar un tick finito.

El cambio global de energía almacenada incluye el cambio de energía interna del gas. El trabajo de contrapresión es trabajo de fuente externo, `-Pback ΔV`, así que el libro sigue siendo `source_work - heat_rejected - stored_energy_change`. La disipación del eje y del motor sigue entrando en la red térmica o en el calor rechazado. Los cambios de energía del gas se evalúan de forma directa para no restar energías absolutas grandes cuando gamma se acerca a uno.

La iteración de Newton se limita a 16 iteraciones, con como máximo 10 ensayos de búsqueda lineal por iteración. El predictor lineal y el recorrido de cigüeñal aceptado deben mantenerse dentro de 0.25 radianes por tick. Los valores no finitos, un recorrido excesivo o la falta de convergencia devuelven `NumericalFailure`; toda la llamada, incluidas las entradas programadas y las actualizaciones del libro, se revierte. Reduce `step_ns` y vuelve a crear el modelo o la sesión para reintentar con un tick fijo más pequeño. Aceptar el paso no garantiza la precisión del paso de tiempo. Los ángulos acumulados muy grandes también pierden resolución angular en binary64; la precisión de larga duración necesita su propia evidencia.

## Experimento observable y portátil

Cada cilindro expone la presión (Pa), la temperatura del gas (K), el volumen (m³), la masa fija (kg), la energía interna absoluta (J), el desplazamiento del pistón desde el punto muerto superior (m) y el par en el cigüeñal (N·m). Los identificadores de canal conservan la codificación de objeto y campo existente. El modelo informa la fidelidad `sealed_adiabatic_gas` y la calibración `unverified`.

Ejecuta `assets/labs/sealed-cylinder.power.json` con la CLI, o solicita `get_example_model({"name":"sealed-cylinder"})` por MCP. La muestra usa un tick de 100 µs, una duración de 0.2 s, dos cambios de par y 21 límites de informe. Los KPI declarados se aplican a la muestra final, como en los experimentos existentes; las pruebas de conservación del núcleo inspeccionan límites repetidos a lo largo de sus ejecuciones.

El mismo documento se exporta a `SealedCylinder.powerasset`. Unity tiene una vista esquemática del pistón gobernada por el canal de desplazamiento; una unidad de escena representa una carrera completa. Las dimensiones físicas y las salidas siguen en el SI. La evidencia real de Editor, Play Mode e IL2CPP sigue pendiente.

`EngineChecks` ejecuta comprobaciones analíticas de geometría y de gas ideal, pasadas de conservación de dos segundos, refinamiento de paso de segundo orden, rotación inversa, casos de paso pequeño y de punto muerto, varios cilindros en cigüeñales compartidos y acoplados, acoplamiento eléctrico y térmico, fallo y recuperación atómicos, cancelación, ramas independientes, compatibilidad de assets y avance sin asignaciones. Las mismas comprobaciones se ejecutan contra ambos ensamblados de destino en el host de .NET.
