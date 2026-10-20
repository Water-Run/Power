# Engine development resume notes

**English** · [简体中文](NEXT_ENGINE_STEP.zh-CN.md) · [Français](NEXT_ENGINE_STEP.fr.md) · [Русский](NEXT_ENGINE_STEP.ru.md) · [日本語](NEXT_ENGINE_STEP.ja.md) · [한국어](NEXT_ENGINE_STEP.ko.md) · [Deutsch](NEXT_ENGINE_STEP.de.md) · [Español](NEXT_ENGINE_STEP.es.md) · [Italiano](NEXT_ENGINE_STEP.it.md) · [Português](NEXT_ENGINE_STEP.pt-BR.md)

## Resume point

The fuel-domain pump now feeds a physical needle and frozen-quality discharge into a finite-liquid-volume fired receiver. Complete inventories include phase change, displacement work, jet kinetic energy and stationary-wall impulse.

[Shaft-driven phase-resolving fuel pump](FUEL_CHAMBER_PUMP.md)

## Next increment

Resolve local inlet inertia and losses, phase-relaxation and slip sensitivity, then extend ignition, intake/exhaust, mechanical losses and coordinated ECU/TCU control. Validate each new equation against independent references and time refinement; obtain measured calibration and actual Unity acceptance separately.

## Acceptance

Require independent analytic or limiting references, full mass/energy ledgers and step refinement appropriate to the new equations. Preserve batch rollback, cancellation, forks, stable channels, portable replay and prior asset readers. Then continue ignition, intake/exhaust, mechanical losses, transmission actuation and coordinated ECU/TCU control.

Parameters remain `unverified`. Enforce declared temperature bounds, positive gas volume and the dilute-vapor material domain. The model omits local inlet inertia, finite-rate nucleation, phase slip, resolved spray, real-fluid critical behavior and measured hardware calibration. Prescribed combustion does not establish chemical-equilibrium or reaction-entropy accuracy. Continue inlet dynamics, ignition, intake/exhaust, mechanical losses and coordinated ECU/TCU control.

[DEVELOPMENT_STATUS.md](DEVELOPMENT_STATUS.md) · [ROADMAP.md](ROADMAP.md) · [VALIDATION.md](VALIDATION.md)

[Frozen-quality fuel discharge and finite receivers](FUEL_DISCHARGE.md)
