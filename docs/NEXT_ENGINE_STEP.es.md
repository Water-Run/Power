# Notas de reanudación del desarrollo del motor

[English](NEXT_ENGINE_STEP.md) · [简体中文](NEXT_ENGINE_STEP.zh-CN.md) · [Français](NEXT_ENGINE_STEP.fr.md) · [Русский](NEXT_ENGINE_STEP.ru.md) · [日本語](NEXT_ENGINE_STEP.ja.md) · [한국어](NEXT_ENGINE_STEP.ko.md) · [Deutsch](NEXT_ENGINE_STEP.de.md) · **Español** · [Italiano](NEXT_ENGINE_STEP.it.md) · [Português](NEXT_ENGINE_STEP.pt-BR.md)

## Punto de continuación

La bomba del dominio de combustible alimenta ahora una aguja física y descarga con calidad congelada en un receptor encendido con volumen líquido finito. Los inventarios completos incluyen cambio de fase, trabajo de desplazamiento, energía cinética e impulso sobre pared inmóvil.

[Bomba de combustible con cambio de fase impulsada por eje](FUEL_CHAMBER_PUMP.es.md)

## Próximo incremento

Resolver inercia y pérdidas locales de entrada, sensibilidad a relajación de fase y deslizamiento, después encendido, admisión/escape, pérdidas mecánicas y ECU/TCU coordinados. Cada ecuación necesita referencias independientes y refinamiento temporal; calibración medida y aceptación Unity real son separadas.

## Aceptación

Exigir referencias analíticas o límite independientes, balances completos de masa/energía y refinamiento adecuado. Preservar rollback de lote, cancelación, ramas, canales estables, replay portable y lectores anteriores. Continuar luego encendido, admisión/escape, pérdidas mecánicas, transmisión y coordinación ECU/TCU.

Los parámetros siguen `unverified`. Se aplican temperaturas declaradas, volumen gaseoso positivo y dominio de vapor diluido. Se omiten inercia local de entrada, nucleación finita, deslizamiento, spray resuelto, comportamiento crítico real y calibración medida. La combustión prescrita no acredita equilibrio químico ni entropía de reacción. Continúe entrada, encendido, admisión/escape, pérdidas y coordinación ECU/TCU.

[DEVELOPMENT_STATUS.es.md](DEVELOPMENT_STATUS.es.md) · [ROADMAP.es.md](ROADMAP.es.md) · [VALIDATION.es.md](VALIDATION.es.md)

[Descarga de combustible con calidad congelada y receptores finitos](FUEL_DISCHARGE.es.md)
