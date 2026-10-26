# 可压缩平衡燃料管路

[English](EQUILIBRIUM_FUEL_DUCT.md) · **简体中文** · [Français](EQUILIBRIUM_FUEL_DUCT.fr.md) · [Русский](EQUILIBRIUM_FUEL_DUCT.ru.md) · [日本語](EQUILIBRIUM_FUEL_DUCT.ja.md) · [한국어](EQUILIBRIUM_FUEL_DUCT.ko.md) · [Deutsch](EQUILIBRIUM_FUEL_DUCT.de.md) · [Español](EQUILIBRIUM_FUEL_DUCT.es.md) · [Italiano](EQUILIBRIUM_FUEL_DUCT.it.md) · [Português](EQUILIBRIUM_FUEL_DUCT.pt-BR.md)

## 模型与材料

`equilibrium_fuel_duct` 通过显式的 `compressible_fuel` 单元输运质量、动量和总能量。液体可压缩，蒸气为理想气体；两相共享压力、温度和速度。局部平衡允许凝结与闪蒸。两个有限静止储罐和一个有限热壁面闭合各项收支。

`compressible_fuel_chamber` 可通过轴功改变任一混合储腔的容积。储腔主体速度保持为零，管路内部单元保持刚性。静止端口／壳体冲量不做功，活塞压力功作为独立机械交换核算。 [轴驱动可压缩燃料腔室](COMPRESSIBLE_FUEL_CHAMBER.zh-CN.md)

液体采用零热膨胀和恒定比热。`K` 是零压参考体积模量，实际模量为 `K+p`。冷压缩能、化学势、熵和平衡声速来自同一势函数。`a` 为已有液相能量偏置，`p0(T)` 为忽略液体体积的饱和曲线，`x` 为蒸气质量分数，`Cv_eq` 为混合物内能对温度在定比容下的导数。`Tr`、`Pref` 和 `Lu_ref` 分别是材料参考温度、压力和潜内能。所有量使用 SI 单位。

```text
p_l = K (rho_l/rho0 - 1); y = p/K
v_l = 1/[rho0 (1+y)]
u_cold = (K/rho0) [ln(1+y) - y/(1+y)]
u_l = cl T + a + u_cold
h_l = cl T + a + (K/rho0) ln(1+y)
s_l = cl ln(T/Tr)
u_v = cv T; v_v = R T/p; h_v = (cv+R) T
s_v = (cv+R) ln(T/Tr) - R ln(p/Pref) + Lu_ref/Tr + R
R T ln[p_sat/p0(T)] = (K/rho0) ln(1+p_sat/K)
v = (1-x) v_l + x v_v; u = (1-x) u_l + x u_v
dp_sat/dT = (h_v-h_l)/[T(v_v-v_l)]
c_eq^2 = v^2 T (dp_sat/dT)^2 / Cv_eq
c_l^2 = K/rho0; c_v^2 = (1+R/cv) R T
```

## 输运与边界

声明 `liquid_bulk_modulus`，省略 `compliance`，并为每个节点显式给出 `initial_velocity`。声明 1–24 个有序且不重复的内部 `cells`，每个单元只能属于一个管路。单元长度等于体积除以 `area`。储罐速度为零，且不能兼作内部单元。所有端口使用相同材料。`linear_resistance` 和 `quadratic_resistance` 表示整条管路的阻力，按单元体积分配；`fluid_heat_fraction` 在流体与壁面之间分配摩擦热。没有直接开度输入。

```text
U = [rho, rho velocity, rho (u + velocity^2/2)]
F = [rho velocity, rho velocity^2+p, (rho (u+velocity^2/2)+p) velocity]
F_Rusanov = (F_left+F_right)/2 - a_max (U_right-U_left)/2
a_max = max_ports(abs(velocity)) + sqrt(max(K/rho0, (1+R/cv) R Tmax))
```

守恒 Rusanov 通量使用同一子步状态推进所有界面。速度上界取最大速度绝对值加材料声速上界。CFL 限制为 0.2 乘最小端口体积再除以截面积和速度，每个积分区间最多 4096 个子步。输运与局部精确阻力衰减采用一阶分裂。数值扩散计入总熵变，与单独报告的摩擦热区分。静止边界吸收储罐动量，机械功为零；流入动能在有限储罐内转化为内能。无效状态或超过舍入误差的熵减会拒绝整个批次。拒绝后应减小时间步，并检查材料边界与单元分辨率。

## 使用与证据

运行 `equilibrium-fuel-condensing-duct` 或 `equilibrium-fuel-flashing-duct`。每个单元提供密度、速度、相分数、声速、温度和能量通道，可观测端口质量与能量、壁面冲量及完整库存。独立检查覆盖液体压缩功、理想蒸气极限、化学势相等、Clapeyron 与 Gibbs 关系、通过熵反演得到的声速、质量能量反演、守恒与有限壁面摩擦。声波单元平均误差在 8、16、24 个单元上依次减小。精确回放、回滚、取消、分支、无分配步进、可移植资产与实际 MCP 分别验证；详细结果保存在验证证据中。

[VALIDATION.zh-CN.md](VALIDATION.zh-CN.md)

## 适用范围与后续工作

参数仍为 `unverified`。须处于声明的温压范围内，并满足潜焓为正、两相共存比容分离及稀薄蒸气条件。这是采用瞬时均相平衡的刚性等截面水平管路，未包含有限速率成核、相间滑移、表面张力、重力、管壁弹性或真实流体临界行为。一阶数值扩散会抹平相变及声波前沿；守恒与回放不能证明空间精度或实测硬件有效性。实际 Unity 验收仍待完成。

增加相容的可压缩方向阀和针阀／喷嘴边界，明确焓、动能与相态假设。之后继续相变弛豫／滑移、点火、进排气、机械损失及 ECU/TCU 协同控制。实测标定和实际 Unity 验收分别开展。

[NIST: thermodynamic derivatives](https://pages.nist.gov/teqp-docs/en/latest/derivs/derivs.html) · [Lund: two-phase relaxation hierarchy](https://www.sintef.no/globalassets/project/co2-dynamics/publications/lund_two-phase_relaxation_hierarchy.pdf) · [MFEM: Rusanov flux](https://docs.mfem.org/html/classmfem_1_1RusanovFlux.html) · [Modelica: conservation balances](https://doc.modelica.org/Modelica%203.2.3/Resources/helpWSM/Modelica/Modelica.Fluid.UsersGuide.ComponentDefinition.BalanceEquations.html)
