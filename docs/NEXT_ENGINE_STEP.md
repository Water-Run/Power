# Engine development resume notes

**English** · [简体中文](NEXT_ENGINE_STEP.zh-CN.md) · [Français](NEXT_ENGINE_STEP.fr.md) · [Русский](NEXT_ENGINE_STEP.ru.md) · [日本語](NEXT_ENGINE_STEP.ja.md) · [한국어](NEXT_ENGINE_STEP.ko.md) · [Deutsch](NEXT_ENGINE_STEP.de.md) · [Español](NEXT_ENGINE_STEP.es.md) · [Italiano](NEXT_ENGINE_STEP.it.md) · [Português](NEXT_ENGINE_STEP.pt-BR.md)

## Resume point

Finite fuel chambers now exchange pressure work with shafts and fill/deliver through ideal one-way restrictions, including bulk phase change under inlet starvation. Physical injector and receiver coupling remains open.

[Shaft-driven phase-resolving fuel pump](FUEL_CHAMBER_PUMP.md)

## Next increment

Connect the fuel-domain pump to physical injector discharge and cylinder/film receivers with explicit two-phase transport, displacement and kinetic-work accounting. Resolve local inlet inertia and sonic choking with independent references. Continue ignition, intake/exhaust, mechanical losses, transmission actuation and ECU/TCU coordination toward the full powertrain objective.

## Acceptance

Require independent analytic or limiting references, full mass/energy ledgers and step refinement appropriate to the new equations. Preserve batch rollback, cancellation, forks, stable channels, portable replay and prior asset readers. Then continue ignition, intake/exhaust, mechanical losses, transmission actuation and coordinated ECU/TCU control.

Parameters remain `unverified`. The material temperature, pressure and representable-volume limits apply throughout the cycle. The model assumes incompressible liquid, ideal pseudo-species vapor, instantaneous common-temperature equilibrium and zero external case pressure. Local inlet inertia/NPSH, sonic choking, nucleation delay, valve inertia/hysteresis, real fuel mixtures and measured calibration remain open. Physical injector handoff and receiver displacement/kinetic-work coupling are still separate requirements.

[DEVELOPMENT_STATUS.md](DEVELOPMENT_STATUS.md) · [ROADMAP.md](ROADMAP.md) · [VALIDATION.md](VALIDATION.md)
