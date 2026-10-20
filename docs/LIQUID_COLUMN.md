# Liquid inlet momentum reference

**English** · [简体中文](LIQUID_COLUMN.zh-CN.md) · [Français](LIQUID_COLUMN.fr.md) · [Русский](LIQUID_COLUMN.ru.md) · [日本語](LIQUID_COLUMN.ja.md) · [한국어](LIQUID_COLUMN.ko.md) · [Deutsch](LIQUID_COLUMN.de.md) · [Español](LIQUID_COLUMN.es.md) · [Italiano](LIQUID_COLUMN.it.md) · [Português](LIQUID_COLUMN.pt-BR.md)

## Physical boundary

`LiquidColumn` is a standalone C# reference for a horizontal rigid pipe with constant area, uniform velocity, constant liquid density and constant temperature. Absolute port pressures are prescribed during each local interval. Linear and quadratic resistance coefficients are explicit research inputs. It is not a graph component, and it does not change the existing fuel network or nozzle equations.

## Momentum and energy

The two ports have equal flow and velocity. Advected kinetic-energy flux cancels, while pressure work changes the column's stored kinetic energy and produces friction heat. An isothermal bath receives that heat; fluid temperature and internal energy remain constant. The returned entropy production is friction heat divided by the declared temperature. This boundary does not represent a finite thermal inventory.

```text
I = rho L / A                         [Pa s^2 / m3]
R                                    [Pa s / m3]
K                                    [Pa s^2 / m6]
I dQ/dt = p_A - p_B - R Q - K Q abs(Q)
E_kinetic = I Q^2 / 2
Q_mean = (Q_before + Q_after) / 2
I (Q_after-Q_before) = dt (p_A-p_B-R Q_mean-K Q_mean abs(Q_mean))
delta_V = dt Q_mean
W_pressure = (p_A-p_B) delta_V
H_friction = dt (R Q_mean^2 + K abs(Q_mean)^3)
W_pressure = delta_E_kinetic + H_friction
S_produced = H_friction / T
```

Implicit midpoint solves the monotone scalar momentum equation. A reversal is split at the zero-flow event using that same equation, preserving separate forward and reverse volumes and their heat. Accepted calls use at most two internal intervals. A step that overshoots the steady-flow equilibrium returns `StepTooLarge`; reduce the duration. Energy and impulse residuals are checked at binary64 rounding scale. A coarse accepted step still needs an accuracy check.

## Core API and numerical evidence

Construct `LiquidColumn` with explicit SI geometry, density, resistance, temperature and minimum absolute pressure. Pass `LiquidColumnState` and a duration in seconds to `Advance`. SI units appear in property names. The result contains signed and directional transfer, pressure work, kinetic-energy change, friction heat and entropy production. Rejections leave the output default and input unchanged; calls allocate no managed memory. This local duration does not replace the integer clock of the simulation.

Independent lossless acceleration, exponential linear-drag and hyperbolic-tangent quadratic-drag solutions check the limits. An independently integrated changing-pressure RK4 reference checks smooth second-order refinement, including reversal. Tests also cover passive decay, steady flow, complete energy balance, nonnegative entropy, rejected pressure/coarse-step inputs, immutable shared use and allocations on both assembly targets.

## Next coupling

Parameters remain `unverified`. The pressure floor is a caller-declared liquid-validity bound, not a predicted vapor pressure or NPSH margin. Compressibility, acoustic waves, gravity, moving geometry, thermal storage, cavitation, phase slip and phase-relaxation kinetics are outside this reference. The existing two-phase fuel links remain quasi-static. Actual Unity acceptance and measured hardware calibration remain separate.

Connect the liquid momentum reference to the finite fuel network with an explicit line energy inventory and compatible port enthalpy. Establish variable-density and phase-change validity before claiming cavitating inlet dynamics. Continue phase relaxation, ignition, intake/exhaust, mechanical losses and coordinated ECU/TCU control.

[Modelica: momentum and energy balances](https://doc.modelica.org/Modelica%203.2.3/Resources/helpWSM/Modelica/Modelica.Fluid.UsersGuide.ComponentDefinition.BalanceEquations.html) · [Modelica: pipe flow models](https://doc.modelica.org/om/Modelica.Fluid.Pipes.BaseClasses.FlowModels.html) · [VALIDATION.md](VALIDATION.md)
