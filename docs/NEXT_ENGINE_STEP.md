# Engine development resume notes

**English** · [简体中文](NEXT_ENGINE_STEP.zh-CN.md) · [Français](NEXT_ENGINE_STEP.fr.md) · [Русский](NEXT_ENGINE_STEP.ru.md) · [日本語](NEXT_ENGINE_STEP.ja.md) · [한국어](NEXT_ENGINE_STEP.ko.md) · [Deutsch](NEXT_ENGINE_STEP.de.md) · [Español](NEXT_ENGINE_STEP.es.md) · [Italiano](NEXT_ENGINE_STEP.it.md) · [Português](NEXT_ENGINE_STEP.pt-BR.md)

## Resume point

The receiver supports reversible fuel phase equilibrium and liquid-supply saturation checks.

[Fuel phase equilibrium and liquid supply limits](FUEL_PHASE_EQUILIBRIUM.md)

## Next increment

Next, resolve tank vapor transfer and two-phase pump/rail flow with explicit properties and conservative volume/work coupling. Keep missing OEM measurements missing. Continue ignition, intake/exhaust, mechanical losses and ECU/TCU coordination as separate work.

## Acceptance

Require independent analytic or limiting references, full mass/energy ledgers and step refinement appropriate to the new equations. Preserve batch rollback, cancellation, forks, stable channels, portable replay and prior asset readers. Then continue ignition, intake/exhaust, mechanical losses, transmission actuation and coordinated ECU/TCU control.

Parameters remain `unverified`. The model does not supply measured gasoline properties, multicomponent or finite-rate phase kinetics, resolved liquid displacement/spray, or OEM calibration. Actual Unity Editor/Play/Player/IL2CPP acceptance remains pending.

[DEVELOPMENT_STATUS.md](DEVELOPMENT_STATUS.md) · [ROADMAP.md](ROADMAP.md) · [VALIDATION.md](VALIDATION.md)
