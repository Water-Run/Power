# 液体入口动量参考

[English](LIQUID_COLUMN.md) · **简体中文** · [Français](LIQUID_COLUMN.fr.md) · [Русский](LIQUID_COLUMN.ru.md) · [日本語](LIQUID_COLUMN.ja.md) · [한국어](LIQUID_COLUMN.ko.md) · [Deutsch](LIQUID_COLUMN.de.md) · [Español](LIQUID_COLUMN.es.md) · [Italiano](LIQUID_COLUMN.it.md) · [Português](LIQUID_COLUMN.pt-BR.md)

## 物理边界

`LiquidColumn` 是独立 C# 参考模型，描述水平刚性管中的恒定截面、均匀流速、恒定液体密度与恒定温度。每个局部区间内给定绝对端口压力。线性和二次阻力系数是显式研究输入。它不是图元件，不改变现有燃油网络或喷嘴方程。

## 动量与能量

两端口具有相同流量与流速，对流动能通量抵消；压力功改变液柱储存的动能并产生摩擦热。等温热浴接收该热量，流体温度和内能保持不变。返回的熵产等于摩擦热除以声明温度。该边界不表示有限热库存。

```text
I = rho L / A                         [Pa s^2 / m3]
R                                    [Pa s / m3]
K                                    [Pa s^2 / m6]
I dQ/dt = p_A - p_B - R Q - K Q abs(Q)
E_kinetic = I Q^2 / 2
Q_mean = (Q_before + Q_after) / 2
I (Q_after-Q_before) = dt (p_A-p_B-R Q_mean-K Q_mean abs(Q_mean))
delta_V = dt Q_mean
W_pressure = (p_A-p_B) delta_V
H_friction = dt (R Q_mean^2 + K abs(Q_mean)^3)
W_pressure = delta_E_kinetic + H_friction
S_produced = H_friction / T
```

隐式中点法求解单调标量动量方程。反向流动按同一方程在零流量事件处分段，分别保留正反向体积及热量。每次成功调用最多使用两个内部区间。若步长越过稳态流量平衡点，则返回 `StepTooLarge`，应缩短时长。能量和冲量残差按 binary64 舍入尺度检查。粗步长即使被接受，仍需检查精度。

## 核心 API 与数值证据

以显式 SI 几何、密度、阻力、温度和最小绝对压力构造 `LiquidColumn`。向 `Advance` 传入 `LiquidColumnState` 及以秒为单位的时长。属性名包含 SI 单位。结果包含有符号及分向输运、压力功、动能变化、摩擦热与熵产。拒绝时输出为默认值、输入不变；调用不分配托管内存。局部时长不替代仿真的整数时钟。

独立的无损加速、线性阻力指数解与二次阻力双曲正切解检查极限。独立积分的变压 RK4 参考验证包含反转的平滑二阶细化。测试还覆盖被动衰减、稳态流动、完整能量平衡、非负熵产、压力及粗步长拒绝、不可变共享使用，以及两个程序集目标上的分配。

## 后续耦合

参数保持 `unverified`。压力下限是调用方声明的液态有效边界，不是预测的蒸汽压或 NPSH 裕量。可压缩性、声波、重力、运动几何、热储存、空化、相间滑移和相变弛豫动力学不在本参考范围。现有两相燃油连接仍为准静态。实际 Unity 验收与实测硬件标定分别进行。

`fuel_inertial_pipe` 连接两个有限燃油节点和有限热壁面。水平刚性管路拥有独立的液体质量、热内能与动能，不替代两端容器库存。管内密度恒定，温度随完全混合库存变化。端口及整条管路必须保持液态；现有准静态连接保留各自的两相行为。 [有限液体燃油管路动量](FUEL_INERTIAL_PIPE.zh-CN.md)

[Modelica: momentum and energy balances](https://doc.modelica.org/Modelica%203.2.3/Resources/helpWSM/Modelica/Modelica.Fluid.UsersGuide.ComponentDefinition.BalanceEquations.html) · [Modelica: pipe flow models](https://doc.modelica.org/om/Modelica.Fluid.Pipes.BaseClasses.FlowModels.html) · [VALIDATION.md](VALIDATION.md)
