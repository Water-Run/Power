# Bounded needle closure prediction and tick-grid cutoff

**English** · [简体中文](CLOSURE_PREDICTION.zh-CN.md) · [Français](CLOSURE_PREDICTION.fr.md) · [Русский](CLOSURE_PREDICTION.ru.md) · [日本語](CLOSURE_PREDICTION.ja.md) · [한국어](CLOSURE_PREDICTION.ko.md) · [Deutsch](CLOSURE_PREDICTION.de.md) · [Español](CLOSURE_PREDICTION.es.md) · [Italiano](CLOSURE_PREDICTION.it.md) · [Português](CLOSURE_PREDICTION.pt-BR.md)

The [physical needle driver](NEEDLE_ACTUATION.md) can compensate for fuel delivered
after its voltage command ends. Optional `closure_prediction_ns` enables a separate
preallocated full-plant replay. Real needle lift, current decay, pressure work,
seat rebound and fuel inventory remain physical; prediction changes the command
timing rather than clipping actual delivered mass.

## Prediction contract

At a due sample, the predictor copies the complete current state. It replays the
configured physical ticks with its coil at zero voltage, or with a bounded period
of drive voltage before cutoff. All other actuator commands are held. Sampled
controllers don't recurse or change commands inside this forecast, and future
external input events aren't anticipated. Gas exchange, crank/cylinder behavior,
combustion, hydraulic/electrical supply, contacts and accepted clutch intervals
continue through the normal plant equations.

Forecasted additional mass is the increase of that injector's total delivery.
The scratch state never commits to the real simulation. Every candidate starts
from the same complete source state; real time, controller memory and physical
histories remain untouched. Simulation-owned solver workspace is prepared again
for the real interval. `PredictNeedleClosure(driver_id, out estimate)` exposes a
read-only zero-voltage prediction for Core clients, with cancellation and status.

The horizon is an integer multiple of physical ticks, covers at least two driver
sample periods and is limited to **4096 physical ticks**. Zero keeps the preceding
on/off driver behavior. Prediction cannot wrap the bounded integer clock. Failed
or cancelled forecasts reject the entire real batch; a partial forecast isn't
silently treated as a valid estimate.

## Scheduled closing decision

The driver compares current cycle delivery plus forecast closing fuel with the
latched request. If zero-voltage closure already reaches the target it cuts now.
Otherwise it also forecasts holding drive until the next sample. If those two
candidates bracket the target, a bounded integer bisection finds neighboring
physical-tick cutoff candidates and chooses the nearer projected final mass.

The selected deadline is a countdown of physical ticks. It can remove voltage
before the next controller sample. The cutoff decision latches for the observed
cycle, avoiding repeated reopening on tiny prediction differences. A new observed
cycle resets that latch. Window/reversal shutdown can cancel a pending deadline.
Actual fuel remains governed by the moving needle throughout closing and rebound.

The local candidate bracket must be monotone within the declared numerical
tolerance. A violated bracket returns numerical failure with model/session state
unchanged; inspect voltage, mechanics, sampling and prediction assumptions rather
than accepting an invalid cutoff. Each candidate is limited to 4096 ticks and the
integer bisection has at most twelve interior queries plus endpoint forecasts.

This is model-based on/off timing, not predictive combustion, calibrated ECU
control, robust fault management or a measured injector map. Holding other
commands and omitting future external events are explicit forecast assumptions.
Changes to future load, pressure or controller action can change actual delivery.

## Horizon and physical accuracy

A finite prediction must include relevant closing/rebound fuel. In the isolated
research actuator, 8-ms prediction truncates a material late tail; 20/30-ms
prediction gives the same tick-grid decision. The horizon study is retained as
evidence instead of treating an arbitrary short forecast as complete closure.

Physical timestep, controller sample period and forecast horizon are separate
accuracy controls. A longer horizon doesn't repair coarse electrical/contact
integration or an inaccurate constitutive model. Forecast/actual equality under
the same held-input model verifies implementation, not OEM calibration. Analytic,
independent ODE, conservation and event checks in the underlying plant still apply.

## Observables and transactions

Prediction-enabled driver outputs include:

- `predicted_fuel_mass`: additional fuel for the selected closing candidate, kg.
- `prediction_ticks`: the configured physical replay count.
- `driver_state`: whether cutoff has been latched for the observed cycle.
- `closing_delay_ticks`: remaining physical ticks before scheduled voltage removal.

Held voltage and last-sample target/delivery remain available. The predicted
amount includes any planned drive delay, whereas the public Core read-only query
always predicts immediate zero-voltage closure. These quantities aren't actual
fuel transfers and don't enter mass, chemical or energy ledgers.

Five additional reported state entries per driver retain prediction mass/count,
cutoff latch/cycle and countdown when prediction is enabled. The separate replay
state is allocated once per simulation. Reads, successful active stepping and
snapshots allocate no managed memory after warmup. Cancellation, revisions,
independent forks, speculative clutch capture and late numerical failure preserve
all prediction/control and physical histories. Disabled prediction retains the
preceding fingerprints and hashes.

## Shared definitions and evidence

JSON accepts optional `needle_driver.parameters.closure_prediction_ns`. Core uses
`NeedleDriverDefinition.ClosurePredictionNanoseconds`. Capabilities declare bounds,
hold assumptions and observables; `closure-compensated-cylinder` is the shared
example. Source `kg` requests remain writable and coil voltage stays driver-owned.
Successful requests are distinct from actual dose tracking and passing KPIs.

Asset v21 keeps the existing count table and extends each driver record from
32 to 40 bytes with a horizon uint64. Prior readers default to disabled prediction;
an authentic v20 fixture retains its fingerprint and same-runtime replay. Enabled
prediction adds fingerprint tag 25 and the configured horizon. Bounded counts,
units, horizon/alignment, controller ownership and downgrade rejection are checked.

The isolated 8-mg request, full fired laboratory, horizon study, immutable models,
read-only forecast/independent manual closure, complete replay, zero allocations
and cancelled/failed batches are verified in [VALIDATION.md](VALIDATION.md).
Full engine/transmission/control, measured physical/actuation maps, rail refill,
actual Unity and calibrated vehicle acceptance remain unfinished.
