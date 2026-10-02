# Combustión premmezclada y contabilidad de la energía del combustible

[English](PREMIXED_COMBUSTION.md) · [简体中文](PREMIXED_COMBUSTION.zh-CN.md) · [Français](PREMIXED_COMBUSTION.fr.md) · [Русский](PREMIXED_COMBUSTION.ru.md) · [日本語](PREMIXED_COMBUSTION.ja.md) · [한국어](PREMIXED_COMBUSTION.ko.md) · [Deutsch](PREMIXED_COMBUSTION.de.md) · **Español** · [Italiano](PREMIXED_COMBUSTION.it.md) · [Português](PREMIXED_COMBUSTION.pt-BR.md)

`premixed_combustion` acopla un perfil de quemado de Wiebe prescrito a un cigüeñal y a una cámara
de gas finita. El combustible, el aire fresco y los productos inertes se transportan por la red de gas;
la reacción consume el reactivo limitante disponible y convierte la energía química almacenada
en energía térmica del gas. El trabajo de presión acciona el mismo solver de cigüeñal que usan los
cilindros móviles. Core, JSON, CLI, MCP y el asset v6 comparten estas definiciones.

Es un modelo premmezclado de propiedades constantes y de parámetros concentrados. Cada constituyente de una red
conectada comparte un solo R y gamma. Las tres clases de masa no representan especies detalladas,
calores específicos variables, cinética de reacción, propagación de llama, autoencendido, picado,
emisiones, evaporación de combustible ni inyección. El ejemplo encendido original usa una admisión gaseosa
ya mezclada. La [dosificación de combustible por ciclo](FUEL_METERING.es.md) admite un raíl gaseoso finito
aparte y la admisión de aire; la pulverización y la evaporación líquidas quedan fuera del modelo. Un quemado prescrito y unas pruebas de conservación que pasan no establecen un rendimiento
medido del motor ni completan el objetivo del grupo motopropulsor completo.

## Composición y puertos

Un nodo de gas añade de forma opcional `premixed` a su objeto `gas` existente:

```json
"gas": {
  "gas_constant": { "value": 287, "unit": "j_kg_k" },
  "gamma": 1.35,
  "premixed": {
    "lower_heating_value": { "value": 44000000, "unit": "j_kg" },
    "stoichiometric_air_fuel_ratio": 14.7,
    "initial_fractions": { "fuel": 0.04, "fresh_air": 0.96 }
  }
}
```

El poder calorífico y la relación másica estequiométrica aire/combustible deben ser positivos y finitos.
Las fracciones de combustible y de aire fresco deben ser no negativas, con suma como máximo uno. El resto
son productos inertes. El aire fresco representa el oxidante junto con su diluyente; consumir
`r` kg de aire fresco con 1 kg de combustible crea `1+r` kg de productos. El aire fresco o el combustible en exceso
siguen disponibles; los productos no pueden reaccionar de nuevo.

Cada restricción de depósito en un nodo premmezclado debe especificar
`reservoir_fractions` explícitas, con los mismos dos campos. Las fracciones están prohibidas en otros
componentes o en restricciones internas. En la entrada, la frontera suministra esa composición;
en la salida, retira la composición real del volumen finito. Los volúmenes de gas finitos conectados
deben compartir el seguimiento, R, gamma, el LHV y la relación estequiométrica. Las conexiones incompatibles o
sin seguimiento se rechazan; los inventarios químicos no pueden desaparecer en un puerto.

El solver de gas transfiere cada constituyente con el mismo flujo másico con signo y las mismas fracciones
aguas arriba que el gas total. Evoluciona masas de constituyente no negativas y reconstruye la masa total
a partir de su suma. Un paso premmezclado también está acotado por el flujo saliente total, incluso cuando
las tasas de masa total entrante y saliente casi se cancelan. La igualación de presión o el reflujo del depósito
no crean inventario químico.

Un gas premmezclado añade tres valores de constituyente almacenados al presupuesto de estado declarado. Un componente
de quemado añade una frontera de ángulo irreversible; todo permanece dentro del límite existente de 64 estados.
Los libros compensados de frontera y de reacción participan en la reversión, el hash y las bifurcaciones.

## Ley de quemado e historia del cigüeñal

El componente conecta `node_a` (cigüeñal) con `node_b` (gas premmezclado). Una cámara móvil debe
usar el cigüeñal de su propia geometría, y cada cámara admite como máximo un componente de quemado. Un recipiente
fijo puede usar un cigüeñal independiente para experimentos analíticos.

```json
{
  "id": 15,
  "kind": "premixed_combustion",
  "node_a": 1,
  "node_b": 2,
  "input_channel": 103,
  "initial_input": { "value": 1, "unit": "fraction" },
  "parameters": {
    "cycle_angle": { "value": 720, "unit": "deg" },
    "start_angle": { "value": 340, "unit": "deg" },
    "duration_angle": { "value": 80, "unit": "deg" },
    "shape_exponent": 3,
    "burn_coefficient": 6.9
  }
}
```

El ciclo es de forma explícita 360 o 720 grados. El ángulo de inicio es relativo al cigüeñal real,
no está desplazado de forma implícita por la fase geométrica del cilindro. La duración está en [1e-6 rad, ángulo de ciclo];
el exponente de forma `n` está en [1,16] y el coeficiente `a` en (0,50]. El inicio se normaliza módulo
el ciclo. Todos los ángulos exigen unidades. Para un avance hacia adelante `z` desde el inicio del quemado, recortado
a [0,1], la intensidad integrada es `H(z) = a z^n`. Cada ciclo completo aporta `a`.

Sobre ángulos hacia adelante recién recorridos:

```text
hazard = burn_multiplier * delta(H)
limiting_fuel = min(fuel_mass, fresh_air_mass / stoichiometric_air_fuel_ratio)
burned_fuel = limiting_fuel * (1 - exp(-hazard))
consumed_air = stoichiometric_air_fuel_ratio * burned_fuel
created_products = burned_fuel + consumed_air
released_heat = LHV * burned_fuel
```

Para una carga cerrada y un multiplicador 1, la fracción quemada es `1-exp(-a z^n)` de su
cantidad inicial de combustible limitante. **No** se fuerza a uno en el límite de la duración:
`exp(-a)` permanece sin quemar tras una ventana de quemado completa. Las exposiciones pequeñas usan `expm1` para
evitar la cancelación. La carga fresca que entra durante una ventana activa se une a los reactivos
bien mezclados; no hay una fuente de calor oculta e ilimitada por ciclo.

El canal de entrada opcional es `burn_multiplier`, una fracción en [0,1] que escala la intensidad.
Cero desactiva la reacción; no impide que el combustible entre por una admisión abierta. Esta entrada
no es un comando de inyector ni un controlador predictivo de encendido.

Cada componente guarda el mayor ángulo de cigüeñal alcanzado, inicializado al ángulo de
partida. La reacción solo ocurre más allá de esa frontera. Parar, girar hacia atrás o
recorrer de nuevo ángulos ya visitados no puede liberar calor otra vez. El avance hacia adelante desactivado
sigue moviendo la frontera, así que volver a activarlo no libera el calor perdido. Empezar dentro
de una ventana de quemado solo consume su exposición hacia adelante restante. Tras una inversión grande,
el quemado sigue suprimido hasta que el cigüeñal supera su máximo anterior; el encendido bidireccional
del motor y el rearme gobernado por el controlador siguen siendo trabajo de control futuro.

## Energía y acoplamiento numérico

La energía interna del gas sigue siendo térmica: `U = m cv T`. La energía química es por separado
`E_chemical = m_fuel LHV`. La entalpía total del depósito incluye tanto `mdot cp T` como la
energía química transportada. El cambio global de energía almacenada incluye el inventario químico,
así que la combustión es una conversión interna, no trabajo de fuente externo adicional:

```text
energy_residual = mechanical/electrical source work + reservoir total enthalpy
                  - rejected heat - change(total stored energy)
```

`net_fuel_energy_in` expone por separado la parte química del libro de frontera. Es
entrada neta, incluido el combustible no quemado que abandona el modelo; no es la entrega bruta de combustible ni
una métrica de consumo de combustible en régimen permanente. `fuel_residual` y `fresh_air_residual` comparan
el inventario inicial, la transferencia neta de frontera, el inventario actual y la reacción acumulada.
`mass_residual` sigue cubriendo la masa total de gas. La conversión de constituyentes conserva la masa.

Para una cámara móvil, el calor anticipado depende del ángulo de cigüeñal nuevo de prueba y participa
en la resolución no lineal del cigüeñal. Con calor total `Q` durante el tick y
`r = (V_old/V_new)^(gamma-1)`:

```text
U_after = (U_before + Q/2) r + Q/2
adiabatic_work = (U_before + Q/2) (1-r)
crank_work = adiabatic_work - back_pressure * (V_new - V_old)
```

El par de presión discreto usa ese mismo trabajo, de modo que la energía del gas, la energía química y
el trabajo del cigüeñal concuerdan. El combustible solo se consume después de que la resolución tenga éxito en el estado candidato.
El transporte de gas sigue usando medios pasos simétricos alrededor del trabajo del cigüeñal y de la reacción. La temperatura de pared
permanece fija a lo largo del tick exterior; el acoplamiento de pared es de primer orden.

Para un quemado activado, el recorrido angular y el recorrido por la velocidad en los extremos por tick deben mantenerse dentro de
`min(0.25 rad, duration_angle/32)`, con la protección correspondiente de resolución angular en binary64.
El calor liberado no debe superar el 25 % de la energía térmica previa al quemado en un tick. Son límites
del trabajo admitido y de la resolución, no garantías de precisión. Se aplican junto a los límites de
subpaso de gas y de iteración del cilindro. Reduce `step_ns` ante `numerical_failure`, alinea
los eventos programados con el tick nuevo y vuelve a crear el modelo o la sesión. Las llamadas fallidas o canceladas
no confirman estado, entrada, frontera, libro químico ni cursor de repetición.

## Salidas, compatibilidad y evidencia

Los nodos premmezclados añaden los campos KPI `fuel_mass`, `fresh_air_mass`, `product_mass` y `chemical_energy`.
Un componente de quemado añade `fuel_burned` (kg) y `heat_released` (J) acumulados.
El campo KPI `burn_frontier` expone su mayor ángulo de cigüeñal visitado (cantidad de canal
`burn_frontier_angle`, rad), de modo que se puede inspeccionar el quemado suprimido tras una inversión.
Los canales globales añaden la energía química, la entrada neta de energía de combustible, el residuo de combustible y el residuo
de aire fresco. Las cantidades de canal que devuelve el descubrimiento son las autoritativas; p. ej., la masa de combustible del nodo
se llama `unburned_fuel_mass`. Las salidas ordinarias de energía interna del gas y de flujo conservan
sus significados térmico y de flujo con signo.

Los modelos premmezclados añaden la etiqueta 7 de huella y los parámetros normalizados de reacción y de composición.
Las huellas y el avance anteriores, sin reacción, siguen sin cambios. El asset v6 añade registros de composición,
de fracciones de depósito y de quemado; los fixtures auténticos v1–v5 conservan la compatibilidad. Las
fidelidades nuevas son `premixed_gas_transport` y `premixed_wiebe_combustion`.

Las pruebas cubren el consumo analítico de combustible y aire y la temperatura en un recipiente cerrado, los reactivos
limitantes, la transferencia de depósito hacia adelante y hacia atrás, la conservación de constituyentes en una red cerrada,
la convergencia de una EDO independiente de cigüeñal y gas en reacción, el quemado parado, invertido o desactivado,
contratos mal formados, reversión de lote, cancelación, bifurcaciones y cero asignaciones en el avance y en la instantánea.
El [laboratorio de cilindro encendido](../assets/labs/fired-cylinder.power.json)
acciona una carga a través de fases repetidas de admisión, compresión, quemado, expansión y escape, y
se repite de forma idéntica en los 63 límites de informe de JSON, CLI, MCP y del asset. La evidencia numérica
y el alcance real de ejecución están registrados en [VALIDATION.es.md](VALIDATION.es.md).

[Las ecuaciones del reactor de gas ideal de Cantera](https://www.cantera.org/stable/reference/reactors/ideal-gas-reactor.html)
aportan el contexto de masa, especies y energía del volumen de control. El
[ejemplo de motor de encendido provocado de Ansys](https://chemkin.docs.pyansys.com/version/stable/examples/advanced/SI_engine_optimization.html)
usa temporización de quemado explícita y parámetros de Wiebe. Estas referencias motivan los contratos;
su química detallada, sus modelos de dos zonas y sus parámetros de ejemplo no se copian ni
se presentan como verificación de este solver de propiedades constantes. No hay dependencia de tiempo de ejecución
de ninguno de los dos paquetes. Todos los parámetros de muestra siguen siendo `unverified`.
