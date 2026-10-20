# Notas de reanudación del desarrollo del motor

[English](NEXT_ENGINE_STEP.md) · [简体中文](NEXT_ENGINE_STEP.zh-CN.md) · [Français](NEXT_ENGINE_STEP.fr.md) · [Русский](NEXT_ENGINE_STEP.ru.md) · [日本語](NEXT_ENGINE_STEP.ja.md) · [한국어](NEXT_ENGINE_STEP.ko.md) · [Deutsch](NEXT_ENGINE_STEP.de.md) · **Español** · [Italiano](NEXT_ENGINE_STEP.it.md) · [Português](NEXT_ENGINE_STEP.pt-BR.md)

## Punto de continuación

Los recipientes de combustible intercambian mezclas líquido/vapor por restricciones disipativas conservativas y enlaces térmicos finitos. Faltan llenado de bomba, eje y descarga del inyector.

[Red de transporte de combustible en equilibrio](EQUILIBRIUM_FUEL_NETWORK.es.md)

## Próximo incremento

Conectar volúmenes de combustible a llenado de bomba por eje y descarga física del inyector con acoplamiento conservativo de materia, cantidad de movimiento y trabajo. Resolver inercia local de entrada y flujo crítico con referencias independientes. Continuar encendido, admisión/escape, pérdidas, accionamiento de transmisión y ECU/TCU sin considerar esta red un tren motriz completo.

## Aceptación

Exigir referencias analíticas o límite independientes, balances completos de masa/energía y refinamiento adecuado. Preservar rollback de lote, cancelación, ramas, canales estables, replay portable y lectores anteriores. Continuar luego encendido, admisión/escape, pérdidas mecánicas, transmisión y coordinación ECU/TCU.

Siguen vigentes los límites de material y deformación representable del cierre. Los parámetros siguen `unverified`. Llenado de bomba y trabajo del eje, conexión de inyector, inercia local, flujo crítico, retraso de nucleación, deslizamiento y medidas de mezclas reales siguen pendientes. Los rieles de inyector alimentados por bomba conservan su ruta de líquido separado hasta integrarlos.

[DEVELOPMENT_STATUS.es.md](DEVELOPMENT_STATUS.es.md) · [ROADMAP.es.md](ROADMAP.es.md) · [VALIDATION.es.md](VALIDATION.es.md)
