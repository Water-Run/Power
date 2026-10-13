# Notas de reanudación del desarrollo del motor

[English](NEXT_ENGINE_STEP.md) · [简体中文](NEXT_ENGINE_STEP.zh-CN.md) · [Français](NEXT_ENGINE_STEP.fr.md) · [Русский](NEXT_ENGINE_STEP.ru.md) · [日本語](NEXT_ENGINE_STEP.ja.md) · [한국어](NEXT_ENGINE_STEP.ko.md) · [Deutsch](NEXT_ENGINE_STEP.de.md) · **Español** · [Italiano](NEXT_ENGINE_STEP.it.md) · [Português](NEXT_ENGINE_STEP.pt-BR.md)

## Punto de continuación

El receptor admite equilibrio reversible y comprobaciones de saturación del suministro.

[Equilibrio de fases del combustible y límites de suministro](FUEL_PHASE_EQUILIBRIUM.es.md)

## Próximo incremento

Resolver después transferencia de vapor del tanque y flujo bifásico bomba/riel con propiedades explícitas y acoplamiento conservativo volumen/trabajo. Mantener ausentes las mediciones OEM faltantes. Encendido, admisión/escape, pérdidas mecánicas y coordinación ECU/TCU siguen como trabajos separados.

## Aceptación

Exigir referencias analíticas o límite independientes, balances completos de masa/energía y refinamiento adecuado. Preservar rollback de lote, cancelación, ramas, canales estables, replay portable y lectores anteriores. Continuar luego encendido, admisión/escape, pérdidas mecánicas, transmisión y coordinación ECU/TCU.

Parámetros `unverified`. No se aportan propiedades medidas de gasolina, cinética multicomponente o de velocidad finita, desplazamiento líquido/pulverización resueltos ni calibración OEM. La aceptación real Unity Editor/Play/Player/IL2CPP sigue pendiente.

[DEVELOPMENT_STATUS.es.md](DEVELOPMENT_STATUS.es.md) · [ROADMAP.es.md](ROADMAP.es.md) · [VALIDATION.es.md](VALIDATION.es.md)
