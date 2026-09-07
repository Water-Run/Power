# Power!

Power! 面向动力总成建模、交互实验和 Agent 驱动的模型开发。当前主线采用 **C# 跨平台物理核心 + Unity 3D 工作室 + MCP 接口**。模型、求解器、实验与画面分层，未来更强的模型可以通过同一组契约生成模型、定位错误、做分支实验并检查物理证据。

## 技术栈

截至 2026-09-07 核实并固定的正式版本：

| 层 | 版本与职责 |
|---|---|
| Unity 3D | **Unity 6.6 / 6000.6.0f1**，桌面工作室 |
| 渲染 / 输入 / UI | **URP 17.6.0**、**Input System 1.20.0**、UI Toolkit |
| C# 工具链 | **.NET 10 SDK 10.0.400 / C# 14**，核心、CLI、Agent 服务和开发工具 |
| Unity 核心程序集 | **.NET Standard 2.1**；与 .NET 10 版本共享源码 |
| Agent 传输 | 官方 **MCP C# SDK 2.2.0**，stdio，依赖锁文件纳入版本控制 |

版本来源：[Unity 6.6 发布说明](https://unity.com/releases/editor/whats-new/6000.6.0f1)、[.NET 10 下载](https://dotnet.microsoft.com/en-us/download/dotnet/10.0)、[MCP SDK 2.2.0](https://www.nuget.org/packages/ModelContextProtocol/2.2.0)。

Unity 6.6 自带编译器仍是 C# 9，运行时 API 默认是 .NET Standard 2.1。现代 C# 核心由外部 .NET SDK 编成 Unity 可引用的 DLL；`Unity/Assets` 内的脚本使用 C# 9 兼容语法。Unity Player 不需要安装 .NET 10。[Unity C# 支持](https://docs.unity3d.com/6000.6/Documentation/Manual/csharp-compiler.html)、[Unity API 兼容级别](https://docs.unity3d.com/6000.6/Documentation/Manual/dotnet-profile-support.html)。

## 开发入口

安装固定 SDK 后，从仓库根目录执行；以下命令兼容 Windows、Linux、macOS：

```sh
dotnet run --file tools/Build.cs -- verify
```

这会串行构建解决方案、导出 Unity 模型资产、运行核心和 Agent 检查、执行实际 MCP 进程联调，并生成 `artifacts/reports` 下的电热与热交换实验报告。构建禁用驻留服务与并发编译，以降低桌面内存压力。当前工作区还可用 `.cache/dotnet/dotnet` 调用已安装的固定 SDK；缓存不进入版本控制。

直接执行实验：

```sh
dotnet run --project src/Power.Cli -c Release -- assets/labs/electrothermal.power.json --output artifacts/reports/electrothermal.json
```

CLI 退出码：`0` 表示实验通过，`2` 表示 KPI/回放未通过，`1` 表示输入或执行错误。模型文档有严格字段检查、显式单位、固定纳秒步长、输入事件和 KPI 条件。报告包含来源哈希、模型指纹、运行平台、保真度、通道、回放证据和能量残差。

## Unity 3D 工作室

1. 执行 `dotnet run --file tools/Build.cs -- build`，生成 `Unity/Assets/Plugins` 下的 Core、Assets 两个程序集，以及 `Assets/Generated/Resources` 下的两个 `.powerasset` 样例。
2. 用 Unity Hub 添加本仓库的 `Unity` 目录，选择 **6000.6.0f1**。
3. 等待包解析和脚本导入；首次导入自动生成 URP 与材质资产。
4. 打开 `Assets/Scenes/PowerLab.unity`，或菜单 **Power > Open laboratory**，进入 Play。

场景代码按资产中的节点、组件和通道生成转子、热节点、连接与输入控件，支持暂停、重置和保存实验的精确事件回放。默认电热样例为 10 秒制动再驱动实验；`ThermalNetwork.powerasset` 是没有外部输入的热交换实验。在资产 Inspector 点击 **Open in Studio** 可切换场景模型。

导出其他模型时，在完成构建后执行：

```sh
dotnet src/Power.Cli/bin/Release/net10.0/Power.Cli.dll export assets/labs/electrothermal.power.json --name "My laboratory" --output Unity/Assets/Models/MyLaboratory.powerasset
```

导入器检查资产摘要、重新编译模型并核对指纹，格式见 [模型资产](docs/ASSET_FORMAT.md)。鼠标拖动环绕、滚轮缩放。每次 Unity `FixedUpdate` 推进不超过 2000 个完整 tick，默认模型推进 20 ms；7 ms 步长模型推进 14 ms。模型状态不读取渲染帧的 `deltaTime`，该调度不保证所有步长下的墙钟实时速度。

设置 `POWER_UNITY_EDITOR` 为编辑器可执行文件后，可执行 `dotnet run --file tools/Build.cs -- unity-test`，检查编辑器导入、Play 生命周期与无界面核心的一致性。当前环境没有 Unity Editor，**尚未验证实际场景显示、Unity 测试和 IL2CPP Player**；[验证记录](docs/VALIDATION.md)明确区分已运行与待运行项目。

## Agent 入口

构建后，把以下程序作为客户端的 stdio MCP server 启动：

```sh
dotnet /absolute/path/to/Power!/src/Power.Mcp/bin/Release/net10.0/Power.Mcp.dll
```

服务提供 12 个带输入/输出 Schema 的工具：能力发现、模型 Schema、示例、校验、实验、资产导出、创建实例、输入修改、推进时间、快照、分支和释放实例。协议输出独占 stdout，运行日志走 stderr。Agent 不需要访问 Unity 界面，也不需要在物理循环中调用模型 API。

详细契约、客户端配置和完整操作序列见 [Agent 开发接口](docs/AGENT_API.md)。核心直接提供 `TryCompile`、可发现通道、`Fork`、取消与原子回滚；MCP 外层增加版本冲突检测和紧凑报告。

## 当前范围

当前 C# 可执行模型包含转动惯量、带正/负传动比的弹性轴、RL 直流电机、扭矩源、热容量和热传导网络。它们共享机电求解与能量账本，适合验证组合建模契约。所有样例参数标记为 `unverified`。

完整发动机、进排气、燃烧、DCT/AT、液压与 ECU/TCU 仍属于项目目标。旧 C 原型及其测试完整保存在 [`legacy/native`](legacy/native/ARCHIVE.md)，其功能尚未全部迁入 C#。EA211 DJS + DQ200、PSA EC5 + AT8 的研究资料仍在 [`assets/samples`](assets/samples)，不会被当作已标定的可运行总成。

后续实现以 [架构](docs/ARCHITECTURE.md)、[开发路线](docs/ROADMAP.md)、[验证记录](docs/VALIDATION.md)为依据。项目自身的发布许可尚待所有者确定；上游参考的许可不自动成为本项目许可。
