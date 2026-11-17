# Engine development resume notes

**English** · [简体中文](NEXT_ENGINE_STEP.zh-CN.md) · [Français](NEXT_ENGINE_STEP.fr.md) · [Русский](NEXT_ENGINE_STEP.ru.md) · [日本語](NEXT_ENGINE_STEP.ja.md) · [한국어](NEXT_ENGINE_STEP.ko.md) · [Deutsch](NEXT_ENGINE_STEP.de.md) · [Español](NEXT_ENGINE_STEP.es.md) · [Italiano](NEXT_ENGINE_STEP.it.md) · [Português](NEXT_ENGINE_STEP.pt-BR.md)

## Resume point

The fuel-domain pump now feeds a physical needle and frozen-quality discharge into a finite-liquid-volume fired receiver. Complete inventories include phase change, displacement work, jet kinetic energy and stationary-wall impulse.

[Shaft-driven phase-resolving fuel pump](FUEL_CHAMBER_PUMP.md)

`LiquidColumn` is a standalone C# reference for a horizontal rigid pipe with constant area, uniform velocity, constant liquid density and constant temperature. Absolute port pressures are prescribed during each local interval. Linear and quadratic resistance coefficients are explicit research inputs. It is not a graph component, and it does not change the existing fuel network or nozzle equations. [Liquid inlet momentum reference](LIQUID_COLUMN.md)

A `fuel_inertial_pipe` connects two finite fuel nodes and a finite thermal wall. Its rigid horizontal line owns liquid mass, caloric energy and kinetic energy, in addition to the inventories of both vessels. The line has constant density and a well-mixed variable temperature. Both ports and the entire line must remain liquid; the existing quasi-static links retain their separate two-phase behavior. [Finite liquid fuel line momentum](FUEL_INERTIAL_PIPE.md)

An `equilibrium_fuel_duct` transports mass, momentum and total energy through explicit `compressible_fuel` cells. The liquid is compressible and the vapor is ideal; both phases share pressure, temperature and velocity. Local equilibrium permits condensation and flashing. Two finite stationary reservoirs and a finite thermal wall close the balances. [Compressible equilibrium fuel ducts](EQUILIBRIUM_FUEL_DUCT.md)

A `compressible_fuel_chamber` connects a rotational shaft to a finite `compressible_fuel` reservoir. Shaft motion changes its volume, while the same pressure work changes fluid energy and shaft torque. The reservoir can supply or receive an `equilibrium_fuel_duct`, including equilibrium flashing and condensation. [Shaft-driven compressible fuel chambers](COMPRESSIBLE_FUEL_CHAMBER.md)

A `compressible_fuel_nozzle` carries one-way flow between finite `compressible_fuel` reservoirs, including shaft-driven chambers. Liquid compression energy and ideal vapor share the reservoir material. Declare `phase_mode` as `frozen`: mass quality stays constant during transit while bulk reservoirs re-equilibrate. [Frozen compressible fuel nozzles](COMPRESSIBLE_FUEL_NOZZLE.md)

Set `compressible_fuel_nozzle` `phase_mode` to `equilibrium` for instantaneous homogeneous phase equilibrium during transit. Liquid and vapor share pressure, temperature and velocity, and vapor quality changes along the isentrope. The same compressible liquid cold potential and ideal-vapor reference supply both nozzle and finite reservoirs. The `frozen` mode remains a separate supported assumption. [Equilibrium fuel nozzle transit](EQUILIBRIUM_FUEL_NOZZLE.md)

A `relaxing_fuel` node carries independent vapor mass quality. Liquid and vapor share pressure, temperature and velocity, while chemical equilibrium takes a declared finite time. A `relaxing_fuel_duct` transports mass, momentum, total energy and vapor mass through explicit cells. Residence follows cell geometry and actual velocity. [Finite-rate fuel phase relaxation](RELAXING_FUEL.md)

## Next increment

Extend compatible finite-rate boundary coupling and obtain material relaxation measurements, then add resolved spray and fired receivers. Continue ignition, intake/exhaust, mechanical losses, transmission actuation and coordinated ECU/TCU control. Measured calibration and actual Unity acceptance remain separate. [Compressible equilibrium fuel ducts](EQUILIBRIUM_FUEL_DUCT.md)

## Acceptance

Require independent analytic or limiting references, full mass/energy ledgers and step refinement appropriate to the new equations. Preserve batch rollback, cancellation, forks, stable channels, portable replay and prior asset readers. Then continue ignition, intake/exhaust, mechanical losses, transmission actuation and coordinated ECU/TCU control.

Parameters remain `unverified`. Declare `initial_vapor_mass_fraction` in [0,1] and an integer relaxation time from 1 ns through one hour. Stay within the material temperature and positive-pressure bounds. The model assumes a rigid horizontal constant-area duct, one pure pseudo-fluid, zero liquid thermal expansion and ideal vapor. It omits phase slip, interfacial heat delay, surface tension, spray and carrier gas. Direct relaxing-domain pump, nozzle, moving chamber and fired receiver coupling remain open. Conservation, replay and refinement do not establish measured hardware validity or actual Unity acceptance. Use dilute vapor away from the real-fluid critical region; the material must retain positive latent enthalpy and separated coexistence volumes.

[DEVELOPMENT_STATUS.md](DEVELOPMENT_STATUS.md) · [ROADMAP.md](ROADMAP.md) · [VALIDATION.md](VALIDATION.md)

[Frozen-quality fuel discharge and finite receivers](FUEL_DISCHARGE.md)
