# Mechanical spool metering and pressure regulation

**English** · [简体中文](HYDRAULIC_SPOOL.zh-CN.md) · [Français](HYDRAULIC_SPOOL.fr.md) · [Русский](HYDRAULIC_SPOOL.ru.md) · [日本語](HYDRAULIC_SPOOL.ja.md) · [한국어](HYDRAULIC_SPOOL.ko.md) · [Deutsch](HYDRAULIC_SPOOL.de.md) · [Español](HYDRAULIC_SPOOL.es.md) · [Italiano](HYDRAULIC_SPOOL.it.md) · [Português](HYDRAULIC_SPOOL.pt-BR.md)

`hydraulic_spool_valve` meters a hydraulic port from the actual displacement of an
explicit `hydraulic_piston`. The piston supplies mass, swept fluid volume, pressure
force and compliant stroke ends; a separate `linear_spring` supplies return force,
preload and damping. Multiple metering lands may reference the same piston.

## Equations and boundaries

Closed and full-open positions define a signed travel L. With displacement x:

```text
opening = clamp((x - closed_position) / L, 0, 1)
d       = pA - pB
Q       = K * opening * d / (d*d + transition_pressure*transition_pressure)^(1/4)
loss    = Q * d >= 0
```

K is an explicit full-open coefficient in m3/(s*sqrt(Pa)); transition pressure is
positive. The implementation scales the denominator to avoid squaring huge pressure
differences. Signed travel supports both opening directions. A closed land seals
exactly; leakage needs an explicit additional path. Only opening saturates: position,
pressure, velocity and stored energy are not clamped.

The land is pressure-balanced, with axial jet force neglected. Its metering-port
pressure difference does not apply an extra axial force to the piston. The explicit
actuator's front/back chamber pressures provide its driving force. Restriction heat
and the piston/spring work use the existing conserving ledgers. This model excludes
seal friction, flow-force momentum effects, cavitation, wear and temperature-dependent
geometry/viscosity. It is a declared research reduction rather than a calibrated valve.

[MathWorks Spool Orifice (IL)](https://www.mathworks.com/help/hydro/ref/spoolorificeil.html)
documents variable opening area and a separate axial flow-force option. Power! uses
its own normalized linear land and existing passive restriction law; no geometry,
fluid-property defaults or implementation code were copied.

## Shared solve and contracts

The valve reads `x_old + dx/2` in the same joint Newton solve as its fluid pressures,
piston force and mechanical constraints. Analytic derivatives include both pressure
and land displacement. Within the metering travel, `dOpening/dx=1/L`; outside it the
derivative is zero. At each endpoint, the Jacobian uses the mean one-sided slope.
This preserves a simultaneous feedback loop rather than a delayed opening command.

| Parameter | Meaning |
|---|---|
| `piston_component` | Stable ID of an explicit hydraulic piston |
| `closed_position`, `full_open_position` | Distinct positions in m or mm, both within the piston's nominal stroke |
| `coefficient` | Nonnegative full-open coefficient in `m3_s_sqrt_pa` |
| `transition_pressure` | Positive regularization pressure in Pa or bar |
| `reservoir_pressure` | Required gauge-pressure boundary when hydraulic B is omitted/zero |

Hydraulic A/B ports and an optional thermal sink follow the restriction contract.
The valve has no `input_channel` or `initial_input`; observe its `opening` channel
and command the actual actuator circuit. Mean flow/power and cumulative hydraulic
heat are observable. Units, referenced component type and stroke bounds produce
actionable validation errors. The ordinary whole-batch rollback, cancellation,
forking, integer clock and exact same-runtime replay contracts include all states
and histories. Existing physical model fingerprints remain unchanged.

Asset v15 adds a 32-byte metering-geometry record. JSON, CLI and MCP retain the same
definitions. `get_example_model("spool-regulated-pump")` demonstrates an electric pump,
mechanically governed bypass and scheduled pressure-clutch fill/drain. Its 200 N
static closing preload comes from a 200 kN/m return spring at 1 mm compression and
an explicit 1000 mm2 actuator area. The rotational drive/brake are 2 N*m; a three-second
experiment leaves enough time for the lower-pressure clutch to capture. Parameters
are synthetic and unverified.

## Evidence and performance

Checks cover signed metering travel, passive bidirectional flow, analytic pressure/
position derivatives, an independent steady pressure root, a separate three-state
RK4 transient, smooth second-order refinement, decreasing error through land opening,
finite-port equalization, swept volume, independent motion/fluid energy and complete
transactions. Warm stepping and snapshot reads allocate zero managed bytes. Land
slopes and Newton/LU buffers belong to each simulation; no new clock or worker is added.

See [VALIDATION.md](VALIDATION.md) for measured errors, runtime scope and elapsed time.
Studio valve/actuator views and import/Play tests are prepared in C# 9 source; actual
Unity Editor and Player evidence remains pending.
