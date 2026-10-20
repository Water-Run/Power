# Notas de reanudación del desarrollo del motor

[English](NEXT_ENGINE_STEP.md) · [简体中文](NEXT_ENGINE_STEP.zh-CN.md) · [Français](NEXT_ENGINE_STEP.fr.md) · [Русский](NEXT_ENGINE_STEP.ru.md) · [日本語](NEXT_ENGINE_STEP.ja.md) · [한국어](NEXT_ENGINE_STEP.ko.md) · [Deutsch](NEXT_ENGINE_STEP.de.md) · **Español** · [Italiano](NEXT_ENGINE_STEP.it.md) · [Português](NEXT_ENGINE_STEP.pt-BR.md)

## Punto de continuación

El tanque resuelve vapor de volumen finito con potenciales corregidos y toma de líquido separado.

[Equilibrio del tanque con volumen líquido finito](TANK_PHASE_EQUILIBRIUM.es.md)

## Próximo incremento

Resolver después flujo bifásico y pérdidas locales con propiedades explícitas y acoplamiento conservativo de volumen, momento y trabajo. Mantener ausentes las mediciones OEM faltantes. Continuar por separado encendido, admisión/escape, pérdidas mecánicas y ECU/TCU.

## Aceptación

Exigir referencias analíticas o límite independientes, balances completos de masa/energía y refinamiento adecuado. Preservar rollback de lote, cancelación, ramas, canales estables, replay portable y lectores anteriores. Continuar luego encendido, admisión/escape, pérdidas mecánicas, transmisión y coordinación ECU/TCU.

Casos acoplados de bomba, calor y venteo demuestran primer orden. Ambas fases fuente permanecen en el intervalo declarado; la densidad total queda bajo la líquida con margen binary64. Densidad/propiedades constantes, equilibrio instantáneo e interfaz plana son aproximaciones de investigación. Mezclas reales, tensión superficial, oleaje, cinética finita, calibración medida y Unity Editor/Play/Player/IL2CPP real siguen sin verificar.

[DEVELOPMENT_STATUS.es.md](DEVELOPMENT_STATUS.es.md) · [ROADMAP.es.md](ROADMAP.es.md) · [VALIDATION.es.md](VALIDATION.es.md)
