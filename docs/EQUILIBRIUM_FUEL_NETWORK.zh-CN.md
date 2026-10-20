# 相平衡燃料输运网络

[English](EQUILIBRIUM_FUEL_NETWORK.md) · **简体中文** · [Français](EQUILIBRIUM_FUEL_NETWORK.fr.md) · [Русский](EQUILIBRIUM_FUEL_NETWORK.ru.md) · [日本語](EQUILIBRIUM_FUEL_NETWORK.ja.md) · [한국어](EQUILIBRIUM_FUEL_NETWORK.ko.md) · [Deutsch](EQUILIBRIUM_FUEL_NETWORK.de.md) · [Español](EQUILIBRIUM_FUEL_NETWORK.es.md) · [Italiano](EQUILIBRIUM_FUEL_NETWORK.it.md) · [Português](EQUILIBRIUM_FUEL_NETWORK.pt-BR.md)

## 模型范围

`fuel` 域在有限弹性容器中共同输送纯燃料液体与蒸气。每个容器独立保存总质量及热量加弹性储能，相平衡闪蒸据此确定温度、压力和蒸气比例。`fuel_restriction` 交换均匀混合的供体流体，`fuel_heat_link` 与有限热节点换热。燃料热值对应的化学能使用独立账本。

燃料节点的 `storage` 为参考体积，单位 `m3` 或 `l`；`initial` 为温度，单位 `k`；`position` 为总质量，单位 `kg`。`fuel` 对象显式声明物性、顺应性以及压力/温度边界。相连容器共用密度、热量与相参考、温度边界及热值。流阻两端均为有限燃料节点，热链接连接燃料与热节点。流阻开度为 [0,1] 比例，可配置输入通道。

## 守恒流动

流阻采用准静态耗散参考体积定律，显式体积流导 K 的单位为 `m3_s_pa`。模型忽略链接惯性、动能储存与相间滑移。供体密度将参考体积流转换为燃料总质量流，供体焓携带两相能量。不添加额外外部压力功源，也不为压降单独再加一项热量；弹性功已储存在容器能量内。

```text
Q_ref = K opening (p_s - p_r)
mdot = Q_ref / v_s
q = dt K opening (p_s' - p_r') / v_s'
H = q h_s'
M_s' = M_s - q; E_s' = E_s - H
M_r' = M_r + q; E_r' = E_r + H
h = (E - E_elastic + p V) / M
```

## 时间积分与熵

有界后向 Euler 求解为两个容器确定同一组已接受质量 q 与焓 H。链接按稳定组件 ID 顺序推进，有限热浴换热使用另一守恒子步。耦合方法为一阶。质量、温度或压力越界及求解失败均拒绝整批，包括输入、补偿历史和离合器试探区间。

兼容物性与正的压差流导使连续成对熵产生非负。后向 Euler 还会增加数值熵。`entropy_produced` 通道记录已接受的完整成对熵变化，包括混合与时间离散效应，不代表实测损失图。守恒与细化之外还进行独立熵检查。

## 使用与证据

通过 MCP 请求 `equilibrium-fuel-network` 或 `electrothermal-fuel-network`，也可用 CLI 运行同一 JSON。读取各容器的 `mass`、`internal_energy`、`elastic_energy`、`vapor_mass`、`vapor_mass_fraction`、`vapor_volume_fraction`、`entropy` 和 `specific_enthalpy`。这里 `internal_energy` 包含弹性壁储能，化学能单独报告。链接公开有符号质量/体积流、累计转移质量、`transport_energy` 和成对熵变化。

实验覆盖液体排出并进入共存区、反向流、开度变化及有限壁/机电热耦合。独立液体压力指数解、同时求解两相质量/焓/传热的 ODE、热驱动反流及离合器试探捕获检查精度与组合。资产和实际 MCP 保持每个采样值及哈希一致。资产 v34 保存新物性记录并读取 v1-v33。实际 Unity Editor/Play/Player/IL2CPP 仍未验证。

## 后续工作

燃料域输运现可与轴驱动 `fuel_chamber` 及理想 `fuel_check_valve` 链接组合。物理喷油器交接、局部惯性/临界流及实测真实混合物行为仍需单独推进。参数保持 `unverified`。 [轴驱动相变燃料泵](FUEL_CHAMBER_PUMP.zh-CN.md)

[Modelica: two-port transport](https://doc.modelica.org/om/Modelica.Fluid.Interfaces.PartialTwoPortTransport.html) · [CompliantFuelVolume](COMPLIANT_FUEL_VOLUME.zh-CN.md) · [VALIDATION.md](VALIDATION.zh-CN.md)
