# Power! 开发路线

用户目标是实现完整的 Power!，主线使用现代 C# 和 Unity 3D，并让核心可由 Agent 直接操作。当前电热实验是迁移基座，不能代表整个目标已经完成。

**2026-09-11: Native migration resumed and completed in Zig.** The 2026-09-08 pause ended for this migration at the owner’s request. All native implementations, tests and hosts are now Zig, with original source provenance retained in Git. See the [native boundary and validation](NATIVE_ZIG.md). [Water-Run/Power](https://github.com/Water-Run/Power) is now public. The C# baseline including sealed-cylinder physics passed [remote verification](https://github.com/Water-Run/Power/actions/runs/34176008291) on Windows, macOS, and Linux. Unity Editor validation and the remaining work below are pending.

| 阶段 | 交付与验收 | 当前状态 |
|---|---|---|
| 1. 托管基座 | 双目标核心、组合拓扑、固定时间、单位检查、回放、故障回滚、可验证实验 | 已实现并通过 Windows/macOS/Linux 托管验收 |
| 2. Agent 接口 | Schema、结构化诊断、MCP、状态分支、版本冲突、取消、紧凑证据 | 已实现并完成实际 MCP 进程联调 |
| 3. Unity 工作室 | 真实导入、Play 生命周期、三维实验、输入/UI、桌面 Player | 工程与测试已写；编辑器与 Player 待验收 |
| 4. 通用建模工作台 | Unity 加载相同模型资产、图编辑、通道配置、保存与实验回放 | 资产编码、CLI/MCP 导出与回放已通过托管验证；Unity 通用加载代码待实测，图编辑/保存待实现 |
| 5. 发动机物理 | 曲柄连杆、容积与质量/能量、进排气、燃烧、泵气、壁面传热、气缸循环 | C# sealed adiabatic cylinder and slider-crank implemented; full engine cycle remains open |
| 6. 传动系统 | 离合器、DCT/AT 拓扑、齿轮/行星排、变矩器、液压、热与混合事件 | Native prototypes ported to Zig; managed transmission migration remains open |
| 7. 控制与整机 | ECU/TCU 周期、传感器/执行器、扭矩协调、电源、附件、故障闭环 | 尚未完成 |
| 8. 证据与发布 | 两套完整动力总成、实测标定、参数来源、误差预算、长时稳定、桌面包 | 研究资料已保留，发布条件未满足 |

For the next managed engine increment, follow the [next engine-step notes](NEXT_ENGINE_STEP.md). Planned sequence: establish conservative nonlinear engine components while actual Unity verification awaits an Editor environment; then add gas exchange, combustion and wall heat transfer, followed by transmission and control components with appropriate solver contracts. Complete Unity import/Play validation, selectable plots, graph editing and saving as separate deliverables. The current plot still shows the first two rotor speeds; all other channels are available in the output list. CLI, MCP and Unity must continue to consume the same model semantics.

The first engine increment is the [sealed-cylinder foundation](SEALED_CYLINDER.md). It covers geometry, trapped ideal gas, crank pressure work and a bounded nonlinear solve. It does not complete the engine milestone. The archived native engine, exhaust, transmission, control and integration prototypes have now been ported to **Zig** under the defined native boundary. This language migration does not complete the managed engine, vehicle calibration or full powertrain milestones. Original C source is available in Git at `c342d4c`; the current tree contains Zig replacements.

未来 Agent 的强建模能力应投入到拓扑构造、组件方程、实验设计、参数识别和证据分析。先完善可观察、可分支、可修复的接口，再根据实际基准引入工作线程、稀疏分解或 Burst 等优化；不把模型规模和可维护性提前锁死在显示层。

EA211 DJS + DQ200 和 PSA EC5 + AT8 必须继续遵守原研究样例的完整总成边界、车型适用性与证据状态。没有实测支持的参数保留 `unverified`；功能完成、数值正确和实车可信是三个独立的验收维度。
