# Notas de reanudación del desarrollo del motor

[English](NEXT_ENGINE_STEP.md) · [简体中文](NEXT_ENGINE_STEP.zh-CN.md) · [Français](NEXT_ENGINE_STEP.fr.md) · [Русский](NEXT_ENGINE_STEP.ru.md) · [日本語](NEXT_ENGINE_STEP.ja.md) · [한국어](NEXT_ENGINE_STEP.ko.md) · [Deutsch](NEXT_ENGINE_STEP.de.md) · **Español** · [Italiano](NEXT_ENGINE_STEP.it.md) · [Português](NEXT_ENGINE_STEP.pt-BR.md)

## Punto de continuación

Las cámaras finitas intercambian trabajo de presión con ejes y llenan/entregan por restricciones unidireccionales ideales, con fase bajo entrada insuficiente. Falta acoplar inyector físico y receptor.

[Bomba de combustible con cambio de fase impulsada por eje](FUEL_CHAMBER_PUMP.es.md)

## Próximo incremento

Conectar bomba a inyector físico y receptores de cilindro/película con transporte bifásico, desplazamiento y trabajo cinético explícitos. Resolver inercia de entrada y flujo crítico con referencias independientes. Continuar encendido, admisión/escape, pérdidas, actuación de transmisión y ECU/TCU hacia el objetivo completo.

## Aceptación

Exigir referencias analíticas o límite independientes, balances completos de masa/energía y refinamiento adecuado. Preservar rollback de lote, cancelación, ramas, canales estables, replay portable y lectores anteriores. Continuar luego encendido, admisión/escape, pérdidas mecánicas, transmisión y coordinación ECU/TCU.

Los parámetros siguen `unverified`. Temperatura, presión y volumen representable están limitados durante todo el ciclo. Se asumen líquido incompresible, vapor de pseudoespecie ideal, equilibrio instantáneo a temperatura común y presión exterior de carcasa cero. Inercia local/NPSH, flujo crítico, retraso de nucleación, inercia/histéresis de válvula, mezclas reales y calibración medida siguen abiertos. Inyector físico y trabajo de desplazamiento/cinética del receptor son requisitos separados.

[DEVELOPMENT_STATUS.es.md](DEVELOPMENT_STATUS.es.md) · [ROADMAP.es.md](ROADMAP.es.md) · [VALIDATION.es.md](VALIDATION.es.md)
