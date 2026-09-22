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

As of version 0.13.0, `get_example_model` accepts an optional `name`: `electrothermal` (default), `sealed-cylinder`, `gas-network`, `moving-cylinder`, `crank-timed-cylinder`, `fired-cylinder`, `fired-clutch`, `fired-planetary`, `fired-converter` `fired-hydraulic` or `fired-pump`. `get_capabilities` advertises supported fidelity levels, readable asset versions, solver limits and input bounds. Exports use `power.asset.v11`; v1–v10 assets remain readable. Output channels and their units are returned by model validation and session creation. Passing laboratory KPIs does not establish a complete or calibrated powertrain.

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
Pump losses/control, piston dynamics, full ECU/TCU control and actual Unity acceptance are separate work.

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
