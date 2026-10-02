# Agent 开发接口

`Power.Core`、`Power.Agent` 与 MCP 是同一套物理核心的不同入口。API 不绑定特定 GPT 版本或供应商。先用工具获取版本、能力和 Schema，再生成模型；不要根据名称猜组件已经实现。

The [finite gas network](GAS_NETWORK.md) is available through JSON, CLI and MCP, with gas composition, controlled restrictions, fixed reservoirs, wall heat links and conservation channels retained in portable assets. Existing linear and sealed-cylinder model semantics remain unchanged.

The [coupled clutch component](CLUTCH_NETWORK.md) is available through the shared
JSON, experiment and session contracts. It includes bounded engagement inputs, static
and sliding capacities, signed ratios, phase/heat outputs and transactional internal
events. The standalone [exact pair](CLUTCH_PHYSICS.md) remains a verification reference.

The [ideal gear/planetary components](GEAR_NETWORK.md) participate in the shared solver
and document contracts. `ideal_gear` has A/B ports and a signed nonzero ratio;
`planetary_gear` has sun/ring/carrier ports A/B/C and a ring/sun tooth ratio greater than
one. Compatible initial speeds and independent permanent constraints are required.
Capabilities describe rank policy, solver tolerances and mean reaction outputs.

The [hydraulic piston contract](HYDRAULIC_PISTON.md) adds `translational` nodes,
`linear_spring`, `hydraulic_piston`, `piston_clutch` and `force_source`. Agents can
observe displacement, velocity, pressure force, pad energy/force, clutch capacities
and cumulative damping heat. A piston clutch has no engagement input: command its
fill/drain valves and inspect pad contact. `get_capabilities.hydraulic_piston`
describes SI units, the volume/work convention, solver scope and negative-pressure
recovery. Model validation returns actionable unit, range and connection errors;
session revision, cancellation and independent-fork contracts apply unchanged.

`hydraulic_spool_valve` references a piston component and explicit closed/full-open
positions. Its opening follows actual motion; it accepts no opening command or
initial-input override. Flow, loss and opening are observable through the shared
model/session contract. `get_capabilities.hydraulic_spool_valve` declares the
position/flow units, simultaneous solve and omitted jet-force physics. Request
`spool-regulated-pump` to inspect mechanical pressure regulation; see
[the metering contract](HYDRAULIC_SPOOL.md).

`gas_piston` links a translational node to a moving gas chamber with explicit area,
reference volume/position, absolute reference pressure and signed compression
direction. Observe gas mass, energy, pressure, temperature, volume, force and
reference work. Combine it with a hydraulic piston on the same mass for an
accumulator; use explicit gas ports/heat links for transport. Validation checks one
volume owner and positive nominal gas volume. Capabilities declare the quarter-volume
interval limit; the contract states the wall-coupled accuracy boundary. Request `gas-accumulator-pump`;
see [the gas/fluid contract](GAS_PISTON.md).

`gas_fuel_injector` connects compatible finite tracked source/receiver gas volumes
and an explicit timing crank. Its input is requested kg per cycle; observe the
latched request, delivered cycle/total fuel and mean delivered flow. Mid-window
input changes apply to the next observed cycle. Backpressure/starvation can cause
underdelivery without an execution error; use output evidence and KPIs. Capabilities
declare timing, dose and scope boundaries. Request `metered-fired-cylinder`; see
[the metering contract](FUEL_METERING.md). This is gaseous admission, while liquid
spray, evaporation and calibrated fuel/ECU hardware remain open.

## 启动与客户端配置

```sh
dotnet run --file tools/Build.cs -- build
dotnet /absolute/path/to/Power!/src/Power.Mcp/bin/Release/net10.0/Power.Mcp.dll
```

通用 MCP 客户端配置示例，按客户端格式放入 server 配置；路径需要替换：

```json
{
  "mcpServers": {
    "power": {
      "command": "dotnet",
      "args": ["/absolute/path/to/Power!/src/Power.Mcp/bin/Release/net10.0/Power.Mcp.dll"]
    }
  }
}
```

Windows 同样使用 `dotnet` 和 DLL 的绝对路径。生产连接应直接运行构建好的 DLL，避免构建输出混入 stdio 协议。服务无需 Unity、凭据或网络；首次 NuGet 还原需要网络。协议传输与版本兼容由固定的官方 [MCP C# SDK](https://csharp.sdk.modelcontextprotocol.io/v2/concepts/getting-started.html)处理。

## 工具与结果

In agent API version 0.29.0, `get_example_model` accepts an optional `name`: `electrothermal` (default), `sealed-cylinder`, `gas-network`, `moving-cylinder`, `crank-timed-cylinder`, `fired-cylinder`, `fired-clutch`, `fired-planetary`, `fired-converter`, `fired-hydraulic`, `fired-pump`, `fired-pump-losses`, `electric-pump`, `pressure-regulated-pump`, `battery-regulated-pump`, `piston-actuated-clutch`, `spool-regulated-pump`, `gas-accumulator-pump`, `metered-fired-cylinder`, `film-fired-cylinder`, `liquid-injected-cylinder`, `needle-actuated-cylinder`, `closure-compensated-cylinder`, `dual-clutch-transmission`, `fired-dual-clutch`, `controlled-dual-clutch`, `controlled-fired-dual-clutch`, `ravigneaux-transmission`, `fired-ravigneaux-converter`, `resolved-ravigneaux-transmission`, `fired-resolved-ravigneaux-converter`, `hydraulic-ravigneaux-transmission` or `fired-hydraulic-ravigneaux`. `get_capabilities` advertises supported fidelity levels, readable asset versions, solver limits and input bounds. Exports use `power.asset.v24`; v1–v23 assets remain readable. Output channels and their units are returned by model validation and session creation. Passing laboratory KPIs does not establish a complete or calibrated powertrain.

| 工具 | 用途 |
|---|---|
| `get_capabilities` | 版本、模型能力、规模限制、时间语义与工作流 |
| `get_model_schema` | 完整 `power.model.v1` JSON Schema |
| `get_example_model` | 带事件和 KPI 的可编辑示例 |
| `validate_model` | 校验模型和实验，返回指纹、通道及诊断，不运行时序 |
| `run_experiment` | 完整实验、两个批大小的回放、KPI 与来源；默认紧凑结果 |
| `export_model_asset` | 校验并导出 `.powerasset`，返回 Base64 内容、文件摘要、来源和模型指纹 |
| `create_session` | 创建独立交互仿真，返回初始快照和通道信息 |
| `read_snapshot` | 当前时间、版本、哈希、可选择的输出通道 |
| `set_inputs` | 在当前时刻原子提交输入帧，增加会话版本 |
| `step_session` | 原子推进指定纳秒数，支持取消，增加会话版本 |
| `fork_session` | 复制当前物理状态，创建版本为 0 的新分支 |
| `close_session` | 释放指定会话 |

所有工具都有输入和输出 Schema；成功或领域错误均提供 `structuredContent` 与兼容文本结果。MCP `isError` 对应 `ok=false`。[SDK 结构化工具结果](https://csharp.sdk.modelcontextprotocol.io/v2/concepts/tools/tools.html)。

```json
{"schema":"power.agent.v1","ok":true,"data":{"revision":"2","time_ns":"1000000000","state_hash":"...","values":[]}}
```

```json
{"schema":"power.agent.v1","ok":false,"error":{"code":"revision_conflict","message":"Read the snapshot, then use its current revision.","retryable":true,"current_revision":"2"}}
```

参考 [模型 Schema](../schemas/power.model.v1.schema.json)、[响应 Schema](../schemas/power.agent.v1.schema.json)。模型 Schema 检查结构，编译器继续检查量纲、拓扑、正值、有限值和数值系统；实验校验继续检查 tick 对齐、事件顺序、通道和 KPI 上下界。

## 完整操作序列

1. 调用 `get_capabilities`，确认所需物理组件已支持。
2. 获取示例和 Schema，构造 `document` 对象；参数必须注明单位。
3. `validate_model({"document": ...})`，按 `error.object_id`、`error.field`、`error.code` 修复模型。
4. `run_experiment({"document": ...})`，检查 `data.passed`、`checks`、`replay`、`model.calibration`。`ok=true` 只说明实验完成，KPI 可能失败。
5. 用同一文档 `create_session`，保存 `session_id`、初始 `revision` 和通道映射。
6. 例如 `set_inputs({"session_id":"...","expected_revision":"0","values":[{"channel":"100","value":24}]})`，读取返回的新版本。
7. `step_session({"session_id":"...","expected_revision":"1","delta_ns":"1000000000"})`，取得 1 秒后的快照。
8. `fork_session({"session_id":"...","expected_revision":"2"})`，在子会话用 4 V 制动，保留父会话作为对照。
9. 完成比较后用各自最新版本 `close_session`。

需要在 Unity 中检查模型时，调用 `export_model_asset({"document": ..., "name": "My laboratory"})`。将 `data.content` 按 Base64 解码，核对完整文件的 `data.asset_sha256`，保存为 Unity `Assets` 下的 `.powerasset`，再通过资产 Inspector 的 **Open in Studio** 打开。此工具只返回内容，不写本地文件；导出成功只表示数据合法，KPI 与标定仍需单独检查。格式及限制见 [模型资产](ASSET_FORMAT.md)。

版本从 0 开始，每次成功输入提交和步进增加 1。过期、无效和取消的操作不增加版本。分支父会话版本保持不变。任何传输中断后先读快照确认版本，再决定后续操作；不要直接重发带旧版本的写命令。

会话快照的 `time_ns`、`revision`、通道 ID 都是字符串，避免超过 JavaScript 精确整数范围。模型文档中的实验时间限制在一小时以内；模型输入通道目前使用整数，建议选择不超过 `2^53-1` 的 ID 以保证经过其他 JSON 客户端时精确。输出的高位命名空间 ID 应原样保留为字符串。

`read_snapshot` 的 `channels` 是输出 ID 字符串数组，省略则返回所有输出；不接受输入通道和重复字段。`include_samples=true` 才会让实验返回所有采样边界。

## 错误与修复

| 错误 | 下一步 |
|---|---|
| `model_unit` / `model_connection` / `model_range` | 根据对象和字段修复单位、引用或参数 |
| `invalid_argument` / `invalid_json` | 修正字段、事件顺序、时间或文档结构 |
| `unknown_channel` / `invalid_input` | 从通道表选择输入，消除重复与非有限值 |
| `invalid_time_step` | 使用正的整数 tick，遵守单次一百万 tick 限制 |
| `numerical_failure` | 检查参数尺度、输入和步长；当前状态没有被修改 |
| `revision_conflict` | 先读最新快照，再基于真实状态决定操作 |
| `cancelled` | 整批回滚，可缩小计算批次后重试 |
| `session_capacity` | 关闭不再需要的会话 |
| `unknown_session` | 进程重启或会话已关闭，重新创建并回放 |

会话是进程内对象，尚不支持持久化恢复或连接正在运行的 Unity 场景。MCP 接口当前用于相同核心的无界面实验；以后连接 Unity 时仍需保留版本、时间和原子性契约。

## 作为开发 Agent

组件新增流程是：写清方程与适用范围 → 定义带单位端口/参数 → 在核心中实现 → 用解析解、守恒、步长收敛与故障测试取得证据 → 增加 Schema 和能力发现 → 提供可回放实验 → 接入 Unity 显示。实测来源及不确定度独立登记，不能由测试通过推导“已校准”。

## Gas-network workflow

Request `gas-network`, validate it, then run the experiment and export its asset using
the existing tools. `power.model.v1` gains additive gas node/component definitions;
clients should discover them from the schema and capabilities. No tool names change.
Gas volumes consume two scalar states each, and connected volumes must share R and gamma.

`gas_orifice` inputs use `fraction` values in [0, 1]. A missing or zero input channel
keeps the explicit `initial_input` fixed. Validation and export reject out-of-range
scheduled values before any experiment executes. Interactive rejection preserves both
state and revision. Compilation, successful execution, KPI success and calibration
remain distinct: the example is synthetic and `unverified`.

Gas-only session operations use the same nanosecond times, revision checks, cancellation,
filtered snapshots and independent forks. Sessions start from component initial inputs;
`create_session` does not execute the experiment's event schedule. Use `run_experiment`
or portable playback for that schedule. Static validation cannot guarantee a future
state remains numerically solvable: on `numerical_failure`, reduce `step_ns` and inspect
flow area, volume, conductance and initial conditions before recreating the session.

## Moving-cylinder workflow

`get_example_model({"name":"moving-cylinder"})` returns an uncalibrated motoring
experiment with two time-controlled restrictions, crank pressure work and wall transfer.
Gas nodes without `storage` must connect to exactly one `gas_cylinder`, whose parameters
supply the geometry. The compiler validates ownership and derives initial mass/energy
from the gas node's pressure/temperature and the crank's initial geometry.

Capabilities advertise `moving_cylinder_gas_exchange`, the 0.25-rad crank bound and the
split integration scope. Gas states remain channels on the gas node; volume, displacement
and torque are channels on the gas-cylinder component. The experiment, export, session,
revision and failure contracts are unchanged. See [moving cylinders](MOVING_CYLINDER.md).
Time-scheduled restrictions do not establish crank-angle valve timing or combustion.

## Crank-timed valve workflow

`get_example_model({"name":"crank-timed-cylinder"})` returns a 720-degree motoring
experiment with variable speed, intake/exhaust profiles and wall heat. `valve_timing`
on a `gas_orifice` requires a rotational `crank_node` and unit-bearing `cycle_angle`,
`open_angle` and `duration_angle`. The capability object advertises cycles, profile,
limits and recovery. See [the timing contract](VALVE_TIMING.md).

Timed input channels represent `peak_opening` in [0, 1]; the observable
`effective_opening` is derived from actual crank angle. Use KPI field `opening` to check
it. A stopped crank can remain open; reverse motion retraces the same profile. Phase
is explicit, independent of cylinder geometry phase. A scheduled peak change scales
the lobe; it does not replace crank timing.

Validation checks topology and parameters but does not guarantee runtime resolution.
On `numerical_failure`, reduce `step_ns` so angle travel and endpoint-speed travel stay
within `min(0.25 rad, duration_angle/8)`, then recreate the session. The entire failed
batch preserves inputs, state and revision. Asset v11 retains the profile and v1–v10
compatibility. The new fidelity is `crank_timed_gas_exchange`; successful execution,
passing KPIs and calibration remain distinct.

## Premixed-combustion workflow

`get_example_model({"name":"fired-cylinder"})` returns a premixed fired cylinder driving
an external load. The `combustion` capability declares the Wiebe prescription, fuel/air/
product classes, input range, forward-history behavior and numerical limits. Gas nodes
specify `gas.premixed`, and their reservoir restrictions specify explicit
`reservoir_fractions`. The compiler rejects missing fractions, incompatible connected
mixtures and multiple burn components on one chamber.

`premixed_combustion` connects a rotational `node_a` to a premixed-gas `node_b`, with
explicit cycle/start/duration angles, shape exponent and burn coefficient. Its optional
input channel scales the burn hazard through `burn_multiplier` in [0,1]. Zero disables
burning but does not stop fuel arriving at an open inlet. Forward angles beyond the
recorded frontier consume fuel; stopping/reversal/retracing cannot repeat heat release.

Discover constituent masses, chemical energy, cumulative fuel burned, heat released
and `burn_frontier_angle` from the channel table. Global fuel/fresh-air residuals supplement total mass and energy.
`reservoir_enthalpy` includes transported chemical energy for premixed gases, and
`net_fuel_energy_in` exposes that part separately. Gas internal energy remains thermal.
The report fidelities are `premixed_gas_transport` or `premixed_wiebe_combustion`; both
remain `unverified`.

On a burn-resolution failure, reduce `step_ns` and recreate the session. Enabled burning
requires crank travel and endpoint-speed travel no greater than
`min(0.25 rad, burn duration/32)`; heat per tick is limited to 25% of pre-burn thermal
energy. Whole-call rollback and revision contracts remain unchanged. A valid model can
still fail a runtime bound; successful execution can still fail KPIs. See
[PREMIXED_COMBUSTION.md](PREMIXED_COMBUSTION.md) for equations and limitations.

## Clutch workflow

`get_example_model({"name":"fired-clutch"})` returns a fired engine, separate load,
clutch and heat sink, with exact-tick engagement/release events. `clutch` capabilities
declare input bounds, solver budgets, mode codes and output-history semantics. Define
`parameters.static_capacity` and `sliding_capacity` in Nm, plus a signed nonzero `ratio`.
The compiler enforces `static >= sliding >= 0`, rotational endpoints and a thermal loss
sink. Ground brakes use omitted/zero `node_b` and ratio one.

The `engagement` input lies in `[0,1]`; zero disengages. Discover current relative slip,
last accepted phase, last-tick mean torque/heat power and cumulative friction heat from
the channel table. Phases are 0 disengaged, 1 locked, 2 positive slip and 3 negative slip.
Updating engagement does not rewrite the preceding tick's mean outputs or phase.
The fidelity `hybrid_clutch_powertrain` identifies models containing this component;
it does not imply a complete transmission or calibrated vehicle.

Use `run_experiment` to evaluate KPI and replay evidence, or session tools to vary
engagement while preserving revision checks and independent branches. On numerical
failure, reduce `step_ns` and inspect inertia/ratio scaling, redundant constraints and
capacity schedules. The failed/cancelled call commits no inputs, phases, heat or physical
state. Breakaway under changing loads uses interval-average demand; timestep refinement
is required near transitions. See [CLUTCH_NETWORK.md](CLUTCH_NETWORK.md).

## Ideal transmission workflow

Request `fired-planetary` to obtain a synthetic engine, ring brake, sun/ring clutch,
planetary set and final drive. The scheduled upshift/downshift uses the same exact-tick
semantics as other experiments, with 84 matching replay boundaries. `node_c` is the
planetary carrier; gears accept only their rotational ports and `parameters.ratio`.

`slip_speed` and `constraint_error` expose current speed and phase residuals. `torque`,
`torque_at_b` and planetary-only `torque_at_c` are mean reactions on the corresponding
rotors over the last complete tick. They start at zero and are not rewritten by boundary
input changes. Initial-speed failures return `model_connection` with field `initial_speed`;
dependent constraint rows return `model_solver` with field `gear.constraints`.
Correct topology or initial conditions rather than retrying unchanged data.

Asset v11 retains all prior readers, including an authentic v7 fired-clutch fixture.
This model establishes a synthetic transmission path, not complete DCT/AT, hydraulic
actuation, TCU behavior or measured calibration. Actual Unity evidence remains separate.

## Converter workflow

Request `fired-converter` for a synthetic engine, mapped fluid path, separate lockup,
planetary shift and thermal sink. Capabilities advertise all four required signed maps,
point/component limits, reference-member convention, nonlinear iteration budgets,
observable semantics and runtime recovery. The fidelity is
`quasisteady_converter_powertrain`; passing the 87 replay boundaries establishes
numerical consistency, not measured transmission performance.

`torque_converter` requires pump/turbine `node_a`/`node_b`, optionally `heat_node`, and
four explicit map arrays under `parameters`. Each point has dimensionless speed and
torque ratios and a coefficient in `nm_s2_rad2`. No map, reverse quadrant, input channel
or stator-rotor port is inferred. Compilation checks interpolation passivity and map
continuity, reporting `converter.<map>` or `converter.counter_rotation` with the object ID.

Discover mean pump/turbine/stator torques, fluid heat power, cumulative fluid heat,
current signed speed ratio and driver code from channels. A parallel `clutch` supplies
lockup engagement. The session revision, cancellation, branch independence and complete
rollback contracts also cover converter histories. On `numerical_failure`, reduce
`step_ns` and inspect map slopes, inertia/speed scales and clutch constraints. See
[the equations, bounds and evidence](CONVERTER_NETWORK.md). Exports use asset v11;
authentic prior fixtures preserve v1–v10 compatibility. Automatic hydraulic control and actual
Unity Editor/Player validation remain separate unfinished work.

## Hydraulic workflow

Request `fired-hydraulic` for valve-controlled pressure chambers operating shift and
lockup clutches. The `hydraulics` capability exposes gauge-pressure convention, storage
and flow models, units, iteration limits, pressure tolerance, actuator scope and recovery.
The fidelity is `compliant_hydraulic_powertrain`; calibration remains `unverified`.

A hydraulic node requires positive compliance `storage` in `m3_pa` and nonnegative
initial gauge pressure. `hydraulic_resistance` and `hydraulic_orifice` require explicit
flow coefficients and valve opening; an orifice additionally needs a positive transition
pressure. Reservoir endpoints require an explicit `reservoir_pressure`. A missing or
zero input channel fixes the supplied opening. The compiler never infers fluid properties,
leakage, reservoir pressure or an OEM map.

`hydraulic_clutch` has rotational ports and geometry under `parameters`, including its
hydraulic `pressure_node`. It has no engagement input. Discover pressure, stored reference
volume, hydraulic boundary work, inventory residual, restriction heat, clamp force and
current friction capacities alongside the existing clutch history channels. Valve input
changes preserve stored pressure and last-tick means until accepted stepping advances them.

The complete state, revision, cancellation and branch contracts cover hydraulic pressure
and ledgers. On numerical failure, reduce `step_ns` and inspect compliance, coefficients,
gauge pressures and actuator geometry. Negative final pressure rejects the entire batch;
it is not silently clamped. See [HYDRAULIC_NETWORK.md](HYDRAULIC_NETWORK.md). Asset v11
retains pressure boundaries, flow laws and actuator geometry; all v1–v10 readers remain.
Measured loss/control maps, measured valve/accumulator dynamics, full ECU/TCU control and actual Unity acceptance remain open.

## Pump supply workflow

Request `fired-pump` for a crank-driven pump, compliant line, relief and pressure-operated
transmission. Capabilities expose `hydraulic_pump`, displacement units, inlet convention,
joint-solver limits and signed work semantics. Pump `hydraulic_work` is internal
shaft-to-fluid transfer; global `hydraulic_work` remains external reservoir work.
This example has zero external hydraulic work and explicit initial stored pressure.

`hydraulic_pump` requires shaft/outlet ports, explicit `parameters.inlet_node`, positive
`displacement` in `m3_rad`, and a reservoir pressure only for inlet zero. The relief
requires conductance and cracking pressure, with no input channel. Missing or wrong-domain
ports, dimensions and irrelevant parameters produce actionable validation errors.
Asset v11 retains both definitions. Revisions, cancellation, forks, complete rollback and
KPI/calibration distinctions remain unchanged. See [HYDRAULIC_PUMP.md](HYDRAULIC_PUMP.md).

## Pump assembly workflow

Request `fired-pump-losses`, `electric-pump`, `pressure-regulated-pump` or `battery-regulated-pump`. The `pump_assembly` capability gives
the net-flow/reaction equations, loss units, component composition and electrical
supply boundary. Models contain ordinary pump, resistance and shaft records; the
electric example adds the existing RL motor. No new component kind, schema or asset
version is required. Core clients can use `HydraulicPumpAssembly.CreateComponents`
with their own stable IDs to produce the same graph definitions.

Leakage is an explicit outlet-to-inlet resistance with coefficient in `m3_s_pa`;
shaft friction is a grounded, zero-stiffness shaft with damping in `nm_s_rad`.
Both require supplied values and explicit heat routing. An electric pump accepts
motor voltage through a `v` input, with back EMF, current and copper heat in the
shared solve. It does not infer a battery, efficiency, viscosity, controller or
calibration. Discover channels rather than interpreting ideal pump branch flow as
net assembly delivery. Existing revisions, cancellation, forks and complete batch
rollback apply to the entire composition.

## Pressure feedback workflow

Request `pressure-regulated-pump`. Capabilities advertise the `pressure_controller`
component, dimensional gains, sensor/target requirements, integer sampling, clamping
and transaction semantics. Validate, run and export with the existing tools. Asset v12
retains the full controller definition and all previous readers remain supported.

The example's `105` input changes pressure setpoint in SI Pa. The motor's voltage
channel `100` is owned by the controller and absent from writable channels. Direct
writes return `controlled_input` with guidance to write `pressure_setpoint`; rejection
changes neither state nor revision. Negative pressure targets are rejected. Static
validation detects conflicting owners, wrong domains/units and misaligned sample periods.

Read `sampled_pressure`, `pressure_error`, `integral_voltage` and `command_voltage`
through the discoverable output IDs. These are last-sample state and held command.
Snapshot timestamps identify the clock phase. Input changes do not advance control
history; the next due sample updates it at a physical tick. Forks include integral
memory and clock phase. Cancellation or later arithmetic/solver failure commits no
part of the batch. Overflow recovery requires inspecting gains, targets and integral
scales, rather than retrying identical inputs blindly.

Successful execution and exact replay can accompany failed tracking KPIs when the
actuator saturates. Check `passed` and the error bounds separately from `ok`.
The example's ideal sensor and voltage source are research components; they do not
establish a battery, complete ECU/TCU, calibrated controls or Unity acceptance.

## Battery supply workflow

Request `battery-regulated-pump`. Capabilities expose finite charge, OCV/RC equations,
load and duty rules, control ownership and recovery. Battery node `storage` uses `c`
or `ah`, `initial` is SOC in `fraction`, and `position` is polarization voltage in `v`.
The battery record requires all five electrical parameters. Units, capacity/state
bounds, source ports, heat sinks, increasing OCV and controller periods are validated.

`battery_motor` requires a rotational A port, battery B port and duty input in [-1,1].
`resistive_load` has a battery A port, resistance and opening in [0,1]. The example's
`106` channel changes accessory load; `105` changes pressure setpoint in SI Pa.
Duty `100` is owned by `pressure_duty_controller` and cannot be written directly.
Its gains use `fraction_pa` and `fraction_pa_s`; output bounds are dimensionless.

Read `state_of_charge`, `charge`, `battery_current`, `terminal_voltage`,
`polarization_voltage`, battery stored energy and heat alongside `integral_duty`
and `command_duty`. Voltage/current/load-power channels are instantaneous algebraic
observables, so valid duty/load changes can alter them without changing stored states.
Battery work is internal; global `source_work` includes only explicit external power
boundaries. SOC/voltage violations reject the whole batch. Inspect initial charge,
capacity, duty, loads and batch length before retrying. There is no silent SOC clamp.

Asset v22 retains all supply/control parameters with authentic prior readers/fixtures.
Cancellation and later failure preserve charge, RC/control memory, inputs and revision.
Independent forks compare accessory/duty strategies from the same physical history.
All parameters remain unverified; an ideal averaged duty converter is not a battery
BMS, PWM/current loop, complete vehicle electrical system or calibration.

## Liquid film workflow

Request `film-fired-cylinder`. The `fuel_film` capability declares finite gas/wall
ports, phase-energy reference, units, split accuracy and scope. Supply explicit
initial liquid inventory, temperature, specific heat, saturation temperature,
latent internal energy and conductance. Validate and discover output IDs before
running or exporting the model. Films don't expose a writable input channel.

Read remaining `mass`, signed `internal_energy`, `chemical_energy`,
`evaporated_fuel_mass`, last-tick mean `mass_flow`, cumulative `film_wall_heat` and
instantaneous `heat_flow` alongside receiver fuel and reaction heat. Dry films
report the declared saturation temperature and zero heat flow. Actual vapor
availability governs reaction; a valid film definition doesn't imply evaporation
or passing heat-release KPIs.

Asset v18 retains phase quantities and earlier readers. Revision checks,
cancellation, independent forks and late-failure rollback include all liquid,
thermal, constituent and compensated histories. Wrong units/ports, superheated
initial liquid and excess state counts return structured errors. Inspect the
reported object/field and the finite heat budget before retrying a failed model.
The [film contract](FUEL_FILM.md) records the equations and accuracy boundary.
Initial wetting doesn't establish liquid injection, calibrated fuel properties,
complete engine control or actual Unity acceptance.

## Finite liquid injection workflow

Request `liquid-injected-cylinder`. The `liquid_fuel_injector` capability declares
the finite compliant source, `kg` cycle input, density/compliance units, energy
ledger and receiver boundary. Supply all rail quantities, nozzle geometry and an
existing film/crank reference. Validate first and discover output IDs/units.

The example's `104` input requests kg per cycle. Changes latch at a later observed
forward window; current delivery may remain limited by source pressure. Read rail
`mass`, `pressure`, stored `internal_energy`, chemical energy and volume beside
`requested_fuel_dose`, `delivered_fuel_dose`, `total_fuel_delivered` and last-tick mean
`mass_flow`. Film mass/temperature/evaporation and separate reaction heat identify
the delay between accepting a dose and actual vapor combustion.

Component `source_work` is released stored rail pressure work, `hydraulic_work` is
exported receiver pressure work, and `fluid_heat` is nozzle dissipation routed to
the film wall. Their identities are distinct from global external source work.
The negligible-liquid-volume receiver exports displacement work explicitly;
it doesn't add hidden crank work or model spray geometry.

Asset v19 retains complete source, nozzle and timing with v1-v18 readers. Revisions,
cancellation, forks and late/speculative failure include every rail/quota/heat
history. Wrong units, impossible compliant volume, superheated liquid and mismatched
film/crank ownership produce structured diagnostics. Inspect the failed object/field
and pressure/dose boundaries before retrying. Successful tool execution doesn't
imply full delivery, passing KPIs or calibrated hardware. See
[LIQUID_FUEL_INJECTION.md](LIQUID_FUEL_INJECTION.md).

## Physical needle workflow

Request `needle-actuated-cylinder`. Capabilities declare magnetic slope units,
flux energy, actual opening, sampled control and research limits. The injector's
`104` kg command is writable; driver-owned coil voltage `107` is not. Rejected
writes return `controlled_input` with the correct command name/channel and preserve
state/revision. Update requested fuel mass and advance exact physical ticks.

Read actual needle displacement/velocity and injector opening beside coil current,
magnetic energy, copper heat, electrical work, held voltage and last-sample target/
delivery. Fluid can continue after voltage is removed, the window closes or target
delivery is reached. Remaining liquid, gas fuel, unburned/boundary fuel and reaction
stay separately observable. A valid request or successful tool doesn't establish
exact dose delivery or calibrated control.

Asset v20 retains magnetic/stroke/needle/driver tables and v1-v19 readers. Sampling
periods must align to ticks; voltage has one owner; needle, coil and crank references
must match. For solver errors inspect positive `L(x)`, R/L/gradient, stroke travel
and timestep; refine physical/control intervals before claiming dynamic accuracy.
Cancellation, forks and rejected/speculative batches include all flux, thermal,
sampled/held and phase histories. See [NEEDLE_ACTUATION.md](NEEDLE_ACTUATION.md).

## Closure-compensated needle workflow

Request `closure-compensated-cylinder`. Its driver enables an aligned finite
`closure_prediction_ns` horizon. Capabilities give the 4096-tick limit, held-input
assumption and bounded cutoff search. Source kg requests remain writable; voltage
stays driver-owned. Discover predicted mass/count, cutoff latch and pending tick
channels beside actual needle position, delivery and held voltage.

Prediction is a separate full-state plant replay. It holds other commands and
doesn't know future external input events, so inspect actual post-closure delivery
and horizon/timestep refinement rather than treating forecast as measured fuel.
Failed/cancelled prediction commits no part of the real batch. Clock overflow,
invalid horizon or nonmonotone cutoff candidates require revising timing/model
assumptions; partial forecasts aren't silently accepted.

Asset v21 writes the horizon and retains earlier readers. Revisions, independent
forks and whole-batch rollback include the prediction latch and countdown. Core
clients can issue read-only `PredictNeedleClosure`; MCP snapshots expose the last
sampled selected-candidate estimate. Scope and evidence are in
[CLOSURE_PREDICTION.md](CLOSURE_PREDICTION.md).

## Dual-clutch power-path workflow

Request `dual-clutch-transmission` or `fired-dual-clutch`. Capabilities describe
the ordinary seven-forward/reverse graph, two input paths, three output branches
and research limits. Validate and discover every gear reaction, clutch slip/mode/
heat and rotor speed before changing selector/drive commands.

The examples use drive channels `500`/`501` and selector channels `600`-`607`
for forward 1-7 and reverse. Commands are fractions; ratios remain permanent
constraints. Preselect an unloaded path by releasing its prior selector and
engaging the target, then coordinate drive-clutch handoff separately. Core's
`DualClutchGraph.SelectPath` produces that path's atomic selector command set.
It doesn't implement TCU sensing, interlocks or actuator dynamics.

Snapshots expose all free/selected hubs, input/output speeds, synchronization and
drive heat, gear phase error and global source/energy/fuel evidence. Unsafe
combinations can bind or brake the physical transmission; a successful input
write doesn't establish a valid shift. Revision checks, cancellation, independent
forks and late failure preserve every state/history. Existing portable format
and prior readers are retained. See
[DUAL_CLUTCH_TRANSMISSION.md](DUAL_CLUTCH_TRANSMISSION.md).

## Sampled DCT control workflow

Request `controlled-dual-clutch` or `controlled-fired-dual-clutch`. Write an
integral `requested_gear` to channel `700`: 1-7 forward, -1 reverse, 0 neutral.
The controller owns drive `500`/`501` and selectors `600`-`607`; direct writes
return `controlled_input` with the correct requested-gear channel. Fractional
gears are invalid and don't alter state/revision.

Read confirmed actual gear, commanded selections, phase, target selector slip
and fault. Requested gear doesn't imply completed shift. The state machine
preselects unloaded paths, confirms physical lock, uses staged torque-interrupted
handoff and exposes timeout/direction/persistent-lock faults. Neutral aborts on a
due sample; another target can recover a fault. A transient slip can report
unconfirmed actual gear while the controller monitors its duration.

The explicit reported-state limit is 128, with 32 nodes/64 components unchanged.
Actual fired/controller composition and near/over-limit checks are verified;
Standard checks still run on .NET 10 and aren't Unity evidence. Asset v22 retains
immutable routes and timed state with prior readers. Cancellation, forks, late
failure and compensated coordinate history remain whole-batch transactions.
Full ECU torque blending, actuators and calibration remain separate requirements.
See [DCT_CONTROL.md](DCT_CONTROL.md).

## Compound planetary paths

`double_pinion_planetary_gear` requires sun/ring/carrier ports A/B/C and ratio
`k > 1`. Its constraint is `sun - k ring + (k-1) carrier = 0`. Existing
`planetary_gear` retains its single-pinion sign. Both expose speed/phase residuals
and all three reaction torques. Incompatible initial speeds, wrong domains,
redundant rows and incomplete carriers return actionable compile errors.

Request `ravigneaux-transmission` or `fired-ravigneaux-converter` for explicit
five-element research schedules, converter/lockup integration and complete
physical replay. Engagement inputs are fractions; a successful command does not
prove a locked range. No AT controller owns these prescribed inputs. Asset v23
retains topology and reads v1-v22. See [RAVIGNEAUX_TRANSMISSION.md](RAVIGNEAUX_TRANSMISSION.md).

## Carrier-relative meshes and internal planet dynamics

`carrier_gear` requires distinct rotational A/B/C ports, finite nonzero signed
ratio and compatible initial speeds. The constraint is
`A - ratio B + (ratio-1) C = 0`; negative external and positive internal ratios,
including one, are supported. C is an actual moving carrier with its own reaction
torque, not an implicit ground. Channels expose all three mean torques and
speed/phase residuals. Zero ratios, missing carriers, wrong domains and dependent
constraints return typed compile errors.

Request `resolved-ravigneaux-transmission` or
`fired-resolved-ravigneaux-converter`. Both retain four physical meshes, two
absolute planet-spin states and declared orbital inertia in the carrier.
Plain rotor storage includes their actual kinetic energies; inputs remain
prescribed engagement fractions, not full AT control. The flat graph records
aggregate inertias and ratios, while source descriptions retain the declared
geometry/masses that generated them. Asset v24 includes this primitive and
reads v1-v23. See [RESOLVED_PLANETS.md](RESOLVED_PLANETS.md).

## Pump-fed AT piston actuation

Request `hydraulic-ravigneaux-transmission` or `fired-hydraulic-ravigneaux`.
Use explicit fill/drain fractions on 700/701 through 708/709; the fired lockup
uses 710/711. Previous range engagement IDs are absent. Validate/discover channels
before writing. Piston pressure/travel/contact determine capacities; a command
accepted by the API does not confirm physical lock.

Reports retain line/chamber pressure, travel, contact capacity, pump work, swept
volume, friction/restriction/damping heat and every model hash. Full revisions,
cancellation, late rollback and independent valve-release forks use the ordinary
contracts. The graph uses existing asset v24 records, not a new serialization
format. See [AT_HYDRAULIC_ACTUATION.md](AT_HYDRAULIC_ACTUATION.md).
