# Movimiento resuelto de los satélites Ravigneaux

[English](RESOLVED_PLANETS.md) · [简体中文](RESOLVED_PLANETS.zh-CN.md) · [Français](RESOLVED_PLANETS.fr.md) · [Русский](RESOLVED_PLANETS.ru.md) · [日本語](RESOLVED_PLANETS.ja.md) · [한국어](RESOLVED_PLANETS.ko.md) · [Deutsch](RESOLVED_PLANETS.de.md) · **Español** · [Italiano](RESOLVED_PLANETS.it.md) · [Português](RESOLVED_PLANETS.pt-BR.md)

El conjunto resuelto incluye el giro absoluto de ambos juegos internos de satélites y su inercia de masa orbital alrededor del portasatélites. Cuatro restricciones físicas de engrane conectan seis rotores. Los cinco embragues y frenos de rango, y el convertidor externo, siguen siendo componentes ordinarios. La reducción de cuatro miembros sigue disponible como simplificación declarada aparte; no aporta evidencia del giro de los satélites.

La conectividad de engrane y las relaciones de paso tienen una [referencia estructural](https://www.mathworks.com/help/sdl/ref/ravigneauxgear.html) aparte. Power! deriva e implementa su propio grafo de rotores conservativo y comprobaciones independientes de la matriz de masa. No se incluye ninguna implementación ni paquete de modelo de un proveedor.

## Geometría y energía

Para el radio de paso de la corona `R` y las relaciones de sol grande y pequeño `kL` y `kS`, la geometría rígida de paso es:

```text
large sun radius = R/kL
small sun radius = R/kS
outer planet radius = (R-large sun radius)/2
inner planet radius = (large sun radius-small sun radius)/2
outer orbit radius = (R+large sun radius)/2
inner orbit radius = (large sun radius+small sun radius)/2
```

`RavigneauxPlanetParameters` exige un radio de corona en el SI, masas e inercias de giro positivas por satélite, y de 1 a 32 pares de satélites iguales y síncronos. El espaciado uniforme debe caber en ambos juegos sin solapamiento de las circunferencias de paso. La geometría y la inercia agregada deben seguir siendo representables. Todas estas entradas son propiedades explícitas de investigación; el ayudante no aporta valores medidos.

Para `n` pares iguales, el portasatélites recibe la inercia orbital `n (mInner orbitInner^2 + mOuter orbitOuter^2)`. El parámetro existente `CarrierInertia` es la inercia de la estructura del portasatélites en este camino resuelto. Cada rotor nuevo tiene `n` veces su inercia de giro por satélite. Sus velocidades son velocidades angulares absolutas, así que la energía cinética es el `J omega^2/2` ordinario; la corrotación conserva la energía de giro de los satélites. Usar el giro relativo con este almacenamiento diagonal omitiría el acoplamiento del portasatélites.

## Contrato de engrane

`carrier_gear` impone `A - ratio B + (ratio-1) C = 0`, donde C es el portasatélites que realmente se mueve. Los engranes externos usan una relación negativa de radios de paso; el engrane interno corona/satélite exterior usa una relación positiva. Se admiten relaciones con signo finitas y distintas de cero, incluida la unidad. Se exigen tres puertos rotacionales distintos y velocidades iniciales compatibles.

Los cuatro engranes son sol grande/satélite exterior, sol pequeño/satélite interior, corona/satélite exterior y satélite interior/satélite exterior. Los tres pares de reacción entran en la misma proyección de punto medio y tienen potencia de puerto sumada nula y suma de pares nula. La reacción del portasatélites no se envía en silencio a la masa estacionaria. Un refinamiento acotado del residuo relativo mejora las respuestas de fuerza pequeñas. La resolución de punto medio impone residuo de velocidad nulo en el extremo siguiente, y evita reflejar de forma repetida el redondeo precedente. Ambas operaciones usan las respuestas reales de fuerza de la restricción y conservan sus multiplicadores de corrección en los historiales reales de reacción. El espacio de trabajo pertenece a cada simulación; los factores compilados permanecen inmutables. Las filas normalizadas, las coordenadas compensadas y los historiales completos de reacción preservan la fase, las bifurcaciones, la cancelación y la reversión del lote.

La referencia libre independiente usa coordenadas de corona y de portasatélites. Con `aOuter = R/outerRadius` y `aInner = R/innerRadius`:

```text
outer planet speed = aOuter ring + (1-aOuter) carrier
inner planet speed = -aInner ring + (1+aInner) carrier
M = sum over rotors of J [ring coefficient, carrier coefficient]^T
                         [ring coefficient, carrier coefficient]
```

Esto incluye ambas energías de giro y la inercia orbital añadida por separado. Cargas generalizadas independientes, inercias reflejadas de todos los caminos adelante y de marcha atrás, el momento angular y el impulso y el calor de captura del portasatélites comprueban la resolución ensamblada.

## Grafo compartido y evidencia

`CreateResolvedGraph` toma los puertos originales, cuatro ID distintos de nodo y de engrane de satélite, y las propiedades de satélite declaradas. Devuelve definiciones ordinarias inmutables: seis rotores internos, cuatro engranes de portasatélites, una reducción final y cinco elementos de fricción. El JSON plano conserva las inercias totales de los rotores y las relaciones de engrane con signo; la descripción del ejemplo registra la geometría generadora y las propiedades por satélite. Los resúmenes de fuente preservan esa evidencia declarada de autoría.

`resolved-ravigneaux-transmission` ejercita todas las entregas adelante de subida y de bajada. `fired-resolved-ravigneaux-converter` añade el motor, los mapas de convertidor con signo y el bloqueo. Ambos declaran tres pares, R=0.1 m, masas interior/exterior de 0.3/1 kg e inercias de giro por satélite de 0.000015/0.0005 kg m2. La estructura del portasatélites es 0.03 kg m2; la adición explícita de órbita es 0.0184375 kg m2. Son entradas de investigación.

El asset portátil v24 conserva el engrane de portasatélites con signo y lee las versiones anteriores. El primitivo añade la etiqueta de huella 28; los grafos anteriores conservan sus huellas y su repetición. Los marcadores de Studio preparados identifican los tres puertos de engrane. La verificación real de Unity Editor, Play Mode, Player e IL2CPP sigue aparte. Ejecuta la verificación en serie exigida `dotnet run --file tools/Build.cs -- verify`; los resultados numéricos y el alcance viven en [VALIDATION.es.md](VALIDATION.es.md).

## Comportamiento restante

Los juegos rígidos de satélites iguales y síncronos no modelan la flexibilidad del diente, el reparto de carga de fabricación, el juego, las pérdidas de engrane, la lubricación ni las propiedades dependientes de la temperatura. Está disponible la [actuación por pistón hidráulico alimentada por bomba](AT_HYDRAULIC_ACTUATION.es.md). El control completo del cambio, la coordinación de la ECU y la geometría y los mapas OEM medidos siguen sin terminar. El conjunto genérico no demuestra la identidad PSA AT8/AL4. Los límites de las muestras y las mediciones que faltan permanecen intactos.
