# 预混燃烧与燃料能量核算

[English](PREMIXED_COMBUSTION.md) · **简体中文** · [Français](PREMIXED_COMBUSTION.fr.md) · [Русский](PREMIXED_COMBUSTION.ru.md) · [日本語](PREMIXED_COMBUSTION.ja.md) · [한국어](PREMIXED_COMBUSTION.ko.md) · [Deutsch](PREMIXED_COMBUSTION.de.md) · [Español](PREMIXED_COMBUSTION.es.md) · [Italiano](PREMIXED_COMBUSTION.it.md) · [Português](PREMIXED_COMBUSTION.pt-BR.md)

`premixed_combustion` 把预设的 Wiebe 燃烧曲线耦合到一根曲轴与一个有限气室。燃油、新鲜空气与惰性产物经气体网络输运;反应消耗可用的限制反应物,并把储存的化学能转化为气体热能。压力功驱动运动气缸所用的同一曲轴求解器。Core、JSON、CLI、MCP 与资产 v6 共享这些定义。

这是集总、常物性的预混模型。相连网络中的每种组分共享同一个 R 与 gamma。这三类质量并不表示精细组分、可变热容、反应动力学、火焰传播、自燃、爆震、排放、燃油蒸发或喷射。最初的点火示例使用已经混合好的气体入口。[循环燃油计量](FUEL_METERING.zh-CN.md)支持单独的有限气体燃油轨与进气;液体喷雾/蒸发仍在本模型之外。预设燃烧以及通过的守恒测试,并不能确立实测发动机性能,也不能完成完整动力总成目标。

## 组分与端口

气体节点可以在既有的 `gas` 对象上可选地加入 `premixed`:

```json
"gas": {
  "gas_constant": { "value": 287, "unit": "j_kg_k" },
  "gamma": 1.35,
  "premixed": {
    "lower_heating_value": { "value": 44000000, "unit": "j_kg" },
    "stoichiometric_air_fuel_ratio": 14.7,
    "initial_fractions": { "fuel": 0.04, "fresh_air": 0.96 }
  }
}
```

热值与化学计量空燃质量比必须为正且有限。燃油与新鲜空气分数必须非负,且之和至多为 1。其余为惰性产物。新鲜空气表示氧化剂及其稀释气;1 kg 燃油消耗 `r` kg 新鲜空气会生成 `1+r` kg 产物。过量的新鲜空气或燃油仍然可用;产物不能再次反应。

预混节点上的每个储气库节流必须用同样的两个字段显式指定 `reservoir_fractions`。其他部件或内部节流禁止填写分数。流入时,边界供给该组分;流出时,按有限容积的实际组分移除。相连的有限气体容积必须共享追踪方式、R、gamma、LHV 与化学计量比。不兼容或未追踪的连接会被拒绝;化学存量不能在端口处消失。

气体求解器按与总气体相同的带符号质量通量与上游分数输运每种组分。它演化非负的组分质量,并由它们之和重建总质量。预混步还受总流出的限制,即使流入与流出的总质量速率几乎抵消。压力均衡或储气库回流不会凭空产生化学存量。

预混气体向声明的状态预算增加三个储存的组分值。燃烧部件增加一个不可逆角度前沿;全部仍在既有的 64 状态界限内。补偿后的边界账本与反应账本参与回滚、哈希与分支。

## 燃烧定律与曲轴历史

该部件把 `node_a`(曲轴)连接到 `node_b`(预混气体)。运动气室必须使用它自己的几何曲轴,且每个气室最多允许一个燃烧部件。定容容器可以为解析实验使用独立曲轴。

```json
{
  "id": 15,
  "kind": "premixed_combustion",
  "node_a": 1,
  "node_b": 2,
  "input_channel": 103,
  "initial_input": { "value": 1, "unit": "fraction" },
  "parameters": {
    "cycle_angle": { "value": 720, "unit": "deg" },
    "start_angle": { "value": 340, "unit": "deg" },
    "duration_angle": { "value": 80, "unit": "deg" },
    "shape_exponent": 3,
    "burn_coefficient": 6.9
  }
}
```

循环显式为 360 或 720 度。起始角相对于实际曲轴,不会被气缸几何相位隐含地偏移。持续角位于 [1e-6 rad, 循环角];形状指数 `n` 位于 [1,16],系数 `a` 位于 (0,50]。起始角按循环取模归一化。所有角度都需要单位。自燃烧起点起的正向进程 `z` 被截到 [0,1],累积危险度 `H(z) = a z^n`。每个完整循环贡献 `a`。

在新经过的正向角度上:

```text
hazard = burn_multiplier * delta(H)
limiting_fuel = min(fuel_mass, fresh_air_mass / stoichiometric_air_fuel_ratio)
burned_fuel = limiting_fuel * (1 - exp(-hazard))
consumed_air = stoichiometric_air_fuel_ratio * burned_fuel
created_products = burned_fuel + consumed_air
released_heat = LHV * burned_fuel
```

对于封闭充量且倍率为 1,已燃分数是其初始限制燃油量的 `1-exp(-a z^n)`。它在持续角边界处**并不**被强制为 1:一个完整燃烧窗口之后,`exp(-a)` 仍然未燃。小暴露量使用 `expm1` 以避免相消。在活动窗口期间进入的新鲜充量加入充分混合的反应物;不存在隐藏的、不受限的每循环热源。

可选输入通道是 `burn_multiplier`,即 [0,1] 内缩放危险度的分数。零会禁用反应;它并不阻止燃油从开启的入口进入。该输入不是喷油器指令,也不是预测性点火控制器。

每个部件储存所到达的最大曲轴角,初始化为起始角。反应只发生在该前沿之外。停转、反向旋转或回扫先前经过的角度,都不能再次放热。被禁用时的正向行程仍会移动前沿,因此重新启用不会释放错过的热量。在燃烧窗口内部启动时,只消耗其剩余的正向暴露。大幅反转之后,燃烧保持抑制,直到曲轴超过先前的最大值;双向发动机点火以及由控制器驱动的重新触发仍是今后的控制工作。

## 能量与数值耦合

气体内能仍是热的:`U = m cv T`。化学能单独为 `E_chemical = m_fuel LHV`。储气库总焓同时包含 `mdot cp T` 与被输运的化学能。全局储存能量变化包含化学存量,因此燃烧是内部转化,不是额外的外部源功:

```text
energy_residual = 机械/电气源功 + 储气库总焓
                  - 排出热量 - 总储存能量的变化
```

`net_fuel_energy_in` 单独暴露边界账本中的化学部分。它是净流入,包括离开模型的未燃燃油;它不是总供油量,也不是稳态油耗指标。`fuel_residual` 与 `fresh_air_residual` 比较初始存量、净边界转移、当前存量与累积反应。`mass_residual` 继续覆盖总气体质量。组分转化保持质量。

对于运动气室,热量预览取决于试探的新曲轴角,并参与非线性曲轴求解。设节拍内总热量为 `Q`,且 `r = (V_old/V_new)^(gamma-1)`:

```text
U_after = (U_before + Q/2) r + Q/2
adiabatic_work = (U_before + Q/2) (1-r)
crank_work = adiabatic_work - back_pressure * (V_new - V_old)
```

离散压力扭矩使用这同一份功,因此气体能量、化学能与曲轴功一致。只有求解在候选状态中成功之后才消耗燃油。气体输运仍在曲轴功/反应两侧使用对称半步。壁温在整个外层节拍上保持固定;壁面耦合是一阶的。

对于启用的燃烧,每节拍的转角行程与端点转速行程必须保持在 `min(0.25 rad, duration_angle/32)` 以内,并带有对应的 binary64 角度分辨率防护。一个节拍内释放的热量不得超过燃烧前热能的 25%。这些是对所接纳的功与分辨率的界限,不是精度保证。它们与气体子步及气缸迭代界限一并适用。出现 `numerical_failure` 时减小 `step_ns`,把已调度事件对齐到新节拍,并重新创建模型/会话。失败或取消的调用不提交状态、输入、前沿、化学账本或回放游标。

## 输出、兼容性与证据

预混节点增加 `fuel_mass`、`fresh_air_mass`、`product_mass` 与 `chemical_energy` KPI 字段。燃烧部件增加累积的 `fuel_burned`(kg)与 `heat_released`(J)。`burn_frontier` KPI 字段暴露其所到达的最大曲轴角(通道量 `burn_frontier_angle`,rad),因此可以检查反转之后被抑制的燃烧。全局通道增加化学能、净燃料能量输入、燃油残差与新鲜空气残差。发现所返回的通道量是权威的;例如节点燃油质量名为 `unburned_fuel_mass`。普通气体内能与流动输出保留其热含义与带符号流动含义。

预混模型加入指纹标签 7 以及归一化的反应/组分参数。更早的非反应指纹与步进保持不变。资产 v6 增加组分、储气库分数与燃烧记录;真实的 v1–v5 夹具保持兼容。新的保真度是 `premixed_gas_transport` 与 `premixed_wiebe_combustion`。

测试覆盖封闭容器的解析燃油/空气消耗与温度、限制反应物、储气库正向/反向转移、封闭网络的组分守恒、独立反应的曲轴/气体 ODE 收敛、停转/反转/禁用燃烧、非法契约、批次回滚、取消、分支,以及步进/快照的零分配。[点火气缸实验室](../assets/labs/fired-cylinder.power.json)带动负载通过重复的进气/压缩/燃烧/膨胀/排气阶段,并在全部 63 个 JSON/CLI/MCP/资产报告边界上作相同回放。数值证据与实际执行范围记录在 [VALIDATION.md](VALIDATION.zh-CN.md)。

[Cantera 的理想气体反应器方程](https://www.cantera.org/stable/reference/reactors/ideal-gas-reactor.html)提供控制容积的质量/组分/能量背景。[Ansys 火花点火发动机示例](https://chemkin.docs.pyansys.com/version/stable/examples/advanced/SI_engine_optimization.html)使用显式燃烧定时与 Wiebe 参数。这些参考促使形成这些契约;其精细化学、双区模型与示例参数并未被复制,也不被声称为对这一常物性求解器的验证。对这两个软件包都没有运行时依赖。全部示例参数仍为 `unverified`。
