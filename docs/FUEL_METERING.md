# Finite gaseous fuel rail and cycle-dose metering

**English** · [简体中文](FUEL_METERING.zh-CN.md) · [Français](FUEL_METERING.fr.md) · [Русский](FUEL_METERING.ru.md) · [日本語](FUEL_METERING.ja.md) · [한국어](FUEL_METERING.ko.md) · [Deutsch](FUEL_METERING.de.md) · [Español](FUEL_METERING.es.md) · [Italiano](FUEL_METERING.it.md) · [Português](FUEL_METERING.pt-BR.md)

`gas_fuel_injector` transfers fuel from a finite tracked gas rail into a compatible
gas chamber. A forward-crank window latches one requested fuel mass per cycle;
pressure, temperature, nozzle area and available rail inventory determine actual
delivery. The controller throttles the port near its quota. It does not add fuel
directly to the receiving state or assume that a requested dose was delivered.

This extends the current constant-property gaseous mixture model. Separate air
admission, fuel metering, mixing and prescribed reaction can now be modeled. It does
not implement liquid gasoline spray, evaporation, needle/electrical dynamics, liquid
rail/tank physics, detailed species properties or calibrated OEM injection/ECU behavior.
These remain required work toward the full engine objective.

## Flow, constituents and energy

The source and receiver are distinct finite premixed gas nodes. Both share R, gamma,
heating value and stoichiometric ratio. The source can be pure gaseous fuel or a
tracked fuel-containing mixture. Flow follows the existing pressure-dependent,
choked/subcritical [gas orifice](GAS_NETWORK.md) law with explicit area and discharge
coefficient. Reverse-pressure flow is closed: receiver gas does not backfill the rail.

The quota concerns **fuel mass**, not total source-mixture mass. Each gas advance
limits fuel rate by `remaining_cycle_fuel / advance_duration`. The same flow factor
scales total mass and upstream thermal enthalpy; constituent fractions use the actual
upstream state. The Heun stages and accepted delivery history use the same transfers.
Consequently, the receiving fuel, rail depletion, thermal energy and chemical
inventory remain consistent even when rail pressure falls or source composition changes.

Internal fuel transfer does not enter external `fuel_energy_in` or reservoir
enthalpy. Stored source chemical energy moves with the fuel and only becomes gas
heat when the separate burn component consumes it. Incompatible chemistry or
caloric gas parameters are rejected at compilation. Dose limiting changes admitted
flow rather than correcting mass or energy after integration.

## Cycle and command contract

| Data | Meaning |
|---|---|
| Source A / receiver B | Distinct finite tracked gas volumes |
| `area`, `discharge_coefficient` | Positive m2/mm2 and coefficient in (0,1] |
| `crank_node` | Explicit rotational timing reference; a moving crank chamber uses its own crank |
| `cycle_angle` | 360 or 720 degrees, with explicit angle units |
| `start_angle`, `duration_angle` | Window start and positive duration no greater than one cycle |
| `maximum_dose` | Positive per-cycle fuel limit in kg |
| Input `fuel_dose_per_cycle` | Requested kg in [0,maximum_dose] |

The ideal rectangular window opens only during forward motion. The first accepted
open-window advance samples the requested dose. Writes during that observed cycle
apply to the next window; the requested-cycle channel continues showing the latched
target. Zero disables that cycle. If available pressure/fuel is insufficient, actual
delivery remains below the target. A successful step does not imply a full dose.

Cycle ordinals are signed bounded integers reconstructed from representable crank
angles. Returning to an earlier observed cycle cannot reset its quota; reversing
motion closes the window. Travel per mechanical interval is limited to
`min(0.25 rad,duration/8)`. Window endpoints use the existing fixed-tick/symmetric
split approximation, so timing near discontinuities requires timestep refinement.
No exact continuous switching-time claim is made.

Outputs include current metering-window opening, last-tick mean delivered fuel rate,
latched requested dose, delivered dose in the observed cycle and cumulative delivered
fuel. Finite rail pressure, temperature and remaining fuel are ordinary gas channels.
All quota/ordinal/delivery histories, including compensation, copy and hash with
speculative state. Cancellation, late failure and forks preserve the complete state.

Asset v17 preserves both nozzle and timing records. JSON, CLI and MCP share the same
model; schema and compiler check units, bounds, finite compatible endpoints and
timing ownership. Core targets remain dependency-free net10.0/netstandard2.1.

## Experiment and evidence

`metered-fired-cylinder` replaces premixed-fuel intake with an air-only inlet and a
finite gaseous rail. An ideal injector supplies explicit 8/12/4-mg requests through
a crank window before the prescribed Wiebe burn. Dose changes are sampled at the
next observed window. The six-tenths-second experiment delivers 28 mg, burns about
27.930 mg and releases about 1228.918 J; unburned and boundary-lost fuel remain in
the constituent account. All parameters are synthetic and unverified.

Checks cover exact quota-limited delivery, finite rail starvation, reverse pressure,
mid-window command latching, reversal without quota reissue, an independent two-vessel
mass/enthalpy ODE with smooth refinement, analytic metered burn, complete transactions,
state-capacity/units/chemistry and allocation-free stepping. Reports/portable/MCP agree
at every boundary. The separate delivery and burn channels distinguish an accepted
command from actual fuel and heat. See [VALIDATION.md](VALIDATION.md) for measured
bounds and runtime/performance evidence. Studio views and Edit/Play tests are prepared
in C# 9 source; actual Unity Editor and Player acceptance remain pending.
