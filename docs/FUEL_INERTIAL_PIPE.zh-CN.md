# 有限液体燃油管路动量

[English](FUEL_INERTIAL_PIPE.md) · **简体中文** · [Français](FUEL_INERTIAL_PIPE.fr.md) · [Русский](FUEL_INERTIAL_PIPE.ru.md) · [日本語](FUEL_INERTIAL_PIPE.ja.md) · [한국어](FUEL_INERTIAL_PIPE.ko.md) · [Deutsch](FUEL_INERTIAL_PIPE.de.md) · [Español](FUEL_INERTIAL_PIPE.es.md) · [Italiano](FUEL_INERTIAL_PIPE.it.md) · [Português](FUEL_INERTIAL_PIPE.pt-BR.md)

## 物理契约

`fuel_inertial_pipe` 连接两个有限燃油节点和有限热壁面。水平刚性管路拥有独立的液体质量、热内能与动能，不替代两端容器库存。管内密度恒定，温度随完全混合库存变化。端口及整条管路必须保持液态；现有准静态连接保留各自的两相行为。

声明长度、面积、线性及二次阻力、初始流量、初始温度与 `fluid_heat_fraction`。阻力单位为 `pa_s_m3` 和 `pa_s2_m6`。两端燃油材料属性与密度必须匹配。没有直接开度输入：关闭其他阀门不会抹去管路动量。材料及化学能参考继承自端口。

## 能量与端口

上游容器输出供给焓。管路将输入显热与自身液体库存混合，再以接收端压力向下游输出混合焓。压力功改变动能并且只计一次摩擦热。声明比例的摩擦热加热管内液体，其余加热有限壁面。流向反转时交换供给端与接收端，仍采用相同平衡。

```text
M_line = rho L A; I = rho L / A
E_line = U_line + I Q^2/2 + M_line LHV
D = R Q^2 + K abs(Q)^3
dM_A/dt = -rho Q; dM_B/dt = rho Q
I dQ/dt = p_A - p_B - R Q - K Q abs(Q)
dU_line/dt = rho abs(Q) (u_donor-u_line) + xi D
dE_kinetic/dt = (p_A-p_B) Q - D
dE_wall/dt = (1-xi) D
h_port = u + p/rho
```

动量方程采用中点流量及接受的末态压力；端口能量与管内混合采用后向欧拉，因此完整耦合为一阶收敛。即使没有摩擦，末态压力离散也会将部分压力/动能振荡转为数值显热；该部分不记作物理摩擦。成对熵变化包含混合与时间离散。零流量事件分别保留正反向输运历史。

## 使用与证据

运行 `inertial-fuel-line` 检查反向流及热混合，或运行 `inertial-pump-fed-cylinder` 检查入口管路、泵、物理电磁针阀及规定燃烧。读取 `volume_flow`、`mass_flow`、`forward_volume`、`reverse_volume`、`kinetic_energy`、`energy_at_a`、`energy_at_b`、热库存及完整账本。体积流量为瞬时值，质量流量为上一节拍均值。管路具有额外质量及化学能库存。

独立有限柔性谐振解及单独的非线性动量/温度 RK4 积分验证精度与一阶细化。检查覆盖摩擦热分配、成对熵、物理反转、完整能量/质量/燃油账本、离合器试探区间、无分配步进、回滚、取消及独立分支。便携资产和实际 MCP 检查每个采样边界。这不证明硬件标定或实际 Unity 验收。

## 有效范围与后续工作

参数保持 `unverified`。遵守材料温压区间与压力修正的液态稳定条件。失败区间完整回滚；减小节拍并检查源耗尽、压力及相态边界。模型不包含变密度两相动量、空化、声波、重力、运动管形及解析热前沿。规定燃烧不证明详细化学平衡或反应熵准确性。

将可压缩管路接入相容的轴驱动腔室和针阀／喷嘴边界，明确压力功与相态假设。之后继续相变弛豫／滑移、点火、进排气、机械损失及 ECU/TCU 协同控制。实测标定和实际 Unity 验收分别开展。 [可压缩平衡燃料管路](EQUILIBRIUM_FUEL_DUCT.zh-CN.md)

[Modelica: momentum and energy balances](https://doc.modelica.org/Modelica%203.2.3/Resources/helpWSM/Modelica/Modelica.Fluid.UsersGuide.ComponentDefinition.BalanceEquations.html) · [Modelica: dynamic pipe boundaries](https://doc.modelica.org/om/Modelica.Fluid.Pipes.DynamicPipe.html) · [VALIDATION.md](VALIDATION.md)
