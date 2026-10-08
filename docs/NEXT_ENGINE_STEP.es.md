# Notas de reanudación del desarrollo del motor

[English](NEXT_ENGINE_STEP.md) · [简体中文](NEXT_ENGINE_STEP.zh-CN.md) · [Français](NEXT_ENGINE_STEP.fr.md) · [Русский](NEXT_ENGINE_STEP.ru.md) · [日本語](NEXT_ENGINE_STEP.ja.md) · [한국어](NEXT_ENGINE_STEP.ko.md) · [Deutsch](NEXT_ENGINE_STEP.de.md) · **Español** · [Italiano](NEXT_ENGINE_STEP.it.md) · [Português](NEXT_ENGINE_STEP.pt-BR.md)

## Punto de continuación

El suministro incluye tanques finitos, retornos conservativos, espacio gaseoso geométrico y venteo explícito. Bomba, riel y gas intercambian trabajo interno; suministro líquido, evaporación y reacción prescrita siguen separados. Continuar desde contratos y evidencia actuales.

[Geometría del tanque y espacio gaseoso finito](TANK_HEADSPACE.es.md)

## Próximo incremento

Centrar el próximo incremento en equilibrio de fases por presión y cavitación con propiedades explícitas. Mantener ausentes las mediciones OEM faltantes y sin verificar los parámetros de investigación. Llenado/regulación medidos de bombas/válvulas, actuación magnética/electrónica y pulverización resuelta siguen pendientes.

## Aceptación

Exigir referencias analíticas o límite independientes, balances completos de masa/energía y refinamiento adecuado. Preservar rollback de lote, cancelación, ramas, canales estables, replay portable y lectores anteriores. Continuar luego encendido, admisión/escape, pérdidas mecánicas, transmisión y coordinación ECU/TCU.

Tanque rígido mezclado, líquido incompresible y gas ideal. Oleaje/forma hidrostática, equilibrio de fases, cavitación, mapas medidos de bomba/válvula, calibración OEM y Unity Editor/Play/Player/IL2CPP real siguen abiertos. Parámetros `unverified`.

[DEVELOPMENT_STATUS.es.md](DEVELOPMENT_STATUS.es.md) · [ROADMAP.es.md](ROADMAP.es.md) · [VALIDATION.es.md](VALIDATION.es.md)
