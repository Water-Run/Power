# Notas de reanudación del desarrollo del motor

[English](NEXT_ENGINE_STEP.md) · [简体中文](NEXT_ENGINE_STEP.zh-CN.md) · [Français](NEXT_ENGINE_STEP.fr.md) · [Русский](NEXT_ENGINE_STEP.ru.md) · [日本語](NEXT_ENGINE_STEP.ja.md) · [한국어](NEXT_ENGINE_STEP.ko.md) · [Deutsch](NEXT_ENGINE_STEP.de.md) · **Español** · [Italiano](NEXT_ENGINE_STEP.it.md) · [Português](NEXT_ENGINE_STEP.pt-BR.md)

## Punto de continuación

La bomba del dominio de combustible alimenta ahora una aguja física y descarga con calidad congelada en un receptor encendido con volumen líquido finito. Los inventarios completos incluyen cambio de fase, trabajo de desplazamiento, energía cinética e impulso sobre pared inmóvil.

[Bomba de combustible con cambio de fase impulsada por eje](FUEL_CHAMBER_PUMP.es.md)

`LiquidColumn` es una referencia C# independiente de un tubo horizontal rígido con sección, velocidad uniforme, densidad líquida y temperatura constantes. Las presiones absolutas se prescriben por intervalo. Las resistencias lineal y cuadrática son entradas de investigación explícitas. No es un componente del grafo ni cambia las ecuaciones de red o tobera. [Referencia de cantidad de movimiento en la entrada líquida](LIQUID_COLUMN.es.md)

Un `fuel_inertial_pipe` conecta dos nodos finitos de combustible y una pared térmica finita. La línea horizontal rígida posee masa líquida, energía calórica y cinética propias, además de los recipientes. Densidad constante y temperatura variable perfectamente mezclada. Puertos y línea deben permanecer líquidos; los enlaces cuasiestáticos conservan su modelo bifásico separado. [Cantidad de movimiento en una línea finita de combustible líquido](FUEL_INERTIAL_PIPE.es.md)

Un `equilibrium_fuel_duct` transporta masa, cantidad de movimiento y energía total por celdas explícitas `compressible_fuel`. El líquido es compresible y el vapor ideal; ambas fases comparten presión, temperatura y velocidad. El equilibrio local permite condensación y vaporización súbita. Dos depósitos finitos estacionarios y una pared térmica finita cierran los balances. [Conductos de combustible compresible en equilibrio](EQUILIBRIUM_FUEL_DUCT.es.md)

## Próximo incremento

Conecte conductos compresibles con cámaras accionadas por eje y fronteras de aguja/tobera compatibles, con trabajo de presión e hipótesis de fase explícitos. Después aborde relajación/deslizamiento, encendido, admisión/escape, pérdidas mecánicas y coordinación ECU/TCU. Calibración medida y aceptación real de Unity siguen separadas. [Conductos de combustible compresible en equilibrio](EQUILIBRIUM_FUEL_DUCT.es.md)

## Aceptación

Exigir referencias analíticas o límite independientes, balances completos de masa/energía y refinamiento adecuado. Preservar rollback de lote, cancelación, ramas, canales estables, replay portable y lectores anteriores. Continuar luego encendido, admisión/escape, pérdidas mecánicas, transmisión y coordinación ECU/TCU.

Los parámetros siguen `unverified`. Respete los rangos declarados de temperatura y presión, entalpía latente positiva, volúmenes de coexistencia separados y vapor diluido. Es un conducto horizontal rígido de sección constante y equilibrio homogéneo instantáneo. Omite nucleación a velocidad finita, deslizamiento, tensión superficial, gravedad, elasticidad y comportamiento crítico real. La difusión de primer orden puede ensanchar frentes; conservación y reproducción no demuestran precisión espacial ni validez de hardware medido. La aceptación real de Unity está pendiente.

[DEVELOPMENT_STATUS.es.md](DEVELOPMENT_STATUS.es.md) · [ROADMAP.es.md](ROADMAP.es.md) · [VALIDATION.es.md](VALIDATION.es.md)

[Descarga de combustible con calidad congelada y receptores finitos](FUEL_DISCHARGE.es.md)
