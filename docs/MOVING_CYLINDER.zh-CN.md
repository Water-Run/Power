# 运动气缸换气

[English](MOVING_CYLINDER.md) · **简体中文** · [Français](MOVING_CYLINDER.fr.md) · [Русский](MOVING_CYLINDER.ru.md) · [日本語](MOVING_CYLINDER.ja.md) · [한국어](MOVING_CYLINDER.ko.md) · [Deutsch](MOVING_CYLINDER.de.md) · [Español](MOVING_CYLINDER.es.md) · [Italiano](MOVING_CYLINDER.it.md) · [Português](MOVING_CYLINDER.pt-BR.md)

`gas_cylinder` 把一根转动曲轴连接到一个气室。与绝热封闭基准不同,该气室携带独立的质量与内能,因此节流与壁面链可以在压力驱动曲轴的同时改变其状态。该部件可通过 Core、JSON、CLI、MCP 与可移植资产使用。它不建模活塞惯量或精细化学。单独的[预混燃烧](PREMIXED_COMBUSTION.zh-CN.md)与[曲轴转角定时](VALVE_TIMING.zh-CN.md)部件现在提供燃料能量转换,并控制所连接的节流。

## 模型契约

```csharp
var definition = new ModelDefinition
{
    StepNanoseconds = 50_000,
    Nodes = [NodeDefinition.Rotor(1, 0.2, 60),
        NodeDefinition.CylinderGas(2, 100_000, 300),
        NodeDefinition.Thermal(3, 500, 350)],
    Components = [ComponentDefinition.GasCylinder(10, 1, 2, new()
    {
        Bore = new(86, Unit.Millimeter), Stroke = new(86, Unit.Millimeter),
        RodLength = new(143, Unit.Millimeter), Phase = new(0, Unit.Radian),
        CompressionRatio = 10, BackPressure = new(1, Unit.Bar)
    }), ComponentDefinition.GasReservoir(11, 2, 50e-6, 110_000, 300, 0.8, 100, 1),
        ComponentDefinition.GasHeatLink(12, 2, 3, 2.5)]
};
```

气体节点提供初始绝对压力、温度、R 与 gamma。其 `Storage` 量为零/`None`:恰好一个气缸部件拥有该容积。编译器根据曲轴初始角度(含相位)处的几何导出初始质量与能量。它拒绝为气室单独指定容积,或让两个气缸部件拥有同一气室。未连接的定容气体节点仍需要正的显式容积。

在 JSON 中,运动气室使用 `domain: "gas"` 并省略 `storage`。`gas_cylinder` 部件需要 `node_a`(转动)、`node_b`(气体),以及参数 `bore`、`stroke`、`rod_length`、`phase`、`compression_ratio` 与 `back_pressure`。该部件不重复填写组分或初始气体状态。气体节点暴露压力、温度、质量与内能;气缸暴露容积、活塞位移与曲轴扭矩。端口、节流、开度界限与壁面链使用既有的[气体网络契约](GAS_NETWORK.zh-CN.md)。

含运动气室、且没有定时节流或预混追踪的模型报告 `moving_cylinder_gas_exchange` 保真度,并加入求解器指纹标签 5。既有的线性、封闭气缸与仅定容模型保留其指纹与步进。组分保持固定,相连气体节点必须一致,且全部参数仍为 `unverified`。

## 方程与守恒耦合

对于组分固定的量热完全气体:

```text
p = (gamma - 1) U / V(theta)
T = U / (m cv)
dm/dt = sum(inflow) - sum(outflow)
dU/dt = -p dV/dt + Q_wall + sum(mdot_in h_in) - sum(mdot_out h_out)
tau_gas = (p - p_back) dV/dtheta
```

质量/能量平衡遵循标准的开系第一定律;参见 [Cantera 的控制容积方程](https://www.cantera.org/stable/reference/reactors/controlreactor.html)。该参考支持这些方程,并不支持 Power! 的积分格式或验证。气体使用既有的双向可压缩喷管定律,而不是 Cantera 的线性、单向气门实现。背压功是外部源功;储气库焓与壁面交换保留既有的账本符号。

含运动气室的模型使用对称算子分裂:

1. 在节拍开始时的曲轴几何上,把换气与壁面热推进半个节拍。
2. 用有界的离散梯度曲轴求解器,在整个节拍上求解耦合机电与绝热压力功。
3. 在得到的曲轴几何上,把换气与壁面热再推进半个节拍。
4. 把累积的气壁热量与机电损耗施加到热求解。

第 2 步期间质量固定,且 `U_new = U_old (V_old/V_new)^(gamma-1)`。平均气体压力与扭矩来自这同一能量变化的差商。曲轴获得气体功减去背压功;气室失去的恰好是对应的气体功,精确到浮点精度。`log1p`/`expm1` 与解析容积差商避免在小步长和止点附近去减几乎相等的状态。多个气缸可以共享一根曲轴,或通过耦合轴作用。

对该测试过的、无壁面传热的光滑临界流情形,分裂具有二阶收敛。两次气体半步期间壁温保持固定,随后是既有的热求解:与壁面耦合的精度仍是一阶。近平衡流动限制器也会改变局部阶数。守恒并不能确立精度。

## 界限、失败与兼容性

曲轴转角限制为每节拍 0.25 rad;非线性求解最多 16 次迭代、10 次线搜索试探。每个气体半步仍保留 4096 子步上限、2% 目标相对变化,以及 25% 校正变化拒绝。无效或非有限的气体状态、输出,或求解器耗尽,会拒绝调用方的整个批次,包括此前所有节拍与已调度输入。重试前减小 `step_ns`,并检查流通面积、气体状态、热导、曲轴转速与惯量。取消与分支保留全部气体和账本状态;成功的步进以及调用方缓冲区快照不分配托管内存。

资产 v4 为每个气体气缸增加一条有界的索引几何记录,并保留全部 v1/v2/v3 读取器。它从不序列化求解器工作区。一份真实的定容 v3 夹具验证:引入运动几何不会改变先前的气体指纹或回放。参见[资产格式](ASSET_FORMAT.zh-CN.md)与[夹具溯源](../tests/Power.Tests/Fixtures/README.md)。

## 实验与证据

[运动气缸实验室](../assets/labs/moving-cylinder.power.json)用两个储气库节流和一面有限热壁拖动一个气缸。八个基于时间的开度事件演练流入与流出气室,并包含报告边界与展示边界之间的节拍。这是未经标定的拖动实验;该时间表不是 ECU、凸轮型线、四冲程发动机控制器或燃烧模型。

测试通过正转/反转与止点,把封闭气室与既有封闭气缸实现相比较,并把开放气室与独立编写的控制 ODE 的 RK4 积分相比较。后者直接由方程写出几何、临界质量流与压力功。步长细化分别检查光滑流动与壁面耦合的精度。额外检查覆盖多根耦合/共享曲轴、封闭与开放气缸混合、守恒、非法归属、单位归一化、原子失败/恢复、分支、取消、零分配、可移植兼容性,以及全部 JSON/MCP/资产报告边界。

Unity 包含运动活塞视图、气体连接以及导入/Play 测试。真实的 Editor、渲染、Play Mode 与 IL2CPP 证据仍待完成。已执行的检查见[验证记录](VALIDATION.zh-CN.md),剩余的发动机、变速器、控制与标定工作见[路线图](ROADMAP.zh-CN.md)。
