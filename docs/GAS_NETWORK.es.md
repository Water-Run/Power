# Red de gas compilada

[English](GAS_NETWORK.md) · [简体中文](GAS_NETWORK.zh-CN.md) · [Français](GAS_NETWORK.fr.md) · [Русский](GAS_NETWORK.ru.md) · [日本語](GAS_NETWORK.ja.md) · [한국어](GAS_NETWORK.ko.md) · [Deutsch](GAS_NETWORK.de.md) · **Español** · [Italiano](GAS_NETWORK.it.md) · [Português](GAS_NETWORK.pt-BR.md)

Las redes de gas finitas se ejecutan ahora a través de `CompiledModel` y `Simulation`. El punto de control
cubre cámaras de volumen fijo, depósitos de presión y temperatura fijos, orificios
controlados y enlaces térmicos de pared. La [extensión de cilindro móvil](MOVING_CYLINDER.es.md) conecta ahora el intercambio de gas con el volumen dependiente del cigüeñal y el trabajo de presión; el solver de volumen fijo descrito abajo conserva su comportamiento original.

## API de C# y unidades

```csharp
var model = CompiledModel.Compile(new ModelDefinition
{
    StepNanoseconds = 100_000,
    Nodes = [NodeDefinition.GasVolume(1, 0.002, 2e6, 900)],
    Components = [ComponentDefinition.GasReservoir(10, 1, 2e-5, 1e5, 300,
        dischargeCoefficient: 0.9, channel: 100, opening: 0)]
});
var simulation = model.CreateSimulation();
simulation.SubmitInputs([new Scalar(100, 0.5)]);
var status = simulation.Step(100_000_000);
var values = new Scalar[model.OutputCount];
var snapshot = simulation.ReadSnapshot(values);
```

`GasVolume` toma el volumen en m³, la presión en Pa, la temperatura en K y, de forma opcional,
R en J/(kg K) y gamma. Un nodo de gas guarda el volumen en `Storage`, la temperatura en `Initial`,
la presión en `Position` y la composición en `Gas`. Litros, bar y milímetros cuadrados los
aceptan cantidades explícitas y se normalizan antes de la huella.

`GasOrifice` une dos identificadores de nodo de gas. `GasReservoir` une un nodo a una frontera fija;
`NodeB == 0` identifica ese depósito. `GasHeatLink` une un nodo de gas y un nodo térmico
con conductancia en W/K. Los nodos de gas conectados deben compartir exactamente el mismo R y gamma.
La apertura es una fracción adimensional en [0,1], validada para entradas iniciales, directas y programadas.
Un identificador de canal de entrada cero deja fija la apertura inicial. Las redes solo de gas no necesitan
un rotor ficticio. Los límites siguen siendo 32 nodos, 64 componentes y 64 estados escalares; cada volumen
de gas consume dos estados.

Cada nodo de gas expone presión, temperatura, masa y energía interna. Las restricciones
exponen el flujo másico con signo de A hacia B; los enlaces de calor exponen el flujo de calor con signo del gas hacia la pared.
La entalpía del depósito es positiva hacia dentro. El residuo de energía es
`source_work + reservoir_enthalpy - heat_rejected - stored_energy_change`.
El residuo de masa es `sum(mass - initial_mass) - cumulative_reservoir_mass`.
Los residuos de coma flotante se valoran frente a escalas físicas, no frente al cero exacto.

## Método numérico y frontera

El solver de gas usa subpasos explícitos con un predictor/corrector de Heun. La tasa relativa máxima
inicial de masa y energía del tick elige un número uniforme de subpasos, con un objetivo del 2 % de cambio
por subpaso. Más de 4096 subpasos, estados no físicos, valores no finitos o un cambio corregido
de masa o energía superior al 25 % rechazan el lote entero. Reduce `StepNanoseconds` y
vuelve a compilar, o inspecciona el área de flujo, el volumen, la conductancia y las condiciones iniciales.

La ley de tobera tiene una derivada singular a presiones iguales. Cada evaluación limita la
energía transferida a la cantidad de presión igualada del par conectado, y escala juntas la masa y
la entalpía aguas arriba. Para volúmenes finitos esa energía es
`abs(pA-pB) / ((gammaA-1)/VA + (gammaB-1)/VB)`; un depósito fijo omite el término B.
Esto evita oscilaciones de cruce de presión en un par aislado y conserva los
libros emparejados. El limitador cambia la integración cerca del equilibrio; la precisión de segundo orden se
afirma solo para el caso de refinamiento de flujo bloqueado, suave y no acotado, de las pruebas.

La temperatura de pared se mantiene en su valor inicial durante los subpasos de gas. El calor de pared acumulado
entra después en la resolución térmica existente. Este acoplamiento es de primer orden en el tick exterior;
la estabilidad o la conservación con un paso grande no establecen por sí solas la precisión. La prueba de pared
compara temperaturas en tiempo finito con la solución analítica de dos capacidades. Este método
no es el solver implícito por pares propuesto antes y no valida esa propuesta.

La masa, la energía, las sumas de depósito y las correcciones del libro compensado pertenecen al estado
de la simulación y entran en la copia, la reversión, las bifurcaciones y los hashes. El avance con éxito y
las instantáneas del búfer del llamador no asignan memoria administrada. Un lote programado fallido restaura
todos los ticks y las entradas anteriores, incluido el fallo después de ticks ya correctos. Las actualizaciones
de entrada y los eventos programados terminales también rechazan observables de gas no finitos.

Los modelos con nodos de gas añaden la etiqueta 4 de huella del solver. Las huellas y los hashes de estado
de los modelos lineales o de cilindro existentes conservan su construcción anterior. Los parámetros de muestra
siguen siendo `unverified`.

## Integración JSON, de agente y portátil — 2026-09-22

El [laboratorio de red de gas](../assets/labs/gas-network.power.json) es el ejemplo compartido
para la repetición en JSON, CLI, MCP y el asset portátil. Contiene dos cámaras de gas, una restricción
interna controlada, una restricción de depósito controlada y un enlace de calor de pared. Sus eventos
incluyen ticks entre límites de informe y de presentación; cada límite de informe se compara
con la repetición decodificada del asset. Los parámetros siguen siendo sintéticos y `unverified`.

`power.model.v1` añade estas definiciones explícitas:

| Definición | Campos JSON y unidades |
|---|---|
| Nodo de gas | `domain: "gas"`; `storage`: m3 o l; `initial`: k; `position`: pa o bar; `gas`: gas_constant en j_kg_k y gamma > 1 |
| Orificio de gas | `kind: "gas_orifice"`; node_a/node_b; `initial_input`: fracción en [0, 1]; parámetros: area en m2 o mm2 y discharge_coefficient |
| Orificio de depósito | Orificio de gas con node_b ausente o cero; exige además reservoir_pressure en pa o bar y reservoir_temperature en k |
| Enlace de pared de gas | `kind: "gas_heat_link"`; node_a es gas, node_b es térmico; parámetros: conductance en w_k |

Un `input_channel` ausente o cero mantiene fija la apertura inicial explícita. Los parámetros de depósito
están prohibidos en una restricción de dos volúmenes. La composición solo es obligatoria en
nodos de gas. Los campos de comprobación nuevos son `mass_flow`, `heat_flow`, `reservoir_enthalpy` y
`mass_residual`; los campos de comprobación existentes de estado de gas y de energía siguen disponibles.

`CompiledModel.ValidateInput` comprueba las restricciones estáticas de canal y valor sin mutar
el estado. La validación del experimento y la creación del asset portátil lo usan para todas las aperturas
programadas, incluidos los eventos posteriores. El envío y el avance en tiempo de ejecución siguen haciendo
comprobaciones adicionales de observables dependientes del estado y conservan la reversión completa.

`power.asset.v3` y las versiones posteriores conservan la composición del gas, el área, el coeficiente de descarga y la presión
del depósito con registros de extensión indexados y acotados. La conductancia de pared, la temperatura del depósito,
las aperturas iniciales y los identificadores de canal de entrada usan los campos base del componente. Los lectores v1/v2
siguen admitidos para sus conjuntos de modelos originales y rechazan las definiciones de gas. Fixtures auténticos
anteriores al cambio verifican la compatibilidad hacia atrás. Consulta [el formato de asset](ASSET_FORMAT.es.md).

Las capacidades MCP de la versión 0.8.0 anuncian el dominio de gas, los componentes, la fidelidad, los límites
de apertura y los límites acotados del solver. `get_example_model` acepta `gas-network`. La compilación
exporta `GasNetwork.powerasset`; Studio añade recipientes esquemáticos, marcadores de depósito y
caminos de restricción y de calor con las entradas y los canales de salida existentes. Sus pruebas nuevas de importación y
de Play Mode exigen una ejecución real del Editor de Unity y no las cubre la evidencia de .NET.

## Validación y trabajo de motor pendiente

Los nueve grupos de modelo compilado y los seis grupos de primitivas de gas siguen ejecutándose contra
ambos destinos de Core. Las pruebas portátiles cubren además modelos mixtos de cilindro, gas y térmica,
cantidades no SI, composición no predeterminada, corrupción de extensiones, registros ausentes o duplicados,
compatibilidad v1/v2, límites programados, cancelación y reversión del cursor de eventos.
La equivalencia JSON/Core y la repetición MCP real cubren la frontera de integración.
Consulta [la validación](VALIDATION.es.md) para los resultados de la verificación en serie.

Los ensamblados Standard se ejecutan en .NET 10 para estas comprobaciones; esto no es evidencia del Editor de Unity ni
de IL2CPP. Las ecuaciones del solver solo de volumen fijo, los límites de integración y la construcción de la huella
siguen sin cambios para los modelos sin restricciones temporizadas ni seguimiento premmezclado. Los modelos con cámaras móviles
o restricciones temporizadas usan el acoplamiento partido versionado por separado, documentado en
[MOVING_CYLINDER.es.md](MOVING_CYLINDER.es.md) y [VALVE_TIMING.es.md](VALVE_TIMING.es.md).

La [combustión premmezclada](PREMIXED_COMBUSTION.es.md) opcional transporta ahora combustible, aire fresco
y productos con propiedades de gas constantes. La termoquímica detallada de especies, las muestras
de vehículo calibradas y los hitos completos de motor, transmisión y control siguen abiertos.
