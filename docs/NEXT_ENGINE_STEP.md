# Engine development resume notes

**English** · [简体中文](NEXT_ENGINE_STEP.zh-CN.md) · [Français](NEXT_ENGINE_STEP.fr.md) · [Русский](NEXT_ENGINE_STEP.ru.md) · [日本語](NEXT_ENGINE_STEP.ja.md) · [한국어](NEXT_ENGINE_STEP.ko.md) · [Deutsch](NEXT_ENGINE_STEP.de.md) · [Español](NEXT_ENGINE_STEP.es.md) · [Italiano](NEXT_ENGINE_STEP.it.md) · [Português](NEXT_ENGINE_STEP.pt-BR.md)

## Resume point

The fuel-domain pump now feeds a physical needle and frozen-quality discharge into a finite-liquid-volume fired receiver. Complete inventories include phase change, displacement work, jet kinetic energy and stationary-wall impulse.

[Shaft-driven phase-resolving fuel pump](FUEL_CHAMBER_PUMP.md)

`LiquidColumn` is a standalone C# reference for a horizontal rigid pipe with constant area, uniform velocity, constant liquid density and constant temperature. Absolute port pressures are prescribed during each local interval. Linear and quadratic resistance coefficients are explicit research inputs. It is not a graph component, and it does not change the existing fuel network or nozzle equations. [Liquid inlet momentum reference](LIQUID_COLUMN.md)

A `fuel_inertial_pipe` connects two finite fuel nodes and a finite thermal wall. Its rigid horizontal line owns liquid mass, caloric energy and kinetic energy, in addition to the inventories of both vessels. The line has constant density and a well-mixed variable temperature. Both ports and the entire line must remain liquid; the existing quasi-static links retain their separate two-phase behavior. [Finite liquid fuel line momentum](FUEL_INERTIAL_PIPE.md)

An `equilibrium_fuel_duct` transports mass, momentum and total energy through explicit `compressible_fuel` cells. The liquid is compressible and the vapor is ideal; both phases share pressure, temperature and velocity. Local equilibrium permits condensation and flashing. Two finite stationary reservoirs and a finite thermal wall close the balances. [Compressible equilibrium fuel ducts](EQUILIBRIUM_FUEL_DUCT.md)

## Next increment

Connect compressible ducts to compatible shaft-driven chambers and needle/nozzle boundaries, with explicit pressure work and phase assumptions. Then address phase relaxation/slip, ignition, intake/exhaust, mechanical losses and coordinated ECU/TCU control. Measured calibration and actual Unity acceptance remain separate. [Compressible equilibrium fuel ducts](EQUILIBRIUM_FUEL_DUCT.md)

## Acceptance

Require independent analytic or limiting references, full mass/energy ledgers and step refinement appropriate to the new equations. Preserve batch rollback, cancellation, forks, stable channels, portable replay and prior asset readers. Then continue ignition, intake/exhaust, mechanical losses, transmission actuation and coordinated ECU/TCU control.

Parameters remain `unverified`. Stay inside the declared temperature and pressure range, with positive latent enthalpy, separated coexistence volumes and dilute vapor. This is a rigid constant-area horizontal duct with instantaneous homogeneous equilibrium. It omits finite-rate nucleation, phase slip, surface tension, gravity, pipe elasticity and real-fluid critical behavior. First-order numerical diffusion can smear phase and acoustic fronts; conservation and replay do not establish spatial accuracy or measured hardware validity. Actual Unity acceptance remains pending.

[DEVELOPMENT_STATUS.md](DEVELOPMENT_STATUS.md) · [ROADMAP.md](ROADMAP.md) · [VALIDATION.md](VALIDATION.md)

[Frozen-quality fuel discharge and finite receivers](FUEL_DISCHARGE.md)
