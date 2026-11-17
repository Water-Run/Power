# Notas de reanudación del desarrollo del motor

[English](NEXT_ENGINE_STEP.md) · [简体中文](NEXT_ENGINE_STEP.zh-CN.md) · [Français](NEXT_ENGINE_STEP.fr.md) · [Русский](NEXT_ENGINE_STEP.ru.md) · [日本語](NEXT_ENGINE_STEP.ja.md) · [한국어](NEXT_ENGINE_STEP.ko.md) · [Deutsch](NEXT_ENGINE_STEP.de.md) · **Español** · [Italiano](NEXT_ENGINE_STEP.it.md) · [Português](NEXT_ENGINE_STEP.pt-BR.md)

## Punto de continuación

La bomba del dominio de combustible alimenta ahora una aguja física y descarga con calidad congelada en un receptor encendido con volumen líquido finito. Los inventarios completos incluyen cambio de fase, trabajo de desplazamiento, energía cinética e impulso sobre pared inmóvil.

[Bomba de combustible con cambio de fase impulsada por eje](FUEL_CHAMBER_PUMP.es.md)

`LiquidColumn` es una referencia C# independiente de un tubo horizontal rígido con sección, velocidad uniforme, densidad líquida y temperatura constantes. Las presiones absolutas se prescriben por intervalo. Las resistencias lineal y cuadrática son entradas de investigación explícitas. No es un componente del grafo ni cambia las ecuaciones de red o tobera. [Referencia de cantidad de movimiento en la entrada líquida](LIQUID_COLUMN.es.md)

Un `fuel_inertial_pipe` conecta dos nodos finitos de combustible y una pared térmica finita. La línea horizontal rígida posee masa líquida, energía calórica y cinética propias, además de los recipientes. Densidad constante y temperatura variable perfectamente mezclada. Puertos y línea deben permanecer líquidos; los enlaces cuasiestáticos conservan su modelo bifásico separado. [Cantidad de movimiento en una línea finita de combustible líquido](FUEL_INERTIAL_PIPE.es.md)

Un `equilibrium_fuel_duct` transporta masa, cantidad de movimiento y energía total por celdas explícitas `compressible_fuel`. El líquido es compresible y el vapor ideal; ambas fases comparten presión, temperatura y velocidad. El equilibrio local permite condensación y vaporización súbita. Dos depósitos finitos estacionarios y una pared térmica finita cierran los balances. [Conductos de combustible compresible en equilibrio](EQUILIBRIUM_FUEL_DUCT.es.md)

Una `compressible_fuel_chamber` conecta un eje a un depósito finito `compressible_fuel`. Su movimiento cambia el volumen; el mismo trabajo de presión cambia energía del fluido y par del eje. El depósito puede alimentar o recibir un `equilibrium_fuel_duct`, con vaporización súbita y condensación en equilibrio. [Cámaras de combustible compresible accionadas por eje](COMPRESSIBLE_FUEL_CHAMBER.es.md)

Una `compressible_fuel_nozzle` transporta flujo unidireccional entre depósitos finitos `compressible_fuel`, incluidas cámaras accionadas por eje. Compresión líquida y vapor ideal comparten el material. Declare `phase_mode` como `frozen`: la calidad másica permanece fija durante el tránsito y los depósitos se reequilibran. [Toberas de combustible compresible de calidad congelada](COMPRESSIBLE_FUEL_NOZZLE.es.md)

Defina `phase_mode` de `compressible_fuel_nozzle` como `equilibrium` para equilibrio homogéneo instantáneo durante el tránsito. Líquido y vapor comparten presión, temperatura y velocidad; la calidad cambia sobre la isentrópica. Tobera y depósitos finitos usan el mismo potencial líquido compresible y referencia de vapor ideal. `frozen` sigue siendo una hipótesis independiente admitida. [Tránsito de combustible en equilibrio por toberas](EQUILIBRIUM_FUEL_NOZZLE.es.md)

Un nodo `relaxing_fuel` tiene una fracción másica de vapor independiente. Líquido y vapor comparten presión, temperatura y velocidad, pero el equilibrio químico requiere un tiempo finito declarado. Un `relaxing_fuel_duct` transporta masa, cantidad de movimiento, energía total y masa de vapor por celdas explícitas. La residencia depende de su geometría y velocidad real. [Relajación de fase del combustible a velocidad finita](RELAXING_FUEL.es.md)

## Próximo incremento

Ampliar los acoplamientos de frontera compatibles con velocidad finita y obtener medidas de relajación del material, después añadir pulverización resuelta y receptores con combustión. Continuar encendido, admisión/escape, pérdidas mecánicas, actuación de transmisión y coordinación ECU/TCU. La calibración medida y la aceptación real de Unity siguen separadas. [Conductos de combustible compresible en equilibrio](EQUILIBRIUM_FUEL_DUCT.es.md)

## Aceptación

Exigir referencias analíticas o límite independientes, balances completos de masa/energía y refinamiento adecuado. Preservar rollback de lote, cancelación, ramas, canales estables, replay portable y lectores anteriores. Continuar luego encendido, admisión/escape, pérdidas mecánicas, transmisión y coordinación ECU/TCU.

Los parámetros siguen `unverified`. Declare `initial_vapor_mass_fraction` en [0,1] y un tiempo entero de 1 ns a una hora. Respete los límites de temperatura y presión positiva del material. Se supone un conducto rígido horizontal de área constante, un pseudofluido puro, expansión térmica líquida nula y vapor ideal. No incluye deslizamiento de fases, demora térmica interfacial, tensión superficial, pulverización ni gas portador. Quedan pendientes los acoplamientos directos del dominio relajante a bombas, toberas, cámaras móviles y receptores con combustión. Conservación, reproducción y refinamiento no acreditan validez experimental ni aceptación real de Unity. Utilice vapor diluido lejos de la región crítica real; el material debe mantener entalpía latente positiva y volúmenes específicos de coexistencia separados.

[DEVELOPMENT_STATUS.es.md](DEVELOPMENT_STATUS.es.md) · [ROADMAP.es.md](ROADMAP.es.md) · [VALIDATION.es.md](VALIDATION.es.md)

[Descarga de combustible con calidad congelada y receptores finitos](FUEL_DISCHARGE.es.md)
