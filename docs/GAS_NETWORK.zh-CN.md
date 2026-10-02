# 已编译气体网络

[English](GAS_NETWORK.md) · **简体中文** · [Français](GAS_NETWORK.fr.md) · [Русский](GAS_NETWORK.ru.md) · [日本語](GAS_NETWORK.ja.md) · [한국어](GAS_NETWORK.ko.md) · [Deutsch](GAS_NETWORK.de.md) · [Español](GAS_NETWORK.es.md) · [Italiano](GAS_NETWORK.it.md) · [Português](GAS_NETWORK.pt-BR.md)

有限气体网络现在通过 `CompiledModel` 与 `Simulation` 运行。该检查点覆盖定容气室、定压力/温度储气库、受控孔口与壁面热链。[运动气缸扩展](MOVING_CYLINDER.zh-CN.md)现在把换气连接到随曲轴变化的容积与压力功;下文所述的定容求解器保留其原有行为。

## C# API 与单位

```csharp
var model = CompiledModel.Compile(new ModelDefinition
{
    StepNanoseconds = 100_000,
    Nodes = [NodeDefinition.GasVolume(1, 0.002, 2e6, 900)],
    Components = [ComponentDefinition.GasReservoir(10, 1, 2e-5, 1e5, 300,
        dischargeCoefficient: 0.9, channel: 100, opening: 0)]
});
var simulation = model.CreateSimulation();
simulation.SubmitInputs([new Scalar(100, 0.5)]);
var status = simulation.Step(100_000_000);
var values = new Scalar[model.OutputCount];
var snapshot = simulation.ReadSnapshot(values);
```

`GasVolume` 接受以 m³ 计的容积、以 Pa 计的压力、以 K 计的温度,以及可选的、以 J/(kg K) 计的 R 和 gamma。气体节点把容积存在 `Storage`,温度存在 `Initial`,压力存在 `Position`,组分存在 `Gas`。升、巴与平方毫米由显式的量接受,并在计算指纹之前归一化。

`GasOrifice` 连接两个气体节点 ID。`GasReservoir` 把一个节点连到固定边界;`NodeB == 0` 标识该储气库。`GasHeatLink` 连接一个气体节点与一个热节点,热导以 W/K 计。相连的气体节点必须共享完全相同的 R 与 gamma。开度是 [0,1] 内的无量纲分数,对初始、直接与已调度输入都作校验。输入通道 ID 为零时,初始开度保持固定。纯气体网络不需要占位转子。界限仍是 32 个节点、64 个部件与 64 个标量状态;每个气体容积消耗两个状态。

每个气体节点暴露压力、温度、质量与内能。节流暴露带符号的 A 到 B 质量流;热链暴露带符号的气体到壁面热流。储气库焓以流入为正。能量残差是 `source_work + reservoir_enthalpy - heat_rejected - stored_energy_change`。质量残差是 `sum(mass - initial_mass) - cumulative_reservoir_mass`。浮点残差按物理尺度评估,而不是按精确零。

## 数值方法与边界

气体求解器使用显式子步与 Heun 预测—校正。节拍开始时的最大相对质量/能量变化率用来选择统一的子步数,目标是每个子步变化 2%。子步超过 4096、非物理状态、非有限值,或校正后的质量/能量变化超过 25%,都会拒绝整个批次。减小 `StepNanoseconds` 并重新编译,或检查流通面积、容积、热导与初始条件。

喷管定律在压力相等处具有奇异导数。每次求值把传递的能量限制为相连一对达到等压时的交换量,并按同一比例缩放质量与上游焓。对于有限容积,该能量是 `abs(pA-pB) / ((gammaA-1)/VA + (gammaB-1)/VB)`;固定储气库省略 B 项。这可以防止孤立一对发生压力穿越振荡,同时保持成对账本。该限制器会改变近平衡积分;二阶精度仅在测试中光滑、未受平衡限制的临界流细化情形下断言。

气体子步期间壁温保持为该节拍开始时的值。累积的壁面热量随后进入既有的热求解。这一耦合相对外层节拍是一阶的;大步长下的稳定性或守恒本身并不能确立精度。壁面测试把有限时间内的温度与解析双热容解相比较。本方法不是早前提议的成对隐式求解器,也不验证该提议。

质量、能量、储气库累加以及补偿账本修正属于仿真状态,并包含在复制、回滚、分支与哈希中。成功的步进与调用方缓冲区快照不分配托管内存。失败的已调度批次会恢复此前全部分节拍与输入,包括在更早的成功节拍之后失败。输入更新与终止性的已调度事件也会拒绝非有限的气体可观测量。

带气体节点的模型加入求解器指纹标签 4。既有线性/气缸模型的指纹与状态哈希保留其先前构造。示例参数仍为 `unverified`。

## JSON、智能体与可移植集成 — 2026-09-22

[气体网络实验室](../assets/labs/gas-network.power.json)是 JSON、CLI、MCP 与可移植回放的共享示例。它包含两个气室、一个受控内部节流、一个受控储气库节流和一条壁面热链。其事件包含报告/展示边界之间的节拍;每个报告边界都与解码后的资产回放比较。参数仍是合成的,且为 `unverified`。

`power.model.v1` 增加这些显式定义:

| 定义 | JSON 字段与单位 |
|---|---|
| 气体节点 | `domain: "gas"`;`storage`:m3 或 l;`initial`:k;`position`:pa 或 bar;`gas`:gas_constant 以 j_kg_k 计,且 gamma > 1 |
| 气体孔口 | `kind: "gas_orifice"`;node_a/node_b;`initial_input`:[0, 1] 内的分数;参数:面积以 m2 或 mm2 计,以及 discharge_coefficient |
| 储气库孔口 | node_b 缺失或为零的气体孔口;还需要 reservoir_pressure,以 pa 或 bar 计,以及 reservoir_temperature,以 k 计 |
| 气体壁面链 | `kind: "gas_heat_link"`;node_a 为气体,node_b 为热;参数:conductance,以 w_k 计 |

缺失或为零的 `input_channel` 使显式初始开度保持固定。双容积节流禁止填写储气库参数。组分仅在气体节点上必填。新的检查字段是 `mass_flow`、`heat_flow`、`reservoir_enthalpy` 与 `mass_residual`;既有的气体状态与能量检查字段仍然可用。

`CompiledModel.ValidateInput` 检查静态的通道/取值约束,且不改变状态。实验校验与可移植资产创建对所有已调度开度使用它,包括较晚的事件。运行时提交/步进仍会做额外的、依赖状态的可观测量检查,并保留完整回滚。

`power.asset.v3` 及之后版本用有界的索引扩展记录保留气体组分、面积、流量系数与储气库压力。壁面热导、储气库温度、初始开度与输入通道 ID 使用基础部件字段。v1/v2 读取器仍支持其原有模型集,并拒绝气体定义。变更前的真实夹具验证向后兼容。参见[资产格式](ASSET_FORMAT.zh-CN.md)。

MCP 能力版本 0.8.0 通告气体域、部件、保真度、开度界限与有界求解器限制。`get_example_model` 接受 `gas-network`。构建导出 `GasNetwork.powerasset`;Studio 在既有输入与输出通道上增加示意容器、储气库标记以及节流/热路径。其新的导入与 Play Mode 测试需要真实的 Unity Editor 运行,不在 .NET 证据覆盖范围内。

## 验证与剩余的发动机工作

九组已编译模型检查与六组气体原语检查继续在两个 Core 目标上运行。可移植测试额外覆盖混合的气缸/气体/热模型、非 SI 量、非默认组分、扩展损坏、缺失/重复记录、v1/v2 兼容性、调度界限、取消以及事件游标回滚。JSON/Core 等价性与真实 MCP 回放覆盖集成边界。串行验证结果见[验证记录](VALIDATION.zh-CN.md)。

这些检查中 Standard 程序集在 .NET 10 上运行;这不是 Unity Editor 或 IL2CPP 证据。对于没有定时节流或预混追踪的模型,仅定容求解器的方程、积分界限与指纹构造保持不变。带运动气室或定时节流的模型使用另行版本化的分裂耦合,记录于 [MOVING_CYLINDER.md](MOVING_CYLINDER.zh-CN.md) 与 [VALVE_TIMING.md](VALVE_TIMING.zh-CN.md)。

可选的[预混燃烧](PREMIXED_COMBUSTION.zh-CN.md)现在以恒定气体物性输运燃油、新鲜空气与产物。精细组分热化学、标定车辆样本,以及完整的发动机/变速器/控制里程碑仍未完成。
