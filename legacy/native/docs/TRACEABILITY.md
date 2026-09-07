# 需求追踪与规划完成审计

审计日期：2026-09-07（模型运行时增量）；上游测绘记录仍为 2026-08-27 基线。

本文件追踪“拉取参考项目、完成测绘和基础计划”以及随后“按完整动力总成边界开始开发”的可核查交付物。它区分已运行的原型、尚未完成的产品能力和没有实车标定支撑的命名样例。

## 1. 需求追踪矩阵

| 用户要求 | 规划证据 | 后续实现验收 | 当前规划状态 |
|---|---|---|---|
| 项目重命名为 Power! | [命名约定](NAMING.md) 区分展示名与 `pwr_`/`power` 技术标识；README、架构、路线图、验证和参考边界已同步 | CI 扫描旧符号/扩展名；首次公开发布前完成商标与发布坐标检查 | 已覆盖 |
| 拉取经典 `engine-sim` | [参考锁定](../reference/README.md) 记录两个上游、精确 SHA、子模块和复现命令；本地 checkout 与记录一致 | 不适用；参考仓库不进入产品构建 | 已覆盖 |
| 测绘原版 | [源码测绘](UPSTREAM_SURVEY.md) 覆盖规模、配置、机械、气体/燃烧、步进、音频、显示、平台、测试和许可边界 | 上游基线变化时重新运行清单和差异检查 | 已覆盖 |
| 完整重写，只参考思想 | [目标架构（§1）](ARCHITECTURE.md) 与 [参考许可边界](../reference/README.md) 明确不移植、不兼容、不链接旧子模块 | 贡献指南要求所有例外复制登记来源；发布 BOM 不含旧构建依赖 | 已覆盖 |
| 技术栈改为最新 C + Lua 5.5 | [目标架构（§4–5）](ARCHITECTURE.md) 规定 ISO C23、严格 `-std=c23`、CMake、Lua 5.5.1、私有 VM、符号隔离和沙箱 | GCC/Clang 双编译器 CI、C23 feature probe、Lua 限额负测、宿主 Lua 冲突测试 | 已覆盖 |
| 发动机由 Lua 定义 | [Lua DSL（§6）](ARCHITECTURE.md) 给出量纲单位、发动机示例、完整动力系统组合和热重载语义 | `.power.lua` 编译成 C arena；销毁 Model VM 后可持续运行 | 已覆盖 |
| 电子控制达到动态仿真级别 | [电子控制架构（§11.5）](ARCHITECTURE.md) 覆盖低压供电、传感器/执行器、ECU/VCU/BMS/MCU、任务调度、消息总线与故障；[P1–P3](ROADMAP.md) 有纵向交付 | 采样/延迟/饱和/deadline/通信/欠压与故障闭环可复现；不得绕过传感器直接驱动物理量 | 已覆盖 |
| 排气系统达到动态仿真级别 | [排气与后处理架构（§11.6）](ARCHITECTURE.md) 覆盖排气门至尾管的流动、组分、热、背压、后处理与声学；[验证 KPI](VALIDATION.md) 定义证据 | 质量/组分/能量闭合，背压反馈发动机，light-off 与尾管累计排放有数据/收敛报告 | 已覆盖 |
| 真正的 3D 版本 | [3D 架构（§13.1）](ARCHITECTURE.md) 规定参数化/glTF 网格、不可变快照、PBR、剖切/爆炸/叠加层与 SDL_GPU；[P5](ROADMAP.md) 有交付和退出门槛 | 机构姿态与解析状态一致，LOD/视觉不改变物理，Linux GPU 达成冻结预算 | 已覆盖 |
| 支持 BEV | [模型范围（§11.2）](ARCHITECTURE.md)、[P2](ROADMAP.md) 和 [验证 L3](VALIDATION.md) | 电池、电机、逆变器、BMS、再生和热降额通过组件/场景验证 | 已覆盖 |
| 支持 REV | [模型范围（§11.3）](ARCHITECTURE.md) 明确无发动机—车轮机械直连；[P4a](ROADMAP.md) | 燃料、电、机械、热账本闭合且拓扑检查无隐藏直连 | 已覆盖 |
| 支持 HEV | [模型范围（§11.4）](ARCHITECTURE.md) 覆盖串联、并联、功率分流；[P4b](ROADMAP.md) | 模式切换能量连续；各承诺拓扑有 golden scenario | 已覆盖 |
| 支持 PHEV | [模型范围（§11.4）](ARCHITECTURE.md) 区分充电口/OBC、CD/CS；[验证 KPI](VALIDATION.md) | 充电和 CD/CS 场景通过，首尾 SOC 修正后的能耗可核算 | 已覆盖 |
| 包括材质 | [材料设计（§12）](ARCHITECTURE.md) 分离工程材料和 PBR 视觉材质；[验证（§7）](VALIDATION.md) | 密度/热/电/磁等进入模型；视觉材质变化不改变物理 | 已覆盖 |
| 模拟级别高于原版 | [精度档（§10）](ARCHITECTURE.md) 和完整 [验证计划](VALIDATION.md) 将其定义为方程可追踪、守恒、收敛、实测对照和能量账本 | 只有发布验证报告后才允许具体精度声明 | 已覆盖，且避免无证据承诺 |
| 可作为插件嵌入其它游戏 | [游戏嵌入 API（§15）](ARCHITECTURE.md) 定义函数表、opaque handle、批量快照、VFS 和 Godot/Unity/Unreal 路径；[P6](ROADMAP.md) | C/C++ 宿主、ABI 兼容矩阵、至少一个正式游戏引擎适配器通过 | 已覆盖 |
| 支持 Linux | [Linux Tier 1（§17）](ARCHITECTURE.md) 定义 GCC/Clang、x86_64、SDL_GPU Vulkan、Wayland/X11、音频和诊断；路线图 P0/P6 有门槛 | headless CI、软件 Vulkan smoke、多 GPU/桌面/音频实机矩阵 | 已覆盖 |
| 给出基础计划 | [路线图](ROADMAP.md) 含 MVP/1.0/非目标、依赖、阶段、人周、前 30 日、首批 issue、退出门槛和风险 | 每阶段按退出门槛而非功能展示关闭 | 已覆盖 |
| 给出可落地实现方案 | [首阶段实现方案](IMPLEMENTATION.md) 定义 Controlled Shaft/Exhaust Flow 两条切片、编译流水线、组件契约、执行顺序、API 和提交顺序 | 两条 headless 切片达到 P1 退出门槛后再扩展完整 BEV/ICE | 已覆盖 |
| 开始开发可被游戏集成的深度仿真 | [开发状态](DEVELOPMENT_STATUS.md) 记录 C23 构建、固定步调度、版本化 C ABI、受控轴系、SI 发动机、守恒排气、DCT7 和液力 4AT 动态组件 | 完整模型/VFS/Lua 加载、录制回放、整机公开 builtin 和游戏引擎适配器通过 | 原型开发中 |
| 首个单元为 EA211 DJS + DQ200 | [DJS/DQ200 样例](../assets/samples/ea211_djs_dq200/README.md) 与 `source_manifest.toml` 冻结候选供体、证据等级、DQ200 奇偶轴拓扑、完整待测参数和发布门槛 | 供体身份、DJS/0CW 专属参数与台架验证完成；当前代码只提供通用模型骨架 | Research / Unverified calibration |
| 第二个单元为 PSA EC5 + AT8 4AT | [EC5/AT8 样例](../assets/samples/psa_ec5_at8_4at/README.md) 与 `source_manifest.toml` 区分四挡 AT8 和八挡 EAT8，记录变矩器/液压/行星/摩擦元件边界 | 供体身份、EC5/AT8 专属齿比、行星/液压/控制和台架验证完成 | Research / Unverified calibration |
| 模拟整个动力总成 | 两样例共享机器可读 `power.complete_powertrain.v1`，覆盖空气/燃油/12 V/控制、发动机、进排气/后处理、热/附件、变速器、终传动/差速器/半轴 | 冷起动—起步—换挡—热/排气/故障全闭环整机场景和总账通过；不得以发动机+理想挡位表冒充 | 边界已冻结，整机耦合开发中 |
| 可通过 LuaInstaller 打包 | [发行设计](PACKAGING.md) 区分可信 Lua 应用壳、C SDK/游戏插件和沙箱模型资产；已有 CMake helper 与可信入口骨架 | Lua 5.5 `power_native`、onedir/onefile clean-environment CI、原生依赖/许可/签名审计通过 | 架构与接入骨架已覆盖 |
| 考虑 GPT-6 / 未来模型能力推进开发，允许重构架构 | [模型运行时](MODEL_RUNTIME.md)：公开带单位 IR、图编译/隐式耦合求解、通用 SDK backend、模型/实例分离、JSON 实验反馈 | 解析解、收敛、能量账本、ABI 生命周期和真实 JSON 实验/采样回放通过；非线性与参数证据继续推进 | 线性电—机械—热基线已实现 |

## 2. 测绘事实复核

以下数据由当前锁定 checkout 重新计算，不依赖先前会话记忆：

| 事实 | 当前证据 |
|---|---|
| 经典上游 SHA | `85f7c3b959a908ed5232ede4f1a4ac7eafe6b630` |
| 社区发布仓库 SHA | `4e5c20da3e2c8b373ec795931b081f4e614048c3` |
| 经典子模块 | 5 个，均已 checkout 到 `.gitmodules` 固定提交 |
| 测绘代码范围 | 80 个 `.cpp`、114 个 `.h`、15,287 行有效 C/C++ |
| 内容资产 | 58 个 `.mr`、83 个 `.wav` |
| 测试 | 30 个 GoogleTest case，集中于 gas/function/synthesizer |
| 默认调度 | 10 kHz 主模拟频率、每主步 8 个流体子步、44.1 kHz 音频 |
| 平台限制 | README 明示 Windows-only；CMake 使用 `WIN32`、固定 Discord `.lib`，旧平台层含 Win32/D3D |

## 3. 当前实现边界

已经完成的是：参考源码拉取/锁定与静态测绘、Power! 命名和目标架构、严格 C23/CMake 构建、固定步调度器、公开 C ABI 生命周期/批量输入/快照、受控电驱轴系、通用平均值 SI 发动机、守恒排气/催化器网络、通用 DCT7 和液力 4AT 动态骨架，以及两个完整动力总成样例的证据/测量骨架。实际文件和限制以 [开发状态](DEVELOPMENT_STATUS.md) 为准。

本轮新增：公开带单位 descriptor IR、线性电—机械—热图、预分解隐式机电求解、热损耗账本、共享模型/独立实例、通用 SDK backend、类型化句柄、JSON 场景和采样回放报告。

尚未完成的是：Lua 5.5.1 vendoring/沙箱及其 IR 前端、非线性通用组件图、曲轴角逐缸气体交换与燃烧、DJS 增压及专属 ECU、EC5 专属控制、DQ200 机电液压细节、AT8 供体行星/摩擦元件/阀体标定、完整冷却/润滑/附件、终传动/半轴精细模型、完整录制/checkpoint 恢复、3D renderer、音频、游戏引擎适配器和产品验证。已有通用默认值只可用于测试，不能因代码存在就升级为命名实车模型。

## 4. 尚待所有者决策但不阻断计划完整性

- Power! 名称的商标/混淆检查、发布 slug、开源/商业模式和许可证。
- DJS/DQ200 与 EC5/AT8 的合法供体身份，以及可发布的机械、电气、控制时序、排气与尾气组分验证数据。
- 首个正式支持的游戏引擎适配器。
- 运行期 Lua 是否只支持低频监督控制，以及何时需要外部 ECU/VCU 的 SIL/FMI 接口；高频内置闭环固定为 C 控制图。
- 最低 Linux 硬件和游戏中的目标并发实例数。

这些选择已经被放在 [P0 退出条件](ROADMAP.md) 内；在决定前不会妨碍架构和工作分解成立，但会阻止实现阶段宣称 P0 完成。

## 5. 文档一致性验收

完成审计要求：

- 所有 Markdown 代码围栏成对。
- 所有本地 Markdown 链接存在。
- 规划文档不存在已废弃技术栈的残留假设。
- 两个参考 checkout 干净，经典仓库 5 个子模块完整。
- 参考源码目录被父仓库忽略，避免意外 vendoring；锁定说明仍可纳入父仓库。

上述检查已在 2026-08-27 当前工作区通过。
