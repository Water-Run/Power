# Película de combustible líquido finita y evaporación

[English](FUEL_FILM.md) · [简体中文](FUEL_FILM.zh-CN.md) · [Français](FUEL_FILM.fr.md) · [Русский](FUEL_FILM.ru.md) · [日本語](FUEL_FILM.ja.md) · [한국어](FUEL_FILM.ko.md) · [Deutsch](FUEL_FILM.de.md) · **Español** · [Italiano](FUEL_FILM.it.md) · [Português](FUEL_FILM.pt-BR.md)

`fuel_film` almacena un inventario líquido inicial explícito junto a un receptor de gas
rastreado. Un nodo térmico finito aporta el calor sensible y el del cambio de fase. El combustible
evaporado se une a la masa, la energía interna y el constituyente de combustible del receptor; la reacción
premmezclada existente solo consume vapor. El inventario líquido es un mojado inicial,
no combustible inyectado, y sigue formando parte del libro de masa total y de energía química.

Es un modelo de investigación de propiedades constantes, con un volumen de desplazamiento líquido
despreciable y una temperatura de saturación prescrita. No implementa dinámica de raíl
líquido, de aguja ni de pulverización, equilibrio de fases dependiente de la presión, condensación,
propiedades de combustible multicomponente ni un comportamiento de gasolina calibrado.

## Energía de fase y fuente de calor finita

Sean `c_l` el calor específico del líquido, `c_v` la capacidad calorífica isocora del gas receptor,
`T_s` la temperatura de saturación declarada y `L_u > 0` la diferencia de energía interna
específica vapor menos líquido en `T_s`. La referencia térmica compartida es:

```text
e_offset = (c_v - c_l) T_s - L_u
U_liquid = m_liquid (c_l T_liquid + e_offset)
U_vapor_added = delta_m c_v T_s
```

`L_u` es una diferencia de energía interna en J/kg, no una entalpía de
vaporización. Una entalpía suministrada necesita una conversión explícita y justificada antes de
poder usarse aquí. La energía térmica del líquido puede ser negativa bajo esta referencia;
la temperatura y la masa deben seguir siendo físicamente admisibles. La energía química
`m_liquid * LHV` es aparte y se transfiere con el vapor sin crear calor de reacción
ni trabajo de fuente externo.

Por debajo de la saturación, la conductancia `K` acopla la capacidad líquida `m_liquid c_l` a la capacidad
finita de pared `C_w`. La diferencia de temperatura decae de forma analítica a la tasa
`K (1 / (m_liquid c_l) + 1 / C_w)`. La temperatura media ponderada por capacidad permanece
constante. Si el líquido alcanza `T_s`, la ley resuelve ese instante y usa el
intervalo restante para la evaporación.

En saturación con `T_wall > T_s`, el sobrecalentamiento de la pared decae a la tasa `K / C_w`.
El calor de fase disponible en un intervalo `h` es
`C_w (T_wall - T_s) (1 - exp(-K h / C_w))`, limitado por `m_liquid L_u`.
La película permanece en `T_s` hasta secarse; la masa evaporada es el calor de fase dividido por
`L_u`. El secado deja exactamente masa y energía líquidas nulas y detiene la extracción de calor.
Una pared fría puede enfriar el líquido existente; no condensa el vapor del receptor.

Toda transferencia cumple `delta U_liquid + U_vapor_added = Q_from_wall`.
La pared pierde ese mismo calor, así que el cambio de fase no introduce una frontera
de energía externa. Los inventarios de combustible no negativos y el libro completo de constituyentes
se comprueban con independencia del libro de energía total.

## Contrato de grafo y de documento

| Dato | Requisito |
|---|---|
| `node_a` | Receptor de gas finito con seguimiento explícito de combustible premmezclado y LHV |
| `node_b` | Pared térmica finita, con capacidad y temperatura positivas |
| `initial_mass` | kg no negativos; el inventario líquido inicial completo |
| `initial_temperature` | K positivos, no mayores que la saturación |
| `liquid_specific_heat` | J/(kg K) positivos, unidad JSON `j_kg_k` |
| `saturation_temperature` | K positivos, unidad JSON `k` |
| `latent_internal_energy` | J/kg positivos, unidad JSON `j_kg` |
| `conductance` | W/K no negativos, unidad JSON `w_k` |

JSON exige los seis parámetros. La película no tiene canal de entrada, temporización de cigüeñal ni
sumidero de calor aparte. Se rechazan los parámetros no relacionados, los dominios de puerto incorrectos, las unidades, los valores
no finitos y una capacidad de estado no admitida. Quien llama desde Core usa
`ComponentDefinition.LiquidFilm` y `FuelFilmDefinition`; la ley independiente
`EquilibriumFuelFilm` expone la creación de un estado admisible y el avance de un baño finito.

Cada película aporta cinco entradas de estado informadas al presupuesto de estado acotado del compilador.
La masa, la energía térmica, el historial de evaporación, el flujo medio, el calor de pared y
sus historiales compensados pertenecen a la simulación. La cancelación, las entradas rechazadas,
los fallos tardíos del solver, las bifurcaciones independientes y los intervalos especulativos de embrague conservan
la transacción completa. El avance con éxito y las lecturas de instantánea no asignan
memoria administrada tras el calentamiento.

## Precisión de la integración

Un intervalo aceptado usa medios pasos de película / gas / mecánica y reacción / gas / película.
Las películas que comparten una pared se ejecutan en un orden de componentes estable antes del avance
de gas y en orden inverso después. Su temperatura de pared finita se transporta
entre subpasos de película, y el calor de pared entra en la misma resolución térmica.

La ley aislada del baño finito es analítica a través del calentamiento sensible, la saturación y
el secado. Una integración simultánea e independiente de EDO verifica el refinamiento suave de segundo orden
para dos películas que comparten una pared y para el vapor transportado por una salida de gas
bloqueada, sin otras fuentes de calor de pared. Los enlaces de calor de gas
y otras fuentes térmicas siguen leyendo la temperatura de pared explícita del intervalo exterior,
así que ese acoplamiento conserva la precisión de primer orden. Las ventanas de reacción,
los eventos de válvula y el secado necesitan sus propias comprobaciones de refinamiento; la repetición exacta del lote por sí sola
no demuestra la precisión del paso de tiempo ni un segundo orden uniforme para un grupo motopropulsor encendido.

## Semántica observable y portátil

| Campo de la película | Significado |
|---|---|
| `mass` | Combustible líquido restante, kg |
| `temperature` | Temperatura del líquido; temperatura de saturación declarada cuando está seco |
| `internal_energy` | Energía térmica líquida con signo bajo la referencia de fase declarada, J |
| `chemical_energy` | Energía química del combustible líquido restante, J |
| `evaporated_fuel_mass` | Vapor entregado acumulado, kg |
| `mass_flow` | Entrega media de vapor durante el último tick físico completo, kg/s |
| `film_wall_heat` | Calor acumulado extraído de la pared, J; el enfriamiento puede hacerlo negativo |
| `heat_flow` | `K (T_wall - T_liquid)` instantáneo, W; cero cuando está seco |

Descubre los identificadores y las unidades de salida mediante la validación o la creación de la sesión. Los observables globales
de masa, de combustible y de energía química incluyen el inventario de la película. La entrega interna
de vapor no incrementa la energía de combustible del depósito ni la entalpía externa.

El asset v19 almacena todas las propiedades de fase y conserva los lectores v1-v18. Cada película necesita
un registro de fase tipado de 64 bytes. Se comprueban longitudes y cuentas acotadas, el resumen, la cobertura completa,
los registros duplicados o ausentes, las unidades, la compilación física y la protección contra degradación.
El fixture auténtico de dosificación de combustible v17 conserva su huella y
la repetición actualizada en el mismo runtime. Consulta [ASSET_FORMAT.es.md](ASSET_FORMAT.es.md).

## Laboratorio y trabajo pendiente

`film-fired-cylinder` calienta una película inicialmente mojada, admite aire por separado y después
consume el vapor disponible mediante el quemado de Wiebe prescrito. La pared caliente finita
paga el calor de fase; el líquido no se quema de forma directa. JSON, la CLI, la repetición portátil y
el servidor MCP real coinciden en cada límite de informe. Los contratos de fuente, de esquema y de sesión
siguen compartidos; los parámetros son `unverified`.

Consulta [VALIDATION.es.md](VALIDATION.es.md) para la evidencia numérica medida. Los marcadores de película
y las comprobaciones de ciclo de vida preparados en Unity siguen exigiendo una verificación real de Editor y Play.
El [inyector líquido](LIQUID_FUEL_INJECTION.es.md) aparte repone ahora las películas desde
una fuente flexible finita. La bomba y el repostaje, la aguja y la pulverización, las propiedades de combustible medidas,
el encendido y la ECU, el comportamiento completo de admisión y escape, los controles de transmisión y los grupos
motopropulsores calibrados siguen siendo requisitos aparte.
