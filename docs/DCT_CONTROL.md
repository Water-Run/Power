# Sampled dual-clutch synchronization and staged handoff

`dct_controller` owns the two drive-clutch and eight selector channels of a
[seven-forward/reverse research graph](DUAL_CLUTCH_TRANSMISSION.md). Its integer
requested-gear command is separate from confirmed actual gear, selected paths,
shift phase, measured synchronization error and controller fault.

This is a sensor-driven research state machine with an explicit torque
interruption. It doesn't establish full TCU/ECU torque blending, detailed dog/
baulk-ring or clutch actuator behavior, a calibrated shift strategy or complete
vehicle fault management.

## States and physical confirmation

| Phase | Held command policy and transition |
|---|---|
| Neutral | Both drive clutches and selectors released |
| Preparing | Target opposite path is unloaded and preselected while the preceding drive remains engaged |
| Releasing | Preceding drive opening ramps down; the target drive remains released |
| Synchronizing | Target selector ramps to full opening with both drives released; wait for measured slip and physical lock |
| Engaging | Target drive ramps up; the other drive remains released |
| Driving | Confirmed target drive/selector; adjacent unloaded-path preselection is allowed |
| Fault | Both drives and every selector released; retain fault until neutral or a different request |

The target is latched while a handoff is in progress. Later non-neutral requests
are processed after that handoff; neutral aborts on a due sample. Same-input-path
changes release its drive before changing selectors. Opposite-input-path changes
can prepare the unloaded target before drive release. Each path commands at most
one selector, and no commanded drive-clutch overlap is used.

Selector opening ramps use the configured engagement duration. A selector is
ready only after full command, measured slip within the supplied tolerance and
physical `Locked` mode. The drive is confirmed only after full engagement command,
small drive slip and physical lock. Command acceptance doesn't announce an
instantaneous ratio or physical gear completion.

Inactive preselection can briefly disturb a confirmed path. Snapshot actual gear
is zero while the drive or selected path isn't physically locked. Persistent
loss is timed separately; the controller doesn't confuse one transient sample
with a sustained fault. That timer resets on phase changes and recovery.

## Requested gear, direction and faults

Requested gear is an integer in `[-1,7]`, with zero neutral and -1 reverse. Static,
immediate and scheduled input validation reject fractions. The source command
uses explicit `state_code` units; no ordinary clutch fraction is interpreted as
a gear number.

Synchronization timeout returns an observable unloaded fault. A reverse request
against positive vehicle motion above the supplied speed limit, or a forward
request against negative motion, is blocked as a direction-change fault. Sustained
loss of a confirmed drive/selector lock uses the same supplied timeout and a
distinct fault code. These controller outcomes are physical policy states, not
numerical failures or implicit successful shift KPIs.

| Fault code | Meaning |
|---:|---|
| 0 | No fault |
| 1 | Synchronization/engagement timeout |
| 2 | Direction-change request blocked by vehicle motion |
| 3 | Persistent loss of confirmed lock |

Neutral clears the fault and releases the train. A different valid request can
start a new attempt; repeatedly submitting the same failed target doesn't reset
the timeout on every sample. Higher-level fault decisions, plausibility checks,
sensor failures and driver/vehicle safety functions remain separate work.

## Definition and ownership

The controller declares engine A node, vehicle node, odd/even drive-clutch IDs,
eight selectors in forward 1-7/reverse order and its requested-gear input. All ten
actuator channels must be distinct, initially released and have one owner. The
compiler verifies ordinary clutch types, shaft/hub/final-drive topology, odd/even
assignment, the reverse idler path and stable references. Selector lists are
copied into immutable definition/compiled data.

Explicit timing consists of sample, release, engagement and synchronization
timeout nanoseconds. Sampling aligns to physical ticks; other times are positive
sample multiples and at most ten seconds. Synchronization tolerance and direction
speed limit use `rad_s` or `rpm`. No OEM values, actuator maps or loss curves are
silently supplied.

Agents write requested gear. Direct drive/selector writes return `controlled_input`
with the correct command name/channel and leave revision/state unchanged. Reads
expose live request, confirmed gear, commanded odd/even selections, phase,
target selector slip and fault. These state codes and physical channels retain
their different semantics.

## Integer clocks and complete transactions

Samples run on bounded integer simulation time. Input writes don't advance
control memory. Held fractions are applied to the normal physical solver; inertia,
gear reactions, synchronization and drive heat stay in the existing ledgers.
Controller state contains latched/active gear, selections, phase/fault, phase
clock, measured error and persistent-lock timer. Forks, cancellation, late failed
batches and speculative intervals copy/hash/rollback that memory and every held
command together. Successful stepping and snapshots allocate no managed memory.

Long controlled gear runs use compensated coordinate increments from midpoint
velocity. Their compensation is transactional and hashed; strict phase tolerances
remain unchanged. This resolves accumulated roundoff exposed by the new long
loaded synchronization scenario. Earlier model paths retain preceding integration
and replay behavior.

The reported state limit is explicitly **128**, with 32 nodes and 64 components
unchanged. This allows the complete research fired/DCT/controller composition,
which exceeds the previous 64-state bound. Exact-boundary compilation/stepping
and over-limit physical/controller models are checked; builds/tests remain serial.

## Portable and shared experiments

Asset v22 adds one typed 104-byte route/timing/tolerance record per DCT controller.
It retains v1-v21 readers, stable preceding IDs and bounded count/length, digest,
typed ownership, units and physical compilation checks. Controller models add
fingerprint tag 26. Requested/actual/selection/phase/error/fault fields are appended
without changing preceding IDs. An authentic v21 graph fixture retains its digest
and same-runtime upgraded replay.

`controlled-dual-clutch` issues gear requests through all seven paths and selected
downshifts. It observes the final handoff and inactive preselection through physical
completion rather than assuming a nominal time. `controlled-fired-dual-clutch`
combines the same sampled policy with open-cylinder premixed combustion.
JSON, CLI, portable replay and an actual MCP child server share the definitions.

[VALIDATION.md](VALIDATION.md) records state/interlock, fault/recovery, ownership,
integer input, immutable route, capacity, long phase preservation, conservation
and complete replay evidence. All parameters remain `unverified`. Torque blending,
full actuator/sensor/ECU behavior, complete AT, actual Unity and calibrated target
powertrains remain unfinished.
