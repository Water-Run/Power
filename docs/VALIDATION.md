# 验证记录

## 2026-09-08: sealed-cylinder increment

Serial `tools/Build.cs verify` passed on Linux x64 with SDK 10.0.400 and runtime 10.0.11: **38/38 managed checks, 26/26 checks against the actual .NET Standard 2.1 assemblies, and 6/6 MCP process integration groups**. Release compilation reported zero warnings and errors. The log is `artifacts/reports/cylinder-verify.log`; all three laboratory JSON documents also passed the published JSON Schema using the local `jsonschema` validator.

The new checks cover analytic slider-crank geometry and derivatives, ideal-gas state identities, two-second conservation runs with and without back pressure, second-order convergence under step refinement, reverse rotation, tiny steps and dead centers, shared/coupled cranks with electrical and thermal components, nonlinear failure rollback, cancellation, forks, units, malformed cylinder extensions, and zero managed allocations in steady-state stepping/snapshots. A preserved v1 fixture decodes, retains its original linear fingerprint and replays identically after v2 export.

GitHub Actions repeated the same verification successfully on **Windows, macOS and Linux**, with 38/38, 26/26 and 6/6 checks and zero warnings/errors on every platform. Evidence: [run 34176008291](https://github.com/Water-Run/Power/actions/runs/34176008291), code commit [`6209df2`](https://github.com/Water-Run/Power/commit/6209df22876f9928ddc04a1a32845cd7efe087da). Complete job logs and status metadata are retained locally as `artifacts/reports/github-actions-34176008291.log` and `.json`. The subsequent documentation update changes no executable code.

The synthetic cylinder experiment passed its final KPIs and replayed exactly at 21 boundaries through JSON, asset playback and an actual MCP child server. Linux results: fingerprint `c64b61efdb827680`, final speed `153.00340249454544 rad/s`, pressure `118835.36885412445 Pa`, temperature `315.16234058802814 K`, final energy residual `8.7464e-10 J`, and maximum sampled absolute residual `8.9570e-10 J`. Full report: `artifacts/reports/sealed-cylinder.json`. These values establish numerical evidence for this sealed ideal-gas benchmark, not engine calibration.

Unity importer and Play tests now include the cylinder asset and schematic piston motion, but were **not executed**. Unity Editor, Mono, rendering and IL2CPP evidence remains pending. The active stack remains C#/Unity; the future Zig rewrite direction introduces no native runtime in this increment.

## Pause checkpoint: 2026-09-08

The owner requested wrap-up and a development pause after the cylinder increment. Executable source remains at verified code commit `6209df2`; the later commits update documentation only. The prospective gas-exchange extension was not applied, built or published. Its [resume notes](NEXT_ENGINE_STEP.md) distinguish proposed work from implemented capabilities. No further build was needed for this documentation-only checkpoint. Resume development only after an explicit owner instruction.

## Historical baseline: 2026-09-07


环境：2026-09-07，Linux x64，.NET SDK 10.0.400，运行时 .NET 10.0.11。实际执行结果以 `tools/Build.cs verify` 输出和生成报告为准。

当前托管基线：30/30 核心、资产与 Agent 检查，19/19 标准库程序集检查，5/5 MCP 进程联调组通过；Release 构建为 0 警告、0 错误。实际 MCP 进程完成 12 个工具发现和输入/输出 Schema 检查，成功与错误响应均检查必填输出字段和文本兼容结果。原始执行日志保存于 `artifacts/reports/managed-verification.log`。

## 已取得的证据

- 核心和资产层已同时编译为 `net10.0` 和 `netstandard2.1`。
- 解析解检查覆盖恒定扭矩、RL 响应、热平衡；步长减半检查机械二阶与热一阶收敛。
- 正负传动比检查广义动量、阻尼发热和守恒；回馈制动检查负电流与源功减少。
- 输入拒绝、后续 tick 溢出、预取消、缓冲区容量检查都验证状态/调用者数据不被部分修改。
- 模型描述所有权、并行独立实例、完整状态分支和逐 tick/批量推进一致性有执行检查。
- 使用 .NET 线程分配计数器测得核心热路径的输入、步进和快照合计 0 托管分配；这不包含编译、报告或 Unity UI。
- 资产编码往返保留来源、模型与事件；损坏摘要、伪造计数、格式版本、模型指纹和额外字节均被拒绝。
- 调度输入覆盖零时刻、批次终点和呈现批次内部的事件，后续数值失败整批回滚。资产回放在每个 tick 均有输入变更时测得 0 托管分配；取消保留事件游标。
- JSON 报告与导入资产在所有报告边界比较状态哈希和输出值，包括不落在 20 ms 呈现边界的事件。
- 同一组物理检查直接加载实际复制给 Unity 的 .NET Standard 2.1 DLL，并核实其目标框架。执行宿主仍为 .NET 10，不能据此声称 Mono/IL2CPP 已通过。
- Agent 检查覆盖结构化字段诊断、过滤快照、会话限制、并发版本冲突、取消、父子分支隔离、生命周期与紧凑报告。
- 官方 MCP 客户端启动实际服务子进程，完成 12 工具发现、输入/输出 Schema、错误恢复、会话操作、完整实验和资产导出。Base64 解码后校验文件摘要，并比较导入回放与 MCP 实验终态。

默认电热实验推进 10 秒，在 5 秒降至 4 V，6 秒恢复 24 V。两个批大小在 11 个边界逐位一致。典型终值约为：电机 `29.74182442 rad/s`、负载 `9.91394147 rad/s`、电机温度 `302.4760663 K`。能量残差门槛为 `1e-5 J`。回放哈希只在相同二进制、运行时与架构范围内比较；跨运行时数值使用容差。

热交换实验使用节点 42/77、无外部输入和 7 ms 步长，推进 7 秒；两个批大小在 11 个边界一致。终温与后向 Euler 离散解相差小于 `1e-9 K`，与连续解析解相差小于 `0.004 K`，总能量误差小于 `1e-7 J`。两份报告分别为 `artifacts/reports/electrothermal.json` 和 `thermal-network.json`。

## 重现

```sh
dotnet run --file tools/Build.cs -- verify
```

这里的检查是会在 Release 执行断言的控制台验收程序，并非依赖 `Debug.Assert` 的空测试。它们不需要 Unity、Python 或原 C 库。MCP 项目使用官方 NuGet 包，`packages.lock.json` 固定解析结果。

GitHub Actions 已在 Windows、macOS、Linux 上完成同一组托管验收：各平台均为 30/30、19/19、5/5。证据对应代码提交 [`aea6136`](https://github.com/Water-Run/Power/commit/aea6136bbdcbeaea91d63836d947637e7eac730e) 和 [运行 34087686661](https://github.com/Water-Run/Power/actions/runs/34087686661)。本地保存了 `artifacts/reports/github-actions-34087686661.log` 与 `.json`，包含实际作业输出和终态；另外从不含缓存及生成程序集的干净源码副本完成了一轮本地验收，日志为 `github-clean-checkout.log`。

The repository and CI evidence links are public. Development resumed on 2026-09-08; the earlier records below identify their own verified baselines.

The GPL publication update added license notices without changing executable source content; a comparison against the preceding commit confirmed all 90 source/build edits were notice-only. A fresh serial verification passed 30/30 managed checks, 19/19 Unity-facing assembly checks, and 5/5 MCP integration groups, with zero build warnings or errors. Its log is `artifacts/reports/license-verification.log`. This does not add Unity Editor or Player validation evidence.

## 尚未取得的证据

当前环境没有安装 Unity Editor。本次没有运行编辑器导入、EditMode/PlayMode 测试、场景画面检查或 IL2CPP 构建。对应项目、场景、测试与自动化入口已提供：

```sh
dotnet run --file tools/Build.cs -- unity-test
```

先设置 `POWER_UNITY_EDITOR`。Unity 日志与 XML 结果输出至 `artifacts/unity`。Play 测试需要能够运行图形编辑器的环境和有效 Unity 许可。已写但未运行的测试涵盖：URP/程序集及两种模型资产导入、回放一致性、连续启停无残留、10 秒参考实验、运行中切换纯热拓扑、动态节点和输入列表、7 ms tick 调度。还需人工检查控件、主题、不同窗口大小、桌面平台显示，并完成三平台 Player 构建。

发布入口为 `Power.Studio.Editor.ProjectSetup.BuildPlayer`，使用所选桌面目标和 IL2CPP。需要相应 Unity 平台构建模块；目前没有已构建或已测试的 Player 包。

所有当前参数均为合成实验参数。完整发动机/变速器功能、实车标定、排放/声学、实时预算与长时运行仍需后续实现及验证，不能由这些检查推导完成。
