> Historical C-era document from commit `c342d4c`. For current Zig build commands, see [the native README](../README.md).

# Power!（历史 C 原型）

当前主线已迁至 C# 与 Unity 3D。本文件描述迁移前的原型，参见 [存档说明](https://github.com/Water-Run/Power/blob/c342d4c05c9d0b14cae0ce1a85e3f2100dfd7bb3/legacy/native/ARCHIVE.md)与[当前 README](https://github.com/Water-Run/Power/blob/c342d4c05c9d0b14cae0ce1a85e3f2100dfd7bb3/README.md)。

Power! 是一个正在开发的 C23 + Lua 5.5 3D 动力总成模拟器。它只参考经典 `engine-sim` 的产品思想，是不移植旧代码、不追求旧实现兼容的完整重写。目标不是简单把二维界面换成三维，而是建立一个可验证、可分级运行、可无界面嵌入游戏的多领域仿真核心。

当前 0.2.0 开发树已有可编译、可测试的 headless 内核、可通过公开 C ABI 编译的电—机械—热组件图，以及 JSON 实验/回放工具。Lua 资产编译器、非线性多域图、量产标定和 3D/音频前端尚未完成。现有模型是物理架构原型，不具备工程预测或“数字孪生”声明所需的数据精度。

## 已实现的开发基线

- 严格 ISO C23 CMake 工程；GCC/Clang、`-Werror`、导出符号检查和 headless C 宿主测试。
- 数据驱动模型 IR：带单位的节点/部件、类型化连接、稳定 ID、结构化编译诊断、不可变共享模型和独立实例。
- 可组合电—机械—热图：惯量、柔性传动轴、有符号速比、直流电机 RL 支路、扭矩源、热容/热连接；机电隐式中点和热网络后向 Euler，统一能量账本。
- 离线 JSON 实验入口：固定时刻输入、通道自省、KPI、运行库指纹，以及不同批量步进的采样回放比对。便于 GPT-6 与后续模型参与建模、实验和验证。
- 整数纳秒固定基准时钟、稳定任务排序、确定性状态哈希和 generation handle 生命周期。
- 受控轴系：低压供电、传感器采样/延迟、控制任务、执行器故障、电机—柔性轴—负载和电—机械—热能量账本。
- 通用 SI 发动机：进气歧管充排、电子节气门、喷油/燃烧、曲轴与起动、背压泵气损失、排气组分、冷却热状态、ECU 欠压/传感器超时和质量—能量残差。
- 守恒排气网络：三集中容积、双向/临界流、四组分、壁面换热、催化器热与转化状态、尾管堵塞反馈。
- 通用七挡双干式离合器：奇偶轴、预选、离合器交接、滑摩/热衰退和能量账本；默认故意不提供 DQ200 齿比。
- 通用四挡液力自动：变矩器、锁止、液压压力/换挡执行器、连续换挡过渡、ATF/锁止热状态和诊断；默认值仅为实验参数，不是 AT8 标定。
- 版本化 `pwr_get_api` C ABI、批量标量输入、固定步进和调用者持有缓冲区的快照；支持编译/查询模型和创建图实例。公开 builtin 仍为 Controlled Shaft Lab，发动机/变速器原型暂走内部测试接口。

快速构建与测试：

```bash
cmake -S . -B build -DCMAKE_BUILD_TYPE=Debug
cmake --build build --parallel
ctest --test-dir build --output-on-failure
./build/power_c_host
./build/power_model_host
python3 tools/model_lab.py assets/labs/electrothermal.power.json \
  --library build/libpower.so --output build/electrothermal-report.json
```

本轮架构、实际物理范围、求解方程、实验格式和未来扩展路线见 [可组合模型运行时](https://github.com/Water-Run/Power/blob/c342d4c05c9d0b14cae0ce1a85e3f2100dfd7bb3/legacy/native/docs/MODEL_RUNTIME.md)。更强的基础模型通过结构化定义、测试与实验参与开发；仿真运行时使用已编译模型，保持确定性与独立部署。

## 产品目标

- 支持 ICE、BEV、HEV、PHEV 和 REV（增程式电动车）。
- HEV 覆盖串联、并联和功率分流；REV 明确建模为发动机驱动发电机、牵引由电机完成的拓扑。
- 把 ECU/TCU/VCU/BMS、电机控制器、传感器、执行器、低压供电和车载通信作为有状态、有时序、可故障注入的电子控制系统仿真，而不是界面脚本。
- 把进气、燃烧、排气、涡轮、消声和后处理组成质量—组分—能量守恒网络；背压、温度、声学与尾管排放会双向耦合到动力系统。
- 同时支持工程材料属性与 3D PBR 外观材质，并让质量、惯量、热、电、磁、摩擦等属性进入相应模型。
- 以 Linux 为一级平台，提供有界面应用和完全 headless 的仿真库。
- 用 Lua 定义发动机、材料和完整动力系统拓扑；C 在加载时把定义编译成实时组件图。
- 通过稳定 C API/ABI 嵌入 Godot、Unity、Unreal 或自研游戏。
- 保留原版的实时机械响应与声音特色，同时用守恒检查、收敛测试和公开/授权测量数据定义“更高模拟级别”。

## 关键架构结论

仿真核心、Lua 定义层、音频、3D 前端和宿主 SDK 必须分离。Lua 负责声明和组合，不跑缸压/气流/电机等数值热循环；C 负责验证、编译、求解和实时调度。发动机内部运动由动力系统状态推导 3D 姿态，不把通用 3D 刚体引擎当作热力学或曲轴求解器。所有动力形式由同一个带类型端口的组件图连接：旋转机械、电气、热、气体/液体遵守守恒契约，控制与通信遵守显式采样和因果时序契约。

第一条纵向切片优先做受控电驱轴系。低压电源—传感器—控制任务—逆变器—电机—轴—负载可以最早验证 Lua DSL、组件图、能量核算、多速率控制、故障、热降额、3D 状态快照、音频线程和插件 ABI；下一条流体切片验证气源—管路—催化器—尾管的质量/组分/能量账本，再加入曲轴角燃烧并组合出完整 ICE、REV、HEV/PHEV。

## 规划文档

- [项目命名约定](https://github.com/Water-Run/Power/blob/c342d4c05c9d0b14cae0ce1a85e3f2100dfd7bb3/legacy/native/docs/NAMING.md)
- [上游源码测绘](https://github.com/Water-Run/Power/blob/c342d4c05c9d0b14cae0ce1a85e3f2100dfd7bb3/legacy/native/docs/UPSTREAM_SURVEY.md)
- [目标架构与模型范围](https://github.com/Water-Run/Power/blob/c342d4c05c9d0b14cae0ce1a85e3f2100dfd7bb3/legacy/native/docs/ARCHITECTURE.md)
- [可组合模型运行时与未来模型开发架构](https://github.com/Water-Run/Power/blob/c342d4c05c9d0b14cae0ce1a85e3f2100dfd7bb3/legacy/native/docs/MODEL_RUNTIME.md)
- [首阶段实现方案](https://github.com/Water-Run/Power/blob/c342d4c05c9d0b14cae0ce1a85e3f2100dfd7bb3/legacy/native/docs/IMPLEMENTATION.md)
- [当前开发状态与整机集成基线](https://github.com/Water-Run/Power/blob/c342d4c05c9d0b14cae0ce1a85e3f2100dfd7bb3/legacy/native/docs/DEVELOPMENT_STATUS.md)
- [LuaInstaller 与平台发行设计](https://github.com/Water-Run/Power/blob/c342d4c05c9d0b14cae0ce1a85e3f2100dfd7bb3/legacy/native/docs/PACKAGING.md)
- [验证与性能基线](https://github.com/Water-Run/Power/blob/c342d4c05c9d0b14cae0ce1a85e3f2100dfd7bb3/legacy/native/docs/VALIDATION.md)
- [阶段路线图](https://github.com/Water-Run/Power/blob/c342d4c05c9d0b14cae0ce1a85e3f2100dfd7bb3/legacy/native/docs/ROADMAP.md)
- [需求追踪与规划完成审计](https://github.com/Water-Run/Power/blob/c342d4c05c9d0b14cae0ce1a85e3f2100dfd7bb3/legacy/native/docs/TRACEABILITY.md)
- [上游版本与许可边界](https://github.com/Water-Run/Power/blob/c342d4c05c9d0b14cae0ce1a85e3f2100dfd7bb3/legacy/native/reference/README.md)

## 首批完整动力总成样例

- [EA211 DJS + DQ200](https://github.com/Water-Run/Power/blob/c342d4c05c9d0b14cae0ce1a85e3f2100dfd7bb3/legacy/native/assets/samples/ea211_djs_dq200/README.md)：候选基线为上汽大众 DJS + 0CW.C，已建立完整系统边界、证据清单和待测参数；尚无可发布标定。
- [PSA EC5 + AT8 4AT](https://github.com/Water-Run/Power/blob/c342d4c05c9d0b14cae0ce1a85e3f2100dfd7bb3/legacy/native/assets/samples/psa_ec5_at8_4at/README.md)：候选基线为 C-Elysée 培训资料中的 EC5 115 Ch + 四挡 BVA AT8，已覆盖变矩器、液压/行星机构、热与电控取证边界；不要与后来的八挡 EAT8 混淆。

两个样例共同遵守 `power.complete_powertrain.v1`：空气、燃油、12 V、驾驶员和车辆网络进入，内部覆盖发动机、进排气/后处理、热/润滑/附件、ECU/TCU、发动机—变速器耦合、变速器、主减速器、差速器和半轴，最终输出半轴扭矩、尾管物质/热状态、电/热负载、诊断和声振事件。缺少其中任一域时不得标为“完整动力总成”。

## 当前参考源码

本地已拉取：

- `reference/engine-sim`：经典开源版，固定在 `85f7c3b959a908ed5232ede4f1a4ac7eafe6b630`，含全部子模块。
- `reference/engine-sim-community-edition`：后续发布/教程仓库，固定在 `4e5c20da3e2c8b373ec795931b081f4e614048c3`；该仓库没有应用源码。

两个 checkout 均被 `.gitignore` 排除，不会意外把上游源码 vendoring 到新项目。

## 近期出口条件

下一里程碑把受限 Lua 前端接入当前公开 IR，建立参数证据与非线性求解契约，再逐步迁入发动机/变速器和采样控制模型。完整动力总成必须通过冷起动、起步、换挡、排气堵塞、欠压和热保护场景；现有线性图与采样回放只是其中一层基线。DJS/DQ200 与 EC5/AT8 的供体身份、齿比、液压/控制和排放标定在合法测量数据到位前持续保持 `Research / Unverified calibration`。详细门槛见路线图。
